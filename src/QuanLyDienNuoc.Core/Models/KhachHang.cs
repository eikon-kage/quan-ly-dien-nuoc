namespace QuanLyDienNuoc.Models;

/// <summary>Khách hàng của cửa hàng. Mỗi khách có bảng giá riêng cho từng vật tư.</summary>
public sealed class KhachHang
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Ten { get; set; } = string.Empty;

    public string DienThoai { get; set; } = string.Empty;

    public string DiaChi { get; set; } = string.Empty;

    public string GhiChu { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.Today;

    /// <summary>Giá riêng của khách này theo từng vật tư: VatTu.Id -> đơn giá.</summary>
    public Dictionary<Guid, decimal> BangGiaRieng { get; set; } = new();

    public override string ToString() => Ten;
}
