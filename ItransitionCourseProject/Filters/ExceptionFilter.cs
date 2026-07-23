using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ItransitionCourseProject.Filters;

public sealed class ExceptionFilter : IExceptionFilter {
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger) {
        _logger = logger;
    }

    public void OnException(ExceptionContext context) {
        context.Result = context.Exception switch
        {
            KeyNotFoundException exception => new NotFoundObjectResult(new
            {
                message = exception.Message
            }),
            DbUpdateConcurrencyException exception => new ConflictObjectResult(new
            {
                message = exception.Message,
                conflict = true
            }),
            UnauthorizedAccessException exception => new ObjectResult(new
            {
                message = exception.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },
            InvalidOperationException exception => new BadRequestObjectResult(new
            {
                message = exception.Message
            }),
            _ => CreateServerError(context.Exception)
        };

        context.ExceptionHandled = true;
    }

    private ObjectResult CreateServerError(Exception exception) {
        _logger.LogError(exception, "Unhandled backend error.");
        return new ObjectResult(new
        {
            message = "An unexpected server error occurred."
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
}
