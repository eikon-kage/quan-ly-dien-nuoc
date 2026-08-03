using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra dòng "Thành tiền (bằng chữ)" trên hoá đơn.</summary>
public class DocSoTests
{
    [Theory]
    [InlineData(0, "Không đồng")]
    [InlineData(1, "Một đồng")]
    [InlineData(5, "Năm đồng")]
    [InlineData(10, "Mười đồng")]
    [InlineData(11, "Mười một đồng")]          // không phải "mười mốt"
    [InlineData(14, "Mười bốn đồng")]          // không phải "mười tư"
    [InlineData(15, "Mười lăm đồng")]
    [InlineData(21, "Hai mươi mốt đồng")]
    [InlineData(24, "Hai mươi tư đồng")]
    [InlineData(25, "Hai mươi lăm đồng")]
    [InlineData(100, "Một trăm đồng")]
    [InlineData(105, "Một trăm linh năm đồng")]
    [InlineData(1000, "Một nghìn đồng")]
    [InlineData(15000, "Mười lăm nghìn đồng")]
    [InlineData(140000, "Một trăm bốn mươi nghìn đồng")]
    [InlineData(1500000, "Một triệu năm trăm nghìn đồng")]
    [InlineData(2507900, "Hai triệu năm trăm linh bảy nghìn chín trăm đồng")]
    [InlineData(1000005, "Một triệu không trăm linh năm đồng")]
    [InlineData(1000000000, "Một tỷ đồng")]
    [InlineData(-50000, "Âm năm mươi nghìn đồng")]
    public void DocTien_DocDungTiengViet(decimal soTien, string mongDoi)
    {
        Assert.Equal(mongDoi, DocSo.DocTien(soTien));
    }

    [Fact]
    public void DocTien_LamTronDenDongTruocKhiDoc()
    {
        Assert.Equal("Mười lăm nghìn đồng", DocSo.DocTien(14999.6m));
        Assert.Equal("Mười lăm nghìn đồng", DocSo.DocTien(15000.4m));
    }
}
