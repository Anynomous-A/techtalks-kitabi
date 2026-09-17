using System.ComponentModel.DataAnnotations;

namespace Kitabi.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
{

    var dto = context.Arguments.OfType<T>().FirstOrDefault();
    if (dto is null)
        return Results.BadRequest("Request body is missing or malformed.");

    var validationResults = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true);
    

    if (!isValid)
    {
        var errors = validationResults
            .GroupBy(v => v.MemberNames.FirstOrDefault() ?? "General")
            .ToDictionary(g => g.Key, g => g.Select(v => v.ErrorMessage ?? "Invalid value").ToArray());

        return Results.ValidationProblem(errors);
    }

    return await next(context);
}
}