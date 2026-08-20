using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChamCong.SoDiDong;

/// <summary>Lỗi đã dịch thành câu hiện được lên màn hình.</summary>
public sealed class LoiSupabase : Exception
{
    public LoiSupabase(string thongDiep, string? goc = null)
        : base(thongDiep)
    {
        Goc = goc;
    }

    /// <summary>Câu lỗi gốc, giữ lại để còn lần ra nguyên nhân.</summary>
    public string? Goc { get; }
}

/// <summary>Một bản sao lưu đang nằm trên tài khoản. Không mang theo cả sổ — danh sách phải nhẹ.</summary>
public sealed record BanTaiKhoan(string Ngay, string SuaLuc);

/// <summary>
/// Đọc sổ chấm công từ Supabase — bảng <c>sao_luu</c>, mỗi ngày một hàng.
///
/// <para>
/// Máy tính chỉ **đọc**. Sổ thật nằm trong điện thoại của chủ, bảng này là bản sao lưu theo
/// tài khoản. Không ghi gì lên đây: hai máy cùng đẩy lên là hai sổ đè nhau mà không ai biết,
/// và app điện thoại mới là chỗ có đủ luồng hỏi lại trước khi ghi đè.
/// </para>
///
/// <para>
/// Không có câu lọc "chỉ lấy của tôi" trong mã: gọi <c>select</c> cả bảng thì Postgres tự cắt
/// còn đúng những hàng của tài khoản đang đăng nhập, theo RLS. Máy tính viết sai cũng không đọc
/// được sổ của người khác — ổ khoá nằm ở database, không nằm ở đây.
/// </para>
///
/// <para>
/// Khoá công khai (anon / publishable key) **không phải bí mật**: nó nằm trong mọi bản app cài
/// trên máy người dùng. Tuyệt đối không điền <c>service_role</c> key vào đây — khoá ấy bỏ qua
/// RLS, ai moi được là đọc và xoá được cả database.
/// </para>
/// </summary>
public sealed class NguonSupabase : IDisposable
{
    private const string Bang = "sao_luu";

    private readonly HttpClient _khach;
    private readonly string _diaChi;
    private readonly string _khoaCongKhai;

    /// <param name="handler">Chỉ để bài kiểm thử đưa vào một bộ trả lời giả, app để trống.</param>
    public NguonSupabase(string diaChi, string khoaCongKhai, HttpMessageHandler? handler = null)
    {
        _diaChi = diaChi.Trim().TrimEnd('/');
        _khoaCongKhai = khoaCongKhai.Trim();
        _khach = handler is null ? new HttpClient() : new HttpClient(handler);
        _khach.Timeout = TimeSpan.FromSeconds(25);
    }

    /// <summary>Đã điền địa chỉ và khoá chưa. Chưa thì màn hình phải mời điền, đừng gọi mạng.</summary>
    public bool DaCauHinh => _diaChi.Length > 0 && _khoaCongKhai.Length > 0;

    public bool DaDangNhap => TheDangNhap is not null;

    /// <summary>Vé đăng nhập của phiên hiện tại. Chỉ nằm trong bộ nhớ, không ghi xuống đĩa.</summary>
    private string? TheDangNhap { get; set; }

    /// <summary>Email của tài khoản đang đăng nhập, để hiện lên màn hình.</summary>
    public string EmailDangDung { get; private set; } = string.Empty;

    /// <summary>
    /// Đăng nhập bằng email và mật khẩu của **tài khoản chủ** — đúng tài khoản đã đẩy sổ lên,
    /// vì RLS khoá bảng này theo <c>user_id</c>. Tài khoản thợ (đăng nhập ẩn danh) không có bản
    /// nào ở đây.
    /// </summary>
    public async Task DangNhap(string email, string matKhau, CancellationToken huy = default)
    {
        BuocPhaiCauHinh();

        using var yeuCau = new HttpRequestMessage(
            HttpMethod.Post, $"{_diaChi}/auth/v1/token?grant_type=password")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { email = email.Trim(), password = matKhau }),
                Encoding.UTF8,
                "application/json"),
        };
        yeuCau.Headers.TryAddWithoutValidation("apikey", _khoaCongKhai);

        var traLoi = await Goi(yeuCau, huy).ConfigureAwait(false);
        var than = await traLoi.Content.ReadAsStringAsync(huy).ConfigureAwait(false);

        if (traLoi.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            throw new LoiSupabase("Email hoặc mật khẩu không đúng.", than);
        }

        NemNeuLoi(traLoi, than);

        using var goc = JsonDocument.Parse(than);
        if (!goc.RootElement.TryGetProperty("access_token", out var the) || the.GetString() is not { Length: > 0 } ve)
        {
            throw new LoiSupabase("Đăng nhập xong nhưng không nhận được vé vào cửa.", than);
        }

        TheDangNhap = ve;
        EmailDangDung = email.Trim();
    }

    /// <summary>Các bản đang có trên tài khoản, mới nhất đứng đầu. Chưa có bản nào thì rỗng.</summary>
    public async Task<List<BanTaiKhoan>> DanhSachBan(CancellationToken huy = default)
    {
        var than = await LayJson($"select=ngay,sua_luc&order=ngay.desc", huy).ConfigureAwait(false);

        using var goc = JsonDocument.Parse(than);
        var ds = new List<BanTaiKhoan>();
        foreach (var hang in goc.RootElement.EnumerateArray())
        {
            ds.Add(new BanTaiKhoan(
                hang.TryGetProperty("ngay", out var ngay) ? ngay.GetString() ?? string.Empty : string.Empty,
                hang.TryGetProperty("sua_luc", out var sua) ? sua.GetString() ?? string.Empty : string.Empty));
        }

        return ds;
    }

    /// <summary>
    /// Đọc một bản ra và mở gói. Ném <see cref="GoiHong"/> nếu hàng ấy không đúng khuôn — hàng
    /// này sửa tay được trong SQL Editor nên không phải chỗ để tin sẵn.
    /// </summary>
    public async Task<Goi> DocBan(string ngay, CancellationToken huy = default)
    {
        var than = await LayJson($"select=goi&ngay=eq.{Uri.EscapeDataString(ngay)}&limit=1", huy)
            .ConfigureAwait(false);

        using var goc = JsonDocument.Parse(than);
        if (goc.RootElement.GetArrayLength() == 0)
        {
            throw new LoiSupabase($"Không thấy bản sao lưu ngày {ngay} trên tài khoản này.");
        }

        var hang = goc.RootElement[0];
        if (!hang.TryGetProperty("goi", out var goi))
        {
            throw new LoiSupabase("Hàng sao lưu này thiếu phần dữ liệu.");
        }

        return SoDiDong.Goi.Doc(goi);
    }

    public void Dispose() => _khach.Dispose();

    private async Task<string> LayJson(string cauTruyVan, CancellationToken huy)
    {
        BuocPhaiCauHinh();
        if (TheDangNhap is null)
        {
            throw new LoiSupabase("Chưa đăng nhập tài khoản chủ.");
        }

        using var yeuCau = new HttpRequestMessage(
            HttpMethod.Get, $"{_diaChi}/rest/v1/{Bang}?{cauTruyVan}");
        yeuCau.Headers.TryAddWithoutValidation("apikey", _khoaCongKhai);
        yeuCau.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TheDangNhap);

        var traLoi = await Goi(yeuCau, huy).ConfigureAwait(false);
        var than = await traLoi.Content.ReadAsStringAsync(huy).ConfigureAwait(false);
        NemNeuLoi(traLoi, than);
        return than;
    }

    private async Task<HttpResponseMessage> Goi(HttpRequestMessage yeuCau, CancellationToken huy)
    {
        try
        {
            return await _khach.SendAsync(yeuCau, huy).ConfigureAwait(false);
        }
        catch (TaskCanceledException loi) when (!huy.IsCancellationRequested)
        {
            throw new LoiSupabase("Gọi mạng quá lâu không thấy trả lời. Kiểm tra mạng rồi thử lại.", loi.Message);
        }
        catch (HttpRequestException loi)
        {
            throw new LoiSupabase("Không nối được mạng, hoặc địa chỉ Supabase sai.", loi.Message);
        }
    }

    private void BuocPhaiCauHinh()
    {
        if (!DaCauHinh)
        {
            throw new LoiSupabase("Chưa điền địa chỉ Supabase và khoá công khai.");
        }
    }

    /// <summary>Dịch lỗi của Supabase thành câu người dùng đọc được, giữ câu gốc để lần nguyên nhân.</summary>
    private static void NemNeuLoi(HttpResponseMessage traLoi, string than)
    {
        if (traLoi.IsSuccessStatusCode)
        {
            return;
        }

        var chu = than.ToLowerInvariant();

        // Bảng chưa dựng: người gặp lỗi này chính là người sửa được nó, nên nói thẳng.
        if (chu.Contains("does not exist") || chu.Contains("schema cache"))
        {
            throw new LoiSupabase(
                "Chỗ sao lưu trên tài khoản chưa được dựng. Cần chạy file supabase/thiet-lap.sql.", than);
        }

        if (traLoi.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new LoiSupabase("Tài khoản này không có quyền đọc bản sao lưu.", than);
        }

        if (chu.Contains("invalid api key") || chu.Contains("no api key"))
        {
            throw new LoiSupabase("Khoá công khai không đúng.", than);
        }

        throw new LoiSupabase($"Supabase trả lỗi {(int)traLoi.StatusCode}.", than);
    }
}
