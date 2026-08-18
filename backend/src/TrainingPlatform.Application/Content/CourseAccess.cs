using Microsoft.EntityFrameworkCore;
using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Application.Abstractions.Data;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Content;

/// <summary>
/// Open-catalog model (confirmed 2026-08-17): any authenticated user can browse/see a
/// published course (list, detail, search) whether or not they're enrolled — enrollment gates
/// only the ability to actually download a document (REQ-RBAC-02: "retrieve materials"), not
/// whether the course is discoverable.
/// </summary>
internal static class CourseAccess
{
    public static bool CanManage(Course course, IUserContext user) =>
        user.Roles.Contains(Roles.Administrator) || course.TrainerId == user.UserId;

    /// <summary>Browsing rule for course list/detail/search: owned/managed, or published.</summary>
    public static bool CanView(Course course, IUserContext user) =>
        CanManage(course, user) || course.IsPublished;

    /// <summary>Download rule: owned/managed, or an active enrollment — being able to *see* a
    /// published course does not by itself grant the ability to download its documents.</summary>
    public static async Task<bool> CanDownloadAsync(
        Course course, IUserContext user, IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        if (CanManage(course, user))
        {
            return true;
        }

        return await dbContext.Enrollments
            .AnyAsync(e => e.CourseId == course.Id && e.UserId == user.UserId, cancellationToken);
    }
}
