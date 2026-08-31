using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.BaoCao;

/// <summary>Một dòng hàng của khách trong ngày, kèm tờ hoá đơn mà nó nằm trong.</summary>
/// <param name="HoaDon">Tờ chứa dòng này — để bảng kê ghi được mã tờ khi khách lấy ở nhiều tờ.</param>
public sealed record DongBangKe(HoaDon HoaDon, ChiTietHoaDon Dong)
{
    /// <summary>
    /// Dòng này là hàng khách mang trả về: hoặc dòng số lượng âm ghi thẳng vào tờ đang mở,
    /// hoặc dòng của một tờ hoàn hàng. Bảng kê ghi chú lại để khách khỏi tưởng là mua thêm.
    /// </summary>
    public bool LaHoanTra => HoaDon.LaHoanHang || Dong.LaTraLai;

    /// <summary>
    /// Thành tiền giữ nguyên dấu như trong sổ (hoàn trả là số âm) — bảng kê ngày cộng thẳng
    /// các dòng lại thành tiền hàng trong ngày, đổi dấu ở đây là cộng ra số sai.
    /// </summary>
    public decimal ThanhTien => Dong.ThanhTien;
}

/// <summary>
/// Bảng kê hàng khách lấy trong đúng một ngày, gom từ mọi hoá đơn của khách đó.
/// </summary>
/// <param name="ConNo">Còn nợ của khách tính đến <paramref name="MocNo"/>, gồm mọi hoá đơn.</param>
public sealed record BangKeNgay(
    KhachHang Khach,
    DateTime Ngay,
    IReadOnlyList<DongBangKe> Dong,
    IReadOnlyList<ThanhToan> TraTrongNgay,
    decimal ConNo,
    DateTime MocNo)
{
    /// <summary>Tiền hàng trong ngày, đã trừ phần khách trả lại.</summary>
    public decimal TienHang => Dong.Sum(d => d.ThanhTien);

    /// <summary>Tiền khách đưa trong chính ngày ấy.</summary>
    public decimal DaTraTrongNgay => TraTrongNgay.Sum(t => t.SoTien);

    /// <summary>Hôm ấy khách không lấy hàng mà cũng không trả tiền — không có gì để gửi.</summary>
    public bool Trong => Dong.Count == 0 && TraTrongNgay.Count == 0;

    /// <summary>Mã các tờ hoá đơn góp dòng vào ngày này, theo đúng thứ tự bày trong bảng.</summary>
    public IReadOnlyList<string> MaHoaDons => Dong
        .Select(d => d.HoaDon.MaHoaDon)
        .Distinct()
        .ToList();
}

/// <summary>
/// Tổng hợp hàng khách lấy trong một ngày để gửi cho khách xem lại (thường là chụp thành ảnh
/// dán sang Zalo cuối buổi).
/// <para>
/// Gom cả những dòng nằm ở tờ khác: một khách có thể lấy hàng cho hai công trình vào hai tờ
/// khác nhau trong cùng một ngày, gửi thiếu một tờ là khách đối chiếu ra ngay.
/// </para>
/// </summary>
public static class TongHopNgay
{
    /// <param name="hoaDons">Mọi hoá đơn của khách, kể cả tờ hoàn hàng và tờ của năm khác.</param>
    /// <param name="ngay">Ngày cần tổng hợp (chỉ tính phần ngày, bỏ giờ).</param>
    /// <param name="mocNo">
    /// Tính còn nợ đến hết ngày nào — thường là hôm nay, kể cả khi đang tổng hợp cho hôm qua:
    /// khách đọc bảng kê hôm qua vẫn muốn biết giờ mình còn nợ bao nhiêu.
    /// </param>
    public static BangKeNgay Lam(
        KhachHang khach,
        IEnumerable<HoaDon> hoaDons,
        DateTime ngay,
        DateTime? mocNo = null)
    {
        var trongNgay = ngay.Date;
        var moc = (mocNo ?? trongNgay).Date;

        // Tờ mở trước xếp trước, mã tờ để phân định khi hai tờ mở cùng ngày. Trong một tờ thì
        // giữ nguyên thứ tự dòng của tờ ấy: thứ tự đó là thứ tự người ta ghi tay lên giấy.
        var to = hoaDons
            .OrderBy(h => h.NgayMo)
            .ThenBy(h => h.MaHoaDon, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var dong = new List<DongBangKe>();
        var tra = new List<ThanhToan>();

        foreach (var hoaDon in to)
        {
            foreach (var chiTiet in hoaDon.ChiTiet.Where(c => c.Ngay.Date == trongNgay))
            {
                dong.Add(new DongBangKe(hoaDon, chiTiet));
            }

            tra.AddRange(hoaDon.ThanhToans.Where(t => t.Ngay.Date == trongNgay));
        }

        return new BangKeNgay(khach, trongNgay, dong, tra, ConNoDenNgay(to, moc), moc);
    }

    /// <summary>
    /// Còn nợ tính đến hết một ngày: chỉ cộng hàng đã lấy và tiền đã trả từ ngày ấy trở về
    /// trước. Cộng cả sổ thì bảng kê của hôm qua lại mang con số nợ của hôm nay.
    /// </summary>
    private static decimal ConNoDenNgay(IEnumerable<HoaDon> hoaDons, DateTime moc)
    {
        var tienHang = 0m;
        var daTra = 0m;

        foreach (var hoaDon in hoaDons)
        {
            tienHang += hoaDon.ChiTiet.Where(c => c.Ngay.Date <= moc).Sum(c => c.ThanhTien);
            daTra += hoaDon.ThanhToans.Where(t => t.Ngay.Date <= moc).Sum(t => t.SoTien);
        }

        return tienHang - daTra;
    }
}
