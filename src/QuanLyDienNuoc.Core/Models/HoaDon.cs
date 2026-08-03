using System.Text.Json.Serialization;

namespace QuanLyDienNuoc.Models;

/// <summary>
/// Hoá đơn mua hàng của một khách. Một hoá đơn kéo dài nhiều ngày:
/// mỗi lần khách lấy hàng thì thêm dòng vào <see cref="ChiTiet"/>.
/// </summary>
public sealed class HoaDon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid KhachHangId { get; set; }

    public string MaHoaDon { get; set; } = string.Empty;

    public int Nam { get; set; } = DateTime.Today.Year;

    public DateTime NgayMo { get; set; } = DateTime.Today;

    /// <summary>Có giá trị khi hoá đơn đã chốt (không cho sửa nữa).</summary>
    public DateTime? NgayChot { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    public List<ChiTietHoaDon> ChiTiet { get; set; } = new();

    public List<ThanhToan> ThanhToans { get; set; } = new();

    [JsonIgnore]
    public decimal TongTien => ChiTiet.Sum(c => c.ThanhTien);

    [JsonIgnore]
    public decimal DaThanhToan => ThanhToans.Sum(t => t.SoTien);

    [JsonIgnore]
    public decimal ConLai => TongTien - DaThanhToan;

    [JsonIgnore]
    public bool DaChot => NgayChot.HasValue;
}
