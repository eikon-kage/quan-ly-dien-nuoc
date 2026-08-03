using NPOI.SS.UserModel;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Vị trí các ô trên một trang của mẫu hoá đơn Excel (dòng/cột đánh số từ 0).
/// Sửa mẫu trong thư mục MauHoaDon thì chỉnh lại các số này cho khớp.
/// </summary>
public sealed record ViTriTrang(
    int DongDauDuLieu,
    int SoDongMoiTrang,
    int DongTong,
    int DongBangChu,
    int DongNgay,
    int DongTenKhach = -1,
    int DongDiaChi = -1);

/// <summary>Mô tả mẫu hoá đơn giấy của cửa hàng: trang 1 có tiêu đề, các trang sau chỉ có bảng.</summary>
public static class MauHoaDon
{
    public const int CotTT = 0;
    public const int CotTenHang = 1;
    public const int CotDonVi = 2;
    public const int CotSoLuong = 3;
    public const int CotDonGia = 4;
    public const int CotThanhTien = 5;
    public const int CotNgayThang = 3;

    public const string TenFileTrang1 = "trang-1.xls";
    public const string TenFileTrangSau = "trang-sau.xls";

    /// <summary>Trang đầu: tiêu đề cửa hàng, tên khách, địa chỉ rồi 32 dòng hàng.</summary>
    public static readonly ViTriTrang Trang1 = new(
        DongDauDuLieu: 7,
        SoDongMoiTrang: 32,
        DongTong: 39,
        DongBangChu: 40,
        DongNgay: 41,
        DongTenKhach: 3,
        DongDiaChi: 4);

    /// <summary>Trang thứ hai trở đi: chỉ có bảng 35 dòng hàng.</summary>
    public static readonly ViTriTrang TrangSau = new(
        DongDauDuLieu: 1,
        SoDongMoiTrang: 35,
        DongTong: 36,
        DongBangChu: 38,
        DongNgay: 40);

    /// <summary>
    /// Tên tab dùng làm mẫu trang đầu, xếp theo thứ tự ưu tiên. Nhờ vậy thả thẳng file
    /// hoá đơn gốc (nhiều tab) vào thư mục mẫu vẫn lấy đúng tab.
    /// </summary>
    public static readonly string[] TenTabTrang1 = { "mẫu hoá đơn mối", "Trang 1" };

    /// <summary>Tên tab dùng làm mẫu cho trang thứ hai trở đi.</summary>
    public static readonly string[] TenTabTrangSau = { "mau cũ", "Trang sau" };

    /// <summary>Thư mục mẫu nằm cạnh file chạy của phần mềm.</summary>
    public static string ThuMucMacDinh => Path.Combine(AppContext.BaseDirectory, "MauHoaDon");

    /// <summary>
    /// Tìm tab mẫu trong file: ưu tiên đúng tên, không có thì lấy tab đầu tiên trông
    /// giống bảng hàng (bỏ qua tab biểu đồ, tab trống).
    /// </summary>
    public static int TimTab(IWorkbook wb, params string[] tenUuTien)
    {
        foreach (var ten in tenUuTien)
        {
            var can = ChuViet.BoDau(ten.Trim());
            for (var i = 0; i < wb.NumberOfSheets; i++)
            {
                if (ChuViet.BoDau(wb.GetSheetName(i).Trim()) == can)
                {
                    return i;
                }
            }
        }

        for (var i = 0; i < wb.NumberOfSheets; i++)
        {
            if (LaBangHang(wb.GetSheetAt(i)))
            {
                return i;
            }
        }

        return 0;
    }

    private static bool LaBangHang(ISheet sheet)
    {
        var het = Math.Min(sheet.LastRowNum, 40);
        for (var r = 0; r <= het; r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                continue;
            }

            for (var c = hang.FirstCellNum; c < hang.LastCellNum && c >= 0; c++)
            {
                var o = hang.GetCell(c);
                if (o?.CellType == CellType.String
                    && ChuViet.BoDau(o.StringCellValue).Replace('\n', ' ').Contains("ten hang"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
