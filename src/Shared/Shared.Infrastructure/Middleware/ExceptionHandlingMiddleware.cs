using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Domain.Exceptions;
using System.Text.Json;

namespace Shared.Infrastructure.Middleware;


/// Tüm servislerde kullanılan merkezi hata yakalama middleware'i.
/// SRP: Hata yönetimi tek bir noktada toplanır; iş mantığına karışmaz.
/// Global exception handler — RFC 7807 ProblemDetails formatında yanıt döner.


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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string title;

        if (exception is ValidationException validationEx)
        {
            // FluentValidation.ValidationException → 400 Bad Request
            // ValidationBehavior pipeline'ından fırlatılır
            statusCode = 400;
            title      = "Validation Error";

            var validationProblem = new ValidationProblemDetails(
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status   = 400,
                Title    = title,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode  = 400;

            var vJson = JsonSerializer.Serialize(validationProblem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(vJson);
            return;
        }
        else if (exception is DomainException domainEx)
        {
            // DomainException → StatusCode domain exception'dan gelir
            // AuthException(401), ProductNotFoundException(404) vb. otomatik desteklenir
            statusCode = domainEx.StatusCode;
            title = statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                422 => "Unprocessable Entity",
                _ => "Domain Error"
            };
        }
        else
        {
            // Framework / infrastructure exception'ları
            (statusCode, title) = exception switch
            {
                KeyNotFoundException => (404, "Not Found"),
                UnauthorizedAccessException => (401, "Unauthorized"),
                ArgumentNullException => (400, "Bad Request"),
                ArgumentException => (400, "Bad Request"),
                InvalidOperationException => (400, "Invalid Operation"),
                NotSupportedException => (400, "Not Supported"),
                OperationCanceledException => (499, "Request Cancelled"),
                _ => (500, "Internal Server Error")
            };
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
