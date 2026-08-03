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
            return ChupAnhGiaoDien.Chay(thuMucRa);
        }

        try
        {
            KhoDuLieu.Instance.Nap();
        }
        catch (Exception ex)
        {
            HopThoai.Loi(
                null,
                $"Không đọc được file dữ liệu:\n{KhoDuLieu.Instance.DuongDanFile}\n\n{ex.Message}\n\n" +
                "Có thể đổi tên file dulieu.json.bak cạnh nó thành dulieu.json để khôi phục bản lưu trước đó.");
            return 1;
        }

        Application.Run(new MainForm());
        return 0;
    }
}
