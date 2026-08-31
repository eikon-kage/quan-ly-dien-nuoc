using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra tờ lịch tiếng Việt mà bảng chọn ngày tự vẽ.</summary>
public class LichVietTests
{
    [Fact]
    public void TenCot_BatDauTuThuHaiNhuLichTreoTuong()
    {
        Assert.Equal(new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" }, LichViet.TenThu);
    }

    [Theory]
    [InlineData(2026, 8, 31, "Thứ hai")]
    [InlineData(2026, 8, 30, "Chủ nhật")]
    [InlineData(2026, 8, 29, "Thứ bảy")]
    [InlineData(2026, 3, 1, "Chủ nhật")]
    public void TenThuDayDu_GoiTheoLoiNoiTiengViet(int nam, int thang, int ngay, string mongDoi)
    {
        Assert.Equal(mongDoi, LichViet.TenThuDayDu(new DateTime(nam, thang, ngay)));
    }

    [Fact]
    public void ChuNgay_LuonKieuVietDuKhiMayCaiWindowsTiengAnh()
    {
        // Không đi qua CultureInfo của máy: máy đặt Region kiểu Mỹ vẫn phải ra 03/08/2026,
        // chứ ra 8/3/2026 là chủ cửa hàng đọc nhầm sang ngày 8 tháng 3.
        Assert.Equal("03/08/2026", LichViet.ChuNgay(new DateTime(2026, 8, 3)));
        Assert.Equal("Thứ hai, 31/08/2026", LichViet.ThuVaNgay(new DateTime(2026, 8, 31)));
    }

    [Fact]
    public void TieuDeThang_VietBangTiengViet()
    {
        Assert.Equal("Tháng 8, 2026", LichViet.TieuDeThang(new DateTime(2026, 8, 31)));
    }

    [Theory]
    [InlineData(2026, 8)]    // mùng 1 rơi vào thứ bảy
    [InlineData(2026, 3)]    // mùng 1 rơi vào chủ nhật — tháng "lệch" nhất
    [InlineData(2026, 2)]    // tháng ngắn
    [InlineData(2028, 2)]    // tháng hai năm nhuận
    [InlineData(2027, 11)]   // mùng 1 rơi đúng thứ hai
    public void Luoi_LuonDu42ODuTaiThangNao(int nam, int thang)
    {
        var moc = new DateTime(nam, thang, 1);
        var luoi = LichViet.Luoi(moc);

        // Đủ 6 hàng thì bảng không nhảy cao thấp lúc lật tháng.
        Assert.Equal(42, luoi.Count);

        // Ô đầu luôn là thứ hai, các ô liền nhau cách đúng một ngày.
        Assert.Equal(DayOfWeek.Monday, luoi[0].DayOfWeek);
        Assert.All(
            Enumerable.Range(1, luoi.Count - 1),
            i => Assert.Equal(luoi[i - 1].AddDays(1), luoi[i]));

        // Và chứa trọn vẹn mọi ngày của tháng đang xem.
        var trongThang = luoi.Where(n => LichViet.TrongThang(n, moc)).ToList();
        Assert.Equal(DateTime.DaysInMonth(nam, thang), trongThang.Count);
        Assert.Equal(1, trongThang[0].Day);
        Assert.Equal(DateTime.DaysInMonth(nam, thang), trongThang[^1].Day);
    }

    [Fact]
    public void Luoi_ThangMungMotLaChuNhat_CoDuTuanTruocODau()
    {
        // 1/3/2026 là chủ nhật: tờ lịch phải mở bằng thứ hai 23/2, không phải mùng 1 nằm lẻ loi
        // ở cột đầu — xếp sai chỗ này là bấm ngày nào cũng lệch một cột.
        var luoi = LichViet.Luoi(new DateTime(2026, 3, 15));

        Assert.Equal(new DateTime(2026, 2, 23), luoi[0]);
        Assert.Equal(6, LichViet.Cot(new DateTime(2026, 3, 1)));
        Assert.Equal(new DateTime(2026, 3, 1), luoi[6]);
    }

    [Theory]
    [InlineData(2026, 8, 31, -1, 2026, 7, 31)]   // tháng 7 cũng có 31 ngày, giữ nguyên ngày
    [InlineData(2026, 8, 31, 1, 2026, 9, 30)]    // sang tháng chỉ có 30 ngày thì lùi về ngày cuối
    [InlineData(2026, 1, 15, -1, 2025, 12, 15)]  // lùi qua năm
    [InlineData(2026, 12, 15, 1, 2027, 1, 15)]   // tiến qua năm
    [InlineData(2028, 2, 29, 12, 2029, 2, 28)]   // 29/2 năm nhuận sang năm thường
    public void DoiThang_GiuNgayTrongThang_KhongTranSangThangSau(
        int nam, int thang, int ngay, int soThang, int namRa, int thangRa, int ngayRa)
    {
        Assert.Equal(
            new DateTime(namRa, thangRa, ngayRa),
            LichViet.DoiThang(new DateTime(nam, thang, ngay), soThang));
    }
}
