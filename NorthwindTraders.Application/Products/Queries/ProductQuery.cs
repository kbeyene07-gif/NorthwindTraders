namespace NorthwindTraders.Application.Products.Queries;

public sealed class ProductQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
    public int? SupplierId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? Discontinued { get; init; }

    public string SortBy { get; init; } = "name";  // name | price | createdAt
    public string SortDir { get; init; } = "asc";  // asc | desc
}
