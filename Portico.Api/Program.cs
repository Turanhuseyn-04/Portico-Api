using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Portico.Api.Middlewares;
using Portico.Api.Security;
using Repository;
using Repository.Data;
using Service;
using Service.Helpers.Email;
using Service.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddDbContext<PorticoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Portico")));
builder.Services.AddRepositoryLayer();
builder.Services.AddServiceLayer();

// Tokens are signed with Jwt:Key. Without a key of at least 32 characters the API does not start.
// The key is never committed: in development it lives in user-secrets, elsewhere in the environment (Jwt__Key).
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be set to at least 32 characters. In development run: dotnet user-secrets set Jwt:Key <random value> --project Portico.Api");
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();

// Confirmation emails go through Email:Smtp when a host is set, otherwise into the Email:PickupDirectory folder.
var email = builder.Configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
if (string.IsNullOrWhiteSpace(email.ConfirmUrl))
    throw new InvalidOperationException("Email:ConfirmUrl must point at the Web's confirm-email page.");
email.PickupDirectory = Path.GetFullPath(email.PickupDirectory, builder.Environment.ContentRootPath);
builder.Services.AddSingleton(email);
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.SigningKey(),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = PorticoClaims.Name,
            RoleClaimType = PorticoClaims.Role
        };
        options.Events = new JwtBearerEvents
        {
            // The role comes from the database on every request, so a demotion or a deleted account applies at once.
            OnTokenValidated = async context =>
            {
                var email = context.Principal?.EmailOf();
                var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountService>();
                var role = string.IsNullOrWhiteSpace(email) ? null : await accounts.CurrentRoleAsync(email, context.HttpContext.RequestAborted);
                if (role is null || context.Principal?.Identity is not ClaimsIdentity identity)
                {
                    context.Fail("The account no longer exists.");
                    return;
                }

                foreach (var claim in identity.FindAll(PorticoClaims.Role).ToList()) identity.RemoveClaim(claim);
                identity.AddClaim(new Claim(PorticoClaims.Role, role));
            }
        };
    });
builder.Services.AddAuthorization();

// Portico.Web calls the API from its server, so browsers need no CORS. Development allows any localhost page;
// elsewhere only the origins listed in Cors:Origins, none by default.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Portico", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Host == "localhost" || uri.Host == "127.0.0.1"));
        }
        else
        {
            policy.WithOrigins(corsOrigins);
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<AuditMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("Portico");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PorticoDbContext>();
    await CatalogSeed.EnsureAsync(db, CancellationToken.None);

    // The first SuperAdmin comes from configuration, kept in user-secrets in development.
    // SuperAdmin:Email and SuperAdmin:Password are read first; Admin:Email and Admin:Password still work.
    var seed = app.Configuration.GetSection("SuperAdmin");
    if (string.IsNullOrWhiteSpace(seed["Email"])) seed = app.Configuration.GetSection("Admin");
    if (!string.IsNullOrWhiteSpace(seed["Email"]) && !string.IsNullOrWhiteSpace(seed["Password"]))
    {
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        await accounts.EnsureSuperAdminAsync(seed["Name"] ?? "Portico admin", seed["Email"]!, seed["Password"]!, CancellationToken.None);
    }
    else
    {
        app.Logger.LogWarning("No SuperAdmin is configured. Set SuperAdmin:Email and SuperAdmin:Password to create one.");
    }
}

app.Run();
