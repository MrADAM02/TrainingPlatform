using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Common.Messaging;

/// <summary>
/// Minimal, dependency-free CQRS dispatcher: resolves the matching *Handler from DI by
/// reflection and runs any registered FluentValidation validators for commands before invoking
/// the handler (queries are not validated, per REQ: only write operations are validated).
/// </summary>
internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    public async Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(command, cancellationToken);
        if (validationError is not null)
        {
            return Result.Failure(validationError);
        }

        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.Handle))!;

        return await (Task<Result>)method.Invoke(handler, [command, cancellationToken])!;
    }

    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(command, cancellationToken);
        if (validationError is not null)
        {
            return Result.Failure<TResponse>(validationError);
        }

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.Handle))!;

        return await (Task<Result<TResponse>>)method.Invoke(handler, [command, cancellationToken])!;
    }

    public async Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.Handle))!;

        return await (Task<Result<TResponse>>)method.Invoke(handler, [query, cancellationToken])!;
    }

    private async Task<ValidationError?> ValidateAsync(object command, CancellationToken cancellationToken)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(command.GetType());
        var validators = serviceProvider.GetServices(validatorType).Cast<IValidator>().ToList();

        if (validators.Count == 0)
        {
            return null;
        }

        var context = new ValidationContext<object>(command);

        var failures = new List<Error>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors.Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)));
        }

        return failures.Count == 0 ? null : new ValidationError([.. failures]);
    }
}
