using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Thứ tự các dòng hàng trong một hoá đơn: xếp theo ngày lấy hàng, còn trong cùng một ngày
/// thì giữ nguyên thứ tự chủ cửa hàng đã xếp. Nhờ vậy chèn thêm một dòng vào giữa thì dòng
/// đó nằm yên đúng chỗ — trên lưới thấy sao thì tờ hoá đơn in ra đúng như vậy.
/// </summary>
public static class ThuTuDong
{
    /// <summary>Thứ tự để hiện lên lưới, in ra giấy và xuất Excel.</summary>
    public static List<ChiTietHoaDon> TheoThuTu(IEnumerable<ChiTietHoaDon> chiTiet) =>
        // OrderBy giữ nguyên thứ tự cũ của những phần tử bằng điểm nhau, nên các dòng cùng
        // một ngày vẫn theo đúng thứ tự trong danh sách gốc.
        chiTiet.OrderBy(c => c.Ngay.Date).ToList();

    /// <summary>
    /// Chỗ cần chèn vào danh sách để dòng mới nằm ngay trên (hoặc ngay dưới) dòng mốc.
    /// Không có dòng mốc thì chèn vào cuối, tức là thêm bình thường.
    /// </summary>
    public static int ViTriChen(IList<ChiTietHoaDon> chiTiet, Guid? mocId, bool chenDuoi)
    {
        if (mocId is not { } id)
        {
            return chiTiet.Count;
        }

        for (var i = 0; i < chiTiet.Count; i++)
        {
            if (chiTiet[i].Id == id)
            {
                return chenDuoi ? i + 1 : i;
            }
        }

        return chiTiet.Count;
    }

    /// <summary>
    /// Chèn một dòng vào cạnh dòng mốc. Dòng mới phải cùng ngày với dòng mốc thì mới đứng
    /// yên đúng chỗ, nên hàm này đặt luôn ngày cho nó.
    /// </summary>
    public static void Chen(IList<ChiTietHoaDon> chiTiet, ChiTietHoaDon dongMoi, Guid? mocId, bool chenDuoi)
    {
        var viTri = ViTriChen(chiTiet, mocId, chenDuoi);

        if (mocId is { } id && chiTiet.FirstOrDefault(c => c.Id == id) is { } moc)
        {
            dongMoi.Ngay = moc.Ngay;
        }

        chiTiet.Insert(viTri, dongMoi);
    }

    /// <summary>
    /// Chuyển cả một nhóm dòng lên (hoặc xuống) một bậc, thứ tự trong nhóm giữ nguyên. Trả về
    /// số dòng đã chuyển được: 0 là cả nhóm đã sát đầu / cuối ngày của nó, danh sách không đổi.
    /// </summary>
    public static int ChuyenNhom(IList<ChiTietHoaDon> chiTiet, IEnumerable<Guid> id, bool xuong)
    {
        // Chạy theo đúng thứ tự đang hiện trên bảng, không theo thứ tự người dùng bấm chọn.
        var can = id.ToHashSet();
        var thuTuChay = TheoThuTu(chiTiet).Where(c => can.Contains(c.Id)).Select(c => c.Id).ToList();

        // Chuyển xuống thì đi từ dòng cuối nhóm lên, chuyển lên thì đi từ dòng đầu nhóm xuống —
        // làm ngược lại là dòng nọ đè lên dòng kia, cả nhóm dồn cục vào nhau.
        if (xuong)
        {
            thuTuChay.Reverse();
        }

        var soDaChuyen = 0;
        foreach (var mot in thuTuChay)
        {
            // Một dòng đã sát mép ngày thì dừng luôn: nhóm liền khối không đi tiếp được nữa, mà
            // đi tiếp là các dòng sau chồng vào chỗ dòng này.
            if (!Chuyen(chiTiet, mot, xuong))
            {
                break;
            }

            soDaChuyen++;
        }

        return soDaChuyen;
    }

    /// <summary>
    /// Đổi chỗ một dòng với dòng liền kề phía trên (hoặc phía dưới). Chỉ chuyển được trong
    /// cùng một ngày — muốn chuyển sang ngày khác thì sửa ô NGÀY. Trả về false khi dòng đã
    /// nằm ở đầu / cuối ngày của nó, lúc đó danh sách không đổi.
    /// </summary>
    public static bool Chuyen(IList<ChiTietHoaDon> chiTiet, Guid id, bool xuong)
    {
        var thuTu = TheoThuTu(chiTiet);

        var viTri = thuTu.FindIndex(c => c.Id == id);
        if (viTri < 0)
        {
            return false;
        }

        var ke = xuong ? viTri + 1 : viTri - 1;
        if (ke < 0 || ke >= thuTu.Count || thuTu[ke].Ngay.Date != thuTu[viTri].Ngay.Date)
        {
            return false;
        }

        var a = chiTiet.IndexOf(thuTu[viTri]);
        var b = chiTiet.IndexOf(thuTu[ke]);
        if (a < 0 || b < 0)
        {
            return false;
        }

        (chiTiet[a], chiTiet[b]) = (chiTiet[b], chiTiet[a]);
        return true;
    }
}
