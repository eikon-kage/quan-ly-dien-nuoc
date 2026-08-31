using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Kiểm tra sao lưu, khôi phục, xuất toàn bộ dữ liệu ra Excel và nhật ký thay đổi.
/// Mỗi test dùng thư mục tạm riêng nên không đụng vào dữ liệu thật.
/// </summary>
public sealed class SaoLuuTests : IDisposable
{
    private readonly string _thuMucTam;
    private readonly KhoDuLieu _kho;
    private readonly CaiDat _caiDat;

    public SaoLuuTests()
    {
        _thuMucTam = Path.Combine(Path.GetTempPath(), "QuanLyDienNuoc.Tests", Guid.NewGuid().ToString("N"));
        _kho = new KhoDuLieu(Path.Combine(_thuMucTam, "dulieu.json"));
        _kho.Nap();
        _caiDat = _kho.CaiDat;
        _caiDat.ThuMucSaoLuu = Path.Combine(_thuMucTam, "SaoLuu");
    }

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }
    }

    private KhachHang ThemKhach(string ten)
    {
        var khach = new KhachHang { Ten = ten, DienThoai = "0900000001", DiaChi = "Hải Hậu" };
        _kho.DuLieu.KhachHangs.Add(khach);

        var hoaDon = new HoaDon
        {
            KhachHangId = khach.Id,
            MaHoaDon = "HD2026-01",
            Nam = 2026,
            NgayMo = new DateTime(2026, 3, 5),
        };
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 5),
            TenHang = "Ống nhựa PVC D27",
            DonVi = "Cây",
            DonGia = 45_000,
            SoLuong = 6,
        });
        hoaDon.ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 4, 1), SoTien = 100_000 });
        _kho.DuLieu.HoaDons.Add(hoaDon);
        _kho.Luu();

        return khach;
    }

    // ---------- Sao lưu ----------

    [Fact]
    public void Tao_TaoCaFileJsonVaFileExcel()
    {
        ThemKhach("Ông Long");

        var ban = SaoLuu.Tao(_kho, _caiDat, new DateTime(2026, 8, 3, 9, 30, 0));

        Assert.True(File.Exists(ban.DuongDanJson));
        Assert.True(ban.CoExcel);
        Assert.EndsWith("sao-luu-2026-08-03-0930.json", ban.DuongDanJson);
        Assert.Equal(new DateTime(2026, 8, 3, 9, 30, 0), _caiDat.LanSaoLuuCuoi);
    }

    [Fact]
    public void Tao_KhongKemExcelKhiTatTuyChon()
    {
        ThemKhach("Ông Long");
        _caiDat.SaoLuuKemExcel = false;

        var ban = SaoLuu.Tao(_kho, _caiDat, new DateTime(2026, 8, 3, 9, 30, 0));

        Assert.False(ban.CoExcel);
        Assert.Null(ban.DuongDanExcel);
    }

    [Fact]
    public void Tao_XoaBotBanCuKhiVuotSoBanGiuLai()
    {
        ThemKhach("Ông Long");
        _caiDat.SoBanSaoLuuGiuLai = 3;

        for (var i = 0; i < 6; i++)
        {
            SaoLuu.Tao(_kho, _caiDat, new DateTime(2026, 8, 3, 9, 0, 0).AddMinutes(i));
        }

        var danhSach = SaoLuu.DanhSach(_caiDat.ThuMucSaoLuu);

        Assert.Equal(3, danhSach.Count);
        Assert.Equal(new DateTime(2026, 8, 3, 9, 5, 0), danhSach[0].Luc);   // bản mới nhất đứng đầu
    }

    [Fact]
    public void TuDongNeuCan_ChiChayMotLanTrongNgay()
    {
        ThemKhach("Ông Long");

        Assert.NotNull(SaoLuu.TuDongNeuCan(_kho, _caiDat, new DateTime(2026, 8, 3, 8, 0, 0)));
        Assert.Null(SaoLuu.TuDongNeuCan(_kho, _caiDat, new DateTime(2026, 8, 3, 17, 0, 0)));
        Assert.NotNull(SaoLuu.TuDongNeuCan(_kho, _caiDat, new DateTime(2026, 8, 4, 8, 0, 0)));
    }

    [Fact]
    public void TuDongNeuCan_KhongChayKhiTatTuDongSaoLuu()
    {
        ThemKhach("Ông Long");
        _caiDat.TuDongSaoLuu = false;

        Assert.Null(SaoLuu.TuDongNeuCan(_kho, _caiDat, new DateTime(2026, 8, 3, 8, 0, 0)));
    }

    // ---------- Khôi phục ----------

    [Fact]
    public void KhoiPhuc_LayLaiDuocDuLieuDaXoa()
    {
        ThemKhach("Ông Long");
        var ban = SaoLuu.Tao(_kho, _caiDat, new DateTime(2026, 8, 3, 9, 0, 0));

        _kho.DuLieu.KhachHangs.Clear();
        _kho.DuLieu.HoaDons.Clear();
        _kho.Luu();
        Assert.Empty(_kho.DuLieu.KhachHangs);

        SaoLuu.KhoiPhuc(_kho, _caiDat, ban, new DateTime(2026, 8, 3, 10, 0, 0));

        Assert.Equal("Ông Long", Assert.Single(_kho.DuLieu.KhachHangs).Ten);
        Assert.Single(_kho.DuLieu.HoaDons);
    }

    [Fact]
    public void KhoiPhuc_CatLaiBanDangDungTruocKhiDe()
    {
        ThemKhach("Ông Long");
        var ban = SaoLuu.Tao(_kho, _caiDat, new DateTime(2026, 8, 3, 9, 0, 0));

        ThemKhach("Cô Gấm");
        SaoLuu.KhoiPhuc(_kho, _caiDat, ban, new DateTime(2026, 8, 3, 10, 0, 0));

        var cat = Directory.GetFiles(_caiDat.ThuMucSaoLuu, "truoc-khi-khoi-phuc-*.json");
        Assert.Single(cat);

        // Bản cất giữ vẫn còn cả hai khách.
        var khoCu = new KhoDuLieu(cat[0]);
        khoCu.Nap();
        Assert.Equal(2, khoCu.DuLieu.KhachHangs.Count);
    }

    [Fact]
    public void KhoiPhuc_BaoLoiKhiThieuFile()
    {
        var ban = new BanSaoLuu(
            Path.Combine(_thuMucTam, "khong-co.json"),
            new DateTime(2026, 8, 3),
            KichThuoc: 0,
            DuongDanExcel: null);

        Assert.Throws<FileNotFoundException>(() => SaoLuu.KhoiPhuc(_kho, _caiDat, ban));
    }

    // ---------- Xuất toàn bộ ra Excel ----------

    [Fact]
    public void XuatToanBo_CoDuCacTrangVaSoLieu()
    {
        var khach = ThemKhach("Ông Long");
        khach.BangGiaRieng[_kho.DuLieu.VatTus[0].Id] = 40_000;

        var file = Path.Combine(_thuMucTam, "toan-bo.xlsx");
        XuatToanBo.Xuat(_kho.DuLieu, file, new DateTime(2026, 8, 3));

        using var doc = File.OpenRead(file);
        var wb = new XSSFWorkbook(doc);

        foreach (var ten in new[]
                 {
                     "Khách hàng", "Hoá đơn", "Chi tiết hàng", "Thanh toán",
                     "Công nợ", "Vật tư", "Bảng giá riêng",
                 })
        {
            Assert.NotNull(wb.GetSheet(ten));
        }

        // Trang chi tiết: một dòng hàng, thành tiền 6 × 45.000
        var chiTiet = wb.GetSheet("Chi tiết hàng");
        Assert.Equal("Ống nhựa PVC D27", chiTiet.GetRow(1).GetCell(3).StringCellValue);
        Assert.Equal(270_000d, chiTiet.GetRow(1).GetCell(7).NumericCellValue);

        // Trang công nợ: mua 270.000, trả 100.000, còn 170.000
        var congNo = wb.GetSheet("Công nợ");
        Assert.Equal("Ông Long", congNo.GetRow(1).GetCell(0).StringCellValue);
        Assert.Equal(170_000d, congNo.GetRow(1).GetCell(5).NumericCellValue);
    }

    [Fact]
    public void XuatToanBo_DongCuoiLaCongThucCongTong()
    {
        ThemKhach("Ông Long");
        ThemKhach("Cô Gấm");

        var file = Path.Combine(_thuMucTam, "toan-bo.xlsx");
        XuatToanBo.Xuat(_kho.DuLieu, file, new DateTime(2026, 8, 3));

        using var doc = File.OpenRead(file);
        var wb = new XSSFWorkbook(doc);
        var chiTiet = wb.GetSheet("Chi tiết hàng");
        var dongTong = chiTiet.GetRow(3);

        Assert.Equal("TỔNG CỘNG", dongTong.GetCell(0).StringCellValue);
        Assert.Equal(CellType.Formula, dongTong.GetCell(7).CellType);
        Assert.Equal("SUM(H2:H3)", dongTong.GetCell(7).CellFormula);
    }

    [Fact]
    public void XuatToanBo_ChayDuocKhiChuaCoDuLieuNao()
    {
        var file = Path.Combine(_thuMucTam, "rong.xlsx");
        XuatToanBo.Xuat(new DuLieuApp(), file, new DateTime(2026, 8, 3));

        Assert.True(File.Exists(file));
    }

    // ---------- Nhật ký ----------

    [Fact]
    public void NhatKy_GhiLaiMoiThayDoiVaDocNguocLaiDuoc()
    {
        var khach = ThemKhach("Ông Long");
        _kho.ThucHien("Sửa khách hàng Ông Long", () => khach.DienThoai = "0911111111", phatSuKien: false);

        var muc = _kho.NhatKy.Doc();

        Assert.Equal("Sửa khách hàng Ông Long", muc[0].MoTa);
    }

    [Fact]
    public void NhatKy_KhongBiXoaKhiHoanTac()
    {
        var khach = ThemKhach("Ông Long");
        _kho.ThucHien("Sửa khách hàng Ông Long", () => khach.DienThoai = "0911111111", phatSuKien: false);
        _kho.HoanTac();

        var muc = _kho.NhatKy.Doc();

        // Cả thao tác sửa lẫn lần hoàn tác đều còn trong nhật ký.
        Assert.Equal("Hoàn tác", muc[0].MoTa);
        Assert.Equal("Sửa khách hàng Ông Long", muc[0].ChiTiet);
        Assert.Contains(muc, m => m.MoTa == "Sửa khách hàng Ông Long");
    }

    [Fact]
    public void NhatKy_DocTraVeRongKhiChuaCoFile()
    {
        var nhatKy = new NhatKy(Path.Combine(_thuMucTam, "chua-co.jsonl"));

        Assert.Empty(nhatKy.Doc());
    }

    // ---------- Cài đặt ----------

    [Fact]
    public void CaiDat_LuuVaDocLaiDuoc()
    {
        _kho.CaiDat.SoNgayNhacNo = 45;
        _kho.CaiDat.NguongLechGia = 35;
        _kho.LuuCaiDat();

        var khoMoi = new KhoDuLieu(_kho.DuongDanFile);
        khoMoi.Nap();

        Assert.Equal(45, khoMoi.CaiDat.SoNgayNhacNo);
        Assert.Equal(35, khoMoi.CaiDat.NguongLechGia);
    }

    [Fact]
    public void CaiDat_ThuMucSaoLuuMacDinhNamCanhFileDuLieu()
    {
        var caiDat = new CaiDat();

        Assert.Equal(Path.Combine(_thuMucTam, "SaoLuu"), caiDat.ThuMucSaoLuuThat(_kho.DuongDanFile));
    }
}
