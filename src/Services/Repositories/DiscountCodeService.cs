using ApiDiscount.Contracts;
using ApiDiscount.Database.Interfaces;
using ApiDiscount.Domains;
using ApiDiscount.Services.Interfaces;

namespace ApiDiscount.Services.Repositories
{
    public class DiscountCodeService : IDiscountCodeService
    {
        private readonly IDiscountCodeRepository _repo;
        public DiscountCodeService(IDiscountCodeRepository repo) => _repo = repo;

        public Task<int> CreateManyAsync(IEnumerable<string> codes, CancellationToken ct)
            => _repo.CreateManyAsync(codes, ct);

        public async Task<bool> CreateAsync(CreateDiscountCodeRequest req, CancellationToken ct)
        {
            var code = Normalize(req.Code);
            ValidateCodeLength(code);

            var entity = new DiscountCode
            {
                Code = code,
                CreatedAt = DateTimeOffset.UtcNow,
                Used = false,
                UsedAt = null
            };

            return await _repo.CreateAsync(entity, ct);
        }

        public Task<DiscountCode?> GetAsync(string code, CancellationToken ct)
            => _repo.GetAsync(Normalize(code), ct);

        public Task<PagedResult<DiscountCode>> ListAsync(PagedQuery query, CancellationToken ct)
            => _repo.ListAsync(query.Page, query.PageSize, query.OnlyUnused, ct);

        public async Task<bool> UpdateAsync(string code, UpdateDiscountCodeRequest req, CancellationToken ct)
        {
            var c = Normalize(code);
            ValidateCodeLength(c);
            return await _repo.UpdateUsedAsync(c, req.Used, ct);
        }

        public Task<bool> DeleteAsync(string code, CancellationToken ct)
            => _repo.DeleteAsync(Normalize(code), ct);

        public async Task<bool> UseOnceAsync(string code, CancellationToken ct)
        {
            var c = Normalize(code);
            ValidateCodeLength(c);
            return await _repo.TryUseOnceAsync(c, ct);
        }

        private static string Normalize(string code) => code.Trim().ToUpperInvariant();

        private static void ValidateCodeLength(string code)
        {
            if (code.Length is < 7 or > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(code), "code length must be 7 or 8");
            }
        }
    }
}
