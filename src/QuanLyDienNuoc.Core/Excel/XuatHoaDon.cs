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
        var dong = ThuTuDong.TheoThuTu(chiTiet);

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

    /// <summary>
    /// Xuất hoá đơn ra file Excel theo đúng mẫu giấy của cửa hàng.
    /// <paramref name="hoaDonGoc"/> chỉ dùng khi xuất hoá đơn hoàn hàng, để ghi lên tờ giấy
    /// là hoàn cho hoá đơn nào.
    /// </summary>
    public static void Xuat(
        HoaDon hoaDon,
        KhachHang khach,
        string fileRa,
        string? thuMucMau = null,
        DateTime? ngayIn = null,
        HoaDon? hoaDonGoc = null)
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
                hoaDon,
                trang[i],
                soThuTu,
                khach,
                laTrangCuoi: i == trang.Count - 1,
                tongCong,
                ngayIn ?? DateTime.Today,
                hoaDonGoc);
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
        HoaDon hoaDon,
        List<ChiTietHoaDon> dong,
        int soThuTuDau,
        KhachHang khach,
        bool laTrangCuoi,
        decimal tongCong,
        DateTime ngayIn,
        HoaDon? hoaDonGoc)
    {
        // Tờ hoàn hàng dùng chung mẫu giấy với hoá đơn bán, chỉ thêm tên tờ phía trên bảng —
        // có vậy chủ cửa hàng sửa mẫu bằng Excel một lần là cả hai loại đổi theo.
        if (hoaDon.LaHoanHang && viTri.DongTieuDe >= 0)
        {
            // Mẫu giấy mới chỉ chừa được một dòng trống cho tên tờ, không có dòng phụ đề riêng
            // như mẫu cũ. Lúc đó viết luôn "hoàn cho hoá đơn nào" vào cùng dòng chứ không bỏ mất.
            const string tenTo = "HÓA ĐƠN HOÀN HÀNG";
            LayO(sheet, viTri.DongTieuDe, MauHoaDon.CotTieuDe).SetCellValue(
                viTri.DongPhuDe >= 0 ? tenTo : $"{tenTo} {PhuDeHoanHang(hoaDon, hoaDonGoc)}");
            NoiDongChoDuChu(sheet, viTri.DongTieuDe);
        }

        if (hoaDon.LaHoanHang && viTri.DongPhuDe >= 0)
        {
            LayO(sheet, viTri.DongPhuDe, MauHoaDon.CotTieuDe).SetCellValue(PhuDeHoanHang(hoaDon, hoaDonGoc));
        }

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
            LayO(sheet, r, MauHoaDon.CotSoLuong).SetCellValue((double)(ct.SoLuong * hoaDon.DauInRaGiay));

            if (ct.DonGia != 0)
            {
                LayO(sheet, r, MauHoaDon.CotDonGia).SetCellValue((double)ct.DonGia);
            }

            LayO(sheet, r, MauHoaDon.CotThanhTien)
                .SetCellValue((double)(ct.ThanhTien * hoaDon.DauInRaGiay));
        }

        // Dòng tổng: trang cuối là tổng cộng cả hoá đơn, các trang trước là cộng của riêng trang đó.
        var dau = hoaDon.DauInRaGiay;
        var tienCuaTrang = dong.Sum(c => c.ThanhTien);
        var tienDongTong = (laTrangCuoi ? tongCong : tienCuaTrang) * dau;

        LayO(sheet, viTri.DongTong, 0).SetCellValue((laTrangCuoi, hoaDon.LaHoanHang) switch
        {
            (false, _) => "CỘNG TRANG NÀY",
            (true, true) => "TỔNG TIỀN HOÀN LẠI",
            (true, false) => "TỔNG CỘNG",
        });
        LayO(sheet, viTri.DongTong, MauHoaDon.CotThanhTien).SetCellValue((double)tienDongTong);

        LayO(sheet, viTri.DongBangChu, 0).SetCellValue(
            laTrangCuoi ? $"Thành tiền( bằng chữ): {DocSo.DocTien(tongCong * dau)}" : string.Empty);

        LayO(sheet, viTri.DongNgay, MauHoaDon.CotNgayThang).SetCellValue(
            laTrangCuoi ? $"Ngày  {ngayIn.Day}   tháng  {ngayIn.Month}   năm {ngayIn.Year}" : string.Empty);
    }

    /// <summary>Dòng chữ nhỏ dưới tên tờ hoàn hàng: hoàn cho hoá đơn nào, vì sao hoàn.</summary>
    private static string PhuDeHoanHang(HoaDon hoaDon, HoaDon? hoaDonGoc)
    {
        var phan = new List<string>();
        if (hoaDonGoc is { } goc)
        {
            phan.Add($"Hoàn cho hoá đơn {goc.MaHoaDon} ngày {goc.NgayMo:dd/MM/yyyy}");
        }

        if (!string.IsNullOrWhiteSpace(hoaDon.GhiChu))
        {
            phan.Add(hoaDon.GhiChu.Trim());
        }

        return phan.Count == 0 ? "(Khách trả lại hàng)" : "(" + string.Join(" — ", phan) + ")";
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

    /// <summary>
    /// Chiều cao tối thiểu (đơn vị 1/20 điểm) của dòng có chữ. Mẫu giấy chừa dòng ngăn cách rất
    /// mảnh, ghi chữ vào đó mà không nới ra thì in giấy bị cắt mất gần hết chữ.
    /// </summary>
    private const short CaoDongCoChu = 330;

    /// <summary>Nới dòng cho đủ cao để đọc được chữ, dòng nào đã cao hơn thì để nguyên.</summary>
    private static void NoiDongChoDuChu(ISheet sheet, int dong)
    {
        var hang = sheet.GetRow(dong) ?? sheet.CreateRow(dong);
        if (hang.Height < CaoDongCoChu)
        {
            hang.Height = CaoDongCoChu;
        }
    }

    /// <summary>Lấy ô để ghi, tạo mới nếu thiếu và mượn định dạng cùng dòng để không mất khung kẻ.</summary>
    private static ICell LayO(ISheet sheet, int dong, int cot)
    {
        var hang = sheet.GetRow(dong) ?? sheet.CreateRow(dong);
        var o = hang.GetCell(cot);
        if (o is not null)
        {
            // Mẫu có sẵn công thức ở ô này (dòng tổng của mẫu mới là =SUM(...)) thì phải bỏ đi:
            // giữ lại là Excel tự tính lại theo đúng một trang, còn trang cuối của hoá đơn nhiều
            // trang phải là tổng cộng cả hoá đơn.
            if (o.CellType == CellType.Formula)
            {
                o.SetCellType(CellType.Blank);
            }

            return o;
        }

        o = hang.CreateCell(cot);
        for (var c = MauHoaDon.CotTT; c <= MauHoaDon.CotThanhTien; c++)
        {
            if (c != cot && hang.GetCell(c) is { } oMau)
            {
                o.CellStyle = oMau.CellStyle;
                return o;
            }
        }

        // Cả dòng trống trơn (dòng chừa cho tên tờ hoàn hàng chẳng hạn) thì mượn định dạng của ô
        // cùng cột phía trên, không thì chữ ra font mặc định của Excel, lạc hẳn khỏi tờ giấy.
        for (var d = dong - 1; d >= 0; d--)
        {
            if (sheet.GetRow(d)?.GetCell(cot) is { } oTren)
            {
                o.CellStyle = oTren.CellStyle;
                break;
            }
        }

        return o;
    }
}
