namespace QuanLyDienNuoc.Models;

/// <summary>Một lần khách trả tiền cho hoá đơn (hoá đơn kéo dài nhiều ngày nên trả nhiều lần).</summary>
public sealed class ThanhToan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Ngay { get; set; } = DateTime.Today;

    public decimal SoTien { get; set; }

    public string GhiChu { get; set; } = string.Empty;
}
