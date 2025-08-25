using ApiDiscount.Contracts;
using ApiDiscount.Domains;
using System.Collections.Concurrent;

namespace ApiDiscount.Services.Interfaces
{
    public interface IDiscountCodeService
    {
        Task<int> CreateManyAsync(IEnumerable<string> codes, CancellationToken ct);

        Task<bool> CreateAsync(CreateDiscountCodeRequest req, CancellationToken ct);
        Task<DiscountCode?> GetAsync(string code, CancellationToken ct);
        Task<PagedResult<DiscountCode>> ListAsync(PagedQuery query, CancellationToken ct);
        Task<bool> UpdateAsync(string code, UpdateDiscountCodeRequest req, CancellationToken ct);
        Task<bool> DeleteAsync(string code, CancellationToken ct);

        Task<bool> UseOnceAsync(string code, CancellationToken ct);
    }
}
