using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System.Data;
using System.Linq.Expressions;

namespace GymManagement.Tests.Fakes
{
    /// <summary>All "tables" of the fake database, shared by every fake repository.</summary>
    public sealed class InMemoryDb(TimeProvider clock)
    {
        public readonly Dictionary<Type, object> Tables = [];
        public int SaveCount;
        public TimeProvider Clock { get; } = clock;

        public List<T> Table<T>() where T : BaseEntity
        {
            if (!Tables.TryGetValue(typeof(T), out var list)) Tables[typeof(T)] = list = new List<T>();
            return (List<T>)list;
        }

        public List<MemberEntity> Members => Table<MemberEntity>();
        public List<TrainerEntity> Trainers => Table<TrainerEntity>();
        public List<PlanEntity> Plans => Table<PlanEntity>();
        public List<CategoryEntity> Categories => Table<CategoryEntity>();
        public List<SessionEntity> Sessions => Table<SessionEntity>();
        public List<MembershipEntity> Memberships => Table<MembershipEntity>();
        public List<BookingEntity> Bookings => Table<BookingEntity>();
        public List<HealthRecordEntity> HealthRecords => Table<HealthRecordEntity>();

        private int _nextId = 1000;

        /// <summary>Mimics what EF does on insert: key generation and navigation fix-up.</summary>
        public void OnAdd(BaseEntity entity)
        {
            if (entity is not BookingEntity && entity.Id == 0) entity.Id = ++_nextId;

            switch (entity)
            {
                case SessionEntity s:
                    s.Trainer ??= Trainers.First(t => t.Id == s.TrainerId);
                    s.Category ??= Categories.First(c => c.Id == s.CategoryId);
                    break;
                case MembershipEntity m:
                    m.Member ??= Members.First(x => x.Id == m.MemberId);
                    m.Plan ??= Plans.First(p => p.Id == m.PlanId);
                    break;
                case BookingEntity b:
                    b.Session ??= Sessions.First(x => x.Id == b.SessionId);
                    b.Member ??= Members.First(x => x.Id == b.MemberId);
                    break;
            }
        }

        public void StampOnSave()
        {
            var now = Clock.GetLocalNow().DateTime;
            foreach (var list in Tables.Values)
                foreach (var e in ((System.Collections.IEnumerable)list).Cast<BaseEntity>())
                    if (e.CreatedAt == default) e.CreatedAt = now;
        }
    }

    public class InMemoryRepository<T>(InMemoryDb db) : IGenericRepository<T> where T : BaseEntity
    {
        protected List<T> Items => db.Table<T>();

        public virtual void Add(T entity) { db.OnAdd(entity); Items.Add(entity); }
        public void Update(T entity) { }
        public void Delete(T entity) => Items.Remove(entity);

        public Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(e => e.Id == id));

        public Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, bool tracking = false, CancellationToken ct = default)
            => Task.FromResult<IEnumerable<T>>(Filter(predicate).ToList());

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool tracking = false, CancellationToken ct = default)
            => Task.FromResult(Filter(predicate).FirstOrDefault());

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(Filter(predicate).Any());

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(Filter(predicate).Count());

        public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(Filter(predicate).Sum(selector.Compile()));

        public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(Filter(predicate).Select(selector.Compile()).ToList());

        public Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>>? predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(Page(Filter(predicate).AsQueryable(), orderBy, page, pageSize));

        protected IEnumerable<T> Filter(Expression<Func<T, bool>>? predicate)
            => predicate is null ? Items.ToList() : Items.Where(CaseInsensitive(predicate).Compile()).ToList();

        /// <summary>
        /// SQL Server's default collation compares strings case-insensitively; LINQ-to-objects does not.
        /// Rewrite string ==, Contains and StartsWith so the fake behaves like the real database.
        /// </summary>
        private static Expression<Func<T, bool>> CaseInsensitive(Expression<Func<T, bool>> expression)
            => (Expression<Func<T, bool>>)new CollationVisitor().Visit(expression);

        private sealed class CollationVisitor : ExpressionVisitor
        {
            private static readonly System.Reflection.MethodInfo Equals3 =
                typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string), typeof(StringComparison)])!;
            private static readonly System.Reflection.MethodInfo Contains2 =
                typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if ((node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
                    && node.Left.Type == typeof(string) && node.Right.Type == typeof(string))
                {
                    var eq = Expression.Call(Equals3, Visit(node.Left), Visit(node.Right), Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    return node.NodeType == ExpressionType.Equal ? eq : Expression.Not(eq);
                }
                return base.VisitBinary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(string) && node.Method.Name == nameof(string.Contains)
                    && node.Arguments.Count == 1 && node.Arguments[0].Type == typeof(string))
                    return Expression.Call(Visit(node.Object)!, Contains2, Visit(node.Arguments[0]), Expression.Constant(StringComparison.OrdinalIgnoreCase));
                return base.VisitMethodCall(node);
            }
        }

        protected static PagedResult<TItem> Page<TItem>(IQueryable<TItem> query,
            Func<IQueryable<TItem>, IOrderedQueryable<TItem>> orderBy, int page, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var total = query.Count();
            var last = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, last);
            var items = orderBy(query).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<TItem>(items, total, page, pageSize);
        }
    }

    public sealed class InMemorySessionRepository(InMemoryDb db) : InMemoryRepository<SessionEntity>(db), ISessionRepository
    {
        private IEnumerable<SessionEntity> Query(Expression<Func<SessionEntity, bool>>? p, bool newestFirst)
        {
            var q = Filter(p);
            return newestFirst ? q.OrderByDescending(s => s.StartDate).ThenByDescending(s => s.Id)
                               : q.OrderBy(s => s.StartDate).ThenBy(s => s.Id);
        }

        public Task<List<SessionEntity>> GetWithTrainerAndCategoryAsync(Expression<Func<SessionEntity, bool>>? predicate = null, bool newestFirst = false, int? take = null, CancellationToken ct = default)
        {
            var q = Query(predicate, newestFirst);
            if (take is > 0) q = q.Take(take.Value);
            return Task.FromResult(q.ToList());
        }

        public Task<PagedResult<SessionEntity>> GetPagedWithTrainerAndCategoryAsync(Expression<Func<SessionEntity, bool>>? predicate, bool newestFirst, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(Page(Query(predicate, newestFirst).AsQueryable(),
                q => newestFirst ? q.OrderByDescending(s => s.StartDate) : q.OrderBy(s => s.StartDate), page, pageSize));

        public Task<SessionEntity?> GetSessionWithTrainerAndCategoryAsync(int sessionId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(s => s.Id == sessionId));

        public Task<int> GetCountOfBookedSlotsAsync(int sessionId, CancellationToken ct = default)
            => Task.FromResult(db.Bookings.Count(b => b.SessionId == sessionId));

        public Task<Dictionary<int, int>> GetBookedCountsAsync(IReadOnlyCollection<int> sessionIds, CancellationToken ct = default)
            => Task.FromResult(db.Bookings.Where(b => sessionIds.Contains(b.SessionId))
                .GroupBy(b => b.SessionId).ToDictionary(g => g.Key, g => g.Count()));

        public Task<bool> HasTrainerConflictAsync(int trainerId, DateTime start, DateTime end, int? excludeSessionId = null, CancellationToken ct = default)
            => Task.FromResult(Items.Any(s => s.TrainerId == trainerId && (excludeSessionId == null || s.Id != excludeSessionId)
                                              && s.StartDate < end && s.EndDate > start));
    }

    public sealed class InMemoryMembershipRepository(InMemoryDb db) : InMemoryRepository<MembershipEntity>(db), IMembershipRepository
    {
        public Task<List<MembershipEntity>> GetWithMemberAndPlanAsync(Expression<Func<MembershipEntity, bool>>? predicate,
            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> orderBy, int? take = null, CancellationToken ct = default)
        {
            IEnumerable<MembershipEntity> q = orderBy(Filter(predicate).AsQueryable());
            if (take is > 0) q = q.Take(take.Value);
            return Task.FromResult(q.ToList());
        }

        public Task<PagedResult<MembershipEntity>> GetPagedWithMemberAndPlanAsync(Expression<Func<MembershipEntity, bool>>? predicate,
            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> orderBy, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(Page(Filter(predicate).AsQueryable(), orderBy, page, pageSize));

        public Task<List<MembershipEntity>> GetHistoryForMemberAsync(int memberId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(m => m.MemberId == memberId).OrderByDescending(m => m.CreatedAt).ToList());

        public Task<List<MembershipEntity>> GetActiveForMembersAsync(IReadOnlyCollection<int> memberIds, DateTime now, CancellationToken ct = default)
            => Task.FromResult(Items.Where(m => memberIds.Contains(m.MemberId) && m.CreatedAt <= now && m.EndDate > now).ToList());
    }

    public sealed class InMemoryBookingRepository(InMemoryDb db) : InMemoryRepository<BookingEntity>(db), IBookingRepository
    {
        public Task<List<BookingEntity>> GetBySessionIdAsync(int sessionId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(b => b.SessionId == sessionId).OrderBy(b => b.CreatedAt).ToList());

        public Task<List<BookingEntity>> GetRecentByMemberIdAsync(int memberId, int take, CancellationToken ct = default)
            => Task.FromResult(Items.Where(b => b.MemberId == memberId).OrderByDescending(b => b.Session.StartDate).Take(take).ToList());
    }

    public sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public InMemoryDb Db { get; }
        public IMembershipRepository MembershipRepository { get; }
        public ISessionRepository SessionRepository { get; }
        public IBookingRepository BookingRepository { get; }
        public int Transactions { get; private set; }

        public InMemoryUnitOfWork(InMemoryDb db)
        {
            Db = db;
            MembershipRepository = new InMemoryMembershipRepository(db);
            SessionRepository = new InMemorySessionRepository(db);
            BookingRepository = new InMemoryBookingRepository(db);
        }

        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity
        {
            if (typeof(TEntity) == typeof(SessionEntity)) return (IGenericRepository<TEntity>)SessionRepository;
            if (typeof(TEntity) == typeof(MembershipEntity)) return (IGenericRepository<TEntity>)MembershipRepository;
            if (typeof(TEntity) == typeof(BookingEntity)) return (IGenericRepository<TEntity>)BookingRepository;
            return new InMemoryRepository<TEntity>(Db);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            Db.StampOnSave();
            return Task.FromResult(++Db.SaveCount);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel isolationLevel = IsolationLevel.Serializable, CancellationToken ct = default)
        {
            Transactions++;
            return await operation();
        }
    }
}
