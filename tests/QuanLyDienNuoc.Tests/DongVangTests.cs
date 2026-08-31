using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Cắm dòng vàng (dòng gõ dở) vào trang đang xem. Từ khi Ctrl+Enter mở **thêm** một dòng vàng
/// giữa bảng mà vẫn giữ dòng vàng ở cuối lưới, một trang có thể phải cắm hai dòng một lượt —
/// cắm sai thứ tự là dòng nọ đẩy lệch chỗ dòng kia.
/// </summary>
public class DongVangTests
{
    private static List<string> Trang(int soDong) =>
        Enumerable.Range(1, soDong).Select(i => $"hàng{i}").ToList();

    [Fact]
    public void Cam_MotDongVangOCuoiSo()
    {
        var trang = Trang(3);

        DongVang.Cam(trang, trangDangXem: 0, new[] { (3, "vàng-cuối") });

        Assert.Equal(new[] { "hàng1", "hàng2", "hàng3", "vàng-cuối" }, trang);
    }

    [Fact]
    public void Cam_DongChenNamDungTrenDongMoc()
    {
        var trang = Trang(4);

        // Chèn lên trên hàng3 (chỗ số 2) và dòng vàng cuối lưới ở chỗ số 4.
        DongVang.Cam(trang, 0, new[] { (4, "vàng-cuối"), (2, "vàng-chèn") });

        Assert.Equal(
            new[] { "hàng1", "hàng2", "vàng-chèn", "hàng3", "hàng4", "vàng-cuối" },
            trang);
    }

    [Fact]
    public void Cam_HaiDongVangTrungCho_ThiDongChenNamTren()
    {
        var trang = Trang(2);

        // Ô chèn vừa mất mốc (dòng mốc bị xoá) nên cũng về cuối sổ: nó phải đứng trên ô cuối,
        // đúng thứ tự người dùng gõ.
        DongVang.Cam(trang, 0, new[] { (2, "vàng-cuối"), (2, "vàng-chèn") });

        Assert.Equal(new[] { "hàng1", "hàng2", "vàng-chèn", "vàng-cuối" }, trang);
    }

    [Fact]
    public void Cam_DongVangCuaTrangKhac_ThiKhongCam()
    {
        // Sổ 35 dòng, đang xem trang 2 (5 dòng cuối). Ô chèn ở chỗ số 2 thuộc trang 1.
        var trang = Trang(5);

        DongVang.Cam(trang, trangDangXem: 1, new[] { (35, "vàng-cuối"), (2, "vàng-chèn") });

        Assert.Equal(new[] { "hàng1", "hàng2", "hàng3", "hàng4", "hàng5", "vàng-cuối" }, trang);
    }

    [Fact]
    public void Cam_TrangVuaTronThiDongVangDungLuonCuoiTrangDo()
    {
        // Sổ tròn 30 dòng: dòng vàng ở chỗ số 30, tức ngay sau dòng cuối trang 1 — vẫn cắm vào
        // trang ấy chứ không đẻ thêm một trang chỉ để chứa mỗi nó.
        var trang = Trang(30);

        DongVang.Cam(trang, 0, new[] { (30, "vàng-cuối") });

        Assert.Equal(31, trang.Count);
        Assert.Equal("vàng-cuối", trang[^1]);
    }

    [Fact]
    public void Cam_ChoCamAm_ThiBoQua()
    {
        var trang = Trang(2);

        DongVang.Cam(trang, 0, new[] { (-1, "chưa có chỗ") });

        Assert.Equal(new[] { "hàng1", "hàng2" }, trang);
    }

    [Fact]
    public void Cam_KhongCoDongVangNao_ThiTrangGiuNguyen()
    {
        var trang = Trang(3);

        DongVang.Cam(trang, 0, Array.Empty<(int, string)>());

        Assert.Equal(Trang(3), trang);
    }
}
