using System.Globalization;
using System.Text.RegularExpressions;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Đọc ngày người dùng gõ vào ô chọn ngày, theo đúng thói quen viết tay của cửa hàng.
/// <para>
/// Ô ngày không dùng ô gõ của Windows nữa (chữ dính sát viền, cắt cụt ở cỡ chữ to) mà là ô nhập
/// thường, nên phải tự đọc lấy chữ người ta gõ. Gõ kiểu gì cũng nhận: <c>3/8</c>, <c>03/08</c>,
/// <c>3-8-26</c>, <c>3.8.2026</c>, <c>31082026</c> — thiếu năm thì lấy năm của ngày đang chọn.
/// </para>
/// </summary>
public static class NgayViet
{
    /// <summary>Ngày viết ra chữ, luôn kiểu Việt Nam.</summary>
    public static string Viet(DateTime ngay) => LichViet.ChuNgay(ngay);

    private static readonly Regex MauTach = new(
        @"^\s*(\d{1,2})\s*[/\\.\- ]\s*(\d{1,2})(?:\s*[/\\.\- ]\s*(\d{2}|\d{4}))?\s*$",
        RegexOptions.Singleline);

    private static readonly Regex MauLienSo = new(@"^\s*(\d{4}|\d{6}|\d{8})\s*$", RegexOptions.Singleline);

    /// <summary>
    /// Đọc chữ thành ngày. <paramref name="moc"/> là ngày đang chọn — gõ thiếu năm thì lấy năm
    /// của nó, gõ mỗi con số thì hiểu là ngày trong tháng của nó.
    /// </summary>
    public static bool TryDoc(string? chu, DateTime moc, out DateTime ngay)
    {
        ngay = moc.Date;

        if (string.IsNullOrWhiteSpace(chu))
        {
            return false;
        }

        chu = chu.Trim();

        // Gõ liền không dấu tách: 3108 (ngày tháng), 310826, 31082026.
        if (MauLienSo.Match(chu) is { Success: true } lien)
        {
            var so = lien.Groups[1].Value;
            return Ghep(
                int.Parse(so[..2], CultureInfo.InvariantCulture),
                int.Parse(so.Substring(2, 2), CultureInfo.InvariantCulture),
                so.Length > 4 ? int.Parse(so[4..], CultureInfo.InvariantCulture) : null,
                moc,
                out ngay);
        }

        // Chỉ một hai con số: ngày trong tháng đang chọn.
        if (chu.Length <= 2 && int.TryParse(chu, NumberStyles.None, CultureInfo.InvariantCulture, out var soNgay))
        {
            return Ghep(soNgay, moc.Month, moc.Year, moc, out ngay);
        }

        if (MauTach.Match(chu) is not { Success: true } khop)
        {
            return false;
        }

        return Ghep(
            int.Parse(khop.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(khop.Groups[2].Value, CultureInfo.InvariantCulture),
            khop.Groups[3].Success ? int.Parse(khop.Groups[3].Value, CultureInfo.InvariantCulture) : null,
            moc,
            out ngay);
    }

    /// <summary>
    /// Ghép ngày, tháng, năm lại. Thiếu năm thì lấy năm của ngày đang chọn; năm gõ hai chữ số
    /// thì là năm 20xx — cửa hàng không ghi sổ của thế kỷ trước.
    /// </summary>
    private static bool Ghep(int ngayTrongThang, int thang, int? nam, DateTime moc, out DateTime ngay)
    {
        ngay = moc.Date;

        var namDung = nam switch
        {
            null => moc.Year,
            < 100 => 2000 + nam.Value,
            _ => nam.Value,
        };

        if (thang is < 1 or > 12 || namDung is < 1900 or > 2200)
        {
            return false;
        }

        // Ngày 31 của tháng chỉ có 30 thì coi như gõ nhầm, giữ nguyên ngày cũ — đoán bừa sang
        // ngày 1 tháng sau là chủ cửa hàng không nhận ra mình gõ hụt.
        if (ngayTrongThang < 1 || ngayTrongThang > DateTime.DaysInMonth(namDung, thang))
        {
            return false;
        }

        ngay = new DateTime(namDung, thang, ngayTrongThang);
        return true;
    }
}
