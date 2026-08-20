using System.Text;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Forms;
using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Chạy phần mềm ở chế độ "chụp ảnh": tạo dữ liệu mẫu, mở lần lượt từng màn hình rồi
/// lưu thành ảnh PNG. Dùng trên máy Windows dựng tự động (GitHub Actions) để xem giao diện
/// mà không cần cài Windows ở máy làm việc.
/// </summary>
public static class ChupAnhGiaoDien
{
    private const int RongAnh = 1600;
    private const int CaoAnh = 950;

    private static readonly StringBuilder NhatKy = new();

    public static int Chay(string thuMucRa)
    {
        Directory.CreateDirectory(thuMucRa);
        var loi = 0;

        // BẮT BUỘC: ép dữ liệu mẫu vào thư mục riêng trước khi chạm tới kho dữ liệu.
        // Chế độ này xoá sạch khách hàng để dựng dữ liệu mẫu, chạy nhầm vào file thật là mất hết.
        Environment.SetEnvironmentVariable(
            "QLDN_FILE_DULIEU",
            Path.Combine(thuMucRa, "du-lieu-mau", "dulieu.json"));

        try
        {
            var kho = KhoDuLieu.Instance;
            kho.Nap();
            var (khach, hoaDon) = TaoDuLieuMau(kho);
            Ghi($"Dữ liệu mẫu: {kho.DuLieu.KhachHangs.Count} khách, {kho.DuLieu.HoaDons.Count} hoá đơn, file {kho.DuongDanFile}");

            var fileExcel = Path.Combine(thuMucRa, "hoa-don-mau-xuat-ra.xls");
            XuatHoaDon.Xuat(hoaDon, khach, fileExcel, ngayIn: new DateTime(2026, 8, 3));
            Ghi($"Đã xuất Excel mẫu: {fileExcel}");

            loi += ChupForm(thuMucRa, "01-trang-chu", () => new MainForm());
            loi += ChupForm(thuMucRa, "02-don-hang-cua-khach", () => new DonHangForm(khach.Id, 2026));
            loi += ChupForm(thuMucRa, "03-them-khach-hang", () => new KhachHangForm(null));
            loi += ChupForm(thuMucRa, "04-sua-khach-hang", () => new KhachHangForm(khach));
            loi += ChupForm(thuMucRa, "05-bang-gia-rieng", () => new BangGiaForm(khach.Id));
            loi += ChupForm(thuMucRa, "06-danh-muc-vat-tu", () => new VatTuForm());
            loi += ChupForm(thuMucRa, "07-thanh-toan", () => new ThanhToanForm(hoaDon.Id));
            loi += ChupForm(thuMucRa, "07b-thu-tien-cua-khach", () => new ThuTienForm(khach.Id));
            loi += ChupForm(thuMucRa, "08-tao-hoa-don", () => new HoaDonForm(null, "HD2026-03", 2026));
            loi += ChupForm(thuMucRa, "09-nhap-tu-excel", () => new NhapExcelForm(khach.Id, 2026, hoaDon.Id, fileExcel));
            loi += ChupForm(thuMucRa, "11-so-cong-no", () => new CongNoForm());
            loi += ChupForm(thuMucRa, "12-bo-hang", () => new BoHangForm());
            loi += ChupForm(
                thuMucRa,
                "13-nhap-nhieu-dong",
                () => new NhapNhieuDongForm(khach.Id, new DateTime(2026, 8, 3), "ống 27 x10, co 90 x5, keo dán ống x2, băng tan x5"));
            loi += ChupForm(thuMucRa, "14-sao-luu", () => new SaoLuuForm());
            loi += ChupForm(thuMucRa, "15-nhat-ky", () => new NhatKyForm());
            loi += ChupForm(
                thuMucRa,
                "16-tin-nhac-no",
                () => new VanBanForm(
                    "Tin nhắc nợ",
                    $"{khach.Ten} — chép rồi dán sang Zalo.",
                    BaoCao.TinNhacNo.Soan(khach, kho.HoaDonCuaKhach(khach.Id), new DateTime(2026, 8, 3), ThongTinCuaHang.DocTuMau())));

            // Chụp lúc chưa nối Supabase — đúng cái người dùng thấy lần đầu mở ra. Không gọi
            // mạng: cửa sổ chỉ gọi khi bấm đăng nhập.
            loi += ChupForm(thuMucRa, "17-cham-cong", () => new ChamCongForm());

            var fileToanBo = Path.Combine(thuMucRa, "toan-bo-du-lieu.xlsx");
            XuatToanBo.Xuat(kho.DuLieu, fileToanBo, new DateTime(2026, 8, 3));
            Ghi($"Đã xuất Excel toàn bộ dữ liệu: {fileToanBo}");

            loi += ChupBanIn(thuMucRa, hoaDon, khach);
        }
        catch (Exception ex)
        {
            Ghi("LỖI CHUNG: " + ex);
            loi++;
        }

        File.WriteAllText(Path.Combine(thuMucRa, "nhat-ky.txt"), NhatKy.ToString(), Encoding.UTF8);
        return loi == 0 ? 0 : 1;
    }

    private static int ChupForm(string thuMucRa, string ten, Func<Form> tao)
    {
        Form? form = null;
        try
        {
            form = tao();
            form.WindowState = FormWindowState.Normal;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Size = new Size(RongAnh, CaoAnh);
            form.ShowInTaskbar = false;
            form.Show();

            // Cửa sổ to hơn màn hình thì Windows co lại lúc hiện; đặt lại cỡ sau khi hiện
            // để ảnh luôn đủ rộng dù máy dựng chỉ có màn hình 1024x768.
            form.Size = new Size(RongAnh, CaoAnh);

            for (var i = 0; i < 8; i++)
            {
                Application.DoEvents();
                Thread.Sleep(60);
            }

            using var anh = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(anh, new Rectangle(0, 0, form.Width, form.Height));

            var duongDan = Path.Combine(thuMucRa, ten + ".png");
            anh.Save(duongDan, System.Drawing.Imaging.ImageFormat.Png);
            Ghi($"OK  {ten}.png  ({form.Width}x{form.Height})");
            return 0;
        }
        catch (Exception ex)
        {
            Ghi($"LỖI {ten}: {ex.GetType().Name} - {ex.Message}");
            return 1;
        }
        finally
        {
            form?.Close();
            form?.Dispose();
            Application.DoEvents();
        }
    }

    /// <summary>Vẽ bản in ra ảnh khổ A4 mà không cần máy in.</summary>
    private static int ChupBanIn(string thuMucRa, HoaDon hoaDon, KhachHang khach)
    {
        try
        {
            using var taiLieu = new InHoaDon(hoaDon, khach, ThongTinCuaHang.DocTuMau(), new DateTime(2026, 8, 3));

            // A4 = 8.27 x 11.69 inch. Toạ độ bản in tính bằng 1/100 inch, xuất ảnh ở 150 dpi.
            const int RongA4 = 827;
            const int CaoA4 = 1169;
            const float PhongTo = 1.5f;
            var le = new Rectangle(60, 50, RongA4 - 110, CaoA4 - 100);

            for (var trang = 0; trang < taiLieu.SoTrang; trang++)
            {
                using var anh = new Bitmap((int)(RongA4 * PhongTo), (int)(CaoA4 * PhongTo));
                using (var g = Graphics.FromImage(anh))
                {
                    g.Clear(Color.White);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.ScaleTransform(PhongTo, PhongTo);
                    taiLieu.VeTrangRaAnh(g, le, trang);
                }

                var ten = $"10-ban-in-trang-{trang + 1}.png";
                anh.Save(Path.Combine(thuMucRa, ten), System.Drawing.Imaging.ImageFormat.Png);
                Ghi($"OK  {ten}  (A4 150dpi)");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Ghi("LỖI bản in: " + ex.Message);
            return 1;
        }
    }

    private static (KhachHang Khach, HoaDon HoaDon) TaoDuLieuMau(KhoDuLieu kho)
    {
        kho.DuLieu.KhachHangs.Clear();
        kho.DuLieu.HoaDons.Clear();

        var khach = new KhachHang
        {
            Ten = "Ông Long (thợ xây)",
            DienThoai = "0912 345 678",
            DiaChi = "Xóm 5, Hải Minh, Hải Hậu",
            GhiChu = "Khách mối, thanh toán cuối tháng",
        };

        var khachKhac = new[]
        {
            new KhachHang { Ten = "Anh Dũng", DienThoai = "0987 111 222", DiaChi = "Hải Anh" },
            new KhachHang { Ten = "Chú Hải xây dựng", DienThoai = "0972 333 444", DiaChi = "Thị trấn Yên Định" },
            new KhachHang { Ten = "Cô Gấm tạp hoá", DienThoai = "0968 555 666", DiaChi = "Chợ Cồn" },
            new KhachHang { Ten = "Nhà thầu Nguyễn Văn Bình", DienThoai = "0913 777 888", DiaChi = "Hải Phương" },
        };

        kho.DuLieu.KhachHangs.Add(khach);
        kho.DuLieu.KhachHangs.AddRange(khachKhac);

        // Giá riêng cho khách mối
        foreach (var vatTu in kho.DuLieu.VatTus.Take(4))
        {
            khach.BangGiaRieng[vatTu.Id] = Math.Round(vatTu.DonGiaMacDinh * 0.92m / 500m, 0) * 500m;
        }

        var hoaDon = new HoaDon
        {
            KhachHangId = khach.Id,
            MaHoaDon = "HD2026-01",
            Nam = 2026,
            NgayMo = new DateTime(2026, 3, 5),
            GhiChu = "Công trình nhà 2 tầng",
        };

        var hang = new (string Ten, string DonVi, decimal Gia, decimal SoLuong, int NgayLech)[]
        {
            ("Ống 90", "Cây", 143000, 2, 0),
            ("Ống 76", "Cây", 37000, 2, 0),
            ("Góc 90", "Cái", 15000, 1, 0),
            ("Tê 76", "Cái", 17000, 1, 0),
            ("Ống 21", "m", 17000, 5.7m, 6),
            ("Ống 20", "m", 23000, 7, 6),
            ("Khoá 21", "Cái", 17000, 1, 6),
            ("Góc ren đồng 4", "Cái", 15000, 4, 12),
            ("Băng tan", "Cuộn", 5000, 5, 12),
            ("Keo dán ống", "Lọ", 8000, 2, 12),
            ("Dây điện Cadivi 2x1.5", "m", 12000, 40, 25),
            ("Ổ cắm đôi 3 chấu", "Cái", 65000, 6, 25),
            ("Aptomat 1 pha 20A", "Cái", 95000, 2, 25),
            ("Bóng đèn LED bulb 9W", "Bóng", 45000, 8, 40),
            ("Ống điều hoà", "m", 180000, 4.2m, 40),
            ("Tháo + lắp điều hoà", "Công", 450000, 1, 40),
            ("Công đục + cắt bê tông", "Công", 600000, 1, 52),
            ("Công làm", "Công", 300000, 2, 52),

            // Khách trả lại hàng thừa: số lượng âm nên thành tiền trừ bớt vào hoá đơn.
            ("Ống 21", "m", 17000, -1.7m, 55),
            ("Băng tan", "Cuộn", 5000, -2, 55),
        };

        foreach (var (ten, donVi, gia, soLuong, ngayLech) in hang)
        {
            hoaDon.ChiTiet.Add(new ChiTietHoaDon
            {
                Ngay = hoaDon.NgayMo.AddDays(ngayLech),
                TenHang = ten,
                DonVi = donVi,
                DonGia = gia,
                SoLuong = soLuong,
                VatTuId = kho.TimVatTuTheoTen(ten)?.Id,
            });
        }

        hoaDon.ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 3, 20), SoTien = 2000000, GhiChu = "Trả đợt 1" });
        hoaDon.ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 4, 15), SoTien = 1500000, GhiChu = "Chuyển khoản" });

        var hoaDonCu = new HoaDon
        {
            KhachHangId = khach.Id,
            MaHoaDon = "HD2026-02",
            Nam = 2026,
            NgayMo = new DateTime(2026, 6, 2),
            NgayChot = new DateTime(2026, 6, 28),
        };
        hoaDonCu.ChiTiet.Add(new ChiTietHoaDon { Ngay = new DateTime(2026, 6, 2), TenHang = "Bồn 1000", DonVi = "Cái", DonGia = 2150000, SoLuong = 1 });
        hoaDonCu.ChiTiet.Add(new ChiTietHoaDon { Ngay = new DateTime(2026, 6, 3), TenHang = "Chân giá bình nóng", DonVi = "Bộ", DonGia = 10000, SoLuong = 1 });
        hoaDonCu.ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 6, 28), SoTien = 1000000 });

        kho.DuLieu.HoaDons.Add(hoaDon);
        kho.DuLieu.HoaDons.Add(hoaDonCu);

        // Một lần khách đưa tiền trả cho cả hai hoá đơn, tự chia từ hoá đơn cũ nhất.
        BaoCao.ThuTien.Ghi(
            BaoCao.ThuTien.Chia(new[] { hoaDon, hoaDonCu }, 2_000_000m),
            new DateTime(2026, 7, 2),
            "Trả gộp cuối tháng");

        // Vài hoá đơn của khách khác cho trang chủ có số liệu
        var rong = new HoaDon
        {
            KhachHangId = khachKhac[0].Id,
            MaHoaDon = "HD2026-01",
            Nam = 2026,
            NgayMo = new DateTime(2026, 5, 12),
        };
        rong.ChiTiet.Add(new ChiTietHoaDon { Ngay = new DateTime(2026, 5, 12), TenHang = "Ống nhựa PVC D27", DonVi = "Cây", DonGia = 45000, SoLuong = 6 });
        kho.DuLieu.HoaDons.Add(rong);

        // Một khách nợ đã lâu để dải nhắc nợ và sổ công nợ có cái mà hiện.
        var noLau = new HoaDon
        {
            KhachHangId = khachKhac[2].Id,
            MaHoaDon = "HD2026-01",
            Nam = 2026,
            NgayMo = new DateTime(2026, 1, 8),
        };
        noLau.ChiTiet.Add(new ChiTietHoaDon { Ngay = new DateTime(2026, 1, 8), TenHang = "Máng đèn LED 1m2", DonVi = "Bộ", DonGia = 130000, SoLuong = 12 });
        noLau.ChiTiet.Add(new ChiTietHoaDon { Ngay = new DateTime(2026, 1, 9), TenHang = "Dây điện Cadivi 2x2.5", DonVi = "Mét", DonGia = 18000, SoLuong = 80 });
        noLau.ThanhToans.Add(new ThanhToan { Ngay = new DateTime(2026, 1, 20), SoTien = 1000000, GhiChu = "Trả trước" });
        kho.DuLieu.HoaDons.Add(noLau);

        // Mã tắt cho vài mặt hàng hay dùng
        void MaTat(string ten, string ma)
        {
            if (kho.TimVatTuTheoTen(ten) is { } v)
            {
                v.MaTat = ma;
            }
        }

        MaTat("Ống nhựa PVC D21", "o21");
        MaTat("Ống nhựa PVC D27", "o27");
        MaTat("Ống nhựa PVC D34", "o34");
        MaTat("Keo dán ống 100g", "keo");
        MaTat("Aptomat 1 pha 20A", "at20");

        // Bộ hàng thường dùng
        var boBon = new BoHang { Ten = "Bộ lắp bồn nước", GhiChu = "Hay đi cùng nhau" };
        foreach (var (ten, soLuong) in new (string, decimal)[]
                 {
                     ("Ống nhựa PVC D27", 3), ("Co nối PVC D21", 6), ("Tê PVC D21", 2),
                     ("Van khoá nước D21", 2), ("Keo dán ống 100g", 1),
                 })
        {
            var vatTu = kho.TimVatTuTheoTen(ten);
            boBon.Dong.Add(new DongBoHang
            {
                VatTuId = vatTu?.Id,
                TenHang = ten,
                DonVi = vatTu?.DonVi ?? string.Empty,
                SoLuong = soLuong,
            });
        }

        var boDien = new BoHang { Ten = "Bộ điện một phòng" };
        foreach (var (ten, soLuong) in new (string, decimal)[]
                 {
                     ("Dây điện Cadivi 2x1.5", 30), ("Ống ruột gà D20", 20),
                     ("Ổ cắm đôi 3 chấu", 2), ("Công tắc đơn", 2), ("Bóng đèn LED bulb 9W", 3),
                 })
        {
            var vatTu = kho.TimVatTuTheoTen(ten);
            boDien.Dong.Add(new DongBoHang
            {
                VatTuId = vatTu?.Id,
                TenHang = ten,
                DonVi = vatTu?.DonVi ?? string.Empty,
                SoLuong = soLuong,
            });
        }

        kho.DuLieu.BoHangs.Add(boBon);
        kho.DuLieu.BoHangs.Add(boDien);

        kho.Luu();

        // Một bản sao lưu và vài dòng nhật ký để hai màn hình đó có số liệu mà xem.
        kho.NhatKy.Ghi("Thêm khách hàng " + khach.Ten, luc: new DateTime(2026, 3, 5, 8, 12, 0));
        kho.NhatKy.Ghi("Thêm \"Ống 90\" ngày 05/03/2026", luc: new DateTime(2026, 3, 5, 8, 14, 0));
        kho.NhatKy.Ghi("Sửa đơn giá", "Ống 21: 15.000 → 17.000", new DateTime(2026, 3, 11, 16, 40, 0));
        kho.NhatKy.Ghi("Chốt hoá đơn HD2026-02", luc: new DateTime(2026, 6, 28, 17, 5, 0));
        SaoLuu.Tao(kho, kho.CaiDat, new DateTime(2026, 8, 3, 8, 15, 0));

        return (khach, hoaDon);
    }

    private static void Ghi(string dong)
    {
        NhatKy.AppendLine(dong);
        Console.WriteLine(dong);
    }
}
