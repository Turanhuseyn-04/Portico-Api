using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portico.Api.Security;
using Repository.Data;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using Service.Helpers.Email;

namespace Portico.Tests.Integration;

// Runs the real Portico.Api pipeline (routing, JWT auth, role checks, middleware, services, repositories) in memory.
// Only the edges are swapped: SQL Server becomes EF InMemory, emails are captured, and the SQL Server-only
// booking lock runs the work directly.
public class PorticoApiFactory : WebApplicationFactory<Program>
{
    public const string SuperAdminEmail = "boss@portico.test";
    public const string SuperAdminPassword = "boss-password-1";
    public const string ConfirmUrl = "https://web.test/confirm-email";

    private readonly string _database = "portico-tests-" + Guid.NewGuid();
    private readonly string _scratch = Path.Combine(Path.GetTempPath(), "portico-tests", Guid.NewGuid().ToString("N"));

    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" keeps user-secrets and appsettings.Development.json out of the run.
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("SuperAdmin:Email", SuperAdminEmail);
        builder.UseSetting("SuperAdmin:Password", SuperAdminPassword);
        builder.UseSetting("Email:ConfirmUrl", ConfirmUrl);
        builder.UseSetting("Email:PickupDirectory", Path.Combine(_scratch, "mail"));
        builder.UseSetting("LogFiles:Directory", Path.Combine(_scratch, "logs"));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PorticoDbContext>>();
            services.AddDbContext<PorticoDbContext>(options => options.UseInMemoryDatabase(_database));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<IReservationRepository>();
            services.AddScoped<ReservationRepository>();
            services.AddScoped<IReservationRepository, UnlockedReservationRepository>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(_scratch, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    public HttpClient ClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<string> SignInAsync(string email, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/account/signin", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.ReadAsync<SignedIn>()).Token;
    }

    public async Task<HttpClient> SuperAdminAsync() => ClientWithToken(await SignInAsync(SuperAdminEmail, SuperAdminPassword));

    // Registers a guest, opens the link from the captured email, signs in, and returns a client carrying the token.
    public async Task<HttpClient> NewGuestAsync(string? email = null, string password = "guest-password-1")
    {
        email ??= $"guest-{Guid.NewGuid():N}@portico.test";
        var anonymous = CreateClient();
        (await anonymous.PostAsJsonAsync("/account/register", new { name = "Test Guest", email, password })).EnsureSuccessStatusCode();
        (await anonymous.PostAsJsonAsync("/account/confirm", new { email, token = Emails.ConfirmTokenFor(email) })).EnsureSuccessStatusCode();
        return ClientWithToken(await SignInAsync(email, password));
    }
}

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> Sent => _sent.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public string ConfirmTokenFor(string email)
    {
        var mail = Sent.Last(m => m.To == email && m.Subject == "Confirm your Portico email");
        var link = mail.Text.Split('\n').Single(line => line.StartsWith(PorticoApiFactory.ConfirmUrl));
        return HttpUtility.ParseQueryString(new Uri(link).Query)["token"]!;
    }
}

// EF InMemory has no transactions or sp_getapplock, so the lock is skipped; everything else is the real repository.
public sealed class UnlockedReservationRepository : IReservationRepository
{
    private readonly ReservationRepository _inner;

    public UnlockedReservationRepository(ReservationRepository inner) => _inner = inner;

    public Task<IReadOnlyList<Portico.Domain.Reservations.Reservation>> ListAsync(string? email, CancellationToken cancellationToken) => _inner.ListAsync(email, cancellationToken);
    public Task<Portico.Domain.Reservations.Reservation?> FindByReferenceAsync(string reference, bool tracking, CancellationToken cancellationToken) => _inner.FindByReferenceAsync(reference, tracking, cancellationToken);
    public Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken) => _inner.ReferenceExistsAsync(reference, cancellationToken);
    public Task AddAsync(Portico.Domain.Reservations.Reservation reservation, CancellationToken cancellationToken) => _inner.AddAsync(reservation, cancellationToken);
    public Task SaveAsync(CancellationToken cancellationToken) => _inner.SaveAsync(cancellationToken);
    public Task<T> WithPropertyLockAsync<T>(string propertyId, Func<Task<T>> work, CancellationToken cancellationToken) => work();
}

public static class HttpTestExtensions
{
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>())!;

    public static async Task<string[]> ErrorsAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ErrorBody>())!.Errors;

    public sealed record ErrorBody(bool IsSuccess, string[] Errors);
}
