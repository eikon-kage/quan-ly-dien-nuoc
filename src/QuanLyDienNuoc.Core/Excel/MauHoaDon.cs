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

    /// <summary>Thư mục mẫu nằm cạnh file chạy của phần mềm.</summary>
    public static string ThuMucMacDinh => Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
}
