namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Phép chia trang cho các bảng dài. Tách thành hàm thuần, không chạm vào giao diện, vì đây là
/// chỗ dễ sai lặng lẽ: xoá dòng cuối của trang cuối thì trang ấy biến mất, mà con trỏ trang vẫn
/// trỏ vào đó — bảng hiện ra trống trơn trong khi sổ vẫn còn đầy dòng.
/// </summary>
public static class PhanTrang
{
    /// <summary>Mỗi trang bao nhiêu dòng, dùng chung cho mọi bảng.</summary>
    public const int MoiTrang = 30;

    /// <summary>Tổng số trang. Sổ trống vẫn tính là một trang, để màn hình ghi "trang 1/1".</summary>
    public static int SoTrang(int tongDong, int moiTrang = MoiTrang)
    {
        if (moiTrang <= 0)
        {
            return 1;
        }

        return Math.Max(1, (tongDong + moiTrang - 1) / moiTrang);
    }

    /// <summary>
    /// Kẹp số trang về khoảng còn hợp lệ. Trang đang xem vượt quá cuối sổ (vừa xoá bớt dòng, vừa
    /// lọc hẹp lại) thì lùi về trang cuối chứ không hiện ra bảng trống.
    /// </summary>
    public static int TrangHopLe(int trang, int tongDong, int moiTrang = MoiTrang) =>
        Math.Clamp(trang, 0, SoTrang(tongDong, moiTrang) - 1);

    /// <summary>Cắt ra đúng một trang. <paramref name="trang"/> đếm từ 0.</summary>
    public static List<T> Cat<T>(IReadOnlyList<T> tatCa, int trang, int moiTrang = MoiTrang)
    {
        if (moiTrang <= 0)
        {
            return tatCa.ToList();
        }

        var batDau = TrangHopLe(trang, tatCa.Count, moiTrang) * moiTrang;
        return tatCa.Skip(batDau).Take(moiTrang).ToList();
    }

    /// <summary>
    /// Câu mô tả cho thanh phân trang: "Trang 2/7  ·  dòng 31–60 trong 196". Sổ trống thì nói
    /// thẳng là chưa có dòng nào, chứ đừng ghi "dòng 1–0".
    /// </summary>
    public static string MoTa(int trang, int tongDong, int moiTrang = MoiTrang)
    {
        if (tongDong == 0)
        {
            return "Chưa có dòng nào";
        }

        var soTrang = SoTrang(tongDong, moiTrang);
        var hopLe = TrangHopLe(trang, tongDong, moiTrang);
        var tu = (hopLe * moiTrang) + 1;
        var den = Math.Min(tongDong, tu + moiTrang - 1);

        return soTrang == 1
            ? $"{tongDong} dòng"
            : $"Trang {hopLe + 1}/{soTrang}   ·   dòng {tu}–{den} trong {tongDong}";
    }

    /// <summary>Trang chứa dòng thứ <paramml name="viTri"/> (đếm từ 0). Dùng để nhảy về đúng trang có dòng vừa chọn.</summary>
    public static int TrangCuaDong(int viTri, int moiTrang = MoiTrang) =>
        moiTrang <= 0 || viTri < 0 ? 0 : viTri / moiTrang;
}
