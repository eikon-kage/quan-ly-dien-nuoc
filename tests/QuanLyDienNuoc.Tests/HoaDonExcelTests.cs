using NPOI.HSSF.UserModel;
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

    // Mốc ngày ghi đè lên ô số thứ tự của dòng hàng nên không ăn thêm dòng nào: trang 1 chứa
    // đủ 25 dòng hàng, trang sau 35, dù tờ gom hàng của bao nhiêu ngày.
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
    public void LenTrang_MocNgayNamODongHangDauTienCuaNgayDo()
    {
        // Tờ của khách mối gom hàng mỗi ngày một ít: mỗi ngày lấy hai món, ngày ghi vào ô số
        // thứ tự của món đầu tiên hôm ấy chứ không chiếm riêng một dòng.
        var chiTiet = Enumerable.Range(0, 24)
            .Select(i => new ChiTietHoaDon
            {
                Ngay = new DateTime(2026, 3, 1).AddDays(i / 2), TenHang = $"Hàng {i + 1}", SoLuong = 1, DonGia = 1000,
            })
            .ToList();

        var trang = XuatHoaDon.LenTrang(chiTiet);

        // 24 dòng hàng của 12 ngày vẫn là 24 dòng trên giấy, vừa trong trang 1.
        Assert.Single(trang);
        Assert.Equal(24, trang[0].Count);
        Assert.Equal(12, trang[0].Count(d => d.Moc is not null));

        // Mốc nằm ở dòng hàng đầu của mỗi ngày, dòng thứ hai cùng ngày thì không.
        Assert.All(trang[0].Where((_, i) => i % 2 == 0), d => Assert.NotNull(d.Moc));
        Assert.All(trang[0].Where((_, i) => i % 2 == 1), d => Assert.Null(d.Moc));

        // Số thứ tự vẫn chạy liên tục, kể cả ở dòng có mốc (dòng đó in ngày thay cho số).
        Assert.Equal(
            Enumerable.Range(1, 24).ToArray(),
            trang[0].Select(d => d.SoThuTu).ToArray());
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
    public void ChiaTrang_GiuNguyenThuTuTrongSo_KhongTuXepTheoNgay()
    {
        var chiTiet = new List<ChiTietHoaDon>
        {
            new() { Ngay = new DateTime(2026, 5, 10), TenHang = "Go truoc" },
            new() { Ngay = new DateTime(2026, 3, 1), TenHang = "Go sau" },
        };

        var trang = XuatHoaDon.ChiaTrang(chiTiet);

        // Dòng ngày 10/5 gõ trước thì in ra vẫn nằm trước, dù ngày của nó muộn hơn: bảng trên màn
        // hình thấy sao thì tờ giấy đúng như vậy.
        Assert.Equal("Go truoc", trang[0][0].TenHang);
        Assert.Equal("Go sau", trang[0][1].TenHang);
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
        var daGhi = XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));

        Assert.All(daGhi, f => Assert.True(File.Exists(f)));

        // 40 dòng của 40 ngày khác nhau vẫn chỉ là 40 dòng trên giấy: 25 dòng trang 1 và 15
        // dòng trang 2, mỗi trang một file.
        Assert.Equal(2, daGhi.Count);

        var trang = daGhi
            .Select(f => DocHoaDon.Doc(f, new DateTime(2026, 1, 1)).Trang.Single())
            .ToList();

        Assert.Equal(40, trang.Sum(t => t.Dong.Count));
        Assert.Equal("Ông Mẫu", trang[0].TenKhach);
        Assert.Equal(new DateTime(2026, 8, 3), trang[^1].NgayTrenHoaDon);
        Assert.Equal(hoaDon.TongTien, trang.Sum(t => t.TongTien));

        var dongDau = trang[0].Dong[0];
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
        var daGhi = XuatHoaDon.Xuat(hoaDon, khach, fileRa, thuMucMau);

        var trang = daGhi.Select(f => DocHoaDon.Doc(f, DateTime.Today).Trang.Single()).ToList();

        // Mỗi file đúng một trang, không dính tab mẫu cũ hay tab biểu đồ "Chart1".
        Assert.Equal(new[] { "Trang 1", "Trang 2" }, trang.Select(t => t.TenSheet).ToArray());
        Assert.Equal(35, trang.Sum(t => t.Dong.Count));
        Assert.Equal("Chú Hải", trang[0].TenKhach);
        Assert.Equal(hoaDon.TongTien, trang.Sum(t => t.TongTien));

        // Dữ liệu cũ trong file mẫu phải bị dọn sạch, không lẫn vào hoá đơn mới.
        Assert.All(trang, t => Assert.All(t.Dong, d => Assert.StartsWith("Van khoá", d.TenHang)));
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
    public void Doc_FileCoCaTabMauCuVaMauMoi_ThiChiLayTabMauCu()
    {
        // to2.xls là tờ thật của cửa hàng: tab "mau cũ" là tờ đã điền cho khách, tab "mẫu mới"
        // là mẫu trắng còn sót mấy dòng ví dụ (bóng điện, bệt, công làm — file nào cũng y hệt).
        // Lấy cả hai là sổ có thêm gần 4 triệu tiền hàng chẳng ai mua.
        var file = Path.Combine(ThuMucHoaDonCu, "to2.xls");
        Assert.True(File.Exists(file), $"Thiếu file kiểm thử: {file}");

        var doc = DocHoaDon.Doc(file, DateTime.Today);

        Assert.Equal("mau cũ", Assert.Single(doc.Trang).TenSheet);
        Assert.DoesNotContain(doc.Trang.SelectMany(t => t.Dong), d => d.TenHang == "Bệt");
    }

    [Fact]
    public void Doc_TabMauCuKhongCoDongNao_ThiVanLayTabMauMoi()
    {
        // Bỏ tab "mẫu mới" chỉ vì có tab "mẫu cũ" nằm cạnh: người điền vào tab kia thì cả file
        // trắng trơn, mà trên màn hình nhập chẳng có gì nói vì sao.
        var doc = DocHaiTabMau(coDongOMauCu: false);

        Assert.Equal("mẫu mới", Assert.Single(doc.Trang).TenSheet);
    }

    [Fact]
    public void Doc_TenTabKhongPhaiDungHaiChuMauCuMauMoi_ThiGiuCaHai()
    {
        // to1.xls đặt tên tab là "mẫu hoá đơn mối" và "mau hoa don cũ", mà tờ đã điền của khách
        // lại nằm ở tab "mối" — so lỏng tay là vứt đúng tờ cần lấy.
        var doc = DocHoaDon.Doc(Path.Combine(ThuMucHoaDonCu, "to1.xls"), DateTime.Today);

        Assert.Equal(2, doc.Trang.Count);
        Assert.Equal("Ông Mẫu", doc.Trang[0].TenKhach);
    }

    /// <summary>Một file hai tab "mau cũ" và "mẫu mới", chọn tab nào có dòng hàng.</summary>
    private KetQuaDocExcel DocHaiTabMau(bool coDongOMauCu)
    {
        Directory.CreateDirectory(_thuMucTam);
        var file = Path.Combine(_thuMucTam, "hai-tab-mau.xls");

        var wb = new HSSFWorkbook();
        foreach (var ten in new[] { "mau cũ", "mẫu mới" })
        {
            var sheet = wb.CreateSheet(ten);
            var tieuDe = sheet.CreateRow(0);
            foreach (var (cot, chu) in new[] { (0, "TÊN HÀNG"), (1, "ĐVT"), (2, "SỐ LƯỢNG"), (3, "ĐƠN GIÁ") })
            {
                tieuDe.CreateCell(cot).SetCellValue(chu);
            }

            if (coDongOMauCu == (ten == "mau cũ"))
            {
                var hang = sheet.CreateRow(1);
                hang.CreateCell(0).SetCellValue("Ống 27");
                hang.CreateCell(1).SetCellValue("Cây");
                hang.CreateCell(2).SetCellValue(2d);
                hang.CreateCell(3).SetCellValue(45000d);
            }
        }

        using (var ghi = new FileStream(file, FileMode.Create, FileAccess.Write))
        {
            wb.Write(ghi, leaveOpen: false);
        }

        return DocHoaDon.Doc(file, new DateTime(2026, 6, 2));
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
    public void Doc_ToHoanGoTayHaiDongTieuDe_VanLayDuocHoaDonGocVaLyDo()
    {
        // Tờ hoàn gõ tay trên Excel (hoặc mẫu giấy cũ có dòng phụ đề riêng): tên tờ một dòng,
        // "hoàn cho hoá đơn nào" một dòng khác. Cả hai kiểu đều phải đọc ra được hoá đơn gốc.
        Directory.CreateDirectory(_thuMucTam);
        var file = Path.Combine(_thuMucTam, "hoan-go-tay.xls");

        var wb = new HSSFWorkbook();
        var sheet = wb.CreateSheet("Trang 1");
        sheet.CreateRow(0).CreateCell(0).SetCellValue("HÓA ĐƠN HOÀN HÀNG");
        sheet.CreateRow(1).CreateCell(0)
            .SetCellValue("(Hoàn cho hoá đơn HD2026-02 ngày 02/06/2026 — hàng lỗi)");

        var tieuDe = sheet.CreateRow(2);
        foreach (var (cot, chu) in new[] { (0, "TÊN HÀNG"), (1, "ĐVT"), (2, "SỐ LƯỢNG"), (3, "ĐƠN GIÁ") })
        {
            tieuDe.CreateCell(cot).SetCellValue(chu);
        }

        var hang = sheet.CreateRow(3);
        hang.CreateCell(0).SetCellValue("Ống 27");
        hang.CreateCell(1).SetCellValue("Cây");
        hang.CreateCell(2).SetCellValue(2d);
        hang.CreateCell(3).SetCellValue(45000d);

        using (var ghi = new FileStream(file, FileMode.Create, FileAccess.Write))
        {
            wb.Write(ghi, leaveOpen: false);
        }

        var doc = DocHoaDon.Doc(file, new DateTime(2026, 6, 2));

        Assert.True(doc.Trang[0].LaHoanHang);
        Assert.Equal("HD2026-02", doc.MaHoaDonGoc);
        Assert.Equal("hàng lỗi", doc.LyDoHoan);

        // Trên giấy ghi 2, vào sổ là hàng trả về nên thành -2.
        Assert.Equal(-2m, Assert.Single(doc.Trang[0].Dong).SoLuong);
        Assert.Equal(-90_000m, doc.Trang[0].TongTien);
    }

    [Fact]
    public void Doc_ToHoanMauCuDungRiengThiVanLayDuocLyDoODongPhuDe()
    {
        // Mẫu giấy cũ có dòng phụ đề riêng. Tờ hoàn không nối vào hoá đơn nào thì dòng đó chỉ
        // còn đúng lý do — không có chữ "hoàn" nào để nhận ra, mà vẫn phải lấy được lý do.
        var doc = DocMotToHoanGoTay("hoan-mau-cu.xls", "HÓA ĐƠN HOÀN HÀNG", "(Hàng lỗi)");

        Assert.True(doc.Trang[0].LaHoanHang);
        Assert.Null(doc.MaHoaDonGoc);
        Assert.Equal("Hàng lỗi", doc.LyDoHoan);
    }

    [Fact]
    public void Doc_LyDoCoDauGachMaKhongCoMaHoaDonGocThiKhongBiCatMatNuaCau()
    {
        // Dấu gạch dài chỉ là chỗ ngăn giữa mã hoá đơn gốc và lý do. Câu không có mã thì cắt ở
        // dấu gạch là mất nửa câu lý do người bán đã ghi.
        var doc = DocMotToHoanGoTay(
            "hoan-ly-do-co-gach.xls",
            "HÓA ĐƠN HOÀN HÀNG (Hàng lỗi — sứt vòi, khách không nhận)",
            null);

        Assert.Null(doc.MaHoaDonGoc);
        Assert.Equal("Hàng lỗi — sứt vòi, khách không nhận", doc.LyDoHoan);
    }

    /// <summary>Một tờ hoàn gõ tay: dòng tên tờ, dòng phụ đề (nếu có) rồi một dòng hàng.</summary>
    private KetQuaDocExcel DocMotToHoanGoTay(string tenFile, string tenTo, string? phuDe)
    {
        Directory.CreateDirectory(_thuMucTam);
        var file = Path.Combine(_thuMucTam, tenFile);

        var wb = new HSSFWorkbook();
        var sheet = wb.CreateSheet("Trang 1");
        var dong = 0;
        sheet.CreateRow(dong++).CreateCell(0).SetCellValue(tenTo);
        if (phuDe is not null)
        {
            sheet.CreateRow(dong++).CreateCell(0).SetCellValue(phuDe);
        }

        var tieuDe = sheet.CreateRow(dong++);
        foreach (var (cot, chu) in new[] { (0, "TÊN HÀNG"), (1, "ĐVT"), (2, "SỐ LƯỢNG"), (3, "ĐƠN GIÁ") })
        {
            tieuDe.CreateCell(cot).SetCellValue(chu);
        }

        var hang = sheet.CreateRow(dong);
        hang.CreateCell(0).SetCellValue("Ống 27");
        hang.CreateCell(1).SetCellValue("Cây");
        hang.CreateCell(2).SetCellValue(2d);
        hang.CreateCell(3).SetCellValue(45000d);

        using (var ghi = new FileStream(file, FileMode.Create, FileAccess.Write))
        {
            wb.Write(ghi, leaveOpen: false);
        }

        return DocHoaDon.Doc(file, new DateTime(2026, 6, 2));
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
