using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Api.Endpoints;

public static class CustomResults
{
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create a problem result from a successful result.");
        }

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        if (result.Error is ValidationError validationError)
        {
            var errorsDict = validationError.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return Microsoft.AspNetCore.Http.Results.ValidationProblem(errorsDict);
        }

        return Microsoft.AspNetCore.Http.Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Description,
            statusCode: statusCode);
    }
}
