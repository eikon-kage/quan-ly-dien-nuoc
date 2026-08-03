using QuanLyDienNuoc.Data;

namespace QuanLyDienNuoc.Ui;

/// <summary>Các hộp thoại thông báo dùng chung, tiếng Việt.</summary>
public static class HopThoai
{
    /// <summary>
    /// Đang mở ở chế độ chỉ xem thì báo cho người dùng và trả về true để nơi gọi dừng lại,
    /// khỏi hiện những câu như "đã ghi xong" trong khi chẳng ghi được gì.
    /// </summary>
    public static bool ChanKhiChiXem(IWin32Window? chu, KhoDuLieu kho)
    {
        if (!kho.ChiXem)
        {
            return false;
        }

        CanhBao(
            chu,
            $"Đang mở ở chế độ CHỈ XEM nên không ghi được.\n\n{kho.LyDoChiXem}.\n\n" +
            "Đóng phần mềm ở máy kia rồi mở lại là sửa được bình thường.");
        return true;
    }

    public static void Bao(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void CanhBao(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Chú ý", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Loi(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static bool Hoi(IWin32Window? chu, string noiDung) =>
        MessageBox.Show(chu, noiDung, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
