using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Application.Abstractions.Messaging;
using TrainingPlatform.Domain.Common;
using TrainingPlatform.Domain.Content;

namespace TrainingPlatform.Application.Content.Bookmarks.AddBookmark;

/// <summary>Saves a course to the caller's "My Library" (2026-08-24 redesign). Any authenticated
/// user can bookmark any course they can view (owned/managed, or published) — same boundary as
/// browsing the catalog, not enrollment.</summary>
public sealed record AddBookmarkCommand(Guid CourseId) : ICommand;

public sealed class AddBookmarkCommandValidator : AbstractValidator<AddBookmarkCommand>
{
    public AddBookmarkCommandValidator()
    {
        RuleFor(c => c.CourseId).NotEmpty();
    }
}

public sealed class AddBookmarkCommandHandler(IApplicationDbContext dbContext, IUserContext currentUser)
    : ICommandHandler<AddBookmarkCommand>
{
    public async Task<Result> Handle(AddBookmarkCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken);
        if (course is null)
        {
            return Result.Failure(ContentErrors.CourseNotFound(command.CourseId));
        }

        if (!CourseAccess.CanView(course, currentUser))
        {
            return Result.Failure(ContentErrors.CourseNotAccessible);
        }

        var alreadyBookmarked = await dbContext.CourseBookmarks
            .AnyAsync(b => b.UserId == currentUser.UserId && b.CourseId == command.CourseId, cancellationToken);
        if (alreadyBookmarked)
        {
            return Result.Success();
        }

        dbContext.CourseBookmarks.Add(CourseBookmark.Create(currentUser.UserId, command.CourseId));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
