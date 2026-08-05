using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Chèn dòng vào giữa, đổi chỗ dòng, và thứ tự đó phải giữ nguyên khi in ra.</summary>
public class ThuTuDongTests
{
    private static ChiTietHoaDon Dong(string ten, int ngay = 1) => new()
    {
        Ngay = new DateTime(2026, 3, ngay),
        TenHang = ten,
        DonVi = "Cái",
        DonGia = 10_000,
        SoLuong = 1,
    };

    private static List<string> Ten(IEnumerable<ChiTietHoaDon> dong) => dong.Select(d => d.TenHang).ToList();

    [Fact]
    public void TheoThuTu_XepTheoNgay_TrongCungNgayGiuNguyenThuTuNguoiDungXep()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Keo", ngay: 5),
            Dong("Ống 27", ngay: 1),
            Dong("Co 90", ngay: 1),
            Dong("Băng tan", ngay: 5),
        };

        // Không xếp theo vần: trong cùng một ngày thì "Ống 27" vẫn đứng trước "Co 90".
        Assert.Equal(
            new[] { "Ống 27", "Co 90", "Keo", "Băng tan" },
            Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chen_LenTren_NamNgayTruocDongMoc()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var moc = chiTiet[1];

        ThuTuDong.Chen(chiTiet, Dong("Van khoá"), moc.Id, chenDuoi: false);

        Assert.Equal(new[] { "Ống 27", "Van khoá", "Co 90", "Keo" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chen_XuongDuoi_NamNgaySauDongMoc()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var moc = chiTiet[1];

        ThuTuDong.Chen(chiTiet, Dong("Van khoá"), moc.Id, chenDuoi: true);

        Assert.Equal(new[] { "Ống 27", "Co 90", "Van khoá", "Keo" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chen_LayNgayCuaDongMoc_NenDungYenDungCho()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27", ngay: 1),
            Dong("Co 90", ngay: 1),
            Dong("Keo", ngay: 9),
        };

        // Dòng mới mang ngày 20/3 nhưng chèn cạnh dòng ngày 1/3 thì phải theo ngày của dòng mốc,
        // không thì xếp lại theo ngày là nó nhảy xuống tận cuối bảng.
        var dongMoi = Dong("Van khoá", ngay: 20);
        ThuTuDong.Chen(chiTiet, dongMoi, chiTiet[0].Id, chenDuoi: true);

        Assert.Equal(new DateTime(2026, 3, 1), dongMoi.Ngay);
        Assert.Equal(new[] { "Ống 27", "Van khoá", "Co 90", "Keo" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chen_KhongCoDongMoc_ThiThemVaoCuoi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        ThuTuDong.Chen(chiTiet, Dong("Keo"), mocId: null, chenDuoi: false);

        Assert.Equal(new[] { "Ống 27", "Co 90", "Keo" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chuyen_LenXuong_DoiChoVoiDongLienKe()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var keo = chiTiet[2];

        Assert.True(ThuTuDong.Chuyen(chiTiet, keo.Id, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Keo", "Co 90" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));

        Assert.True(ThuTuDong.Chuyen(chiTiet, keo.Id, xuong: true));
        Assert.Equal(new[] { "Ống 27", "Co 90", "Keo" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chuyen_DaODauNgay_ThiKhongDoiGi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        Assert.False(ThuTuDong.Chuyen(chiTiet, chiTiet[0].Id, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Co 90" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chuyen_KhongVuotSangNgayKhac()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27", ngay: 1),
            Dong("Keo", ngay: 5),
            Dong("Băng tan", ngay: 5),
        };

        // "Keo" là dòng đầu của ngày 5/3, chuyển lên nữa là lấn sang ngày 1/3 nên phải chặn.
        Assert.False(ThuTuDong.Chuyen(chiTiet, chiTiet[1].Id, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Keo", "Băng tan" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));

        // Còn "Băng tan" thì đổi chỗ được với "Keo" vì cùng ngày.
        Assert.True(ThuTuDong.Chuyen(chiTiet, chiTiet.First(c => c.TenHang == "Băng tan").Id, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Băng tan", "Keo" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void Chuyen_DongKhongCoTrongHoaDon_ThiKhongDoiGi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        Assert.False(ThuTuDong.Chuyen(chiTiet, Guid.NewGuid(), xuong: true));
        Assert.Equal(new[] { "Ống 27", "Co 90" }, Ten(ThuTuDong.TheoThuTu(chiTiet)));
    }

    [Fact]
    public void ChiaTrang_InRaGiayDungThuTuDaXepTrenLuoi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Keo"), Dong("Co 90") };
        ThuTuDong.Chen(chiTiet, Dong("Van khoá"), chiTiet[0].Id, chenDuoi: true);

        var trang = XuatHoaDon.ChiaTrang(chiTiet);

        Assert.Equal(new[] { "Ống 27", "Van khoá", "Keo", "Co 90" }, Ten(trang[0]));
    }
}
