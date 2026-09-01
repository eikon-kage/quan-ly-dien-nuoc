using ChamCong.BaoCao;
using ChamCong.Models;
using Xunit;

namespace ChamCong.Tests;

/// <summary>Kiểm tra bảng lương: bao nhiêu công, thành bao nhiêu tiền, trừ ứng còn bao nhiêu.</summary>
public class BangLuongTests
{
    private static Tho ThemTho(DuLieuChamCong duLieu, string ten, decimal tienMotCong)
    {
        var tho = new Tho { Ten = ten, TienMotCong = tienMotCong };
        duLieu.Thos.Add(tho);
        return tho;
    }

    private static void Cham(
        DuLieuChamCong duLieu,
        Tho tho,
        DateTime ngay,
        BuoiLam buoi,
        decimal soCong = BuoiCong.CongMotBuoi,
        decimal? tienMotCong = null)
    {
        duLieu.BuoiCongs.Add(new BuoiCong
        {
            ThoId = tho.Id,
            Ngay = ngay,
            Buoi = buoi,
            SoCong = soCong,
            TienMotCong = tienMotCong ?? tho.TienMotCong,
        });
    }

    [Fact]
    public void Tinh_CongSangVaChieuTachRieng_TienNhanTheoTongCong()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Sang);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Chieu);
        Cham(duLieu, tho, new DateTime(2026, 8, 4), BuoiLam.Sang);

        var dong = Assert.Single(BangLuong.Thang(duLieu, 2026, 8));

        Assert.Equal(1m, dong.CongSang);
        Assert.Equal(0.5m, dong.CongChieu);
        // Một ngày rưỡi đi làm là một công rưỡi.
        Assert.Equal(1.5m, dong.TongCong);
        Assert.Equal(450_000m, dong.TienCong);
    }

    [Fact]
    public void Tinh_MoiThoMotGiaKhacNhau()
    {
        var duLieu = new DuLieuChamCong();
        var tuan = ThemTho(duLieu, "Anh Tuấn", 300_000);
        var binh = ThemTho(duLieu, "Anh Bình", 250_000);
        Cham(duLieu, tuan, new DateTime(2026, 8, 3), BuoiLam.Sang);
        Cham(duLieu, binh, new DateTime(2026, 8, 3), BuoiLam.Sang);

        var bang = BangLuong.Thang(duLieu, 2026, 8);

        Assert.Equal(125_000m, bang.Single(d => d.Tho.Id == binh.Id).TienCong);
        Assert.Equal(150_000m, bang.Single(d => d.Tho.Id == tuan.Id).TienCong);
    }

    [Fact]
    public void Tinh_TangLuongThoKhongLamDoiBangLuongThangTruoc()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        Cham(duLieu, tho, new DateTime(2026, 7, 10), BuoiLam.Sang);

        // Sang tháng 8 anh tăng lương cho thợ, buổi đã chấm tháng 7 vẫn giữ giá cũ.
        tho.TienMotCong = 350_000;
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Sang);

        Assert.Equal(150_000m, Assert.Single(BangLuong.Thang(duLieu, 2026, 7)).TienCong);
        Assert.Equal(175_000m, Assert.Single(BangLuong.Thang(duLieu, 2026, 8)).TienCong);
    }

    [Fact]
    public void Tinh_BuoiChuaChupGia_ThiLayGiaHienTaiCuaTho()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        duLieu.BuoiCongs.Add(new BuoiCong
        {
            ThoId = tho.Id,
            Ngay = new DateTime(2026, 8, 3),
            Buoi = BuoiLam.Sang,
            TienMotCong = null,
        });

        // Buổi để mặc định là nửa công, nên một buổi ra nửa ngày tiền.
        Assert.Equal(150_000m, Assert.Single(BangLuong.Thang(duLieu, 2026, 8)).TienCong);
    }

    [Fact]
    public void Tinh_NuaCongVaLamThem()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Sang, soCong: 0.25m);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Chieu, soCong: 0.75m);

        var dong = Assert.Single(BangLuong.Thang(duLieu, 2026, 8));

        Assert.Equal(1m, dong.TongCong);
        Assert.Equal(300_000m, dong.TienCong);
    }

    [Fact]
    public void Tinh_TruTienDaUng()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Sang);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Chieu);
        duLieu.UngTiens.Add(new UngTien { ThoId = tho.Id, Ngay = new DateTime(2026, 8, 5), SoTien = 200_000 });

        var dong = Assert.Single(BangLuong.Thang(duLieu, 2026, 8));

        Assert.Equal(200_000m, dong.DaUng);
        Assert.Equal(100_000m, dong.ConLai);
    }

    [Fact]
    public void Tinh_UngQuaTien_ThiConLaiAm()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Sang);
        duLieu.UngTiens.Add(new UngTien { ThoId = tho.Id, Ngay = new DateTime(2026, 8, 5), SoTien = 500_000 });

        Assert.Equal(-350_000m, Assert.Single(BangLuong.Thang(duLieu, 2026, 8)).ConLai);
    }

    [Fact]
    public void Tinh_ChiLayCongTrongKhoangDangXem()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        Cham(duLieu, tho, new DateTime(2026, 7, 31), BuoiLam.Sang);
        Cham(duLieu, tho, new DateTime(2026, 8, 1), BuoiLam.Sang);
        Cham(duLieu, tho, new DateTime(2026, 8, 31), BuoiLam.Sang);
        Cham(duLieu, tho, new DateTime(2026, 9, 1), BuoiLam.Sang);

        Assert.Equal(1m, Assert.Single(BangLuong.Thang(duLieu, 2026, 8)).TongCong);
    }

    [Fact]
    public void Tinh_BoQuaThoKhongCoCongVaKhongUng()
    {
        var duLieu = new DuLieuChamCong();
        ThemTho(duLieu, "Anh Tuấn", 300_000);

        Assert.Empty(BangLuong.Thang(duLieu, 2026, 8));
    }

    [Fact]
    public void Tinh_ThoDaNghiNhungTrongKyConCong_ThiVanHien()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        tho.DangLam = false;
        Cham(duLieu, tho, new DateTime(2026, 8, 3), BuoiLam.Sang);

        Assert.Single(BangLuong.Thang(duLieu, 2026, 8));
    }

    [Fact]
    public void Tinh_ChiUngMaKhongDiLam_ThiVanHienDeBietConNoAnh()
    {
        var duLieu = new DuLieuChamCong();
        var tho = ThemTho(duLieu, "Anh Tuấn", 300_000);
        duLieu.UngTiens.Add(new UngTien { ThoId = tho.Id, Ngay = new DateTime(2026, 8, 5), SoTien = 500_000 });

        var dong = Assert.Single(BangLuong.Thang(duLieu, 2026, 8));

        Assert.Equal(0m, dong.TongCong);
        Assert.Equal(-500_000m, dong.ConLai);
    }

    [Fact]
    public void Tinh_XepTheoTenTho()
    {
        var duLieu = new DuLieuChamCong();
        var tuan = ThemTho(duLieu, "Anh Tuấn", 300_000);
        var binh = ThemTho(duLieu, "Anh Bình", 250_000);
        Cham(duLieu, tuan, new DateTime(2026, 8, 3), BuoiLam.Sang);
        Cham(duLieu, binh, new DateTime(2026, 8, 3), BuoiLam.Sang);

        var bang = BangLuong.Thang(duLieu, 2026, 8);

        Assert.Equal(new[] { "Anh Bình", "Anh Tuấn" }, bang.Select(d => d.TenHienThi));
    }
}
