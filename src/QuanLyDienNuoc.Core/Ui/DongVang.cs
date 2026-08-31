namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Cắm mấy **dòng vàng** — dòng gõ dở, chưa ghi vào sổ — vào đúng chỗ của trang đang xem.
/// <para>
/// Tách thành hàm thuần vì đây là chỗ dễ sai lặng lẽ: bảng chia trang nên chỗ cắm phải tính lùi
/// về chỉ số trong trang, mà cắm hai dòng vàng một lượt thì dòng cắm trước còn đẩy lệch chỗ của
/// dòng cắm sau. Sai một nấc là dòng gõ dở hiện ra trên một dòng khác chứ không phải dòng người
/// dùng vừa bấm chèn.
/// </para>
/// </summary>
public static class DongVang
{
    /// <summary>
    /// Cắm các dòng vàng vào <paramref name="trang"/> (đang chứa đúng các dòng thật của trang).
    /// <para>
    /// Đi từ chỗ cắm xa nhất về gần: cắm vào một chỗ nhỏ hơn thì không đẩy lệch chỗ đã cắm rồi.
    /// Hai dòng vàng cùng một chỗ thì dòng nào đứng trước trong <paramref name="dongVang"/> sẽ
    /// nằm **dưới** — cứ truyền dòng cuối lưới trước, dòng chèn giữa bảng sau, là ra đúng thứ tự
    /// người dùng gõ.
    /// </para>
    /// </summary>
    /// <param name="trangDangXem">Số hiệu trang đang xem, đếm từ 0.</param>
    /// <param name="dongVang">Chỗ cắm trong cả sổ (không phải trong trang) và chính dòng vàng.</param>
    public static void Cam<T>(
        List<T> trang,
        int trangDangXem,
        IReadOnlyList<(int ViTri, T Dong)> dongVang,
        int moiTrang = PhanTrang.MoiTrang)
    {
        // Số dòng thật của trang: mọi phép so đều theo con số này, chứ theo trang.Count đang lớn
        // dần sau mỗi lần cắm thì dòng vàng thứ hai lại được nới thêm một nấc.
        var soDongThat = trang.Count;

        foreach (var (viTri, dong) in dongVang.OrderByDescending(v => v.ViTri))
        {
            if (viTri < 0)
            {
                continue;
            }

            var trongTrang = viTri - (trangDangXem * moiTrang);

            // Nằm ngay sau dòng cuối trang cũng cắm (dấu <=): hoá đơn vừa tròn một trang thì dòng
            // vàng đứng luôn cuối trang ấy, chứ đẻ thêm một trang chỉ để chứa mỗi nó thì người
            // dùng tìm mãi không ra.
            if (trongTrang >= 0 && trongTrang <= soDongThat)
            {
                trang.Insert(trongTrang, dong);
            }
        }
    }
}
