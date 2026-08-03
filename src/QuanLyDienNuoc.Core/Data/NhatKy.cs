using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace QuanLyDienNuoc.Data;

/// <summary>Một dòng nhật ký: lúc nào, làm gì.</summary>
public sealed record MucNhatKy(DateTime Luc, string MoTa, string ChiTiet = "");

/// <summary>
/// Nhật ký thay đổi, ghi nối tiếp vào một file text (mỗi dòng một mục JSON).
/// Cố ý để ngoài file dữ liệu: Ctrl+Z quay lại trạng thái cũ nhưng nhật ký vẫn còn nguyên,
/// nên vẫn tra được ai sửa gì lúc nào khi khách thắc mắc.
/// </summary>
public sealed class NhatKy
{
    /// <summary>File to hơn mức này thì cắt bớt phần đầu cho khỏi phình mãi.</summary>
    private const long KichThuocToiDa = 2 * 1024 * 1024;

    /// <summary>Số mục giữ lại sau mỗi lần cắt.</summary>
    private const int SoMucGiuLai = 5000;

    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly object _khoa = new();

    public NhatKy(string duongDanFile) => DuongDanFile = duongDanFile;

    public string DuongDanFile { get; }

    /// <summary>Nhật ký nằm cạnh file dữ liệu, tên là nhatky.jsonl.</summary>
    public static string DuongDanBenCanh(string duongDanDuLieu)
    {
        var thuMuc = Path.GetDirectoryName(duongDanDuLieu);
        return Path.Combine(string.IsNullOrEmpty(thuMuc) ? "." : thuMuc, "nhatky.jsonl");
    }

    /// <summary>Ghi một mục. Không bao giờ ném lỗi ra ngoài — hỏng nhật ký thì mặc kệ, dữ liệu mới quan trọng.</summary>
    public void Ghi(string moTa, string chiTiet = "", DateTime? luc = null)
    {
        if (string.IsNullOrWhiteSpace(moTa))
        {
            return;
        }

        try
        {
            lock (_khoa)
            {
                var thuMuc = Path.GetDirectoryName(DuongDanFile);
                if (!string.IsNullOrEmpty(thuMuc))
                {
                    Directory.CreateDirectory(thuMuc);
                }

                var muc = new MucNhatKy(luc ?? DateTime.Now, moTa.Trim(), chiTiet.Trim());
                File.AppendAllText(
                    DuongDanFile,
                    JsonSerializer.Serialize(muc, TuyChonJson) + Environment.NewLine,
                    new UTF8Encoding(false));

                CatBotNeuQuaTo();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Không ghi được nhật ký thì bỏ qua, không cản trở việc lưu dữ liệu.
        }
    }

    /// <summary>Đọc các mục gần nhất, mục mới nhất đứng đầu.</summary>
    public IReadOnlyList<MucNhatKy> Doc(int soMuc = 1000)
    {
        if (!File.Exists(DuongDanFile))
        {
            return Array.Empty<MucNhatKy>();
        }

        try
        {
            lock (_khoa)
            {
                var ketQua = new List<MucNhatKy>();
                foreach (var dong in File.ReadLines(DuongDanFile, Encoding.UTF8).Reverse())
                {
                    if (ketQua.Count >= soMuc)
                    {
                        break;
                    }

                    if (PhanTich(dong) is { } muc)
                    {
                        ketQua.Add(muc);
                    }
                }

                return ketQua;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<MucNhatKy>();
        }
    }

    private static MucNhatKy? PhanTich(string dong)
    {
        if (string.IsNullOrWhiteSpace(dong))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MucNhatKy>(dong, TuyChonJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void CatBotNeuQuaTo()
    {
        var thongTin = new FileInfo(DuongDanFile);
        if (!thongTin.Exists || thongTin.Length <= KichThuocToiDa)
        {
            return;
        }

        var giuLai = File.ReadLines(DuongDanFile, Encoding.UTF8).TakeLast(SoMucGiuLai).ToList();
        File.WriteAllLines(DuongDanFile, giuLai, new UTF8Encoding(false));
    }
}
