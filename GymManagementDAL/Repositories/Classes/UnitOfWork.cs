using GymManagementDAL.Common;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GymManagementDAL.Repositories.Classes
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = [];
        private readonly GymDbContext _dbContext;

        public IMembershipRepository MembershipRepository { get; }
        public ISessionRepository SessionRepository { get; }
        public IBookingRepository BookingRepository { get; }

        public UnitOfWork(GymDbContext dbContext,
            IMembershipRepository membershipRepository,
            ISessionRepository sessionRepository,
            IBookingRepository bookingRepository)
        {
            _dbContext = dbContext;
            MembershipRepository = membershipRepository;
            SessionRepository = sessionRepository;
            BookingRepository = bookingRepository;
        }

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity
        {
            if (_repositories.TryGetValue(typeof(TEntity), out var existing))
                return (IGenericRepository<TEntity>)existing;

            var repository = new GenericRepository<TEntity>(_dbContext);
            _repositories[typeof(TEntity)] = repository;
            return repository;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sql)
            {
                throw new DuplicateKeyException(sql.Message, ex);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 } sql)
            {
                throw new ReferenceConstraintException(sql.Message, ex);
            }
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.Serializable,
            CancellationToken ct = default)
        {
            // The execution strategy (see AddDbContext in the web project) retries the whole delegate on
            // transient failures such as deadlocks, which is exactly what a serializable read-then-write needs.
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return strategy.ExecuteAsync(async () =>
            {
                // A retry must not see entities tracked by the failed attempt.
                _dbContext.ChangeTracker.Clear();

                await using var transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel, ct);
                var result = await operation();
                await transaction.CommitAsync(ct);
                return result;
            });
        }
    }
}
