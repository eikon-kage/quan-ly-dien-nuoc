using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChamCong.Models;

namespace ChamCong.Data;

/// <summary>
/// Kho dữ liệu chấm công. Dữ liệu nằm trong bộ nhớ và được ghi ra một file JSON sau
/// mỗi thay đổi, giống cách app quản lý điện nước làm.
/// </summary>
public sealed class KhoChamCong
{
    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Tạo kho trỏ vào một file bất kỳ. Ứng dụng dùng <see cref="DuongDanMacDinh"/>;
    /// hàm dựng này để test có thể trỏ vào thư mục tạm thay vì dữ liệu thật.
    /// </summary>
    public KhoChamCong(string duongDanFile)
    {
        DuongDanFile = duongDanFile;

        var thuMuc = Path.GetDirectoryName(duongDanFile);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }
    }

    public string DuongDanFile { get; }

    public DuLieuChamCong DuLieu { get; private set; } = new();

    /// <summary>Chỗ để file dữ liệu: thư mục dữ liệu của ứng dụng trên máy đang chạy.</summary>
    public static string DuongDanMacDinh()
    {
        var chiDinh = Environment.GetEnvironmentVariable("CHAMCONG_FILE_DULIEU");
        if (!string.IsNullOrWhiteSpace(chiDinh))
        {
            return chiDinh;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChamCong",
            "chamcong.json");
    }

    /// <summary>Đọc dữ liệu từ file. Chưa có file thì bắt đầu với dữ liệu rỗng.</summary>
    public void Doc()
    {
        if (!File.Exists(DuongDanFile))
        {
            DuLieu = new DuLieuChamCong();
            return;
        }

        var noiDung = File.ReadAllText(DuongDanFile);
        DuLieu = JsonSerializer.Deserialize<DuLieuChamCong>(noiDung, TuyChonJson) ?? new DuLieuChamCong();
    }

    /// <summary>
    /// Ghi dữ liệu ra file. Ghi ra file tạm rồi mới đổi tên, để mất điện giữa chừng
    /// không làm hỏng file đang có.
    /// </summary>
    public void Ghi()
    {
        var noiDung = JsonSerializer.Serialize(DuLieu, TuyChonJson);
        var fileTam = DuongDanFile + ".tam";
        File.WriteAllText(fileTam, noiDung);
        File.Move(fileTam, DuongDanFile, overwrite: true);
    }

    public Tho ThemTho(string ten, decimal tienMotCong, string dienThoai = "", string ghiChu = "")
    {
        var tho = new Tho
        {
            Ten = ten.Trim(),
            TienMotCong = tienMotCong,
            DienThoai = dienThoai.Trim(),
            GhiChu = ghiChu.Trim(),
        };

        DuLieu.Thos.Add(tho);
        Ghi();
        return tho;
    }

    /// <summary>Thợ đang còn làm, xếp theo tên — đây là danh sách của màn hình chấm công.</summary>
    public List<Tho> ThoDangLam() =>
        DuLieu.Thos
            .Where(t => t.DangLam)
            .OrderBy(t => t.Ten, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// Tất cả thợ, người đang làm xếp trước rồi mới tới người đã nghỉ.
    /// Không có hàm xoá thợ: xoá là mất luôn bảng lương các tháng trước, nghỉ việc thì
    /// tắt <see cref="Tho.DangLam"/>.
    /// </summary>
    public List<Tho> TatCaTho() =>
        DuLieu.Thos
            .OrderByDescending(t => t.DangLam)
            .ThenBy(t => t.Ten, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>Ghi lại thợ sau khi sửa tên, tiền công hay đánh dấu đã nghỉ.</summary>
    public void LuuTho(Tho tho)
    {
        tho.Ten = tho.Ten.Trim();
        tho.SuaLuc = DateTime.UtcNow;
        Ghi();
    }

    /// <summary>Buổi công đã chấm của một thợ trong một buổi, chưa chấm thì trả về null.</summary>
    public BuoiCong? DangCham(Guid thoId, DateTime ngay, BuoiLam buoi) =>
        DuLieu.BuoiCongs.FirstOrDefault(
            b => b.ThoId == thoId && b.Ngay.Date == ngay.Date && b.Buoi == buoi);

    /// <summary>
    /// Chấm một buổi cho thợ. Chấm lại buổi đã chấm thì sửa số công chứ không thêm dòng mới.
    /// Tiền một công được chụp lại theo giá hiện tại của thợ.
    /// </summary>
    public BuoiCong Cham(
        Guid thoId, DateTime ngay, BuoiLam buoi, decimal soCong = BuoiCong.CongMotBuoi, string ghiChu = "")
    {
        if (soCong <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(soCong), soCong, "Số công phải lớn hơn 0. Muốn bỏ chấm thì dùng BoCham.");
        }

        var tho = DuLieu.Thos.FirstOrDefault(t => t.Id == thoId)
            ?? throw new ArgumentException("Không có thợ này.", nameof(thoId));

        var buoiCong = DangCham(thoId, ngay, buoi);
        if (buoiCong is null)
        {
            buoiCong = new BuoiCong { ThoId = thoId, Ngay = ngay.Date, Buoi = buoi };
            DuLieu.BuoiCongs.Add(buoiCong);
        }

        buoiCong.SoCong = soCong;
        buoiCong.GhiChu = ghiChu;
        buoiCong.TienMotCong = tho.TienMotCong;
        buoiCong.SuaLuc = DateTime.UtcNow;

        Ghi();
        return buoiCong;
    }

    /// <summary>Bỏ chấm một buổi. Trả về false nếu buổi đó vốn chưa chấm.</summary>
    public bool BoCham(Guid thoId, DateTime ngay, BuoiLam buoi)
    {
        var buoiCong = DangCham(thoId, ngay, buoi);
        if (buoiCong is null)
        {
            return false;
        }

        DuLieu.BuoiCongs.Remove(buoiCong);
        Ghi();
        return true;
    }

    public UngTien ThemUng(Guid thoId, DateTime ngay, decimal soTien, string ghiChu = "")
    {
        if (soTien <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(soTien), soTien, "Số tiền ứng phải lớn hơn 0.");
        }

        var ung = new UngTien
        {
            ThoId = thoId,
            Ngay = ngay.Date,
            SoTien = soTien,
            GhiChu = ghiChu.Trim(),
        };

        DuLieu.UngTiens.Add(ung);
        Ghi();
        return ung;
    }
}
