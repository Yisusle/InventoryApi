namespace Inventory.Api.Constants;

public static class Paging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1
            ? AppConstants.DefaultValues.DefaultPageSize
            : Math.Min(pageSize, AppConstants.DefaultValues.MaxPageSize);

        return (normalizedPage, normalizedPageSize);
    }
}
