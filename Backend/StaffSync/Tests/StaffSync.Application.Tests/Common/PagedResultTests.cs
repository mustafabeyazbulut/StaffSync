using StaffSync.Application.Features.Results.Common;

namespace StaffSync.Application.Tests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(1, 30, 31, true)]
    [InlineData(2, 30, 60, false)]
    [InlineData(1, 30, 0, false)]
    public void HasMore_is_true_only_when_items_remain(int page, int size, int total, bool expected) =>
        Assert.Equal(expected, new PagedResult<int>(new List<int>(), total, page, size).HasMore);
}
