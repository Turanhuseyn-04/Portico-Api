using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Portico.Api.Middlewares;
using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Portico.Tests.Unit.Api;

public class TokenServiceTests
{
    private static readonly JwtOptions Options = new() { Key = new string('k', 48), Days = 7 };

    [Fact]
    public void Issue_SignsTokenWithAccountClaimsAndExpiry()
    {
        var signedIn = new TokenService(Options).Issue(new AccountProfile("Turan", "turan@portico.test", AccountRoles.Admin));

        Assert.Equal(("Turan", "turan@portico.test", "Admin"), (signedIn.Name, signedIn.Email, signedIn.Role));
        Assert.InRange(signedIn.ExpiresAt, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7));

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(signedIn.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = Options.SigningKey()
        }, out _);

        Assert.Equal("turan@portico.test", principal.EmailOf());
        Assert.Equal("Admin", principal.RoleOf());
        Assert.Equal("Turan", principal.FindFirstValue(PorticoClaims.Name));
    }

    [Fact]
    public void Issue_TokenFailsValidationWithAnotherKey()
    {
        var signedIn = new TokenService(Options).Issue(new AccountProfile("T", "t@portico.test", "Guest"));

        Assert.ThrowsAny<SecurityTokenException>(() => new JwtSecurityTokenHandler().ValidateToken(signedIn.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new JwtOptions { Key = new string('x', 48) }.SigningKey()
        }, out _));
    }

    [Theory]
    [InlineData("guest@portico.test", true)]
    [InlineData(" GUEST@portico.test ", true)]
    [InlineData("other@portico.test", false)]
    public void IsSelf_ComparesEmailIgnoringCaseAndSpaces(string email, bool expected)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(PorticoClaims.Email, "guest@portico.test") }, "test"));
        Assert.Equal(expected, user.IsSelf(email));
    }

    [Fact]
    public void RoleOf_NoRoleClaim_IsEmpty()
    {
        Assert.Equal("", new ClaimsPrincipal(new ClaimsIdentity()).RoleOf());
    }
}

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> Run(Exception ex)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = context.Response.Body.Length == 0 ? default : JsonDocument.Parse(context.Response.Body).RootElement;
        return (context.Response.StatusCode, body);
    }

    public static TheoryData<Exception, int> Mapped => new()
    {
        { new NotFoundException("missing"), 404 },
        { new BadRequestException("missing"), 400 },
        { new ConflictException("missing"), 409 },
        { new ForbiddenException("missing"), 403 },
        { new TooManyRequestsException("missing"), 429 },
    };

    [Theory]
    [MemberData(nameof(Mapped))]
    public async Task KnownException_MapsToStatusWithItsMessage(Exception ex, int status)
    {
        var (code, body) = await Run(ex);

        Assert.Equal(status, code);
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("missing", body.GetProperty("errors")[0].GetString());
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutLeakingDetails()
    {
        var (code, body) = await Run(new InvalidOperationException("connection string secret"));

        Assert.Equal(500, code);
        var message = body.GetProperty("errors")[0].GetString();
        Assert.DoesNotContain("secret", message);
    }

    [Fact]
    public async Task DatabaseException_Returns500()
    {
        var (code, _) = await Run(new DbUpdateException("constraint"));
        Assert.Equal(500, code);
    }

    [Fact]
    public async Task NoException_LeavesResponseAlone()
    {
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(c => { c.Response.StatusCode = 201; return Task.CompletedTask; }, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(201, context.Response.StatusCode);
    }
}

public class AuditMiddlewareTests
{
    private readonly Mock<IAuditService> _audit = new();

    private static DefaultHttpContext Context(string method, string path, string? role, int status = 200)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.StatusCode = status;
        if (role is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(PorticoClaims.Email, "staff@portico.test"),
                new Claim(PorticoClaims.Role, role)
            }, "test"));
        }
        return context;
    }

    private Task Invoke(HttpContext context, string? detail = null) =>
        new AuditMiddleware(c => { if (detail is not null) c.Items[AuditMiddleware.DetailKey] = detail; return Task.CompletedTask; },
            NullLogger<AuditMiddleware>.Instance).InvokeAsync(context, _audit.Object);

    [Fact]
    public async Task StaffChangeUnderAdmin_IsRecordedWithStatusAndDetail()
    {
        await Invoke(Context("PUT", "/admin/slides/3", AccountRoles.Admin, 409), "slide 3");

        _audit.Verify(a => a.RecordAsync(
            new AuditRecord("staff@portico.test", "Admin", "PUT", "/admin/slides/3", 409, "slide 3"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("GET", "/admin/slides", "Admin")]      // reads are not audited
    [InlineData("POST", "/reservations", "Admin")]     // outside /admin
    [InlineData("POST", "/admin/slides", "Guest")]     // not staff
    [InlineData("POST", "/admin/slides", null)]        // anonymous
    public async Task OtherRequests_AreNotRecorded(string method, string path, string? role)
    {
        await Invoke(Context(method, path, role));
        _audit.Verify(a => a.RecordAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AuditFailure_DoesNotBreakTheRequest()
    {
        _audit.Setup(a => a.RecordAsync(It.IsAny<AuditRecord>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        var context = Context("DELETE", "/admin/videos/1", AccountRoles.SuperAdmin, 204);

        await Invoke(context);

        Assert.Equal(204, context.Response.StatusCode);
    }
}
