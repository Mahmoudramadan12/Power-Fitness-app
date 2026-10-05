using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.TrainerViewModels;
using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;

namespace GymManagementBLL.Services.Classes
{
    public class TrainerService(IUnitOfWork unitOfWork, TimeProvider time) : ITrainerService
    {
        public async Task<PagedList<TrainerViewModel>> GetTrainersAsync(TrainerListQuery query, CancellationToken ct = default)
        {
            var now = time.Now();
            var term = query.SearchTerm;
            var specialty = query.Specialty;

            var page = await unitOfWork.GetRepository<TrainerEntity>().GetPagedAsync(
                t => (term == null || t.Name.Contains(term) || t.Email.Contains(term) || t.Phone.Contains(term))
                     && (specialty == null || t.Specialties == specialty),
                q => q.OrderBy(t => t.Name).ThenBy(t => t.Id),
                query.Page, query.PageSize, ct);

            return new PagedList<TrainerViewModel>(
                page.Items.Select(t => t.ToViewModel(now)).ToList(), page.TotalCount, page.Page, page.PageSize);
        }

        public async Task<TrainerDetailsViewModel?> GetTrainerDetailsAsync(int trainerId, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<TrainerEntity>().FirstOrDefaultAsync(t => t.Id == trainerId, ct: ct);
            if (trainer is null) return null;

            var now = time.Now();
            var upcoming = await unitOfWork.SessionRepository.GetWithTrainerAndCategoryAsync(
                s => s.TrainerId == trainerId && s.EndDate >= now, newestFirst: false, take: 8, ct);
            var counts = await unitOfWork.SessionRepository.GetBookedCountsAsync(upcoming.Select(s => s.Id).ToList(), ct);

            var vm = trainer.ToDetailsViewModel(now);
            vm.UpcomingSessions = upcoming.Select(s => s.ToViewModel(counts.GetValueOrDefault(s.Id), now)).ToList();
            vm.TotalSessions = await unitOfWork.SessionRepository.CountAsync(s => s.TrainerId == trainerId, ct);
            return vm;
        }

        public async Task<TrainerToUpdateViewModel?> GetTrainerToUpdateAsync(int trainerId, CancellationToken ct = default)
        {
            var trainer = await unitOfWork.GetRepository<TrainerEntity>().FirstOrDefaultAsync(t => t.Id == trainerId, ct: ct);
            return trainer?.ToUpdateViewModel();
        }

        public async Task<Result> CreateTrainerAsync(CreateTrainerViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<TrainerEntity>();
            var email = model.Email.Trim();
            var phone = model.Phone.Trim();

            if (await repo.AnyAsync(t => t.Email == email, ct))
                return Result.Fail("A trainer with this email already exists.");
            if (await repo.AnyAsync(t => t.Phone == phone, ct))
                return Result.Fail("A trainer with this phone number already exists.");

            try
            {
                repo.Add(model.ToEntity());
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (DuplicateKeyException ex)
            {
                return Result.Fail(DuplicateMessage(ex));
            }
        }

        public async Task<Result> UpdateTrainerDetailsAsync(int trainerId, TrainerToUpdateViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<TrainerEntity>();
            var trainer = await repo.GetByIdAsync(trainerId, ct);
            if (trainer is null) return Result.NotFound("Trainer not found.");

            var email = model.Email.Trim();
            var phone = model.Phone.Trim();
            if (await repo.AnyAsync(t => t.Email == email && t.Id != trainerId, ct))
                return Result.Fail("Another trainer is already using this email.");
            if (await repo.AnyAsync(t => t.Phone == phone && t.Id != trainerId, ct))
                return Result.Fail("Another trainer is already using this phone number.");

            // A specialty change must not strand sessions this trainer is already scheduled to lead.
            if (model.Specialty != trainer.Specialties)
            {
                var now = time.Now();
                var future = await unitOfWork.SessionRepository.GetWithTrainerAndCategoryAsync(
                    s => s.TrainerId == trainerId && s.EndDate >= now, ct: ct);
                if (future.Any(s => !TrainerCategoryRules.CanLead(model.Specialty!.Value, s.Category.CategoryName)))
                    return Result.Fail("This trainer has upcoming sessions that don't match the new specialty. Reassign or remove them first.");
            }

            try
            {
                model.Apply(trainer);
                repo.Update(trainer);
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (DuplicateKeyException ex)
            {
                return Result.Fail(DuplicateMessage(ex));
            }
        }

        public async Task<Result> RemoveTrainerAsync(int trainerId, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<TrainerEntity>();
            var trainer = await repo.GetByIdAsync(trainerId, ct);
            if (trainer is null) return Result.NotFound("Trainer not found.");

            // Past sessions are history (bookings, attendance); they must never be deleted as a side effect.
            if (await unitOfWork.SessionRepository.AnyAsync(s => s.TrainerId == trainerId, ct))
                return Result.Fail("This trainer has sessions on record (past or upcoming), so they can't be deleted without losing that history.");

            try
            {
                repo.Delete(trainer);
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (ReferenceConstraintException)
            {
                return Result.Fail("This trainer is still referenced by other records and can't be deleted.");
            }
        }

        private static string DuplicateMessage(DuplicateKeyException ex) =>
            ex.Involves("Email") ? "A trainer with this email already exists."
            : ex.Involves("Phone") ? "A trainer with this phone number already exists."
            : "A trainer with the same details already exists.";
    }
}
