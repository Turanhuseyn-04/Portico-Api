using Portico.Api.Security;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Portico.Api.Logging;

// One place decides where Portico.Api's logs go and what every entry carries. The rest of the code only asks for an
// ILogger<T>; levels per category come from the "Serilog:MinimumLevel" section of appsettings.
//
// Every entry goes to the console and to a JSON file per day under LogFiles:Directory (default "logs" next to the app),
// kept for LogFiles:RetainedDays days. Entries carry Application, Environment, RequestId and the W3C TraceId, which
// Portico.Web sends with its calls, so one guest action can be followed across both apps.
public static class LoggingSetup
{
    // Requests slower than this are logged as warnings even when they succeed.
    private const double SlowRequestMs = 2000;

    // Writes to the console until configuration is read, so a failure while starting is still recorded.
    public static void UseBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }

    public static WebApplicationBuilder AddPorticoLogging(this WebApplicationBuilder builder)
    {
        var files = builder.Configuration.GetSection("LogFiles");
        var directory = Path.GetFullPath(files["Directory"] ?? "logs", builder.Environment.ContentRootPath);
        var retainedDays = files.GetValue("RetainedDays", 14);

        builder.Host.UseSerilog((context, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(directory, "portico-api-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedDays,
                fileSizeLimitBytes: 50 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                shared: true));
        return builder;
    }

    // One entry per request: method, path, status and time taken. The query string is left out on purpose,
    // since links can carry emails and confirmation tokens.
    public static IApplicationBuilder UsePorticoRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.GetLevel = LevelFor;
            options.EnrichDiagnosticContext = (diagnostics, http) =>
            {
                var role = http.User.RoleOf();
                diagnostics.Set("UserRole", string.IsNullOrEmpty(role) ? "Anonymous" : role);
                if (http.GetEndpoint()?.DisplayName is { } endpoint) diagnostics.Set("Endpoint", endpoint);
            };
        });
    }

    private static LogEventLevel LevelFor(HttpContext http, double elapsedMs, Exception? ex)
    {
        if (ex is not null || http.Response.StatusCode >= StatusCodes.Status500InternalServerError) return LogEventLevel.Error;
        if (http.Response.StatusCode >= StatusCodes.Status400BadRequest || elapsedMs > SlowRequestMs) return LogEventLevel.Warning;
        if (http.Request.Path.StartsWithSegments("/swagger")) return LogEventLevel.Verbose;
        return LogEventLevel.Information;
    }
}
