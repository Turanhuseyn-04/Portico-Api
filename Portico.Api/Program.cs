using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portico.Api.Middlewares;
using Repository;
using Repository.Data;
using Service;
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Host == "localhost" || uri.Host == "127.0.0.1"))
            .AllowAnyHeader()
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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("Development");

app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PorticoDbContext>();
    await CatalogSeed.EnsureAsync(db, CancellationToken.None);

    // The first admin comes from configuration: Admin:Email and Admin:Password, kept in user-secrets in development.
    var admin = app.Configuration.GetSection("Admin");
    if (!string.IsNullOrWhiteSpace(admin["Email"]) && !string.IsNullOrWhiteSpace(admin["Password"]))
    {
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
        await accounts.EnsureAdminAsync(admin["Name"] ?? "Portico admin", admin["Email"]!, admin["Password"]!, CancellationToken.None);
    }
    else
    {
        app.Logger.LogWarning("No admin is configured. Set Admin:Email and Admin:Password to create one.");
    }
}

app.Run();
