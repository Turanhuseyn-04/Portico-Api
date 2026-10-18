using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Middlewares;

// Records every change staff make under /admin, including the ones that were refused, after the response is decided.
// It sits outside the exception handler so the status it records is the one the caller received.
public class AuditMiddleware
{
    public const string DetailKey = "AuditDetail";

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService audit)
    {
        await _next(context);

        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method)) return;
        if (!context.Request.Path.StartsWithSegments("/admin")) return;

        var role = context.User.RoleOf();
        if (!AccountRoles.IsStaff(role)) return;

        try
        {
            await audit.RecordAsync(new AuditRecord(
                context.User.EmailOf() ?? "",
                role,
                context.Request.Method,
                context.Request.Path.Value ?? "",
                context.Response.StatusCode,
                context.Items[DetailKey] as string), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not write the audit entry for {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
    }
}
