using ApiDiscount.Contracts;
using ApiDiscount.Domains;

namespace ApiDiscount.Database.Interfaces
{
    public interface IDiscountCodeRepository
    {
        Task<int> CreateManyAsync(IEnumerable<string> codes, CancellationToken ct);
        Task<bool> CreateAsync(DiscountCode code, CancellationToken ct);
        Task<DiscountCode?> GetAsync(string code, CancellationToken ct);
        Task<PagedResult<DiscountCode>> ListAsync(int page, int pageSize, bool? onlyUnused, CancellationToken ct);
        Task<bool> UpdateUsedAsync(string code, bool used, CancellationToken ct);
        Task<bool> DeleteAsync(string code, CancellationToken ct);

        Task<bool> TryUseOnceAsync(string code, CancellationToken ct);
    }
}
