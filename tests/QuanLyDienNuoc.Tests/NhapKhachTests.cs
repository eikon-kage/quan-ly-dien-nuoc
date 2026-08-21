using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Nhập danh sách khách hàng từ file: xuất file mẫu, đọc lại file người dùng điền, và chấm
/// dòng nào nhập được. Cột phải nhận ra theo chữ ở dòng tiêu đề, file không có tiêu đề mới
/// đọc theo thứ tự 1-2-3-4.
/// </summary>
public class NhapKhachTests : IDisposable
{
    private static readonly string ThuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
    private static readonly string ThuMucHoaDonCu = Path.Combine(AppContext.BaseDirectory, "HoaDonCu");

    private readonly string _thuMucTam = Path.Combine(
        Path.GetTempPath(),
        "qldn-khach-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void FileMau_DocLaiKhongCoDongNao_VaNhanRaTieuDe()
    {
        var file = Path.Combine(_thuMucTam, "mau-khach-hang.xlsx");
        NhapKhachHang.XuatFileMau(file);

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        // Ví dụ nằm ở sheet Hướng dẫn nên điền xong nhập luôn cũng không kéo theo khách ảo.
        Assert.Empty(ketQua.Dong);
        Assert.True(ketQua.TheoTieuDe);
        Assert.Equal(NhapKhachHang.TenSheetMau, ketQua.TenBang);
        Assert.Empty(ketQua.CanhBao);
    }

    [Fact]
    public void FileMau_NguoiDungDienVao_DocDuDonCot()
    {
        var file = Path.Combine(_thuMucTam, "mau-da-dien.xlsx");
        NhapKhachHang.XuatFileMau(file);
        DienThemDong(
            file,
            new[] { "Anh Tuấn sắt", "0912345678", "12 Nguyễn Trãi", "Trả cuối tháng" },
            new[] { "Chị Hoa nước", "0987654321", "Số 5 Trần Duy Hưng", string.Empty });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.Equal(2, ketQua.Dong.Count);
        Assert.Equal(2, ketQua.SoSeNhap);

        var dau = ketQua.Dong[0];
        Assert.Equal("Anh Tuấn sắt", dau.Ten);
        Assert.Equal("0912345678", dau.DienThoai);
        Assert.Equal("12 Nguyễn Trãi", dau.DiaChi);
        Assert.Equal("Trả cuối tháng", dau.GhiChu);
        Assert.Equal(TinhTrangDongKhach.ThemMoi, dau.TinhTrang);
        Assert.Equal(2, dau.SoDong);
    }

    [Fact]
    public void DoiChoCot_VanDocDungTheoChuOTieuDe()
    {
        var file = TaoFile(
            new[] { "Ghi chú", "SĐT", "Tên khách hàng", "Địa chỉ" },
            new[] { "Khách quen", "0912345678", "Anh Tuấn sắt", "12 Nguyễn Trãi" });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        var dong = Assert.Single(ketQua.Dong);
        Assert.True(ketQua.TheoTieuDe);
        Assert.Equal("Anh Tuấn sắt", dong.Ten);
        Assert.Equal("0912345678", dong.DienThoai);
        Assert.Equal("12 Nguyễn Trãi", dong.DiaChi);
        Assert.Equal("Khách quen", dong.GhiChu);
    }

    [Fact]
    public void ThemCotLa_VanDocDungCotCanLay()
    {
        var file = TaoFile(
            new[] { "STT", "Tên khách hàng", "Nợ cũ", "Điện thoại", "Địa chỉ" },
            new[] { "1", "Anh Tuấn sắt", "1.500.000", "0912345678", "12 Nguyễn Trãi" });

        var dong = Assert.Single(NhapKhachHang.Doc(file, Array.Empty<KhachHang>()).Dong);

        Assert.Equal("Anh Tuấn sắt", dong.Ten);
        Assert.Equal("0912345678", dong.DienThoai);
        Assert.Equal("12 Nguyễn Trãi", dong.DiaChi);
    }

    [Fact]
    public void KhongCoTieuDe_DocTheoThuTuCotCuaFileMau_VaCanhBao()
    {
        var file = TaoFile(
            new[] { "Anh Tuấn sắt", "0912345678", "12 Nguyễn Trãi", "Trả cuối tháng" },
            new[] { "Chị Hoa nước", "0987654321", "Số 5 Trần Duy Hưng", string.Empty });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.False(ketQua.TheoTieuDe);
        Assert.Single(ketQua.CanhBao);
        Assert.Equal(2, ketQua.Dong.Count);
        Assert.Equal("Anh Tuấn sắt", ketQua.Dong[0].Ten);
        Assert.Equal("0987654321", ketQua.Dong[1].DienThoai);
    }

    [Fact]
    public void TrungTenKhachDaCo_TuBoTichVaNoiTrungVoiAi()
    {
        var file = TaoFile(
            new[] { "Tên khách hàng", "Điện thoại" },
            new[] { "anh tuan sat", "0912345678" },
            new[] { "Chị Hoa nước", "0987654321" });

        var daCo = new[] { new KhachHang { Ten = "Anh Tuấn sắt" } };
        var ketQua = NhapKhachHang.Doc(file, daCo);

        Assert.Equal(TinhTrangDongKhach.TrungKhachCu, ketQua.Dong[0].TinhTrang);
        Assert.False(ketQua.Dong[0].Chon);
        Assert.Contains("Anh Tuấn sắt", ketQua.Dong[0].TinhTrangChu);
        Assert.True(ketQua.Dong[1].Chon);
        Assert.Equal(1, ketQua.SoSeNhap);
    }

    [Fact]
    public void TrungTenTrongCungFile_ChiNhapDongDauTien()
    {
        var file = TaoFile(
            new[] { "Tên khách hàng", "Điện thoại" },
            new[] { "Anh Tuấn sắt", "0912345678" },
            new[] { "Anh Tuân Sat", "0912345679" });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.Equal(TinhTrangDongKhach.ThemMoi, ketQua.Dong[0].TinhTrang);
        Assert.Equal(TinhTrangDongKhach.TrungTrongFile, ketQua.Dong[1].TinhTrang);
        Assert.Equal(1, ketQua.SoSeNhap);
    }

    [Fact]
    public void ThieuTen_KhongNhapDuocNhungVanHienDeSuaTay()
    {
        var file = TaoFile(
            new[] { "Tên khách hàng", "Điện thoại" },
            new[] { string.Empty, "0912345678" });

        var dong = Assert.Single(NhapKhachHang.Doc(file, Array.Empty<KhachHang>()).Dong);

        Assert.Equal(TinhTrangDongKhach.ThieuTen, dong.TinhTrang);
        Assert.False(dong.Chon);
        Assert.Equal("0912345678", dong.DienThoai);
    }

    [Fact]
    public void SuaTenTayRoiChamLai_ThanhNhapDuoc()
    {
        var dong = new List<DongKhachNhap>
        {
            new() { SoDong = 2, DienThoai = "0912345678", TinhTrang = TinhTrangDongKhach.ThieuTen },
        };

        dong[0].Ten = "  Anh Tuấn sắt  ";
        NhapKhachHang.ChamLaiTinhTrang(dong, Array.Empty<KhachHang>());

        Assert.Equal(TinhTrangDongKhach.ThemMoi, dong[0].TinhTrang);
        Assert.True(dong[0].Chon);
        Assert.Equal("Anh Tuấn sắt", dong[0].Ten);
    }

    [Fact]
    public void TuTayTichDongTrung_ChamLaiKhongDeLenYNguoiDung()
    {
        var dong = new List<DongKhachNhap>
        {
            new() { SoDong = 2, Ten = "Anh Tuấn sắt", Chon = true, TuTayChon = true },
        };
        var daCo = new[] { new KhachHang { Ten = "Anh Tuấn sắt" } };

        NhapKhachHang.ChamLaiTinhTrang(dong, daCo);

        // Vẫn báo là trùng để người dùng biết, nhưng tích họ tự đặt thì giữ nguyên.
        Assert.Equal(TinhTrangDongKhach.TrungKhachCu, dong[0].TinhTrang);
        Assert.True(dong[0].Chon);
    }

    [Fact]
    public void DongTrongOGiuaVaCuoiFile_BoQuaHan()
    {
        var file = TaoFile(
            new[] { "Tên khách hàng", "Điện thoại" },
            new[] { "Anh Tuấn sắt", "0912345678" },
            new[] { string.Empty, string.Empty },
            new[] { "Chị Hoa nước", "0987654321" },
            new[] { string.Empty, string.Empty });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.Equal(2, ketQua.Dong.Count);
        Assert.Equal(new[] { 2, 4 }, ketQua.Dong.Select(d => d.SoDong).ToArray());
    }

    [Fact]
    public void SoDienThoaiONhapKieuSo_TraLaiSo0DauDong()
    {
        var file = Path.Combine(_thuMucTam, "so-kieu-so.xlsx");
        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Sheet1");

        var tieuDe = sheet.CreateRow(0);
        tieuDe.CreateCell(0).SetCellValue("Tên khách hàng");
        tieuDe.CreateCell(1).SetCellValue("Điện thoại");

        var hang = sheet.CreateRow(1);
        hang.CreateCell(0).SetCellValue("Anh Tuấn sắt");
        hang.CreateCell(1).SetCellValue(912345678d);

        Ghi(wb, file);

        var dong = Assert.Single(NhapKhachHang.Doc(file, Array.Empty<KhachHang>()).Dong);
        Assert.Equal("0912345678", dong.DienThoai);
    }

    [Fact]
    public void FileCsvXuatTuExcelVietNam_DocDuocCaDauChamPhay()
    {
        var file = Path.Combine(_thuMucTam, "khach.csv");
        Directory.CreateDirectory(_thuMucTam);
        File.WriteAllLines(file, new[]
        {
            "Tên khách hàng;Điện thoại;Địa chỉ;Ghi chú",
            "Anh Tuấn sắt;0912345678;\"12 Nguyễn Trãi, Hà Đông\";Trả cuối tháng",
        });

        var dong = Assert.Single(NhapKhachHang.Doc(file, Array.Empty<KhachHang>()).Dong);

        Assert.Equal("Anh Tuấn sắt", dong.Ten);
        Assert.Equal("0912345678", dong.DienThoai);
        Assert.Equal("12 Nguyễn Trãi, Hà Đông", dong.DiaChi);
    }

    [Fact]
    public void ThanhKhachHang_LayDungThongTinDaSoat()
    {
        var dong = new DongKhachNhap
        {
            Ten = " Anh Tuấn sắt ",
            DienThoai = " 0912345678 ",
            DiaChi = " 12 Nguyễn Trãi ",
            GhiChu = " Trả cuối tháng ",
        };

        var khach = dong.ThanhKhachHang(new DateTime(2026, 8, 21));

        Assert.Equal("Anh Tuấn sắt", khach.Ten);
        Assert.Equal("0912345678", khach.DienThoai);
        Assert.Equal("12 Nguyễn Trãi", khach.DiaChi);
        Assert.Equal("Trả cuối tháng", khach.GhiChu);
        Assert.Equal(new DateTime(2026, 8, 21), khach.NgayTao);
    }

    [Theory]
    [InlineData("to1.xls")]
    [InlineData("to2.xls")]
    public void ChonNhamToHoaDon_NhanRaLaHoaDonChuKhongDocBuaThanhKhach(string tenFile)
    {
        // Đúng file chủ cửa hàng đã chọn nhầm (bản ẩn danh): trước đây đọc theo thứ tự cột ra
        // 32 "khách" gồm tên cửa hàng, "ĐC:", "ĐT:", và từng dòng hàng của tờ hoá đơn.
        var file = Path.Combine(ThuMucHoaDonCu, tenFile);
        Assert.True(File.Exists(file), $"Thiếu file kiểm thử {file}");

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.True(ketQua.LaHoaDon);
        Assert.Empty(ketQua.Dong);
        Assert.Single(ketQua.CanhBao);
    }

    [Fact]
    public void MauHoaDonGiayCuaCuaHang_CungNhanRaLaHoaDon()
    {
        var file = Path.Combine(ThuMucMau, "trang-1.xls");
        Assert.True(File.Exists(file), $"Thiếu file kiểm thử {file}");

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.True(ketQua.LaHoaDon);
        Assert.Empty(ketQua.Dong);
    }

    [Fact]
    public void BangHangCoThemCotGhiChu_VanKhongBiCoiLaDanhSachKhach()
    {
        // "TÊN HÀNG" + "GHI CHÚ" từng đủ hai nhãn để bị nhận là dòng tiêu đề danh sách khách.
        var file = TaoFile(
            new[] { "TT", "TÊN HÀNG", "ĐVT", "SỐ LƯỢNG", "GHI CHÚ" },
            new[] { "1", "Dây 2x1", "m", "291", string.Empty });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.True(ketQua.LaHoaDon);
        Assert.Empty(ketQua.Dong);
    }

    [Theory]
    [InlineData("ĐC: Xóm 9 Liên Minh - Hải Hậu")]
    [InlineData("ĐT: 0347.458.570- 0816503678")]
    [InlineData("Tên khách hàng: ...................")]
    [InlineData("Địa chỉ: ......................")]
    [InlineData("TT")]
    [InlineData("1")]
    [InlineData("Tổng cộng")]
    public void DongKhongGiongTenKhach_VanHienNhungBoTich(string oTen)
    {
        var file = TaoFile(
            new[] { "Tên khách hàng", "Điện thoại" },
            new[] { oTen, string.Empty },
            new[] { "Anh Tuấn sắt", "0912345678" });

        var ketQua = NhapKhachHang.Doc(file, Array.Empty<KhachHang>());

        Assert.Equal(TinhTrangDongKhach.KhongGiongTen, ketQua.Dong[0].TinhTrang);
        Assert.False(ketQua.Dong[0].Chon);
        Assert.True(ketQua.Dong[1].Chon);
        Assert.Equal(1, ketQua.SoSeNhap);
    }

    [Theory]
    [InlineData("Anh Tuấn sắt Bình Minh")]
    [InlineData("Chị Hoa nước Cầu Giấy")]
    [InlineData("Cửa hàng Điện nước Hùng Vương")]
    [InlineData("Cô Ba")]
    [InlineData("Nguyễn Văn Hiền")]
    public void TenKhachThatKhongBiTuongLaDongRac(string ten)
    {
        var file = TaoFile(
            new[] { "Tên khách hàng", "Điện thoại" },
            new[] { ten, "0912345678" });

        var dong = Assert.Single(NhapKhachHang.Doc(file, Array.Empty<KhachHang>()).Dong);

        Assert.Equal(TinhTrangDongKhach.ThemMoi, dong.TinhTrang);
        Assert.True(dong.Chon);
    }

    // ---------- Tiện ích dựng file kiểm thử ----------

    private string TaoFile(params string[][] dong)
    {
        var file = Path.Combine(_thuMucTam, "khach-" + Guid.NewGuid().ToString("N")[..6] + ".xlsx");
        var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Sheet1");

        for (var i = 0; i < dong.Length; i++)
        {
            var hang = sheet.CreateRow(i);
            for (var c = 0; c < dong[i].Length; c++)
            {
                hang.CreateCell(c).SetCellValue(dong[i][c]);
            }
        }

        Ghi(wb, file);
        return file;
    }

    private static void DienThemDong(string file, params string[][] dong)
    {
        IWorkbook wb;
        using (var doc = File.OpenRead(file))
        {
            wb = WorkbookFactory.Create(doc);
        }

        var sheet = wb.GetSheet(NhapKhachHang.TenSheetMau);
        for (var i = 0; i < dong.Length; i++)
        {
            var hang = sheet.CreateRow(sheet.LastRowNum + 1);
            for (var c = 0; c < dong[i].Length; c++)
            {
                hang.CreateCell(c).SetCellValue(dong[i][c]);
            }
        }

        using var ghi = new FileStream(file, FileMode.Create, FileAccess.Write);
        wb.Write(ghi, leaveOpen: false);
    }

    private static void Ghi(IWorkbook wb, string file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using var ra = new FileStream(file, FileMode.Create, FileAccess.Write);
        wb.Write(ra, leaveOpen: false);
    }
}
