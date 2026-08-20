namespace ChamCong.SoDiDong;

/// <summary>Tiền công của một thợ trên một tập bản ghi.</summary>
public sealed class DongLuongSo
{
    public Tho Tho { get; init; } = null!;

    public decimal CongSang { get; init; }

    public decimal CongChieu { get; init; }

    public decimal TongCong => CongSang + CongChieu;

    /// <summary>Tiền công, tính theo mốc lương tại đúng ngày của từng buổi.</summary>
    public decimal TienCong { get; init; }

    public decimal DaUng { get; init; }

    /// <summary>Tiền kỳ trước còn thiếu, mang sang kỳ này. Số âm là kỳ trước trả dư.</summary>
    public decimal NoKyTruoc { get; init; }

    /// <summary>Còn phải trả thợ. Ứng quá tay thì âm.</summary>
    public decimal ConLai => TienCong - DaUng + NoKyTruoc;
}

/// <summary>Kỳ đang mở: phần chưa ai trả tiền, cộng thêm nợ mang sang từ kỳ trước.</summary>
public sealed class KyDangMo
{
    public List<DongLuongSo> Dongs { get; init; } = new();

    /// <summary>Tổng phải móc ví nếu chốt kỳ ngay bây giờ (không tính phần thợ đang cầm dư).</summary>
    public decimal TongPhaiTra => Dongs.Sum(d => Math.Max(0m, d.ConLai));

    public decimal TongTienCong => Dongs.Sum(d => d.TienCong);

    public decimal TongDaUng => Dongs.Sum(d => d.DaUng);

    public decimal TongCong => Dongs.Sum(d => d.TongCong);

    /// <summary>Ngày sớm nhất và muộn nhất có bản ghi trong kỳ, rỗng nếu kỳ chưa có gì.</summary>
    public string TuNgay { get; init; } = string.Empty;

    public string DenNgay { get; init; } = string.Empty;
}

/// <summary>
/// Bảng lương tính trên sổ lấy từ điện thoại. Đây là bản dịch của
/// <c>mobile/src/nghiepvu/bangLuong.ts</c> và <c>ky.ts</c> — **phải ra đúng con số như trên
/// điện thoại**, vì chủ cửa hàng sẽ đặt hai màn hình cạnh nhau mà so.
///
/// <para>
/// Điểm dễ làm sai nhất: kỳ lương **không cắt theo khoảng ngày** mà cắt theo *bản ghi nào đã
/// được quyết toán*. Chấm bù một ngày thuộc kỳ đã chốt thì buổi ấy chưa ai trả tiền, nó phải
/// rơi vào kỳ đang mở. Cắt theo ngày là buổi ấy lọt ra ngoài cả hai kỳ và thợ mất công.
/// </para>
/// </summary>
public static class BangLuongSo
{
    /// <summary>Buổi công và ứng tiền chưa nằm trong kỳ nào đã chốt.</summary>
    public static (List<BuoiCong> BuoiCongs, List<UngTien> UngTiens) BanGhiChuaChot(SoChamCong so)
    {
        var buoiDaChot = so.KyLuongs.SelectMany(k => k.BuoiCongIds).ToHashSet(StringComparer.Ordinal);
        var ungDaChot = so.KyLuongs.SelectMany(k => k.UngTienIds).ToHashSet(StringComparer.Ordinal);

        return (
            so.BuoiCongs.Where(b => !buoiDaChot.Contains(b.Id)).ToList(),
            so.UngTiens.Where(u => !ungDaChot.Contains(u.Id)).ToList());
    }

    /// <summary>
    /// Tiền kỳ trước còn thiếu của từng thợ. Chỉ lấy từ **kỳ chốt gần nhất**: mỗi lần chốt đã
    /// cộng luôn nợ của kỳ trước đó vào rồi, cộng dồn ngược từ đầu là tính hai lần.
    /// </summary>
    public static Dictionary<string, decimal> NoDauKy(SoChamCong so)
    {
        var no = new Dictionary<string, decimal>(StringComparer.Ordinal);
        if (so.KyLuongs.Count == 0)
        {
            return no;
        }

        foreach (var dong in so.KyLuongs[^1].Dongs.Where(d => d.ChuyenKySau != 0m))
        {
            no[dong.ThoId] = dong.ChuyenKySau;
        }

        return no;
    }

    /// <summary>
    /// Tính bảng lương trên đúng một tập buổi công và ứng tiền đã lọc sẵn. Thợ chỉ có mỗi khoản
    /// nợ mang sang, kỳ này chưa làm buổi nào, vẫn phải hiện ra — không thì món nợ biến mất
    /// khỏi màn hình mà vẫn nằm trong sổ.
    /// </summary>
    public static List<DongLuongSo> TinhTuBanGhi(
        SoChamCong so,
        IReadOnlyCollection<BuoiCong> buoiCongs,
        IReadOnlyCollection<UngTien> ungTiens,
        IReadOnlyDictionary<string, decimal>? noTheoTho = null)
    {
        var ketQua = new List<DongLuongSo>();

        foreach (var tho in so.Thos)
        {
            var cuaTho = buoiCongs.Where(b => b.ThoId == tho.Id).ToList();
            var daUng = ungTiens.Where(u => u.ThoId == tho.Id).Sum(u => u.SoTien);
            var noKyTruoc = noTheoTho is not null && noTheoTho.TryGetValue(tho.Id, out var no) ? no : 0m;

            if (cuaTho.Count == 0 && daUng == 0m && noKyTruoc == 0m)
            {
                continue;
            }

            ketQua.Add(new DongLuongSo
            {
                Tho = tho,
                CongSang = cuaTho.Where(b => b.Buoi == "Sang").Sum(b => b.SoCong),
                CongChieu = cuaTho.Where(b => b.Buoi == "Chieu").Sum(b => b.SoCong),

                // Giá của từng buổi lấy theo mốc lương tại đúng ngày đó, nên tăng lương giữa
                // tháng thì nửa đầu tháng vẫn tính giá cũ, nửa sau tính giá mới. Làm tròn cả
                // tổng một lần, đúng chỗ app điện thoại làm tròn.
                TienCong = Math.Round(
                    cuaTho.Sum(b => b.SoCong * (b.TienMotCong ?? tho.TienMotCongNgay(b.Ngay))),
                    0,
                    MidpointRounding.AwayFromZero),
                DaUng = daUng,
                NoKyTruoc = noKyTruoc,
            });
        }

        return ketQua
            .OrderBy(d => d.Tho.Ten, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Kỳ đang mở của cả sổ.</summary>
    public static KyDangMo KyHienTai(SoChamCong so)
    {
        var (buoiCongs, ungTiens) = BanGhiChuaChot(so);
        var ngays = buoiCongs.Select(b => b.Ngay)
            .Concat(ungTiens.Select(u => u.Ngay))
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        return new KyDangMo
        {
            Dongs = TinhTuBanGhi(so, buoiCongs, ungTiens, NoDauKy(so)),
            TuNgay = ngays.Count > 0 ? ngays[0] : string.Empty,
            DenNgay = ngays.Count > 0 ? ngays[^1] : string.Empty,
        };
    }

    /// <summary>Bảng lương trong một khoảng ngày (chuỗi yyyy-MM-dd), tính cả hai đầu.</summary>
    public static List<DongLuongSo> TrongKhoang(SoChamCong so, string tuNgay, string denNgay)
    {
        bool Trong(string ngay) =>
            string.CompareOrdinal(ngay, tuNgay) >= 0 && string.CompareOrdinal(ngay, denNgay) <= 0;

        return TinhTuBanGhi(
            so,
            so.BuoiCongs.Where(b => Trong(b.Ngay)).ToList(),
            so.UngTiens.Where(u => Trong(u.Ngay)).ToList());
    }
}
