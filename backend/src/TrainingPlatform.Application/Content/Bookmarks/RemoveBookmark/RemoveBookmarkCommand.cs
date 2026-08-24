using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;

namespace TrainingPlatform.Application.Content.Bookmarks.RemoveBookmark;

public sealed record RemoveBookmarkCommand(Guid CourseId) : ICommand;

public sealed class RemoveBookmarkCommandValidator : AbstractValidator<RemoveBookmarkCommand>
{
    public RemoveBookmarkCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}

public sealed class RemoveBookmarkCommandHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : ICommandHandler<RemoveBookmarkCommand>
{
    public async Task<Result> Handle(RemoveBookmarkCommand command, CancellationToken cancellationToken)
    {
        var bookmark = await dbContext.CourseBookmarks
            .SingleOrDefaultAsync(b => b.UserId == currentUser.UserId && b.CourseId == command.CourseId, cancellationToken);

        if (bookmark is null)
        {
            return Result.Success();
        }

        dbContext.CourseBookmarks.Remove(bookmark);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
