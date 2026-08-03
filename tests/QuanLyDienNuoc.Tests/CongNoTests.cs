using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra sổ công nợ: ai nợ bao nhiêu, nợ bao lâu, và tin nhắc nợ soạn ra sao.</summary>
public class CongNoTests
{
    private static readonly DateTime HomNay = new(2026, 8, 3);

    private static (DuLieuApp DuLieu, KhachHang Khach) TaoKhach(string ten = "Ông Long")
    {
        var khach = new KhachHang { Ten = ten, DienThoai = "0912345678" };
        var duLieu = new DuLieuApp();
        duLieu.KhachHangs.Add(khach);
        return (duLieu, khach);
    }

    private static HoaDon ThemHoaDon(
        DuLieuApp duLieu,
        KhachHang khach,
        DateTime ngayMo,
        decimal tien,
        decimal daTra = 0m,
        DateTime? ngayTra = null)
    {
        var hoaDon = new HoaDon
        {
            KhachHangId = khach.Id,
            Nam = ngayMo.Year,
            MaHoaDon = $"HD{ngayMo.Year}-{duLieu.HoaDons.Count + 1:00}",
            NgayMo = ngayMo,
        };

        hoaDon.ChiTiet.Add(new ChiTietHoaDon { Ngay = ngayMo, TenHang = "Ống 27", DonGia = tien, SoLuong = 1 });
        if (daTra > 0m)
        {
            hoaDon.ThanhToans.Add(new ThanhToan { Ngay = ngayTra ?? ngayMo, SoTien = daTra });
        }

        duLieu.HoaDons.Add(hoaDon);
        return hoaDon;
    }

    [Fact]
    public void Tinh_KhongLietKeKhachDaTraDu()
    {
        var (duLieu, khach) = TaoKhach();
        ThemHoaDon(duLieu, khach, new DateTime(2026, 1, 10), tien: 500_000, daTra: 500_000);

        Assert.Empty(CongNo.Tinh(duLieu, nam: null, HomNay));
    }

    [Fact]
    public void Tinh_TinhDungTienVaSoNgayNo()
    {
        var (duLieu, khach) = TaoKhach();
        ThemHoaDon(duLieu, khach, new DateTime(2026, 6, 3), tien: 1_000_000, daTra: 400_000, ngayTra: new DateTime(2026, 6, 10));

        var dong = Assert.Single(CongNo.Tinh(duLieu, nam: null, HomNay));

        Assert.Equal(1_000_000m, dong.TongMua);
        Assert.Equal(400_000m, dong.DaTra);
        Assert.Equal(600_000m, dong.ConNo);
        Assert.Equal(1, dong.SoHoaDonNo);

        // Mốc gần nhất là lần trả tiền 10/6, không phải ngày lấy hàng 3/6.
        Assert.Equal(new DateTime(2026, 6, 10), dong.PhatSinhCuoi);
        Assert.Equal(new DateTime(2026, 6, 10), dong.TraCuoi);
        Assert.Equal(54, dong.SoNgayNo);
    }

    [Fact]
    public void Tinh_HoaDonDaTraDuKhongKeoDaiSoNgayNo()
    {
        var (duLieu, khach) = TaoKhach();

        // Hoá đơn cũ còn nợ, hoá đơn mới đã trả xong: số ngày nợ phải tính theo hoá đơn cũ.
        ThemHoaDon(duLieu, khach, new DateTime(2026, 1, 5), tien: 800_000);
        ThemHoaDon(duLieu, khach, new DateTime(2026, 7, 30), tien: 200_000, daTra: 200_000);

        var dong = Assert.Single(CongNo.Tinh(duLieu, nam: null, HomNay));

        Assert.Equal(800_000m, dong.ConNo);
        Assert.Equal(new DateTime(2026, 1, 5), dong.PhatSinhCuoi);
        Assert.Equal(210, dong.SoNgayNo);
    }

    [Fact]
    public void Tinh_XepKhachNoLauNhatLenTruoc()
    {
        var duLieu = new DuLieuApp();
        var moi = new KhachHang { Ten = "Khách mới" };
        var cu = new KhachHang { Ten = "Khách cũ" };
        duLieu.KhachHangs.Add(moi);
        duLieu.KhachHangs.Add(cu);

        ThemHoaDon(duLieu, moi, new DateTime(2026, 7, 25), tien: 5_000_000);
        ThemHoaDon(duLieu, cu, new DateTime(2026, 2, 1), tien: 300_000);

        var dong = CongNo.Tinh(duLieu, nam: null, HomNay);

        Assert.Equal("Khách cũ", dong[0].Khach.Ten);
        Assert.Equal("Khách mới", dong[1].Khach.Ten);
    }

    [Fact]
    public void Tinh_LocDungTheoNam()
    {
        var (duLieu, khach) = TaoKhach();
        ThemHoaDon(duLieu, khach, new DateTime(2025, 4, 2), tien: 700_000);
        ThemHoaDon(duLieu, khach, new DateTime(2026, 4, 2), tien: 300_000);

        Assert.Equal(1_000_000m, CongNo.Tinh(duLieu, nam: null, HomNay)[0].ConNo);
        Assert.Equal(700_000m, CongNo.Tinh(duLieu, nam: 2025, HomNay)[0].ConNo);
        Assert.Equal(300_000m, CongNo.Tinh(duLieu, nam: 2026, HomNay)[0].ConNo);
    }

    [Fact]
    public void QuaHan_ChiLayKhachNoTuNgayNguongTroLen()
    {
        var duLieu = new DuLieuApp();
        var lau = new KhachHang { Ten = "Nợ lâu" };
        var moi = new KhachHang { Ten = "Nợ mới" };
        duLieu.KhachHangs.Add(lau);
        duLieu.KhachHangs.Add(moi);

        ThemHoaDon(duLieu, lau, new DateTime(2026, 1, 1), tien: 100_000);
        ThemHoaDon(duLieu, moi, new DateTime(2026, 7, 20), tien: 100_000);

        var quaHan = CongNo.QuaHan(CongNo.Tinh(duLieu, nam: null, HomNay), soNgay: 60);

        Assert.Equal("Nợ lâu", Assert.Single(quaHan).Khach.Ten);
    }

    [Fact]
    public void TinNhacNo_LietKeTungHoaDonConNoVaTongTien()
    {
        var (duLieu, khach) = TaoKhach("Chú Hải");
        ThemHoaDon(duLieu, khach, new DateTime(2026, 3, 4), tien: 1_500_000, daTra: 500_000);
        ThemHoaDon(duLieu, khach, new DateTime(2026, 5, 6), tien: 200_000, daTra: 200_000);

        var tin = TinNhacNo.Soan(khach, duLieu.HoaDons, HomNay);

        Assert.Contains("Chú Hải", tin);
        Assert.Contains("HD2026-01", tin);
        Assert.DoesNotContain("HD2026-02", tin);       // hoá đơn đã trả xong thì không nhắc
        Assert.Contains("1.000.000", tin);             // còn lại
        Assert.Contains("03/08/2026", tin);
    }
}
