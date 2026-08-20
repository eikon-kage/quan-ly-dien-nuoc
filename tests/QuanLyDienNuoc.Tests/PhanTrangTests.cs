using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Kiểm tra phép chia trang. Chỗ dễ sai lặng lẽ nhất là **trang đang xem vượt quá cuối sổ**:
/// xoá bớt dòng hay lọc hẹp lại thì phải lùi về trang cuối, chứ hiện ra bảng trống là người
/// dùng tưởng mất dữ liệu.
/// </summary>
public class PhanTrangTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(30, 1)]
    [InlineData(31, 2)]
    [InlineData(60, 2)]
    [InlineData(61, 3)]
    [InlineData(196, 7)]
    public void SoTrang_ChiaTronVaChiaLe(int tongDong, int mong)
    {
        Assert.Equal(mong, PhanTrang.SoTrang(tongDong));
    }

    [Fact]
    public void Cat_LayDungDoanCuaTrang()
    {
        var tatCa = Enumerable.Range(1, 70).ToList();

        Assert.Equal(Enumerable.Range(1, 30), PhanTrang.Cat(tatCa, 0));
        Assert.Equal(Enumerable.Range(31, 30), PhanTrang.Cat(tatCa, 1));

        // Trang cuối chỉ còn 10 dòng.
        Assert.Equal(Enumerable.Range(61, 10), PhanTrang.Cat(tatCa, 2));
    }

    [Fact]
    public void Cat_TrangVuotQuaCuoiSo_ThiLuiVeTrangCuoi()
    {
        var tatCa = Enumerable.Range(1, 35).ToList();

        Assert.Equal(Enumerable.Range(31, 5), PhanTrang.Cat(tatCa, 9));
        Assert.Equal(1, PhanTrang.TrangHopLe(9, 35));
    }

    [Fact]
    public void Cat_SoTrong_ThiRaRong_ChuKhongNem()
    {
        Assert.Empty(PhanTrang.Cat(new List<int>(), 3));
        Assert.Equal(0, PhanTrang.TrangHopLe(3, 0));
    }

    [Fact]
    public void MoTa_NoiDungDungSoDongDangHien()
    {
        Assert.Equal("Chưa có dòng nào", PhanTrang.MoTa(0, 0));

        // Vừa một trang thì đừng bắt người dùng đọc "trang 1/1".
        Assert.Equal("12 dòng", PhanTrang.MoTa(0, 12));

        Assert.Equal("Trang 1/7   ·   dòng 1–30 trong 196", PhanTrang.MoTa(0, 196));
        Assert.Equal("Trang 2/7   ·   dòng 31–60 trong 196", PhanTrang.MoTa(1, 196));
        Assert.Equal("Trang 7/7   ·   dòng 181–196 trong 196", PhanTrang.MoTa(6, 196));
    }

    [Fact]
    public void MoTa_TrangVuotQua_ThiNoiTheoTrangCuoi()
    {
        Assert.Equal("Trang 7/7   ·   dòng 181–196 trong 196", PhanTrang.MoTa(99, 196));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(29, 0)]
    [InlineData(30, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 2)]
    [InlineData(-1, 0)]
    public void TrangCuaDong_NhayVeDungTrangChuaDongDo(int viTri, int mong)
    {
        Assert.Equal(mong, PhanTrang.TrangCuaDong(viTri));
    }
}
