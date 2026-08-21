using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Phần đầu hoá đơn (tên cửa hàng, địa chỉ, điện thoại...). Đọc thẳng từ file mẫu Excel
/// nên sửa mẫu bằng Excel là bản in trong phần mềm cũng đổi theo.
/// </summary>
public sealed record ThongTinCuaHang(
    string Ten,
    string DiaChi,
    string DienThoai,
    string NganhNghe1,
    string NganhNghe2,
    string TieuDe,
    string PhuDe)
{
    /// <summary>
    /// Mẫu giấy có in sẵn tên tờ ("HÓA ĐƠN BÁN HÀNG") ở góc trên phải hay không. Mẫu mới của
    /// cửa hàng dùng cả bốn dòng góc phải cho số tài khoản ngân hàng, nên ô này là chữ thường
    /// (tên chủ tài khoản) — bản in phải để nó cỡ chữ thường, không phóng to như tên tờ.
    /// </summary>
    public bool CoTenTo => ChuViet.BoDau(TieuDe).Contains("hoa don");

    public static ThongTinCuaHang MacDinh { get; } = new(
        "CỬA HÀNG - ĐIỆN NƯỚC",
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        "HÓA ĐƠN BÁN HÀNG",
        "(Kiêm hóa đơn thanh toán)");

    /// <summary>Đọc phần đầu từ mẫu trang 1. Thiếu file thì dùng bản mặc định.</summary>
    public static ThongTinCuaHang DocTuMau(string? thuMucMau = null)
    {
        var file = Path.Combine(thuMucMau ?? MauHoaDon.ThuMucMacDinh, MauHoaDon.TenFileTrang1);
        if (!File.Exists(file))
        {
            return MacDinh;
        }

        try
        {
            using var doc = File.OpenRead(file);
            var wb = new HSSFWorkbook(doc);
            var sheet = wb.GetSheetAt(MauHoaDon.TimTab(wb, MauHoaDon.TenTabTrang1));

            string O(int dong, int cot) => sheet.GetRow(dong)?.GetCell(cot) is { } o && o.CellType == CellType.String
                ? o.StringCellValue.Trim()
                : string.Empty;

            return new ThongTinCuaHang(
                Ten: O(0, 0),
                DiaChi: O(1, 0),
                DienThoai: O(2, 0),
                NganhNghe1: O(0, 3),
                NganhNghe2: O(1, 3),
                TieuDe: O(2, 3),
                PhuDe: O(3, 3));
        }
        catch (IOException)
        {
            return MacDinh;
        }
    }
}
