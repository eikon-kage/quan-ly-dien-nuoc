namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Kết quả xét thứ tự các trang trong lô đang tích: lô có trang 1 hay không, trang 1 có đứng
/// đầu hay không, và tên khách đọc được ở trang 1.
/// </summary>
public sealed record XetThuTuTrang(int SoTrang, int SoTrang1, int ViTriTrang1, string? TenKhach)
{
    /// <summary>Chưa tích trang nào.</summary>
    public static readonly XetThuTuTrang KhongCo = new(0, 0, -1, null);

    /// <summary>Lô có trang đầu của tờ, nên đọc được tên khách trên giấy.</summary>
    public bool CoTrang1 => SoTrang1 > 0;

    /// <summary>Lô đang tích trang 1 của nhiều tờ khác nhau.</summary>
    public bool NhieuTrang1 => SoTrang1 > 1;

    /// <summary>Trang 1 nằm sau một trang nối tiếp — thứ tự trang bị đảo.</summary>
    public bool Trang1KhongDungDau => SoTrang1 == 1 && ViTriTrang1 > 0;

    /// <summary>
    /// Câu chặn không cho nhập, hoặc null nếu thứ tự trang dùng được. Thứ tự sai thì hàng vào
    /// sổ lệch trang, mà trên sổ không còn dấu vết trang nào để dò lại.
    /// </summary>
    public string? Chan => NhieuTrang1
        ? $"Lô đang có {SoTrang1} trang 1, tức là {SoTrang1} tờ hoá đơn khác nhau. Mỗi lượt chỉ "
          + "nhập một tờ — trang 1 là trang có \"Tên khách hàng\" ở đầu, hãy bỏ tích trang 1 của tờ kia."
        : Trang1KhongDungDau
            ? $"Trang 1 đang nằm ở vị trí thứ {ViTriTrang1 + 1} của lô. Hãy thêm trang 1 trước rồi "
              + "mới thêm các trang sau, hoặc bỏ tích những trang đứng trước nó."
            : null;

    /// <summary>Câu nhắc khi lô dùng được nhưng có chỗ người dùng nên biết.</summary>
    public string? Nhac => Chan is null && SoTrang > 0 && SoTrang1 == 0
        ? "Lô chưa có trang 1 nên không đọc được tên khách trên giấy — đang nhập thêm các trang "
          + "nối tiếp vào hoá đơn chọn ở ô NHẬP VÀO."
        : null;
}

/// <summary>
/// Xét thứ tự trang của một lô hoá đơn nhập theo nhiều lần: mẫu giấy của cửa hàng để trang đầu
/// và các trang sau ở hai file riêng, nên một tờ hoá đơn dài phải thêm vào lô từng trang một.
/// Trang 1 mang tên khách và phải đứng đầu; các trang sau chỉ có bảng hàng, nối tiếp phía dưới.
/// </summary>
public static class ThuTuTrangGiay
{
    /// <summary>Xét nhóm trang đang tích, theo đúng thứ tự người dùng đã thêm vào lô.</summary>
    public static XetThuTuTrang Xet(IEnumerable<TrangDoc> dangTich)
    {
        var trang = dangTich.ToList();
        if (trang.Count == 0)
        {
            return XetThuTuTrang.KhongCo;
        }

        var viTri = trang.FindIndex(t => t.Loai == LoaiTrangGiay.Trang1);
        var soTrang1 = trang.Count(t => t.Loai == LoaiTrangGiay.Trang1);

        // Tên khách chỉ đọc ở trang 1: các trang sau không có phần đầu nên tên khách của cả tờ
        // là tên ghi ở trang đầu, không phải tên tìm thấy ở trang nào cũng được.
        var tenKhach = viTri >= 0 ? trang[viTri].TenKhach : null;

        return new XetThuTuTrang(
            trang.Count,
            soTrang1,
            viTri,
            string.IsNullOrWhiteSpace(tenKhach) ? null : tenKhach.Trim());
    }
}
