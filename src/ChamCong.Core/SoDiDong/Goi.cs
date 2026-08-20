using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChamCong.SoDiDong;

/// <summary>Gói sao lưu chấm công không đọc được: không phải của app này, hỏng, hoặc mới hơn.</summary>
public sealed class GoiHong : Exception
{
    public GoiHong(string lyDo)
        : base(lyDo)
    {
    }
}

/// <summary>Vài con số để nhìn là biết bản này mang những gì.</summary>
public sealed record TomTat(int SoTho, int SoBuoiCong, int SoUngTien, int SoKy);

/// <summary>
/// Gói sao lưu chấm công: đúng cái gói app điện thoại ghi ra file, và cũng là đúng cái nằm
/// trong cột <c>goi</c> của bảng <c>sao_luu</c> trên Supabase.
///
/// <para>
/// Bộ kiểm ở đây là bản dịch của <c>docGoi</c> trong <c>mobile/src/nghiepvu/goiSaoLuu.ts</c>,
/// và phải giữ nguyên tinh thần của nó: **dữ liệu từ database cũng là dữ liệu từ ngoài vào**.
/// Hàng ấy sửa tay được trong SQL Editor, và cùng một tài khoản có thể vừa chạy bản app cũ vừa
/// chạy bản mới. Nên thà từ chối oan còn hơn nhận bừa rồi hiện ra số sai.
/// </para>
/// </summary>
public sealed class Goi
{
    /// <summary>Nhãn nhận dạng gói của app chấm công. Đừng đổi — bản sao lưu cũ vẫn mang nhãn cũ.</summary>
    public const string NhanApp = "cham-cong";

    /// <summary>Phiên bản cấu trúc gói mà bản máy tính này đọc được.</summary>
    public const int PhienBanHoTro = 1;

    private static readonly JsonSerializerOptions TuyChon = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public string App { get; set; } = string.Empty;

    public int PhienBan { get; set; }

    /// <summary>Lúc bấm sao lưu, dạng ISO.</summary>
    public string TaoLuc { get; set; } = string.Empty;

    public SoChamCong DuLieu { get; set; } = new();

    /// <summary>Đọc một gói từ chuỗi JSON — file sao lưu, hoặc cột <c>goi</c> lấy từ Supabase.</summary>
    /// <exception cref="GoiHong">Không phải gói chấm công, hỏng, hoặc của bản app mới hơn.</exception>
    public static Goi Doc(string json)
    {
        JsonElement goc;
        try
        {
            goc = JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            throw new GoiHong("Nội dung này không phải JSON đọc được.");
        }

        return Doc(goc);
    }

    /// <summary>Đọc một gói đã ở dạng JSON — bản lấy từ cột <c>jsonb</c>.</summary>
    public static Goi Doc(JsonElement goc)
    {
        if (goc.ValueKind != JsonValueKind.Object)
        {
            throw new GoiHong("Đây không phải bản sao lưu chấm công.");
        }

        if (!goc.TryGetProperty("app", out var app) || app.ValueKind != JsonValueKind.String
            || app.GetString() != NhanApp)
        {
            throw new GoiHong("Đây không phải bản sao lưu chấm công.");
        }

        // Gói của bản app mới hơn thì cấu trúc có thể đã khác, đọc vào là hiện ra số sai.
        if (!goc.TryGetProperty("phienBan", out var phienBan)
            || phienBan.ValueKind != JsonValueKind.Number
            || phienBan.GetInt32() > PhienBanHoTro)
        {
            throw new GoiHong(
                "Bản sao lưu này của phiên bản app điện thoại mới hơn. Hãy cập nhật phần mềm máy tính rồi thử lại.");
        }

        if (!goc.TryGetProperty("duLieu", out var duLieu) || duLieu.ValueKind != JsonValueKind.Object)
        {
            throw new GoiHong("Bản sao lưu thiếu phần dữ liệu.");
        }

        return new Goi
        {
            App = NhanApp,
            PhienBan = phienBan.GetInt32(),
            TaoLuc = goc.TryGetProperty("taoLuc", out var taoLuc) && taoLuc.ValueKind == JsonValueKind.String
                ? taoLuc.GetString() ?? string.Empty
                : string.Empty,
            DuLieu = ChuanHoa(duLieu),
        };
    }

    public static TomTat Dem(SoChamCong so) =>
        new(so.Thos.Count, so.BuoiCongs.Count, so.UngTiens.Count, so.KyLuongs.Count);

    /// <summary>
    /// Vá sổ đọc từ ngoài vào cho đủ hình đủ dạng, và chuyển dáng cũ sang dáng mới — bản dịch
    /// của <c>chuanHoa</c> bên app điện thoại. Thợ bản cũ chỉ có một mức <c>tienMotCong</c>:
    /// biến nó thành mốc lương đầu tiên tính từ ngày thêm thợ, để buổi công cũ vẫn ra đúng tiền.
    /// </summary>
    private static SoChamCong ChuanHoa(JsonElement duLieu)
    {
        var so = duLieu.Deserialize<SoChamCong>(TuyChon) ?? new SoChamCong();

        var thoBanCu = duLieu.TryGetProperty("thos", out var thos) && thos.ValueKind == JsonValueKind.Array
            ? thos
            : default;

        for (var i = 0; i < so.Thos.Count; i++)
        {
            var tho = so.Thos[i];
            if (tho.MocLuong.Count > 0)
            {
                continue;
            }

            var tienCu = 0m;
            if (thoBanCu.ValueKind == JsonValueKind.Array && i < thoBanCu.GetArrayLength()
                && thoBanCu[i].TryGetProperty("tienMotCong", out var tien)
                && tien.ValueKind == JsonValueKind.Number)
            {
                tienCu = tien.GetDecimal();
            }

            tho.MocLuong.Add(new MocLuong
            {
                TuNgay = string.IsNullOrEmpty(tho.NgayTao) ? "2000-01-01" : tho.NgayTao,
                TienMotCong = tienCu,
            });
        }

        return so;
    }
}
