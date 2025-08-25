using ApiDiscount.Helpers;
using ApiDiscount.Services.Interfaces;
using Discount.Proto;
using Grpc.Core;

namespace ApiDiscount.Services.Grpc
{
    public sealed class DiscountServiceGrpc : DiscountService.DiscountServiceBase
    {
        private readonly IDiscountCodeService _service;
        private readonly CodeGenerator _generator;

        public DiscountServiceGrpc(IDiscountCodeService service, CodeGenerator generator)
        {
            _service = service;
            _generator = generator;
        }

        public override async Task<GenerateResponse> Generate(GenerateRequest request, ServerCallContext context)
        {
            var ct = context.CancellationToken;

            if (request.Count < 1 || request.Count > 2000)
            {
                return new GenerateResponse
                {
                    Result = false
                };
            }

            if (request.Length is < 7 or > 8)
            {
                return new GenerateResponse
                {
                    Result = false
                };
            }

            int target = (int)request.Count;
            int length = (int)request.Length;
            int inserted = 0;

            const int batchSize = 512;

            while (inserted < target && !ct.IsCancellationRequested)
            {
                int missing = target - inserted;
                int take = Math.Min(batchSize, missing);

                var bag = new System.Collections.Concurrent.ConcurrentBag<string>();
                System.Threading.Tasks.Parallel.For(0, take, _ => bag.Add(_generator.Next(length)));

                var added = await _service.CreateManyAsync(bag, ct).ConfigureAwait(false);
                inserted += added;

                if (added == 0) break;
            }

            return new GenerateResponse { Result = (inserted == target) };
        }

        public override async Task<UseCodeResponse> UseCode(UseCodeRequest request, ServerCallContext context)
        {
            var ct = context.CancellationToken;
            var code = Normalize(request.Code);

            if (code.Length is < 7 or > 8)
            {
                return new UseCodeResponse
                {
                    Result = UseResult.UseInvalid
                }; // INVALID
            }

            var usedNow = await _service.UseOnceAsync(code, ct);
            if (usedNow)
            {
                return new UseCodeResponse
                {
                    Result = UseResult.UseOk
                }; // OK
            }

            var entity = await _service.GetAsync(code, ct);
            if (entity is null)
            {
                return new UseCodeResponse
                {
                    Result = UseResult.UseInvalid
                }; // INVALID
            }

            return new UseCodeResponse
            {
                Result = entity.Used ? UseResult.UseAlreadyused : UseResult.UseInvalid
            }; // ALREADY_USED : INVALID
        }

        private static string Normalize(string s) => (s ?? string.Empty).Trim().ToUpperInvariant();
    }
}
