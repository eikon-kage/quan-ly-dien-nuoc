namespace ChamCong.SoDiDong;

/// <summary>
/// Sổ chấm công **đúng dáng app điện thoại ghi ra**: tên trường, kiểu dữ liệu, cách để ngày
/// đều theo `mobile/src/nghiepvu/kieu.ts`.
///
/// <para>
/// Cố ý tách khỏi <see cref="ChamCong.Models"/>: bộ kia là mô hình của bản máy tính viết
/// trước, thợ chỉ có một mức tiền công và chưa có kỳ quyết toán. Sổ trên điện thoại đã đi xa
/// hơn (mốc lương theo thời gian, kỳ đã chốt). Máy tính **đọc** sổ ấy thì phải đọc đúng dáng
/// của nó, chứ nhét vào mô hình cũ là mất mốc lương và mất kỳ.
/// </para>
///
/// <para>
/// Ngày để dạng chuỗi "yyyy-MM-dd" y như app điện thoại, không đổi sang DateTime: chuỗi ấy so
/// sánh và sắp xếp đã đúng thứ tự thời gian, mà không mang theo giờ với múi giờ để lệch ngày.
/// </para>
/// </summary>
public sealed class SoChamCong
{
    public List<Tho> Thos { get; set; } = new();

    public List<BuoiCong> BuoiCongs { get; set; } = new();

    public List<UngTien> UngTiens { get; set; } = new();

    /// <summary>Ghi chú của từng ngày, mỗi cặp (thợ, ngày) nhiều nhất một bản ghi.</summary>
    public List<GhiChuNgay> GhiChuNgays { get; set; } = new();

    /// <summary>Các kỳ đã quyết toán, xếp theo thứ tự chốt — kỳ mới nhất nằm cuối.</summary>
    public List<KyLuong> KyLuongs { get; set; } = new();
}

/// <summary>
/// Ghi chú cho **cả một ngày** của một thợ, không phải cho một buổi. Chủ gõ trên điện thoại lúc
/// chấm công; máy tính chỉ đọc ra mà xem.
///
/// <para>
/// Không móc vào buổi công, và đó là chủ ý của bên ấy: ngày thợ nghỉ hẳn thì không có buổi nào
/// để treo ghi chú vào, mà đó lại đúng là ngày cần ghi chú nhất.
/// </para>
///
/// <para>
/// Không có <c>Id</c>: khoá là cặp (thợ, ngày). Không ai trỏ vào bản ghi này — kỳ lương chỉ nhớ
/// id của buổi công và ứng tiền — nên thêm id chỉ là thêm một chỗ để trùng.
/// </para>
/// </summary>
public sealed class GhiChuNgay
{
    public string ThoId { get; set; } = string.Empty;

    public string Ngay { get; set; } = string.Empty;

    /// <summary>Luôn khác chuỗi rỗng: bên kia xoá hết chữ là xoá luôn bản ghi.</summary>
    public string NoiDung { get; set; } = string.Empty;

    public string SuaLuc { get; set; } = string.Empty;
}

/// <summary>Một mốc tiền công: từ ngày này trở đi thợ được trả bằng này một công.</summary>
public sealed class MocLuong
{
    public string TuNgay { get; set; } = string.Empty;

    public decimal TienMotCong { get; set; }
}

/// <summary>Một người thợ. Tiền công là cả một dãy mốc vì lương có thể tăng theo thời gian.</summary>
public sealed class Tho
{
    public string Id { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string DienThoai { get; set; } = string.Empty;

    /// <summary>Các mốc tiền công, xếp theo <see cref="MocLuong.TuNgay"/> tăng dần.</summary>
    public List<MocLuong> MocLuong { get; set; } = new();

    public bool DangLam { get; set; } = true;

    public string GhiChu { get; set; } = string.Empty;

    public string NgayTao { get; set; } = string.Empty;

    public string SuaLuc { get; set; } = string.Empty;

    /// <summary>
    /// Tiền một công áp dụng cho một ngày: mốc gần nhất tính từ ngày ấy trở về trước. Ngày
    /// trước cả mốc đầu tiên thì lấy luôn mốc đầu — chấm bù một ngày cũ hơn ngày thêm thợ thì
    /// vẫn phải ra tiền, chứ để 0 là mất công của thợ.
    /// </summary>
    public decimal TienMotCongNgay(string ngay)
    {
        if (MocLuong.Count == 0)
        {
            return 0m;
        }

        var xep = MocLuong.OrderBy(m => m.TuNgay, StringComparer.Ordinal).ToList();
        var ap = xep[0];
        foreach (var moc in xep)
        {
            if (string.CompareOrdinal(moc.TuNgay, ngay) <= 0)
            {
                ap = moc;
            }
        }

        return ap.TienMotCong;
    }

    public override string ToString() => Ten;
}

/// <summary>Một buổi công đã chấm. Mỗi (thợ, ngày, buổi) chỉ có tối đa một bản ghi.</summary>
public sealed class BuoiCong
{
    public string Id { get; set; } = string.Empty;

    public string ThoId { get; set; } = string.Empty;

    public string Ngay { get; set; } = string.Empty;

    /// <summary>"Sang" hoặc "Chieu" — đúng chuỗi app điện thoại ghi.</summary>
    public string Buoi { get; set; } = string.Empty;

    /// <summary>Bình thường là 1. Về sớm 0,5; làm thêm 1,5.</summary>
    public decimal SoCong { get; set; }

    /// <summary>Giá riêng chỉ cho buổi này. Để trống thì tính theo mốc lương của thợ tại ngày đó.</summary>
    public decimal? TienMotCong { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    public string SuaLuc { get; set; } = string.Empty;

    /// <summary>"Sang" -> "Sáng", "Chieu" -> "Chiều"; chuỗi lạ thì giữ nguyên để nhìn là biết lạ.</summary>
    public string BuoiTiengViet => Buoi switch
    {
        "Sang" => "Sáng",
        "Chieu" => "Chiều",
        _ => Buoi,
    };
}

/// <summary>Một lần thợ ứng tiền trước, cuối kỳ trừ vào tiền công.</summary>
public sealed class UngTien
{
    public string Id { get; set; } = string.Empty;

    public string ThoId { get; set; } = string.Empty;

    public string Ngay { get; set; } = string.Empty;

    public decimal SoTien { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    public string SuaLuc { get; set; } = string.Empty;
}

/// <summary>Tiền nong của một thợ tại lúc chốt kỳ — bản chụp, không tính lại bao giờ nữa.</summary>
public sealed class DongQuyetToan
{
    public string ThoId { get; set; } = string.Empty;

    public string TenTho { get; set; } = string.Empty;

    public decimal CongSang { get; set; }

    public decimal CongChieu { get; set; }

    public decimal TongCong { get; set; }

    public decimal TienCong { get; set; }

    public decimal DaUng { get; set; }

    /// <summary>Tiền còn thiếu mang sang từ kỳ trước. Số âm là kỳ trước đã trả dư.</summary>
    public decimal NoKyTruoc { get; set; }

    public decimal PhaiTra { get; set; }

    public decimal DaTra { get; set; }

    /// <summary>Dương là còn nợ thợ, âm là thợ đã cầm dư.</summary>
    public decimal ChuyenKySau { get; set; }
}

/// <summary>
/// Một kỳ lương đã quyết toán. Kỳ ghi lại **những bản ghi nào đã được trả tiền** theo id, chứ
/// không cắt theo khoảng ngày — chấm bù một ngày của kỳ đã chốt thì buổi ấy chưa ai trả tiền,
/// phải rơi vào kỳ đang mở.
/// </summary>
public sealed class KyLuong
{
    public string Id { get; set; } = string.Empty;

    public string TuNgay { get; set; } = string.Empty;

    public string DenNgay { get; set; } = string.Empty;

    /// <summary>Lúc bấm quyết toán, dạng ISO.</summary>
    public string ChotLuc { get; set; } = string.Empty;

    public string GhiChu { get; set; } = string.Empty;

    public List<DongQuyetToan> Dongs { get; set; } = new();

    public List<string> BuoiCongIds { get; set; } = new();

    public List<string> UngTienIds { get; set; } = new();
}
