using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Khách đưa một cục tiền trả cho nhiều hoá đơn: phần mềm chia từ hoá đơn cũ nhất,
/// ghi thành một phiếu thu để sau này xoá được cả lần thu.
/// </summary>
public class ThuTienTests
{
    private static HoaDon TaoHoaDon(string ma, DateTime ngayMo, decimal tien, decimal daTra = 0m)
    {
        var hoaDon = new HoaDon { MaHoaDon = ma, Nam = ngayMo.Year, NgayMo = ngayMo };
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { Ngay = ngayMo, TenHang = "Ống 27", DonGia = tien, SoLuong = 1 });

        if (daTra > 0m)
        {
            hoaDon.ThanhToans.Add(new ThanhToan { Ngay = ngayMo, SoTien = daTra });
        }

        return hoaDon;
    }

    private static List<HoaDon> BaHoaDon() => new()
    {
        TaoHoaDon("HD2025-01", new DateTime(2025, 11, 2), 1_000_000),
        TaoHoaDon("HD2026-01", new DateTime(2026, 2, 10), 2_000_000),
        TaoHoaDon("HD2026-02", new DateTime(2026, 6, 1), 3_000_000),
    };

    // ---------- Chia tiền ----------

    [Fact]
    public void Chia_TraHoaDonCuNhatTruoc()
    {
        var hoaDons = BaHoaDon();

        var ketQua = ThuTien.Chia(hoaDons, 5_000_000m);

        Assert.Equal(new[] { "HD2025-01", "HD2026-01", "HD2026-02" }, ketQua.PhanBo.Select(p => p.HoaDon.MaHoaDon));
        Assert.Equal(new[] { 1_000_000m, 2_000_000m, 2_000_000m }, ketQua.PhanBo.Select(p => p.SoTien));
        Assert.Equal(5_000_000m, ketQua.DaPhanBo);
        Assert.Equal(0m, ketQua.ConDu);
    }

    [Fact]
    public void Chia_KhongDuThiChiTraDuocMayHoaDonDau()
    {
        var hoaDons = BaHoaDon();

        var ketQua = ThuTien.Chia(hoaDons, 1_500_000m);

        Assert.Equal(2, ketQua.PhanBo.Count);
        Assert.Equal(1_000_000m, ketQua.PhanBo[0].SoTien);
        Assert.Equal(500_000m, ketQua.PhanBo[1].SoTien);
        Assert.Equal(0m, ketQua.ConDu);
    }

    [Fact]
    public void Chia_BoQuaHoaDonDaTraXong()
    {
        var hoaDons = BaHoaDon();
        hoaDons[0].ThanhToans.Add(new ThanhToan { SoTien = 1_000_000 });

        var ketQua = ThuTien.Chia(hoaDons, 2_500_000m);

        Assert.Equal(new[] { "HD2026-01", "HD2026-02" }, ketQua.PhanBo.Select(p => p.HoaDon.MaHoaDon));
        Assert.Equal(2_000_000m, ketQua.PhanBo[0].SoTien);
        Assert.Equal(500_000m, ketQua.PhanBo[1].SoTien);
    }

    [Fact]
    public void Chia_TraDuThiPhanThuaDeRieng()
    {
        var hoaDons = BaHoaDon();

        var ketQua = ThuTien.Chia(hoaDons, 7_000_000m);

        Assert.Equal(6_000_000m, ketQua.DaPhanBo);
        Assert.Equal(1_000_000m, ketQua.ConDu);
    }

    [Fact]
    public void Chia_TraDuVaChoGhiDuThiPhanThuaVaoHoaDonMoiNhat()
    {
        var hoaDons = BaHoaDon();

        var ketQua = ThuTien.Chia(hoaDons, 7_000_000m, ghiDuVaoHoaDonMoiNhat: true);

        Assert.Equal(0m, ketQua.ConDu);
        Assert.Equal(7_000_000m, ketQua.DaPhanBo);

        // Hoá đơn mới nhất nhận cả phần nợ của nó lẫn phần trả trước, gộp thành một dòng.
        var moiNhat = ketQua.PhanBo.Single(p => p.HoaDon.MaHoaDon == "HD2026-02");
        Assert.Equal(4_000_000m, moiNhat.SoTien);
    }

    [Fact]
    public void Chia_KhachHetNoMaVanDuaTien_GhiTraTruocVaoHoaDonMoiNhat()
    {
        var hoaDons = BaHoaDon();
        foreach (var hoaDon in hoaDons)
        {
            hoaDon.ThanhToans.Add(new ThanhToan { SoTien = hoaDon.TongTien });
        }

        Assert.Empty(ThuTien.Chia(hoaDons, 500_000m).PhanBo);

        var traTruoc = ThuTien.Chia(hoaDons, 500_000m, ghiDuVaoHoaDonMoiNhat: true);
        var dong = Assert.Single(traTruoc.PhanBo);
        Assert.Equal("HD2026-02", dong.HoaDon.MaHoaDon);
        Assert.Equal(500_000m, dong.SoTien);
    }

    [Fact]
    public void Chia_SoTienKhongHopLeThiKhongChiaGiCa()
    {
        var hoaDons = BaHoaDon();

        Assert.Empty(ThuTien.Chia(hoaDons, 0m).PhanBo);
        Assert.Empty(ThuTien.Chia(hoaDons, -100m).PhanBo);
        Assert.Empty(ThuTien.Chia(new List<HoaDon>(), 100_000m).PhanBo);
    }

    // ---------- Ghi vào sổ ----------

    [Fact]
    public void Ghi_ChiaTienVaoTungHoaDonVoiCungMotPhieuThu()
    {
        var hoaDons = BaHoaDon();
        var ngay = new DateTime(2026, 8, 3);

        var phieuThuId = ThuTien.Ghi(ThuTien.Chia(hoaDons, 1_500_000m), ngay, "Trả gộp");

        Assert.Equal(0m, hoaDons[0].ConLai);
        Assert.Equal(1_500_000m, hoaDons[1].ConLai);
        Assert.Equal(3_000_000m, hoaDons[2].ConLai);

        var dong = hoaDons.SelectMany(h => h.ThanhToans).ToList();
        Assert.Equal(2, dong.Count);
        Assert.All(dong, t => Assert.Equal(phieuThuId, t.PhieuThuId));
        Assert.All(dong, t => Assert.Equal(ngay, t.Ngay));
        Assert.All(dong, t => Assert.Equal("Trả gộp", t.GhiChu));
    }

    [Fact]
    public void Xoa_BoCaLanThuKhoiMoiHoaDon()
    {
        var hoaDons = BaHoaDon();
        var phieuThuId = ThuTien.Ghi(ThuTien.Chia(hoaDons, 1_500_000m), new DateTime(2026, 8, 3));
        ThuTien.Ghi(ThuTien.Chia(hoaDons, 500_000m), new DateTime(2026, 8, 4));

        var daXoa = ThuTien.Xoa(hoaDons, phieuThuId);

        Assert.Equal(2, daXoa);
        Assert.Equal(500_000m, hoaDons.Sum(h => h.DaThanhToan));
    }

    [Fact]
    public void Xoa_KhoanTraGhiThangVaoMotHoaDonCungXoaDuoc()
    {
        var hoaDons = BaHoaDon();
        var lanTraLe = new ThanhToan { Ngay = new DateTime(2026, 8, 1), SoTien = 300_000 };
        hoaDons[0].ThanhToans.Add(lanTraLe);

        var lan = Assert.Single(ThuTien.LichSu(hoaDons));
        Assert.Equal(lanTraLe.Id, lan.Ma);
        Assert.False(lan.ChiaNhieuHoaDon);

        Assert.Equal(1, ThuTien.Xoa(hoaDons, lan.Ma));
        Assert.Equal(0m, hoaDons.Sum(h => h.DaThanhToan));
    }

    // ---------- Lịch sử thu tiền ----------

    [Fact]
    public void LichSu_GomLanThuChiaNhieuHoaDonThanhMotDong()
    {
        var hoaDons = BaHoaDon();
        ThuTien.Ghi(ThuTien.Chia(hoaDons, 1_500_000m), new DateTime(2026, 8, 3), "Trả gộp");
        hoaDons[2].ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 8, 5), SoTien = 200_000, GhiChu = "Trả lẻ" });

        var lichSu = ThuTien.LichSu(hoaDons);

        Assert.Equal(2, lichSu.Count);

        // Mới nhất đứng đầu.
        Assert.Equal(new DateTime(2026, 8, 5), lichSu[0].Ngay);
        Assert.Equal(200_000m, lichSu[0].SoTien);
        Assert.Equal("HD2026-02", lichSu[0].MoTaHoaDon);

        Assert.Equal(1_500_000m, lichSu[1].SoTien);
        Assert.True(lichSu[1].ChiaNhieuHoaDon);
        Assert.Equal(2, lichSu[1].SoHoaDon);
        Assert.Equal("HD2025-01, HD2026-01", lichSu[1].MoTaHoaDon);
        Assert.Equal("Trả gộp", lichSu[1].GhiChu);
    }

    [Fact]
    public void LichSu_KhongCoLanTraNaoThiRong()
    {
        Assert.Empty(ThuTien.LichSu(BaHoaDon()));
    }

    [Fact]
    public void XepTuCuNhat_TheoNgayMoRoiToiMaHoaDon()
    {
        var hoaDons = BaHoaDon();
        hoaDons.Reverse();

        Assert.Equal(
            new[] { "HD2025-01", "HD2026-01", "HD2026-02" },
            ThuTien.XepTuCuNhat(hoaDons).Select(h => h.MaHoaDon));
    }
}
