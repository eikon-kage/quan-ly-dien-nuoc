using NPOI.SS.UserModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;
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

    // ---------- Lập tờ hoàn bằng cách gõ tay từng món ----------

    [Fact]
    public void TaoTuDongGo_GhiDungNhungGiDaGoVaTruVaoNoCuaKhach()
    {
        var (duLieu, _, goc) = TaoSo();

        var toHoan = HoanHang.TaoTuDongGo(
            goc,
            new[]
            {
                new ChiTietHoaDon
                {
                    Ngay = new DateTime(2026, 4, 2),
                    TenHang = "  Ống 27  ",
                    DonVi = " Cây ",
                    DonGia = 45_000,
                    SoLuong = 2,
                    GhiChu = " nứt ",
                },
            },
            "HH2026-01",
            new DateTime(2026, 4, 2),
            "Hàng lỗi");

        duLieu.HoaDons.Add(toHoan);

        var dong = Assert.Single(toHoan.ChiTiet);
        Assert.Equal("Ống 27", dong.TenHang);
        Assert.Equal("Cây", dong.DonVi);
        Assert.Equal("nứt", dong.GhiChu);

        // Gõ số dương, vào sổ thành số âm nên tự trừ vào nợ của khách.
        Assert.Equal(-2m, dong.SoLuong);
        Assert.Equal(90_000m, toHoan.TienHoan);
        Assert.Equal(380_000m, duLieu.HoaDons.Sum(h => h.ConLai));

        // Hoá đơn gốc không bị sửa một chữ.
        Assert.Equal(470_000m, goc.TongTien);
        Assert.Equal(goc.Id, toHoan.HoaDonGocId);
        Assert.Equal(2026, toHoan.Nam);
    }

    [Fact]
    public void TaoTuDongGo_HoanDuocCaMonKhongCoTrenHoaDonGoc()
    {
        var (_, _, goc) = TaoSo();

        // Khách đổi trả món lấy từ lần khác, hoặc hai bên thoả lại giá lúc hoàn: tờ hoàn là
        // chứng từ riêng nên ghi đúng những gì đã bàn, không bị bó theo dòng của tờ gốc.
        var toHoan = HoanHang.TaoTuDongGo(
            goc,
            new[] { new ChiTietHoaDon { TenHang = "Van khoá 21", DonVi = "Cái", DonGia = 30_000, SoLuong = 3 } },
            "HH2026-01",
            new DateTime(2026, 4, 2));

        var dong = Assert.Single(toHoan.ChiTiet);
        Assert.Equal("Van khoá 21", dong.TenHang);
        Assert.Equal(90_000m, toHoan.TienHoan);

        // Không nối vào dòng nào của tờ gốc: món này đâu có trên tờ ấy.
        Assert.Null(dong.DongGocId);
    }

    [Fact]
    public void TaoTuDongGo_BoQuaDongTrongVaDongChuaGoSo()
    {
        var (_, _, goc) = TaoSo();

        var toHoan = HoanHang.TaoTuDongGo(
            goc,
            new[]
            {
                new ChiTietHoaDon { TenHang = "   ", DonGia = 45_000, SoLuong = 2 },
                new ChiTietHoaDon { TenHang = "Ống 27", DonGia = 45_000, SoLuong = 0 },
                new ChiTietHoaDon { TenHang = "Băng tan", DonVi = "Cuộn", DonGia = 5_000, SoLuong = 4 },
            },
            "HH2026-01",
            new DateTime(2026, 4, 2));

        Assert.Equal("Băng tan", Assert.Single(toHoan.ChiTiet).TenHang);
        Assert.Equal(20_000m, toHoan.TienHoan);
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

    // ---------- Nhập tờ hoàn từ file Excel ----------

    [Fact]
    public void XuatRoiNhapLai_ToHoanTuFileNoiDungLaiHoaDonGocVaLyDo()
    {
        var thuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
        var thuMucTam = Path.Combine(Path.GetTempPath(), "qldn-hoan-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            var (duLieu, khach, goc) = TaoSo();
            var toHoan = Hoan(duLieu, goc, 2m, lyDo: "Hàng lỗi");

            var fileRa = Path.Combine(thuMucTam, "hoan-hang.xls");
            XuatHoaDon.Xuat(toHoan, khach, fileRa, thuMucMau, new DateTime(2026, 8, 3), goc);

            // File Excel của tờ hoàn là chứng từ đủ: đọc lại biết ngay nó hoàn cho hoá đơn nào
            // và vì sao hoàn, nên nhập vào máy khác không phải gõ lại bằng tay.
            var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 4, 2));

            Assert.Equal("HD2026-01", doc.MaHoaDonGoc);
            Assert.Equal("Hàng lỗi", doc.LyDoHoan);
            Assert.Same(goc, HoanHang.TimHoaDonGoc(duLieu.HoaDons, doc.MaHoaDonGoc));
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
    public void XuatRoiNhapLai_ToHoanDungRiengThiKhongCoHoaDonGoc()
    {
        var thuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
        var thuMucTam = Path.Combine(Path.GetTempPath(), "qldn-hoan-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            var khach = new KhachHang { Ten = "Ông Long" };
            var toHoan = new HoaDon
            {
                KhachHangId = khach.Id,
                Loai = LoaiHoaDon.HoanHang,
                MaHoaDon = "HH2026-01",
                Nam = 2026,
                NgayMo = new DateTime(2026, 4, 2),
            };
            toHoan.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = new DateTime(2026, 4, 2),
                TenHang = "Ống 27",
                DonVi = "Cây",
                DonGia = 45_000,
                SoLuong = -2,
            });

            var fileRa = Path.Combine(thuMucTam, "hoan-hang-rieng.xls");
            XuatHoaDon.Xuat(toHoan, khach, fileRa, thuMucMau, new DateTime(2026, 8, 3));

            var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 4, 2));

            // Tờ hoàn không nối vào hoá đơn nào thì vẫn là tờ hoàn, chỉ là không có mã gốc —
            // câu "(Khách trả lại hàng)" in trên giấy chỉ là tên tờ nói lại, không phải lý do.
            Assert.True(doc.Trang[0].LaHoanHang);
            Assert.Null(doc.MaHoaDonGoc);
            Assert.Null(doc.LyDoHoan);
            Assert.Equal(-2m, Assert.Single(doc.Trang[0].Dong).SoLuong);
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
    public void NhapLaiToHoanTuFileExcel_ThanhMotDonHangRiengTruVaoNoVaBietDaHoanBaoNhieu()
    {
        var thuMucMau = Path.Combine(AppContext.BaseDirectory, "MauHoaDon");
        var thuMucTam = Path.Combine(Path.GetTempPath(), "qldn-hoan-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            // Cửa hàng xuất tờ hoàn ra Excel ở một máy...
            var (duLieu, khach, goc) = TaoSo();
            var fileRa = Path.Combine(thuMucTam, "hoan-hang.xls");
            XuatHoaDon.Xuat(
                HoanHang.Tao(goc, new[] { new MucHoan(goc.ChiTiet[0], 3m) }, "HH2026-01", new DateTime(2026, 4, 2), "Hàng lỗi"),
                khach,
                fileRa,
                thuMucMau,
                new DateTime(2026, 4, 2),
                goc);

            // ...máy kia nhập file đó vào: đúng những bước màn hình "Nhập hoá đơn từ Excel" làm.
            var soMayKia = new DuLieuApp();
            soMayKia.KhachHangs.Add(khach);
            soMayKia.HoaDons.Add(goc);

            var doc = DocHoaDon.Doc(fileRa, new DateTime(2026, 4, 2));
            var loai = LoaiToNhap.Xet(doc.Trang);
            Assert.True(loai.LaHoanHang);
            Assert.False(loai.LonLoai);

            var gocTrongSo = HoanHang.TimHoaDonGoc(soMayKia.HoaDons, doc.MaHoaDonGoc);
            Assert.Same(goc, gocTrongSo);

            var ghep = HoanHang.GhepVaoHoaDonGoc(
                soMayKia.HoaDons,
                gocTrongSo!,
                doc.Trang.SelectMany(t => t.Dong));

            var toHoan = new HoaDon
            {
                KhachHangId = khach.Id,
                Loai = LoaiHoaDon.HoanHang,
                HoaDonGocId = gocTrongSo!.Id,
                MaHoaDon = "HH2026-01",
                Nam = gocTrongSo.Nam,
                NgayMo = new DateTime(2026, 4, 2),
                GhiChu = doc.LyDoHoan ?? string.Empty,
            };
            toHoan.ChiTiet.AddRange(ghep.Dong);
            soMayKia.HoaDons.Add(toHoan);

            // Tờ hoàn nhập từ file là một đơn hàng riêng, chỉ khác là tiền của nó trừ đi.
            Assert.Empty(ghep.CanhBao);
            Assert.Equal("Hàng lỗi", toHoan.GhiChu);
            Assert.Equal(-135_000m, toHoan.TongTien);
            Assert.Equal(135_000m, toHoan.TienHoan);
            Assert.Equal(
                goc.TongTien - 135_000m,
                soMayKia.HoaDons.Where(h => h.KhachHangId == khach.Id).Sum(h => h.TongTien));

            // Và nó ghép được vào hoá đơn gốc nên hoàn tiếp chỉ còn hoàn được 7 cây.
            Assert.Equal(3m, HoanHang.DaHoan(soMayKia.HoaDons, goc.Id, goc.ChiTiet[0].Id));
            Assert.Equal(7m, HoanHang.DongCoTheHoanCua(soMayKia.HoaDons, goc)[0].ConHoanDuoc);
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
    public void TimHoaDonGoc_MaKhongCoTrongSoThiToHoanDungRieng()
    {
        var (duLieu, _, goc) = TaoSo();

        Assert.Same(goc, HoanHang.TimHoaDonGoc(duLieu.HoaDons, "hd2026-01"));
        Assert.Null(HoanHang.TimHoaDonGoc(duLieu.HoaDons, "HD2026-99"));
        Assert.Null(HoanHang.TimHoaDonGoc(duLieu.HoaDons, null));
        Assert.Null(HoanHang.TimHoaDonGoc(duLieu.HoaDons, "   "));
    }

    [Fact]
    public void TimHoaDonGoc_KhongLayToHoanLamHoaDonGoc()
    {
        var (duLieu, _, goc) = TaoSo();
        var toHoan = Hoan(duLieu, goc, 2m);

        // Trùng mã kiểu này chỉ xảy ra khi người dùng tự sửa mã, nhưng hoàn cho một tờ hoàn
        // thì số hoàn cộng ngược trở lại vào nợ — thà để tờ mới đứng riêng.
        toHoan.MaHoaDon = goc.MaHoaDon;
        duLieu.HoaDons.Remove(goc);

        Assert.Null(HoanHang.TimHoaDonGoc(duLieu.HoaDons, goc.MaHoaDon));
    }

    [Fact]
    public void GhepVaoHoaDonGoc_NoiTungDongVaoDongGocNenDaHoanCongDung()
    {
        var (duLieu, _, goc) = TaoSo();

        // Dòng đọc từ file hoàn hàng: mang số lượng âm, chưa biết hoàn cho dòng nào.
        var ghep = HoanHang.GhepVaoHoaDonGoc(duLieu.HoaDons, goc, new[]
        {
            new ChiTietHoaDon { TenHang = "Ống 27", DonVi = "Cây", DonGia = 45_000, SoLuong = -3 },
        });

        var dong = Assert.Single(ghep.Dong);
        Assert.Empty(ghep.CanhBao);
        Assert.Equal(goc.ChiTiet[0].Id, dong.DongGocId);
        Assert.Equal(-3m, dong.SoLuong);

        // Ghi vào sổ rồi thì phải thấy 3 cái đã hoàn, chỉ còn hoàn được 7.
        var toHoan = new HoaDon
        {
            KhachHangId = goc.KhachHangId,
            Loai = LoaiHoaDon.HoanHang,
            HoaDonGocId = goc.Id,
            MaHoaDon = "HH2026-01",
            Nam = 2026,
        };
        toHoan.ChiTiet.AddRange(ghep.Dong);
        duLieu.HoaDons.Add(toHoan);

        var conHoan = HoanHang.DongCoTheHoanCua(duLieu.HoaDons, goc);
        Assert.Equal(3m, conHoan[0].DaHoan);
        Assert.Equal(7m, conHoan[0].ConHoanDuoc);
    }

    [Fact]
    public void GhepVaoHoaDonGoc_HoaDonGocBanNhieuNgayThiTachDongTheoTungNgay()
    {
        var (duLieu, _, goc) = TaoSo();
        goc.ChiTiet.Add(new ChiTietHoaDon
        {
            Ngay = new DateTime(2026, 3, 20),
            TenHang = "Ống 27",
            DonVi = "Cây",
            DonGia = 45_000,
            SoLuong = 4,
        });

        var ghep = HoanHang.GhepVaoHoaDonGoc(duLieu.HoaDons, goc, new[]
        {
            new ChiTietHoaDon { TenHang = "Ống 27", DonVi = "Cây", DonGia = 45_000, SoLuong = -12 },
        });

        // 12 cái hoàn: 10 của dòng ngày 5/3 và 2 của dòng ngày 20/3.
        Assert.Empty(ghep.CanhBao);
        Assert.Equal(2, ghep.Dong.Count);
        Assert.Equal(new[] { -10m, -2m }, ghep.Dong.Select(d => d.SoLuong).ToArray());
        Assert.Equal(
            new[] { goc.ChiTiet[0].Id, goc.ChiTiet[2].Id },
            ghep.Dong.Select(d => d.DongGocId!.Value).ToArray());
        Assert.Equal(-540_000m, ghep.Dong.Sum(d => d.ThanhTien));
    }

    [Fact]
    public void GhepVaoHoaDonGoc_HoanQuaSoDaLayThiVanGhiNhungBaoMotCau()
    {
        var (duLieu, _, goc) = TaoSo();
        Hoan(duLieu, goc, 8m);

        var ghep = HoanHang.GhepVaoHoaDonGoc(duLieu.HoaDons, goc, new[]
        {
            new ChiTietHoaDon { TenHang = "Ống 27", DonVi = "Cây", DonGia = 45_000, SoLuong = -5 },
        });

        // Còn hoàn được 2, tờ giấy ghi 5: sổ phải khớp tờ khách đang giữ nên vẫn ghi đủ 5, chỗ
        // vượt tách riêng và báo một câu.
        Assert.Equal(-5m, ghep.Dong.Sum(d => d.SoLuong));
        Assert.Equal(goc.ChiTiet[0].Id, ghep.Dong[0].DongGocId);
        Assert.Equal(-2m, ghep.Dong[0].SoLuong);
        Assert.Null(ghep.Dong[1].DongGocId);
        Assert.Equal(-3m, ghep.Dong[1].SoLuong);

        // Câu nhắc nói theo phần thừa — "hoàn quá 3 Cây". Đưa số 3 vào chỗ đọc ra là "còn hoàn
        // được" thì người dùng đối chiếu ngược hẳn con số trên giấy.
        var cau = Assert.Single(ghep.CanhBao);
        Assert.Contains("Ống 27", cau);
        Assert.Contains($"hoàn quá {So.Luong(3m)} Cây", cau);
        Assert.Contains(goc.MaHoaDon, cau);
    }

    [Fact]
    public void GhepVaoHoaDonGoc_GiaLechHoacMonLaThiDeDungRiengVaBao()
    {
        var (duLieu, _, goc) = TaoSo();

        var ghep = HoanHang.GhepVaoHoaDonGoc(duLieu.HoaDons, goc, new[]
        {
            // Cùng tên nhưng giá khác: hoàn theo giá nào là chuyện của người bán, đừng nối bừa.
            new ChiTietHoaDon { TenHang = "Ống 27", DonVi = "Cây", DonGia = 50_000, SoLuong = -1 },
            new ChiTietHoaDon { TenHang = "Keo dán", DonVi = "Lọ", DonGia = 20_000, SoLuong = -1 },
        });

        Assert.Equal(2, ghep.Dong.Count);
        Assert.All(ghep.Dong, d => Assert.Null(d.DongGocId));
        Assert.Equal(2, ghep.CanhBao.Count);
        Assert.All(ghep.CanhBao, c => Assert.Contains(goc.MaHoaDon, c));
    }

    [Fact]
    public void GhepVaoHoaDonGoc_KhongXetDauVaHoaThuongCuaTenHang()
    {
        var (duLieu, _, goc) = TaoSo();

        var ghep = HoanHang.GhepVaoHoaDonGoc(duLieu.HoaDons, goc, new[]
        {
            new ChiTietHoaDon { TenHang = "BĂNG TAN", DonVi = "Cuộn", DonGia = 5_000, SoLuong = -1 },
        });

        Assert.Equal(goc.ChiTiet[1].Id, Assert.Single(ghep.Dong).DongGocId);
        Assert.Empty(ghep.CanhBao);
    }

    [Fact]
    public void GhepVaoHoaDonGoc_DongThieuSoLuongThiGiuNguyenDeNguoiDungSua()
    {
        var (duLieu, _, goc) = TaoSo();

        var ghep = HoanHang.GhepVaoHoaDonGoc(duLieu.HoaDons, goc, new[]
        {
            new ChiTietHoaDon { TenHang = "Ống 27", DonVi = "Cây", DonGia = 45_000, SoLuong = 0 },
        });

        var dong = Assert.Single(ghep.Dong);
        Assert.Equal(0m, dong.SoLuong);
        Assert.Null(dong.DongGocId);
        Assert.Empty(ghep.CanhBao);
    }

    [Fact]
    public void LoaiToNhap_TichLanCaToBanVaToHoanThiBaoLonLoai()
    {
        var toBan = new TrangDoc { TenSheet = "Trang 1" };
        var toHoan = new TrangDoc { TenSheet = "Trang 1", LaHoanHang = true };

        Assert.Equal(new LoaiToNhap(false, false), LoaiToNhap.Xet(Array.Empty<TrangDoc>()));
        Assert.Equal(new LoaiToNhap(false, false), LoaiToNhap.Xet(new[] { toBan, toBan }));
        Assert.Equal(new LoaiToNhap(true, false), LoaiToNhap.Xet(new[] { toHoan, toHoan }));
        Assert.Equal(new LoaiToNhap(false, true), LoaiToNhap.Xet(new[] { toBan, toHoan }));
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
