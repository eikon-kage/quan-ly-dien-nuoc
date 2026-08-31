using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Xuất toàn bộ dữ liệu ra một file Excel nhiều trang (khách hàng, hoá đơn, chi tiết hàng,
/// thanh toán, công nợ, vật tư, bảng giá riêng). Đây vừa là bản sao lưu đọc được
/// bằng Excel/WPS mà không cần phần mềm, vừa là cách lấy số liệu ra để tự lọc, tự cộng.
/// </summary>
public static class XuatToanBo
{
    public static void Xuat(DuLieuApp duLieu, string fileRa, DateTime? homNay = null)
    {
        var ngay = (homNay ?? DateTime.Today).Date;
        var wb = new XSSFWorkbook();
        var kieu = new BoKieu(wb);

        TrangKhachHang(wb, kieu, duLieu);
        TrangHoaDon(wb, kieu, duLieu);
        TrangChiTiet(wb, kieu, duLieu);
        TrangThanhToan(wb, kieu, duLieu);
        TrangCongNo(wb, kieu, duLieu, ngay);
        TrangVatTu(wb, kieu, duLieu);
        TrangBangGiaRieng(wb, kieu, duLieu);

        var thuMuc = Path.GetDirectoryName(fileRa);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }

        using var ghi = new FileStream(fileRa, FileMode.Create, FileAccess.Write);
        wb.Write(ghi, leaveOpen: false);
    }

    // ---------- Từng trang ----------

    private static void TrangKhachHang(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu)
    {
        var sheet = TaoTrang(wb, kieu, "Khách hàng",
            ("Tên khách hàng", 32), ("Điện thoại", 16), ("Địa chỉ", 30), ("Ghi chú", 26),
            ("Ngày tạo", 13), ("Số hoá đơn", 12), ("Tổng mua", 16), ("Đã trả", 16), ("Còn nợ", 16));

        var dong = 1;
        foreach (var khach in duLieu.KhachHangs.OrderBy(k => k.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            var hoaDons = duLieu.HoaDons.Where(h => h.KhachHangId == khach.Id).ToList();
            var tong = hoaDons.Sum(h => h.TongTien);
            var traRoi = hoaDons.Sum(h => h.DaThanhToan);

            var r = sheet.CreateRow(dong++);
            Chu(r, 0, khach.Ten, kieu);
            Chu(r, 1, khach.DienThoai, kieu);
            Chu(r, 2, khach.DiaChi, kieu);
            Chu(r, 3, khach.GhiChu, kieu);
            Ngay(r, 4, khach.NgayTao, kieu);
            SoNguyen(r, 5, hoaDons.Count, kieu);
            Tien(r, 6, tong, kieu);
            Tien(r, 7, traRoi, kieu);
            Tien(r, 8, tong - traRoi, kieu);
        }

        ChotDong(sheet, dong, 9, 5, 6, 7, 8);
    }

    private static void TrangHoaDon(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu)
    {
        var sheet = TaoTrang(wb, kieu, "Hoá đơn",
            ("Khách hàng", 30), ("Mã hoá đơn", 15), ("Loại", 13), ("Hoàn cho hoá đơn", 17),
            ("Năm", 8), ("Mở ngày", 13), ("Chốt ngày", 13),
            ("Số dòng hàng", 13), ("Tổng tiền", 16), ("Đã trả", 16), ("Còn nợ", 16), ("Ghi chú", 26));

        // Tờ hoàn hàng có tổng tiền âm nên trong bảng này nó tự trừ vào cột tổng cuối trang.
        var maTheoId = duLieu.HoaDons.ToDictionary(h => h.Id, h => h.MaHoaDon);

        var dong = 1;
        foreach (var hoaDon in SapXepHoaDon(duLieu))
        {
            var r = sheet.CreateRow(dong++);
            Chu(r, 0, TenKhach(duLieu, hoaDon.KhachHangId), kieu);
            Chu(r, 1, hoaDon.MaHoaDon, kieu);
            Chu(r, 2, hoaDon.LaHoanHang ? "Hoàn hàng" : "Bán hàng", kieu);
            Chu(
                r,
                3,
                hoaDon.HoaDonGocId is { } gocId && maTheoId.TryGetValue(gocId, out var maGoc)
                    ? maGoc
                    : string.Empty,
                kieu);
            SoNguyen(r, 4, hoaDon.Nam, kieu);
            Ngay(r, 5, hoaDon.NgayMo, kieu);
            if (hoaDon.NgayChot is { } chot)
            {
                Ngay(r, 6, chot, kieu);
            }
            else
            {
                Chu(r, 6, string.Empty, kieu);
            }

            SoNguyen(r, 7, hoaDon.ChiTiet.Count, kieu);
            Tien(r, 8, hoaDon.TongTien, kieu);
            Tien(r, 9, hoaDon.DaThanhToan, kieu);
            Tien(r, 10, hoaDon.ConLai, kieu);
            Chu(r, 11, hoaDon.GhiChu, kieu);
        }

        ChotDong(sheet, dong, 12, 7, 8, 9, 10);
    }

    private static void TrangChiTiet(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu)
    {
        var sheet = TaoTrang(wb, kieu, "Chi tiết hàng",
            ("Ngày", 13), ("Khách hàng", 30), ("Mã hoá đơn", 15), ("Tên hàng", 34), ("Đơn vị", 10),
            ("Đơn giá", 14), ("Số lượng", 12), ("Thành tiền", 16), ("Ghi chú", 24));

        var dong = 1;
        foreach (var hoaDon in SapXepHoaDon(duLieu))
        {
            var tenKhach = TenKhach(duLieu, hoaDon.KhachHangId);
            foreach (var ct in hoaDon.ChiTiet)
            {
                var r = sheet.CreateRow(dong++);
                Ngay(r, 0, ct.Ngay, kieu);
                Chu(r, 1, tenKhach, kieu);
                Chu(r, 2, hoaDon.MaHoaDon, kieu);
                Chu(r, 3, ct.TenHang, kieu);
                Chu(r, 4, ct.DonVi, kieu);
                Tien(r, 5, ct.DonGia, kieu);
                Luong(r, 6, ct.SoLuong, kieu);
                Tien(r, 7, ct.ThanhTien, kieu);
                Chu(r, 8, ct.GhiChu, kieu);
            }
        }

        ChotDong(sheet, dong, 9, 7);
    }

    private static void TrangThanhToan(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu)
    {
        var sheet = TaoTrang(wb, kieu, "Thanh toán",
            ("Ngày", 13), ("Khách hàng", 30), ("Mã hoá đơn", 15), ("Số tiền", 16), ("Ghi chú", 30));

        var dong = 1;
        foreach (var hoaDon in SapXepHoaDon(duLieu))
        {
            var tenKhach = TenKhach(duLieu, hoaDon.KhachHangId);
            foreach (var tt in hoaDon.ThanhToans.OrderBy(t => t.Ngay))
            {
                var r = sheet.CreateRow(dong++);
                Ngay(r, 0, tt.Ngay, kieu);
                Chu(r, 1, tenKhach, kieu);
                Chu(r, 2, hoaDon.MaHoaDon, kieu);
                Tien(r, 3, tt.SoTien, kieu);
                Chu(r, 4, tt.GhiChu, kieu);
            }
        }

        ChotDong(sheet, dong, 5, 3);
    }

    private static void TrangCongNo(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu, DateTime homNay)
    {
        var sheet = TaoTrang(wb, kieu, "Công nợ",
            ("Khách hàng", 30), ("Điện thoại", 16), ("Số HĐ còn nợ", 14), ("Tổng mua", 16),
            ("Đã trả", 16), ("Còn nợ", 16), ("Phát sinh cuối", 15), ("Trả lần cuối", 15), ("Số ngày nợ", 12));

        var dong = 1;
        foreach (var cn in CongNo.Tinh(duLieu, nam: null, homNay))
        {
            var r = sheet.CreateRow(dong++);
            Chu(r, 0, cn.Khach.Ten, kieu);
            Chu(r, 1, cn.Khach.DienThoai, kieu);
            SoNguyen(r, 2, cn.SoHoaDonNo, kieu);
            Tien(r, 3, cn.TongMua, kieu);
            Tien(r, 4, cn.DaTra, kieu);
            Tien(r, 5, cn.ConNo, kieu);
            if (cn.PhatSinhCuoi is { } psc)
            {
                Ngay(r, 6, psc, kieu);
            }
            else
            {
                Chu(r, 6, string.Empty, kieu);
            }

            if (cn.TraCuoi is { } tc)
            {
                Ngay(r, 7, tc, kieu);
            }
            else
            {
                Chu(r, 7, string.Empty, kieu);
            }

            SoNguyen(r, 8, cn.SoNgayNo, kieu);
        }

        ChotDong(sheet, dong, 9, 2, 3, 4, 5);
    }

    private static void TrangVatTu(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu)
    {
        var sheet = TaoTrang(wb, kieu, "Vật tư",
            ("Tên hàng", 34), ("Mã tắt", 14), ("Nhóm", 18), ("Đơn vị", 12), ("Giá chung", 16));

        var dong = 1;
        foreach (var vatTu in duLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            var r = sheet.CreateRow(dong++);
            Chu(r, 0, vatTu.Ten, kieu);
            Chu(r, 1, vatTu.MaTat, kieu);
            Chu(r, 2, vatTu.Nhom, kieu);
            Chu(r, 3, vatTu.DonVi, kieu);
            Tien(r, 4, vatTu.DonGiaMacDinh, kieu);
        }

        ChotDong(sheet, dong, 5);
    }

    private static void TrangBangGiaRieng(IWorkbook wb, BoKieu kieu, DuLieuApp duLieu)
    {
        var sheet = TaoTrang(wb, kieu, "Bảng giá riêng",
            ("Khách hàng", 30), ("Tên hàng", 34), ("Đơn vị", 12), ("Giá riêng", 16), ("Giá chung", 16));

        var dong = 1;
        foreach (var khach in duLieu.KhachHangs.OrderBy(k => k.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            foreach (var (vatTuId, gia) in khach.BangGiaRieng)
            {
                var vatTu = duLieu.VatTus.FirstOrDefault(v => v.Id == vatTuId);
                if (vatTu is null)
                {
                    continue;
                }

                var r = sheet.CreateRow(dong++);
                Chu(r, 0, khach.Ten, kieu);
                Chu(r, 1, vatTu.Ten, kieu);
                Chu(r, 2, vatTu.DonVi, kieu);
                Tien(r, 3, gia, kieu);
                Tien(r, 4, vatTu.DonGiaMacDinh, kieu);
            }
        }

        ChotDong(sheet, dong, 5);
    }

    // ---------- Tiện ích dựng bảng ----------

    private static List<HoaDon> SapXepHoaDon(DuLieuApp duLieu)
    {
        var thuTuKhach = duLieu.KhachHangs
            .OrderBy(k => k.Ten, StringComparer.CurrentCultureIgnoreCase)
            .Select((k, i) => (k.Id, ThuTu: i))
            .ToDictionary(x => x.Id, x => x.ThuTu);

        return duLieu.HoaDons
            .OrderBy(h => thuTuKhach.TryGetValue(h.KhachHangId, out var t) ? t : int.MaxValue)
            .ThenBy(h => h.Nam)
            .ThenBy(h => h.NgayMo)
            .ToList();
    }

    private static string TenKhach(DuLieuApp duLieu, Guid id) =>
        duLieu.KhachHangs.FirstOrDefault(k => k.Id == id)?.Ten ?? "(khách đã xoá)";

    private static ISheet TaoTrang(IWorkbook wb, BoKieu kieu, string ten, params (string TieuDe, int Rong)[] cot)
    {
        var sheet = wb.CreateSheet(ten);
        var hang = sheet.CreateRow(0);
        hang.HeightInPoints = 22f;

        for (var i = 0; i < cot.Length; i++)
        {
            var o = hang.CreateCell(i);
            o.SetCellValue(cot[i].TieuDe);
            o.CellStyle = kieu.TieuDe;
            sheet.SetColumnWidth(i, cot[i].Rong * 256);
        }

        // Khoá dòng tiêu đề và bật bộ lọc để mở bằng Excel là lọc/sắp xếp được ngay.
        sheet.CreateFreezePane(0, 1);
        if (cot.Length > 0)
        {
            sheet.SetAutoFilter(new CellRangeAddress(0, 0, 0, cot.Length - 1));
        }

        return sheet;
    }

    /// <summary>
    /// Dòng cuối cộng tổng, để mở file ra là thấy ngay con số tổng. Chỉ cộng những cột
    /// nêu trong <paramref name="cotCong"/> — cộng cột "Năm" hay "Số ngày nợ" thì vô nghĩa.
    /// </summary>
    private static void ChotDong(ISheet sheet, int dongTiepTheo, int soCot, params int[] cotCong)
    {
        if (dongTiepTheo <= 1 || cotCong.Length == 0)
        {
            return;
        }

        var wb = sheet.Workbook;
        var kieuTong = wb.CreateCellStyle();
        var font = wb.CreateFont();
        font.IsBold = true;
        kieuTong.SetFont(font);
        kieuTong.DataFormat = wb.CreateDataFormat().GetFormat("#,##0");
        kieuTong.BorderTop = BorderStyle.Thin;

        var hang = sheet.CreateRow(dongTiepTheo);
        for (var c = 0; c < soCot; c++)
        {
            var o = hang.CreateCell(c);
            o.CellStyle = kieuTong;

            if (c == 0)
            {
                o.SetCellValue("TỔNG CỘNG");
            }
            else if (Array.IndexOf(cotCong, c) >= 0)
            {
                var cot = CellReference.ConvertNumToColString(c);
                o.CellFormula = $"SUM({cot}2:{cot}{dongTiepTheo})";
            }
        }
    }

    private static void Chu(IRow hang, int cot, string giaTri, BoKieu kieu)
    {
        var o = hang.CreateCell(cot);
        o.SetCellValue(giaTri ?? string.Empty);
        o.CellStyle = kieu.Chu;
    }

    private static void Ngay(IRow hang, int cot, DateTime giaTri, BoKieu kieu)
    {
        var o = hang.CreateCell(cot);
        o.SetCellValue(giaTri);
        o.CellStyle = kieu.Ngay;
    }

    private static void Tien(IRow hang, int cot, decimal giaTri, BoKieu kieu)
    {
        var o = hang.CreateCell(cot);
        o.SetCellValue((double)giaTri);
        o.CellStyle = kieu.Tien;
    }

    private static void Luong(IRow hang, int cot, decimal giaTri, BoKieu kieu)
    {
        var o = hang.CreateCell(cot);
        o.SetCellValue((double)giaTri);
        o.CellStyle = kieu.Luong;
    }

    private static void SoNguyen(IRow hang, int cot, int giaTri, BoKieu kieu)
    {
        var o = hang.CreateCell(cot);
        o.SetCellValue(giaTri);
        o.CellStyle = kieu.SoNguyen;
    }

    /// <summary>Các kiểu ô dùng lại cho cả file (Excel giới hạn số kiểu nên không tạo mới từng ô).</summary>
    private sealed class BoKieu
    {
        public BoKieu(IWorkbook wb)
        {
            var dinhDang = wb.CreateDataFormat();

            var fontDam = wb.CreateFont();
            fontDam.IsBold = true;

            TieuDe = wb.CreateCellStyle();
            TieuDe.SetFont(fontDam);
            TieuDe.BorderBottom = BorderStyle.Thin;
            TieuDe.VerticalAlignment = VerticalAlignment.Center;

            Chu = wb.CreateCellStyle();

            Ngay = wb.CreateCellStyle();
            Ngay.DataFormat = dinhDang.GetFormat("dd/mm/yyyy");

            Tien = wb.CreateCellStyle();
            Tien.DataFormat = dinhDang.GetFormat("#,##0");

            Luong = wb.CreateCellStyle();
            Luong.DataFormat = dinhDang.GetFormat("#,##0.##");

            SoNguyen = wb.CreateCellStyle();
            SoNguyen.DataFormat = dinhDang.GetFormat("0");
        }

        public ICellStyle TieuDe { get; }

        public ICellStyle Chu { get; }

        public ICellStyle Ngay { get; }

        public ICellStyle Tien { get; }

        public ICellStyle Luong { get; }

        public ICellStyle SoNguyen { get; }
    }
}
