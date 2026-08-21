using System.Globalization;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>Một bảng hàng đọc được trong file Excel (mỗi sheet là một bảng).</summary>
public sealed class TrangDoc
{
    public string TenSheet { get; set; } = string.Empty;

    public string? TenKhach { get; set; }

    public string? DiaChi { get; set; }

    public DateTime? NgayTrenHoaDon { get; set; }

    /// <summary>
    /// Tờ giấy này là hoá đơn hoàn hàng (nhận ra ở tên tờ in phía trên bảng). Số lượng trên
    /// giấy là số dương, đọc vào sổ thì đổi lại thành số âm cho tự trừ vào nợ của khách.
    /// </summary>
    public bool LaHoanHang { get; set; }

    public List<ChiTietHoaDon> Dong { get; } = new();

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
}

/// <summary>
/// Đọc ngược file hoá đơn Excel (mẫu của cửa hàng, kể cả file cũ) thành danh sách dòng hàng.
/// Tự dò dòng tiêu đề bảng nên không phụ thuộc bảng nằm ở dòng nào.
/// </summary>
public static class DocHoaDon
{
    /// <summary>Số nhãn cột tối thiểu phải cùng nằm trên một dòng thì mới coi là tiêu đề bảng.</summary>
    private const int SoNhanToiThieu = 3;

    private static readonly Regex MauNgay = new(
        @"ng[aà]y\s*\.*\s*(\d{1,2})\D+th[aá]ng\s*\.*\s*(\d{1,2})\D+n[aă]m\s*\.*\s*(\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static KetQuaDocExcel Doc(string duongDanFile, DateTime ngayChoDongHang)
    {
        var ketQua = new KetQuaDocExcel();

        using var doc = File.OpenRead(duongDanFile);
        using var wb = WorkbookFactory.Create(doc);

        for (var i = 0; i < wb.NumberOfSheets; i++)
        {
            var trang = DocMotSheet(wb.GetSheetAt(i), ngayChoDongHang);
            if (trang is not null)
            {
                ketQua.Trang.Add(trang);
            }
        }

        return ketQua;
    }

    private static TrangDoc? DocMotSheet(ISheet sheet, DateTime ngayChoDongHang)
    {
        var (dongTieuDe, cot) = TimTieuDe(sheet);
        if (dongTieuDe < 0 || cot is null)
        {
            return null;
        }

        var trang = new TrangDoc { TenSheet = sheet.SheetName };

        DocPhanDau(sheet, dongTieuDe, trang);
        DocPhanBang(sheet, dongTieuDe, cot, trang, ngayChoDongHang);
        DocNgayThang(sheet, dongTieuDe, trang);

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

    private static void DocPhanBang(
        ISheet sheet,
        int dongTieuDe,
        CotBang cot,
        TrangDoc trang,
        DateTime ngayChoDongHang)
    {
        for (var r = dongTieuDe + 1; r <= sheet.LastRowNum; r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                continue;
            }

            var tenHang = LayChu(hang, cot.TenHang);
            var khongDau = ChuViet.BoDau(LayChu(hang, cot.TT) + " " + tenHang);

            // Chạm dòng tổng là hết bảng.
            if (khongDau.Contains("tong cong") || khongDau.Contains("cong trang") || khongDau.Contains("bang chu"))
            {
                break;
            }

            if (tenHang.Length == 0)
            {
                continue;
            }

            var soLuong = LaySo(hang, cot.SoLuong);
            var donGia = LaySo(hang, cot.DonGia);
            var thanhTien = LaySo(hang, cot.ThanhTien);

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

            trang.Dong.Add(new ChiTietHoaDon
            {
                Ngay = ngayChoDongHang,
                TenHang = tenHang,
                DonVi = LayChu(hang, cot.DonVi),
                SoLuong = soLuong,
                DonGia = donGia,
            });
        }
    }

    private static void DocNgayThang(ISheet sheet, int dongTieuDe, TrangDoc trang)
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
                var khop = MauNgay.Match(LayChu(hang, c));
                if (!khop.Success)
                {
                    continue;
                }

                var ngay = int.Parse(khop.Groups[1].Value, CultureInfo.InvariantCulture);
                var thang = int.Parse(khop.Groups[2].Value, CultureInfo.InvariantCulture);
                var nam = int.Parse(khop.Groups[3].Value, CultureInfo.InvariantCulture);

                if (ngay is >= 1 and <= 31 && thang is >= 1 and <= 12 && nam is >= 2000 and <= 2100)
                {
                    trang.NgayTrenHoaDon = new DateTime(nam, thang, ngay);
                    return;
                }
            }
        }
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
            CellType.String => o.StringCellValue.Trim(),
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
