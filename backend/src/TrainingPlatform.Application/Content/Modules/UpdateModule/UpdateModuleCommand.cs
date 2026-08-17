using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Activity;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Activity;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Modules.UpdateModule;

public sealed record UpdateModuleCommand(Guid ModuleId, string Title, int Order) : ICommand;

public sealed class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public UpdateModuleCommandValidator()
    {
        RuleFor(c => c.ModuleId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Order).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateModuleCommandHandler(
    IApplicationDbContext dbContext,
    IActivityLogService activityLog,
    IUserContext currentUser) : ICommandHandler<UpdateModuleCommand>
{
    public async Task<Result> Handle(UpdateModuleCommand command, CancellationToken cancellationToken)
    {
        var module = await dbContext.Modules.SingleOrDefaultAsync(m => m.Id == command.ModuleId, cancellationToken);
        if (module is null)
        {
            return Result.Failure(ContentErrors.ModuleNotFound(command.ModuleId));
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(c => c.Id == module.CourseId, cancellationToken);
        if (course is null || !CourseAccess.CanManage(course, currentUser))
        {
            return Result.Failure(ContentErrors.NotCourseOwner);
        }

        module.UpdateDetails(command.Title, command.Order);
        await dbContext.SaveChangesAsync(cancellationToken);

        await activityLog.LogAsync(currentUser.UserId, ActivityActions.ModuleUpdated, "Module", module.Id.ToString(), cancellationToken);

        return Result.Success();
    }
}
