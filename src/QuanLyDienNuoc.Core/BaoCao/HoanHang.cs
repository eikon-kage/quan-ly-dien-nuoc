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

/// <summary>Các dòng để ghi vào tờ hoàn sau khi đã ghép với hoá đơn gốc, kèm chỗ cần xem lại.</summary>
public sealed record KetQuaGhepHoan(List<ChiTietHoaDon> Dong, List<string> CanhBao);

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

    /// <summary>
    /// Hoá đơn bán mà một tờ hoàn nhập từ file Excel hoàn cho, tìm theo mã ghi trên giấy
    /// ("Hoàn cho hoá đơn HD2026-02"). Không có mã, hoặc mã không còn trong sổ, thì tờ hoàn
    /// đứng riêng một mình — vẫn trừ vào nợ của khách như thường, chỉ là bản in không có dòng
    /// "hoàn cho hoá đơn nào".
    /// </summary>
    public static HoaDon? TimHoaDonGoc(IEnumerable<HoaDon> hoaDons, string? maTrenGiay)
    {
        if (string.IsNullOrWhiteSpace(maTrenGiay))
        {
            return null;
        }

        var ma = maTrenGiay.Trim();
        return hoaDons.FirstOrDefault(h =>
            !h.LaHoanHang && string.Equals(h.MaHoaDon, ma, StringComparison.CurrentCultureIgnoreCase));
    }

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

        return goc.ChiTiet
            .Where(c => c.SoLuong > 0m)
            .Select(c => new DongCoTheHoan(c, c.SoLuong, daHoan.TryGetValue(c.Id, out var da) ? da : 0m))
            .ToList();
    }

    /// <summary>
    /// Ghép các dòng hoàn đọc từ file Excel vào đúng dòng của hoá đơn gốc, để tờ hoàn nhập từ
    /// file cũng biết mỗi món hoàn cho dòng nào — có vậy màn hình hoàn hàng mới cộng đúng cột
    /// ĐÃ HOÀN, không cho hoàn lần nữa số đã hoàn bằng file.
    /// <para>
    /// Chỉ ghép khi trùng cả tên hàng và đơn giá: giá lệch là món khác (hoặc hoàn theo giá khác)
    /// nên thà để dòng đứng riêng và báo một câu, hơn là nối bừa vào dòng không phải nó. Một
    /// dòng trên giấy có thể tách ra nhiều dòng trong sổ khi hoá đơn gốc bán món đó ở nhiều ngày.
    /// </para>
    /// <para>
    /// Phần hoàn quá số còn hoàn được vẫn ghi vào tờ hoàn (file là chứng từ khách đang giữ, sổ
    /// phải khớp tờ giấy) nhưng có câu cảnh báo để người dùng xem lại.
    /// </para>
    /// </summary>
    public static KetQuaGhepHoan GhepVaoHoaDonGoc(
        IEnumerable<HoaDon> hoaDons,
        HoaDon goc,
        IEnumerable<ChiTietHoaDon> dongDoc)
    {
        var coTheHoan = DongCoTheHoanCua(hoaDons, goc);
        var conHoan = coTheHoan.ToDictionary(d => d.Dong.Id, d => d.ConHoanDuoc);

        var dong = new List<ChiTietHoaDon>();
        var canhBao = new List<string>();

        foreach (var doc in dongDoc)
        {
            // Dòng đọc từ tờ hoàn mang số lượng âm. Dòng 0 (file thiếu số lượng) hay dòng dương
            // lạc vào đây thì để nguyên, đừng đoán hộ người dùng.
            var conPhaiXep = -doc.SoLuong;
            if (conPhaiXep <= 0m)
            {
                dong.Add(SaoDong(doc, doc.SoLuong, null));
                continue;
            }

            var coMon = coTheHoan.Any(d => CungMotMon(d.Dong, doc));
            foreach (var ung in coTheHoan)
            {
                if (conPhaiXep <= 0m)
                {
                    break;
                }

                if (conHoan[ung.Dong.Id] <= 0m || !CungMotMon(ung.Dong, doc))
                {
                    continue;
                }

                var lay = Math.Min(conPhaiXep, conHoan[ung.Dong.Id]);
                conHoan[ung.Dong.Id] -= lay;
                conPhaiXep -= lay;
                dong.Add(SaoDong(doc, -lay, ung.Dong));
            }

            if (conPhaiXep <= 0m)
            {
                continue;
            }

            dong.Add(SaoDong(doc, -conPhaiXep, null));
            // conPhaiXep là phần thừa không xếp được vào dòng nào, nên nói theo phần thừa: đọc
            // ra thành "số còn hoàn được" là ngược hẳn con số người dùng cần đối chiếu.
            canhBao.Add(coMon
                ? $"\"{doc.TenHang}\": hoàn quá {So.Luong(conPhaiXep)} {doc.DonVi} so với số còn "
                  + $"hoàn được của hoá đơn {goc.MaHoaDon} — vẫn ghi vào tờ hoàn, cần xem lại."
                : $"\"{doc.TenHang}\" (giá {So.Tien(doc.DonGia)}): không có trên hoá đơn "
                  + $"{goc.MaHoaDon} — vẫn ghi vào tờ hoàn, cần xem lại.");
        }

        return new KetQuaGhepHoan(dong, canhBao);
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

    /// <summary>Cùng một món để ghép: trùng tên hàng (không xét dấu, hoa thường) và trùng đơn giá.</summary>
    private static bool CungMotMon(ChiTietHoaDon dongGoc, ChiTietHoaDon dongDoc) =>
        dongGoc.DonGia == dongDoc.DonGia
        && ChuViet.BoDau(dongGoc.TenHang).Trim() == ChuViet.BoDau(dongDoc.TenHang).Trim();

    private static ChiTietHoaDon SaoDong(ChiTietHoaDon doc, decimal soLuong, ChiTietHoaDon? dongGoc) =>
        new()
        {
            Ngay = doc.Ngay,
            VatTuId = doc.VatTuId ?? dongGoc?.VatTuId,
            DongGocId = dongGoc?.Id,
            TenHang = doc.TenHang,
            DonVi = string.IsNullOrWhiteSpace(doc.DonVi) ? dongGoc?.DonVi ?? string.Empty : doc.DonVi,
            DonGia = doc.DonGia,
            SoLuong = soLuong,
            GhiChu = doc.GhiChu,
        };
}
