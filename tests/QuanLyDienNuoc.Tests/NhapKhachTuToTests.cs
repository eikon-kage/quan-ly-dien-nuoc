using NPOI.SS.UserModel;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Nhập <b>một</b> khách hàng từ file: một tờ hoá đơn của cửa hàng là của đúng một khách, tên
/// khách ghi ở đầu trang 1. Kiểm phần chấm tên khách đọc trên giấy, những chỗ chặn không cho
/// ghi vào sổ, và hai file mẫu người dùng tải về điền.
/// </summary>
public class NhapKhachTuToTests : IDisposable
{
    private static readonly string ThuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
    private static readonly string ThuMucHoaDonCu = Path.Combine(AppContext.BaseDirectory, "HoaDonCu");

    private readonly string _thuMucTam = Path.Combine(
        Path.GetTempPath(),
        "qldn-khach-to-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_thuMucTam))
        {
            Directory.Delete(_thuMucTam, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    // ---------------- Đọc tờ giấy thật ----------------

    [Fact]
    public void ToDaDien_LayTenVaDiaChiKhachOTrang1()
    {
        // Xuất một tờ hoá đơn ra đúng mẫu giấy của cửa hàng rồi nhập lại: phải ra đúng một
        // khách, tên và địa chỉ lấy ở phần đầu trang 1.
        var doc = DocHoaDon.Doc(XuatToCuaKhach("Anh Dũng sắt Hà Đông", "12 Nguyễn Trãi, Hà Đông")[0], DateTime.Today);

        var xet = NhapKhachTuTo.Xet(doc.Trang, Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Null(xet.Nhac);
        Assert.Equal("Anh Dũng sắt Hà Đông", xet.TenTrenGiay);
        Assert.Equal("12 Nguyễn Trãi, Hà Đông", xet.DiaChiTrenGiay);
        Assert.Null(xet.KhachTrung);
    }

    [Fact]
    public void ToDaiHaiTrang_VanChiRaMotKhach_LayOTrangDau()
    {
        // Tờ dài nằm ở hai trang — hai file, mỗi trang một file. Trang sau không có phần đầu
        // nên chẳng có tên nào để lấy: tên của cả tờ vẫn là tên ghi ở trang 1.
        var trang = XuatToCuaKhach("Chị Hoa nước Cầu Giấy", "88 Xuân Thuỷ", soDong: 30)
            .Select(f => DocHoaDon.Doc(f, DateTime.Today).Trang.Single())
            .ToList();

        Assert.Equal(2, trang.Count);
        Assert.Equal(LoaiTrangGiay.TrangSau, trang[1].Loai);

        var xet = NhapKhachTuTo.Xet(trang, Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Equal("Chị Hoa nước Cầu Giấy", xet.TenTrenGiay);
        Assert.Equal(2, xet.ThuTu.SoTrang);
    }

    [Fact]
    public void HoaDonThatCoHaiToTrongMotFile_ThiChan()
    {
        // to1.xls của cửa hàng có hai sheet đều là trang 1, tức hai tờ của hai lượt mua khác
        // nhau. Mỗi lượt nhập chỉ ra một khách nên phải chặn, không dồn cả hai vào một hoá đơn.
        var doc = DocHoaDon.Doc(Path.Combine(ThuMucHoaDonCu, "to1.xls"), DateTime.Today);

        var xet = NhapKhachTuTo.Xet(doc.Trang, Array.Empty<KhachHang>());

        Assert.True(doc.Trang.Count > 1);
        Assert.NotNull(xet.Chan);
        Assert.Contains("trang 1", xet.Chan);
    }

    // ---------------- Thứ tự trang trong lô ----------------

    [Fact]
    public void LoBatDauBangTrang1_ThiNhapDuoc()
    {
        var xet = NhapKhachTuTo.Xet(
            new[]
            {
                new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ông Long" },
                new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
            },
            Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Null(xet.Nhac);
        Assert.Equal("Ông Long", xet.TenTrenGiay);
    }

    [Fact]
    public void Trang1NamSauTrangNoiTiep_ThiChan()
    {
        // Thêm file trang sau trước rồi mới thêm trang 1: hàng vào sổ lệch trang mà trên sổ
        // không còn dấu vết trang nào để dò lại.
        var xet = NhapKhachTuTo.Xet(
            new[]
            {
                new TrangDoc { Loai = LoaiTrangGiay.TrangSau },
                new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ông Long" },
            },
            Array.Empty<KhachHang>());

        Assert.NotNull(xet.Chan);
    }

    [Fact]
    public void LoChiCoTrangSau_ThiNhacGoTenTay()
    {
        // Không có trang 1 thì không đọc được tên khách, nhưng vẫn nhập được: gõ tên vào ô
        // trên màn hình là đủ.
        var xet = NhapKhachTuTo.Xet(
            new[] { new TrangDoc { Loai = LoaiTrangGiay.TrangSau } },
            Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Null(xet.TenTrenGiay);
        Assert.NotNull(xet.Nhac);
        Assert.Contains("chưa có trang 1", xet.Nhac);
    }

    [Fact]
    public void Trang1DeTrongChoTenKhach_ThiNhacGoTenTay()
    {
        var xet = NhapKhachTuTo.Xet(
            new[] { new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "  " } },
            Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Null(xet.TenTrenGiay);
        Assert.NotNull(xet.Nhac);
        Assert.Contains("để trống", xet.Nhac);
    }

    [Fact]
    public void LoChuaCoTrangNao_ThiKhongChanCungKhongNhac()
    {
        var xet = NhapKhachTuTo.Xet(Array.Empty<TrangDoc>(), Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Null(xet.Nhac);
        Assert.Null(xet.TenTrenGiay);
    }

    [Fact]
    public void LoCoToHoanHang_ThiChan()
    {
        // Tờ hoàn là tờ hoàn cho một hoá đơn đã có nên không mở đầu sổ của một khách mới.
        var xet = NhapKhachTuTo.Xet(
            new[] { new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ông Long", LaHoanHang = true } },
            Array.Empty<KhachHang>());

        Assert.Equal(NhapKhachTuTo.ChanToHoan, xet.Chan);
    }

    // ---------------- Chấm tên khách ----------------

    [Fact]
    public void TenTrenGiayTrungKhachDaCo_ThiChiRaKhachCu()
    {
        // Giấy viết không dấu mà trong sổ có dấu vẫn phải nhận ra là một người: thêm nữa là
        // công nợ của một khách bị chia đôi.
        var daCo = new[] { new KhachHang { Ten = "Ông Mẫu" } };

        var xet = NhapKhachTuTo.Xet(
            new[] { new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "Ong Mau" } },
            daCo);

        Assert.NotNull(xet.KhachTrung);
        Assert.Equal("Ông Mẫu", xet.KhachTrung!.Ten);
    }

    [Theory]
    [InlineData("...............")]
    [InlineData("TT")]
    [InlineData("Tổng cộng")]
    [InlineData("TÊN HÀNG")]
    [InlineData("Người mua hàng")]
    [InlineData("ĐC: 12 Nguyễn Trãi")]
    [InlineData("A")]
    public void NhanCuaToGiay_KhongPhaiTenKhach(string o)
    {
        Assert.False(NhapKhachTuTo.GiongTenKhach(o));
    }

    [Theory]
    [InlineData("Anh Dũng sắt Hà Đông")]
    [InlineData("Ông Mẫu")]
    [InlineData("Chi Hoa nuoc Cau Giay")]
    public void TenNguoiThat_ThiNhanLaTenKhach(string o)
    {
        Assert.True(NhapKhachTuTo.GiongTenKhach(o));
    }

    [Fact]
    public void ChoDeTrongInSanCuaToGiay_ThiKhongLayLamTenKhach()
    {
        // Tờ chưa điền in sẵn "Tên khách hàng: ............." — lấy chỗ đó làm tên là sổ có
        // thêm một khách rác không ai nhận ra.
        var xet = NhapKhachTuTo.Xet(
            new[] { new TrangDoc { Loai = LoaiTrangGiay.Trang1, TenKhach = "............." } },
            Array.Empty<KhachHang>());

        Assert.Null(xet.TenTrenGiay);
        Assert.NotNull(xet.Nhac);
    }

    // ---------------- File mẫu tải về điền ----------------

    [Fact]
    public void XuatFileMau_RaHaiMauGiayCuaCuaHang_BangHangCoCotSoThuTu()
    {
        var (trang1, trangSau) = NhapKhachTuTo.XuatFileMau(_thuMucTam, ThuMucMau);

        Assert.True(File.Exists(trang1));
        Assert.True(File.Exists(trangSau));

        // Mẫu trang 1: có chỗ điền tên khách ở phần đầu, và bảng hàng mở đầu bằng cột số thứ tự.
        var nhanTrang1 = DongTieuDeBang(trang1);
        Assert.Contains("Tên khách hàng", ONhanO(trang1, MauHoaDon.Trang1.DongTenKhach, 0));
        Assert.Equal("TT", nhanTrang1[MauHoaDon.CotTT]);
        Assert.Contains("TÊN HÀNG", nhanTrang1[MauHoaDon.CotTenHang]);

        // Mẫu trang sau: bảng nằm ngay đầu tờ, không có phần đầu — đó là chỗ phần mềm nhận ra
        // đây là trang nối tiếp chứ không phải một tờ mới.
        Assert.Equal("TT", DongTieuDeBang(trangSau)[MauHoaDon.CotTT]);
    }

    [Fact]
    public void DienVaoFileMauTrang1_RoiNhapLai_RaMotKhachKemDongHang()
    {
        var (trang1, _) = NhapKhachTuTo.XuatFileMau(_thuMucTam, ThuMucMau);
        DienVaoMauTrang1(trang1, "Anh Bình điện nước Thanh Xuân", "45 Khương Trung");

        var doc = DocHoaDon.Doc(trang1, new DateTime(2026, 5, 4), namChon: 2026);
        var xet = NhapKhachTuTo.Xet(doc.Trang, Array.Empty<KhachHang>());

        Assert.Null(xet.Chan);
        Assert.Equal("Anh Bình điện nước Thanh Xuân", xet.TenTrenGiay);
        Assert.Equal("45 Khương Trung", xet.DiaChiTrenGiay);

        var dong = Assert.Single(doc.Trang).Dong;
        Assert.Equal(2, dong.Count);
        Assert.Equal("Ống nhựa PVC D21", dong[0].TenHang);
        Assert.Equal(2 * 32000m, dong[0].ThanhTien);
    }

    [Fact]
    public void FileMauChuaDienGiThi_KhongRaKhachNao()
    {
        // Mẫu trang 1 in sẵn số thứ tự 1..26 và công thức thành tiền ra 0: tải mẫu về rồi trỏ
        // thẳng vào phần mềm mà chưa điền gì thì không được ra 25 mặt hàng rỗng.
        var (trang1, trangSau) = NhapKhachTuTo.XuatFileMau(_thuMucTam, ThuMucMau);

        Assert.Empty(DocHoaDon.Doc(trang1, DateTime.Today).Trang);
        Assert.Empty(DocHoaDon.Doc(trangSau, DateTime.Today).Trang);
    }

    // ---------------- Dựng file kiểm thử ----------------

    /// <summary>Xuất một tờ hoá đơn của một khách ra đúng mẫu giấy của cửa hàng.</summary>
    private List<string> XuatToCuaKhach(string ten, string diaChi, int soDong = 3)
    {
        var khach = new KhachHang { Ten = ten, DiaChi = diaChi };
        var hoaDon = new HoaDon();
        for (var i = 0; i < soDong; i++)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = new DateTime(2026, 6, 2), TenHang = $"Hàng {i + 1}", SoLuong = 1, DonGia = 1000,
            });
        }

        Directory.CreateDirectory(_thuMucTam);
        var fileRa = Path.Combine(_thuMucTam, $"to-{Guid.NewGuid().ToString("N")[..6]}.xls");
        return XuatHoaDon.Xuat(hoaDon, khach, fileRa, ThuMucMau, new DateTime(2026, 8, 3));
    }

    /// <summary>Điền tên khách, địa chỉ và hai dòng hàng vào file mẫu trang 1 như người dùng.</summary>
    private static void DienVaoMauTrang1(string file, string ten, string diaChi)
    {
        IWorkbook wb;
        using (var doc = File.OpenRead(file))
        {
            wb = WorkbookFactory.Create(doc);
        }

        var sheet = wb.GetSheetAt(MauHoaDon.TimTab(wb, MauHoaDon.TenTabTrang1));
        var viTri = MauHoaDon.Trang1;

        LayO(sheet, viTri.DongTenKhach, 0).SetCellValue($"Tên khách hàng: {ten}");
        LayO(sheet, viTri.DongDiaChi, 0).SetCellValue($"Địa chỉ: {diaChi}");

        DienDongHang(sheet, viTri.DongDauDuLieu, "Ống nhựa PVC D21", "Cây", 2, 32000);
        DienDongHang(sheet, viTri.DongDauDuLieu + 1, "Van khoá nước D21", "Cái", 1, 55000);

        using var ghi = new FileStream(file, FileMode.Create, FileAccess.Write);
        wb.Write(ghi, leaveOpen: false);
    }

    private static void DienDongHang(ISheet sheet, int dong, string tenHang, string donVi, double soLuong, double donGia)
    {
        LayO(sheet, dong, MauHoaDon.CotTenHang).SetCellValue(tenHang);
        LayO(sheet, dong, MauHoaDon.CotDonVi).SetCellValue(donVi);
        LayO(sheet, dong, MauHoaDon.CotSoLuong).SetCellValue(soLuong);
        LayO(sheet, dong, MauHoaDon.CotDonGia).SetCellValue(donGia);
        LayO(sheet, dong, MauHoaDon.CotThanhTien).SetCellValue(soLuong * donGia);
    }

    private static ICell LayO(ISheet sheet, int dong, int cot)
    {
        var hang = sheet.GetRow(dong) ?? sheet.CreateRow(dong);
        return hang.GetCell(cot) ?? hang.CreateCell(cot);
    }

    /// <summary>Chữ trong một ô của file, bỏ dấu xuống dòng để so cho gọn.</summary>
    private static string ONhanO(string file, int dong, int cot)
    {
        using var doc = File.OpenRead(file);
        using var wb = WorkbookFactory.Create(doc);
        var sheet = wb.GetSheetAt(MauHoaDon.TimTab(wb, MauHoaDon.TenTabTrang1));
        return sheet.GetRow(dong)?.GetCell(cot)?.ToString()?.Replace('\n', ' ') ?? string.Empty;
    }

    /// <summary>Nhãn của dòng tiêu đề bảng hàng trong file mẫu, theo số cột.</summary>
    private static string[] DongTieuDeBang(string file)
    {
        using var doc = File.OpenRead(file);
        using var wb = WorkbookFactory.Create(doc);
        var sheet = wb.GetSheetAt(0);

        for (var r = 0; r <= Math.Min(sheet.LastRowNum, 40); r++)
        {
            var hang = sheet.GetRow(r);
            if (hang is null)
            {
                continue;
            }

            var o = new string[Math.Max((int)hang.LastCellNum, MauHoaDon.CotThanhTien + 1)];
            for (var c = 0; c < o.Length; c++)
            {
                o[c] = (hang.GetCell(c)?.ToString() ?? string.Empty).Replace('\n', ' ').Trim();
            }

            if (o[MauHoaDon.CotTT] == "TT" && o[MauHoaDon.CotTenHang].Contains("TÊN HÀNG"))
            {
                return o;
            }
        }

        throw new InvalidOperationException($"Không tìm thấy dòng tiêu đề bảng hàng trong {file}");
    }
}
