using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.BaoCao;

/// <summary>Một phần của lần thu tiền được gán vào một hoá đơn.</summary>
public sealed record PhanBoThuTien(HoaDon HoaDon, decimal SoTien);

/// <summary>Kết quả chia tiền: gán vào những hoá đơn nào và còn thừa bao nhiêu.</summary>
public sealed record KetQuaThuTien(List<PhanBoThuTien> PhanBo, decimal ConDu)
{
    public decimal DaPhanBo => PhanBo.Sum(p => p.SoTien);
}

/// <summary>
/// Một lần khách đưa tiền đã ghi vào sổ, gom lại từ các dòng thanh toán cùng phiếu thu.
/// <paramref name="Ma"/> dùng để xoá cả lần thu, kể cả khoản trả ghi thẳng vào một hoá đơn.
/// </summary>
public sealed record LanThuTien(
    Guid Ma,
    bool ChiaNhieuHoaDon,
    DateTime Ngay,
    decimal SoTien,
    string GhiChu,
    IReadOnlyList<string> MaHoaDons)
{
    public string MoTaHoaDon => string.Join(", ", MaHoaDons);

    public int SoHoaDon => MaHoaDons.Count;
}

/// <summary>
/// Khách đưa một cục tiền trả cho nhiều hoá đơn. Phần mềm tự chia cho hoá đơn cũ nhất trước,
/// thay vì bắt chủ cửa hàng ngồi tính tay xem hoá đơn nào trừ bao nhiêu.
/// </summary>
public static class ThuTien
{
    /// <summary>Hoá đơn xếp từ cũ tới mới — thứ tự trả nợ.</summary>
    public static List<HoaDon> XepTuCuNhat(IEnumerable<HoaDon> hoaDons) => hoaDons
        .OrderBy(h => h.NgayMo)
        .ThenBy(h => h.MaHoaDon, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    /// <summary>
    /// Chia <paramref name="soTien"/> cho các hoá đơn còn nợ, cũ nhất trả trước.
    /// Trả dư mà bật <paramref name="ghiDuVaoHoaDonMoiNhat"/> thì phần thừa ghi vào hoá đơn
    /// mới nhất (thành trả trước), ngược lại phần thừa để lại trong <see cref="KetQuaThuTien.ConDu"/>.
    /// </summary>
    public static KetQuaThuTien Chia(
        IEnumerable<HoaDon> hoaDons,
        decimal soTien,
        bool ghiDuVaoHoaDonMoiNhat = false)
    {
        var phanBo = new List<PhanBoThuTien>();
        var xepCu = XepTuCuNhat(hoaDons);
        var conLai = soTien;

        if (conLai <= 0m)
        {
            return new KetQuaThuTien(phanBo, 0m);
        }

        foreach (var hoaDon in xepCu.Where(h => h.ConLai > 0m))
        {
            if (conLai <= 0m)
            {
                break;
            }

            var phan = Math.Min(conLai, hoaDon.ConLai);
            phanBo.Add(new PhanBoThuTien(hoaDon, phan));
            conLai -= phan;
        }

        if (conLai > 0m && ghiDuVaoHoaDonMoiNhat && xepCu.Count > 0)
        {
            var moiNhat = xepCu[^1];
            var daCo = phanBo.FindIndex(p => p.HoaDon == moiNhat);
            if (daCo >= 0)
            {
                phanBo[daCo] = phanBo[daCo] with { SoTien = phanBo[daCo].SoTien + conLai };
            }
            else
            {
                phanBo.Add(new PhanBoThuTien(moiNhat, conLai));
            }

            conLai = 0m;
        }

        return new KetQuaThuTien(phanBo, conLai);
    }

    /// <summary>
    /// Ghi lần thu tiền vào các hoá đơn. Trả về mã phiếu thu để sau này xoá được cả lần thu
    /// chứ không phải đi từng hoá đơn xoá lẻ.
    /// </summary>
    public static Guid Ghi(KetQuaThuTien ketQua, DateTime ngay, string ghiChu = "")
    {
        var phieuThuId = Guid.NewGuid();

        foreach (var phan in ketQua.PhanBo)
        {
            phan.HoaDon.ThanhToans.Add(new ThanhToan
            {
                Ngay = ngay.Date,
                SoTien = phan.SoTien,
                GhiChu = ghiChu,
                PhieuThuId = phieuThuId,
            });
        }

        return phieuThuId;
    }

    /// <summary>
    /// Xoá cả một lần thu tiền khỏi mọi hoá đơn — <paramref name="ma"/> lấy từ
    /// <see cref="LanThuTien.Ma"/>. Trả về số dòng đã xoá.
    /// </summary>
    public static int Xoa(IEnumerable<HoaDon> hoaDons, Guid ma) =>
        hoaDons.Sum(h => h.ThanhToans.RemoveAll(t => (t.PhieuThuId ?? t.Id) == ma));

    /// <summary>
    /// Các lần khách đưa tiền, mới nhất đứng đầu. Lần thu chia cho nhiều hoá đơn gom thành
    /// một dòng; các khoản trả ghi thẳng vào một hoá đơn vẫn đứng riêng từng dòng.
    /// </summary>
    public static List<LanThuTien> LichSu(IEnumerable<HoaDon> hoaDons)
    {
        var dong = hoaDons
            .SelectMany(h => h.ThanhToans.Select(t => (HoaDon: h, Lan: t)))
            .GroupBy(x => x.Lan.PhieuThuId ?? x.Lan.Id);

        return dong
            .Select(nhom => new LanThuTien(
                nhom.Key,
                nhom.First().Lan.PhieuThuId is not null,
                nhom.Min(x => x.Lan.Ngay),
                nhom.Sum(x => x.Lan.SoTien),
                nhom.Select(x => x.Lan.GhiChu).FirstOrDefault(g => !string.IsNullOrWhiteSpace(g)) ?? string.Empty,
                nhom.OrderBy(x => x.HoaDon.NgayMo).Select(x => x.HoaDon.MaHoaDon).ToList()))
            .OrderByDescending(l => l.Ngay)
            .ThenByDescending(l => l.SoTien)
            .ToList();
    }
}
