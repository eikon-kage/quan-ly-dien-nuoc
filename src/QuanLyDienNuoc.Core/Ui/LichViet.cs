using System.Globalization;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Cách bày một tháng lên tờ lịch và tên gọi tiếng Việt của thứ, của tháng.
/// <para>
/// Phần mềm tự vẽ lấy bảng lịch chứ không dùng bảng lịch của Windows: bảng ấy lấy tên tháng và
/// tên thứ theo <b>cài đặt Region của máy</b>, không theo ngôn ngữ phần mềm đặt, nên máy cài
/// Windows tiếng Anh thì chủ cửa hàng thấy "August 2026 — S M T W T F S".
/// </para>
/// <para>
/// Để ở đây (không nằm trong phần giao diện) để chạy được test trên máy không có Windows.
/// </para>
/// </summary>
public static class LichViet
{
    /// <summary>Tờ lịch luôn 6 hàng, kể cả tháng ngắn — bảng không nhảy cao thấp khi lật tháng.</summary>
    public const int SoHang = 6;

    public const int SoCot = 7;

    public const int SoO = SoHang * SoCot;

    /// <summary>Tên cột, bắt đầu từ thứ hai như lịch treo tường của ta chứ không phải chủ nhật.</summary>
    public static readonly string[] TenThu = { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

    /// <summary>Chữ trên đầu bảng lịch: "Tháng 8, 2026".</summary>
    public static string TieuDeThang(DateTime thang) => $"Tháng {thang.Month}, {thang.Year}";

    /// <summary>Ngày viết kiểu Việt Nam, không phụ thuộc cài đặt của máy.</summary>
    public static string ChuNgay(DateTime ngay) => ngay.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>Thứ của một ngày: "Thứ hai" … "Chủ nhật".</summary>
    public static string TenThuDayDu(DateTime ngay) => ngay.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ hai",
        DayOfWeek.Tuesday => "Thứ ba",
        DayOfWeek.Wednesday => "Thứ tư",
        DayOfWeek.Thursday => "Thứ năm",
        DayOfWeek.Friday => "Thứ sáu",
        DayOfWeek.Saturday => "Thứ bảy",
        _ => "Chủ nhật",
    };

    /// <summary>"Thứ hai, 31/08/2026" — dòng chân bảng lịch và lời mách khi trỏ vào ô ngày.</summary>
    public static string ThuVaNgay(DateTime ngay) => $"{TenThuDayDu(ngay)}, {ChuNgay(ngay)}";

    /// <summary>Cột của một ngày trên tờ lịch: thứ hai là 0, chủ nhật là 6.</summary>
    public static int Cot(DateTime ngay) => ((int)ngay.DayOfWeek + 6) % 7;

    /// <summary>Thứ hai của tuần chứa ngày này.</summary>
    public static DateTime DauTuan(DateTime ngay) => ngay.Date.AddDays(-Cot(ngay));

    /// <summary>
    /// 42 ô của tờ lịch tháng, xếp theo hàng từ trái sang phải. Ô đầu là thứ hai của tuần chứa
    /// ngày mùng một, nên đầu và cuối tờ có vài ngày của tháng bên cạnh — bảng vẽ chúng mờ đi.
    /// </summary>
    public static List<DateTime> Luoi(DateTime thang)
    {
        var dau = DauTuan(new DateTime(thang.Year, thang.Month, 1));
        return Enumerable.Range(0, SoO).Select(i => dau.AddDays(i)).ToList();
    }

    /// <summary>Ngày này có thuộc tháng đang xem không — ô của tháng bên cạnh thì vẽ mờ.</summary>
    public static bool TrongThang(DateTime ngay, DateTime thang) =>
        ngay.Year == thang.Year && ngay.Month == thang.Month;

    /// <summary>
    /// Lùi/tiến tháng mà giữ nguyên ngày trong tháng, ngày 31 sang tháng chỉ có 30 thì lùi về
    /// ngày cuối tháng. Dùng cho nút lật tháng và lật năm của bảng lịch.
    /// </summary>
    public static DateTime DoiThang(DateTime moc, int soThang)
    {
        var dich = new DateTime(moc.Year, moc.Month, 1).AddMonths(soThang);
        return new DateTime(dich.Year, dich.Month, Math.Min(moc.Day, DateTime.DaysInMonth(dich.Year, dich.Month)));
    }
}
