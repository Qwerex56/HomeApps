using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Exceptions.Service;
using Shared.Exceptions.Validators;

namespace Shared.Middleware;

public class ExceptionHandlerMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        }
        catch (Exception e) {
            await HandleExceptionAsync(context, e);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context,  Exception exception) {
        var (statusCode, error) = MapExceptionToHttp(exception);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new {
            errorCode = error,
            message = exception.Message,
        };
        
        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode statusCode, string error) MapExceptionToHttp(Exception exception) {
        return exception switch {
            EmailDuplicationException => (HttpStatusCode.Conflict, "User with this email already exists."),
            ValidationException ve => (ve.HttpStatusCode, ve.Message), 
            
            _ => (HttpStatusCode.InternalServerError, "Server Error")
        };
    } 
}