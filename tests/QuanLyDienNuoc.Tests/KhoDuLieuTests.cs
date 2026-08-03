using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Kiểm tra kho dữ liệu: nạp/lưu file, hoàn tác/làm lại, giá riêng của khách.
/// Mỗi test dùng một thư mục tạm riêng nên không đụng vào dữ liệu thật.
/// </summary>
public sealed class KhoDuLieuTests : IDisposable
{
    private readonly string _thuMucTam;
    private readonly KhoDuLieu _kho;

    public KhoDuLieuTests()
    {
        _thuMucTam = Path.Combine(Path.GetTempPath(), "QuanLyDienNuoc.Tests", Guid.NewGuid().ToString("N"));
        _kho = new KhoDuLieu(Path.Combine(_thuMucTam, "dulieu.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }
    }

    private KhachHang ThemKhach(string ten = "Nguyễn Văn A")
    {
        var khach = new KhachHang { Ten = ten };
        _kho.DuLieu.KhachHangs.Add(khach);
        return khach;
    }

    // ---------- Nạp / lưu ----------

    [Fact]
    public void Nap_TaoFileVaDanhMucMauKhiChuaCoDuLieu()
    {
        Assert.False(File.Exists(_kho.DuongDanFile));

        _kho.Nap();

        Assert.True(File.Exists(_kho.DuongDanFile));
        Assert.NotEmpty(_kho.DuLieu.VatTus);
        Assert.Empty(_kho.DuLieu.KhachHangs);
    }

    [Fact]
    public void Nap_DocLaiDungDuLieuDaLuu()
    {
        _kho.Nap();
        var khach = ThemKhach("Trần Thị B");
        khach.DienThoai = "0900000001";
        _kho.Luu();

        // Kho thứ hai trỏ vào cùng file, giống như mở lại phần mềm.
        var khoMoi = new KhoDuLieu(_kho.DuongDanFile);
        khoMoi.Nap();

        var docLai = Assert.Single(khoMoi.DuLieu.KhachHangs);
        Assert.Equal("Trần Thị B", docLai.Ten);
        Assert.Equal("0900000001", docLai.DienThoai);
        Assert.Equal(khach.Id, docLai.Id);
    }

    [Fact]
    public void Luu_GiuLaiBanSaoBakCuaLanLuuTruoc()
    {
        var fileBak = _kho.DuongDanFile + ".bak";

        // Nạp lần đầu đã tự tạo file, nên chưa có gì để sao lưu.
        _kho.Nap();
        Assert.False(File.Exists(fileBak));

        ThemKhach("Khách một");
        _kho.Luu();

        // .bak giữ trạng thái trước khi thêm "Khách một".
        Assert.True(File.Exists(fileBak));
        Assert.DoesNotContain("Khách một", File.ReadAllText(fileBak));

        ThemKhach("Khách hai");
        _kho.Luu();

        // .bak lùi lại đúng một bước: có "Khách một" nhưng chưa có "Khách hai".
        var noiDungBak = File.ReadAllText(fileBak);
        Assert.Contains("Khách một", noiDungBak);
        Assert.DoesNotContain("Khách hai", noiDungBak);
        Assert.Contains("Khách hai", File.ReadAllText(_kho.DuongDanFile));
    }

    [Fact]
    public void Luu_KhongDeLaiFileTam()
    {
        _kho.Nap();
        _kho.Luu();

        Assert.False(File.Exists(_kho.DuongDanFile + ".tmp"));
    }

    // ---------- Hoàn tác / làm lại ----------

    [Fact]
    public void ChuaLamGi_ThiKhongHoanTacDuoc()
    {
        _kho.Nap();

        Assert.False(_kho.CoTheHoanTac);
        Assert.False(_kho.CoTheLamLai);
        Assert.Null(_kho.HoanTac());
        Assert.Null(_kho.LamLai());
    }

    [Fact]
    public void HoanTac_TraVeTrangThaiTruocThaoTac()
    {
        _kho.Nap();
        _kho.ThucHien("Thêm khách", () => ThemKhach("Khách A"));

        Assert.Single(_kho.DuLieu.KhachHangs);
        Assert.True(_kho.CoTheHoanTac);
        Assert.Equal("Thêm khách", _kho.MoTaHoanTac);

        var moTa = _kho.HoanTac();

        Assert.Equal("Thêm khách", moTa);
        Assert.Empty(_kho.DuLieu.KhachHangs);
        Assert.False(_kho.CoTheHoanTac);
        Assert.True(_kho.CoTheLamLai);
    }

    [Fact]
    public void LamLai_KhoiPhucThaoTacVuaHoanTac()
    {
        _kho.Nap();
        _kho.ThucHien("Thêm khách", () => ThemKhach("Khách A"));
        _kho.HoanTac();

        var moTa = _kho.LamLai();

        Assert.Equal("Thêm khách", moTa);
        Assert.Equal("Khách A", Assert.Single(_kho.DuLieu.KhachHangs).Ten);
        Assert.False(_kho.CoTheLamLai);
        Assert.True(_kho.CoTheHoanTac);
    }

    [Fact]
    public void HoanTac_LanNguocDungThuTuQuaNhieuBuoc()
    {
        _kho.Nap();
        _kho.ThucHien("Thêm A", () => ThemKhach("A"));
        _kho.ThucHien("Thêm B", () => ThemKhach("B"));
        _kho.ThucHien("Thêm C", () => ThemKhach("C"));

        Assert.Equal(3, _kho.DuLieu.KhachHangs.Count);

        Assert.Equal("Thêm C", _kho.HoanTac());
        Assert.Equal(new[] { "A", "B" }, _kho.DuLieu.KhachHangs.Select(k => k.Ten));

        Assert.Equal("Thêm B", _kho.HoanTac());
        Assert.Equal(new[] { "A" }, _kho.DuLieu.KhachHangs.Select(k => k.Ten));

        Assert.Equal("Thêm A", _kho.HoanTac());
        Assert.Empty(_kho.DuLieu.KhachHangs);
    }

    [Fact]
    public void ThaoTacMoi_XoaSachHangDoiLamLai()
    {
        _kho.Nap();
        _kho.ThucHien("Thêm A", () => ThemKhach("A"));
        _kho.HoanTac();

        Assert.True(_kho.CoTheLamLai);

        // Làm việc khác sau khi hoàn tác thì không còn "làm lại" được nữa.
        _kho.ThucHien("Thêm B", () => ThemKhach("B"));

        Assert.False(_kho.CoTheLamLai);
        Assert.Equal("B", Assert.Single(_kho.DuLieu.KhachHangs).Ten);
    }

    [Fact]
    public void HoanTac_GhiLuonXuongFile()
    {
        _kho.Nap();
        _kho.ThucHien("Thêm khách", () => ThemKhach("Khách A"));
        Assert.Contains("Khách A", File.ReadAllText(_kho.DuongDanFile));

        _kho.HoanTac();

        // Đóng phần mềm ngay sau khi Ctrl+Z thì mở lại vẫn phải thấy đã hoàn tác.
        Assert.DoesNotContain("Khách A", File.ReadAllText(_kho.DuongDanFile));
    }

    [Fact]
    public void ThucHien_PhatSuKienDeManHinhNapLai()
    {
        _kho.Nap();
        var soLanBao = 0;
        _kho.DuLieuThayDoi += (_, _) => soLanBao++;

        _kho.ThucHien("Thêm khách", () => ThemKhach());
        Assert.Equal(1, soLanBao);

        _kho.HoanTac();
        Assert.Equal(2, soLanBao);

        _kho.LamLai();
        Assert.Equal(3, soLanBao);
    }

    [Fact]
    public void ThucHien_KhongPhatSuKienKhiDuocYeuCau()
    {
        _kho.Nap();
        var soLanBao = 0;
        _kho.DuLieuThayDoi += (_, _) => soLanBao++;

        _kho.ThucHien("Thêm khách", () => ThemKhach(), phatSuKien: false);

        Assert.Equal(0, soLanBao);
        Assert.Single(_kho.DuLieu.KhachHangs);
    }

    // ---------- Giá riêng của khách ----------

    [Fact]
    public void GiaCho_LayGiaMacDinhKhiKhachChuaCoGiaRieng()
    {
        var khach = new KhachHang();
        var vatTu = new VatTu { DonGiaMacDinh = 32000 };

        Assert.Equal(32000m, _kho.GiaCho(khach, vatTu));
    }

    [Fact]
    public void GiaCho_UuTienGiaRiengCuaKhach()
    {
        var vatTu = new VatTu { DonGiaMacDinh = 32000 };
        var khach = new KhachHang { BangGiaRieng = { [vatTu.Id] = 28000 } };

        Assert.Equal(28000m, _kho.GiaCho(khach, vatTu));
    }

    [Fact]
    public void GiaCho_BoQuaGiaRiengBangKhong()
    {
        var vatTu = new VatTu { DonGiaMacDinh = 32000 };
        var khach = new KhachHang { BangGiaRieng = { [vatTu.Id] = 0 } };

        Assert.Equal(32000m, _kho.GiaCho(khach, vatTu));
    }

    // ---------- Mã hoá đơn / danh sách năm ----------

    [Fact]
    public void TaoMaHoaDon_DanhSoTangDanTheoTungKhachTungNam()
    {
        var khach = ThemKhach();

        Assert.Equal("HD2026-01", _kho.TaoMaHoaDon(khach.Id, 2026));

        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khach.Id, Nam = 2026 });
        Assert.Equal("HD2026-02", _kho.TaoMaHoaDon(khach.Id, 2026));

        // Sang năm khác thì đánh số lại từ đầu.
        Assert.Equal("HD2027-01", _kho.TaoMaHoaDon(khach.Id, 2027));
    }

    [Fact]
    public void TaoMaHoaDon_DemRiengChoTungKhach()
    {
        var khachA = ThemKhach("A");
        var khachB = ThemKhach("B");
        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khachA.Id, Nam = 2026 });

        Assert.Equal("HD2026-02", _kho.TaoMaHoaDon(khachA.Id, 2026));
        Assert.Equal("HD2026-01", _kho.TaoMaHoaDon(khachB.Id, 2026));
    }

    [Fact]
    public void DanhSachNam_LuonCoNamHienTaiVaXepMoiTruoc()
    {
        var khach = ThemKhach();
        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khach.Id, Nam = 2024 });
        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khach.Id, Nam = 2022 });

        var nams = _kho.DanhSachNam();

        Assert.Contains(DateTime.Today.Year, nams);
        Assert.Equal(nams.OrderByDescending(n => n), nams);
        Assert.Contains(2024, nams);
        Assert.Contains(2022, nams);
    }

    [Fact]
    public void HoaDonCuaKhach_ChiLayDungKhachDungNamVaXepMoiTruoc()
    {
        var khachA = ThemKhach("A");
        var khachB = ThemKhach("B");

        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khachA.Id, Nam = 2026, NgayMo = new DateTime(2026, 1, 5) });
        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khachA.Id, Nam = 2026, NgayMo = new DateTime(2026, 6, 1) });
        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khachA.Id, Nam = 2025, NgayMo = new DateTime(2025, 3, 1) });
        _kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khachB.Id, Nam = 2026, NgayMo = new DateTime(2026, 2, 1) });

        var hoaDons = _kho.HoaDonCuaKhach(khachA.Id, 2026);

        Assert.Equal(2, hoaDons.Count);
        Assert.Equal(new DateTime(2026, 6, 1), hoaDons[0].NgayMo);
        Assert.Equal(new DateTime(2026, 1, 5), hoaDons[1].NgayMo);
    }

    // ---------- Tìm kiếm ----------

    [Fact]
    public void TimVatTuTheoTen_KhongPhanBietHoaThuongVaKhoangTrangThua()
    {
        _kho.DuLieu.VatTus.Add(new VatTu { Ten = "Ống nhựa PVC D21" });

        Assert.NotNull(_kho.TimVatTuTheoTen("ống nhựa pvc d21"));
        Assert.NotNull(_kho.TimVatTuTheoTen("  Ống nhựa PVC D21  "));
        Assert.Null(_kho.TimVatTuTheoTen("Ống nhựa PVC D27"));
    }

    [Fact]
    public void TimKhach_TraVeNullKhiKhongCo()
    {
        var khach = ThemKhach();

        Assert.Same(khach, _kho.TimKhach(khach.Id));
        Assert.Null(_kho.TimKhach(Guid.NewGuid()));
    }
}
