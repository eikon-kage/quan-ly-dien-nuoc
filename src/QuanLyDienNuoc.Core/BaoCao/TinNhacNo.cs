using System.Text;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.BaoCao;

/// <summary>
/// Soạn sẵn đoạn tin nhắn nhắc nợ để dán vào Zalo/tin nhắn, kèm bảng kê ngắn từng hoá đơn.
/// Lời lẽ giữ mức lịch sự, khách quen đọc không phật ý.
/// </summary>
public static class TinNhacNo
{
    public static string Soan(
        KhachHang khach,
        IEnumerable<HoaDon> hoaDons,
        DateTime homNay,
        ThongTinCuaHang? cuaHang = null)
    {
        // Lấy cả tờ hoàn hàng (còn lại âm): nhắc nợ mà bỏ phần đã hoàn thì con số đòi cao hơn
        // số khách thật sự nợ — khách đối chiếu ra là mất tin cuốn sổ.
        var conNo = hoaDons
            .Where(h => h.ConLai != 0m)
            .OrderBy(h => h.NgayMo)
            .ToList();

        var tong = conNo.Sum(h => h.ConLai);
        var ten = string.IsNullOrWhiteSpace(cuaHang?.Ten) ? "Cửa hàng" : cuaHang!.Ten.Trim();

        var sb = new StringBuilder();
        sb.AppendLine($"Kính gửi {khach.Ten},");
        sb.AppendLine();
        sb.AppendLine($"{ten} xin gửi anh/chị bảng kê công nợ tính đến ngày {homNay:dd/MM/yyyy}:");
        sb.AppendLine();

        foreach (var hoaDon in conNo)
        {
            var mocCuoi = hoaDon.ChiTiet.Count > 0 ? hoaDon.ChiTiet.Max(c => c.Ngay) : hoaDon.NgayMo;

            if (hoaDon.LaHoanHang)
            {
                sb.AppendLine(
                    $"- {hoaDon.MaHoaDon} (hoàn hàng ngày {mocCuoi:dd/MM/yyyy}): " +
                    $"trừ {So.Tien(hoaDon.TienHoan)}đ");
                continue;
            }

            sb.AppendLine(
                $"- {hoaDon.MaHoaDon} (lấy hàng đến {mocCuoi:dd/MM/yyyy}): " +
                $"mua {So.Tien(hoaDon.TongTien)}đ, đã trả {So.Tien(hoaDon.DaThanhToan)}đ, " +
                $"còn {So.Tien(hoaDon.ConLai)}đ");
        }

        sb.AppendLine();
        sb.AppendLine($"Tổng còn lại: {So.Tien(tong)}đ ({DocSo.DocTien(tong)}).");
        sb.AppendLine();
        sb.AppendLine("Anh/chị thu xếp giúp cửa hàng khi thuận tiện nhé. Có gì chưa khớp anh/chị nhắn lại để em kiểm tra lại sổ.");

        if (!string.IsNullOrWhiteSpace(cuaHang?.DienThoai))
        {
            sb.AppendLine();
            sb.AppendLine($"{ten} - {cuaHang!.DienThoai.Trim()}");
        }

        return sb.ToString().TrimEnd();
    }
}
