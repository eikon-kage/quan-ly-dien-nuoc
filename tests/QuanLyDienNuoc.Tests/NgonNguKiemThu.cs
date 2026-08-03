using System.Globalization;
using System.Runtime.CompilerServices;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Ép toàn bộ kiểm thử chạy ở tiếng Việt, giống hệt lúc phần mềm chạy thật (xem Program.Main).
/// Không có đoạn này thì máy dựng của GitHub (tiếng Anh) in tiền ra "1,000,000" và ngày ra
/// "8/3/2026", làm hỏng những bài kiểm thử đọc chữ hiển thị.
/// </summary>
internal static class NgonNguKiemThu
{
    [ModuleInitializer]
    internal static void DatTiengViet()
    {
        var vi = new CultureInfo("vi-VN");
        CultureInfo.DefaultThreadCurrentCulture = vi;
        CultureInfo.DefaultThreadCurrentUICulture = vi;
        Thread.CurrentThread.CurrentCulture = vi;
        Thread.CurrentThread.CurrentUICulture = vi;
    }
}
