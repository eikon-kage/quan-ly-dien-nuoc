using System.Reflection;
using System.Text.Json;

namespace ChamCong.SoDiDong;

/// <summary>
/// Địa chỉ project Supabase và khoá công khai — **nhét sẵn vào bản dựng**, đúng cách app điện
/// thoại làm (xem <c>mobile/src/nghiepvu/cauHinhSupabase.ts</c>): người dùng chỉ gõ email và
/// mật khẩu, không phải đi tìm địa chỉ với khoá ở đâu để dán vào.
///
/// <para>
/// Khoá công khai (anon / publishable key) **không phải bí mật**: nó nằm trong mọi bản app đã
/// phát ra, ai gỡ ra cũng đọc được, và Supabase phát nó ra để làm đúng việc ấy. Thứ chặn người
/// này đọc sổ người kia là **RLS trong database**. Nhưng *không phải bí mật* khác *nên đưa lên
/// git*: khoá nằm trong repo công khai là ai cũng gọi được vào project ấy, nên nó đi vào bản
/// dựng qua biến môi trường chứ không nằm trong mã nguồn — y như bên app điện thoại.
/// </para>
///
/// <para>
/// Tuyệt đối không dùng <c>service_role</c> key: khoá ấy **bỏ qua RLS**, ai moi được là đọc và
/// xoá được cả database.
/// </para>
/// </summary>
public sealed class CauHinhChamCong
{
    /// <summary>Tên thuộc tính nhét vào assembly lúc dựng, và cũng là tên biến môi trường.</summary>
    public const string TenDiaChi = "ChamCongSupabaseUrl";

    public const string TenKhoa = "ChamCongSupabaseAnonKey";

    /// <summary>Tên file cấu hình để cạnh file chạy, cho ai đã có bản dựng sẵn mà muốn tự điền.</summary>
    public const string TenFile = "supabase.json";

    private CauHinhChamCong(string diaChi, string khoaCongKhai, string nguon)
    {
        DiaChi = diaChi;
        KhoaCongKhai = khoaCongKhai;
        Nguon = nguon;
    }

    public string DiaChi { get; }

    public string KhoaCongKhai { get; }

    /// <summary>Lấy được từ đâu — để màn hình nói cho người dùng biết, và để còn lần ra khi sai.</summary>
    public string Nguon { get; }

    /// <summary>Có đủ cả địa chỉ và khoá thì mới gọi được.</summary>
    public bool DaCoSan => DiaChi.Length > 0 && KhoaCongKhai.Length > 0;

    /// <summary>
    /// Chọn nguồn đầu tiên có **đủ cả hai** giá trị. Thiếu một trong hai thì bỏ qua nguồn ấy chứ
    /// không lấy nửa vời: ghép địa chỉ của nơi này với khoá của nơi khác là ra một lỗi mạng khó
    /// hiểu, không ai đoán nổi nguyên nhân.
    /// </summary>
    public static CauHinhChamCong Chon(params (string Nguon, string? DiaChi, string? Khoa)[] cacNguon)
    {
        foreach (var (nguon, diaChi, khoa) in cacNguon)
        {
            var d = Sach(diaChi);
            var k = Sach(khoa);
            if (d.Length > 0 && k.Length > 0)
            {
                return new CauHinhChamCong(d, k, nguon);
            }
        }

        return new CauHinhChamCong(string.Empty, string.Empty, "chưa có");
    }

    /// <summary>
    /// Gom các nguồn thật theo thứ tự ưu tiên: bản dựng → biến môi trường → file cạnh phần mềm →
    /// những gì người dùng đã tự điền trong phần mềm.
    /// </summary>
    public static CauHinhChamCong MacDinh(string? diaChiDaDien = null, string? khoaDaDien = null)
    {
        var (diaChiFile, khoaFile) = DocFile(ThuMucPhanMem());

        return Chon(
            ("bản dựng", TuAssembly(TenDiaChi), TuAssembly(TenKhoa)),
            ("biến môi trường", Environment.GetEnvironmentVariable(TenDiaChi), Environment.GetEnvironmentVariable(TenKhoa)),
            ($"file {TenFile}", diaChiFile, khoaFile),
            ("phần mềm đã lưu", diaChiDaDien, khoaDaDien));
    }

    private static string Sach(string? chu) => chu?.Trim() ?? string.Empty;

    /// <summary>Thuộc tính nhét vào lúc dựng (xem ItemGroup AssemblyMetadata trong file .csproj).</summary>
    private static string? TuAssembly(string ten)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, ten, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static string ThuMucPhanMem() => AppContext.BaseDirectory;

    /// <summary>
    /// Đọc <c>supabase.json</c> cạnh file chạy: <c>{ "diaChi": "...", "khoaCongKhai": "..." }</c>.
    /// File thiếu hoặc hỏng thì coi như không có, đừng ném lỗi — phần mềm vẫn phải mở lên được.
    /// </summary>
    public static (string? DiaChi, string? Khoa) DocFile(string thuMuc)
    {
        try
        {
            var duongDan = Path.Combine(thuMuc, TenFile);
            if (!File.Exists(duongDan))
            {
                return (null, null);
            }

            using var goc = JsonDocument.Parse(File.ReadAllText(duongDan));
            return (
                goc.RootElement.TryGetProperty("diaChi", out var d) ? d.GetString() : null,
                goc.RootElement.TryGetProperty("khoaCongKhai", out var k) ? k.GetString() : null);
        }
        catch (Exception loi) when (loi is IOException or JsonException or UnauthorizedAccessException)
        {
            return (null, null);
        }
    }
}
