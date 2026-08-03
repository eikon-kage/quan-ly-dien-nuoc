namespace QuanLyDienNuoc.Ui;

/// <summary>Các hộp thoại thông báo dùng chung, tiếng Việt.</summary>
public static class HopThoai
{
    public static void Bao(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void CanhBao(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Chú ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Loi(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static bool Hoi(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
