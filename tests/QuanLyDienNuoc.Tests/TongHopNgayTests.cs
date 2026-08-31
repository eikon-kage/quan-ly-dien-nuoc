using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Bảng kê hàng khách lấy trong một ngày: gom đủ mọi tờ của khách, hàng trả lại trừ đi,
/// và con số còn nợ không được lẫn hàng của những ngày sau.
/// </summary>
public class TongHopNgayTests
{
    private static readonly DateTime Hom1 = new(2026, 8, 30);
    private static readonly DateTime Hom2 = new(2026, 8, 31);

    private static readonly KhachHang Khach = new() { Ten = "Anh Dũng sắt Hà Đông" };

    private static ChiTietHoaDon Dong(DateTime ngay, string ten, decimal gia, decimal soLuong) =>
        new() { Ngay = ngay, TenHang = ten, DonVi = "Cái", DonGia = gia, SoLuong = soLuong };

    private static HoaDon HoaDonBan(string ma, DateTime ngayMo, params ChiTietHoaDon[] dong)
    {
        var hoaDon = new HoaDon { MaHoaDon = ma, Nam = ngayMo.Year, NgayMo = ngayMo };
        hoaDon.ChiTiet.AddRange(dong);
        return hoaDon;
    }

    // ---------- Gom dòng trong ngày ----------

    [Fact]
    public void Lam_ChiLayDongCuaDungNgayDo()
    {
        var hoaDon = HoaDonBan(
            "HD2026-01",
            Hom1,
            Dong(Hom1, "Ống 27", 45_000, 10),
            Dong(Hom2, "Co 90", 8_000, 5));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1);

        Assert.Equal(new[] { "Ống 27" }, bang.Dong.Select(d => d.Dong.TenHang));
        Assert.Equal(450_000m, bang.TienHang);
    }

    [Fact]
    public void Lam_GomCaHaiToKhiKhachLayOHaiHoaDonCungNgay()
    {
        var to1 = HoaDonBan("HD2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, 10));
        var to2 = HoaDonBan("HD2026-02", Hom2, Dong(Hom1, "Bồn 1000L", 2_500_000, 1));

        var bang = TongHopNgay.Lam(Khach, new[] { to2, to1 }, Hom1);

        // Tờ mở trước bày trước, dù danh sách truyền vào đảo ngược.
        Assert.Equal(new[] { "HD2026-01", "HD2026-02" }, bang.MaHoaDons);
        Assert.Equal(2_950_000m, bang.TienHang);
    }

    [Fact]
    public void Lam_GiuNguyenThuTuDongTrongTo()
    {
        var hoaDon = HoaDonBan(
            "HD2026-01",
            Hom1,
            Dong(Hom1, "Keo dán ống", 8_000, 2),
            Dong(Hom1, "Ống 27", 45_000, 10),
            Dong(Hom1, "Băng tan", 3_000, 5));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1);

        Assert.Equal(
            new[] { "Keo dán ống", "Ống 27", "Băng tan" },
            bang.Dong.Select(d => d.Dong.TenHang));
    }

    [Fact]
    public void Lam_NgayKhongCoGiThiBangKeTrong()
    {
        var hoaDon = HoaDonBan("HD2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, 10));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom2);

        Assert.True(bang.Trong);
        Assert.Equal(0m, bang.TienHang);
    }

    [Fact]
    public void Lam_ChiTraTienTrongNgayThiVanCoBangKe()
    {
        var hoaDon = HoaDonBan("HD2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, 10));
        hoaDon.ThanhToans.Add(new ThanhToan { Ngay = Hom2, SoTien = 200_000 });

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom2);

        Assert.False(bang.Trong);
        Assert.Empty(bang.Dong);
        Assert.Equal(200_000m, bang.DaTraTrongNgay);
    }

    [Fact]
    public void Lam_BoGioTrongNgay()
    {
        var hoaDon = HoaDonBan("HD2026-01", Hom1, Dong(Hom1.AddHours(17), "Ống 27", 45_000, 10));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1.AddHours(9));

        Assert.Single(bang.Dong);
    }

    // ---------- Hàng khách trả lại ----------

    [Fact]
    public void Lam_DongSoLuongAmLaHoanTraVaTruVaoTienHang()
    {
        var hoaDon = HoaDonBan(
            "HD2026-01",
            Hom1,
            Dong(Hom1, "Ống 27", 45_000, 10),
            Dong(Hom1, "Ống 27", 45_000, -2));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1);

        Assert.False(bang.Dong[0].LaHoanTra);
        Assert.True(bang.Dong[1].LaHoanTra);
        Assert.Equal(360_000m, bang.TienHang);
    }

    [Fact]
    public void Lam_DongCuaToHoanHangCungLaHoanTra()
    {
        var toBan = HoaDonBan("HD2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, 10));
        var toHoan = HoaDonBan("HH2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, -3));
        toHoan.Loai = LoaiHoaDon.HoanHang;
        toHoan.HoaDonGocId = toBan.Id;

        var bang = TongHopNgay.Lam(Khach, new[] { toBan, toHoan }, Hom1);

        Assert.True(bang.Dong[1].LaHoanTra);
        Assert.Equal(315_000m, bang.TienHang);
    }

    // ---------- Còn nợ ----------

    [Fact]
    public void Lam_ConNoTinhDenMocChuKhongPhaiCaSo()
    {
        var hoaDon = HoaDonBan(
            "HD2026-01",
            Hom1,
            Dong(Hom1, "Ống 27", 45_000, 10),
            Dong(Hom2, "Bồn 1000L", 2_500_000, 1));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1, mocNo: Hom1);

        Assert.Equal(450_000m, bang.ConNo);
        Assert.Equal(Hom1, bang.MocNo);
    }

    [Fact]
    public void Lam_ConNoTruTienDaTraDenMoc()
    {
        var hoaDon = HoaDonBan("HD2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, 10));
        hoaDon.ThanhToans.Add(new ThanhToan { Ngay = Hom1, SoTien = 200_000 });
        hoaDon.ThanhToans.Add(new ThanhToan { Ngay = Hom2, SoTien = 100_000 });

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1, mocNo: Hom1);

        Assert.Equal(250_000m, bang.ConNo);
    }

    [Fact]
    public void Lam_TongHopNgayCuMaConNoVanTinhDenHomNay()
    {
        var hoaDon = HoaDonBan(
            "HD2026-01",
            Hom1,
            Dong(Hom1, "Ống 27", 45_000, 10),
            Dong(Hom2, "Bồn 1000L", 2_500_000, 1));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1, mocNo: Hom2);

        Assert.Single(bang.Dong);
        Assert.Equal(2_950_000m, bang.ConNo);
    }

    [Fact]
    public void Lam_ConNoGomCaToHoanHang()
    {
        var toBan = HoaDonBan("HD2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, 10));
        var toHoan = HoaDonBan("HH2026-01", Hom1, Dong(Hom1, "Ống 27", 45_000, -3));
        toHoan.Loai = LoaiHoaDon.HoanHang;

        var bang = TongHopNgay.Lam(Khach, new[] { toBan, toHoan }, Hom1);

        Assert.Equal(315_000m, bang.ConNo);
    }

    [Fact]
    public void Lam_MocNoMacDinhLaChinhNgayTongHop()
    {
        var hoaDon = HoaDonBan(
            "HD2026-01",
            Hom1,
            Dong(Hom1, "Ống 27", 45_000, 10),
            Dong(Hom2, "Bồn 1000L", 2_500_000, 1));

        var bang = TongHopNgay.Lam(Khach, new[] { hoaDon }, Hom1);

        Assert.Equal(450_000m, bang.ConNo);
    }
}
