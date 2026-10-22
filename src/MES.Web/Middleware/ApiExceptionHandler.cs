using MES.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace MES.Web.Middleware;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var originalPath = context.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? context.Request.Path.Value ?? "";
        if (!new PathString(originalPath).StartsWithSegments("/api")) return false;
        var status = exception switch
        {
            DataAccessException => 503,
            ArgumentException => 400,
            _ => 500
        };
        logger.LogError(exception, "Błąd API. TraceId: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == 400 ? "Nieprawidłowe parametry." : "Nie udało się wykonać operacji.",
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken);
        return true;
    }
}

