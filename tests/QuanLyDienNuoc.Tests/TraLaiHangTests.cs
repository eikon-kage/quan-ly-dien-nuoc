using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Khách trả lại hàng: dòng hàng ghi số lượng âm nên thành tiền trừ bớt vào hoá đơn,
/// in ra và xuất Excel đều có dấu trừ.
/// </summary>
public class TraLaiHangTests
{
    private static HoaDon TaoHoaDon()
    {
        var hoaDon = new HoaDon { MaHoaDon = "HD2026-01", NgayMo = new DateTime(2026, 3, 5) };
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 5),
            TenHang = "Ống 27",
            DonVi = "Cây",
            DonGia = 45_000,
            SoLuong = 10,
        });

        return hoaDon;
    }

    [Fact]
    public void DongTraLai_ThanhTienAmVaTruVaoTongHoaDon()
    {
        var hoaDon = TaoHoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = -2 });

        var dongTraLai = hoaDon.ChiTiet[^1];
        Assert.True(dongTraLai.LaTraLai);
        Assert.Equal(-90_000m, dongTraLai.ThanhTien);

        // 10 cây bán ra, trả lại 2 cây: hoá đơn còn 8 cây.
        Assert.Equal(360_000m, hoaDon.TongTien);
        Assert.Equal(360_000m, hoaDon.ConLai);
    }

    [Fact]
    public void DongLayHang_KhongPhaiTraLai()
    {
        var dong = new ChiTietHoaDon { SoLuong = 3, DonGia = 1000 };

        Assert.False(dong.LaTraLai);
        Assert.Equal(3000m, dong.ThanhTien);
    }

    [Fact]
    public void TraLaiHetHang_HoaDonVeKhong()
    {
        var hoaDon = TaoHoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = -10 });

        Assert.Equal(0m, hoaDon.TongTien);
    }

    [Fact]
    public void TraLaiSauKhiDaTraTien_ThanhTienTraThua()
    {
        var hoaDon = TaoHoaDon();
        hoaDon.ThanhToans.Add(new ThanhToan { SoTien = 450_000 });
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = -2 });

        // Khách trả đủ 450.000 rồi mới trả lại 2 cây: cửa hàng đang giữ thừa 90.000 của khách.
        Assert.Equal(-90_000m, hoaDon.ConLai);
    }

    // ---------- Chống nhập nhầm ----------

    [Fact]
    public void SoLuongDangGiu_CongHangDaLayVaTruHangDaTraLai()
    {
        var hoaDon = TaoHoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = -2 });

        Assert.Equal(8m, KiemTra.SoLuongDangGiu(new[] { hoaDon }, "Ống 27", vatTuId: null));
        Assert.Equal(0m, KiemTra.SoLuongDangGiu(new[] { hoaDon }, "Ống 34", vatTuId: null));
    }

    [Fact]
    public void TraLaiQuaSoDaMua_HoiLaiKemSoDangGiu()
    {
        var hoaDon = TaoHoaDon();

        // Khách chỉ lấy 10 cây mà ghi trả lại 12 — chắc gõ nhầm.
        Assert.Equal(10m, KiemTra.TraLaiQuaSoDaMua(new[] { hoaDon }, "Ống 27", null, -12));

        // Trả lại vừa đủ hoặc ít hơn thì không hỏi.
        Assert.Null(KiemTra.TraLaiQuaSoDaMua(new[] { hoaDon }, "Ống 27", null, -10));
        Assert.Null(KiemTra.TraLaiQuaSoDaMua(new[] { hoaDon }, "Ống 27", null, -3));

        // Dòng lấy hàng bình thường thì phép kiểm này không dính dáng gì.
        Assert.Null(KiemTra.TraLaiQuaSoDaMua(new[] { hoaDon }, "Ống 27", null, 99));
    }

    [Fact]
    public void TraLaiQuaSoDaMua_TinhCaHangDaTraLaiTruocDo()
    {
        var hoaDon = TaoHoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = -8 });

        // Còn giữ 2 cây, trả lại thêm 3 là quá.
        Assert.Equal(2m, KiemTra.TraLaiQuaSoDaMua(new[] { hoaDon }, "Ống 27", null, -3));
        Assert.Null(KiemTra.TraLaiQuaSoDaMua(new[] { hoaDon }, "Ống 27", null, -2));
    }

    // ---------- Hiển thị / in ----------

    [Fact]
    public void HienThi_SoAmCoDauTru()
    {
        Assert.Equal("-90.000", So.Tien(-90_000m));
        Assert.Equal("-1,7", So.Luong(-1.7m));
    }

    [Fact]
    public void DocTien_HoaDonAmDocLaAm()
    {
        Assert.Equal("Âm chín mươi nghìn đồng", DocSo.DocTien(-90_000m));
    }

    // ---------- Gõ nhanh một dòng nhiều món ----------

    [Fact]
    public void DongNhapNhanh_SoAmLaHangTraLai()
    {
        var muc = DongNhapNhanh.Tach("ống 27 x-2, keo x1");

        Assert.Equal(2, muc.Count);
        Assert.Equal("ống 27", muc[0].Ten);
        Assert.Equal(-2m, muc[0].SoLuong);
        Assert.Equal(1m, muc[1].SoLuong);
    }

    [Fact]
    public void DongNhapNhanh_SoLuongKhongThiVanLayMot()
    {
        var muc = Assert.Single(DongNhapNhanh.Tach("băng tan x0"));

        Assert.Equal(1m, muc.SoLuong);
    }
}
