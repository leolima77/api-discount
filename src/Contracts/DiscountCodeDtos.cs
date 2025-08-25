namespace ApiDiscount.Contracts
{
    public class CreateDiscountCodeRequest
    {
        public string Code { get; set; } = default!;
    }

    public class UpdateDiscountCodeRequest
    {
        public bool Used { get; set; }
    }

    public class PagedQuery
    {
        public int Page { get; set; } = 1;       
        public int PageSize { get; set; } = 50; 
        public bool? OnlyUnused { get; set; }   
    }

    public class PagedResult<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long Total { get; init; }
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    }
}
