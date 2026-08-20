using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace QuanLyDienNuoc.Data;

/// <summary>
/// Cài đặt của phần mềm (nhắc nợ, sao lưu, cảnh báo nhập sai). Lưu ra file riêng cạnh
/// file dữ liệu để Ctrl+Z không cuốn theo và để đổi cài đặt không tính là một bước hoàn tác.
/// </summary>
public sealed class CaiDat
{
    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Nợ quá bao nhiêu ngày thì phần mềm nhắc khi mở lên.</summary>
    public int SoNgayNhacNo { get; set; } = 60;

    /// <summary>Thư mục chứa các bản sao lưu. Để trống là dùng thư mục "SaoLuu" cạnh file dữ liệu.</summary>
    public string ThuMucSaoLuu { get; set; } = string.Empty;

    /// <summary>Giữ lại bao nhiêu bản sao lưu gần nhất, bản cũ hơn sẽ bị xoá.</summary>
    public int SoBanSaoLuuGiuLai { get; set; } = 30;

    /// <summary>Tự sao lưu mỗi ngày một lần khi mở phần mềm.</summary>
    public bool TuDongSaoLuu { get; set; } = true;

    /// <summary>Mỗi bản sao lưu kèm luôn một file Excel đọc được bằng Excel/WPS.</summary>
    public bool SaoLuuKemExcel { get; set; } = true;

    public DateTime? LanSaoLuuCuoi { get; set; }

    /// <summary>Giá lệch quá bao nhiêu phần trăm so với lần trước thì hỏi lại.</summary>
    public int NguongLechGia { get; set; } = 20;

    /// <summary>Hỏi lại khi thêm một dòng giống hệt dòng đã có (cùng ngày, cùng hàng, cùng số lượng).</summary>
    public bool CanhBaoDongTrung { get; set; } = true;

    /// <summary>Địa chỉ project Supabase của app chấm công, ví dụ https://abc.supabase.co.</summary>
    public string ChamCongDiaChi { get; set; } = string.Empty;

    /// <summary>
    /// Khoá công khai (anon / publishable key) của project ấy. Khoá này **không phải bí mật** —
    /// nó nằm trong mọi bản app điện thoại đã cài; thứ chặn người này đọc sổ người kia là RLS
    /// trong database. Tuyệt đối không điền service_role key: khoá ấy bỏ qua RLS.
    /// </summary>
    public string ChamCongKhoaCongKhai { get; set; } = string.Empty;

    /// <summary>
    /// Email tài khoản chủ, nhớ lại cho khỏi gõ mỗi lần. **Mật khẩu thì không nhớ** — file cài
    /// đặt này nằm cạnh file dữ liệu, ai mở máy ra cũng đọc được.
    /// </summary>
    public string ChamCongEmail { get; set; } = string.Empty;

    /// <summary>Cài đặt mặc định nằm cạnh file dữ liệu, tên là caidat.json.</summary>
    public static string DuongDanBenCanh(string duongDanDuLieu)
    {
        var thuMuc = Path.GetDirectoryName(duongDanDuLieu);
        return Path.Combine(string.IsNullOrEmpty(thuMuc) ? "." : thuMuc, "caidat.json");
    }

    /// <summary>Đọc cài đặt. File thiếu hoặc hỏng thì trả về bản mặc định chứ không báo lỗi.</summary>
    public static CaiDat Doc(string duongDan)
    {
        if (!File.Exists(duongDan))
        {
            return new CaiDat();
        }

        try
        {
            var json = File.ReadAllText(duongDan, Encoding.UTF8);
            return JsonSerializer.Deserialize<CaiDat>(json, TuyChonJson) ?? new CaiDat();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return new CaiDat();
        }
    }

    public void Luu(string duongDan)
    {
        var thuMuc = Path.GetDirectoryName(duongDan);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }

        File.WriteAllText(duongDan, JsonSerializer.Serialize(this, TuyChonJson), new UTF8Encoding(false));
    }

    /// <summary>Thư mục sao lưu thật sự đang dùng.</summary>
    public string ThuMucSaoLuuThat(string duongDanDuLieu)
    {
        if (!string.IsNullOrWhiteSpace(ThuMucSaoLuu))
        {
            return ThuMucSaoLuu;
        }

        var thuMuc = Path.GetDirectoryName(duongDanDuLieu);
        return Path.Combine(string.IsNullOrEmpty(thuMuc) ? "." : thuMuc, "SaoLuu");
    }
}
