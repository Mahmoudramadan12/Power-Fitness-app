using GymManagementBLL.Common;
using GymManagementBLL.ViewModels.AnalyticsViewModels;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.PlanViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementBLL.ViewModels.TrainerViewModels;

namespace GymManagementBLL.Services.Interfaces
{
    public interface IMemberService
    {
        Task<PagedList<MemberViewModel>> GetMembersAsync(ListQuery query, CancellationToken ct = default);
        Task<MemberDetailsViewModel?> GetMemberDetailsAsync(int memberId, CancellationToken ct = default);
        Task<MemberToUpdateViewModel?> GetMemberToUpdateAsync(int memberId, CancellationToken ct = default);
        Task<HealthRecordViewModel?> GetMemberHealthRecordAsync(int memberId, CancellationToken ct = default);
        Task<string?> GetPhotoFileNameAsync(int memberId, CancellationToken ct = default);

        Task<Result> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct = default);
        Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default);
        Task<Result> UpdateHealthRecordAsync(int memberId, HealthRecordViewModel model, CancellationToken ct = default);
        Task<Result> RemoveMemberAsync(int memberId, CancellationToken ct = default);
    }

    public interface ITrainerService
    {
        Task<PagedList<TrainerViewModel>> GetTrainersAsync(TrainerListQuery query, CancellationToken ct = default);
        Task<TrainerDetailsViewModel?> GetTrainerDetailsAsync(int trainerId, CancellationToken ct = default);
        Task<TrainerToUpdateViewModel?> GetTrainerToUpdateAsync(int trainerId, CancellationToken ct = default);
        Task<Result> CreateTrainerAsync(CreateTrainerViewModel model, CancellationToken ct = default);
        Task<Result> UpdateTrainerDetailsAsync(int trainerId, TrainerToUpdateViewModel model, CancellationToken ct = default);
        Task<Result> RemoveTrainerAsync(int trainerId, CancellationToken ct = default);
    }

    public interface IPlanService
    {
        Task<IReadOnlyList<PlanViewModel>> GetAllPlansAsync(CancellationToken ct = default);
        Task<PlanViewModel?> GetPlanByIdAsync(int planId, CancellationToken ct = default);
        Task<UpdatePlanViewModel?> GetPlanToUpdateAsync(int planId, CancellationToken ct = default);
        Task<Result> CreatePlanAsync(CreatePlanViewModel model, CancellationToken ct = default);
        Task<Result> UpdatePlanAsync(int id, UpdatePlanViewModel model, CancellationToken ct = default);
        Task<Result> ToggleActivationAsync(int planId, CancellationToken ct = default);
    }

    public interface ISessionService
    {
        Task<PagedList<SessionViewModel>> GetSessionsAsync(SessionListQuery query, CancellationToken ct = default);
        Task<SessionViewModel?> GetSessionByIdAsync(int sessionId, CancellationToken ct = default);
        Task<UpdateSessionViewModel?> GetSessionToUpdateAsync(int sessionId, CancellationToken ct = default);
        Task<Result> CreateSessionAsync(CreateSessionViewModel model, CancellationToken ct = default);
        Task<Result> UpdateSessionAsync(int id, UpdateSessionViewModel model, CancellationToken ct = default);
        Task<Result> RemoveSessionAsync(int sessionId, CancellationToken ct = default);
        Task<IReadOnlyList<TrainerSelectViewModel>> GetTrainersForDropDownAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CategorySelectViewModel>> GetCategoriesForDropDownAsync(CancellationToken ct = default);
    }

    public interface IMembershipService
    {
        Task<PagedList<MembershipViewModel>> GetMembershipsAsync(MembershipListQuery query, CancellationToken ct = default);
        Task<IReadOnlyList<PlanSelectListViewModel>> GetPlansForDropDownAsync(CancellationToken ct = default);
        Task<IReadOnlyList<MemberSelectListViewModel>> GetMembersForDropDownAsync(CancellationToken ct = default);
        Task<Result> CreateMembershipAsync(CreateMembershipViewModel model, CancellationToken ct = default);

        /// <summary>Ends an active membership today, or removes one that has not started yet.</summary>
        Task<Result> CancelMembershipAsync(int membershipId, CancellationToken ct = default);
    }

    public interface IBookingService
    {
        /// <summary>Sessions that have not finished yet (upcoming and ongoing), soonest first.</summary>
        Task<IReadOnlyList<SessionViewModel>> GetScheduleAsync(CancellationToken ct = default);
        Task<SessionRosterViewModel?> GetRosterAsync(int sessionId, CancellationToken ct = default);
        Task<Result> CreateNewBookingAsync(CreateBookingViewModel model, CancellationToken ct = default);
        Task<Result> CancelBookingAsync(int memberId, int sessionId, CancellationToken ct = default);
        Task<Result> MarkAttendedAsync(int memberId, int sessionId, CancellationToken ct = default);
    }

    public interface IAnalyticsService
    {
        Task<DashboardViewModel> GetDashboardAsync(CancellationToken ct = default);
    }
}
