using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra cách cộng tiền của hoá đơn nhiều ngày, nhiều lần trả.</summary>
public class HoaDonTests
{
    private static ChiTietHoaDon Dong(decimal donGia, decimal soLuong) =>
        new() { DonGia = donGia, SoLuong = soLuong };

    [Fact]
    public void ThanhTien_NhanDonGiaVoiSoLuong()
    {
        Assert.Equal(96000m, Dong(32000, 3).ThanhTien);
    }

    [Fact]
    public void ThanhTien_LamTronVeDongNguyen()
    {
        // 12000 x 2.5 = 30000 (chẵn)
        Assert.Equal(30000m, Dong(12000, 2.5m).ThanhTien);

        // 4500 x 0.5 = 2250 -> giữ nguyên vì đã là số nguyên
        Assert.Equal(2250m, Dong(4500, 0.5m).ThanhTien);

        // 3333 x 1.5 = 4999.5 -> làm tròn lên 5000 (AwayFromZero)
        Assert.Equal(5000m, Dong(3333, 1.5m).ThanhTien);

        // 3331 x 1.5 = 4996.5 -> làm tròn lên 4997
        Assert.Equal(4997m, Dong(3331, 1.5m).ThanhTien);
    }

    [Fact]
    public void TongTien_CongTatCaCacDongTrongHoaDon()
    {
        var hoaDon = new HoaDon
        {
            ChiTiet = { Dong(32000, 3), Dong(45000, 2), Dong(4000, 10) },
        };

        Assert.Equal(96000m + 90000m + 40000m, hoaDon.TongTien);
    }

    [Fact]
    public void TongTien_BangKhongKhiChuaLayHang()
    {
        Assert.Equal(0m, new HoaDon().TongTien);
    }

    [Fact]
    public void ConLai_TruDanQuaNhieuLanTra()
    {
        var hoaDon = new HoaDon
        {
            ChiTiet = { Dong(100000, 5) },   // 500.000
            ThanhToans =
            {
                new ThanhToan { SoTien = 200000 },
                new ThanhToan { SoTien = 150000 },
            },
        };

        Assert.Equal(500000m, hoaDon.TongTien);
        Assert.Equal(350000m, hoaDon.DaThanhToan);
        Assert.Equal(150000m, hoaDon.ConLai);
    }

    [Fact]
    public void ConLai_AmKhiKhachTraDu()
    {
        var hoaDon = new HoaDon
        {
            ChiTiet = { Dong(100000, 1) },
            ThanhToans = { new ThanhToan { SoTien = 150000 } },
        };

        Assert.Equal(-50000m, hoaDon.ConLai);
    }

    [Fact]
    public void DaChot_ChiDungKhiDaCoNgayChot()
    {
        var hoaDon = new HoaDon();
        Assert.False(hoaDon.DaChot);

        hoaDon.NgayChot = new DateTime(2026, 8, 3);
        Assert.True(hoaDon.DaChot);
    }
}
