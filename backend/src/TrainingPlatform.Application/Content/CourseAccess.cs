using TrainingPlatform.Application.Abstractions.Authentication;
using TrainingPlatform.Domain.Content;
using TrainingPlatform.Domain.Users;

namespace TrainingPlatform.Application.Content;

/// <summary>
/// Interim visibility rule ahead of the Enrollment model (Phase 3, REQ-RBAC-02/03): Trainees
/// see published courses; Trainer/Admin manage-access rules stay unaffected.
/// </summary>
internal static class CourseAccess
{
    public static bool CanManage(Course course, IUserContext user) =>
        user.Roles.Contains(Roles.Administrator) || course.TrainerId == user.UserId;

    public static bool CanView(Course course, IUserContext user) =>
        CanManage(course, user) || course.IsPublished;
}
