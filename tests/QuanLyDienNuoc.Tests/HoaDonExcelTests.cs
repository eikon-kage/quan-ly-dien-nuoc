using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra xuất hoá đơn ra mẫu Excel của cửa hàng và đọc ngược lại.</summary>
public class HoaDonExcelTests : IDisposable
{
    private static readonly string ThuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
    private static readonly string ThuMucHoaDonCu = Path.Combine(AppContext.BaseDirectory, "HoaDonCu");

    private readonly string _thuMucTam = Path.Combine(
        Path.GetTempPath(),
        "qldn-test-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(0, new[] { 0 })]           // hoá đơn rỗng vẫn in ra một trang
    [InlineData(1, new[] { 1 })]
    [InlineData(25, new[] { 25 })]         // vừa đủ trang 1
    [InlineData(26, new[] { 25, 1 })]
    [InlineData(60, new[] { 25, 35 })]     // vừa đủ hai trang
    [InlineData(61, new[] { 25, 35, 1 })]
    public void ChiaTrang_ChiaDungSucChuaTungTrang(int soDong, int[] mongDoi)
    {
        var chiTiet = Enumerable.Range(1, soDong)
            .Select(i => new ChiTietHoaDon { TenHang = $"Hàng {i}", SoLuong = 1, DonGia = 1000 })
            .ToList();

        var trang = XuatHoaDon.ChiaTrang(chiTiet);

        Assert.Equal(mongDoi, trang.Select(t => t.Count).ToArray());
    }

    [Fact]
    public void Xuat_RoiDoc_GiuNguyenDongTraLai()
    {
        var khach = new KhachHang { Ten = "Ông Mẫu" };
        var hoaDon = new HoaDon { MaHoaDon = "HD2026-01" };
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 1),
            TenHang = "Ống 27",
            DonVi = "Cây",
            DonGia = 45000,
            SoLuong = 10,
        });
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 2),
            TenHang = "Ống 27 (trả lại)",
            DonVi = "Cây",
            DonGia = 45000,
            SoLuong = -2,
        });

        var fileRa = Path.Combine(_thuMucTam, "hoa-don-tra-lai.xls");
        XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));

        var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 1, 1));
        var dongTraLai = doc.Trang[0].Dong[1];

        Assert.Equal(-2m, dongTraLai.SoLuong);
        Assert.Equal(45000m, dongTraLai.DonGia);
        Assert.Equal(-90_000m, dongTraLai.ThanhTien);

        // Tổng của tờ hoá đơn đã trừ phần trả lại.
        Assert.Equal(360_000m, doc.Trang[0].TongTien);
        Assert.Equal(hoaDon.TongTien, doc.Trang.Sum(t => t.TongTien));
    }

    [Fact]
    public void ChiaTrang_SapXepTheoNgay()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            new() { Ngay = new DateTime(2026, 5, 10), TenHang = "Sau" },
            new() { Ngay = new DateTime(2026, 3, 1), TenHang = "Truoc" },
        };

        var trang = XuatHoaDon.ChiaTrang(chiTiet);

        Assert.Equal("Truoc", trang[0][0].TenHang);
        Assert.Equal("Sau", trang[0][1].TenHang);
    }

    [Fact]
    public void Xuat_RoiDoc_GiuNguyenDuLieu()
    {
        var khach = new KhachHang { Ten = "Ông Mẫu", DiaChi = "Xóm 5" };
        var hoaDon = new HoaDon { MaHoaDon = "HD2026-01" };
        for (var i = 1; i <= 40; i++)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = new DateTime(2026, 3, 1).AddDays(i),
                TenHang = $"Ống 90 #{i}",
                DonVi = "m",
                DonGia = 17000,
                SoLuong = 2.5m,
            });
        }

        var fileRa = Path.Combine(_thuMucTam, "hoa-don.xls");
        XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));

        Assert.True(File.Exists(fileRa));

        var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 1, 1));

        Assert.Equal(2, doc.Trang.Count);                       // 40 dòng => 2 trang
        Assert.Equal(40, doc.TongSoDong);
        Assert.Equal("Ông Mẫu", doc.TenKhach);
        Assert.Equal(new DateTime(2026, 8, 3), doc.NgayTrenHoaDon);
        Assert.Equal(hoaDon.TongTien, doc.Trang.Sum(t => t.TongTien));

        var dongDau = doc.Trang[0].Dong[0];
        Assert.Equal("Ống 90 #1", dongDau.TenHang);
        Assert.Equal("m", dongDau.DonVi);
        Assert.Equal(2.5m, dongDau.SoLuong);
        Assert.Equal(17000m, dongDau.DonGia);
    }

    [Fact]
    public void Xuat_HoaDonNgan_ChiMotTrang()
    {
        var khach = new KhachHang { Ten = "Cô Gấm" };
        var hoaDon = new HoaDon { MaHoaDon = "HD2026-02" };
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Bóng đèn LED", DonVi = "Bóng", DonGia = 45000, SoLuong = 3 });

        var fileRa = Path.Combine(_thuMucTam, "mot-trang.xls");
        XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau);

        var doc = DocHoaDon.Doc(fileRa, DateTime.Today);

        Assert.Single(doc.Trang);
        Assert.Single(doc.Trang[0].Dong);
        Assert.Equal(135000m, doc.Trang[0].TongTien);
    }

    [Fact]
    public void Xuat_DungFileGocNhieuTab_LayDungTabMauVaBoTabThua()
    {
        // Thả thẳng hai file hoá đơn gốc vào thư mục mẫu: to1 có 3 tab, to2 có 4 tab (kèm tab biểu đồ).
        Directory.CreateDirectory(_thuMucTam);
        var thuMucMau = Path.Combine(_thuMucTam, "MauGoc");
        Directory.CreateDirectory(thuMucMau);
        File.Copy(Path.Combine(ThuMucHoaDonCu, "to1.xls"), Path.Combine(thuMucMau, MauHoaDon.TenFileTrang1));
        File.Copy(Path.Combine(ThuMucHoaDonCu, "to2.xls"), Path.Combine(thuMucMau, MauHoaDon.TenFileTrangSau));

        var khach = new KhachHang { Ten = "Chú Hải", DiaChi = "Hải Minh" };
        var hoaDon = new HoaDon { MaHoaDon = "HD2026-09" };
        for (var i = 1; i <= 35; i++)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = $"Van khoá {i}", DonVi = "Cái", DonGia = 55000, SoLuong = 1 });
        }

        var fileRa = Path.Combine(_thuMucTam, "tu-file-goc.xls");
        XuatHoaDon.Xuat(hoaDon, khach, fileRa, thuMucMau);

        var doc = DocHoaDon.Doc(fileRa, DateTime.Today);

        // Chỉ còn đúng hai trang, không dính tab mẫu cũ hay tab biểu đồ "Chart1".
        Assert.Equal(new[] { "Trang 1", "Trang 2" }, doc.Trang.Select(t => t.TenSheet).ToArray());
        Assert.Equal(35, doc.TongSoDong);
        Assert.Equal("Chú Hải", doc.TenKhach);
        Assert.Equal(hoaDon.TongTien, doc.Trang.Sum(t => t.TongTien));

        // Dữ liệu cũ trong file mẫu phải bị dọn sạch, không lẫn vào hoá đơn mới.
        Assert.All(doc.Trang, t => Assert.All(t.Dong, d => Assert.StartsWith("Van khoá", d.TenHang)));
    }

    [Fact]
    public void ThongTinCuaHang_DocDuocPhanDauTuTabDungTen()
    {
        var cuaHang = ThongTinCuaHang.DocTuMau(ThuMucMau);

        Assert.Contains("ĐIỆN NƯỚC", cuaHang.Ten);

        // Mẫu giấy đang dùng in số tài khoản ngân hàng kín cả bốn dòng góc trên phải, không còn
        // ô tên tờ "HÓA ĐƠN BÁN HÀNG" như mẫu cũ — bản in phải biết để không phóng to dòng đó.
        Assert.Contains("Số tài khoản", cuaHang.NganhNghe1);
        Assert.Contains("Agribank", cuaHang.NganhNghe2);
        Assert.False(cuaHang.CoTenTo);
    }

    [Fact]
    public void ThongTinCuaHang_FileGocNhieuTab_LayTabKhopVoiToaDoDangDung()
    {
        // to1.xls có cả hai kiểu mẫu: tab "mẫu hoá đơn mối" là mẫu cũ (có ô "HÓA ĐƠN BÁN HÀNG",
        // bảng 32 dòng), tab "mau hoa don cũ" là tờ đang dùng (số tài khoản, bảng 25 dòng).
        // Toạ độ trong MauHoaDon đo theo tờ đang dùng nên phải lấy đúng tab đó.
        Directory.CreateDirectory(_thuMucTam);
        var thuMucMau = Path.Combine(_thuMucTam, "MauGoc");
        Directory.CreateDirectory(thuMucMau);
        File.Copy(Path.Combine(ThuMucHoaDonCu, "to1.xls"), Path.Combine(thuMucMau, MauHoaDon.TenFileTrang1));

        var cuaHang = ThongTinCuaHang.DocTuMau(thuMucMau);

        Assert.Contains("Số tài khoản", cuaHang.NganhNghe1);
        Assert.False(cuaHang.CoTenTo);
    }

    [Fact]
    public void ThongTinCuaHang_MauCoOTenTo_ThiNhanRaLaTenTo()
    {
        // Mẫu cũ vẫn phải dùng được: ô góc phải có chữ "hoá đơn" thì đó là tên tờ, bản in
        // phóng to như trước chứ không hạ xuống cỡ chữ thường.
        var cuaHang = ThongTinCuaHang.MacDinh with { TieuDe = "HÓA ĐƠN BÁN HÀNG" };

        Assert.True(cuaHang.CoTenTo);
    }

    [Fact]
    public void ThongTinCuaHang_ThieuFileMau_VanCoTenTo()
    {
        var cuaHang = ThongTinCuaHang.DocTuMau(Path.Combine(_thuMucTam, "khong-co"));

        Assert.Same(ThongTinCuaHang.MacDinh, cuaHang);
        Assert.True(cuaHang.CoTenTo);
    }

    [Fact]
    public void Doc_BoQuaSheetBieuDoCuaFileCu()
    {
        // to2.xls có sheet "Chart1" chứa dữ liệu biểu đồ xoay ngang, không phải bảng hàng.
        var file = Path.Combine(ThuMucHoaDonCu, "to2.xls");
        Assert.True(File.Exists(file), $"Thiếu file kiểm thử: {file}");

        var doc = DocHoaDon.Doc(file, DateTime.Today);

        Assert.DoesNotContain(doc.Trang, t => t.TenSheet == "Chart1");
        Assert.All(doc.Trang, t => Assert.DoesNotContain(t.Dong, d => d.TenHang is "ĐVT" or "SỐ LƯỢNG" or "Đơn giá"));
    }

    [Fact]
    public void Doc_HoaDonCuThat_LayDuocTenKhachVaDongHang()
    {
        var file = Path.Combine(ThuMucHoaDonCu, "to1.xls");
        Assert.True(File.Exists(file), $"Thiếu file kiểm thử: {file}");

        var doc = DocHoaDon.Doc(file, new DateTime(2026, 8, 3));
        var trangDau = doc.Trang[0];

        Assert.Equal("Ông Mẫu", trangDau.TenKhach);
        Assert.Equal(32, trangDau.Dong.Count);
        Assert.Equal(2507900m, trangDau.TongTien);
        Assert.Equal("Ống 90", trangDau.Dong[0].TenHang);
        Assert.Equal(143000m, trangDau.Dong[0].DonGia);
        Assert.All(trangDau.Dong, d => Assert.Equal(new DateTime(2026, 8, 3), d.Ngay));
    }

    [Fact]
    public void Doc_TuTinhDonGiaKhiFileCuChiCoThanhTien()
    {
        var khach = new KhachHang { Ten = "Khách" };
        var hoaDon = new HoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Dây điện", DonVi = "m", DonGia = 0, SoLuong = 10 });

        var fileRa = Path.Combine(_thuMucTam, "thieu-gia.xls");
        XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau);

        var doc = DocHoaDon.Doc(fileRa, DateTime.Today);

        // Không có đơn giá lẫn thành tiền thì giữ 0 và báo để người dùng sửa.
        Assert.Equal(0m, doc.Trang[0].Dong[0].DonGia);
    }
}
