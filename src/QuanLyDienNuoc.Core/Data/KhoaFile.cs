using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace QuanLyDienNuoc.Data;

/// <summary>Máy nào đang mở file dữ liệu.</summary>
public sealed record ThongTinKhoa(string May, string NguoiDung, int TienTrinh, DateTime Luc)
{
    public string MoTa => $"máy {May} (người dùng {NguoiDung}) mở lúc {Luc:HH:mm dd/MM/yyyy}";
}

/// <summary>
/// Khoá file dữ liệu trong lúc phần mềm đang mở. Để dữ liệu trên thư mục mạng rồi mở ở hai máy
/// thì máy lưu sau đè mất máy lưu trước, nên máy thứ hai phải biết mà chỉ xem thôi.
///
/// Dùng hai file cạnh file dữ liệu:
/// <list type="bullet">
/// <item><c>.khoa</c> — file rỗng bị giữ mở suốt phiên, máy khác không mở nổi. Máy treo hay mất
/// điện thì hệ điều hành tự nhả, lần sau mở lại vẫn vào được bình thường.</item>
/// <item><c>.dangmo</c> — ghi tên máy, tên người dùng và giờ mở để báo cho máy thứ hai biết
/// đang vướng ai. File này ai cũng đọc được nên không giữ mở.</item>
/// </list>
/// </summary>
public sealed class KhoaFile : IDisposable
{
    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _duongDanKhoa;
    private readonly string _duongDanThongTin;
    private FileStream? _giuFile;

    private KhoaFile(string duongDanKhoa, string duongDanThongTin, FileStream giuFile)
    {
        _duongDanKhoa = duongDanKhoa;
        _duongDanThongTin = duongDanThongTin;
        _giuFile = giuFile;
    }

    public static string DuongDanKhoa(string duongDanDuLieu) => duongDanDuLieu + ".khoa";

    public static string DuongDanThongTin(string duongDanDuLieu) => duongDanDuLieu + ".dangmo";

    /// <summary>
    /// Giành quyền sửa file dữ liệu. Trả về null khi máy khác đang giữ — lúc đó dùng
    /// <see cref="DocAiDangGiu"/> để biết hỏi ai.
    /// </summary>
    public static KhoaFile? Thu(string duongDanDuLieu, DateTime? luc = null)
    {
        var duongDanKhoa = DuongDanKhoa(duongDanDuLieu);
        var duongDanThongTin = DuongDanThongTin(duongDanDuLieu);

        var thuMuc = Path.GetDirectoryName(duongDanKhoa);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }

        FileStream giuFile;
        try
        {
            giuFile = new FileStream(duongDanKhoa, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var khoa = new KhoaFile(duongDanKhoa, duongDanThongTin, giuFile);
        khoa.GhiThongTin(new ThongTinKhoa(
            Environment.MachineName,
            Environment.UserName,
            Environment.ProcessId,
            luc ?? DateTime.Now));

        return khoa;
    }

    /// <summary>Đọc xem ai đang giữ file. Trả về null nếu không đọc được thông tin.</summary>
    public static ThongTinKhoa? DocAiDangGiu(string duongDanDuLieu)
    {
        var duongDan = DuongDanThongTin(duongDanDuLieu);
        if (!File.Exists(duongDan))
        {
            return null;
        }

        try
        {
            using var doc = new FileStream(duongDan, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var chu = new StreamReader(doc, Encoding.UTF8);
            return JsonSerializer.Deserialize<ThongTinKhoa>(chu.ReadToEnd(), TuyChonJson);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_giuFile is null)
        {
            return;
        }

        _giuFile.Dispose();
        _giuFile = null;

        Xoa(_duongDanThongTin);
        Xoa(_duongDanKhoa);
    }

    private void GhiThongTin(ThongTinKhoa thongTin)
    {
        try
        {
            File.WriteAllText(
                _duongDanThongTin,
                JsonSerializer.Serialize(thongTin, TuyChonJson),
                new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Không ghi được thông tin thì vẫn giữ khoá; máy khác chỉ thiếu dòng "ai đang mở".
        }
    }

    private static void Xoa(string duongDan)
    {
        try
        {
            File.Delete(duongDan);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Thư mục mạng chập chờn thì để lại, lần mở sau ghi đè.
        }
    }
}
