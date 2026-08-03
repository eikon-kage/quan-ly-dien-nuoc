using System.Globalization;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Forms;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        Thread.CurrentThread.CurrentCulture = vi;
        Thread.CurrentThread.CurrentUICulture = vi;

        ApplicationConfiguration.Initialize();

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
            return;
        }

        Application.Run(new MainForm());
    }
}
