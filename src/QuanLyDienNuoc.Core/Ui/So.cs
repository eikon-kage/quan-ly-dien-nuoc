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

    /// <summary>
    /// Như <see cref="TryDoc"/> nhưng gõ được cả phép tính ngay trong ô: "3+2*4" ra 11,
    /// "1,2+0,8" ra 2, "2x3" ra 6. Thợ hay cộng nhiều đoạn ống nên khỏi phải bấm máy tính riêng.
    /// Dấu bằng ở đầu như trong Excel cũng chấp nhận: "=5+5".
    /// </summary>
    public static bool TryTinh(string? chuoi, out decimal giaTri)
    {
        giaTri = 0m;
        if (string.IsNullOrWhiteSpace(chuoi))
        {
            return false;
        }

        var s = chuoi.Trim();
        if (s.StartsWith('='))
        {
            s = s[1..];
        }

        // Số thuần đọc theo kiểu Việt Nam trước ("1.500" là một nghìn rưỡi, không phải 1,5).
        if (TryDoc(s, out giaTri))
        {
            return true;
        }

        var viTri = 0;
        if (!DocBieuThuc(s, ref viTri, out giaTri))
        {
            giaTri = 0m;
            return false;
        }

        BoTrang(s, ref viTri);
        if (viTri < s.Length)
        {
            giaTri = 0m;
            return false;
        }

        return true;
    }

    /// <summary>Tính biểu thức, không hợp lệ thì trả về 0.</summary>
    public static decimal Tinh(string? chuoi) => TryTinh(chuoi, out var v) ? v : 0m;

    /// <summary>Tiền: 1500000 -> "1.500.000".</summary>
    public static string Tien(decimal giaTri) => giaTri.ToString("#,##0", CultureInfo.CurrentCulture);

    /// <summary>Số lượng: bỏ phần thập phân thừa. 2 -> "2", 2.5 -> "2,5".</summary>
    public static string Luong(decimal giaTri) => giaTri.ToString("#,##0.##", CultureInfo.CurrentCulture);

    // ---------- Bộ tính biểu thức: cộng trừ trước, nhân chia sau, có ngoặc ----------

    private static void BoTrang(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i]))
        {
            i++;
        }
    }

    private static bool DocBieuThuc(string s, ref int i, out decimal giaTri)
    {
        if (!DocHang(s, ref i, out giaTri))
        {
            return false;
        }

        while (true)
        {
            BoTrang(s, ref i);
            if (i >= s.Length || (s[i] != '+' && s[i] != '-'))
            {
                return true;
            }

            var dauCong = s[i] == '+';
            i++;
            if (!DocHang(s, ref i, out var ve))
            {
                return false;
            }

            giaTri = dauCong ? giaTri + ve : giaTri - ve;
        }
    }

    private static bool DocHang(string s, ref int i, out decimal giaTri)
    {
        if (!DocThuaSo(s, ref i, out giaTri))
        {
            return false;
        }

        while (true)
        {
            BoTrang(s, ref i);
            if (i >= s.Length)
            {
                return true;
            }

            var dau = char.ToLowerInvariant(s[i]);
            if (dau != '*' && dau != '/' && dau != 'x' && dau != '×' && dau != ':')
            {
                return true;
            }

            i++;
            if (!DocThuaSo(s, ref i, out var ve))
            {
                return false;
            }

            if (dau == '*' || dau == 'x' || dau == '×')
            {
                giaTri *= ve;
            }
            else
            {
                if (ve == 0m)
                {
                    return false;
                }

                giaTri /= ve;
            }
        }
    }

    private static bool DocThuaSo(string s, ref int i, out decimal giaTri)
    {
        giaTri = 0m;
        BoTrang(s, ref i);
        if (i >= s.Length)
        {
            return false;
        }

        if (s[i] == '-')
        {
            i++;
            if (!DocThuaSo(s, ref i, out giaTri))
            {
                return false;
            }

            giaTri = -giaTri;
            return true;
        }

        if (s[i] == '+')
        {
            i++;
            return DocThuaSo(s, ref i, out giaTri);
        }

        if (s[i] == '(')
        {
            i++;
            if (!DocBieuThuc(s, ref i, out giaTri))
            {
                return false;
            }

            BoTrang(s, ref i);
            if (i >= s.Length || s[i] != ')')
            {
                return false;
            }

            i++;
            return true;
        }

        var dau = i;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == ','))
        {
            i++;
        }

        return i > dau && TryDoc(s[dau..i], out giaTri);
    }
}
