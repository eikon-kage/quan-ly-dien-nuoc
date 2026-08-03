using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra tìm kiếm tiếng Việt không dấu.</summary>
public class ChuVietTests
{
    [Theory]
    [InlineData("Nguyễn Văn A", "nguyen van a")]
    [InlineData("Đà Nẵng", "da nang")]
    [InlineData("Ống nhựa PVC D21", "ong nhua pvc d21")]
    [InlineData("HỒ CHÍ MINH", "ho chi minh")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void BoDau_ChuyenVeKhongDauChuThuong(string? nhap, string mongDoi)
    {
        Assert.Equal(mongDoi, ChuViet.BoDau(nhap));
    }

    [Theory]
    [InlineData("Nguyễn Văn A", "nguyen")]   // gõ không dấu vẫn tìm ra
    [InlineData("Nguyễn Văn A", "NGUYỄN")]   // gõ có dấu, khác hoa thường
    [InlineData("Ống nhựa PVC D21", "d21")]
    [InlineData("Đèn LED", "den")]
    public void Chua_TimDuocKhiGoKhongDau(string nguon, string tuKhoa)
    {
        Assert.True(ChuViet.Chua(nguon, tuKhoa));
    }

    [Fact]
    public void Chua_TraVeTrueKhiTuKhoaRong()
    {
        Assert.True(ChuViet.Chua("Nguyễn Văn A", ""));
        Assert.True(ChuViet.Chua("Nguyễn Văn A", null));
    }

    [Fact]
    public void Chua_TraVeFalseKhiKhongKhop()
    {
        Assert.False(ChuViet.Chua("Nguyễn Văn A", "tran"));
    }
}
