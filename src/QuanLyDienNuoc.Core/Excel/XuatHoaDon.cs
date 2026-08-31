using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Một dòng hàng trên tờ giấy. Chủ cửa hàng viết mốc ngày ("1/12") vào ô số thứ tự của dòng
/// hàng đầu tiên lấy hôm ấy, các dòng dưới mới ghi số thứ tự — nên dòng nào cũng là dòng hàng,
/// mốc ngày chỉ là chuyện dòng ấy ghi gì ở cột TT.
/// </summary>
public sealed record DongTrenTo
{
    /// <summary>Dòng hàng của tờ giấy.</summary>
    public required ChiTietHoaDon Hang { get; init; }

    /// <summary>
    /// Ngày ghi đè lên ô số thứ tự, chỉ dòng hàng đầu tiên của mỗi ngày mới có.
    /// </summary>
    public DateTime? Moc { get; init; }

    /// <summary>Số thứ tự của dòng hàng, chạy liên tục qua các trang. Dòng có mốc thì không in ra.</summary>
    public int SoThuTu { get; init; }
}

/// <summary>Điền dữ liệu hoá đơn vào mẫu Excel của cửa hàng, tự chia trang khi nhiều dòng.</summary>
public static class XuatHoaDon
{
    /// <summary>
    /// Xếp các dòng hàng lên từng trang giấy, đánh dấu mốc ngày vào dòng hàng đầu tiên của mỗi
    /// ngày. Luôn trả về ít nhất một trang.
    /// <para>
    /// Mốc không ăn thêm dòng nào của trang — nó chỉ thay con số ở cột TT — nên trang 1 chứa
    /// đủ 25 dòng hàng dù tờ gom hàng của bao nhiêu ngày.
    /// </para>
    /// </summary>
    public static List<List<DongTrenTo>> LenTrang(IEnumerable<ChiTietHoaDon> chiTiet)
    {
        var dong = chiTiet.ToList();

        var trang = new List<List<DongTrenTo>>();
        var daLay = 0;
        var soThuTu = 1;

        do
        {
            var sucChua = (trang.Count == 0 ? MauHoaDon.Trang1 : MauHoaDon.TrangSau).SoDongMoiTrang;
            var mot = new List<DongTrenTo>();

            // Mỗi trang là một file riêng và nhập vào phần mềm từng lần một, nên trang nào cũng
            // phải tự mang ngày của nó: dòng đầu trang luôn có mốc, kể cả khi cùng ngày với dòng
            // cuối trang trước.
            DateTime? ngayDongTren = null;

            while (daLay < dong.Count && mot.Count < sucChua)
            {
                var ngay = dong[daLay].Ngay.Date;

                mot.Add(new DongTrenTo
                {
                    Hang = dong[daLay],
                    Moc = ngayDongTren == ngay ? null : ngay,
                    SoThuTu = soThuTu++,
                });

                ngayDongTren = ngay;
                daLay++;
            }

            trang.Add(mot);
        }
        while (daLay < dong.Count);

        return trang;
    }

    /// <summary>Chia riêng các dòng hàng theo từng trang.</summary>
    public static List<List<ChiTietHoaDon>> ChiaTrang(IEnumerable<ChiTietHoaDon> chiTiet) =>
        LenTrang(chiTiet)
            .Select(t => t.Select(d => d.Hang).ToList())
            .ToList();

    /// <summary>
    /// Tên file của từng trang khi xuất. Tờ một trang giữ đúng tên người dùng đặt; tờ nhiều
    /// trang thì mỗi trang một file, số trang ghi luôn trong tên để xếp trong thư mục đúng thứ
    /// tự và nhập lại cũng theo thứ tự ấy.
    /// </summary>
    public static string TenFileTrang(string fileRa, int soTrang, int tongSoTrang)
    {
        if (tongSoTrang <= 1)
        {
            return fileRa;
        }

        var thuMuc = Path.GetDirectoryName(fileRa) ?? string.Empty;
        var ten = Path.GetFileNameWithoutExtension(fileRa);
        var duoi = Path.GetExtension(fileRa);
        return Path.Combine(thuMuc, $"{ten} - trang {soTrang}{duoi}");
    }

    /// <summary>
    /// Xuất hoá đơn ra Excel theo đúng mẫu giấy của cửa hàng: <b>mỗi trang một file riêng</b>,
    /// không gộp mấy trang thành mấy tab trong một file. Mẫu giấy của cửa hàng vốn là hai file
    /// khác nhau (trang đầu, trang sau) và màn nhập cũng gom từng file trang thành một lô, nên
    /// xuất ra rời từng trang thì cầm đi in hay nhập lại đều khớp; tab thì máy in bỏ qua và
    /// người dùng cũng không thấy.
    /// <para>
    /// Trả về danh sách file đã ghi, theo thứ tự trang. Tờ nhiều trang thì tên file có thêm
    /// " - trang N" — xem <see cref="TenFileTrang"/>.
    /// </para>
    /// <paramref name="hoaDonGoc"/> chỉ dùng khi xuất hoá đơn hoàn hàng, để ghi lên tờ giấy
    /// là hoàn cho hoá đơn nào.
    /// </summary>
    public static List<string> Xuat(
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

        var trang = LenTrang(hoaDon.ChiTiet);

        if (trang.Count > 1 && !File.Exists(fileTrangSau))
        {
            throw new FileNotFoundException(
                $"Hoá đơn có {trang.Count} trang nhưng thiếu file mẫu:\n{fileTrangSau}", fileTrangSau);
        }

        var thuMucRa = Path.GetDirectoryName(fileRa);
        if (!string.IsNullOrEmpty(thuMucRa))
        {
            Directory.CreateDirectory(thuMucRa);
        }

        var daGhi = new List<string>();

        for (var i = 0; i < trang.Count; i++)
        {
            var laTrang1 = i == 0;

            HSSFWorkbook wb;
            using (var doc = File.OpenRead(laTrang1 ? fileTrang1 : fileTrangSau))
            {
                wb = new HSSFWorkbook(doc);
            }

            // File mẫu có thể còn nhiều tab khác (mẫu cũ, biểu đồ...) — chỉ giữ đúng tab cần dùng.
            GiuLaiMotTab(
                wb,
                MauHoaDon.TimTab(wb, laTrang1 ? MauHoaDon.TenTabTrang1 : MauHoaDon.TenTabTrangSau));
            wb.SetSheetName(0, $"Trang {i + 1}");

            DienMotTrang(
                wb.GetSheetAt(0),
                laTrang1 ? MauHoaDon.Trang1 : MauHoaDon.TrangSau,
                hoaDon,
                trang[i],
                khach,
                laTrangCuoi: i == trang.Count - 1,
                hoaDon.TongTien,
                ngayIn ?? DateTime.Today,
                hoaDonGoc);

            var file = TenFileTrang(fileRa, i + 1, trang.Count);
            using (var ghi = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                wb.Write(ghi, leaveOpen: false);
            }

            daGhi.Add(file);
        }

        return daGhi;
    }

    private static void DienMotTrang(
        ISheet sheet,
        ViTriTrang viTri,
        HoaDon hoaDon,
        List<DongTrenTo> dong,
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
            var r = viTri.DongDauDuLieu + i;

            var ct = dong[i].Hang;

            // Mốc ngày "1/12" ghi vào ô số thứ tự của dòng hàng đầu tiên lấy hôm ấy, đúng lối
            // chủ cửa hàng viết tay: các dòng dưới ghi số thứ tự, tới khi gặp mốc khác. Trên tờ
            // giấy không có chỗ nào khác ghi ngày cho từng dòng, nên đây là chỗ duy nhất giữ
            // được ngày để nhập lại file này vào phần mềm.
            LayO(sheet, r, MauHoaDon.CotTT).SetCellValue(
                dong[i].Moc is { } moc ? $"{moc.Day}/{moc.Month}" : dong[i].SoThuTu.ToString());

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
        var tienCuaTrang = dong.Sum(d => d.Hang.ThanhTien);
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
