using ChamCong.SoDiDong;
using Xunit;

namespace ChamCong.Tests;

/// <summary>
/// Kiểm tra cách chọn nguồn cấu hình Supabase. Chỗ dễ sai: lấy **nửa vời** — địa chỉ của nơi này
/// ghép với khoá của nơi khác. Lúc ấy phần mềm báo một lỗi mạng khó hiểu, không ai đoán nổi tại
/// sao, nên thà coi như chưa có gì.
/// </summary>
public class CauHinhChamCongTests
{
    [Fact]
    public void Chon_LayNguonDauTienCoDuCaHai()
    {
        var cauHinh = CauHinhChamCong.Chon(
            ("bản dựng", null, null),
            ("biến môi trường", "https://a.supabase.co", "khoa-a"),
            ("phần mềm đã lưu", "https://b.supabase.co", "khoa-b"));

        Assert.True(cauHinh.DaCoSan);
        Assert.Equal("https://a.supabase.co", cauHinh.DiaChi);
        Assert.Equal("khoa-a", cauHinh.KhoaCongKhai);
        Assert.Equal("biến môi trường", cauHinh.Nguon);
    }

    [Fact]
    public void Chon_NguonChiCoMotNua_ThiBoQuaCaNguonAy()
    {
        var cauHinh = CauHinhChamCong.Chon(
            ("bản dựng", "https://a.supabase.co", ""),
            ("phần mềm đã lưu", "https://b.supabase.co", "khoa-b"));

        Assert.Equal("https://b.supabase.co", cauHinh.DiaChi);
        Assert.Equal("khoa-b", cauHinh.KhoaCongKhai);
        Assert.Equal("phần mềm đã lưu", cauHinh.Nguon);
    }

    [Fact]
    public void Chon_KhongNguonNaoDu_ThiCoiNhuChuaCo()
    {
        var cauHinh = CauHinhChamCong.Chon(
            ("bản dựng", null, "khoa"),
            ("biến môi trường", "   ", "khoa"));

        Assert.False(cauHinh.DaCoSan);
        Assert.Equal(string.Empty, cauHinh.DiaChi);
        Assert.Equal("chưa có", cauHinh.Nguon);
    }

    [Fact]
    public void Chon_CatKhoangTrangHaiDau()
    {
        var cauHinh = CauHinhChamCong.Chon(("bản dựng", "  https://a.supabase.co \n", " khoa-a "));

        Assert.Equal("https://a.supabase.co", cauHinh.DiaChi);
        Assert.Equal("khoa-a", cauHinh.KhoaCongKhai);
    }

    [Fact]
    public void Chon_KhongCoNguonNao_ThiVanChayChuKhongNem()
    {
        Assert.False(CauHinhChamCong.Chon().DaCoSan);
    }

    [Fact]
    public void DocFile_DocDuocDiaChiVaKhoa()
    {
        var thuMuc = Path.Combine(Path.GetTempPath(), "cauhinh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(thuMuc);
        try
        {
            File.WriteAllText(
                Path.Combine(thuMuc, CauHinhChamCong.TenFile),
                """{ "diaChi": "https://a.supabase.co", "khoaCongKhai": "khoa-a" }""");

            var (diaChi, khoa) = CauHinhChamCong.DocFile(thuMuc);

            Assert.Equal("https://a.supabase.co", diaChi);
            Assert.Equal("khoa-a", khoa);
        }
        finally
        {
            Directory.Delete(thuMuc, recursive: true);
        }
    }

    [Fact]
    public void DocFile_FileThieuHoacHong_ThiCoiNhuKhongCo()
    {
        var thuMuc = Path.Combine(Path.GetTempPath(), "cauhinh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(thuMuc);
        try
        {
            Assert.Equal((null, null), CauHinhChamCong.DocFile(thuMuc));

            File.WriteAllText(Path.Combine(thuMuc, CauHinhChamCong.TenFile), "khong-phai-json");
            Assert.Equal((null, null), CauHinhChamCong.DocFile(thuMuc));
        }
        finally
        {
            Directory.Delete(thuMuc, recursive: true);
        }
    }

    [Fact]
    public void MacDinh_ChuaCoGiThiLayTheoNhungGiNguoiDungDaDien()
    {
        var cauHinh = CauHinhChamCong.MacDinh("https://da-dien.supabase.co", "khoa-da-dien");

        // Máy chạy test không nhét khoá vào bản dựng, cũng không đặt biến môi trường.
        Assert.True(cauHinh.DaCoSan);
        Assert.Equal("https://da-dien.supabase.co", cauHinh.DiaChi);
        Assert.Equal("phần mềm đã lưu", cauHinh.Nguon);
    }
}
