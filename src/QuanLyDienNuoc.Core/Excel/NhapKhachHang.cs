using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>Một dòng khách đọc được trong file, kèm việc dòng đó có nhập được hay không.</summary>
public enum TinhTrangDongKhach
{
    /// <summary>Khách mới, nhập được.</summary>
    ThemMoi,

    /// <summary>Trùng tên với khách đã có trong phần mềm.</summary>
    TrungKhachCu,

    /// <summary>Trùng tên với một dòng khác ở phía trên trong cùng file.</summary>
    TrungTrongFile,

    /// <summary>Không có tên khách nên không nhập được.</summary>
    ThieuTen,

    /// <summary>
    /// Ô tên không giống tên khách: nhãn của tờ giấy ("ĐC:", "Tên khách hàng: ....."),
    /// dòng tiêu đề bảng ("TT", "TÊN HÀNG") hay chỉ là con số thứ tự.
    /// </summary>
    KhongGiongTen,
}

/// <summary>Một dòng khách hàng đọc từ file, hiện lên lưới xem trước cho người dùng soát lại.</summary>
public sealed class DongKhachNhap
{
    /// <summary>Có nhập dòng này hay không. Người dùng tích/bỏ tích được trên lưới.</summary>
    public bool Chon { get; set; }

    /// <summary>
    /// Người dùng đã tự tay tích/bỏ tích dòng này, nên lần chấm lại sau không được đè lên
    /// ý của họ: cố tình thêm một khách trùng tên rồi sửa dòng khác là mất tích vừa đặt.
    /// </summary>
    public bool TuTayChon { get; set; }

    /// <summary>Số dòng như Excel hiện ở lề trái, để người dùng mở file ra dò lại đúng chỗ.</summary>
    public int SoDong { get; set; }

    public string Ten { get; set; } = string.Empty;

    public string DienThoai { get; set; } = string.Empty;

    public string DiaChi { get; set; } = string.Empty;

    public string GhiChu { get; set; } = string.Empty;

    public TinhTrangDongKhach TinhTrang { get; set; }

    /// <summary>Tên khách đã có bị trùng, để câu tình trạng nói rõ trùng với ai.</summary>
    public string TenTrung { get; set; } = string.Empty;

    /// <summary>Câu tình trạng hiện trên lưới, viết cho chủ cửa hàng đọc là hiểu.</summary>
    public string TinhTrangChu => TinhTrang switch
    {
        TinhTrangDongKhach.ThemMoi => "Thêm mới",
        TinhTrangDongKhach.TrungKhachCu => $"Đã có khách \"{TenTrung}\" — bỏ qua",
        TinhTrangDongKhach.TrungTrongFile => "Trùng dòng phía trên — bỏ qua",
        TinhTrangDongKhach.KhongGiongTen => "Không giống tên khách — bỏ qua",
        _ => "Thiếu tên khách — không nhập được",
    };

    /// <summary>Dựng đối tượng khách hàng để ghi vào sổ.</summary>
    public KhachHang ThanhKhachHang(DateTime ngayTao) => new()
    {
        Ten = Ten.Trim(),
        DienThoai = DienThoai.Trim(),
        DiaChi = DiaChi.Trim(),
        GhiChu = GhiChu.Trim(),
        NgayTao = ngayTao.Date,
    };
}

/// <summary>Kết quả đọc một file danh sách khách hàng.</summary>
public sealed class KetQuaNhapKhach
{
    /// <summary>Tên sheet đã lấy dữ liệu.</summary>
    public string TenBang { get; set; } = string.Empty;

    /// <summary>
    /// Đọc được nhờ dòng tiêu đề (an toàn, cột nào cũng nhận ra dù người dùng đổi chỗ).
    /// False là file không có tiêu đề, phải đọc theo đúng thứ tự cột 1-2-3-4 của file mẫu.
    /// </summary>
    public bool TheoTieuDe { get; set; }

    /// <summary>
    /// File này là một tờ hoá đơn (có bảng tên hàng / đvt / số lượng), không phải danh sách
    /// khách hàng — đọc theo thứ tự cột thì cả phần đầu tờ giấy thành "khách", nên dừng hẳn
    /// và chỉ đường sang chỗ nhập hàng từ hoá đơn.
    /// </summary>
    public bool LaHoaDon { get; set; }

    public List<DongKhachNhap> Dong { get; } = new();

    public List<string> CanhBao { get; } = new();

    public int SoSeNhap => Dong.Count(d => d.Chon);
}

/// <summary>
/// Nhập danh sách khách hàng từ file Excel/CSV: xuất file mẫu để người dùng điền, rồi đọc
/// file đó ra thành từng dòng khách để soát trước khi ghi vào sổ.
///
/// Cột nhận ra bằng chữ trên dòng tiêu đề trước, nên người dùng đổi chỗ cột hay thêm cột lạ
/// vẫn đọc đúng. File không có tiêu đề mới đọc theo thứ tự 1-2-3-4 của file mẫu, và khi đó
/// màn hình phải nói rõ là đang đoán theo thứ tự cột.
/// </summary>
public static class NhapKhachHang
{
    /// <summary>Tiêu đề cột trong file mẫu. Đánh số để người dùng biết cột nào là cột mấy.</summary>
    public static readonly string[] TieuDeMau =
    {
        "1. TÊN KHÁCH HÀNG (bắt buộc)",
        "2. ĐIỆN THOẠI",
        "3. ĐỊA CHỈ",
        "4. GHI CHÚ",
    };

    /// <summary>Tên sheet chứa danh sách trong file mẫu.</summary>
    public const string TenSheetMau = "Khách hàng";

    /// <summary>Số dòng đầu tối đa còn dò tìm dòng tiêu đề, quá đó coi như file không có tiêu đề.</summary>
    private const int SoDongDoTieuDe = 20;

    /// <summary>Số cột tối đa còn đọc, đủ rộng cho file có thêm cột lạ ở giữa.</summary>
    private const int SoCotDoc = 30;

    /// <summary>
    /// Ghi ra file mẫu .xlsx: một sheet danh sách chỉ có dòng tiêu đề đánh số, và một sheet
    /// hướng dẫn kèm ví dụ. Ví dụ để riêng ở sheet hướng dẫn để người dùng điền xong nhập
    /// luôn mà không kéo theo mấy dòng ví dụ vào sổ.
    /// </summary>
    public static void XuatFileMau(string fileRa)
    {
        var wb = new XSSFWorkbook();

        var fontDam = wb.CreateFont();
        fontDam.IsBold = true;

        var kieuTieuDe = wb.CreateCellStyle();
        kieuTieuDe.SetFont(fontDam);
        kieuTieuDe.BorderBottom = BorderStyle.Thin;
        kieuTieuDe.VerticalAlignment = VerticalAlignment.Center;

        // Ô chữ: số điện thoại 0912... vào ô kiểu số là Excel cắt mất số 0 đứng đầu.
        var kieuChu = wb.CreateCellStyle();
        kieuChu.DataFormat = wb.CreateDataFormat().GetFormat("@");

        var sheet = wb.CreateSheet(TenSheetMau);
        var hang = sheet.CreateRow(0);
        hang.HeightInPoints = 24f;

        var rong = new[] { 34, 18, 34, 30 };
        for (var i = 0; i < TieuDeMau.Length; i++)
        {
            var o = hang.CreateCell(i);
            o.SetCellValue(TieuDeMau[i]);
            o.CellStyle = kieuTieuDe;
            sheet.SetColumnWidth(i, rong[i] * 256);
            sheet.SetDefaultColumnStyle(i, kieuChu);
        }

        sheet.CreateFreezePane(0, 1);
        sheet.SetAutoFilter(new CellRangeAddress(0, 0, 0, TieuDeMau.Length - 1));

        TrangHuongDan(wb, fontDam);

        var thuMuc = Path.GetDirectoryName(fileRa);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }

        using var ghi = new FileStream(fileRa, FileMode.Create, FileAccess.Write);
        wb.Write(ghi, leaveOpen: false);
    }

    /// <summary>
    /// Đọc file danh sách khách (.xlsx, .xls hoặc .csv) thành từng dòng, đã chấm sẵn dòng nào
    /// nhập được. <paramref name="khachDaCo"/> là danh sách khách hiện có để phát hiện trùng tên.
    /// </summary>
    public static KetQuaNhapKhach Doc(string duongDanFile, IEnumerable<KhachHang> khachDaCo)
    {
        var bang = Path.GetExtension(duongDanFile).ToLowerInvariant() is ".csv" or ".txt"
            ? DocCsv(duongDanFile)
            : DocExcel(duongDanFile);

        var ketQua = new KetQuaNhapKhach { TenBang = bang.Ten };

        var cot = new[] { 0, 1, 2, 3 };
        var dongDauTien = 0;
        var viTriTieuDe = TimDongTieuDe(bang.Dong);

        if (viTriTieuDe is { } tieuDe)
        {
            cot = tieuDe.Cot;
            dongDauTien = tieuDe.SoDong + 1;
            ketQua.TheoTieuDe = true;
        }
        else if (bang.CoBangHang || LaBangHoaDon(bang.Dong))
        {
            // Không có tiêu đề danh sách khách mà lại có bảng hàng: đây là tờ hoá đơn. Đoán
            // theo thứ tự cột ở đây là biến cả phần đầu tờ giấy (tên cửa hàng, "ĐC:", "ĐT:",
            // "Tên khách hàng: .....") và từng dòng hàng thành mấy chục khách rác.
            ketQua.LaHoaDon = true;
            ketQua.CanhBao.Add(
                "File này là một tờ hoá đơn (có bảng tên hàng · đvt · số lượng), không phải " +
                "danh sách khách hàng.");
            return ketQua;
        }
        else
        {
            ketQua.CanhBao.Add(
                "File không có dòng tiêu đề nên đang đọc theo đúng thứ tự cột của file mẫu: " +
                "1 tên khách · 2 điện thoại · 3 địa chỉ · 4 ghi chú. Soát lại bảng dưới trước khi nhập.");
        }

        for (var i = dongDauTien; i < bang.Dong.Count; i++)
        {
            var o = bang.Dong[i];
            var ten = LayCot(o, cot[0]);
            var dienThoai = SoDienThoai(LayCot(o, cot[1]));
            var diaChi = LayCot(o, cot[2]);
            var ghiChu = LayCot(o, cot[3]);

            // Dòng trống hẳn thì bỏ im, không kể vào để câu tổng kết khỏi đếm mấy trăm dòng
            // trống mà Excel vẫn giữ ở đuôi sheet.
            if (ten.Length == 0 && dienThoai.Length == 0 && diaChi.Length == 0 && ghiChu.Length == 0)
            {
                continue;
            }

            ketQua.Dong.Add(new DongKhachNhap
            {
                SoDong = i + 1,
                Ten = ten,
                DienThoai = dienThoai,
                DiaChi = diaChi,
                GhiChu = ghiChu,
            });
        }

        ChamLaiTinhTrang(ketQua.Dong, khachDaCo);
        return ketQua;
    }

    /// <summary>
    /// Chấm lại từng dòng: thiếu tên, trùng khách cũ, trùng dòng phía trên hay thêm mới.
    /// Gọi lại sau mỗi lần người dùng sửa tay trên lưới xem trước.
    /// </summary>
    public static void ChamLaiTinhTrang(IEnumerable<DongKhachNhap> dong, IEnumerable<KhachHang> khachDaCo)
    {
        var daCo = khachDaCo.ToList();
        var tenTrongFile = new Dictionary<string, string>();

        foreach (var d in dong)
        {
            d.Ten = d.Ten.Trim();
            d.TenTrung = string.Empty;

            var canhSo = ChuViet.BoDau(d.Ten).Trim();
            if (canhSo.Length == 0)
            {
                // Thiếu tên thì không ghi được vào sổ, tích tay cũng không cứu được.
                d.TinhTrang = TinhTrangDongKhach.ThieuTen;
                d.Chon = false;
                continue;
            }

            if (KhongGiongTenKhach(d.Ten))
            {
                d.TinhTrang = TinhTrangDongKhach.KhongGiongTen;
                Tich(d, false);
                continue;
            }

            if (tenTrongFile.TryGetValue(canhSo, out var truoc))
            {
                d.TinhTrang = TinhTrangDongKhach.TrungTrongFile;
                d.TenTrung = truoc;
                Tich(d, false);
                continue;
            }

            tenTrongFile[canhSo] = d.Ten;

            if (KiemTra.KhachTrungTen(daCo, d.Ten) is { } cu)
            {
                d.TinhTrang = TinhTrangDongKhach.TrungKhachCu;
                d.TenTrung = cu.Ten;
                Tich(d, false);
                continue;
            }

            d.TinhTrang = TinhTrangDongKhach.ThemMoi;
            Tich(d, true);
        }
    }

    private static void Tich(DongKhachNhap dong, bool tich)
    {
        if (!dong.TuTayChon)
        {
            dong.Chon = tich;
        }
    }

    // ---------- Đọc file thành bảng chữ ----------

    private sealed record BangChu(string Ten, List<string[]> Dong, bool CoBangHang = false);

    private static BangChu DocExcel(string duongDanFile)
    {
        using var doc = File.OpenRead(duongDanFile);
        using var wb = WorkbookFactory.Create(doc);

        BangChu? chonTam = null;
        var coBangHang = false;

        for (var i = 0; i < wb.NumberOfSheets; i++)
        {
            var sheet = wb.GetSheetAt(i);
            var bang = new BangChu(sheet.SheetName, DocSheet(sheet));

            // Sheet nào có dòng tiêu đề nhận ra được thì lấy sheet đó, khỏi bắt người dùng
            // chọn sheet: file mẫu có thêm sheet "Hướng dẫn" nằm ngay bên cạnh.
            if (TimDongTieuDe(bang.Dong) is not null)
            {
                return bang;
            }

            // Phải soi hết các sheet mới biết đây là file hoá đơn: tờ hoá đơn cũ của cửa hàng
            // hay có sheet biểu đồ trống đứng đầu, bảng hàng nằm ở sheet thứ hai.
            coBangHang |= LaBangHoaDon(bang.Dong);

            if (chonTam is null || (SoDongCoChu(chonTam.Dong) == 0 && SoDongCoChu(bang.Dong) > 0))
            {
                chonTam = bang;
            }
        }

        var chon = chonTam ?? new BangChu(string.Empty, new List<string[]>());
        return chon with { CoBangHang = coBangHang };
    }

    private static int SoDongCoChu(List<string[]> dong)
    {
        return dong.Count(d => d.Any(o => o.Trim().Length > 0));
    }

    private static List<string[]> DocSheet(ISheet sheet)
    {
        var dong = new List<string[]>();

        for (var i = 0; i <= sheet.LastRowNum; i++)
        {
            var hang = sheet.GetRow(i);
            if (hang is null)
            {
                dong.Add(Array.Empty<string>());
                continue;
            }

            var soCot = Math.Min(Math.Max((int)hang.LastCellNum, 0), SoCotDoc);
            var o = new string[soCot];
            for (var c = 0; c < soCot; c++)
            {
                o[c] = ChuTrongO(hang.GetCell(c));
            }

            dong.Add(o);
        }

        return dong;
    }

    private static BangChu DocCsv(string duongDanFile)
    {
        var dong = new List<string[]>();
        var chuoi = File.ReadAllLines(duongDanFile);
        var dauTach = DauTachCsv(chuoi);

        foreach (var hang in chuoi)
        {
            dong.Add(TachDongCsv(hang, dauTach));
        }

        return new BangChu(Path.GetFileName(duongDanFile), dong);
    }

    /// <summary>Excel ở máy Việt Nam hay lưu CSV bằng dấu chấm phẩy, nên phải dò dấu tách.</summary>
    private static char DauTachCsv(IReadOnlyList<string> chuoi)
    {
        var mau = string.Join("\n", chuoi.Take(SoDongDoTieuDe));
        var dem = new[] { ';', '\t', ',' }.Select(d => (Dau: d, So: mau.Count(c => c == d))).ToList();
        var nhieuNhat = dem.OrderByDescending(x => x.So).First();
        return nhieuNhat.So > 0 ? nhieuNhat.Dau : ',';
    }

    private static string[] TachDongCsv(string hang, char dauTach)
    {
        var o = new List<string>();
        var dangTrongNgoac = false;
        var dem = new System.Text.StringBuilder();

        foreach (var c in hang)
        {
            if (c == '"')
            {
                dangTrongNgoac = !dangTrongNgoac;
            }
            else if (c == dauTach && !dangTrongNgoac)
            {
                o.Add(dem.ToString().Trim());
                dem.Clear();
            }
            else
            {
                dem.Append(c);
            }
        }

        o.Add(dem.ToString().Trim());
        return o.Take(SoCotDoc).ToArray();
    }

    private static string ChuTrongO(ICell? o)
    {
        if (o is null)
        {
            return string.Empty;
        }

        var loai = o.CellType == CellType.Formula ? o.CachedFormulaResultType : o.CellType;
        return loai switch
        {
            CellType.String => o.StringCellValue.Trim(),
            CellType.Numeric => SoTrongO(o),
            CellType.Boolean => o.BooleanCellValue ? "x" : string.Empty,
            _ => string.Empty,
        };
    }

    private static string SoTrongO(ICell o)
    {
        if (DateUtil.IsCellDateFormatted(o) && o.DateCellValue is { } ngay)
        {
            return ngay.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        var so = o.NumericCellValue;
        return Math.Abs(so - Math.Truncate(so)) < 0.0000001 && Math.Abs(so) < 1e15
            ? ((long)so).ToString(CultureInfo.InvariantCulture)
            : so.ToString("#,##0.##", CultureInfo.InvariantCulture);
    }

    private static string LayCot(string[] dong, int cot)
    {
        return cot >= 0 && cot < dong.Length ? dong[cot].Trim() : string.Empty;
    }

    /// <summary>
    /// Số điện thoại điền vào ô kiểu số thì Excel cắt mất số 0 đứng đầu. Thấy đúng 9 chữ số
    /// mà không có số 0 ở đầu thì trả lại số 0 cho khỏi phải sửa tay từng dòng.
    /// </summary>
    private static string SoDienThoai(string chu)
    {
        var gon = chu.Trim();
        return gon.Length == 9 && gon.All(char.IsDigit) && gon[0] != '0' ? "0" + gon : gon;
    }

    // ---------- Dò dòng tiêu đề ----------

    private sealed record ViTriTieuDe(int SoDong, int[] Cot);

    private static ViTriTieuDe? TimDongTieuDe(List<string[]> dong)
    {
        var het = Math.Min(dong.Count, SoDongDoTieuDe);

        for (var i = 0; i < het; i++)
        {
            var cot = new[] { -1, -1, -1, -1 };
            var soNhan = 0;

            for (var c = 0; c < dong[i].Length; c++)
            {
                if (NhanCot(dong[i][c]) is { } nhan && cot[nhan] < 0)
                {
                    cot[nhan] = c;
                    soNhan++;
                }
            }

            // Phải có cột tên khách mới coi là tiêu đề: một dòng dữ liệu bình thường không
            // có chữ "tên" nào, còn dòng chỉ có "địa chỉ" thì thường là địa chỉ cửa hàng
            // ghi ở đầu file.
            if (cot[0] >= 0 && soNhan >= 2)
            {
                return new ViTriTieuDe(i, cot);
            }
        }

        return null;
    }

    /// <summary>Nhãn cột đọc từ chữ trên dòng tiêu đề: 0 tên, 1 điện thoại, 2 địa chỉ, 3 ghi chú.</summary>
    private static int? NhanCot(string chu)
    {
        var s = GonLaiChu(chu);

        if (s.Length == 0)
        {
            return null;
        }

        // "TÊN HÀNG" của hoá đơn từng bị nhận là cột tên khách (cùng có chữ "tên"), thành ra
        // tờ hoá đơn có thêm cột "GHI CHÚ" là đủ hai nhãn và bị coi là danh sách khách.
        if (NhanBangHang.Any(n => s.Contains(n)))
        {
            return null;
        }

        if (s.Contains("dien thoai") || s.Contains("sdt") || s.Contains("so dt") || s == "dt"
            || s.Contains("phone") || s.Contains("mobile"))
        {
            return 1;
        }

        if (s.Contains("dia chi") || s.Contains("address"))
        {
            return 2;
        }

        if (s.Contains("ghi chu") || s.Contains("chu thich") || s.Contains("note"))
        {
            return 3;
        }

        if (s.Contains("ten") || s.Contains("khach") || s.Contains("name"))
        {
            return 0;
        }

        return null;
    }

    /// <summary>
    /// Nhãn cột của bảng hàng trên hoá đơn. Thấy chúng là biết đang xem tờ hoá đơn, không phải
    /// danh sách khách hàng.
    /// </summary>
    private static readonly string[] NhanBangHang =
    {
        "ten hang", "mat hang", "ten vat tu", "dvt", "don vi", "so luong", "don gia", "thanh tien",
    };

    /// <summary>Chữ hay gặp ở dòng tiêu đề hoặc dòng chốt của bảng, không phải tên khách.</summary>
    private static readonly string[] ChuKhongPhaiTen =
    {
        "tt", "stt", "so tt", "cong", "tong", "tong cong", "tong tien", "tong cong tien",
        "ten hang", "mat hang", "dvt", "don vi", "so luong", "don gia", "thanh tien",
        "ngay", "ghi chu", "tien bang chu", "nguoi mua hang", "nguoi ban hang",
    };

    /// <summary>
    /// Bảng này là bảng hàng của một tờ hoá đơn: có ít nhất hai nhãn cột kiểu tên hàng / đvt /
    /// số lượng / đơn giá nằm cùng một dòng.
    /// </summary>
    private static bool LaBangHoaDon(List<string[]> dong)
    {
        var het = Math.Min(dong.Count, SoDongDoTieuDe + 10);
        var nhanCaKhoi = new HashSet<string>();

        for (var i = 0; i < het; i++)
        {
            var nhanTrongDong = new HashSet<string>();
            foreach (var o in dong[i])
            {
                var chu = GonLaiChu(o);
                foreach (var nhan in NhanBangHang.Where(n => chu.Contains(n)))
                {
                    nhanTrongDong.Add(nhan);
                    nhanCaKhoi.Add(nhan);
                }
            }

            // Hai nhãn nằm cùng một dòng là dòng tiêu đề của bảng hàng, chắc chắn là hoá đơn.
            if (nhanTrongDong.Count >= 2)
            {
                return true;
            }
        }

        // Sheet biểu đồ của file hoá đơn cũ để mỗi nhãn một dòng, không thành dòng tiêu đề nào.
        return nhanCaKhoi.Count >= 3;
    }

    /// <summary>
    /// Ô này không phải tên khách: nhãn của tờ giấy ("ĐC: ...", "Tên khách hàng: ....."), dòng
    /// tiêu đề hay dòng chốt của bảng, hoặc chỉ là số thứ tự. Không xoá dòng đi — vẫn hiện lên
    /// bảng nhưng bỏ tích, để người dùng thấy phần mềm bỏ qua cái gì.
    /// </summary>
    private static bool KhongGiongTenKhach(string ten)
    {
        var gon = ten.Trim();
        if (gon.Length < 2)
        {
            return true;
        }

        // Chỗ để trống trên tờ in: "Tên khách hàng: .............", "Địa chỉ: ......".
        if (gon.Contains("..."))
        {
            return true;
        }

        // Nhãn đầu dòng của tờ giấy: "ĐC:", "ĐT:", "Kính gửi:", "Tên khách hàng:".
        var haiCham = gon.IndexOf(':');
        if (haiCham >= 0 && haiCham <= 25)
        {
            return true;
        }

        var canhSo = GonLaiChu(ten);
        return canhSo.Length == 0 || ChuKhongPhaiTen.Contains(canhSo);
    }

    /// <summary>Bỏ dấu, đổi mọi thứ không phải chữ cái thành khoảng trắng rồi gộp khoảng trắng.</summary>
    private static string GonLaiChu(string chu)
    {
        var s = ChuViet.BoDau(chu);
        s = new string(s.Select(c => char.IsLetter(c) ? c : ' ').ToArray());
        return string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // ---------- Sheet hướng dẫn trong file mẫu ----------

    private static void TrangHuongDan(IWorkbook wb, IFont fontDam)
    {
        var kieuDam = wb.CreateCellStyle();
        kieuDam.SetFont(fontDam);

        var sheet = wb.CreateSheet("Hướng dẫn");
        sheet.SetColumnWidth(0, 34 * 256);
        sheet.SetColumnWidth(1, 18 * 256);
        sheet.SetColumnWidth(2, 34 * 256);
        sheet.SetColumnWidth(3, 30 * 256);

        var loi = new[]
        {
            "CÁCH ĐIỀN FILE NÀY",
            string.Empty,
            $"1. Mở sheet \"{TenSheetMau}\" bên cạnh, điền mỗi khách một dòng, ngay dưới dòng tiêu đề.",
            "2. Giữ nguyên thứ tự cột: 1 tên khách hàng · 2 điện thoại · 3 địa chỉ · 4 ghi chú.",
            "3. Chỉ cột 1 (tên khách hàng) là bắt buộc, ba cột còn lại để trống được.",
            "4. Đừng xoá dòng tiêu đề — phần mềm đọc chữ ở dòng đó để biết cột nào là cột gì.",
            "5. Lưu lại rồi vào phần mềm bấm \"Nhập từ file\", chọn đúng file này.",
            string.Empty,
            "Phần mềm hiện bảng xem trước để soát lại từng dòng trước khi ghi vào sổ:",
            "khách đã có sẵn và dòng thiếu tên sẽ tự bỏ tích, sửa ngay trên bảng đó cũng được.",
            string.Empty,
            "VÍ DỤ (chỉ để xem, không cần xoá — phần mềm không đọc sheet này)",
        };

        var dong = 0;
        foreach (var chu in loi)
        {
            var o = sheet.CreateRow(dong++).CreateCell(0);
            o.SetCellValue(chu);
            if (chu.StartsWith("CÁCH", StringComparison.Ordinal) || chu.StartsWith("VÍ DỤ", StringComparison.Ordinal))
            {
                o.CellStyle = kieuDam;
            }
        }

        var hangTieuDe = sheet.CreateRow(dong++);
        for (var i = 0; i < TieuDeMau.Length; i++)
        {
            var o = hangTieuDe.CreateCell(i);
            o.SetCellValue(TieuDeMau[i]);
            o.CellStyle = kieuDam;
        }

        var viDu = new[]
        {
            new[] { "Anh Tuấn sắt Bình Minh", "0912345678", "12 Nguyễn Trãi, Hà Đông", "Khách quen, trả cuối tháng" },
            new[] { "Chị Hoa nước Cầu Giấy", "0987654321", "Số 5 Trần Duy Hưng", string.Empty },
        };

        foreach (var mau in viDu)
        {
            var hang = sheet.CreateRow(dong++);
            for (var i = 0; i < mau.Length; i++)
            {
                hang.CreateCell(i).SetCellValue(mau[i]);
            }
        }
    }
}
