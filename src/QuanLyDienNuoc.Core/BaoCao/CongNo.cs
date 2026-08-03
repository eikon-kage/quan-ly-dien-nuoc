using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.BaoCao;

/// <summary>Công nợ của một khách hàng tại thời điểm xem.</summary>
public sealed class DongCongNo
{
    public KhachHang Khach { get; init; } = null!;

    /// <summary>Số hoá đơn đang còn nợ.</summary>
    public int SoHoaDonNo { get; init; }

    public decimal TongMua { get; init; }

    public decimal DaTra { get; init; }

    public decimal ConNo { get; init; }

    /// <summary>Lần phát sinh gần nhất trên các hoá đơn còn nợ: lấy hàng hoặc trả tiền.</summary>
    public DateTime? PhatSinhCuoi { get; init; }

    /// <summary>Lần trả tiền gần nhất (mọi hoá đơn trong phạm vi đang xem).</summary>
    public DateTime? TraCuoi { get; init; }

    /// <summary>Bao nhiêu ngày rồi khách chưa động tĩnh gì mà vẫn còn nợ.</summary>
    public int SoNgayNo { get; init; }

    public string TenHienThi => Khach.Ten;
}

/// <summary>
/// Sổ công nợ của cả cửa hàng: ai đang nợ bao nhiêu và nợ đã bao lâu.
/// Đây là câu hỏi mà cuốn sổ tay và file Excel không trả lời được ngay.
/// </summary>
public static class CongNo
{
    /// <summary>
    /// Tính công nợ từng khách. <paramref name="nam"/> để trống là tính tất cả các năm.
    /// Chỉ trả về khách còn nợ, xếp theo số ngày nợ giảm dần rồi tới số tiền.
    /// </summary>
    public static List<DongCongNo> Tinh(DuLieuApp duLieu, int? nam, DateTime homNay)
    {
        var ketQua = new List<DongCongNo>();

        foreach (var khach in duLieu.KhachHangs)
        {
            var hoaDons = duLieu.HoaDons
                .Where(h => h.KhachHangId == khach.Id && (nam is null || h.Nam == nam))
                .ToList();

            if (hoaDons.Count == 0)
            {
                continue;
            }

            var tongMua = hoaDons.Sum(h => h.TongTien);
            var daTra = hoaDons.Sum(h => h.DaThanhToan);
            var conNo = tongMua - daTra;
            if (conNo <= 0m)
            {
                continue;
            }

            var hoaDonNo = hoaDons.Where(h => h.ConLai > 0m).ToList();
            var phatSinhCuoi = MocGanNhat(hoaDonNo);
            var traCuoi = hoaDons
                .SelectMany(h => h.ThanhToans)
                .Select(t => (DateTime?)t.Ngay)
                .DefaultIfEmpty(null)
                .Max();

            ketQua.Add(new DongCongNo
            {
                Khach = khach,
                SoHoaDonNo = hoaDonNo.Count,
                TongMua = tongMua,
                DaTra = daTra,
                ConNo = conNo,
                PhatSinhCuoi = phatSinhCuoi,
                TraCuoi = traCuoi,
                SoNgayNo = phatSinhCuoi is { } moc ? Math.Max(0, (homNay.Date - moc.Date).Days) : 0,
            });
        }

        return ketQua
            .OrderByDescending(d => d.SoNgayNo)
            .ThenByDescending(d => d.ConNo)
            .ThenBy(d => d.Khach.Ten, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Các khách nợ quá <paramref name="soNgay"/> ngày.</summary>
    public static List<DongCongNo> QuaHan(IEnumerable<DongCongNo> dong, int soNgay) =>
        dong.Where(d => d.SoNgayNo >= soNgay).ToList();

    /// <summary>Ngày phát sinh gần nhất trên nhóm hoá đơn: lấy hàng hoặc trả tiền, lấy ngày muộn hơn.</summary>
    private static DateTime? MocGanNhat(IEnumerable<HoaDon> hoaDons)
    {
        DateTime? moc = null;

        foreach (var hoaDon in hoaDons)
        {
            foreach (var ngay in hoaDon.ChiTiet.Select(c => c.Ngay)
                         .Concat(hoaDon.ThanhToans.Select(t => t.Ngay))
                         .Append(hoaDon.NgayMo))
            {
                if (moc is null || ngay > moc)
                {
                    moc = ngay;
                }
            }
        }

        return moc;
    }
}
