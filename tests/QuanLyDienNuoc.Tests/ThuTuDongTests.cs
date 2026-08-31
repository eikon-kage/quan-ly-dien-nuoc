using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Chèn dòng vào giữa, đổi chỗ dòng, và thứ tự đó phải giữ nguyên khi in ra — bảng đi theo đúng
/// thứ tự chủ cửa hàng đã xếp, không tự xếp lại theo ngày.
/// </summary>
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
    public void KhongTuXepLaiTheoNgay_GoSaoThiNamVayVaInRaVay()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Keo", ngay: 5),
            Dong("Ống 27", ngay: 1),
            Dong("Co 90", ngay: 1),
            Dong("Băng tan", ngay: 5),
        };

        // Ngày 5/3 gõ trước ngày 1/3 thì nó vẫn nằm trước: bảng không tự xếp lại theo ngày, cũng
        // không xếp theo vần. Chủ cửa hàng gõ bù một dòng của hôm trước thì dòng ấy nằm yên chỗ
        // vừa gõ, chứ không tự nhảy lên giữa bảng.
        Assert.Equal(new[] { "Keo", "Ống 27", "Co 90", "Băng tan" }, Ten(chiTiet));

        // Tờ in ra cũng đúng thứ tự ấy.
        Assert.Equal(new[] { "Keo", "Ống 27", "Co 90", "Băng tan" }, Ten(XuatHoaDon.ChiaTrang(chiTiet)[0]));
    }

    [Fact]
    public void Chen_LenTren_NamNgayTruocDongMoc()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var moc = chiTiet[1];

        ThuTuDong.Chen(chiTiet, Dong("Van khoá"), moc.Id, chenDuoi: false);

        Assert.Equal(new[] { "Ống 27", "Van khoá", "Co 90", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void Chen_XuongDuoi_NamNgaySauDongMoc()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var moc = chiTiet[1];

        ThuTuDong.Chen(chiTiet, Dong("Van khoá"), moc.Id, chenDuoi: true);

        Assert.Equal(new[] { "Ống 27", "Co 90", "Van khoá", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void Chen_GiuNguyenNgayNguoiDungGo_VaNamDungChoDaChen()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27", ngay: 1),
            Dong("Co 90", ngay: 1),
            Dong("Keo", ngay: 9),
        };

        // Dòng mới mang ngày 20/3 mà chèn cạnh dòng ngày 1/3: ngày để nguyên như người dùng gõ,
        // dòng vẫn nằm yên đúng chỗ vừa chèn. Trước đây phần mềm sửa ngày dòng mới theo dòng mốc,
        // chỉ để nó khỏi bị phép xếp theo ngày kéo xuống cuối bảng — nay không còn phép xếp ấy.
        var dongMoi = Dong("Van khoá", ngay: 20);
        ThuTuDong.Chen(chiTiet, dongMoi, chiTiet[0].Id, chenDuoi: true);

        Assert.Equal(new DateTime(2026, 3, 20), dongMoi.Ngay);
        Assert.Equal(new[] { "Ống 27", "Van khoá", "Co 90", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void Chen_KhongCoDongMoc_ThiThemVaoCuoi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        ThuTuDong.Chen(chiTiet, Dong("Keo"), mocId: null, chenDuoi: false);

        Assert.Equal(new[] { "Ống 27", "Co 90", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void Chuyen_LenXuong_DoiChoVoiDongLienKe()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var keo = chiTiet[2];

        Assert.True(ThuTuDong.Chuyen(chiTiet, keo.Id, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Keo", "Co 90" }, Ten(chiTiet));

        Assert.True(ThuTuDong.Chuyen(chiTiet, keo.Id, xuong: true));
        Assert.Equal(new[] { "Ống 27", "Co 90", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void Chuyen_DaODauBang_ThiKhongDoiGi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        Assert.False(ThuTuDong.Chuyen(chiTiet, chiTiet[0].Id, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Co 90" }, Ten(chiTiet));
    }

    [Fact]
    public void Chuyen_DiDuocSangChoCoNgayKhac()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27", ngay: 1),
            Dong("Keo", ngay: 5),
            Dong("Băng tan", ngay: 5),
        };

        // "Keo" mang ngày 5/3, ngay trên nó là dòng ngày 1/3. Trước đây chặn không cho vượt mép
        // ngày; nay bảng không xếp theo ngày nữa nên Alt+↑ đi được khắp bảng.
        Assert.True(ThuTuDong.Chuyen(chiTiet, chiTiet[1].Id, xuong: false));
        Assert.Equal(new[] { "Keo", "Ống 27", "Băng tan" }, Ten(chiTiet));
    }

    [Fact]
    public void Chuyen_DongKhongCoTrongHoaDon_ThiKhongDoiGi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        Assert.False(ThuTuDong.Chuyen(chiTiet, Guid.NewGuid(), xuong: true));
        Assert.Equal(new[] { "Ống 27", "Co 90" }, Ten(chiTiet));
    }

    [Fact]
    public void ChuyenNhom_NhieuDongLienNhau_DiCaKhoiVaGiuNguyenThuTuTrongNhom()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27"), Dong("Co 90"), Dong("Keo"), Dong("Băng tan"),
        };
        var nhom = new[] { chiTiet[2].Id, chiTiet[3].Id };

        Assert.Equal(2, ThuTuDong.ChuyenNhom(chiTiet, nhom, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Keo", "Băng tan", "Co 90" }, Ten(chiTiet));

        Assert.Equal(2, ThuTuDong.ChuyenNhom(chiTiet, nhom, xuong: true));
        Assert.Equal(new[] { "Ống 27", "Co 90", "Keo", "Băng tan" }, Ten(chiTiet));
    }

    [Fact]
    public void ChuyenNhom_KhongTheoThuTuNguoiDungBamChon_VanDiDungCaKhoi()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27"), Dong("Co 90"), Dong("Keo"), Dong("Băng tan"),
        };

        // Ctrl+bấm "Co 90" trước rồi mới bấm "Ống 27": thứ tự chọn ngược với thứ tự trên bảng,
        // mà chuyển xuống vẫn phải đi từ dòng dưới lên. Chạy sai thứ tự là hai dòng đổi chỗ
        // qua lại rồi về đúng chỗ cũ, bấm Alt+↓ mà bảng không nhích.
        var nhom = new[] { chiTiet[1].Id, chiTiet[0].Id };

        Assert.Equal(2, ThuTuDong.ChuyenNhom(chiTiet, nhom, xuong: true));
        Assert.Equal(new[] { "Keo", "Ống 27", "Co 90", "Băng tan" }, Ten(chiTiet));
    }

    [Fact]
    public void ChuyenNhom_ChonRoiRac_DiCungMotBac()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27"), Dong("Co 90"), Dong("Keo"), Dong("Băng tan"),
        };
        var nhom = new[] { chiTiet[1].Id, chiTiet[3].Id };

        Assert.Equal(2, ThuTuDong.ChuyenNhom(chiTiet, nhom, xuong: false));
        Assert.Equal(new[] { "Co 90", "Ống 27", "Băng tan", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void ChuyenNhom_DaSatDauBang_ThiKhongDoiGi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var nhom = new[] { chiTiet[0].Id, chiTiet[1].Id };

        // Nhóm đã sát đầu bảng: báo 0 để màn hình khỏi ghi một bước hoàn tác rỗng.
        Assert.Equal(0, ThuTuDong.ChuyenNhom(chiTiet, nhom, xuong: false));
        Assert.Equal(new[] { "Ống 27", "Co 90", "Keo" }, Ten(chiTiet));
    }

    [Fact]
    public void ChuyenNhom_DiDuocSangChoCoNgayKhac()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            Dong("Ống 27", ngay: 1),
            Dong("Keo", ngay: 5),
            Dong("Băng tan", ngay: 5),
        };
        var nhom = new[] { chiTiet[1].Id, chiTiet[2].Id };

        // Cả nhóm mang ngày 5/3, ngay trên là dòng ngày 1/3: nay cả khối vượt qua được.
        Assert.Equal(2, ThuTuDong.ChuyenNhom(chiTiet, nhom, xuong: false));
        Assert.Equal(new[] { "Keo", "Băng tan", "Ống 27" }, Ten(chiTiet));
    }

    [Fact]
    public void ViTriChen_DongTrongDungCanhDongMoc()
    {
        // Ctrl+Enter mở một dòng trống ngay cạnh dòng đang chọn. Chỗ đặt nó trên lưới tính theo
        // thứ tự đang hiện, chính là hàm này.
        var thuTu = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };

        Assert.Equal(1, ThuTuDong.ViTriChen(thuTu, thuTu[1].Id, chenDuoi: false));
        Assert.Equal(2, ThuTuDong.ViTriChen(thuTu, thuTu[1].Id, chenDuoi: true));
    }

    [Fact]
    public void ViTriChen_KhongCoMocHoacMocDaBiXoa_ThiDongTrongVeCuoiLuoi()
    {
        var thuTu = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90") };

        // Chưa chèn gì thì dòng trống nằm cuối lưới như thường.
        Assert.Equal(2, ThuTuDong.ViTriChen(thuTu, mocId: null, chenDuoi: false));

        // Xoá mất dòng mốc (hoặc hoàn tác, đổi hoá đơn) thì cũng về cuối, không treo lơ lửng.
        Assert.Equal(2, ThuTuDong.ViTriChen(thuTu, Guid.NewGuid(), chenDuoi: true));
    }

    [Fact]
    public void Chen_LenTrenNhieuLan_GiuNguyenMoc_ThiRaDungThuTuNguoiDungGo()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var moc = chiTiet[1];

        // Gõ liền hai dòng ở chỗ chèn: chèn lên trên thì cứ giữ nguyên mốc là đã đúng thứ tự gõ.
        ThuTuDong.Chen(chiTiet, Dong("Van khoá"), moc.Id, chenDuoi: false);
        ThuTuDong.Chen(chiTiet, Dong("Băng tan"), moc.Id, chenDuoi: false);

        Assert.Equal(
            new[] { "Ống 27", "Van khoá", "Băng tan", "Co 90", "Keo" },
            Ten(chiTiet));
    }

    [Fact]
    public void Chen_XuongDuoiNhieuLan_PhaiDoiMocSangDongVuaGhi()
    {
        var chiTiet = new List<ChiTietHoaDon> { Dong("Ống 27"), Dong("Co 90"), Dong("Keo") };
        var moc = chiTiet[1];

        var dongDau = Dong("Van khoá");
        ThuTuDong.Chen(chiTiet, dongDau, moc.Id, chenDuoi: true);

        // Chèn xuống dưới mà vẫn lấy mốc cũ thì dòng gõ sau chen lên trước dòng gõ trước, gõ mấy
        // dòng liền nhau là ra thứ tự ngược. Mốc phải chuyển sang dòng vừa ghi.
        ThuTuDong.Chen(chiTiet, Dong("Băng tan"), dongDau.Id, chenDuoi: true);

        Assert.Equal(
            new[] { "Ống 27", "Co 90", "Van khoá", "Băng tan", "Keo" },
            Ten(chiTiet));
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
