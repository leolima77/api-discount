using ApiDiscount.Contracts;
using ApiDiscount.Database.Interfaces;   
using ApiDiscount.Domains;            
using ApiDiscount.Services.Interfaces;  
using ApiDiscount.Services.Repositories;
using FluentAssertions;
using Moq;

namespace Discount.Api.Tests;

public class DiscountCodeServiceTests
{
    private readonly Mock<IDiscountCodeRepository> _repo = new();
    private readonly IDiscountCodeService _svc;

    public DiscountCodeServiceTests()
    {
        _svc = new DiscountCodeService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalize_And_InsertEntity()
    {
        var req = new CreateDiscountCodeRequest { Code = "  abC1234 " };

        DiscountCode? captured = null;

        _repo.Setup(r => r.CreateAsync(It.IsAny<DiscountCode>(), It.IsAny<CancellationToken>()))
             .Callback<DiscountCode, CancellationToken>((d, _) => captured = d)
             .ReturnsAsync(true);

        var ok = await _svc.CreateAsync(req, CancellationToken.None);

        ok.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Code.Should().Be("ABC1234");
        captured.Used.Should().BeFalse();
        captured.UsedAt.Should().BeNull();
        captured.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("ABC12")]    
    [InlineData("ABC123456")]
    public async Task CreateAsync_InvalidLength_ShouldThrow(string code)
    {
        var req = new CreateDiscountCodeRequest { Code = code };

        var act = async () => await _svc.CreateAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*code length must be 7 or 8*");
        _repo.Verify(r => r.CreateAsync(It.IsAny<DiscountCode>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepoReturnsFalse_ShouldPropagateFalse()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<DiscountCode>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        var ok = await _svc.CreateAsync(new CreateDiscountCodeRequest { Code = "ABC1234" }, CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_ShouldNormalizeBeforeCallingRepo()
    {
        _repo.Setup(r => r.GetAsync("ABC1234", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new DiscountCode { Code = "ABC1234", CreatedAt = DateTimeOffset.UtcNow });

        var item = await _svc.GetAsync("  abc1234 ", CancellationToken.None);

        item.Should().NotBeNull();
        item!.Code.Should().Be("ABC1234");
        _repo.Verify(r => r.GetAsync("ABC1234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ShouldPassParametersThrough()
    {
        var query = new PagedQuery { Page = 2, PageSize = 10, OnlyUnused = true };
        var expected = new PagedResult<DiscountCode>
        {
            Page = 2,
            PageSize = 10,
            Total = 100,
            Items = new[] { new DiscountCode { Code = "ABC1234", CreatedAt = DateTimeOffset.UtcNow } }
        };

        _repo.Setup(r => r.ListAsync(2, 10, true, It.IsAny<CancellationToken>()))
             .ReturnsAsync(expected);

        var resp = await _svc.ListAsync(query, CancellationToken.None);

        resp.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UpdateAsync_ShouldValidateAndCallRepo()
    {
        _repo.Setup(r => r.UpdateUsedAsync("ABC1234", true, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var ok = await _svc.UpdateAsync("abc1234", new UpdateDiscountCodeRequest { Used = true }, CancellationToken.None);

        ok.Should().BeTrue();
        _repo.Verify(r => r.UpdateUsedAsync("ABC1234", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidLength_ShouldThrow()
    {
        var act = async () => await _svc.UpdateAsync("ABC12", new UpdateDiscountCodeRequest { Used = true }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
                 .WithMessage("*code length must be 7 or 8*");

        _repo.Verify(r => r.UpdateUsedAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNormalizeAndCallRepo()
    {
        _repo.Setup(r => r.DeleteAsync("ABC1234", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var ok = await _svc.DeleteAsync("  abc1234 ", CancellationToken.None);

        ok.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync("ABC1234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UseOnceAsync_ShouldValidateAndCallRepo()
    {
        _repo.Setup(r => r.TryUseOnceAsync("ABC1234", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var ok = await _svc.UseOnceAsync("abc1234", CancellationToken.None);

        ok.Should().BeTrue();
        _repo.Verify(r => r.TryUseOnceAsync("ABC1234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UseOnceAsync_InvalidLength_ShouldThrow()
    {
        var act = async () => await _svc.UseOnceAsync("ABC123456", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
                 .WithMessage("*code length must be 7 or 8*");

        _repo.Verify(r => r.TryUseOnceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateManyAsync_ShouldDelegateAndReturnCount()
    {
        var input = new[] { "a1", "b2", "c3" };
        _repo.Setup(r => r.CreateManyAsync(input, It.IsAny<CancellationToken>()))
             .ReturnsAsync(2);

        var inserted = await _svc.CreateManyAsync(input, CancellationToken.None);

        inserted.Should().Be(2);
        _repo.Verify(r => r.CreateManyAsync(input, It.IsAny<CancellationToken>()), Times.Once);
    }
}
