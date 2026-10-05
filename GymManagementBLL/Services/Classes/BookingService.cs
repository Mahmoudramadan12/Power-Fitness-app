using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;

namespace GymManagementBLL.Services.Classes
{
    public class BookingService(IUnitOfWork unitOfWork, TimeProvider time) : IBookingService
    {
        public async Task<IReadOnlyList<SessionViewModel>> GetScheduleAsync(CancellationToken ct = default)
        {
            var now = time.Now();
            var sessions = await unitOfWork.SessionRepository.GetWithTrainerAndCategoryAsync(
                s => s.EndDate >= now, newestFirst: false, take: null, ct);

            var counts = await unitOfWork.SessionRepository.GetBookedCountsAsync(sessions.Select(s => s.Id).ToList(), ct);
            return sessions.Select(s => s.ToViewModel(counts.GetValueOrDefault(s.Id), now)).ToList();
        }

        public async Task<SessionRosterViewModel?> GetRosterAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetSessionWithTrainerAndCategoryAsync(sessionId, ct);
            if (session is null) return null;

            var now = time.Now();
            var bookings = await unitOfWork.BookingRepository.GetBySessionIdAsync(sessionId, ct);

            var roster = new SessionRosterViewModel
            {
                Session = session.ToViewModel(bookings.Count, now),
                Members = bookings.Select(b => new MemberForSessionViewModel
                {
                    MemberId = b.MemberId,
                    SessionId = sessionId,
                    MemberName = b.Member.Name,
                    BookedAt = b.CreatedAt,
                    IsAttended = b.IsAttended
                }).ToList()
            };

            if (roster.CanBook)
                roster.EligibleMembers = await GetEligibleMembersAsync(session, bookings.Select(b => b.MemberId).ToList(), ct);

            return roster;
        }

        public async Task<Result> CreateNewBookingAsync(CreateBookingViewModel model, CancellationToken ct = default)
        {
            try
            {
                // Serializable + retry: two admins booking the last seat at the same moment can't both succeed.
                return await unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var session = await unitOfWork.SessionRepository.GetByIdAsync(model.SessionId, ct);
                    if (session is null) return Result.NotFound("Session not found.");

                    if (session.StartDate <= time.Now())
                        return Result.Fail("This session has already started, so it can't be booked.");

                    if (!await unitOfWork.GetRepository<MemberEntity>().AnyAsync(m => m.Id == model.MemberId, ct))
                        return Result.NotFound("Member not found.");

                    var start = session.StartDate;
                    var covered = await unitOfWork.MembershipRepository.AnyAsync(
                        m => m.MemberId == model.MemberId && m.CreatedAt <= start && m.EndDate > start, ct);
                    if (!covered)
                        return Result.Fail("The member has no membership that covers the session date.");

                    if (await unitOfWork.BookingRepository.AnyAsync(
                            b => b.SessionId == model.SessionId && b.MemberId == model.MemberId, ct))
                        return Result.Fail("This member is already booked into this session.");

                    var booked = await unitOfWork.SessionRepository.GetCountOfBookedSlotsAsync(model.SessionId, ct);
                    if (booked >= session.Capacity)
                        return Result.Fail("This session is full.");

                    unitOfWork.BookingRepository.Add(new BookingEntity
                    {
                        MemberId = model.MemberId,
                        SessionId = model.SessionId
                    });
                    await unitOfWork.SaveChangesAsync(ct);
                    return Result.Ok();
                }, ct: ct);
            }
            catch (DuplicateKeyException)
            {
                return Result.Fail("This member is already booked into this session.");
            }
        }

        public async Task<Result> CancelBookingAsync(int memberId, int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null) return Result.NotFound("Session not found.");

            if (session.StartDate <= time.Now())
                return Result.Fail("Bookings can't be cancelled once the session has started.");

            var booking = await unitOfWork.BookingRepository.FirstOrDefaultAsync(
                b => b.SessionId == sessionId && b.MemberId == memberId, tracking: true, ct: ct);
            if (booking is null) return Result.NotFound("Booking not found.");

            unitOfWork.BookingRepository.Delete(booking);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> MarkAttendedAsync(int memberId, int sessionId, CancellationToken ct = default)
        {
            var session = await unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null) return Result.NotFound("Session not found.");

            var now = time.Now();
            if (now < session.StartDate || now > session.EndDate)
                return Result.Fail("Attendance can only be recorded while the session is in progress.");

            var booking = await unitOfWork.BookingRepository.FirstOrDefaultAsync(
                b => b.SessionId == sessionId && b.MemberId == memberId, tracking: true, ct: ct);
            if (booking is null) return Result.NotFound("Booking not found.");
            if (booking.IsAttended) return Result.Ok();

            booking.IsAttended = true;
            unitOfWork.BookingRepository.Update(booking);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        /// <summary>Members who are not yet booked and whose membership covers the session's start.</summary>
        private async Task<IReadOnlyList<MemberSelectListViewModel>> GetEligibleMembersAsync(
            SessionEntity session, IReadOnlyCollection<int> alreadyBooked, CancellationToken ct)
        {
            var start = session.StartDate;
            var coveredIds = (await unitOfWork.MembershipRepository.SelectAsync(
                m => m.MemberId, m => m.CreatedAt <= start && m.EndDate > start, ct)).Distinct().ToList();

            var booked = alreadyBooked.ToList();
            var members = await unitOfWork.GetRepository<MemberEntity>().GetAllAsync(
                m => coveredIds.Contains(m.Id) && !booked.Contains(m.Id), ct: ct);

            return members.OrderBy(m => m.Name)
                .Select(m => new MemberSelectListViewModel { Id = m.Id, Name = m.Name, Phone = m.Phone })
                .ToList();
        }
    }
}
