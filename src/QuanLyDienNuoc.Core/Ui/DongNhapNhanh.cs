using System.Text.RegularExpressions;

namespace QuanLyDienNuoc.Ui;

/// <summary>Một món tách được từ dòng gõ tự do.</summary>
public sealed record MucNhapNhanh(string Ten, decimal SoLuong, decimal? DonGia);

/// <summary>
/// Tách một dòng gõ tự do thành nhiều món hàng, kiểu ghi sổ ngoài công trình:
/// <c>ống 27 x10, co 90 x5, keo x1</c>. Số lượng phải đứng sau dấu <c>x</c> (hoặc <c>*</c>)
/// để tên hàng có sẵn số như "ống 27" không bị hiểu nhầm là số lượng.
/// Muốn ghi luôn giá thì thêm <c>@</c>: <c>ống 27 x10 @45000</c>.
/// </summary>
public static partial class DongNhapNhanh
{
    public static List<MucNhapNhanh> Tach(string? dong)
    {
        var ketQua = new List<MucNhapNhanh>();
        if (string.IsNullOrWhiteSpace(dong))
        {
            return ketQua;
        }

        var muc = dong.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var phan in muc)
        {
            var chuoi = phan.Trim();
            if (chuoi.Length == 0)
            {
                continue;
            }

            var khop = MauMuc().Match(chuoi);
            if (!khop.Success)
            {
                // Không ghi số lượng thì coi như lấy 1.
                ketQua.Add(new MucNhapNhanh(chuoi, 1m, null));
                continue;
            }

            var ten = khop.Groups["ten"].Value.Trim();
            if (ten.Length == 0)
            {
                continue;
            }

            var soLuong = So.Doc(khop.Groups["sl"].Value);
            if (soLuong <= 0)
            {
                soLuong = 1m;
            }

            decimal? gia = null;
            var chuoiGia = khop.Groups["gia"].Value;
            if (chuoiGia.Length > 0 && So.TryDoc(chuoiGia, out var g) && g > 0)
            {
                gia = g;
            }

            ketQua.Add(new MucNhapNhanh(ten, soLuong, gia));
        }

        return ketQua;
    }

    // "ten" tham lam nên dấu nhân cuối cùng mới được tính: "ống 27 x10" ra tên "ống 27", số lượng 10.
    [GeneratedRegex(@"^(?<ten>.+)[x*×]\s*(?<sl>[\d.,]+)\s*(?:@\s*(?<gia>[\d.,]+))?$", RegexOptions.IgnoreCase)]
    private static partial Regex MauMuc();
}
