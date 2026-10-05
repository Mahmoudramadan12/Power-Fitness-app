namespace GymManagementDAL.Common
{
    /// <summary>One page of a larger result set plus the totals needed to render a pager.</summary>
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static PagedResult<T> Empty(int page = 1, int pageSize = 10) => new([], 0, page, pageSize);
    }
}
