using Microsoft.AspNetCore.Mvc;
using System.Net;
using FluentValidation;

namespace ATS.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, detail) = exception switch
        {
            ValidationException validationException => (
                (int)HttpStatusCode.BadRequest, 
                "Validation Error", 
                string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))),
            _ => (
                (int)HttpStatusCode.InternalServerError, 
                "Server Error", 
                "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
