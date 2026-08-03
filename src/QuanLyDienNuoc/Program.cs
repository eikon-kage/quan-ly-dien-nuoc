using System.Globalization;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Forms;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc;

internal static class Program
{
    [STAThread]
    private static int Main(string[] thamSo)
    {
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        Thread.CurrentThread.CurrentCulture = vi;
        Thread.CurrentThread.CurrentUICulture = vi;

        ApplicationConfiguration.Initialize();

        // Chế độ chụp ảnh giao diện, dùng cho máy dựng tự động: --chup-anh <thư mục>
        var viTri = Array.IndexOf(thamSo, "--chup-anh");
        if (viTri >= 0)
        {
            var thuMucRa = viTri + 1 < thamSo.Length
                ? thamSo[viTri + 1]
                : Path.Combine(Directory.GetCurrentDirectory(), "anh-giao-dien");
            // Thoát dứt khoát: sau khi mở form mà không chạy vòng lặp thông điệp,
            // tiến trình vẫn còn luồng nền giữ lại, treo máy dựng cho tới lúc hết giờ.
            Environment.Exit(ChupAnhGiaoDien.Chay(thuMucRa));
        }

        var kho = KhoDuLieu.Instance;

        // Giữ file trong suốt phiên làm việc. Để dữ liệu trên thư mục mạng rồi mở ở hai máy
        // thì máy lưu sau đè mất máy lưu trước, nên máy thứ hai chỉ được xem.
        using var khoa = KhoaFile.Thu(kho.DuongDanFile);
        if (khoa is null && !ChapNhanChiXem(kho))
        {
            return 0;
        }

        try
        {
            kho.Nap();
        }
        catch (Exception ex)
        {
            HopThoai.Loi(
                null,
                $"Không đọc được file dữ liệu:\n{kho.DuongDanFile}\n\n{ex.Message}\n\n" +
                "Có thể đổi tên file dulieu.json.bak cạnh nó thành dulieu.json để khôi phục bản lưu trước đó.");
            return 1;
        }

        kho.HoiKhiFileBiMayKhacSua = HoiKhiFileBiMayKhacSua;

        if (!kho.ChiXem)
        {
            TuDongSaoLuu();
        }

        Application.Run(new MainForm());
        return 0;
    }

    /// <summary>Máy khác đang giữ file: hỏi xem có mở ở chế độ chỉ xem không. False là thoát.</summary>
    private static bool ChapNhanChiXem(KhoDuLieu kho)
    {
        var ai = KhoaFile.DocAiDangGiu(kho.DuongDanFile);
        var moTa = ai is null ? "một máy khác" : ai.MoTa;

        if (!HopThoai.Hoi(
                null,
                $"File dữ liệu đang được {moTa}:\n{kho.DuongDanFile}\n\n" +
                "Hai máy cùng sửa một file thì máy lưu sau đè mất việc của máy lưu trước.\n\n" +
                "Mở ở chế độ CHỈ XEM (xem và in được, không sửa được gì)?\n\n" +
                "Chọn Không để thoát và mở lại sau khi máy kia đóng phần mềm."))
        {
            return false;
        }

        kho.BatChiXem($"File đang được {moTa}");
        return true;
    }

    /// <summary>
    /// File đã bị máy khác sửa từ lúc mình đọc. Trả về true là ghi đè bằng dữ liệu đang mở,
    /// false là bỏ thay đổi vừa làm và nạp lại file.
    /// </summary>
    private static bool HoiKhiFileBiMayKhacSua(XungDotFile xungDot) => HopThoai.Hoi(
        Form.ActiveForm,
        $"File dữ liệu đã bị máy khác sửa lúc {xungDot.LucMayKhacSua:HH:mm dd/MM/yyyy}:\n{xungDot.DuongDanFile}\n\n" +
        "Chọn CÓ: ghi đè bằng dữ liệu đang mở ở máy này.\n" +
        $"Bản của máy kia được cất lại thành:\n{Path.GetFileName(xungDot.DuongDanCatBanMayKhac)}\n\n" +
        "Chọn KHÔNG: bỏ thay đổi vừa làm và nạp lại bản mới nhất trong file.");

    /// <summary>Sao lưu mỗi ngày một lần khi mở phần mềm. Lỗi sao lưu không được cản việc dùng app.</summary>
    private static void TuDongSaoLuu()
    {
        var kho = KhoDuLieu.Instance;
        try
        {
            if (SaoLuu.TuDongNeuCan(kho, kho.CaiDat) is { } ban)
            {
                kho.NhatKy.Ghi("Tự động sao lưu", ban.DuongDanJson);
            }
        }
        catch (Exception ex)
        {
            HopThoai.CanhBao(
                null,
                "Không tự sao lưu được:\n" + ex.Message +
                "\n\nVào Tiện ích → Sao lưu và khôi phục để chọn lại thư mục.");
        }
    }
}
