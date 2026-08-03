namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Tìm tên hàng theo kiểu gõ tắt: không dấu, không cần đúng thứ tự, và nhận cả mã tắt
/// do cửa hàng tự đặt. Gõ "ong 27" hay "27 ong" đều ra "Ống nhựa PVC D27".
/// </summary>
public static class TimHang
{
    /// <summary>Điểm càng cao thì gợi ý càng đứng trên. 0 là không khớp.</summary>
    public static int Diem(string? ten, string? maTat, string? tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return 1;
        }

        var khoa = ChuViet.BoDau(tuKhoa).Trim();
        if (khoa.Length == 0)
        {
            return 1;
        }

        var tenGon = ChuViet.BoDau(ten);
        var ma = ChuViet.BoDau(maTat);

        if (ma.Length > 0)
        {
            if (ma == khoa)
            {
                return 100;
            }

            if (ma.StartsWith(khoa, StringComparison.Ordinal))
            {
                return 90;
            }
        }

        if (tenGon.StartsWith(khoa, StringComparison.Ordinal))
        {
            return 80;
        }

        if (tenGon.Contains(khoa, StringComparison.Ordinal))
        {
            return 60;
        }

        // Gõ nhiều mảnh rời: "27 ong" vẫn ra "ong nhua pvc d27".
        var manh = khoa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (manh.Length > 1 && manh.All(m => tenGon.Contains(m, StringComparison.Ordinal)))
        {
            return 40;
        }

        // Bỏ hết khoảng trắng: gõ "ongnhua" vẫn ra "ống nhựa".
        var tenLien = tenGon.Replace(" ", string.Empty);
        var khoaLien = khoa.Replace(" ", string.Empty);
        if (khoaLien.Length > 0 && tenLien.Contains(khoaLien, StringComparison.Ordinal))
        {
            return 30;
        }

        return 0;
    }

    public static bool Khop(string? ten, string? maTat, string? tuKhoa) => Diem(ten, maTat, tuKhoa) > 0;
}
