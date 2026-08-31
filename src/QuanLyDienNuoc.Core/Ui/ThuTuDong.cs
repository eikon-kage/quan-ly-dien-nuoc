using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Thứ tự các dòng hàng trong một hoá đơn: **đúng thứ tự chủ cửa hàng đã xếp**, giữ nguyên như
/// trong sổ. Trên lưới thấy sao thì tờ hoá đơn in ra và file Excel đúng như vậy.
/// <para>
/// Trước đây bảng tự xếp lại theo ngày lấy hàng. Bỏ đi vì nó giành quyền của người dùng: gõ bù
/// một dòng của hôm trước là dòng ấy tự nhảy lên giữa bảng, sửa ô NGÀY một dòng là nó biến mất
/// khỏi chỗ đang nhìn. Tờ hoá đơn viết tay vốn hàng nào ghi trước thì nằm trước, phần mềm theo
/// đúng nếp ấy; muốn đổi chỗ thì có Alt+↑ / Alt+↓ và Ctrl+Enter chèn dòng.
/// </para>
/// </summary>
public static class ThuTuDong
{
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
    /// Chèn một dòng vào cạnh dòng mốc. Ngày của dòng mới để nguyên như người dùng đã gõ: bảng
    /// không xếp lại theo ngày nên dòng nằm ở đâu là do chỗ chèn, không phải do ngày của nó.
    /// </summary>
    public static void Chen(IList<ChiTietHoaDon> chiTiet, ChiTietHoaDon dongMoi, Guid? mocId, bool chenDuoi)
    {
        chiTiet.Insert(ViTriChen(chiTiet, mocId, chenDuoi), dongMoi);
    }

    /// <summary>
    /// Chuyển cả một nhóm dòng lên (hoặc xuống) một bậc, thứ tự trong nhóm giữ nguyên. Trả về
    /// số dòng đã chuyển được: 0 là cả nhóm đã sát đầu / cuối bảng, danh sách không đổi.
    /// </summary>
    public static int ChuyenNhom(IList<ChiTietHoaDon> chiTiet, IEnumerable<Guid> id, bool xuong)
    {
        // Chạy theo đúng thứ tự đang hiện trên bảng, không theo thứ tự người dùng bấm chọn.
        var can = id.ToHashSet();
        var thuTuChay = chiTiet.Where(c => can.Contains(c.Id)).Select(c => c.Id).ToList();

        // Chuyển xuống thì đi từ dòng cuối nhóm lên, chuyển lên thì đi từ dòng đầu nhóm xuống —
        // làm ngược lại là dòng nọ đè lên dòng kia, cả nhóm dồn cục vào nhau.
        if (xuong)
        {
            thuTuChay.Reverse();
        }

        var soDaChuyen = 0;
        foreach (var mot in thuTuChay)
        {
            // Một dòng đã sát mép bảng thì dừng luôn: nhóm liền khối không đi tiếp được nữa, mà
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
    /// Đổi chỗ một dòng với dòng liền kề phía trên (hoặc phía dưới). Đi được khắp bảng, kể cả
    /// vượt sang chỗ có ngày khác — bảng không còn xếp theo ngày thì cũng không có mép ngày nào
    /// để chặn. Trả về false khi dòng đã nằm ở đầu / cuối bảng, lúc đó danh sách không đổi.
    /// </summary>
    public static bool Chuyen(IList<ChiTietHoaDon> chiTiet, Guid id, bool xuong)
    {
        var viTri = -1;
        for (var i = 0; i < chiTiet.Count; i++)
        {
            if (chiTiet[i].Id == id)
            {
                viTri = i;
                break;
            }
        }

        if (viTri < 0)
        {
            return false;
        }

        var ke = xuong ? viTri + 1 : viTri - 1;
        if (ke < 0 || ke >= chiTiet.Count)
        {
            return false;
        }

        (chiTiet[viTri], chiTiet[ke]) = (chiTiet[ke], chiTiet[viTri]);
        return true;
    }
}
