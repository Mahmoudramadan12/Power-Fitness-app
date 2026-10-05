namespace GymManagementBLL.Common
{
    /// <summary>Base for list-page queries (search box + pager).</summary>
    public class ListQuery
    {
        public const int DefaultPageSize = 10;

        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;

        public string? SearchTerm => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }

    public interface IPagedList
    {
        int Page { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPrevious { get; }
        bool HasNext { get; }
        int FirstItem { get; }
        int LastItem { get; }
    }

    public sealed class PagedList<T> : IPagedList
    {
        public IReadOnlyList<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }

        public PagedList(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int LastItem => TotalCount == 0 ? 0 : FirstItem + Items.Count - 1;

        public static PagedList<T> Empty { get; } = new([], 0, 1, ListQuery.DefaultPageSize);
    }

    public enum SessionStatus { Upcoming, Ongoing, Completed }

    public enum MembershipStatus { Upcoming, Active, Expired }
}
