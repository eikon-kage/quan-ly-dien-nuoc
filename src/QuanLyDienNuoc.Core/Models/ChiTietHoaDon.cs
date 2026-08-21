using System.Text.Json.Serialization;

namespace QuanLyDienNuoc.Models;

/// <summary>Một dòng hàng đã lấy trong hoá đơn, gắn với ngày lấy hàng cụ thể.</summary>
public sealed class ChiTietHoaDon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Ngay { get; set; } = DateTime.Today;

    /// <summary>Vật tư trong danh mục (nếu có) — dùng để tra bảng giá riêng của khách.</summary>
    public Guid? VatTuId { get; set; }

    /// <summary>
    /// Dòng của hoá đơn bán mà dòng này hoàn lại. Chỉ các dòng trong hoá đơn hoàn hàng mới
    /// có, để biết mỗi món đã hoàn bao nhiêu rồi mà không cho hoàn quá số khách đã lấy.
    /// </summary>
    public Guid? DongGocId { get; set; }

    public string TenHang { get; set; } = string.Empty;

    public string DonVi { get; set; } = string.Empty;

    public decimal DonGia { get; set; }

    public decimal SoLuong { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    /// <summary>Dòng khách trả lại hàng: số lượng âm nên thành tiền trừ đi khỏi hoá đơn.</summary>
    [JsonIgnore]
    public bool LaTraLai => SoLuong < 0m;

    [JsonIgnore]
    public decimal ThanhTien => Math.Round(DonGia * SoLuong, 0, MidpointRounding.AwayFromZero);
}
