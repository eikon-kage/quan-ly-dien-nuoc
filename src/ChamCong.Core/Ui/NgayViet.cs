namespace ChamCong.Ui;

/// <summary>
/// Viết ngày theo kiểu người Việt đọc. Không dùng định dạng "dddd" của .NET vì kết quả
/// đổi theo ngôn ngữ đang cài trên máy — điện thoại để tiếng Anh sẽ ra "Monday".
/// </summary>
public static class NgayViet
{
    private static readonly string[] TenThu =
    {
        "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy",
    };

    public static string Thu(DateTime ngay) => TenThu[(int)ngay.DayOfWeek];

    /// <summary>Kiểu hiện trên đầu màn hình chấm công: "Thứ Hai 03/08".</summary>
    public static string ThuVaNgay(DateTime ngay) => $"{Thu(ngay)} {ngay:dd/MM}";

    /// <summary>Số công viết gọn: 1 công ra "1", nửa công ra "0,5".</summary>
    public static string SoCong(decimal soCong) =>
        soCong.ToString("0.#").Replace('.', ',');

    /// <summary>Tiền viết đủ chữ số kèm đơn vị: "1.500.000 đ". Số âm giữ dấu trừ.</summary>
    public static string Tien(decimal soTien)
    {
        var am = soTien < 0m;
        var chu = Math.Abs(soTien).ToString("#,##0").Replace(',', '.');
        return am ? $"-{chu} đ" : $"{chu} đ";
    }
}
