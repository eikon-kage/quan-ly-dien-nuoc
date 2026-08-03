using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra các cảnh báo chống nhập nhầm: giá lệch bất thường, dòng trùng, khách trùng tên.</summary>
public class KiemTraTests
{
    private static HoaDon HoaDonCo(params (string Ten, decimal Gia, DateTime Ngay)[] dong)
    {
        var hoaDon = new HoaDon { MaHoaDon = "HD2026-01", NgayMo = new DateTime(2026, 1, 1) };
        foreach (var (ten, gia, ngay) in dong)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = ten, DonGia = gia, SoLuong = 1, Ngay = ngay });
        }

        return hoaDon;
    }

    // ---------- Giá lệch ----------

    [Fact]
    public void LechGia_BaoKhiLechQuaNguong()
    {
        var hoaDon = HoaDonCo(("Ống 27", 45_000, new DateTime(2026, 5, 1)));

        var lech = KiemTra.LechGia(new[] { hoaDon }, "Ống 27", vatTuId: null, giaMoi: 90_000, nguongPhanTram: 20);

        Assert.NotNull(lech);
        Assert.Equal(45_000m, lech!.Value.GiaCu);
        Assert.Equal(new DateTime(2026, 5, 1), lech.Value.Ngay);
    }

    [Fact]
    public void LechGia_ImLangKhiLechNhoHonNguong()
    {
        var hoaDon = HoaDonCo(("Ống 27", 45_000, new DateTime(2026, 5, 1)));

        Assert.Null(KiemTra.LechGia(new[] { hoaDon }, "Ống 27", null, giaMoi: 48_000, nguongPhanTram: 20));
    }

    [Fact]
    public void LechGia_SoVoiLanBanGanNhatChuKhongPhaiLanDauTien()
    {
        var hoaDon = HoaDonCo(
            ("Ống 27", 30_000, new DateTime(2026, 1, 5)),
            ("Ống 27", 45_000, new DateTime(2026, 6, 5)));

        var lech = KiemTra.LechGia(new[] { hoaDon }, "Ống 27", null, giaMoi: 80_000, nguongPhanTram: 20);

        Assert.Equal(45_000m, lech!.Value.GiaCu);
    }

    [Fact]
    public void LechGia_KhongBaoKhiMatHangChuaTungBanChoKhachNay()
    {
        var hoaDon = HoaDonCo(("Ống 27", 45_000, new DateTime(2026, 5, 1)));

        Assert.Null(KiemTra.LechGia(new[] { hoaDon }, "Aptomat", null, giaMoi: 500_000, nguongPhanTram: 20));
    }

    [Fact]
    public void LechGia_NhanRaCungMatHangQuaMaVatTuDuTenGoKhac()
    {
        var vatTuId = Guid.NewGuid();
        var hoaDon = new HoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            VatTuId = vatTuId,
            TenHang = "Ống nhựa PVC D27",
            DonGia = 45_000,
            SoLuong = 1,
            Ngay = new DateTime(2026, 5, 1),
        });

        var lech = KiemTra.LechGia(new[] { hoaDon }, "ống 27", vatTuId, giaMoi: 90_000, nguongPhanTram: 20);

        Assert.Equal(45_000m, lech!.Value.GiaCu);
    }

    // ---------- Dòng trùng ----------

    [Fact]
    public void DongTrung_BaoKhiTrungCaNgayTenVaSoLuong()
    {
        var hoaDon = new HoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            TenHang = "Ống 27",
            SoLuong = 5,
            DonGia = 45_000,
            Ngay = new DateTime(2026, 6, 1),
        });

        var trung = KiemTra.DongTrung(hoaDon, new DateTime(2026, 6, 1), " ống 27 ", 5m);

        Assert.NotNull(trung);
    }

    [Theory]
    [InlineData("Ống 27", 6)]        // khác số lượng
    [InlineData("Ống 21", 5)]        // khác mặt hàng
    public void DongTrung_ImLangKhiKhacSoLuongHoacKhacHang(string ten, decimal soLuong)
    {
        var hoaDon = new HoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            TenHang = "Ống 27",
            SoLuong = 5,
            Ngay = new DateTime(2026, 6, 1),
        });

        Assert.Null(KiemTra.DongTrung(hoaDon, new DateTime(2026, 6, 1), ten, soLuong));
    }

    [Fact]
    public void DongTrung_ImLangKhiKhacNgay()
    {
        var hoaDon = new HoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", SoLuong = 5, Ngay = new DateTime(2026, 6, 1) });

        Assert.Null(KiemTra.DongTrung(hoaDon, new DateTime(2026, 6, 2), "Ống 27", 5m));
    }

    // ---------- Khách trùng tên ----------

    [Fact]
    public void KhachTrungTen_NhanRaDuKhacDauVaKhacHoaThuong()
    {
        var danhSach = new[] { new KhachHang { Ten = "Nguyễn Văn Bình" } };

        Assert.NotNull(KiemTra.KhachTrungTen(danhSach, "nguyen van binh"));
        Assert.Null(KiemTra.KhachTrungTen(danhSach, "Nguyễn Văn Bảo"));
    }

    [Fact]
    public void KhachTrungTen_BoQuaChinhKhachDangSua()
    {
        var khach = new KhachHang { Ten = "Cô Gấm" };
        var danhSach = new[] { khach };

        Assert.Null(KiemTra.KhachTrungTen(danhSach, "Cô Gấm", boQua: khach.Id));
    }
}
