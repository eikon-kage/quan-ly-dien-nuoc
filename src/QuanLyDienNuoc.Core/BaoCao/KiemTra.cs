using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.BaoCao;

/// <summary>Các phép kiểm tra chống nhập nhầm trước khi ghi một dòng hàng vào hoá đơn.</summary>
public static class KiemTra
{
    /// <summary>
    /// Giá lần này lệch quá <paramref name="nguongPhanTram"/> so với lần gần nhất bán cho
    /// chính khách này. Trả về giá lần trước nếu lệch, ngược lại trả về null.
    /// </summary>
    public static (decimal GiaCu, DateTime Ngay)? LechGia(
        IEnumerable<HoaDon> hoaDonCuaKhach,
        string tenHang,
        Guid? vatTuId,
        decimal giaMoi,
        int nguongPhanTram)
    {
        if (giaMoi <= 0m || nguongPhanTram <= 0)
        {
            return null;
        }

        var lanTruoc = hoaDonCuaKhach
            .SelectMany(h => h.ChiTiet)
            .Where(c => c.DonGia > 0m && CungMatHang(c, tenHang, vatTuId))
            .OrderByDescending(c => c.Ngay)
            .FirstOrDefault();

        if (lanTruoc is null || lanTruoc.DonGia == giaMoi)
        {
            return null;
        }

        var lech = Math.Abs(giaMoi - lanTruoc.DonGia) / lanTruoc.DonGia * 100m;
        return lech >= nguongPhanTram ? (lanTruoc.DonGia, lanTruoc.Ngay) : null;
    }

    /// <summary>Dòng y hệt đã có sẵn trong hoá đơn: cùng ngày, cùng hàng, cùng số lượng.</summary>
    public static ChiTietHoaDon? DongTrung(HoaDon? hoaDon, DateTime ngay, string tenHang, decimal soLuong) =>
        hoaDon?.ChiTiet.FirstOrDefault(c =>
            c.Ngay.Date == ngay.Date
            && c.SoLuong == soLuong
            && string.Equals(c.TenHang.Trim(), tenHang.Trim(), StringComparison.CurrentCultureIgnoreCase));

    /// <summary>Khách đã có tên gần giống (so không dấu) — tránh tạo hai lần một người.</summary>
    public static KhachHang? KhachTrungTen(IEnumerable<KhachHang> khachHangs, string ten, Guid? boQua = null)
    {
        var canhSo = ChuViet.BoDau(ten).Trim();
        if (canhSo.Length == 0)
        {
            return null;
        }

        return khachHangs.FirstOrDefault(k =>
            (boQua is null || k.Id != boQua) && ChuViet.BoDau(k.Ten).Trim() == canhSo);
    }

    private static bool CungMatHang(ChiTietHoaDon dong, string tenHang, Guid? vatTuId)
    {
        if (vatTuId is { } id && dong.VatTuId == id)
        {
            return true;
        }

        return string.Equals(dong.TenHang.Trim(), tenHang.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }
}
