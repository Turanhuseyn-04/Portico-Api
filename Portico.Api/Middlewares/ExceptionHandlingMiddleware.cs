using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Service.Helpers.Exceptions;

namespace Portico.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            Expected(context, ex, StatusCodes.Status404NotFound);
            await WriteAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (BadRequestException ex)
        {
            Expected(context, ex, StatusCodes.Status400BadRequest);
            await WriteAsync(context, StatusCodes.Status400BadRequest, ex.Errors);
        }
        catch (ConflictException ex)
        {
            Expected(context, ex, StatusCodes.Status409Conflict);
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            Expected(context, ex, StatusCodes.Status403Forbidden);
            await WriteAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (TooManyRequestsException ex)
        {
            Expected(context, ex, StatusCodes.Status429TooManyRequests);
            await WriteAsync(context, StatusCodes.Status429TooManyRequests, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("{Method} {Path} was cancelled by the caller.", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex) when (IsDatabase(ex))
        {
            _logger.LogError(ex, "Database error during {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception during {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Something went wrong. Please try again later.");
        }
    }

    // A refusal the API means to give, such as a stay that does not exist. The request log already records the status,
    // so the reason is kept at Debug for when it is needed. These messages are written for guests and hold no secrets.
    private void Expected(HttpContext context, Exception ex, int statusCode)
    {
        _logger.LogDebug("{Method} {Path} answered {StatusCode}: {Reason}", context.Request.Method, context.Request.Path, statusCode, ex.Message);
    }

    private static bool IsDatabase(Exception ex) => ex is DbUpdateException or DbException || ex.InnerException is DbException;

    private static Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        return WriteAsync(context, statusCode, [message]);
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string[] errors)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            isSuccess = false,
            errors
        });
    }
}
