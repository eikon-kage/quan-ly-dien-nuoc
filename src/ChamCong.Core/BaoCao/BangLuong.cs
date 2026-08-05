using ChamCong.Models;

namespace ChamCong.BaoCao;

/// <summary>Tiền công của một thợ trong khoảng đang xem.</summary>
public sealed class DongLuong
{
    public Tho Tho { get; init; } = null!;

    public decimal CongSang { get; init; }

    public decimal CongChieu { get; init; }

    public decimal TongCong => CongSang + CongChieu;

    /// <summary>Tiền công đã tính theo giá của lúc chấm từng buổi.</summary>
    public decimal TienCong { get; init; }

    public decimal DaUng { get; init; }

    /// <summary>Số tiền còn phải trả thợ. Ứng quá tay thì số này âm.</summary>
    public decimal ConLai => TienCong - DaUng;

    public string TenHienThi => Tho.Ten;
}

/// <summary>
/// Bảng lương: mỗi thợ làm bao nhiêu công, thành bao nhiêu tiền, đã ứng bao nhiêu
/// và còn phải trả bao nhiêu.
/// </summary>
public static class BangLuong
{
    /// <summary>
    /// Tính bảng lương trong khoảng ngày, tính cả <paramref name="tuNgay"/> và
    /// <paramref name="denNgay"/>. Thợ đã nghỉ vẫn hiện nếu trong kỳ có công hoặc có ứng tiền.
    /// Xếp theo tên thợ.
    /// </summary>
    public static List<DongLuong> Tinh(DuLieuChamCong duLieu, DateTime tuNgay, DateTime denNgay)
    {
        var tu = tuNgay.Date;
        var den = denNgay.Date;

        var ketQua = new List<DongLuong>();

        foreach (var tho in duLieu.Thos)
        {
            var buoiCongs = duLieu.BuoiCongs
                .Where(b => b.ThoId == tho.Id && b.Ngay.Date >= tu && b.Ngay.Date <= den)
                .ToList();

            var daUng = duLieu.UngTiens
                .Where(u => u.ThoId == tho.Id && u.Ngay.Date >= tu && u.Ngay.Date <= den)
                .Sum(u => u.SoTien);

            if (buoiCongs.Count == 0 && daUng == 0m)
            {
                continue;
            }

            ketQua.Add(new DongLuong
            {
                Tho = tho,
                CongSang = buoiCongs.Where(b => b.Buoi == BuoiLam.Sang).Sum(b => b.SoCong),
                CongChieu = buoiCongs.Where(b => b.Buoi == BuoiLam.Chieu).Sum(b => b.SoCong),
                TienCong = buoiCongs.Sum(b => b.SoCong * (b.TienMotCong ?? tho.TienMotCong)),
                DaUng = daUng,
            });
        }

        return ketQua
            .OrderBy(d => d.Tho.Ten, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Bảng lương của trọn một tháng.</summary>
    public static List<DongLuong> Thang(DuLieuChamCong duLieu, int nam, int thang)
    {
        var dauThang = new DateTime(nam, thang, 1);
        return Tinh(duLieu, dauThang, dauThang.AddMonths(1).AddDays(-1));
    }
}
