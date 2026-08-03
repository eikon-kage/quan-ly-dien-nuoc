using System.Globalization;
using System.Text;

namespace QuanLyDienNuoc.Ui;

/// <summary>Tiện ích tìm kiếm tiếng Việt: gõ "nguyen" vẫn tìm ra "Nguyễn".</summary>
public static class ChuViet
{
    public static string BoDau(string? chuoi)
    {
        if (string.IsNullOrEmpty(chuoi))
        {
            return string.Empty;
        }

        var tach = chuoi.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(tach.Length);

        foreach (var kyTu in tach)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(kyTu) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(kyTu);
            }
        }

        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .ToLowerInvariant();
    }

    public static bool Chua(string? nguon, string? tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return true;
        }

        return BoDau(nguon).Contains(BoDau(tuKhoa), StringComparison.Ordinal);
    }
}
