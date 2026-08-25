using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Ngày và tháng viết trên giấy, chưa có năm. Mẫu giấy của cửa hàng không có chỗ ghi năm cho
/// từng dòng nên năm phải lấy từ ô chọn năm lúc nhập file.
/// </summary>
public readonly record struct NgayThangGiay(int Ngay, int Thang);

/// <summary>
/// Trang giấy này là trang đầu của tờ hoá đơn hay một trang nối tiếp. Mẫu của cửa hàng để hai
/// file riêng: <c>trang-1.xls</c> có phần đầu (tên cửa hàng, tên khách, địa chỉ) rồi mới đến
/// bảng hàng; <c>trang-sau.xls</c> chỉ có bảng, tiêu đề nằm ngay dòng đầu tiên.
/// </summary>
public enum LoaiTrangGiay
{
    /// <summary>Trang đầu: có phần đầu phía trên bảng nên đọc được tên khách và địa chỉ.</summary>
    Trang1,

    /// <summary>Trang thứ hai trở đi: chỉ có bảng hàng, không có tên khách.</summary>
    TrangSau,
}

/// <summary>Một bảng hàng đọc được trong file Excel (mỗi sheet là một bảng).</summary>
public sealed class TrangDoc
{
    public string TenSheet { get; set; } = string.Empty;

    /// <summary>Tên file đọc ra trang này, để lô nhiều trang nói rõ trang nào ở file nào.</summary>
    public string TenFile { get; set; } = string.Empty;

    /// <summary>Trang đầu của tờ hay trang nối tiếp — xét theo có phần đầu phía trên bảng hay không.</summary>
    public LoaiTrangGiay Loai { get; set; }

    /// <summary>Dòng tiêu đề bảng hàng (đánh số từ 0). Bằng 0 là trang nối tiếp.</summary>
    public int DongTieuDe { get; set; }

    public string? TenKhach { get; set; }

    public string? DiaChi { get; set; }

    public DateTime? NgayTrenHoaDon { get; set; }

    /// <summary>Ngày trong tháng đọc ở dòng "Ngày … tháng … năm …", nếu có.</summary>
    public int? NgayTrongThang { get; set; }

    /// <summary>Tháng đọc ở dòng "Ngày … tháng … năm …", nếu có.</summary>
    public int? ThangTrenGiay { get; set; }

    /// <summary>
    /// Năm ghi trên giấy. Mẫu trắng của cửa hàng chỉ in "năm 20........." nên chỗ này thường
    /// trống — năm phải lấy từ ô chọn năm lúc nhập file.
    /// </summary>
    public int? NamTrenGiay { get; set; }

    /// <summary>
    /// Tờ giấy này là hoá đơn hoàn hàng (nhận ra ở tên tờ in phía trên bảng). Số lượng trên
    /// giấy là số dương, đọc vào sổ thì đổi lại thành số âm cho tự trừ vào nợ của khách.
    /// </summary>
    public bool LaHoanHang { get; set; }

    /// <summary>
    /// Mã hoá đơn bán ghi trên tờ hoàn ("Hoàn cho hoá đơn HD2026-02"). Có mã thì lúc nhập
    /// vào sổ, tờ hoàn nối lại được đúng hoá đơn nó hoàn cho; không có thì nó đứng riêng.
    /// </summary>
    public string? MaHoaDonGoc { get; set; }

    /// <summary>Lý do hoàn in trên tờ giấy (hàng lỗi, khách lấy thừa…), nếu có.</summary>
    public string? LyDoHoan { get; set; }

    public List<ChiTietHoaDon> Dong { get; } = new();

    /// <summary>
    /// Ngày/tháng đọc được cho từng dòng, khoá là vị trí dòng trong <see cref="Dong"/>. Cửa
    /// hàng hay viết mốc ngày ("1/12", "12\4") vào cột số thứ tự, các dòng từ đó xuống là hàng
    /// lấy hôm ấy. Giữ riêng ngày/tháng để đổi ô chọn năm là cả lô đổi năm theo.
    /// </summary>
    public Dictionary<int, NgayThangGiay> NgayThangCuaDong { get; } = new();

    public List<string> CanhBao { get; } = new();

    public decimal TongTien => Dong.Sum(d => d.ThanhTien);
}

/// <summary>Kết quả đọc một file hoá đơn Excel.</summary>
public sealed class KetQuaDocExcel
{
    public List<TrangDoc> Trang { get; } = new();

    public string? TenKhach => Trang
        .Select(t => t.TenKhach)
        .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

    public DateTime? NgayTrenHoaDon => Trang
        .Select(t => t.NgayTrenHoaDon)
        .FirstOrDefault(n => n is not null);

    public int TongSoDong => Trang.Sum(t => t.Dong.Count);

    /// <summary>Năm ghi trên giấy, lấy ở trang đầu tiên có ghi. Mẫu trắng thì không có.</summary>
    public int? NamTrenGiay => Trang
        .Select(t => t.NamTrenGiay)
        .FirstOrDefault(n => n is not null);

    /// <summary>Mã hoá đơn bán mà file này hoàn cho, lấy ở tờ đầu tiên có ghi.</summary>
    public string? MaHoaDonGoc => Trang
        .Select(t => t.MaHoaDonGoc)
        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

    /// <summary>Lý do hoàn ghi trong file, lấy ở tờ đầu tiên có ghi.</summary>
    public string? LyDoHoan => Trang
        .Select(t => t.LyDoHoan)
        .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
}

/// <summary>
/// Loại của nhóm bảng người dùng tích chọn để nhập: cả nhóm là tờ bán, cả nhóm là tờ hoàn,
/// hay lẫn lộn cả hai — lẫn lộn thì không nhập được vào cùng một hoá đơn.
/// </summary>
public sealed record LoaiToNhap(bool LaHoanHang, bool LonLoai)
{
    /// <summary>Chưa tích bảng nào.</summary>
    public static readonly LoaiToNhap KhongCo = new(false, false);

    /// <summary>
    /// Xét loại của nhóm bảng đang tích: tờ hoá đơn bán cộng vào nợ của khách, tờ hoàn trừ
    /// ra, nên trộn hai loại vào một hoá đơn là tờ giấy nói một chuyện mà sổ ghi chuyện khác.
    /// </summary>
    public static LoaiToNhap Xet(IEnumerable<TrangDoc> trang)
    {
        var dang = trang.ToList();
        if (dang.Count == 0)
        {
            return KhongCo;
        }

        var soHoan = dang.Count(t => t.LaHoanHang);
        return new LoaiToNhap(soHoan == dang.Count, soHoan > 0 && soHoan < dang.Count);
    }
}

/// <summary>
/// Đọc ngược file hoá đơn Excel (mẫu của cửa hàng, kể cả file cũ) thành danh sách dòng hàng.
/// Tự dò dòng tiêu đề bảng nên không phụ thuộc bảng nằm ở dòng nào.
/// </summary>
public static class DocHoaDon
{
    /// <summary>Số nhãn cột tối thiểu phải cùng nằm trên một dòng thì mới coi là tiêu đề bảng.</summary>
    private const int SoNhanToiThieu = 3;

    /// <summary>
    /// Bấy nhiêu dòng trống liền nhau thì coi như hết bảng. Mẫu giấy in sẵn số thứ tự cho cả
    /// trang nên không dừng ở đây thì đọc lố xuống phần chân tờ (dòng tổng, dòng ký tên).
    /// </summary>
    private const int SoDongTrongHetBang = 3;

    /// <summary>
    /// Dòng "Ngày … tháng …" ở chân tờ. Không đòi năm: mẫu giấy của cửa hàng in sẵn
    /// "năm 20........." nên tờ điền tay thường chỉ có ngày và tháng.
    /// </summary>
    private static readonly Regex MauNgayThang = new(
        @"ng[aà]y[\s.…]*(\d{1,2})\D+?th[aá]ng[\s.…]*(\d{1,2})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Mốc ngày viết ở cột số thứ tự: "1/12", "12\4", "5-11". Không nhận dấu chấm để khỏi đọc
    /// một số lẻ ("1.5") thành ngày, và không nhận chữ nào khác ngoài hai con số với dấu tách.
    /// </summary>
    private static readonly Regex MauMocNgay = new(
        @"^\s*(\d{1,2})\s*[/\\-]\s*(\d{1,2})\s*$",
        RegexOptions.Singleline);

    /// <summary>Năm ghi trên tờ, nếu người viết có điền đủ bốn chữ số.</summary>
    private static readonly Regex MauNam = new(
        @"n[aă]m[\s.…]*(\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>Phần trong ngoặc dưới tên tờ hoàn hàng: "(Hoàn cho hoá đơn HD2026-02 ngày … — lý do)".</summary>
    private static readonly Regex MauTrongNgoac = new(@"\(([^)]*)\)", RegexOptions.Singleline);

    /// <summary>Câu "hoàn cho hoá đơn &lt;mã&gt;" — viết có dấu hay không dấu đều nhận.</summary>
    private static readonly Regex MauHoanCho = new(
        @"ho[àa]n\s+cho\s+(?:ho[áa]\s*[đd][ơo]n|h[đd])\s*:?\s*([^\s)]+)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Đọc một file hoá đơn Excel. <paramref name="namChon"/> là năm người dùng chọn lúc nhập
    /// file: mẫu giấy của cửa hàng chỉ in "năm 20........." nên ngày trên tờ thiếu năm, phải
    /// ghép năm đã chọn vào mới ra được ngày đầy đủ.
    /// </summary>
    public static KetQuaDocExcel Doc(string duongDanFile, DateTime ngayChoDongHang, int? namChon = null)
    {
        var ketQua = new KetQuaDocExcel();
        var tenFile = Path.GetFileName(duongDanFile);

        using var doc = File.OpenRead(duongDanFile);
        using var wb = WorkbookFactory.Create(doc);

        for (var i = 0; i < wb.NumberOfSheets; i++)
        {
            var trang = DocMotSheet(wb.GetSheetAt(i), ngayChoDongHang, namChon);
            if (trang is not null)
            {
                trang.TenFile = tenFile;
                ketQua.Trang.Add(trang);
            }
        }

        return ketQua;
    }

    private static TrangDoc? DocMotSheet(ISheet sheet, DateTime ngayChoDongHang, int? namChon)
    {
        var (dongTieuDe, cot) = TimTieuDe(sheet);
        if (dongTieuDe < 0 || cot is null)
        {
            return null;
        }

        // Tiêu đề bảng nằm ngay dòng đầu là mẫu trang sau; có dòng nào phía trên thì đó là phần
        // đầu của trang 1 (tên cửa hàng, "Tên khách hàng:", "Địa chỉ:").
        var trang = new TrangDoc
        {
            TenSheet = sheet.SheetName,
            DongTieuDe = dongTieuDe,
            Loai = dongTieuDe == 0 ? LoaiTrangGiay.TrangSau : LoaiTrangGiay.Trang1,
        };

        DocPhanDau(sheet, dongTieuDe, trang);
        DocPhanBang(sheet, dongTieuDe, cot, trang, ngayChoDongHang, namChon);
        DocNgayThang(sheet, dongTieuDe, trang, namChon);

        return trang.Dong.Count > 0 ? trang : null;
    }

    private static void DocPhanDau(ISheet sheet, int dongTieuDe, TrangDoc trang)
    {
        for (var r = 0; r < dongTieuDe; r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                continue;
            }

            for (var c = hang.FirstCellNum; c < hang.LastCellNum && c >= 0; c++)
            {
                var chu = LayChu(hang, c);
                if (chu.Length == 0)
                {
                    continue;
                }

                var khongDau = ChuViet.BoDau(chu);
                if (khongDau.Contains("hoan hang"))
                {
                    trang.LaHoanHang = true;
                }

                // Mẫu giấy đang dùng gộp "hoàn cho hoá đơn nào" vào cùng ô tên tờ, mẫu cũ có
                // dòng phụ đề riêng — đọc cả hai chỗ để file nào cũng lấy lại được hoá đơn gốc.
                // Tờ hoàn đứng riêng ở mẫu cũ thì dòng phụ đề chỉ còn "(hàng lỗi)", chẳng có chữ
                // nào để nhận ra, nên câu trong ngoặc dưới tên tờ hoàn cũng đọc luôn.
                if (khongDau.Contains("hoan hang")
                    || khongDau.Contains("hoan cho")
                    || (trang.LaHoanHang && chu.TrimStart().StartsWith('(')))
                {
                    DocPhuDeHoan(chu, trang);
                }

                if (trang.TenKhach is null && khongDau.Contains("ten khach hang"))
                {
                    trang.TenKhach = CatSauDauHaiCham(chu);
                }
                else if (trang.DiaChi is null && khongDau.StartsWith("dia chi", StringComparison.Ordinal))
                {
                    trang.DiaChi = CatSauDauHaiCham(chu);
                }
            }
        }
    }

    /// <summary>
    /// Đọc dòng chữ trong ngoặc dưới tên tờ hoàn hàng để lấy lại mã hoá đơn gốc và lý do hoàn.
    /// Nhờ vậy file Excel của tờ hoàn là chứng từ đủ: nhập vào máy khác vẫn biết nó hoàn cho
    /// hoá đơn nào, vì sao hoàn — không phải gõ lại bằng tay.
    /// </summary>
    private static void DocPhuDeHoan(string chu, TrangDoc trang)
    {
        var trongNgoac = MauTrongNgoac.Match(chu);
        var noiDung = (trongNgoac.Success ? trongNgoac.Groups[1].Value : chu).Trim();

        var hoanCho = MauHoanCho.Match(noiDung);
        if (hoanCho.Success)
        {
            trang.MaHoaDonGoc ??= hoanCho.Groups[1].Value.Trim().Trim(',', '.', ';');
        }

        if (!trongNgoac.Success)
        {
            return;
        }

        // Lý do nằm sau dấu gạch ngang dài, nhưng chỉ khi câu có cả mã hoá đơn gốc: không có mã
        // thì cả câu trong ngoặc là lý do, cắt ở dấu gạch là mất nửa câu ("(hàng lỗi — sứt vòi)").
        // Câu mặc định "(Khách trả lại hàng)" chỉ là tên tờ nói lại nên bỏ.
        var viTriGach = hoanCho.Success ? noiDung.IndexOfAny(new[] { '—', '–' }) : -1;
        var lyDo = viTriGach >= 0
            ? noiDung[(viTriGach + 1)..]
            : hoanCho.Success ? string.Empty : noiDung;

        lyDo = lyDo.Trim();
        if (lyDo.Length > 0 && ChuViet.BoDau(lyDo) != "khach tra lai hang")
        {
            trang.LyDoHoan ??= lyDo;
        }
    }

    private static void DocPhanBang(
        ISheet sheet,
        int dongTieuDe,
        CotBang cot,
        TrangDoc trang,
        DateTime ngayChoDongHang,
        int? namChon)
    {
        var soDongTrong = 0;

        // Mốc ngày đang có hiệu lực: cửa hàng viết "1/12" vào cột số thứ tự thì từ dòng đó
        // xuống là hàng lấy hôm ấy, tới khi gặp mốc khác.
        NgayThangGiay? mocNgay = null;
        var namChoMoc = namChon ?? ngayChoDongHang.Year;

        for (var r = dongTieuDe + 1; r <= sheet.LastRowNum; r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                if (++soDongTrong >= SoDongTrongHetBang)
                {
                    break;
                }

                continue;
            }

            var tenHang = LayChu(hang, cot.TenHang);
            var khongDau = ChuViet.BoDau(LayChu(hang, cot.TT) + " " + tenHang);

            // Mốc ngày viết ở cột số thứ tự. Đọc trước khi xét dòng trống: cửa hàng có lúc để
            // mốc ngày đứng riêng một dòng, dòng đó không có hàng nhưng vẫn phải nhớ lấy ngày.
            if (MocNgayTrongO(hang, cot.TT) is { } moc)
            {
                mocNgay = moc;
                soDongTrong = 0;
            }

            // Chạm dòng tổng là hết bảng.
            if (khongDau.Contains("tong cong") || khongDau.Contains("cong trang") || khongDau.Contains("bang chu"))
            {
                break;
            }

            var soLuong = LaySo(hang, cot.SoLuong);
            var donGia = LaySo(hang, cot.DonGia);
            var thanhTien = LaySo(hang, cot.ThanhTien);

            // Dòng tổng không có nhãn: mẫu cũ gộp ô đầu dòng rồi để trống, chỉ còn số tiền ở
            // cột THÀNH TIỀN. Cũng đúng chỗ mẫu mới ghi tiền cộng sang từ tờ trước — lấy vào
            // là sinh ra một mặt hàng không tên và cộng tiền của tờ trước thêm một lần nữa.
            if (tenHang.Length == 0 && soLuong == 0 && donGia == 0 && thanhTien != 0)
            {
                break;
            }

            // Dòng của mẫu in sẵn chưa điền gì: mẫu trang 1 in trước số thứ tự 1..26 và công
            // thức thành tiền ra 0 cho cả trang, nên có chữ ở cột TT không có nghĩa là có hàng.
            if (tenHang.Length == 0 && soLuong == 0 && thanhTien == 0)
            {
                if (++soDongTrong >= SoDongTrongHetBang)
                {
                    break;
                }

                continue;
            }

            soDongTrong = 0;

            // Cửa hàng có lúc viết số lượng mà bỏ trống tên hàng. Vẫn lấy dòng đó vào để người
            // dùng điền tên ngay trên bảng xem trước, chứ bỏ im là mất hàng mà không ai biết.
            if (tenHang.Length == 0)
            {
                trang.CanhBao.Add($"Dòng {r + 1}: có số lượng mà thiếu tên hàng — điền tên trước khi nhập.");
            }

            // Tờ hoàn hàng in số dương; vào sổ thì là hàng trả về nên đổi dấu ngay ở đây, để
            // mọi phép tính bên dưới (kể cả tự tính đơn giá từ thành tiền) làm như dòng trả lại.
            if (trang.LaHoanHang)
            {
                soLuong = -soLuong;
                thanhTien = -thanhTien;
            }

            // Hoá đơn cũ hay chỉ ghi thành tiền mà bỏ trống đơn giá.
            // Dòng trả lại có số lượng và thành tiền cùng âm nên chia ra vẫn đúng đơn giá.
            if (donGia == 0 && thanhTien != 0 && soLuong != 0)
            {
                donGia = Math.Round(thanhTien / soLuong, 0, MidpointRounding.AwayFromZero);
                trang.CanhBao.Add($"\"{tenHang}\": thiếu đơn giá, đã tự tính {So.Tien(donGia)} từ thành tiền.");
            }

            if (soLuong == 0)
            {
                trang.CanhBao.Add($"\"{tenHang}\": không có số lượng, tạm để 0 — cần sửa lại sau khi nhập.");
            }

            // Dòng nào nằm dưới một mốc ngày thì mang ngày của mốc đó, chứ không phải ngày
            // người dùng đặt chung cho cả lô — tờ hoá đơn mối gom hàng của nhiều ngày.
            if (mocNgay is { } ngayGiay)
            {
                trang.NgayThangCuaDong[trang.Dong.Count] = ngayGiay;
            }

            trang.Dong.Add(new ChiTietHoaDon
            {
                Ngay = mocNgay is { } m ? NgayHopLe(namChoMoc, m.Thang, m.Ngay) : ngayChoDongHang,
                TenHang = tenHang,
                DonVi = LayChu(hang, cot.DonVi),
                SoLuong = soLuong,
                DonGia = donGia,
            });
        }
    }

    /// <summary>
    /// Đọc dòng "Ngày … tháng … năm …" ở chân tờ. Năm trên giấy hay bị bỏ trống nên ghép
    /// <paramref name="namChon"/> — năm người dùng chọn lúc nhập file — vào ngày và tháng đọc
    /// được. Năm ghi trên giấy vẫn giữ riêng để màn hình nhập nói được là hai bên lệch nhau.
    /// </summary>
    private static void DocNgayThang(ISheet sheet, int dongTieuDe, TrangDoc trang, int? namChon)
    {
        for (var r = dongTieuDe; r <= sheet.LastRowNum; r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                continue;
            }

            for (var c = hang.FirstCellNum; c < hang.LastCellNum && c >= 0; c++)
            {
                var chu = LayChu(hang, c);
                var khop = MauNgayThang.Match(chu);
                if (!khop.Success)
                {
                    continue;
                }

                var ngay = int.Parse(khop.Groups[1].Value, CultureInfo.InvariantCulture);
                var thang = int.Parse(khop.Groups[2].Value, CultureInfo.InvariantCulture);
                if (ngay is < 1 or > 31 || thang is < 1 or > 12)
                {
                    continue;
                }

                trang.NgayTrongThang = ngay;
                trang.ThangTrenGiay = thang;

                var khopNam = MauNam.Match(chu[khop.Length..]);
                if (khopNam.Success)
                {
                    var nam = int.Parse(khopNam.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (nam is >= 2000 and <= 2100)
                    {
                        trang.NamTrenGiay = nam;
                    }
                }

                if ((trang.NamTrenGiay ?? namChon) is { } namDung)
                {
                    trang.NgayTrenHoaDon = NgayHopLe(namDung, thang, ngay);
                }

                return;
            }
        }
    }

    /// <summary>
    /// Mốc ngày cửa hàng viết ở cột số thứ tự thay cho con số. Excel có thể đã tự đổi ô đó
    /// thành ô ngày thật (gõ "1/12" vào ô kiểu ngày) nên đọc cả ô ngày lẫn ô chữ.
    /// </summary>
    private static NgayThangGiay? MocNgayTrongO(IRow hang, int cot)
    {
        if (cot < 0)
        {
            return null;
        }

        var o = hang.GetCell(cot);
        if (o is null)
        {
            return null;
        }

        var loai = o.CellType == CellType.Formula ? o.CachedFormulaResultType : o.CellType;

        if (loai == CellType.Numeric)
        {
            return DateUtil.IsCellDateFormatted(o)
                ? new NgayThangGiay(o.DateCellValue.Day, o.DateCellValue.Month)
                : null;
        }

        if (loai != CellType.String)
        {
            return null;
        }

        var khop = MauMocNgay.Match(o.StringCellValue);
        if (!khop.Success)
        {
            return null;
        }

        var ngay = int.Parse(khop.Groups[1].Value, CultureInfo.InvariantCulture);
        var thang = int.Parse(khop.Groups[2].Value, CultureInfo.InvariantCulture);

        // Viết theo lối Việt Nam là ngày trước tháng sau. Hai con số mà không ra ngày tháng nào
        // hợp lệ thì để yên, đó chỉ là số thứ tự viết lạ.
        return ngay is >= 1 and <= 31 && thang is >= 1 and <= 12
            ? new NgayThangGiay(ngay, thang)
            : null;
    }

    /// <summary>Ghép năm, tháng, ngày lại; ngày 31 của tháng chỉ có 30 thì lùi về ngày cuối tháng.</summary>
    private static DateTime NgayHopLe(int nam, int thang, int ngay)
    {
        return new DateTime(nam, thang, Math.Min(ngay, DateTime.DaysInMonth(nam, thang)));
    }

    /// <summary>
    /// Dò dòng tiêu đề bảng: phải có ít nhất <see cref="SoNhanToiThieu"/> nhãn cột nằm cùng
    /// một dòng và bắt buộc có cột tên hàng. Nhờ vậy bỏ qua được sheet biểu đồ hoặc sheet rác.
    /// </summary>
    private static (int Dong, CotBang? Cot) TimTieuDe(ISheet sheet)
    {
        var het = Math.Min(sheet.LastRowNum, 40);
        for (var r = 0; r <= het; r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                continue;
            }

            var (cot, soNhan) = MapCot(hang);
            if (soNhan >= SoNhanToiThieu && cot.TenHang >= 0)
            {
                return (r, cot);
            }
        }

        return (-1, null);
    }

    private static (CotBang Cot, int SoNhan) MapCot(IRow tieuDe)
    {
        var cot = new CotBang();
        var soNhan = 0;

        for (var c = tieuDe.FirstCellNum; c < tieuDe.LastCellNum && c >= 0; c++)
        {
            var chu = ChuViet.BoDau(LayChu(tieuDe, c)).Replace('\n', ' ').Replace("  ", " ").Trim();
            if (chu.Length == 0)
            {
                continue;
            }

            if ((chu == "tt" || chu == "stt") && cot.TT < 0)
            {
                cot.TT = c;
            }
            else if (chu.Contains("ten hang") && cot.TenHang < 0)
            {
                cot.TenHang = c;
            }
            else if ((chu.Contains("dvt") || chu.Contains("don vi")) && cot.DonVi < 0)
            {
                cot.DonVi = c;
            }
            else if (chu.Contains("so luong") && cot.SoLuong < 0)
            {
                cot.SoLuong = c;
            }
            else if (chu.Contains("don gia") && cot.DonGia < 0)
            {
                cot.DonGia = c;
            }
            else if (chu.Contains("thanh tien") && cot.ThanhTien < 0)
            {
                cot.ThanhTien = c;
            }
            else
            {
                continue;
            }

            soNhan++;
        }

        return (cot, soNhan);
    }

    private static string CatSauDauHaiCham(string chu)
    {
        var viTri = chu.IndexOf(':');
        var phan = viTri >= 0 ? chu[(viTri + 1)..] : chu;
        return phan.Trim().Trim('.', '…', ' ').Trim();
    }

    private static string LayChu(IRow hang, int cot)
    {
        if (cot < 0)
        {
            return string.Empty;
        }

        var o = hang.GetCell(cot);
        if (o is null)
        {
            return string.Empty;
        }

        var loai = o.CellType == CellType.Formula ? o.CachedFormulaResultType : o.CellType;
        return loai switch
        {
            // Có tờ của cửa hàng lưu chữ Việt ở dạng tổ hợp: "Ngày" là N, g, a rồi dấu huyền
            // rời ra một ký tự. Trông y hệt chữ thường mà so từng ký tự thì trượt hết, nên dồn
            // về một dạng ngay lúc đọc — dòng "Ngày … tháng …" ở chân tờ mới đọc ra được.
            CellType.String => o.StringCellValue.Trim().Normalize(NormalizationForm.FormC),
            CellType.Numeric => o.NumericCellValue.ToString("#,##0.##", CultureInfo.InvariantCulture),
            CellType.Boolean => o.BooleanCellValue.ToString(),
            _ => string.Empty,
        };
    }

    private static decimal LaySo(IRow hang, int cot)
    {
        if (cot < 0)
        {
            return 0m;
        }

        var o = hang.GetCell(cot);
        if (o is null)
        {
            return 0m;
        }

        var loai = o.CellType == CellType.Formula ? o.CachedFormulaResultType : o.CellType;
        if (loai == CellType.Numeric)
        {
            return (decimal)o.NumericCellValue;
        }

        return loai == CellType.String && So.TryDoc(o.StringCellValue, out var giaTri) ? giaTri : 0m;
    }

    private sealed class CotBang
    {
        public int TT { get; set; } = -1;

        public int TenHang { get; set; } = -1;

        public int DonVi { get; set; } = -1;

        public int SoLuong { get; set; } = -1;

        public int DonGia { get; set; } = -1;

        public int ThanhTien { get; set; } = -1;
    }
}
