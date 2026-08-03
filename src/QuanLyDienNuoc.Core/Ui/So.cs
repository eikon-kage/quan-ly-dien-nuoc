using System.Globalization;

namespace QuanLyDienNuoc.Ui;

/// <summary>Đọc và hiển thị số theo thói quen Việt Nam: 1.500.000 hoặc 1500000 đều nhận.</summary>
public static class So
{
    public static bool TryDoc(string? chuoi, out decimal giaTri)
    {
        giaTri = 0m;
        if (string.IsNullOrWhiteSpace(chuoi))
        {
            return false;
        }

        var s = chuoi.Trim().Replace(" ", string.Empty).Replace("đ", string.Empty, StringComparison.OrdinalIgnoreCase);
        var coCham = s.Contains('.');
        var coPhay = s.Contains(',');

        if (coCham && coPhay)
        {
            // "1.500.000,5" -> chấm là phân cách nghìn, phẩy là thập phân
            s = s.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (coCham)
        {
            // "1.500.000" và "1.500" là phân cách nghìn; "1.5" là thập phân
            var phan = s.Split('.');
            var laPhanCachNghin = phan.Length > 2 || (phan.Length == 2 && phan[1].Length == 3);
            if (laPhanCachNghin)
            {
                s = s.Replace(".", string.Empty);
            }
        }
        else if (coPhay)
        {
            s = s.Replace(',', '.');
        }

        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out giaTri);
    }

    public static decimal Doc(string? chuoi) => TryDoc(chuoi, out var v) ? v : 0m;

    /// <summary>Tiền: 1500000 -> "1.500.000".</summary>
    public static string Tien(decimal giaTri) => giaTri.ToString("#,##0", CultureInfo.CurrentCulture);

    /// <summary>Số lượng: bỏ phần thập phân thừa. 2 -> "2", 2.5 -> "2,5".</summary>
    public static string Luong(decimal giaTri) => giaTri.ToString("#,##0.##", CultureInfo.CurrentCulture);
}
