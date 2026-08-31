using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Chia bảng kê dài thành nhiều tấm ảnh. Hai chỗ dễ sai: **trang cao quá** (Zalo nén ảnh dài
/// thành một vệt chữ không đọc được) và **dòng mã tờ đứng một mình ở cuối trang**.
/// </summary>
public class ChiaTrangAnhTests
{
    private static List<KhoiAnh> Hang(int soDong, int cao = 40) =>
        Enumerable.Range(0, soDong).Select(_ => new KhoiAnh(cao, false)).ToList();

    [Fact]
    public void Chia_VuaMotTrangThiKhongChia()
    {
        var trang = ChiaTrangAnh.Chia(Hang(10), choTrong: 1000);

        Assert.Single(trang);
        Assert.Equal(Enumerable.Range(0, 10), trang[0]);
    }

    [Fact]
    public void Chia_KhongTrangNaoVuotChoTrong()
    {
        // 40 dòng cao 40 px = 1600 px, chỗ trống 500 px → mỗi trang 12 dòng.
        var trang = ChiaTrangAnh.Chia(Hang(40), choTrong: 500);

        Assert.Equal(4, trang.Count);
        Assert.Equal(new[] { 12, 12, 12, 4 }, trang.Select(t => t.Count));
        Assert.All(trang, t => Assert.True(t.Count * 40 <= 500));
    }

    [Fact]
    public void Chia_KhongMatDongNaoVaGiuNguyenThuTu()
    {
        var trang = ChiaTrangAnh.Chia(Hang(37), choTrong: 300);

        Assert.Equal(Enumerable.Range(0, 37), trang.SelectMany(t => t));
    }

    [Fact]
    public void Chia_SoTrongThiKhongCoTrangNao()
    {
        Assert.Empty(ChiaTrangAnh.Chia(new List<KhoiAnh>(), choTrong: 500));
    }

    [Fact]
    public void Chia_MotDongCaoHonCaTrangThiVanDatChuKhongBoQua()
    {
        var khoi = new List<KhoiAnh> { new(40, false), new(900, false), new(40, false) };

        var trang = ChiaTrangAnh.Chia(khoi, choTrong: 300);

        Assert.Equal(new[] { 1, 1, 1 }, trang.Select(t => t.Count));
        Assert.Equal(Enumerable.Range(0, 3), trang.SelectMany(t => t));
    }

    [Fact]
    public void Chia_DongMaToKhongDungMotMinhOCuoiTrang()
    {
        // Chỗ trống vừa đúng hai dòng hàng: dòng mã tờ ở giữa phải đẩy sang trang sau cùng với
        // dòng hàng đầu của tờ ấy.
        var khoi = new List<KhoiAnh>
        {
            new(40, false),
            new(40, false),
            new(34, true),
            new(40, false),
        };

        var trang = ChiaTrangAnh.Chia(khoi, choTrong: 114, caoNhomTiep: 34);

        Assert.Equal(2, trang.Count);
        Assert.Equal(new[] { 0, 1 }, trang[0]);
        Assert.Equal(new[] { 2, 3 }, trang[1]);
    }

    [Fact]
    public void Chia_MaToLaKhoiCuoiCungThiVanDatDuoc()
    {
        // Tờ hoàn hàng rỗng (không dòng nào) không xảy ra trong sổ, nhưng vào đây thì cũng
        // không được treo hàm.
        var khoi = new List<KhoiAnh> { new(40, false), new(34, true) };

        var trang = ChiaTrangAnh.Chia(khoi, choTrong: 100, caoNhomTiep: 34);

        Assert.Single(trang);
        Assert.Equal(new[] { 0, 1 }, trang[0]);
    }

    [Fact]
    public void Chia_TrangSauChuaChoGhiLaiMaTo()
    {
        // Mã tờ ở khối 0, sau đó 6 dòng hàng cao 40. Chỗ trống 200 px: trang đầu chứa mã tờ
        // (34) + 4 dòng = 194. Trang sau phải chừa 34 px ghi lại mã tờ nên chỉ còn 4 dòng.
        var khoi = new List<KhoiAnh> { new(34, true) };
        khoi.AddRange(Hang(10));

        var trang = ChiaTrangAnh.Chia(khoi, choTrong: 200, caoNhomTiep: 34);

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, trang[0]);
        Assert.Equal(new[] { 5, 6, 7, 8 }, trang[1]);
        Assert.Equal(new[] { 9, 10 }, trang[2]);
    }

    [Fact]
    public void Chia_KhongGhiMaToThiTrangSauKhongChuaChoThua()
    {
        var trang = ChiaTrangAnh.Chia(Hang(10), choTrong: 200, caoNhomTiep: 0);

        Assert.Equal(new[] { 5, 5 }, trang.Select(t => t.Count));
    }
}
