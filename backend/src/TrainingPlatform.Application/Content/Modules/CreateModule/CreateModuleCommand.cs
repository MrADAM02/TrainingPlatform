using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Modules.CreateModule;

public sealed record CreateModuleCommand(Guid CourseId, string Title, int Order) : ICommand<Guid>;

public sealed class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Order).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateModuleCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<CreateModuleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateModuleCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure<Guid>(ContentErrors.CourseNotFound(command.CourseId));
        }

        if (!CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure<Guid>(ContentErrors.NotCourseOwner);
        }

        var module = Module.Create(command.CourseId, command.Title, command.Order);
        dbContext.Modules.Add(module);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.ModuleCreated, "Module", module.Id.ToString(), cancellationToken);

        return module.Id;
    }
}
