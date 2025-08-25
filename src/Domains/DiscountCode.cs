namespace ApiDiscount.Domains
{
    public class DiscountCode
    {
        public string Code { get; init; } = default!;
        public DateTimeOffset CreatedAt { get; init; }
        public bool Used { get; init; }
        public DateTimeOffset? UsedAt { get; init; }
    }
}
