using ApiDiscount.Contracts;
using ApiDiscount.Database.Interfaces;
using ApiDiscount.Domains;
using Dapper;
using Npgsql;

namespace ApiDiscount.Database.Repositories
{
    public sealed class DiscountCodeRepository : IDiscountCodeRepository
    {
        private readonly DapperContext _ctx;
        public DiscountCodeRepository(DapperContext ctx) => _ctx = ctx;

        public async Task<int> CreateManyAsync(IEnumerable<string> codes, CancellationToken ct)
        {
            const int BatchSize = 1000;
            var totalInserted = 0;

            var normalized = codes?.Where(c => !string.IsNullOrWhiteSpace(c))
                                   .Select(c => c.Trim().ToUpperInvariant())
                                   .ToArray() ?? Array.Empty<string>();

            if (normalized.Length == 0) return 0;

            const string sql = @"
                                insert into discount.discount_code (code)
                                select c from unnest(@codes) as t(c)
                                on conflict (code) do nothing;";

            await using var conn = _ctx.CreateConnection();

            for (var i = 0; i < normalized.Length; i += BatchSize)
            {
                var slice = normalized.Skip(i).Take(BatchSize).ToArray();
                var p = new DynamicParameters();
                p.Add("codes", slice);

                totalInserted += await conn.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: ct));
            }

            return totalInserted;
        }

        public async Task<bool> CreateAsync(DiscountCode code, CancellationToken ct)
        {
            const string sql = @"
                                insert into discount.discount_code (code, created_at, used, used_at)
                                values (@code, @created_at, @used, @used_at);";
            await using var conn = _ctx.CreateConnection();
            try
            {
                var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
                {
                    code = code.Code,
                    created_at = code.CreatedAt,
                    used = code.Used,
                    used_at = code.UsedAt
                }, cancellationToken: ct)).ConfigureAwait(false);
                return rows == 1;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return false;
            }
        }

        public async Task<DiscountCode?> GetAsync(string code, CancellationToken ct)
        {
            const string sql = @"
                                select code, created_at as CreatedAt, used, used_at as UsedAt
                                  from discount.discount_code
                                 where code = @code;";
            await using var conn = _ctx.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<DiscountCode>(new CommandDefinition(sql, new { code }, cancellationToken: ct)).ConfigureAwait(false);
        }

        public async Task<PagedResult<DiscountCode>> ListAsync(int page, int pageSize, bool? onlyUnused, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var where = onlyUnused == true ? "where used = false" : string.Empty;

            var sql = $@"
                        select code, created_at as CreatedAt, used, used_at as UsedAt
                          from discount.discount_code
                          {where}
                         order by created_at desc
                         offset @off limit @lim;

                        select count(*) from discount.discount_code {(onlyUnused == true ? "where used = false" : "")};";

            await using var conn = _ctx.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new
            {
                off = (page - 1) * pageSize,
                lim = pageSize
            }, cancellationToken: ct)).ConfigureAwait(false);

            var items = (await multi.ReadAsync<DiscountCode>()).ToList();
            var total = await multi.ReadFirstAsync<long>();

            return new PagedResult<DiscountCode>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
        }

        public async Task<bool> UpdateUsedAsync(string code, bool used, CancellationToken ct)
        {
            const string sql = @"
                                update discount.discount_code
                                   set used = @used,
                                       used_at = case when @used then now() else null end
                                 where code = @code;";
            await using var conn = _ctx.CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { code, used }, cancellationToken: ct));
            return rows == 1;
        }

        public async Task<bool> DeleteAsync(string code, CancellationToken ct)
        {
            const string sql = @"delete from discount.discount_code where code = @code;";
            await using var conn = _ctx.CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { code }, cancellationToken: ct));
            return rows == 1;
        }

        public async Task<bool> TryUseOnceAsync(string code, CancellationToken ct)
        {
            const string update = @"
                                    update discount.discount_code
                                       set used = true, used_at = now()
                                     where code = @code and used = false;";
            await using var conn = _ctx.CreateConnection();
            var rows = await conn.ExecuteAsync(new CommandDefinition(update, new { code }, cancellationToken: ct));
            return rows == 1;
        }
    }
}
