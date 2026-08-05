using ChamCong.Data;
using ChamCong.Models;
using Xunit;

namespace ChamCong.Tests;

/// <summary>Kiểm tra thao tác chấm công: chấm, chấm lại, bỏ chấm và ghi ra file.</summary>
public class KhoChamCongTests : IDisposable
{
    private static readonly DateTime NgayLam = new(2026, 8, 3);

    private readonly string _thuMuc = Path.Combine(Path.GetTempPath(), "ChamCongTest-" + Guid.NewGuid().ToString("N"));

    private KhoChamCong TaoKho() => new(Path.Combine(_thuMuc, "chamcong.json"));

    public void Dispose()
    {
        if (Directory.Exists(_thuMuc))
        {
            Directory.Delete(_thuMuc, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Cham_MoiBuoiMotDong_MacDinhMotCong()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        kho.Cham(tho.Id, NgayLam, BuoiLam.Sang);
        kho.Cham(tho.Id, NgayLam, BuoiLam.Chieu);

        Assert.Equal(2, kho.DuLieu.BuoiCongs.Count);
        Assert.All(kho.DuLieu.BuoiCongs, b => Assert.Equal(1m, b.SoCong));
    }

    [Fact]
    public void Cham_ChamLaiCungBuoi_SuaDongCuChuKhongThemDongMoi()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        var lanDau = kho.Cham(tho.Id, NgayLam, BuoiLam.Sang);
        var lanSau = kho.Cham(tho.Id, NgayLam, BuoiLam.Sang, soCong: 0.5m, ghiChu: "về sớm");

        Assert.Single(kho.DuLieu.BuoiCongs);
        Assert.Equal(lanDau.Id, lanSau.Id);
        Assert.Equal(0.5m, lanSau.SoCong);
        Assert.Equal("về sớm", lanSau.GhiChu);
    }

    [Fact]
    public void Cham_ChupLaiTienMotCongCuaLucCham()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        var buoiCong = kho.Cham(tho.Id, NgayLam, BuoiLam.Sang);

        Assert.Equal(300_000m, buoiCong.TienMotCong);
    }

    [Fact]
    public void Cham_ChamNgayCoGio_ChiGiuPhanNgay()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        var buoiCong = kho.Cham(tho.Id, NgayLam.AddHours(9).AddMinutes(30), BuoiLam.Sang);

        Assert.Equal(NgayLam, buoiCong.Ngay);
        Assert.NotNull(kho.DangCham(tho.Id, NgayLam, BuoiLam.Sang));
    }

    [Fact]
    public void Cham_SoCongKhongDuong_ThiBaoLoi()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => kho.Cham(tho.Id, NgayLam, BuoiLam.Sang, soCong: 0m));
    }

    [Fact]
    public void Cham_ThoKhongCoTrongDanhSach_ThiBaoLoi()
    {
        var kho = TaoKho();

        Assert.Throws<ArgumentException>(
            () => kho.Cham(Guid.NewGuid(), NgayLam, BuoiLam.Sang));
    }

    [Fact]
    public void BoCham_XoaDungBuoiDoThoi()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);
        kho.Cham(tho.Id, NgayLam, BuoiLam.Sang);
        kho.Cham(tho.Id, NgayLam, BuoiLam.Chieu);

        Assert.True(kho.BoCham(tho.Id, NgayLam, BuoiLam.Sang));

        Assert.Null(kho.DangCham(tho.Id, NgayLam, BuoiLam.Sang));
        Assert.NotNull(kho.DangCham(tho.Id, NgayLam, BuoiLam.Chieu));
    }

    [Fact]
    public void BoCham_BuoiChuaCham_TraVeFalse()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        Assert.False(kho.BoCham(tho.Id, NgayLam, BuoiLam.Sang));
    }

    [Fact]
    public void ThoDangLam_BoQuaThoDaNghi()
    {
        var kho = TaoKho();
        kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);
        var daNghi = kho.ThemTho("Anh Bình", tienMotCong: 280_000);
        daNghi.DangLam = false;

        var danhSach = kho.ThoDangLam();

        Assert.Single(danhSach);
        Assert.Equal("Anh Tuấn", danhSach[0].Ten);
    }

    [Fact]
    public void GhiRoiDoc_GiuNguyenDuLieu()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000, dienThoai: "0912345678");
        kho.Cham(tho.Id, NgayLam, BuoiLam.Chieu, soCong: 1.5m);
        kho.ThemUng(tho.Id, NgayLam, soTien: 500_000, ghiChu: "ứng đổ xăng");

        var khoMoi = TaoKho();
        khoMoi.Doc();

        Assert.Equal("Anh Tuấn", Assert.Single(khoMoi.DuLieu.Thos).Ten);
        var buoiCong = Assert.Single(khoMoi.DuLieu.BuoiCongs);
        Assert.Equal(BuoiLam.Chieu, buoiCong.Buoi);
        Assert.Equal(1.5m, buoiCong.SoCong);
        Assert.Equal(500_000m, Assert.Single(khoMoi.DuLieu.UngTiens).SoTien);
    }

    [Fact]
    public void Doc_ChuaCoFile_ThiBatDauRong()
    {
        var kho = TaoKho();

        kho.Doc();

        Assert.Empty(kho.DuLieu.Thos);
    }

    [Fact]
    public void ThemUng_SoTienKhongDuong_ThiBaoLoi()
    {
        var kho = TaoKho();
        var tho = kho.ThemTho("Anh Tuấn", tienMotCong: 300_000);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => kho.ThemUng(tho.Id, NgayLam, soTien: 0m));
    }
}
