using System.Net;
using System.Text;
using ChamCong.SoDiDong;
using Xunit;

namespace ChamCong.Tests;

/// <summary>
/// Kiểm tra phần gọi Supabase: gọi đúng đường, đọc đúng trả lời, và **dịch lỗi thành câu người
/// dùng đọc được**. Không gọi mạng thật — đưa vào một bộ trả lời giả để chạy được ở mọi máy.
/// </summary>
public class NguonSupabaseTests
{
    private const string DiaChi = "https://abc.supabase.co";
    private const string Khoa = "khoa-cong-khai";

    /// <summary>Bộ trả lời giả: nhớ lại các yêu cầu đã nhận, trả về câu đã dặn trước.</summary>
    private sealed class TraLoiGia : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Ma, string Than)> _cauTraLoi = new();

        public List<HttpRequestMessage> DaNhan { get; } = new();

        public TraLoiGia Dan(HttpStatusCode ma, string than)
        {
            _cauTraLoi.Enqueue((ma, than));
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage yeuCau, CancellationToken huy)
        {
            DaNhan.Add(yeuCau);
            var (ma, than) = _cauTraLoi.Count > 0 ? _cauTraLoi.Dequeue() : (HttpStatusCode.OK, "[]");
            return Task.FromResult(new HttpResponseMessage(ma)
            {
                Content = new StringContent(than, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static async Task<(NguonSupabase Nguon, TraLoiGia Gia)> DaDangNhap(params (HttpStatusCode, string)[] tiepTheo)
    {
        var gia = new TraLoiGia().Dan(HttpStatusCode.OK, """{ "access_token": "ve-vao-cua" }""");
        foreach (var (ma, than) in tiepTheo)
        {
            gia.Dan(ma, than);
        }

        var nguon = new NguonSupabase(DiaChi, Khoa, gia);
        await nguon.DangNhap("chu@cua-hang.vn", "mat-khau");
        return (nguon, gia);
    }

    [Fact]
    public void ChuaDienDiaChiHoacKhoa_ThiCoiNhuChuaBat()
    {
        Assert.False(new NguonSupabase(string.Empty, Khoa).DaCauHinh);
        Assert.False(new NguonSupabase(DiaChi, string.Empty).DaCauHinh);
        Assert.True(new NguonSupabase(DiaChi, Khoa).DaCauHinh);
    }

    [Fact]
    public async Task DangNhap_GoiDungDuongVaGuiKhoa()
    {
        var (nguon, gia) = await DaDangNhap();

        var yeuCau = gia.DaNhan[0];
        Assert.Equal(HttpMethod.Post, yeuCau.Method);
        Assert.Equal($"{DiaChi}/auth/v1/token?grant_type=password", yeuCau.RequestUri!.ToString());
        Assert.Equal(Khoa, Assert.Single(yeuCau.Headers.GetValues("apikey")));
        Assert.True(nguon.DaDangNhap);
        Assert.Equal("chu@cua-hang.vn", nguon.EmailDangDung);
    }

    [Fact]
    public async Task DangNhap_SaiMatKhau_ThiNoiThangLaSaiMatKhau()
    {
        var gia = new TraLoiGia().Dan(HttpStatusCode.BadRequest, """{ "error": "invalid_grant" }""");
        var nguon = new NguonSupabase(DiaChi, Khoa, gia);

        var loi = await Assert.ThrowsAsync<LoiSupabase>(() => nguon.DangNhap("a@b.vn", "sai"));
        Assert.Equal("Email hoặc mật khẩu không đúng.", loi.Message);
        Assert.False(nguon.DaDangNhap);
    }

    [Fact]
    public async Task ChuaDangNhap_MaDoiDanhSach_ThiNhacDangNhap()
    {
        var nguon = new NguonSupabase(DiaChi, Khoa, new TraLoiGia());

        var loi = await Assert.ThrowsAsync<LoiSupabase>(() => nguon.DanhSachBan());
        Assert.Equal("Chưa đăng nhập tài khoản chủ.", loi.Message);
    }

    [Fact]
    public async Task DanhSachBan_DocDungNgayVaLucSua_MoiNhatDungDau()
    {
        var (nguon, gia) = await DaDangNhap((HttpStatusCode.OK, """
        [
          { "ngay": "2026-08-20", "sua_luc": "2026-08-20T03:00:00Z" },
          { "ngay": "2026-08-19", "sua_luc": "2026-08-19T02:00:00Z" }
        ]
        """));

        var ds = await nguon.DanhSachBan();

        Assert.Equal(2, ds.Count);
        Assert.Equal("2026-08-20", ds[0].Ngay);
        Assert.Equal("2026-08-19", ds[1].Ngay);

        // Không có câu lọc "chỉ lấy của tôi": RLS bên database mới là chỗ cắt.
        var duong = gia.DaNhan[1].RequestUri!.ToString();
        Assert.Contains("/rest/v1/sao_luu?", duong);
        Assert.Contains("order=ngay.desc", duong);
        Assert.DoesNotContain("user_id", duong);
        Assert.Equal("Bearer ve-vao-cua", gia.DaNhan[1].Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task DocBan_MoGoiRaSoChamCong()
    {
        var (nguon, gia) = await DaDangNhap((HttpStatusCode.OK, """
        [{ "goi": {
            "app": "cham-cong", "phienBan": 1, "taoLuc": "2026-08-20T03:00:00Z",
            "duLieu": { "thos": [{ "id": "t1", "ten": "Anh Tuấn",
              "mocLuong": [{ "tuNgay": "2026-01-01", "tienMotCong": 300000 }] }] }
        } }]
        """));

        var goi = await nguon.DocBan("2026-08-20");

        Assert.Equal("Anh Tuấn", goi.DuLieu.Thos[0].Ten);
        Assert.Contains("ngay=eq.2026-08-20", gia.DaNhan[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task DocBan_HangKhongDungKhuon_ThiNemGoiHong()
    {
        var (nguon, _) = await DaDangNhap((HttpStatusCode.OK, """[{ "goi": { "app": "app-khac" } }]"""));

        await Assert.ThrowsAsync<GoiHong>(() => nguon.DocBan("2026-08-20"));
    }

    [Fact]
    public async Task DocBan_KhongCoHangNao_ThiNoiRoLaKhongThayBan()
    {
        var (nguon, _) = await DaDangNhap((HttpStatusCode.OK, "[]"));

        var loi = await Assert.ThrowsAsync<LoiSupabase>(() => nguon.DocBan("2026-08-20"));
        Assert.Contains("Không thấy bản sao lưu ngày 2026-08-20", loi.Message);
    }

    [Fact]
    public async Task BangChuaDung_ThiChiDungFileThietLap()
    {
        var (nguon, _) = await DaDangNhap((HttpStatusCode.NotFound, """
        { "message": "relation \"public.sao_luu\" does not exist" }
        """));

        var loi = await Assert.ThrowsAsync<LoiSupabase>(() => nguon.DanhSachBan());
        Assert.Contains("thiet-lap.sql", loi.Message);
    }

    [Fact]
    public async Task KhongCoQuyenDoc_ThiNoiRoLaKhongCoQuyen()
    {
        var (nguon, _) = await DaDangNhap((HttpStatusCode.Forbidden, """{ "message": "permission denied" }"""));

        var loi = await Assert.ThrowsAsync<LoiSupabase>(() => nguon.DanhSachBan());
        Assert.Contains("không có quyền", loi.Message);
    }
}
