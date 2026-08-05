using ChamCong.Ui;
using Xunit;

namespace ChamCong.Tests;

/// <summary>Kiểm tra cách viết ngày, số công và số tiền hiện lên màn hình.</summary>
public class NgayVietTests
{
    [Theory]
    [InlineData(2026, 8, 2, "Chủ Nhật")]
    [InlineData(2026, 8, 3, "Thứ Hai")]
    [InlineData(2026, 8, 8, "Thứ Bảy")]
    public void Thu_DungTenTiengViet(int nam, int thang, int ngay, string mongDoi)
    {
        Assert.Equal(mongDoi, NgayViet.Thu(new DateTime(nam, thang, ngay)));
    }

    [Fact]
    public void ThuVaNgay_KieuHienTrenDauManHinh()
    {
        Assert.Equal("Thứ Hai 03/08", NgayViet.ThuVaNgay(new DateTime(2026, 8, 3)));
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(0.5, "0,5")]
    [InlineData(1.5, "1,5")]
    [InlineData(3, "3")]
    public void SoCong_DungDauPhayKieuViet(decimal soCong, string mongDoi)
    {
        Assert.Equal(mongDoi, NgayViet.SoCong(soCong));
    }

    [Theory]
    [InlineData(1_500_000, "1.500.000 đ")]
    [InlineData(300_000, "300.000 đ")]
    [InlineData(0, "0 đ")]
    [InlineData(-200_000, "-200.000 đ")]
    public void Tien_VietDuChuSoKemDonVi(decimal soTien, string mongDoi)
    {
        Assert.Equal(mongDoi, NgayViet.Tien(soTien));
    }
}
