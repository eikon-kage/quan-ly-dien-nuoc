using NPOI.SS.UserModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Hoá đơn hoàn hàng: khách mang hàng trả về sau khi hoá đơn bán đã in hoặc đã chốt, nên lập
/// một tờ riêng hoàn cho nó. Tờ hoàn ghi số lượng âm nên tự trừ vào nợ của khách, còn hoá đơn
/// gốc không bị sửa một chữ.
/// </summary>
public class HoanHangTests
{
    private static readonly DateTime HomNay = new(2026, 8, 3);

    private static (DuLieuApp DuLieu, KhachHang Khach, HoaDon Goc) TaoSo()
    {
        var khach = new KhachHang { Ten = "Ông Long" };
        var duLieu = new DuLieuApp();
        duLieu.KhachHangs.Add(khach);

        var goc = new HoaDon
        {
            KhachHangId = khach.Id,
            MaHoaDon = "HD2026-01",
            Nam = 2026,
            NgayMo = new DateTime(2026, 3, 5),
        };
        goc.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 5),
            TenHang = "Ống 27",
            DonVi = "Cây",
            DonGia = 45_000,
            SoLuong = 10,
        });
        goc.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 6),
            TenHang = "Băng tan",
            DonVi = "Cuộn",
            DonGia = 5_000,
            SoLuong = 4,
        });

        duLieu.HoaDons.Add(goc);
        return (duLieu, khach, goc);
    }

    private static HoaDon Hoan(DuLieuApp duLieu, HoaDon goc, decimal soLuong, string ma = "HH2026-01", string lyDo = "")
    {
        var toHoan = HoanHang.Tao(
            goc,
            new[] { new MucHoan(goc.ChiTiet[0], soLuong) },
            ma,
            new DateTime(2026, 4, 2),
            lyDo);

        duLieu.HoaDons.Add(toHoan);
        return toHoan;
    }

    // ---------- Lập tờ hoàn ----------

    [Fact]
    public void Tao_GhiSoLuongAmNenTruVaoNoCuaKhach()
    {
        var (duLieu, _, goc) = TaoSo();

        var toHoan = Hoan(duLieu, goc, 2m);

        Assert.Equal(LoaiHoaDon.HoanHang, toHoan.Loai);
        Assert.True(toHoan.LaHoanHang);
        Assert.Equal(-2m, Assert.Single(toHoan.ChiTiet).SoLuong);
        Assert.Equal(-90_000m, toHoan.TongTien);

        // Bày ra cho người đọc thì là số dương: hoàn lại 90.000.
        Assert.Equal(90_000m, toHoan.TienHoan);

        // Hoá đơn gốc không bị sửa; nợ của khách là tổng hai tờ.
        Assert.Equal(470_000m, goc.TongTien);
        Assert.Equal(380_000m, duLieu.HoaDons.Sum(h => h.ConLai));
    }

    [Fact]
    public void Tao_GiuNguyenGiaDaBanVaGanVaoDongGoc()
    {
        var (duLieu, _, goc) = TaoSo();
        goc.ChiTiet[0].VatTuId = Guid.NewGuid();

        var dongHoan = Assert.Single(Hoan(duLieu, goc, 3m).ChiTiet);

        Assert.Equal(goc.ChiTiet[0].Id, dongHoan.DongGocId);
        Assert.Equal(goc.ChiTiet[0].VatTuId, dongHoan.VatTuId);
        Assert.Equal("Ống 27", dongHoan.TenHang);
        Assert.Equal("Cây", dongHoan.DonVi);
        Assert.Equal(45_000m, dongHoan.DonGia);
    }

    [Fact]
    public void Tao_NamTheoHoaDonGocDuChoKhachTraHangSangNamSau()
    {
        var (_, _, goc) = TaoSo();

        var toHoan = HoanHang.Tao(
            goc,
            new[] { new MucHoan(goc.ChiTiet[0], 1m) },
            "HH2026-01",
            new DateTime(2027, 1, 15));

        // Hai tờ phải nằm cùng một năm mới đối chiếu được với nhau.
        Assert.Equal(2026, toHoan.Nam);
        Assert.Equal(new DateTime(2027, 1, 15), toHoan.NgayMo);
        Assert.Equal(new DateTime(2027, 1, 15), Assert.Single(toHoan.ChiTiet).Ngay);
        Assert.Equal(goc.Id, toHoan.HoaDonGocId);
    }

    [Fact]
    public void Tao_BoQuaMonKhongHoanGiVaGiuLyDo()
    {
        var (_, _, goc) = TaoSo();

        var toHoan = HoanHang.Tao(
            goc,
            new[] { new MucHoan(goc.ChiTiet[0], 0m), new MucHoan(goc.ChiTiet[1], 4m) },
            "HH2026-01",
            new DateTime(2026, 4, 2),
            "  Hàng lỗi  ");

        Assert.Equal("Băng tan", Assert.Single(toHoan.ChiTiet).TenHang);
        Assert.Equal("Hàng lỗi", toHoan.GhiChu);
    }

    [Fact]
    public void Tao_HoaDonGocDaChotVanHoanDuoc()
    {
        var (duLieu, _, goc) = TaoSo();
        goc.NgayChot = new DateTime(2026, 3, 31);

        var toHoan = Hoan(duLieu, goc, 1m);

        // Chốt là chặn sửa vào hoá đơn cũ; tờ hoàn là chứng từ riêng nên không dính.
        Assert.False(toHoan.DaChot);
        Assert.Equal(45_000m, toHoan.TienHoan);
        Assert.Equal(new DateTime(2026, 3, 31), goc.NgayChot);
    }

    // ---------- Còn hoàn được bao nhiêu ----------

    [Fact]
    public void DongCoTheHoanCua_TruSoDaHoanOTruocDo()
    {
        var (duLieu, _, goc) = TaoSo();
        Hoan(duLieu, goc, 3m);

        var dong = HoanHang.DongCoTheHoanCua(duLieu.HoaDons, goc);

        Assert.Equal(2, dong.Count);
        Assert.Equal(10m, dong[0].DaMua);
        Assert.Equal(3m, dong[0].DaHoan);
        Assert.Equal(7m, dong[0].ConHoanDuoc);

        // Món chưa hoàn lần nào thì còn nguyên.
        Assert.Equal(0m, dong[1].DaHoan);
        Assert.Equal(4m, dong[1].ConHoanDuoc);
    }

    [Fact]
    public void DongCoTheHoanCua_HoanHetThiKhongConHoanDuocNua()
    {
        var (duLieu, _, goc) = TaoSo();
        Hoan(duLieu, goc, 6m);
        Hoan(duLieu, goc, 4m, "HH2026-02");

        var dong = HoanHang.DongCoTheHoanCua(duLieu.HoaDons, goc);

        Assert.Equal(10m, dong[0].DaHoan);
        Assert.Equal(0m, dong[0].ConHoanDuoc);
    }

    [Fact]
    public void DongCoTheHoanCua_BoQuaDongTraLaiNgayTrongHoaDonGoc()
    {
        var (duLieu, _, goc) = TaoSo();
        goc.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = -2 });

        var dong = HoanHang.DongCoTheHoanCua(duLieu.HoaDons, goc);

        // Hàng đã trả lại ngay trong hoá đơn thì không hoàn lần nữa, kẻo trừ tiền hai lần.
        Assert.Equal(2, dong.Count);
        Assert.DoesNotContain(dong, d => d.Dong.SoLuong < 0m);
    }

    [Fact]
    public void DaHoan_ChiTinhToHoanCuaDungHoaDonGoc()
    {
        var (duLieu, khach, goc) = TaoSo();
        var gocKhac = new HoaDon
        {
            KhachHangId = khach.Id,
            MaHoaDon = "HD2026-02",
            Nam = 2026,
            NgayMo = new DateTime(2026, 5, 2),
        };
        gocKhac.ChiTiet.Add(new ChiTietHoaDon { TenHang = "Ống 27", DonVi = "Cây", DonGia = 45_000, SoLuong = 5 });
        duLieu.HoaDons.Add(gocKhac);

        Hoan(duLieu, goc, 2m);
        Hoan(duLieu, gocKhac, 5m, "HH2026-02");

        Assert.Equal(2m, HoanHang.DaHoan(duLieu.HoaDons, goc.Id, goc.ChiTiet[0].Id));
        Assert.Equal(90_000m, HoanHang.TienDaHoan(duLieu.HoaDons, goc.Id));
        Assert.Equal(225_000m, HoanHang.TienDaHoan(duLieu.HoaDons, gocKhac.Id));
        Assert.Equal("HH2026-01", Assert.Single(HoanHang.HoanCuaHoaDon(duLieu.HoaDons, goc.Id)).MaHoaDon);
    }

    // ---------- Mã hoá đơn ----------

    [Fact]
    public void TaoMaHoaDon_ToHoanDanhSoRiengKhongLamNhaySoHoaDonBan()
    {
        var thuMucTam = Path.Combine(Path.GetTempPath(), "QuanLyDienNuoc.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var kho = new KhoDuLieu(Path.Combine(thuMucTam, "dulieu.json"));
            var khach = new KhachHang { Ten = "Ông Long" };
            kho.DuLieu.KhachHangs.Add(khach);
            kho.DuLieu.HoaDons.Add(new HoaDon { KhachHangId = khach.Id, Nam = 2026 });

            Assert.Equal("HH2026-01", kho.TaoMaHoaDon(khach.Id, 2026, LoaiHoaDon.HoanHang));

            kho.DuLieu.HoaDons.Add(new HoaDon
            {
                KhachHangId = khach.Id,
                Nam = 2026,
                Loai = LoaiHoaDon.HoanHang,
            });

            Assert.Equal("HH2026-02", kho.TaoMaHoaDon(khach.Id, 2026, LoaiHoaDon.HoanHang));

            // Lập tờ hoàn không được làm hoá đơn bán tiếp theo nhảy số.
            Assert.Equal("HD2026-02", kho.TaoMaHoaDon(khach.Id, 2026));
        }
        finally
        {
            if (Directory.Exists(thuMucTam))
            {
                Directory.Delete(thuMucTam, recursive: true);
            }
        }
    }

    // ---------- Sổ công nợ và tin nhắc nợ ----------

    [Fact]
    public void CongNo_TruTienDaHoanKhoiSoNoCuaKhach()
    {
        var (duLieu, _, goc) = TaoSo();
        goc.ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 3, 20), SoTien = 200_000 });
        Hoan(duLieu, goc, 2m);

        var dong = Assert.Single(CongNo.Tinh(duLieu, nam: null, HomNay));

        Assert.Equal(380_000m, dong.TongMua);
        Assert.Equal(200_000m, dong.DaTra);
        Assert.Equal(180_000m, dong.ConNo);
    }

    [Fact]
    public void CongNo_HoanHetThiKhachHetNoVaKhongBiNhacNua()
    {
        var (duLieu, _, goc) = TaoSo();

        duLieu.HoaDons.Add(HoanHang.Tao(
            goc,
            new[] { new MucHoan(goc.ChiTiet[0], 10m), new MucHoan(goc.ChiTiet[1], 4m) },
            "HH2026-01",
            new DateTime(2026, 4, 2)));

        Assert.Equal(0m, duLieu.HoaDons.Sum(h => h.ConLai));
        Assert.Empty(CongNo.Tinh(duLieu, nam: null, HomNay));
    }

    [Fact]
    public void TinNhacNo_KeCaToHoanDeSoDoiChieuKhopVoiKhach()
    {
        var (duLieu, khach, goc) = TaoSo();
        Hoan(duLieu, goc, 2m, lyDo: "Hàng lỗi");

        var tin = TinNhacNo.Soan(khach, duLieu.HoaDons, HomNay);

        Assert.Contains("HD2026-01", tin);
        Assert.Contains("HH2026-01 (hoàn hàng ngày 02/04/2026): trừ 90.000đ", tin);
        Assert.Contains("Tổng còn lại: 380.000đ", tin);
    }

    // ---------- Ghi ra file rồi đọc lại ----------

    [Fact]
    public void LuuRoiNapLai_GiuNguyenLoaiToGocVaDongGoc()
    {
        var thuMucTam = Path.Combine(Path.GetTempPath(), "QuanLyDienNuoc.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var kho = new KhoDuLieu(Path.Combine(thuMucTam, "dulieu.json"));
            var (duLieu, khach, goc) = TaoSo();
            kho.DuLieu.KhachHangs.Add(khach);
            kho.DuLieu.HoaDons.Add(goc);
            kho.DuLieu.HoaDons.Add(Hoan(duLieu, goc, 2m));
            kho.Luu();

            // Loại ghi ra file bằng chữ để mở file JSON ra đọc là hiểu.
            Assert.Contains("\"Loai\": \"HoanHang\"", File.ReadAllText(kho.DuongDanFile));

            var khoMoi = new KhoDuLieu(kho.DuongDanFile);
            khoMoi.Nap();

            var toHoan = Assert.Single(khoMoi.DuLieu.HoaDons, h => h.LaHoanHang);
            Assert.Equal(goc.Id, toHoan.HoaDonGocId);
            Assert.Equal(-90_000m, toHoan.TongTien);
            Assert.Equal(goc.ChiTiet[0].Id, Assert.Single(toHoan.ChiTiet).DongGocId);
            Assert.Equal(2m, HoanHang.DaHoan(khoMoi.DuLieu.HoaDons, goc.Id, goc.ChiTiet[0].Id));
        }
        finally
        {
            if (Directory.Exists(thuMucTam))
            {
                Directory.Delete(thuMucTam, recursive: true);
            }
        }
    }

    [Fact]
    public void FileDuLieuCu_KhongCoLoaiThiVanLaHoaDonBan()
    {
        var thuMucTam = Path.Combine(Path.GetTempPath(), "QuanLyDienNuoc.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var file = Path.Combine(thuMucTam, "dulieu.json");
            Directory.CreateDirectory(thuMucTam);

            // Đúng như file của bản trước khi có hoá đơn hoàn hàng: không có trường "Loai".
            File.WriteAllText(file, """
                {
                  "KhachHangs": [],
                  "VatTus": [],
                  "HoaDons": [ { "MaHoaDon": "HD2026-01", "Nam": 2026 } ],
                  "BoHangs": []
                }
                """);

            var kho = new KhoDuLieu(file);
            kho.Nap();

            var hoaDon = Assert.Single(kho.DuLieu.HoaDons);
            Assert.Equal(LoaiHoaDon.Ban, hoaDon.Loai);
            Assert.False(hoaDon.LaHoanHang);
            Assert.Null(hoaDon.HoaDonGocId);
        }
        finally
        {
            if (Directory.Exists(thuMucTam))
            {
                Directory.Delete(thuMucTam, recursive: true);
            }
        }
    }

    // ---------- In ra Excel rồi đọc lại ----------

    [Fact]
    public void XuatExcel_ToHoanInSoDuongVaDocLaiThanhSoAm()
    {
        var thuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
        var thuMucTam = Path.Combine(Path.GetTempPath(), "qldn-hoan-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            var (duLieu, khach, goc) = TaoSo();
            var toHoan = Hoan(duLieu, goc, 2m, lyDo: "Hàng lỗi");

            var fileRa = Path.Combine(thuMucTam, "hoan-hang.xls");
            XuatHoaDon.Xuat(toHoan, khach, fileRa, thuMucMau, new DateTime(2026, 8, 3), goc);

            var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 4, 2));
            var trang = doc.Trang[0];

            // Trên giấy là số dương, đọc vào sổ thì là hàng trả về nên thành số âm.
            Assert.True(trang.LaHoanHang);
            Assert.Equal(-2m, Assert.Single(trang.Dong).SoLuong);
            Assert.Equal(45_000m, trang.Dong[0].DonGia);
            Assert.Equal(-90_000m, trang.TongTien);
            Assert.Equal(toHoan.TongTien, doc.Trang.Sum(t => t.TongTien));
        }
        finally
        {
            if (Directory.Exists(thuMucTam))
            {
                Directory.Delete(thuMucTam, recursive: true);
            }
        }
    }

    [Fact]
    public void XuatExcel_TenToHoanNamTrenBangVaKemHoaDonGoc()
    {
        var thuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
        var thuMucTam = Path.Combine(Path.GetTempPath(), "qldn-hoan-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            var (duLieu, khach, goc) = TaoSo();
            var toHoan = Hoan(duLieu, goc, 2m, lyDo: "Hàng lỗi");

            var fileRa = Path.Combine(thuMucTam, "hoan-hang.xls");
            XuatHoaDon.Xuat(toHoan, khach, fileRa, thuMucMau, new DateTime(2026, 8, 3), goc);

            using var doc = File.OpenRead(fileRa);
            using var wb = WorkbookFactory.Create(doc);
            var o = wb.GetSheetAt(0)
                .GetRow(MauHoaDon.Trang1.DongTieuDe)
                .GetCell(MauHoaDon.CotTieuDe)
                .StringCellValue;

            // Tên tờ phải nằm trên bảng hàng: DocHoaDon chỉ tìm chữ "hoàn hàng" ở phần trước
            // dòng tiêu đề bảng, ghi thấp hơn là nhập lại file thành hoá đơn bán.
            Assert.True(MauHoaDon.Trang1.DongTieuDe < MauHoaDon.Trang1.DongDauDuLieu - 1);
            Assert.Contains("HÓA ĐƠN HOÀN HÀNG", o);

            // Mẫu giấy mới không có dòng phụ đề riêng nên lý do hoàn và hoá đơn gốc phải được
            // gộp vào cùng dòng tên tờ, không thì tờ giấy không biết hoàn cho hoá đơn nào.
            Assert.Contains(goc.MaHoaDon, o);
            Assert.Contains("Hàng lỗi", o);
        }
        finally
        {
            if (Directory.Exists(thuMucTam))
            {
                Directory.Delete(thuMucTam, recursive: true);
            }
        }
    }
}
