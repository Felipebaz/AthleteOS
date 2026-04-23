using AthleteOS.BuildingBlocks.Domain.Results;
using FluentValidation;
using MediatR;

namespace AthleteOS.BuildingBlocks.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        // Combine all validation errors into a single Error.
        var errors = failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToArray();

        // Return as a validation error Result; throw only if TResponse is not a Result.
        if (typeof(TResponse).IsAssignableTo(typeof(Result)))
        {
            // Use the first error to represent the failure (errors[0]).
            // Full list accessible via the FluentValidation exception if needed.
            var error = errors[0];
            var resultType = typeof(TResponse);

            // Result (non-generic)
            if (resultType == typeof(Result))
                return (TResponse)(object)Result.Failure(error);

            // Result<T>
            var valueType = resultType.GetGenericArguments()[0];
            var method = typeof(Result)
                .GetMethods()
                .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                .MakeGenericMethod(valueType);

            return (TResponse)method.Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
