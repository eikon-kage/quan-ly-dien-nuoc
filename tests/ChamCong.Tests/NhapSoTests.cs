using ChamCong.Ui;
using Xunit;

namespace ChamCong.Tests;

/// <summary>Kiểm tra đọc số tiền người dùng gõ — gõ kiểu gì cũng phải hiểu.</summary>
public class NhapSoTests
{
    [Theory]
    [InlineData("300000", 300_000)]
    [InlineData("300.000", 300_000)]
    [InlineData("300,000", 300_000)]
    [InlineData("300 000", 300_000)]
    [InlineData("300.000 đ", 300_000)]
    public void DocTien_GoKieuNaoCungHieu(string chu, decimal mongDoi)
    {
        Assert.Equal(mongDoi, NhapSo.DocTien(chu));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void DocTien_KhongCoChuSo_ThiTraVeNull(string? chu)
    {
        Assert.Null(NhapSo.DocTien(chu));
    }
}
