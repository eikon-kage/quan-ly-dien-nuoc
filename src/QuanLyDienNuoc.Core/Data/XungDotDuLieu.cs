namespace QuanLyDienNuoc.Data;

/// <summary>File dữ liệu đã bị máy khác sửa từ lúc mình đọc/ghi lần gần nhất.</summary>
public sealed record XungDotFile(string DuongDanFile, DateTime LucMayKhacSua, string DuongDanCatBanMayKhac);

/// <summary>
/// Ném ra khi sắp ghi đè lên file mà máy khác vừa sửa và chưa ai quyết định xử lý thế nào.
/// Phần giao diện gán <see cref="KhoDuLieu.HoiKhiFileBiMayKhacSua"/> để hỏi người dùng thay vì đổ lỗi.
/// </summary>
public sealed class XungDotDuLieuException : IOException
{
    public XungDotDuLieuException(XungDotFile xungDot)
        : base($"File dữ liệu đã bị máy khác sửa lúc {xungDot.LucMayKhacSua:HH:mm dd/MM/yyyy}:\n{xungDot.DuongDanFile}")
    {
        XungDot = xungDot;
    }

    public XungDotFile XungDot { get; }
}
