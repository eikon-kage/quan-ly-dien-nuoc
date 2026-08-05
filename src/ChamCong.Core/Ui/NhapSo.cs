namespace ChamCong.Ui;

/// <summary>Đọc số tiền người dùng gõ vào. Gõ kiểu gì cũng hiểu, miễn là có chữ số.</summary>
public static class NhapSo
{
    /// <summary>
    /// Đọc "300.000", "300000", "300 000" hay "300,000" đều ra 300000.
    /// Không có chữ số nào thì trả về null.
    /// </summary>
    public static decimal? DocTien(string? chu)
    {
        if (string.IsNullOrWhiteSpace(chu))
        {
            return null;
        }

        var so = new string(chu.Where(char.IsDigit).ToArray());
        if (so.Length == 0)
        {
            return null;
        }

        return decimal.TryParse(so, out var ketQua) ? ketQua : null;
    }
}
