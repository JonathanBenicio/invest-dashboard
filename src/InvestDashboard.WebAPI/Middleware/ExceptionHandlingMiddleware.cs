using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Security.Authentication;
using InvestDashboard.Application.Exceptions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace InvestDashboard.WebAPI.Middleware
{
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
                _logger.LogError(ex, "An unhandled exception has occurred during request execution.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError; // 500 default
            var title = "An unexpected error occurred on the server.";
            var detail = "The request could not be completed.";

            if (exception is AuthenticationException)
            {
                code = HttpStatusCode.Unauthorized;
                title = "Authentication is required.";
            }
            else if (exception is RegistrationConflictException)
            {
                code = HttpStatusCode.Conflict;
                title = "Account already exists.";
                detail = exception.Message;
            }
            else if (exception is TransactionLedgerConflictException)
            {
                code = HttpStatusCode.Conflict;
                title = "Transaction history conflicts with this change.";
                detail = exception.Message;
            }
            else if (exception is DbUpdateConcurrencyException)
            {
                code = HttpStatusCode.Conflict;
                title = "Portfolio changed during this operation.";
                detail = "Reload the portfolio and try again.";
            }
            else if (exception is IdentityProviderUnavailableException)
            {
                code = HttpStatusCode.ServiceUnavailable;
                title = "Authentication provider is temporarily unavailable.";
            }
            else if (exception is MarketDataUnavailableException)
            {
                code = HttpStatusCode.ServiceUnavailable;
                title = "Market data is temporarily unavailable.";
            }
            else if (exception is ArgumentException || exception is InvalidOperationException)
            {
                code = HttpStatusCode.BadRequest;
                title = "Bad request. Please verify your parameters.";
                detail = exception.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                code = HttpStatusCode.Forbidden; // 403
                title = "Access denied.";
                detail = "You do not have permission to perform this operation.";
            }
            else if (exception is KeyNotFoundException)
            {
                code = HttpStatusCode.NotFound; // 404
                title = "The requested resource was not found.";
                detail = "The requested resource was not found.";
            }

            var problemDetails = new ProblemDetails
            {
                Status = (int)code,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.io/{(int)code}"
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)code;

            var result = JsonSerializer.Serialize(problemDetails);
            return context.Response.WriteAsync(result);
        }
    }
}
