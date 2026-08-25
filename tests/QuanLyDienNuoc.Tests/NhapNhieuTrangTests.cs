using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Nhập một tờ hoá đơn nằm ở nhiều file: trang 1 có phần đầu với tên khách, các trang sau chỉ
/// có bảng hàng. Kiểm luôn phần chọn lọc dòng (bỏ dòng tổng, dòng mẫu in sẵn), ô chọn năm và
/// mốc ngày viết ở cột số thứ tự.
/// </summary>
public class NhapNhieuTrangTests : IDisposable
{
    private static readonly string ThuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
    private static readonly string ThuMucHoaDonCu = Path.Combine(AppContext.BaseDirectory, "HoaDonCu");

    private readonly string _thuMucTam = Path.Combine(
        Path.GetTempPath(),
        "qldn-trang-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    // ---------------- Nhận ra trang 1 và trang sau ----------------

    [Fact]
    public void MauTrang1_CoPhanDauNenLaTrang1_MauTrangSauThiKhong()
    {
        // Xuất một hoá đơn dài hai trang ra đúng mẫu giấy của cửa hàng rồi đọc lại: trang đầu
        // phải nhận ra là trang 1 (có tên khách), trang thứ hai là trang nối tiếp.
        var trang = XuatHoaDonDaiHaiTrang()
            .Select(f => DocHoaDon.Doc(f, new DateTime(2026, 6, 2)).Trang.Single())
            .ToList();

        Assert.Equal(2, trang.Count);
        Assert.Equal(LoaiTrangGiay.Trang1, trang[0].Loai);
        Assert.Equal("Ông Mẫu", trang[0].TenKhach);

        Assert.Equal(LoaiTrangGiay.TrangSau, trang[1].Loai);
        Assert.Equal(0, trang[1].DongTieuDe);
        Assert.True(string.IsNullOrEmpty(trang[1].TenKhach));
    }

    [Fact]
    public void HoaDonCuThat_TrangCoPhanDauLaTrang1_TrangChiCoBangLaTrangSau()
    {
        // to1.xls là tờ đã điền của mẫu trang 1: phần đầu có "Tên khách hàng: ...".
        var trang1 = DocHoaDon.Doc(Path.Combine(ThuMucHoaDonCu, "to1.xls"), DateTime.Today);
        Assert.All(trang1.Trang, t => Assert.Equal(LoaiTrangGiay.Trang1, t.Loai));
        Assert.Equal("Ông Mẫu", trang1.Trang[0].TenKhach);

        // to2.xls là tờ của mẫu trang sau: tiêu đề bảng nằm ngay dòng đầu, không có tên khách.
        var trangSau = DocHoaDon.Doc(Path.Combine(ThuMucHoaDonCu, "to2.xls"), DateTime.Today);
        Assert.NotEmpty(trangSau.Trang);
        Assert.All(trangSau.Trang, t => Assert.Equal(LoaiTrangGiay.TrangSau, t.Loai));
        Assert.All(trangSau.Trang, t => Assert.True(string.IsNullOrEmpty(t.TenKhach)));
    }

    [Fact]
    public void MauTrangChuaDienGi_KhongRaDongHangNao()
    {
        // Mẫu trang 1 in sẵn số thứ tự 1..26 và công thức thành tiền ra 0 cho cả trang. Đọc
        // theo "có chữ ở cột TT là có hàng" thì ra 25 mặt hàng rỗng.
        foreach (var ten in new[] { MauHoaDon.TenFileTrang1, MauHoaDon.TenFileTrangSau })
        {
            var doc = DocHoaDon.Doc(Path.Combine(ThuMucMau, ten), DateTime.Today);
            Assert.Empty(doc.Trang);
        }
    }

    // ---------------- Chọn lọc dòng ----------------

    [Fact]
    public void DongTongKhongCoNhan_HetBangTaiDoKhongThanhMatHang()
    {
        // Mẫu cũ gộp ô đầu dòng tổng rồi để trống, chỉ còn số tiền ở cột THÀNH TIỀN. Đây cũng
        // đúng chỗ mẫu mới ghi tiền cộng sang từ tờ trước.
        var doc = DocMotBang(sheet =>
        {
            ThemDongHang(sheet, 1, "1", "Ống 27", soLuong: 2, donGia: 45000);
            ThemDongHang(sheet, 2, "2", "Ống 21", soLuong: 1, donGia: 17000);

            // Dòng tổng không nhãn.
            sheet.CreateRow(3).CreateCell(5).SetCellValue(107000d);

            // Hàng đứng dưới dòng tổng là của tờ khác, không được lấy sang.
            ThemDongHang(sheet, 4, "3", "Không được lấy", soLuong: 9, donGia: 1000);
        });

        Assert.Equal(new[] { "Ống 27", "Ống 21" }, doc.Trang[0].Dong.Select(d => d.TenHang).ToArray());
        Assert.Equal(107_000m, doc.Trang[0].TongTien);
    }

    [Fact]
    public void DongCoSoLuongMaThieuTenHang_VanLayVaBaoDeDienTen()
    {
        // Cửa hàng có lúc viết số lượng mà bỏ trống tên hàng. Bỏ im là mất hàng mà không ai biết.
        var doc = DocMotBang(sheet =>
        {
            ThemDongHang(sheet, 1, "1", "Ống 27", soLuong: 2, donGia: 45000);
            ThemDongHang(sheet, 2, "2", tenHang: null, soLuong: 3, donGia: 0);
        });

        Assert.Equal(2, doc.Trang[0].Dong.Count);
        Assert.Equal(3m, doc.Trang[0].Dong[1].SoLuong);
        Assert.Contains(doc.Trang[0].CanhBao, c => c.Contains("thiếu tên hàng"));
    }

    [Fact]
    public void DongTrongCuaMauInSan_KhongThanhMatHang()
    {
        // Ba dòng chỉ có số thứ tự (mẫu in sẵn) là hết bảng, không đọc lố xuống chân tờ.
        var doc = DocMotBang(sheet =>
        {
            ThemDongHang(sheet, 1, "1", "Ống 27", soLuong: 2, donGia: 45000);
            for (var r = 2; r <= 4; r++)
            {
                sheet.CreateRow(r).CreateCell(0).SetCellValue((r + 1).ToString());
            }

            ThemDongHang(sheet, 5, "5", "Nằm dưới ba dòng trống", soLuong: 1, donGia: 1000);
        });

        Assert.Single(doc.Trang[0].Dong);
        Assert.Equal("Ống 27", doc.Trang[0].Dong[0].TenHang);
    }

    [Fact]
    public void HoaDonCuThat_GiuNguyenSoDongDaDocDuocTruocDay()
    {
        // to1.xls có 32 dòng hàng liền nhau, vượt sức chứa 25 dòng của mẫu — cắt cứng theo mẫu
        // là mất 7 dòng thật.
        var doc = DocHoaDon.Doc(Path.Combine(ThuMucHoaDonCu, "to1.xls"), DateTime.Today);

        Assert.Equal(32, doc.Trang[0].Dong.Count);
        Assert.Equal(2_507_900m, doc.Trang[0].TongTien);
    }

    // ---------------- Ô chọn năm ----------------

    [Fact]
    public void GiayKhongGhiNam_ThiGhepNamDaChon()
    {
        var doc = DocMotBang(
            sheet =>
            {
                ThemDongHang(sheet, 1, "1", "Ống 27", soLuong: 2, donGia: 45000);
                sheet.CreateRow(3).CreateCell(3).SetCellValue("Ngày ....8.... tháng ..3....... năm 20.........");
            },
            namChon: 2026);

        Assert.Null(doc.Trang[0].NamTrenGiay);
        Assert.Equal(8, doc.Trang[0].NgayTrongThang);
        Assert.Equal(3, doc.Trang[0].ThangTrenGiay);
        Assert.Equal(new DateTime(2026, 3, 8), doc.Trang[0].NgayTrenHoaDon);
    }

    [Fact]
    public void GiayGhiRoNam_ThiGiuLaiDeManHinhNoiHaiBenLech()
    {
        var doc = DocMotBang(
            sheet =>
            {
                ThemDongHang(sheet, 1, "1", "Ống 27", soLuong: 2, donGia: 45000);
                sheet.CreateRow(3).CreateCell(3).SetCellValue("Ngày 15 tháng 8 năm 2020");
            },
            namChon: 2026);

        Assert.Equal(2020, doc.Trang[0].NamTrenGiay);
    }

    [Fact]
    public void ChuVietDangToHop_VanDocRaDongNgayThang()
    {
        // Có tờ của cửa hàng lưu "Ngày" thành N, g, a rồi dấu huyền rời ra một ký tự. Trông y
        // hệt chữ thường nên không ai nghĩ là khác.
        var doc = DocMotBang(
            sheet =>
            {
                ThemDongHang(sheet, 1, "1", "Ống 27", soLuong: 2, donGia: 45000);
                // "Ngày", "Tháng", "Năm" viết bằng chữ không dấu cộng ký tự dấu rời: đúng
                // cách mấy tờ hoá đơn thật của cửa hàng lưu chữ trong file.
                const string chuToHop = "Nga\u0300y  15    Tha\u0301ng  8   Na\u0306m 2020";
                sheet.CreateRow(3).CreateCell(3).SetCellValue(chuToHop);
            },
            namChon: 2026);

        Assert.Equal(15, doc.Trang[0].NgayTrongThang);
        Assert.Equal(8, doc.Trang[0].ThangTrenGiay);
        Assert.Equal(2020, doc.Trang[0].NamTrenGiay);
    }

    [Fact]
    public void HoaDonCuThat_DocDuocNgayThangNamOChanTo()
    {
        var doc = DocHoaDon.Doc(Path.Combine(ThuMucHoaDonCu, "to1.xls"), DateTime.Today, namChon: 2026);

        Assert.Equal(15, doc.Trang[0].NgayTrongThang);
        Assert.Equal(8, doc.Trang[0].ThangTrenGiay);
        Assert.Equal(2020, doc.Trang[0].NamTrenGiay);
    }

    // ---------------- Mốc ngày viết ở cột số thứ tự ----------------

    [Fact]
    public void MocNgayOCotSoThuTu_CacDongTuDoXuongMangNgayDo()
    {
        var doc = DocMotBang(
            sheet =>
            {
                ThemDongHang(sheet, 1, "1/12", "Ống 27", soLuong: 2, donGia: 45000);
                ThemDongHang(sheet, 2, "2", "Ống 21", soLuong: 1, donGia: 17000);
                ThemDongHang(sheet, 3, @"12\4", "Dây điện", soLuong: 5, donGia: 8000);
                ThemDongHang(sheet, 4, "4", "Băng tan", soLuong: 3, donGia: 5000);
            },
            namChon: 2026);

        var dong = doc.Trang[0].Dong;
        Assert.Equal(new DateTime(2026, 12, 1), dong[0].Ngay);
        Assert.Equal(new DateTime(2026, 12, 1), dong[1].Ngay);
        Assert.Equal(new DateTime(2026, 4, 12), dong[2].Ngay);
        Assert.Equal(new DateTime(2026, 4, 12), dong[3].Ngay);

        // Ngày/tháng giữ riêng để đổi ô chọn năm là cả lô đổi năm theo.
        Assert.Equal(new NgayThangGiay(1, 12), doc.Trang[0].NgayThangCuaDong[0]);
        Assert.Equal(new NgayThangGiay(12, 4), doc.Trang[0].NgayThangCuaDong[3]);
    }

    [Fact]
    public void MocNgayDungRiengMotDong_VanLayNgayChoCacDongDuoi()
    {
        var doc = DocMotBang(
            sheet =>
            {
                // Mốc ngày đứng riêng, dòng đó không có hàng.
                sheet.CreateRow(1).CreateCell(0).SetCellValue("5/11");
                ThemDongHang(sheet, 2, null, "Ống 27", soLuong: 2, donGia: 45000);
            },
            namChon: 2026);

        Assert.Single(doc.Trang[0].Dong);
        Assert.Equal(new DateTime(2026, 11, 5), doc.Trang[0].Dong[0].Ngay);
    }

    [Theory]
    [InlineData("13 .")]
    [InlineData("3 2")]
    [InlineData("1.5")]
    [InlineData("32/13")]
    public void SoThuTuVietLa_KhongDocThanhMocNgay(string soThuTu)
    {
        var doc = DocMotBang(
            sheet => ThemDongHang(sheet, 1, soThuTu, "Ống 27", soLuong: 2, donGia: 45000),
            namChon: 2026);

        Assert.Empty(doc.Trang[0].NgayThangCuaDong);
        Assert.Equal(new DateTime(2026, 6, 2), doc.Trang[0].Dong[0].Ngay);
    }

    [Fact]
    public void Xuat_RoiDocLai_GiuNguyenNgayCuaTungDong()
    {
        // Trên tờ giấy không có chỗ nào ghi ngày cho từng dòng ngoài cột số thứ tự, nên xuất
        // ra cũng phải ghi mốc ngày ở đó — không thì xuất rồi nhập lại là mất hết ngày.
        var khach = new KhachHang { Ten = "Ông Mẫu" };
        var hoaDon = new HoaDon();
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 1), TenHang = "Ống 27", SoLuong = 2, DonGia = 45000,
        });
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 1), TenHang = "Ống 21", SoLuong = 1, DonGia = 17000,
        });
        hoaDon.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 4, 12), TenHang = "Dây điện", SoLuong = 5, DonGia = 8000,
        });

        Directory.CreateDirectory(_thuMucTam);
        var fileRa = Path.Combine(_thuMucTam, "nhieu-ngay.xls");
        XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));

        var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 1, 1), namChon: 2026);
        var dong = doc.Trang[0].Dong;

        Assert.Equal(3, dong.Count);
        Assert.Equal(new NgayThangGiay(1, 3), doc.Trang[0].NgayThangCuaDong[0]);
        Assert.Equal(new DateTime(2026, 3, 1), dong[0].Ngay);
        Assert.Equal(new DateTime(2026, 3, 1), dong[1].Ngay);
        Assert.Equal(new DateTime(2026, 4, 12), dong[2].Ngay);
    }

    [Fact]
    public void Xuat_NhieuTrang_MoiTrangMotFileVaTuMangNgayCuaNo()
    {
        // Mẫu giấy của cửa hàng vốn là hai file rời (trang đầu, trang sau) và màn nhập cũng gom
        // từng file trang thành một lô, nên xuất ra phải rời từng trang chứ không gộp mấy tab
        // vào một file.
        var khach = new KhachHang { Ten = "Ông Mẫu" };
        var hoaDon = new HoaDon();
        for (var i = 0; i < 30; i++)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = new DateTime(2026, 3, 1), TenHang = $"Hàng {i + 1}", SoLuong = 1, DonGia = 1000,
            });
        }

        Directory.CreateDirectory(_thuMucTam);
        var fileRa = Path.Combine(_thuMucTam, "hai-trang.xls");
        var daGhi = XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));

        Assert.Equal(
            new[] { "hai-trang - trang 1.xls", "hai-trang - trang 2.xls" },
            daGhi.Select(Path.GetFileName).ToArray());
        Assert.All(daGhi, f => Assert.True(File.Exists(f)));

        var trang = daGhi
            .Select(f => DocHoaDon.Doc(f, new DateTime(2026, 1, 1), namChon: 2026).Trang.Single())
            .ToList();

        Assert.Equal(30, trang.Sum(t => t.Dong.Count));
        Assert.All(trang.SelectMany(t => t.Dong), d => Assert.Equal(new DateTime(2026, 3, 1), d.Ngay));

        // Trang nào cũng tự mang ngày của nó ở dòng đầu: nhập riêng một trang vào lô khác thì
        // vẫn ra đúng ngày, không rơi về ngày mặc định.
        Assert.All(trang, t => Assert.Equal(new NgayThangGiay(1, 3), t.NgayThangCuaDong[0]));
    }

    [Fact]
    public void Xuat_ToNhieuNgay_CotSoThuTuVanConDuSo()
    {
        // Chỗ này là lỗi cũ: mốc ngày ghi thẳng vào ô số thứ tự của dòng hàng, nên tờ của khách
        // mối — mỗi ngày lấy một ít — có gần như cả cột TT là ngày, chẳng còn số thứ tự nào.
        // Giờ mốc đứng riêng một dòng: dòng hàng giữ số của nó, mà nhập lại vẫn ra đúng ngày.
        var khach = new KhachHang { Ten = "Ông Mẫu" };
        var hoaDon = new HoaDon();
        DateTime[] ngay =
        {
            new(2026, 3, 1), new(2026, 3, 1), new(2026, 4, 12), new(2026, 4, 12), new(2026, 5, 2),
        };
        for (var i = 0; i < ngay.Length; i++)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = ngay[i], TenHang = $"Hàng {i + 1}", SoLuong = 1, DonGia = 1000,
            });
        }

        Directory.CreateDirectory(_thuMucTam);
        var fileRa = Path.Combine(_thuMucTam, "nhieu-ngay-con-stt.xls");
        var daGhi = XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));

        // Tờ một trang thì giữ đúng tên người dùng đặt, không thêm " - trang 1".
        Assert.Equal(new[] { fileRa }, daGhi.ToArray());

        Assert.Equal(
            new[] { "1/3", "1", "2", "12/4", "3", "4", "2/5", "5" },
            CotSoThuTu(fileRa, MauHoaDon.Trang1));

        var trang = DocHoaDon.Doc(fileRa, new DateTime(2026, 1, 1), namChon: 2026).Trang.Single();

        Assert.Equal(ngay, trang.Dong.Select(d => d.Ngay).ToArray());
        Assert.Equal(hoaDon.TongTien, trang.TongTien);
    }

    // ---------------- Thứ tự trang trong lô ----------------

    [Fact]
    public void LoBatDauBangTrang1_ThiNhapDuocVaLayTenKhachOTrang1()
    {
        var xet = ThuTuTrangGiay.Xet(new[]
        {
            new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ông Long" },
            new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
            new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
        });

        Assert.Null(xet.Chan);
        Assert.Null(xet.Nhac);
        Assert.Equal("Ông Long", xet.TenKhach);
    }

    [Fact]
    public void Trang1KhongDungDauLo_ThiChanLai()
    {
        var xet = ThuTuTrangGiay.Xet(new[]
        {
            new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
            new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ông Long" },
        });

        Assert.True(xet.Trang1KhongDungDau);
        Assert.NotNull(xet.Chan);
        Assert.Contains("thứ 2", xet.Chan);
    }

    [Fact]
    public void LoCoHaiTrang1_ThiChanViDoLaHaiToKhacNhau()
    {
        var xet = ThuTuTrangGiay.Xet(new[]
        {
            new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ông Long" },
            new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Chú Hải" },
        });

        Assert.True(xet.NhieuTrang1);
        Assert.NotNull(xet.Chan);
    }

    [Fact]
    public void LoChiCoTrangSau_ThiVanNhapDuocNhungPhaiNhac()
    {
        var xet = ThuTuTrangGiay.Xet(new[]
        {
            new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
            new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
        });

        Assert.Null(xet.Chan);
        Assert.NotNull(xet.Nhac);
        Assert.Null(xet.TenKhach);
    }

    [Fact]
    public void LoChuaCoTrangNao_ThiKhongChanCungKhongNhac()
    {
        var xet = ThuTuTrangGiay.Xet(Array.Empty<TrangDoc>());

        Assert.Null(xet.Chan);
        Assert.Null(xet.Nhac);
    }

    // ---------------- Dựng file kiểm thử ----------------

    /// <summary>
    /// Đọc cột số thứ tự trong vùng bảng của một file đã xuất, bỏ các dòng còn trống ở cuối.
    /// Dòng mốc ngày cũng nằm ở cột này nên đọc được cả hai loại dòng.
    /// </summary>
    private static string[] CotSoThuTu(string file, ViTriTrang viTri)
    {
        IWorkbook wb;
        using (var doc = File.OpenRead(file))
        {
            wb = WorkbookFactory.Create(doc);
        }

        var sheet = wb.GetSheetAt(0);
        var ra = new List<string>();

        for (var i = 0; i < viTri.SoDongMoiTrang; i++)
        {
            var o = sheet.GetRow(viTri.DongDauDuLieu + i)?.GetCell(MauHoaDon.CotTT);
            var chu = (o?.ToString() ?? string.Empty).Trim();
            if (chu.Length > 0)
            {
                ra.Add(chu);
            }
        }

        return ra.ToArray();
    }

    /// <summary>Xuất một hoá đơn 30 dòng ra mẫu giấy của cửa hàng: đủ dài để có hai trang.</summary>
    private List<string> XuatHoaDonDaiHaiTrang()
    {
        var khach = new KhachHang { Ten = "Ông Mẫu" };
        var hoaDon = new HoaDon();
        for (var i = 0; i < 30; i++)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = new DateTime(2026, 6, 2), TenHang = $"Hàng {i + 1}", SoLuong = 1, DonGia = 1000,
            });
        }

        Directory.CreateDirectory(_thuMucTam);
        var fileRa = Path.Combine(_thuMucTam, "dai-hai-trang.xls");
        return XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));
    }

    /// <summary>
    /// Dựng một file .xls có đúng một bảng hàng: dòng 0 là tiêu đề đủ nhãn, phần còn lại do
    /// <paramref name="dienBang"/> điền. Bảng nằm ngay dòng đầu nên đây là mẫu trang sau.
    /// </summary>
    private KetQuaDocExcel DocMotBang(Action<ISheet> dienBang, int? namChon = null)
    {
        Directory.CreateDirectory(_thuMucTam);
        var file = Path.Combine(_thuMucTam, $"bang-{Guid.NewGuid().ToString("N")[..6]}.xls");

        var wb = new HSSFWorkbook();
        var sheet = wb.CreateSheet("Trang sau");

        var tieuDe = sheet.CreateRow(0);
        var nhan = new[] { "TT", "TÊN HÀNG", "ĐVT", "SỐ LƯỢNG", "ĐƠN GIÁ", "THÀNH TIỀN" };
        for (var c = 0; c < nhan.Length; c++)
        {
            tieuDe.CreateCell(c).SetCellValue(nhan[c]);
        }

        dienBang(sheet);

        using (var ghi = new FileStream(file, FileMode.Create, FileAccess.Write))
        {
            wb.Write(ghi, leaveOpen: false);
        }

        return DocHoaDon.Doc(file, new DateTime(2026, 6, 2), namChon);
    }

    private static void ThemDongHang(
        ISheet sheet,
        int dong,
        string? soThuTu,
        string? tenHang,
        double soLuong,
        double donGia)
    {
        var hang = sheet.CreateRow(dong);

        if (soThuTu is not null)
        {
            hang.CreateCell(0).SetCellValue(soThuTu);
        }

        if (tenHang is not null)
        {
            hang.CreateCell(1).SetCellValue(tenHang);
        }

        hang.CreateCell(3).SetCellValue(soLuong);

        if (donGia != 0)
        {
            hang.CreateCell(4).SetCellValue(donGia);
            hang.CreateCell(5).SetCellValue(soLuong * donGia);
        }
    }
}
