using GymManagementDAL.Entities;
using System.Data;

namespace GymManagementDAL.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        IMembershipRepository MembershipRepository { get; }
        ISessionRepository SessionRepository { get; }
        IBookingRepository BookingRepository { get; }
        IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity;

        /// <exception cref="Common.DuplicateKeyException">A unique constraint was violated.</exception>
        /// <exception cref="Common.ReferenceConstraintException">A foreign key blocked the change.</exception>
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Runs <paramref name="operation"/> inside a database transaction. The whole block is retried
        /// if SQL Server picks it as a deadlock victim, so it must be safe to run more than once
        /// (i.e. read, decide, write - and nothing else).
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.Serializable,
            CancellationToken ct = default);
    }
}
