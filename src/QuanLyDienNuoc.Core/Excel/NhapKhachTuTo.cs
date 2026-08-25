using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Excel;

/// <summary>
/// Kết quả xét một lô trang giấy để ra <b>một</b> khách hàng: tên và địa chỉ đọc ở trang 1,
/// khách cũ trùng tên, và những chỗ chặn không cho ghi vào sổ.
/// </summary>
public sealed record XetToKhach(
    XetThuTuTrang ThuTu,
    string? TenTrenGiay,
    string? DiaChiTrenGiay,
    KhachHang? KhachTrung,
    bool CoToHoan)
{
    /// <summary>Chưa tích trang nào.</summary>
    public static readonly XetToKhach KhongCo = new(XetThuTuTrang.KhongCo, null, null, null, false);

    /// <summary>Câu chặn không cho nhập, hoặc null nếu lô dùng được.</summary>
    public string? Chan => CoToHoan ? NhapKhachTuTo.ChanToHoan : ThuTu.Chan;

    /// <summary>
    /// Câu nhắc khi lô vẫn nhập được nhưng có chỗ người dùng phải tự điền: không có trang 1,
    /// hay trang 1 để trống chỗ tên khách, thì tên khách phải gõ tay.
    /// </summary>
    public string? Nhac
    {
        get
        {
            if (Chan is not null || ThuTu.SoTrang == 0 || TenTrenGiay is not null)
            {
                return null;
            }

            return ThuTu.CoTrang1
                ? "Trang 1 để trống chỗ \"Tên khách hàng\" nên không đọc được tên — hãy gõ tên "
                  + "vào ô TÊN KHÁCH HÀNG."
                : "Lô chưa có trang 1 nên không đọc được tên khách. Trang 1 là trang có "
                  + "\"Tên khách hàng\" ở đầu — thêm nó vào lô, hoặc gõ tên vào ô TÊN KHÁCH HÀNG.";
        }
    }
}

/// <summary>
/// Nhập <b>một</b> khách hàng từ file: một tờ hoá đơn của cửa hàng là của đúng một khách, tên
/// khách ghi ở đầu trang 1 và các dòng hàng nằm ở cả tờ. Màn hình nhập gom các file lại thành
/// một lô trang (file đầu theo mẫu <c>trang-1.xls</c>, các file sau theo mẫu
/// <c>trang-sau.xls</c>) rồi ghi vào sổ một khách kèm hoá đơn của khách đó.
/// <para>
/// Phần đọc file dùng chung <see cref="DocHoaDon"/>, thứ tự trang dùng chung
/// <see cref="ThuTuTrangGiay"/> với màn nhập hoá đơn cho khách đã có — chỗ này chỉ thêm việc
/// chấm tên khách đọc được trên giấy.
/// </para>
/// </summary>
public static class NhapKhachTuTo
{
    /// <summary>
    /// Tờ hoàn hàng là tờ hoàn cho một hoá đơn đã có, nên nó không mở đầu sổ của một khách mới:
    /// hoàn trước cả khi mua thì nợ của khách thành số âm mà chẳng có hoá đơn nào để đối chiếu.
    /// </summary>
    public const string ChanToHoan =
        "Lô đang có tờ hoàn hàng. Tờ hoàn là tờ hoàn cho một hoá đơn đã có nên không nhập được "
        + "cùng lúc với khách mới — hãy nhập tờ bán hàng trước, rồi vào Đơn hàng của khách và "
        + "bấm \"Nhập từ Excel\" để nhập tờ hoàn.";

    /// <summary>Tên file mẫu ghi ra khi người dùng bấm "Tải file mẫu...".</summary>
    public const string TenFileMauTrang1 = "Mau-hoa-don-trang-1.xls";

    /// <summary>Tên file mẫu của trang thứ hai trở đi.</summary>
    public const string TenFileMauTrangSau = "Mau-hoa-don-trang-sau.xls";

    /// <summary>
    /// Những ô không phải tên khách, so sau khi bỏ dấu: nhãn bảng hàng và dòng chốt của tờ
    /// giấy lọt vào ô tên khách thì thành một khách rác trong sổ.
    /// </summary>
    private static readonly string[] ChuKhongPhaiTen =
    {
        "tt", "stt", "so tt", "cong", "tong", "tong cong", "tong tien", "tong cong tien",
        "ten hang", "mat hang", "dvt", "don vi", "so luong", "don gia", "thanh tien",
        "ngay", "ghi chu", "tien bang chu", "nguoi mua hang", "nguoi ban hang",
    };

    /// <summary>
    /// Xét nhóm trang đang tích: tên và địa chỉ lấy ở trang 1 (các trang sau không có phần đầu
    /// nên chẳng có gì để lấy), kèm khách cũ trùng tên nếu có.
    /// </summary>
    public static XetToKhach Xet(IEnumerable<TrangDoc> dangTich, IEnumerable<KhachHang> khachDaCo)
    {
        var trang = dangTich.ToList();
        if (trang.Count == 0)
        {
            return XetToKhach.KhongCo;
        }

        var thuTu = ThuTuTrangGiay.Xet(trang);
        var ten = TenDungDuoc(thuTu.TenKhach);

        // Địa chỉ đọc ở đúng trang 1 mà ThuTuTrangGiay đã lấy tên, chứ không phải trang nào có
        // chữ "Địa chỉ" cũng được — trang sau không có phần đầu.
        var diaChi = DiaChiDungDuoc(trang.FirstOrDefault(t => t.Loai == LoaiTrangGiay.Trang1)?.DiaChi);

        return new XetToKhach(
            thuTu,
            ten,
            diaChi,
            ten is null ? null : KiemTra.KhachTrungTen(khachDaCo, ten),
            trang.Any(t => t.LaHoanHang));
    }

    /// <summary>
    /// Ô này dùng được làm tên khách hay không: chỗ để trống của tờ in ("....."), nhãn bảng
    /// hàng hay dòng chốt thì không — ghi vào sổ là một khách rác không ai nhận ra.
    /// </summary>
    public static bool GiongTenKhach(string? ten)
    {
        var gon = (ten ?? string.Empty).Trim();
        if (gon.Length < 2 || gon.Contains("..."))
        {
            return false;
        }

        // Nhãn đầu dòng của tờ giấy còn sót lại: "ĐC: ...", "Kính gửi: ...".
        var haiCham = gon.IndexOf(':');
        if (haiCham >= 0 && haiCham <= 25)
        {
            return false;
        }

        var canhSo = GonLaiChu(gon);
        return canhSo.Length > 0 && !ChuKhongPhaiTen.Contains(canhSo);
    }

    /// <summary>
    /// Ghi hai file mẫu ra một thư mục: mẫu trang 1 (có phần đầu với tên khách, bảng hàng đánh
    /// số thứ tự) và mẫu trang sau. Đây chính là mẫu giấy cửa hàng đang dùng, không phải một
    /// mẫu khác nghĩ ra — điền vào rồi nhập lại là khớp đúng chỗ.
    /// </summary>
    public static (string Trang1, string TrangSau) XuatFileMau(string thuMucRa, string? thuMucMau = null)
    {
        var nguon = thuMucMau ?? MauHoaDon.ThuMucMacDinh;
        Directory.CreateDirectory(thuMucRa);

        var trang1 = Path.Combine(thuMucRa, TenFileMauTrang1);
        var trangSau = Path.Combine(thuMucRa, TenFileMauTrangSau);

        File.Copy(Path.Combine(nguon, MauHoaDon.TenFileTrang1), trang1, overwrite: true);
        File.Copy(Path.Combine(nguon, MauHoaDon.TenFileTrangSau), trangSau, overwrite: true);

        return (trang1, trangSau);
    }

    /// <summary>Tên đọc trên giấy, hoặc null nếu chỗ đó bỏ trống / không phải tên khách.</summary>
    private static string? TenDungDuoc(string? ten)
    {
        var gon = (ten ?? string.Empty).Trim();
        return GiongTenKhach(gon) ? gon : null;
    }

    /// <summary>Địa chỉ đọc trên giấy, bỏ đi chỗ để trống in sẵn ("Địa chỉ: ......").</summary>
    private static string? DiaChiDungDuoc(string? diaChi)
    {
        var gon = (diaChi ?? string.Empty).Trim();
        return gon.Length >= 2 && !gon.Contains("...") ? gon : null;
    }

    /// <summary>Bỏ dấu, đổi mọi thứ không phải chữ cái thành khoảng trắng rồi gộp khoảng trắng.</summary>
    private static string GonLaiChu(string chu)
    {
        var s = ChuViet.BoDau(chu);
        s = new string(s.Select(c => char.IsLetter(c) ? c : ' ').ToArray());
        return string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
