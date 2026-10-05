using GymManagementDAL.Common;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GymManagementDAL.Repositories.Classes
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly GymDbContext DbContext;
        protected readonly DbSet<TEntity> Set;

        public GenericRepository(GymDbContext dbContext)
        {
            DbContext = dbContext;
            Set = dbContext.Set<TEntity>();
        }

        public void Add(TEntity entity) => Set.Add(entity);
        public void Update(TEntity entity) => Set.Update(entity);
        public void Delete(TEntity entity) => Set.Remove(entity);

        public async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
            => await Set.FindAsync([id], ct);

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, bool tracking = false, CancellationToken ct = default)
        {
            IQueryable<TEntity> query = tracking ? Set : Set.AsNoTracking();
            if (predicate is not null) query = query.Where(predicate);
            return await query.ToListAsync(ct);
        }

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool tracking = false, CancellationToken ct = default)
        {
            IQueryable<TEntity> query = tracking ? Set : Set.AsNoTracking();
            return await query.FirstOrDefaultAsync(predicate, ct);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
            => Set.AsNoTracking().AnyAsync(predicate, ct);

        public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
            => predicate is null ? Set.AsNoTracking().CountAsync(ct) : Set.AsNoTracking().CountAsync(predicate, ct);

        public Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        {
            IQueryable<TEntity> query = Set.AsNoTracking();
            if (predicate is not null) query = query.Where(predicate);
            return query.SumAsync(selector, ct);
        }

        public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<TEntity, TResult>> selector, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        {
            IQueryable<TEntity> query = Set.AsNoTracking();
            if (predicate is not null) query = query.Where(predicate);
            return query.Select(selector).ToListAsync(ct);
        }

        public Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
            int page, int pageSize, CancellationToken ct = default)
        {
            IQueryable<TEntity> query = Set.AsNoTracking();
            if (predicate is not null) query = query.Where(predicate);
            return PageAsync(query, orderBy, page, pageSize, ct);
        }

        /// <summary>Shared paging routine (also used by the specialised repositories).</summary>
        internal static async Task<PagedResult<T>> PageAsync<T>(
            IQueryable<T> query,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
            int page, int pageSize, CancellationToken ct)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var total = await query.CountAsync(ct);

            var lastPage = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, lastPage);

            var items = await orderBy(query)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<T>(items, total, page, pageSize);
        }
    }
}
