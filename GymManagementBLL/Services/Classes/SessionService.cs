using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System.Linq.Expressions;

namespace GymManagementBLL.Services.Classes
{
    public class SessionService(IUnitOfWork unitOfWork, TimeProvider time) : ISessionService
    {
        public async Task<PagedList<SessionViewModel>> GetSessionsAsync(SessionListQuery query, CancellationToken ct = default)
        {
            var now = time.Now();
            var term = query.SearchTerm;
            var categoryId = query.CategoryId;

            Expression<Func<SessionEntity, bool>> predicate = query.Status switch
            {
                SessionStatus.Upcoming => s => s.StartDate > now,
                SessionStatus.Ongoing => s => s.StartDate <= now && s.EndDate >= now,
                SessionStatus.Completed => s => s.EndDate < now,
                _ => s => true
            };

            var page = await unitOfWork.SessionRepository.GetPagedWithTrainerAndCategoryAsync(
                Combine(predicate, s =>
                    (categoryId == null || s.CategoryId == categoryId)
                    && (term == null || s.Description.Contains(term) || s.Trainer.Name.Contains(term))),
                newestFirst: query.Status is SessionStatus.Completed or null,
                query.Page, query.PageSize, ct);

            var counts = await unitOfWork.SessionRepository.GetBookedCountsAsync(page.Items.Select(s => s.Id).ToList(), ct);
            var items = page.Items.Select(s => s.ToViewModel(counts.GetValueOrDefault(s.Id), now)).ToList();

            return new PagedList<SessionViewModel>(items, page.TotalCount, page.Page, page.PageSize);
        }

        public async Task<SessionViewModel?> GetSessionByIdAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetSessionWithTrainerAndCategoryAsync(sessionId, ct);
            if (session is null) return null;

            var booked = await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct);
            return session.ToViewModel(booked, time.Now());
        }

        public async Task<UpdateSessionViewModel?> GetSessionToUpdateAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetSessionWithTrainerAndCategoryAsync(sessionId, ct);
            if (session is null) return null;
            if (session.StartDate <= time.Now()) return null;
            if (await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct) > 0) return null;

            return new UpdateSessionViewModel
            {
                Id = session.Id,
                CategoryName = session.Category.CategoryName,
                Description = session.Description,
                Capacity = session.Capacity,
                StartDate = session.StartDate,
                EndDate = session.EndDate,
                TrainerId = session.TrainerId
            };
        }

        public async Task<Result> CreateSessionAsync(CreateSessionViewModel model, CancellationToken ct = default)
        {
            var start = model.StartDate!.Value;
            var end = model.EndDate!.Value;

            if (end <= start) return Result.Validation("End must be after start.");
            if (start <= time.Now()) return Result.Validation("A new session must start in the future.");

            var category = await unitOfWork.GetRepository<CategoryEntity>().GetByIdAsync(model.CategoryId!.Value, ct);
            if (category is null) return Result.NotFound("Category not found.");

            var check = await ValidateTrainerAsync(model.TrainerId!.Value, category, start, end, excludeSessionId: null, ct);
            if (!check.Success) return check;

            unitOfWork.SessionRepository.Add(new SessionEntity
            {
                Description = model.Description.Trim(),
                Capacity = model.Capacity!.Value,
                StartDate = start,
                EndDate = end,
                TrainerId = model.TrainerId.Value,
                CategoryId = category.Id
            });

            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> UpdateSessionAsync(int id, UpdateSessionViewModel model, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(id, ct);
            if (session is null) return Result.NotFound("Session not found.");

            var now = time.Now();
            if (session.StartDate <= now)
                return Result.Fail("A session that has already started can't be edited.");
            if (await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(id, ct) > 0)
                return Result.Fail("A session that already has bookings can't be edited. Cancel the bookings first.");

            var start = model.StartDate!.Value;
            var end = model.EndDate!.Value;
            if (end <= start) return Result.Validation("End must be after start.");
            if (start <= now) return Result.Validation("The session must start in the future.");

            var category = await unitOfWork.GetRepository<CategoryEntity>().GetByIdAsync(session.CategoryId, ct);
            if (category is null) return Result.NotFound("Category not found.");

            var check = await ValidateTrainerAsync(model.TrainerId!.Value, category, start, end, excludeSessionId: id, ct);
            if (!check.Success) return check;

            session.Description = model.Description.Trim();
            session.Capacity = model.Capacity!.Value;
            session.StartDate = start;
            session.EndDate = end;
            session.TrainerId = model.TrainerId.Value;

            unitOfWork.SessionRepository.Update(session);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> RemoveSessionAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null) return Result.NotFound("Session not found.");

            var now = time.Now();
            if (session.StartDate <= now && session.EndDate >= now)
                return Result.Fail("A session that is in progress can't be deleted.");

            if (await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(sessionId, ct) > 0)
                return Result.Fail("This session has bookings, so it can't be deleted.");

            try
            {
                unitOfWork.SessionRepository.Delete(session);
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (ReferenceConstraintException)
            {
                return Result.Fail("This session is still referenced by other records and can't be deleted.");
            }
        }

        public async Task<IReadOnlyList<TrainerSelectViewModel>> GetTrainersForDropDownAsync(CancellationToken ct = default)
        {
            var trainers = (await unitOfWork.GetRepository<TrainerEntity>().GetAllAsync(ct: ct)).OrderBy(t => t.Name).ToList();
            var categories = (await unitOfWork.GetRepository<CategoryEntity>().GetAllAsync(ct: ct)).ToList();

            return trainers.Select(t => new TrainerSelectViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Specialty = EnumDisplay.Name(t.Specialties),
                CategoryIds = categories.Where(c => TrainerCategoryRules.CanLead(t.Specialties, c.CategoryName))
                                        .Select(c => c.Id).ToList()
            }).ToList();
        }

        public async Task<IReadOnlyList<CategorySelectViewModel>> GetCategoriesForDropDownAsync(CancellationToken ct = default)
        {
            var categories = await unitOfWork.GetRepository<CategoryEntity>().GetAllAsync(ct: ct);
            return categories.OrderBy(c => c.CategoryName)
                .Select(c => new CategorySelectViewModel { Id = c.Id, CategoryName = c.CategoryName }).ToList();
        }

        private async Task<Result> ValidateTrainerAsync(int trainerId, CategoryEntity category,
            DateTime start, DateTime end, int? excludeSessionId, CancellationToken ct)
        {
            var trainer = await unitOfWork.GetRepository<TrainerEntity>().GetByIdAsync(trainerId, ct);
            if (trainer is null) return Result.NotFound("Trainer not found.");

            if (!TrainerCategoryRules.CanLead(trainer.Specialties, category.CategoryName))
                return Result.Validation($"{trainer.Name} ({EnumDisplay.Name(trainer.Specialties)}) can't lead {category.CategoryName} sessions.");

            if (await unitOfWork.SessionRepository.HasTrainerConflictAsync(trainerId, start, end, excludeSessionId, ct))
                return Result.Fail($"{trainer.Name} already has a session that overlaps this time.");

            return Result.Ok();
        }

        /// <summary>AND-combines two predicates over the same parameter so EF can still translate the result.</summary>
        private static Expression<Func<SessionEntity, bool>> Combine(
            Expression<Func<SessionEntity, bool>> left, Expression<Func<SessionEntity, bool>> right)
        {
            var parameter = left.Parameters[0];
            var rightBody = new ReplaceParameter(right.Parameters[0], parameter).Visit(right.Body)!;
            return Expression.Lambda<Func<SessionEntity, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
        }

        private sealed class ReplaceParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
        }
    }
}
