using ChamCong.SoDiDong;
using Xunit;

namespace ChamCong.Tests;

/// <summary>
/// Kiểm tra phần đọc sổ chấm công do app điện thoại ghi ra: mở gói, vá dáng cũ, và tính lại
/// bảng lương kỳ đang mở. Con số ở đây phải trùng với app điện thoại — chủ cửa hàng sẽ đặt hai
/// màn hình cạnh nhau mà so.
/// </summary>
public class SoDiDongTests
{
    private static string GoiJson(string duLieu, string app = "cham-cong", int phienBan = 1) =>
        $$"""
        { "app": "{{app}}", "phienBan": {{phienBan}}, "taoLuc": "2026-08-20T03:00:00.000Z", "duLieu": {{duLieu}} }
        """;

    [Fact]
    public void Doc_GoiDayDu_RaDungSoTho_BuoiCong_UngTien_Ky()
    {
        var goi = Goi.Doc(GoiJson("""
        {
          "thos": [{
            "id": "t1", "ten": "Anh Tuấn", "dienThoai": "0912", "dangLam": true,
            "mocLuong": [{ "tuNgay": "2026-01-01", "tienMotCong": 300000 }],
            "ghiChu": "", "ngayTao": "2026-01-01", "suaLuc": "2026-01-01T00:00:00.000Z"
          }],
          "buoiCongs": [{
            "id": "b1", "thoId": "t1", "ngay": "2026-08-03", "buoi": "Sang",
            "soCong": 1, "tienMotCong": null, "ghiChu": "", "suaLuc": "2026-08-03T00:00:00.000Z"
          }],
          "ungTiens": [{
            "id": "u1", "thoId": "t1", "ngay": "2026-08-04", "soTien": 100000,
            "ghiChu": "mua xăng", "suaLuc": "2026-08-04T00:00:00.000Z"
          }],
          "kyLuongs": []
        }
        """));

        Assert.Equal("cham-cong", goi.App);
        var tomTat = Goi.Dem(goi.DuLieu);
        Assert.Equal(new TomTat(1, 1, 1, 0), tomTat);
        Assert.Equal("Anh Tuấn", goi.DuLieu.Thos[0].Ten);
        Assert.Equal(300_000m, goi.DuLieu.Thos[0].MocLuong[0].TienMotCong);
        Assert.Null(goi.DuLieu.BuoiCongs[0].TienMotCong);
        Assert.Equal("Sáng", goi.DuLieu.BuoiCongs[0].BuoiTiengViet);
        Assert.Equal(100_000m, goi.DuLieu.UngTiens[0].SoTien);
    }

    [Fact]
    public void Doc_ThieuCacMang_ThiCoiNhuRong_ChuKhongNem()
    {
        var goi = Goi.Doc(GoiJson("""{ "thos": [] }"""));

        Assert.Equal(new TomTat(0, 0, 0, 0), Goi.Dem(goi.DuLieu));
    }

    [Fact]
    public void Doc_ThoDangCu_ChuyenTienMotCongThanhMocLuongDauTien()
    {
        var goi = Goi.Doc(GoiJson("""
        { "thos": [{ "id": "t1", "ten": "Chú Hải", "tienMotCong": 250000, "ngayTao": "2025-03-04" }] }
        """));

        var tho = goi.DuLieu.Thos[0];
        Assert.Single(tho.MocLuong);
        Assert.Equal("2025-03-04", tho.MocLuong[0].TuNgay);
        Assert.Equal(250_000m, tho.MocLuong[0].TienMotCong);
    }

    [Theory]
    [InlineData("khong-phai-json")]
    [InlineData("""{ "app": "app-khac", "phienBan": 1, "duLieu": {} }""")]
    [InlineData("""{ "app": "cham-cong", "phienBan": 99, "duLieu": {} }""")]
    [InlineData("""{ "app": "cham-cong", "phienBan": 1 }""")]
    [InlineData("[]")]
    public void Doc_GoiKhongDungKhuon_ThiNem(string json)
    {
        Assert.Throws<GoiHong>(() => Goi.Doc(json));
    }

    [Fact]
    public void TienMotCongNgay_LayMocGanNhatTinhVeTruoc()
    {
        var tho = new Tho
        {
            MocLuong =
            {
                new MocLuong { TuNgay = "2026-01-01", TienMotCong = 300_000 },
                new MocLuong { TuNgay = "2026-07-01", TienMotCong = 350_000 },
            },
        };

        Assert.Equal(300_000m, tho.TienMotCongNgay("2026-06-30"));
        Assert.Equal(350_000m, tho.TienMotCongNgay("2026-07-01"));
        Assert.Equal(350_000m, tho.TienMotCongNgay("2026-12-31"));

        // Ngày trước cả mốc đầu tiên (chấm bù) vẫn phải ra tiền, không để 0.
        Assert.Equal(300_000m, tho.TienMotCongNgay("2025-05-05"));
    }

    [Fact]
    public void Doc_GhiChuTheoNgay_DocRaDuocDeMaXem()
    {
        // Ghi chú là của **cả ngày**, không của một buổi: ngày thợ nghỉ hẳn vẫn có ghi chú, mà
        // đó lại đúng là ngày cần ghi chú nhất. Máy tính chỉ đọc ra xem, chủ gõ trên điện thoại.
        var so = Goi.Doc(GoiJson("""
        {
          "thos": [{ "id": "t1", "ten": "Anh Tuấn",
            "mocLuong": [{ "tuNgay": "2026-01-01", "tienMotCong": 300000 }] }],
          "ghiChuNgays": [
            { "thoId": "t1", "ngay": "2026-08-20", "noiDung": "nghỉ đám cưới",
              "suaLuc": "2026-08-20T02:00:00.000Z" }
          ]
        }
        """)).DuLieu;

        var ghi = Assert.Single(so.GhiChuNgays);
        Assert.Equal("t1", ghi.ThoId);
        Assert.Equal("2026-08-20", ghi.Ngay);
        Assert.Equal("nghỉ đám cưới", ghi.NoiDung);

        // Và ngày ấy không có buổi công nào — chính là chỗ ghi chú phải sống được một mình.
        Assert.Empty(so.BuoiCongs);
    }

    [Fact]
    public void Doc_SoCuaBanAppCu_ChuaCoGhiChuNgay_ThiRaDanhSachRong()
    {
        var so = Goi.Doc(GoiJson("""{ "thos": [] }""")).DuLieu;

        Assert.Empty(so.GhiChuNgays);
    }

    [Fact]
    public void KyHienTai_BoQuaBanGhiDaChot_VaCongNoKyTruoc()
    {
        var so = new SoChamCong
        {
            Thos =
            {
                new Tho
                {
                    Id = "t1",
                    Ten = "Anh Tuấn",
                    MocLuong = { new MocLuong { TuNgay = "2026-01-01", TienMotCong = 300_000 } },
                },
            },
            BuoiCongs =
            {
                new BuoiCong { Id = "b-cu", ThoId = "t1", Ngay = "2026-07-10", Buoi = "Sang", SoCong = 1 },
                new BuoiCong { Id = "b-moi", ThoId = "t1", Ngay = "2026-08-03", Buoi = "Sang", SoCong = 1 },
                new BuoiCong { Id = "b-moi-2", ThoId = "t1", Ngay = "2026-08-03", Buoi = "Chieu", SoCong = 0.5m },
            },
            UngTiens =
            {
                new UngTien { Id = "u-cu", ThoId = "t1", Ngay = "2026-07-11", SoTien = 50_000 },
                new UngTien { Id = "u-moi", ThoId = "t1", Ngay = "2026-08-04", SoTien = 100_000 },
            },
            KyLuongs =
            {
                new KyLuong
                {
                    Id = "k1",
                    TuNgay = "2026-07-01",
                    DenNgay = "2026-07-31",
                    BuoiCongIds = { "b-cu" },
                    UngTienIds = { "u-cu" },
                    Dongs = { new DongQuyetToan { ThoId = "t1", ChuyenKySau = 20_000 } },
                },
            },
        };

        var ky = BangLuongSo.KyHienTai(so);
        var dong = Assert.Single(ky.Dongs);

        // Chỉ hai buổi chưa chốt: 1 + 0,5 công.
        Assert.Equal(1m, dong.CongSang);
        Assert.Equal(0.5m, dong.CongChieu);
        Assert.Equal(1.5m, dong.TongCong);
        Assert.Equal(450_000m, dong.TienCong);
        Assert.Equal(100_000m, dong.DaUng);
        Assert.Equal(20_000m, dong.NoKyTruoc);
        Assert.Equal(370_000m, dong.ConLai);
        Assert.Equal(370_000m, ky.TongPhaiTra);
        Assert.Equal("2026-08-03", ky.TuNgay);
        Assert.Equal("2026-08-04", ky.DenNgay);
    }

    [Fact]
    public void KyHienTai_ThoChiConMonNoMangSang_VanHienRa()
    {
        var so = new SoChamCong
        {
            Thos = { new Tho { Id = "t1", Ten = "Chú Hải" } },
            KyLuongs =
            {
                new KyLuong
                {
                    Id = "k1",
                    Dongs = { new DongQuyetToan { ThoId = "t1", ChuyenKySau = -30_000 } },
                },
            },
        };

        var dong = Assert.Single(BangLuongSo.KyHienTai(so).Dongs);
        Assert.Equal(-30_000m, dong.ConLai);

        // Thợ đang cầm dư thì không phải móc ví ra trả thêm.
        Assert.Equal(0m, BangLuongSo.KyHienTai(so).TongPhaiTra);
    }

    [Fact]
    public void TinhTuBanGhi_TangLuongGiuaThang_NuaDauThangVanTinhGiaCu()
    {
        var so = new SoChamCong
        {
            Thos =
            {
                new Tho
                {
                    Id = "t1",
                    Ten = "Anh Tuấn",
                    MocLuong =
                    {
                        new MocLuong { TuNgay = "2026-01-01", TienMotCong = 300_000 },
                        new MocLuong { TuNgay = "2026-08-15", TienMotCong = 350_000 },
                    },
                },
            },
            BuoiCongs =
            {
                new BuoiCong { Id = "b1", ThoId = "t1", Ngay = "2026-08-10", Buoi = "Sang", SoCong = 1 },
                new BuoiCong { Id = "b2", ThoId = "t1", Ngay = "2026-08-20", Buoi = "Sang", SoCong = 1 },
            },
        };

        var dong = Assert.Single(BangLuongSo.TrongKhoang(so, "2026-08-01", "2026-08-31"));
        Assert.Equal(650_000m, dong.TienCong);
    }

    [Fact]
    public void TinhTuBanGhi_BuoiCoGiaRieng_ThiKhongTheoMocLuong()
    {
        var so = new SoChamCong
        {
            Thos =
            {
                new Tho
                {
                    Id = "t1",
                    Ten = "Anh Tuấn",
                    MocLuong = { new MocLuong { TuNgay = "2026-01-01", TienMotCong = 300_000 } },
                },
            },
            BuoiCongs =
            {
                new BuoiCong
                {
                    Id = "b1", ThoId = "t1", Ngay = "2026-08-10", Buoi = "Sang",
                    SoCong = 1, TienMotCong = 500_000,
                },
            },
        };

        Assert.Equal(500_000m, Assert.Single(BangLuongSo.TrongKhoang(so, "2026-08-01", "2026-08-31")).TienCong);
    }
}
