using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra cách đọc ngày người dùng gõ vào ô chọn ngày.</summary>
public class NgayVietTests
{
    /// <summary>Ngày đang chọn lúc gõ — chỗ lấy năm và tháng khi người ta gõ tắt.</summary>
    private static readonly DateTime Moc = new(2026, 8, 15);

    [Theory]
    [InlineData("3/8", 2026, 8, 3)]              // thiếu năm: lấy năm đang chọn
    [InlineData("03/08", 2026, 8, 3)]
    [InlineData("3/8/26", 2026, 8, 3)]           // năm hai chữ số
    [InlineData("3/8/2026", 2026, 8, 3)]
    [InlineData("3-8-2025", 2025, 8, 3)]         // gạch ngang
    [InlineData("3.8.2025", 2025, 8, 3)]         // dấu chấm
    [InlineData("3 8 2025", 2025, 8, 3)]         // dấu cách
    [InlineData(@"3\8", 2026, 8, 3)]             // gạch chéo ngược như lối viết tay
    [InlineData("  31/12/2025  ", 2025, 12, 31)] // thừa khoảng trắng
    [InlineData("3108", 2026, 8, 31)]            // gõ liền, không năm
    [InlineData("310825", 2025, 8, 31)]          // gõ liền, năm hai chữ số
    [InlineData("31082025", 2025, 8, 31)]        // gõ liền đủ tám số
    [InlineData("7", 2026, 8, 7)]                // mỗi con số: ngày trong tháng đang chọn
    [InlineData("29/2/2028", 2028, 2, 29)]       // ngày nhuận có thật
    public void TryDoc_NhanMoiLoiGoQuenThuoc(string nhap, int nam, int thang, int ngay)
    {
        Assert.True(NgayViet.TryDoc(nhap, Moc, out var doc));
        Assert.Equal(new DateTime(nam, thang, ngay), doc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("linh tinh")]
    [InlineData("32/8/2026")]     // không có ngày 32
    [InlineData("31/2/2026")]     // tháng hai không có ngày 31
    [InlineData("29/2/2026")]     // 2026 không nhuận
    [InlineData("3/13/2026")]     // không có tháng 13
    [InlineData("0/8/2026")]
    [InlineData("3/0/2026")]
    public void TryDoc_GoSai_ThiBaoSaiChuKhongDoanBua(string nhap)
    {
        // Trả về false và giữ nguyên ngày đang chọn: đoán bừa (31/2 thành 3/3 chẳng hạn) thì
        // chủ cửa hàng không nhận ra mình gõ hụt, hàng vào sổ sai ngày.
        Assert.False(NgayViet.TryDoc(nhap, Moc, out var doc));
        Assert.Equal(Moc, doc);
    }

    [Fact]
    public void Viet_RoiDocLai_RaDungNgayCu()
    {
        var ngay = new DateTime(2026, 3, 1);

        Assert.Equal("01/03/2026", NgayViet.Viet(ngay));
        Assert.True(NgayViet.TryDoc(NgayViet.Viet(ngay), Moc, out var doc));
        Assert.Equal(ngay, doc);
    }
}
