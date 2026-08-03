using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>Điền dữ liệu hoá đơn vào mẫu Excel của cửa hàng, tự chia trang khi nhiều dòng.</summary>
public static class XuatHoaDon
{
    /// <summary>Chia các dòng hàng theo sức chứa của từng trang. Luôn trả về ít nhất một trang.</summary>
    public static List<List<ChiTietHoaDon>> ChiaTrang(IEnumerable<ChiTietHoaDon> chiTiet)
    {
        var dong = chiTiet
            .OrderBy(c => c.Ngay)
            .ThenBy(c => c.TenHang, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var trang = new List<List<ChiTietHoaDon>>();
        var daLay = 0;
        var sucChua = MauHoaDon.Trang1.SoDongMoiTrang;

        do
        {
            trang.Add(dong.Skip(daLay).Take(sucChua).ToList());
            daLay += sucChua;
            sucChua = MauHoaDon.TrangSau.SoDongMoiTrang;
        }
        while (daLay < dong.Count);

        return trang;
    }

    /// <summary>Xuất hoá đơn ra file Excel theo đúng mẫu giấy của cửa hàng.</summary>
    public static void Xuat(
        HoaDon hoaDon,
        KhachHang khach,
        string fileRa,
        string? thuMucMau = null,
        DateTime? ngayIn = null)
    {
        thuMucMau ??= MauHoaDon.ThuMucMacDinh;
        var fileTrang1 = Path.Combine(thuMucMau, MauHoaDon.TenFileTrang1);
        var fileTrangSau = Path.Combine(thuMucMau, MauHoaDon.TenFileTrangSau);

        if (!File.Exists(fileTrang1))
        {
            throw new FileNotFoundException($"Không tìm thấy file mẫu:\n{fileTrang1}", fileTrang1);
        }

        var trang = ChiaTrang(hoaDon.ChiTiet);
        var tongCong = hoaDon.TongTien;

        HSSFWorkbook wb;
        using (var doc = File.OpenRead(fileTrang1))
        {
            wb = new HSSFWorkbook(doc);
        }

        // File mẫu có thể còn nhiều tab khác (mẫu cũ, biểu đồ...) — chỉ giữ đúng tab cần dùng.
        GiuLaiMotTab(wb, MauHoaDon.TimTab(wb, MauHoaDon.TenTabTrang1));
        wb.SetSheetName(0, "Trang 1");

        if (trang.Count > 1)
        {
            if (!File.Exists(fileTrangSau))
            {
                throw new FileNotFoundException($"Hoá đơn có {trang.Count} trang nhưng thiếu file mẫu:\n{fileTrangSau}", fileTrangSau);
            }

            using var docSau = File.OpenRead(fileTrangSau);
            var wbSau = new HSSFWorkbook(docSau);
            var mauTrangSau = wbSau.GetSheetAt(MauHoaDon.TimTab(wbSau, MauHoaDon.TenTabTrangSau));

            for (var i = 2; i <= trang.Count; i++)
            {
                mauTrangSau.CopyTo(wb, $"Trang {i}", true, true);
            }
        }

        var soThuTu = 1;
        for (var i = 0; i < trang.Count; i++)
        {
            var viTri = i == 0 ? MauHoaDon.Trang1 : MauHoaDon.TrangSau;
            DienMotTrang(
                wb.GetSheetAt(i),
                viTri,
                trang[i],
                soThuTu,
                khach,
                laTrangCuoi: i == trang.Count - 1,
                tongCong,
                ngayIn ?? DateTime.Today);
            soThuTu += trang[i].Count;
        }

        var thuMucRa = Path.GetDirectoryName(fileRa);
        if (!string.IsNullOrEmpty(thuMucRa))
        {
            Directory.CreateDirectory(thuMucRa);
        }

        using var ghi = new FileStream(fileRa, FileMode.Create, FileAccess.Write);
        wb.Write(ghi, leaveOpen: false);
    }

    private static void DienMotTrang(
        ISheet sheet,
        ViTriTrang viTri,
        List<ChiTietHoaDon> dong,
        int soThuTuDau,
        KhachHang khach,
        bool laTrangCuoi,
        decimal tongCong,
        DateTime ngayIn)
    {
        if (viTri.DongTenKhach >= 0)
        {
            LayO(sheet, viTri.DongTenKhach, 0).SetCellValue($"Tên khách hàng: {khach.Ten}");
        }

        if (viTri.DongDiaChi >= 0)
        {
            LayO(sheet, viTri.DongDiaChi, 0).SetCellValue($"Địa chỉ: {khach.DiaChi}");
        }

        // Dọn sạch vùng dữ liệu phòng khi file mẫu còn sót số liệu cũ.
        for (var i = 0; i < viTri.SoDongMoiTrang; i++)
        {
            var r = viTri.DongDauDuLieu + i;
            for (var c = MauHoaDon.CotTT; c <= MauHoaDon.CotThanhTien; c++)
            {
                sheet.GetRow(r)?.GetCell(c)?.SetBlank();
            }
        }

        for (var i = 0; i < dong.Count; i++)
        {
            var ct = dong[i];
            var r = viTri.DongDauDuLieu + i;

            LayO(sheet, r, MauHoaDon.CotTT).SetCellValue((soThuTuDau + i).ToString());
            LayO(sheet, r, MauHoaDon.CotTenHang).SetCellValue(ct.TenHang);
            LayO(sheet, r, MauHoaDon.CotDonVi).SetCellValue(ct.DonVi);
            LayO(sheet, r, MauHoaDon.CotSoLuong).SetCellValue((double)ct.SoLuong);

            if (ct.DonGia != 0)
            {
                LayO(sheet, r, MauHoaDon.CotDonGia).SetCellValue((double)ct.DonGia);
            }

            LayO(sheet, r, MauHoaDon.CotThanhTien).SetCellValue((double)ct.ThanhTien);
        }

        // Dòng tổng: trang cuối là tổng cộng cả hoá đơn, các trang trước là cộng của riêng trang đó.
        var tienCuaTrang = dong.Sum(c => c.ThanhTien);
        LayO(sheet, viTri.DongTong, 0).SetCellValue(laTrangCuoi ? "TỔNG CỘNG" : "CỘNG TRANG NÀY");
        LayO(sheet, viTri.DongTong, MauHoaDon.CotThanhTien)
            .SetCellValue((double)(laTrangCuoi ? tongCong : tienCuaTrang));

        LayO(sheet, viTri.DongBangChu, 0).SetCellValue(
            laTrangCuoi ? $"Thành tiền( bằng chữ): {DocSo.DocTien(tongCong)}" : string.Empty);

        LayO(sheet, viTri.DongNgay, MauHoaDon.CotNgayThang).SetCellValue(
            laTrangCuoi ? $"Ngày  {ngayIn.Day}   tháng  {ngayIn.Month}   năm {ngayIn.Year}" : string.Empty);
    }

    /// <summary>Xoá mọi tab khác trong file, chỉ chừa lại tab mẫu cần dùng.</summary>
    private static void GiuLaiMotTab(IWorkbook wb, int chiSoGiuLai)
    {
        for (var i = wb.NumberOfSheets - 1; i >= 0; i--)
        {
            if (i != chiSoGiuLai)
            {
                wb.RemoveSheetAt(i);
            }
        }
    }

    /// <summary>Lấy ô để ghi, tạo mới nếu thiếu và mượn định dạng cùng dòng để không mất khung kẻ.</summary>
    private static ICell LayO(ISheet sheet, int dong, int cot)
    {
        var hang = sheet.GetRow(dong) ?? sheet.CreateRow(dong);
        var o = hang.GetCell(cot);
        if (o is not null)
        {
            return o;
        }

        o = hang.CreateCell(cot);
        for (var c = MauHoaDon.CotTT; c <= MauHoaDon.CotThanhTien; c++)
        {
            if (c != cot && hang.GetCell(c) is { } oMau)
            {
                o.CellStyle = oMau.CellStyle;
                break;
            }
        }

        return o;
    }
}
