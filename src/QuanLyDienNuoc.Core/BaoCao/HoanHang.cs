using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.BaoCao;

/// <summary>Một món khách mang trả về: dòng hàng trên hoá đơn gốc và số lượng hoàn (số dương).</summary>
public sealed record MucHoan(ChiTietHoaDon Dong, decimal SoLuong);

/// <summary>
/// Một dòng của hoá đơn gốc kèm số đã mua và số đã hoàn ở những lần trước — đủ để màn hình
/// hoàn hàng bày ra bảng chọn mà không phải tự đi cộng lại.
/// </summary>
public sealed record DongCoTheHoan(ChiTietHoaDon Dong, decimal DaMua, decimal DaHoan)
{
    /// <summary>Số còn hoàn được của dòng này. Hoàn hết rồi thì bằng 0.</summary>
    public decimal ConHoanDuoc => Math.Max(0m, DaMua - DaHoan);
}

/// <summary>
/// Hoá đơn hoàn hàng: khách mang hàng trả về sau khi hoá đơn bán đã in (hoặc đã chốt), nên
/// không sửa vào hoá đơn cũ mà lập một tờ riêng hoàn cho nó. Các dòng hàng của tờ hoàn ghi
/// số lượng âm, vậy là tổng tiền âm và tự trừ vào nợ của khách — sổ công nợ, tin nhắc nợ,
/// bảng tổng ở trang chủ không phải biết gì thêm về loại hoá đơn này.
/// </summary>
public static class HoanHang
{
    /// <summary>Các tờ hoàn hàng đã lập cho một hoá đơn bán.</summary>
    public static List<HoaDon> HoanCuaHoaDon(IEnumerable<HoaDon> hoaDons, Guid hoaDonGocId) => hoaDons
        .Where(h => h.LaHoanHang && h.HoaDonGocId == hoaDonGocId)
        .OrderBy(h => h.NgayMo)
        .ThenBy(h => h.MaHoaDon, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    /// <summary>Số lượng của một dòng hàng đã hoàn ở các tờ hoàn trước (số dương).</summary>
    public static decimal DaHoan(IEnumerable<HoaDon> hoaDons, Guid hoaDonGocId, Guid dongGocId) =>
        -HoanCuaHoaDon(hoaDons, hoaDonGocId)
            .SelectMany(h => h.ChiTiet)
            .Where(c => c.DongGocId == dongGocId)
            .Sum(c => c.SoLuong);

    /// <summary>Tổng tiền đã hoàn cho một hoá đơn bán (số dương).</summary>
    public static decimal TienDaHoan(IEnumerable<HoaDon> hoaDons, Guid hoaDonGocId) =>
        HoanCuaHoaDon(hoaDons, hoaDonGocId).Sum(h => h.TienHoan);

    /// <summary>
    /// Các dòng của hoá đơn gốc có thể hoàn, theo đúng thứ tự đang hiện trên bảng. Bỏ những
    /// dòng số lượng âm — đó là hàng khách đã trả lại ngay trong hoá đơn, hoàn nữa là hoàn hai lần.
    /// </summary>
    public static List<DongCoTheHoan> DongCoTheHoanCua(IEnumerable<HoaDon> hoaDons, HoaDon goc)
    {
        // Duyệt danh sách hoá đơn đúng một lần: màn hình hoàn hàng gọi hàm này mỗi lần nạp
        // lại bảng, mà khách mối có thể có vài chục hoá đơn với hàng trăm dòng.
        var daHoan = HoanCuaHoaDon(hoaDons, goc.Id)
            .SelectMany(h => h.ChiTiet)
            .Where(c => c.DongGocId is not null)
            .GroupBy(c => c.DongGocId!.Value)
            .ToDictionary(nhom => nhom.Key, nhom => -nhom.Sum(c => c.SoLuong));

        return ThuTuDong.TheoThuTu(goc.ChiTiet)
            .Where(c => c.SoLuong > 0m)
            .Select(c => new DongCoTheHoan(c, c.SoLuong, daHoan.TryGetValue(c.Id, out var da) ? da : 0m))
            .ToList();
    }

    /// <summary>
    /// Lập tờ hoàn hàng cho <paramref name="goc"/>. Các món có số lượng bằng 0 bị bỏ qua, số
    /// lượng ghi vào hoá đơn là số âm. Hoá đơn hoàn thuộc đúng năm của hoá đơn gốc, kể cả khi
    /// khách trả hàng sang năm sau: hai tờ phải nằm cùng một năm mới đối chiếu được với nhau.
    /// </summary>
    public static HoaDon Tao(HoaDon goc, IEnumerable<MucHoan> muc, string maHoaDon, DateTime ngay, string lyDo = "")
    {
        var hoanHang = new HoaDon
        {
            KhachHangId = goc.KhachHangId,
            Loai = LoaiHoaDon.HoanHang,
            HoaDonGocId = goc.Id,
            MaHoaDon = maHoaDon,
            Nam = goc.Nam,
            NgayMo = ngay.Date,
            GhiChu = lyDo.Trim(),
        };

        foreach (var mot in muc.Where(m => m.SoLuong > 0m))
        {
            hoanHang.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = ngay.Date,
                VatTuId = mot.Dong.VatTuId,
                DongGocId = mot.Dong.Id,
                TenHang = mot.Dong.TenHang,
                DonVi = mot.Dong.DonVi,

                // Giá hoàn đúng bằng giá đã bán cho khách, không lấy giá hiện tại của danh mục:
                // giá lên xuống theo tháng, hoàn theo giá mới là cửa hàng hoặc khách bị hụt.
                DonGia = mot.Dong.DonGia,
                SoLuong = -mot.SoLuong,
            });
        }

        return hoanHang;
    }
}
