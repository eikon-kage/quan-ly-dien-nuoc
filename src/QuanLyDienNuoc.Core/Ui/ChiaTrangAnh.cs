namespace QuanLyDienNuoc.Ui;

/// <summary>Một khối chiếm chỗ theo chiều dọc trên ảnh bảng kê.</summary>
/// <param name="Cao">Chiều cao khối, tính bằng điểm ảnh.</param>
/// <param name="LaDauNhom">
/// Khối này là dòng ghi mã tờ hoá đơn. Nó không được đứng một mình ở cuối trang: khách nhận
/// được tấm ảnh kết thúc bằng chữ "Hoá đơn HD2026-07" mà không có dòng hàng nào bên dưới thì
/// tưởng tờ ấy trống.
/// </param>
public readonly record struct KhoiAnh(int Cao, bool LaDauNhom);

/// <summary>
/// Chia các dòng hàng của bảng kê thành nhiều tấm ảnh khi một tấm dài quá. Tách thành hàm
/// thuần, không chạm vào <c>Graphics</c>, để kiểm thử được bằng mấy con số chiều cao — chỗ dễ
/// sai ở đây là **dòng cuối trang**: tính hụt một dòng là ảnh cắt mất chữ, tính thừa là sinh
/// ra một tấm ảnh chỉ có đầu và chân.
/// </summary>
public static class ChiaTrangAnh
{
    /// <summary>
    /// Xếp khối vào từng trang, không trang nào (trừ trang chỉ có đúng một khối quá khổ) cao
    /// hơn <paramref name="choTrong"/>.
    /// </summary>
    /// <param name="choTrong">Chỗ còn lại cho các khối trên một trang, sau khi trừ đầu và chân ảnh.</param>
    /// <param name="caoNhomTiep">
    /// Chiều cao dòng ghi lại mã tờ ở đầu trang sau, khi một tờ bị cắt ngang giữa hai trang.
    /// Bảng kê không ghi mã tờ (khách chỉ lấy ở một tờ) thì truyền 0.
    /// </param>
    /// <returns>Mỗi trang là danh sách chỉ số khối thuộc trang ấy, giữ nguyên thứ tự đầu vào.</returns>
    public static List<List<int>> Chia(IReadOnlyList<KhoiAnh> khoi, int choTrong, int caoNhomTiep = 0)
    {
        var trang = new List<List<int>>();
        if (khoi.Count == 0)
        {
            return trang;
        }

        var hienTai = new List<int>();
        var daDung = 0;

        // Đã gặp một dòng mã tờ thì tờ ấy còn chạy tiếp sang trang sau, nên trang sau phải chừa
        // chỗ ghi lại mã tờ.
        var dangGiuaNhom = false;

        for (var i = 0; i < khoi.Count; i++)
        {
            // Dòng mã tờ phải kéo theo dòng hàng đầu tiên của tờ: hai khối cùng vừa thì mới đặt.
            var can = khoi[i].Cao;
            if (khoi[i].LaDauNhom && i + 1 < khoi.Count)
            {
                can += khoi[i + 1].Cao;
            }

            if (hienTai.Count > 0 && daDung + can > choTrong)
            {
                trang.Add(hienTai);
                hienTai = new List<int>();
                daDung = dangGiuaNhom && !khoi[i].LaDauNhom ? caoNhomTiep : 0;
            }

            hienTai.Add(i);
            daDung += khoi[i].Cao;

            if (khoi[i].LaDauNhom)
            {
                dangGiuaNhom = true;
            }
        }

        trang.Add(hienTai);
        return trang;
    }
}
