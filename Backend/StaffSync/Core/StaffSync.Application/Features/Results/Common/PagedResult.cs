namespace StaffSync.Application.Features.Results.Common
{
    public class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public IReadOnlyList<T> Items { get; }
        public int TotalCount { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public bool HasMore => PageNumber * PageSize < TotalCount;
    }
}
