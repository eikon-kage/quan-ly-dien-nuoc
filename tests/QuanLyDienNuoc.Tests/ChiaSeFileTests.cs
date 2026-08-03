using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Hai máy dùng chung một file dữ liệu trên thư mục mạng: máy thứ hai chỉ được xem,
/// và nếu file bị máy khác sửa thì không được lặng lẽ ghi đè.
/// </summary>
public sealed class ChiaSeFileTests : IDisposable
{
    private readonly string _thuMucTam;
    private readonly string _duongDanFile;
    private readonly KhoDuLieu _kho;

    public ChiaSeFileTests()
    {
        _thuMucTam = Path.Combine(Path.GetTempPath(), "QuanLyDienNuoc.Tests", Guid.NewGuid().ToString("N"));
        _duongDanFile = Path.Combine(_thuMucTam, "dulieu.json");
        _kho = new KhoDuLieu(_duongDanFile);
        _kho.Nap();
    }

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }
    }

    /// <summary>Máy khác ghi đè file trong lúc mình đang mở.</summary>
    private void MayKhacSuaFile(string tenKhach = "Khách của máy kia")
    {
        var khoMayKhac = new KhoDuLieu(_duongDanFile);
        khoMayKhac.Nap();
        khoMayKhac.DuLieu.KhachHangs.Add(new KhachHang { Ten = tenKhach });
        khoMayKhac.Luu();

        // Dấu vết file so theo giờ sửa: ghi hai lần trong cùng một tích tắc thì phải khác kích thước,
        // nên thêm hẳn một khách mới cho chắc.
        File.SetLastWriteTimeUtc(_duongDanFile, DateTime.UtcNow.AddSeconds(1));
    }

    // ---------- Khoá file ----------

    [Fact]
    public void KhoaFile_MayThuHaiKhongGianhDuocKhoa()
    {
        using var khoaMayMot = KhoaFile.Thu(_duongDanFile);

        Assert.NotNull(khoaMayMot);
        Assert.Null(KhoaFile.Thu(_duongDanFile));
    }

    [Fact]
    public void KhoaFile_DocDuocAiDangGiu()
    {
        var luc = new DateTime(2026, 8, 3, 8, 30, 0);
        using var khoa = KhoaFile.Thu(_duongDanFile, luc);

        var ai = KhoaFile.DocAiDangGiu(_duongDanFile);

        Assert.NotNull(ai);
        Assert.Equal(Environment.MachineName, ai!.May);
        Assert.Equal(Environment.UserName, ai.NguoiDung);
        Assert.Equal(luc, ai.Luc);
        Assert.Contains(Environment.MachineName, ai.MoTa);
    }

    [Fact]
    public void KhoaFile_DongPhanMemThiMayKhacVaoDuoc()
    {
        var khoa = KhoaFile.Thu(_duongDanFile);
        Assert.NotNull(khoa);
        khoa!.Dispose();

        Assert.Null(KhoaFile.DocAiDangGiu(_duongDanFile));

        using var khoaSau = KhoaFile.Thu(_duongDanFile);
        Assert.NotNull(khoaSau);
    }

    [Fact]
    public void KhoaFile_ChuaAiGiuThiKhongCoThongTin()
    {
        Assert.Null(KhoaFile.DocAiDangGiu(_duongDanFile));
    }

    // ---------- Chế độ chỉ xem ----------

    [Fact]
    public void ChiXem_KhongGhiThayDoiVaoBoNhoLanFile()
    {
        _kho.BatChiXem("Máy Kho đang mở");
        var truoc = File.ReadAllText(_duongDanFile);

        _kho.ThucHien("Thêm khách", () => _kho.DuLieu.KhachHangs.Add(new KhachHang { Ten = "Khách mới" }));

        Assert.True(_kho.ChiXem);
        Assert.Equal("Máy Kho đang mở", _kho.LyDoChiXem);
        Assert.Empty(_kho.DuLieu.KhachHangs);
        Assert.Equal(truoc, File.ReadAllText(_duongDanFile));
        Assert.False(_kho.CoTheHoanTac);
    }

    [Fact]
    public void ChiXem_BaoChoManHinhBietThaoTacBiChan()
    {
        _kho.BatChiXem("Máy Kho đang mở");
        var soLanBao = 0;
        _kho.ThaoTacBiChan += (_, _) => soLanBao++;

        _kho.ThucHien("Thêm khách", () => _kho.DuLieu.KhachHangs.Add(new KhachHang()));

        Assert.Equal(1, soLanBao);
    }

    [Fact]
    public void ChiXem_SuaThangTrenLuoiThiTraLaiNhuCu()
    {
        var khach = new KhachHang { Ten = "Tên cũ" };
        _kho.DuLieu.KhachHangs.Add(khach);
        _kho.Luu();
        _kho.BatChiXem("Máy Kho đang mở");

        // Sửa trực tiếp trên lưới: dữ liệu đổi trước rồi mới báo cho kho.
        var truoc = _kho.ChupNhanh();
        _kho.DuLieu.KhachHangs[0].Ten = "Tên mới";
        _kho.GhiNhan(truoc, "Sửa tên khách");

        Assert.Equal("Tên cũ", _kho.DuLieu.KhachHangs[0].Ten);
        Assert.DoesNotContain("Tên mới", File.ReadAllText(_duongDanFile));
    }

    // ---------- Máy khác sửa file ----------

    [Fact]
    public void FileBiMayKhacSua_NhanRaKhiFileDoiSauLanDocCuaMinh()
    {
        Assert.False(_kho.FileBiMayKhacSua());

        MayKhacSuaFile();

        Assert.True(_kho.FileBiMayKhacSua());
    }

    [Fact]
    public void Luu_TuMinhGhiThiKhongCoiLaXungDot()
    {
        _kho.DuLieu.KhachHangs.Add(new KhachHang { Ten = "Khách của mình" });

        Assert.True(_kho.Luu());
        Assert.False(_kho.FileBiMayKhacSua());
        Assert.True(_kho.Luu());
    }

    [Fact]
    public void Luu_KhongAiXuLyXungDotThiBaoLoiChuKhongDeMatDuLieu()
    {
        MayKhacSuaFile();
        _kho.DuLieu.KhachHangs.Add(new KhachHang { Ten = "Khách của mình" });

        var loi = Assert.Throws<XungDotDuLieuException>(() => _kho.Luu());

        Assert.Equal(_duongDanFile, loi.XungDot.DuongDanFile);
        Assert.Contains("Khách của máy kia", File.ReadAllText(_duongDanFile));
        Assert.DoesNotContain("Khách của mình", File.ReadAllText(_duongDanFile));
    }

    [Fact]
    public void Luu_ChonGhiDeThiCatLaiBanCuaMayKhac()
    {
        MayKhacSuaFile();
        XungDotFile? daHoi = null;
        _kho.HoiKhiFileBiMayKhacSua = xungDot =>
        {
            daHoi = xungDot;
            return true;
        };

        _kho.DuLieu.KhachHangs.Add(new KhachHang { Ten = "Khách của mình" });
        Assert.True(_kho.Luu());

        Assert.NotNull(daHoi);
        Assert.Contains("Khách của mình", File.ReadAllText(_duongDanFile));

        // Việc của máy kia không bị mất hẳn, còn nằm trong file cất lại.
        Assert.True(File.Exists(daHoi!.DuongDanCatBanMayKhac));
        Assert.Contains("Khách của máy kia", File.ReadAllText(daHoi.DuongDanCatBanMayKhac));
    }

    [Fact]
    public void Luu_ChonBoThayDoiThiNapLaiBanCuaMayKhac()
    {
        MayKhacSuaFile();
        _kho.HoiKhiFileBiMayKhacSua = _ => false;

        _kho.ThucHien("Thêm khách", () => _kho.DuLieu.KhachHangs.Add(new KhachHang { Ten = "Khách của mình" }));

        var khach = Assert.Single(_kho.DuLieu.KhachHangs);
        Assert.Equal("Khách của máy kia", khach.Ten);
        Assert.DoesNotContain("Khách của mình", File.ReadAllText(_duongDanFile));

        // Lịch sử hoàn tác đã lạc hậu so với file nên phải dọn đi.
        Assert.False(_kho.CoTheHoanTac);
        Assert.False(_kho.CoTheLamLai);
    }

    [Fact]
    public void NapLaiTuFile_LayBanMoiNhatVaBaoManHinhNapLai()
    {
        var soLanBao = 0;
        _kho.DuLieuThayDoi += (_, _) => soLanBao++;
        MayKhacSuaFile();

        _kho.NapLaiTuFile();

        Assert.Equal("Khách của máy kia", Assert.Single(_kho.DuLieu.KhachHangs).Ten);
        Assert.False(_kho.FileBiMayKhacSua());
        Assert.Equal(1, soLanBao);
    }
}
