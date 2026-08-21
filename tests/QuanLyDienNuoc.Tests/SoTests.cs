using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra cách đọc số theo thói quen Việt Nam.</summary>
public class SoTests
{
    [Theory]
    [InlineData("15000", 15000)]
    [InlineData("15.000", 15000)]           // chấm phân cách nghìn
    [InlineData("1.500.000", 1500000)]
    [InlineData("1 500 000", 1500000)]      // có khoảng trắng
    [InlineData("150000đ", 150000)]         // có đuôi "đ"
    [InlineData("150000Đ", 150000)]
    [InlineData("2,5", 2.5)]                // phẩy là thập phân
    [InlineData("1.5", 1.5)]                // chấm là thập phân vì chỉ có 1 chữ số sau
    [InlineData("1.500.000,5", 1500000.5)]  // lẫn cả hai
    [InlineData("0", 0)]
    public void TryDoc_DocDungCacKieuGoQuenThuoc(string nhap, decimal mongDoi)
    {
        Assert.True(So.TryDoc(nhap, out var giaTri));
        Assert.Equal(mongDoi, giaTri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void TryDoc_TraVeFalseKhiKhongPhaiSo(string? nhap)
    {
        Assert.False(So.TryDoc(nhap, out var giaTri));
        Assert.Equal(0m, giaTri);
    }

    [Theory]
    [InlineData("100k", 100_000)]
    [InlineData("100K", 100_000)]
    [InlineData("1tr", 1_000_000)]
    [InlineData("1TR", 1_000_000)]
    [InlineData("1tr5", 1_500_000)]          // "một triệu năm", lối nói miệng
    [InlineData("10k5", 10_500)]
    [InlineData("1,5tr", 1_500_000)]         // thập phân trước đuôi
    [InlineData("150 nghìn", 150_000)]
    [InlineData("150 ngan", 150_000)]
    [InlineData("2 triệu", 2_000_000)]
    [InlineData("2trieu", 2_000_000)]
    [InlineData("3 củ", 3_000_000)]
    [InlineData("45.000", 45_000)]           // số thuần vẫn đọc như cũ
    [InlineData("1.500.000", 1_500_000)]
    public void TryDocTien_DocDuocTienGoTatKieuNoiMieng(string nhap, decimal mongDoi)
    {
        Assert.True(So.TryDocTien(nhap, out var giaTri));
        Assert.Equal(mongDoi, giaTri);
    }

    [Theory]
    [InlineData("1tr50")]   // hai chữ số sau đuôi: người hiểu 1.050.000, người hiểu 1.500.000
    [InlineData("k")]
    [InlineData("k5")]
    [InlineData("abc")]
    [InlineData("ống 27")]
    public void TryDocTien_KhongDoanKhiChuaChac(string nhap)
    {
        Assert.False(So.TryDocTien(nhap, out var giaTri));
        Assert.Equal(0m, giaTri);
    }

    [Fact]
    public void Doc_TraVeKhongKhiKhongDocDuoc()
    {
        Assert.Equal(0m, So.Doc("khong phai so"));
        Assert.Equal(15000m, So.Doc("15.000"));
    }
}
