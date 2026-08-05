using System.Globalization;
using System.Runtime.CompilerServices;

namespace ChamCong.Tests;

/// <summary>
/// Ép toàn bộ kiểm thử chạy ở tiếng Việt, giống hệt lúc phần mềm chạy thật.
/// Danh sách thợ xếp theo tên nên thứ tự phụ thuộc ngôn ngữ của máy đang chạy.
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
