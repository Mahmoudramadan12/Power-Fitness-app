using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.AttachmentService;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;

namespace GymManagementBLL.Services.Classes
{
    public class MemberService(
        IUnitOfWork unitOfWork,
        IAttachmentService attachments,
        TimeProvider time) : IMemberService
    {
        public const string PhotoFolder = "MembersPictures";

        public async Task<PagedList<MemberViewModel>> GetMembersAsync(ListQuery query, CancellationToken ct = default)
        {
            var now = time.Now();
            var term = query.SearchTerm;

            var page = await unitOfWork.GetRepository<MemberEntity>().GetPagedAsync(
                term is null ? null : m => m.Name.Contains(term) || m.Email.Contains(term) || m.Phone.Contains(term),
                q => q.OrderBy(m => m.Name).ThenBy(m => m.Id),
                query.Page, query.PageSize, ct);

            // One extra query for the whole page instead of one per row.
            var active = await unitOfWork.MembershipRepository.GetActiveForMembersAsync(
                page.Items.Select(m => m.Id).ToList(), now, ct);
            var activeByMember = active
                .GroupBy(m => m.MemberId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.EndDate).First());

            var items = page.Items
                .Select(m => m.ToViewModel(activeByMember.GetValueOrDefault(m.Id)))
                .ToList();

            return new PagedList<MemberViewModel>(items, page.TotalCount, page.Page, page.PageSize);
        }

        public async Task<MemberDetailsViewModel?> GetMemberDetailsAsync(int memberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<MemberEntity>().FirstOrDefaultAsync(m => m.Id == memberId, ct: ct);
            if (member is null) return null;

            var now = time.Now();
            var history = (await unitOfWork.MembershipRepository.GetHistoryForMemberAsync(memberId, ct))
                .Select(m => m.ToSummary(now)).ToList();
            var health = await unitOfWork.GetRepository<HealthRecordEntity>().FirstOrDefaultAsync(h => h.MemberId == memberId, ct: ct);
            var bookings = await unitOfWork.BookingRepository.GetRecentByMemberIdAsync(memberId, 10, ct);

            return new MemberDetailsViewModel
            {
                Id = member.Id,
                Photo = member.Photo,
                Name = member.Name,
                Email = member.Email,
                Phone = member.Phone,
                Gender = member.Gender.ToString(),
                DateOfBirth = member.DateOfBirth,
                Age = member.DateOfBirth.AgeOn(now),
                Address = Mappers.FormatAddress(member.Address),
                JoinDate = member.CreatedAt,
                MembershipHistory = history,
                CurrentMembership = history.FirstOrDefault(h => h.Status == MembershipStatus.Active),
                HealthRecord = health?.ToViewModel(member.Name),
                RecentBookings = bookings.Select(b => new MemberBookingViewModel
                {
                    SessionId = b.SessionId,
                    CategoryName = b.Session.Category.CategoryName,
                    TrainerName = b.Session.Trainer.Name,
                    StartDate = b.Session.StartDate,
                    EndDate = b.Session.EndDate,
                    IsAttended = b.IsAttended,
                    Status = Mappers.StatusOf(b.Session, now)
                }).ToList()
            };
        }

        public async Task<MemberToUpdateViewModel?> GetMemberToUpdateAsync(int memberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<MemberEntity>().FirstOrDefaultAsync(m => m.Id == memberId, ct: ct);
            return member?.ToUpdateViewModel();
        }

        public async Task<HealthRecordViewModel?> GetMemberHealthRecordAsync(int memberId, CancellationToken ct = default)
        {
            var member = await unitOfWork.GetRepository<MemberEntity>().FirstOrDefaultAsync(m => m.Id == memberId, ct: ct);
            if (member is null) return null;

            var record = await unitOfWork.GetRepository<HealthRecordEntity>().FirstOrDefaultAsync(h => h.MemberId == memberId, ct: ct);
            return record?.ToViewModel(member.Name);
        }

        public async Task<string?> GetPhotoFileNameAsync(int memberId, CancellationToken ct = default)
        {
            var photos = await unitOfWork.GetRepository<MemberEntity>().SelectAsync(m => m.Photo, m => m.Id == memberId, ct);
            return photos.FirstOrDefault();
        }

        public async Task<Result> CreateMemberAsync(CreateMemberViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<MemberEntity>();
            var email = model.Email.Trim();
            var phone = model.Phone.Trim();

            if (await repo.AnyAsync(m => m.Email == email, ct))
                return Result.Fail("A member with this email already exists.");
            if (await repo.AnyAsync(m => m.Phone == phone, ct))
                return Result.Fail("A member with this phone number already exists.");

            string? photo = null;
            if (model.PhotoFile is { Length: > 0 } file)
            {
                await using var stream = file.OpenReadStream();
                photo = await attachments.UploadAsync(stream, file.FileName, PhotoFolder, ct);
                if (photo is null)
                    return Result.Validation("The photo could not be saved. Use a real JPG or PNG up to 5 MB.");
            }

            try
            {
                repo.Add(model.ToEntity(photo));
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (DuplicateKeyException ex)
            {
                DeletePhoto(photo);
                return Result.Fail(DuplicateMessage(ex, "member"));
            }
            catch
            {
                DeletePhoto(photo);
                throw;
            }
        }

        public async Task<Result> UpdateMemberDetailsAsync(int id, MemberToUpdateViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<MemberEntity>();
            var member = await repo.GetByIdAsync(id, ct);
            if (member is null) return Result.NotFound("Member not found.");

            var email = model.Email.Trim();
            var phone = model.Phone.Trim();
            if (await repo.AnyAsync(m => m.Email == email && m.Id != id, ct))
                return Result.Fail("Another member is already using this email.");
            if (await repo.AnyAsync(m => m.Phone == phone && m.Id != id, ct))
                return Result.Fail("Another member is already using this phone number.");

            string? newPhoto = null;
            if (model.PhotoFile is { Length: > 0 } file)
            {
                await using var stream = file.OpenReadStream();
                newPhoto = await attachments.UploadAsync(stream, file.FileName, PhotoFolder, ct);
                if (newPhoto is null)
                    return Result.Validation("The photo could not be saved. Use a real JPG or PNG up to 5 MB.");
            }

            var oldPhoto = member.Photo;
            model.Apply(member);
            if (newPhoto is not null) member.Photo = newPhoto;

            try
            {
                repo.Update(member);
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DuplicateKeyException ex)
            {
                DeletePhoto(newPhoto);
                return Result.Fail(DuplicateMessage(ex, "member"));
            }
            catch
            {
                DeletePhoto(newPhoto);
                throw;
            }

            if (newPhoto is not null) DeletePhoto(oldPhoto);   // only after the new one is safely referenced
            return Result.Ok();
        }

        public async Task<Result> UpdateHealthRecordAsync(int memberId, HealthRecordViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<HealthRecordEntity>();
            var record = await repo.FirstOrDefaultAsync(h => h.MemberId == memberId, tracking: true, ct: ct);

            if (record is null)
            {
                if (!await unitOfWork.GetRepository<MemberEntity>().AnyAsync(m => m.Id == memberId, ct))
                    return Result.NotFound("Member not found.");

                var created = model.ToEntity();
                created.MemberId = memberId;
                repo.Add(created);
            }
            else
            {
                model.Apply(record);
                repo.Update(record);
            }

            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> RemoveMemberAsync(int memberId, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<MemberEntity>();
            var member = await repo.GetByIdAsync(memberId, ct);
            if (member is null) return Result.NotFound("Member not found.");

            var now = time.Now();
            if (await unitOfWork.BookingRepository.AnyAsync(b => b.MemberId == memberId && b.Session.EndDate >= now, ct))
                return Result.Fail("This member has upcoming or ongoing bookings. Cancel them first.");

            repo.Delete(member);
            await unitOfWork.SaveChangesAsync(ct);

            DeletePhoto(member.Photo);
            return Result.Ok();
        }

        private void DeletePhoto(string? photo)
        {
            if (!string.IsNullOrEmpty(photo)) attachments.Delete(photo, PhotoFolder);
        }

        private static string DuplicateMessage(DuplicateKeyException ex, string who) =>
            ex.Involves("Email") ? $"A {who} with this email already exists."
            : ex.Involves("Phone") ? $"A {who} with this phone number already exists."
            : $"A {who} with the same details already exists.";
    }
}
