namespace QuanLyDienNuoc.Models;

/// <summary>Một lần khách trả tiền cho hoá đơn (hoá đơn kéo dài nhiều ngày nên trả nhiều lần).</summary>
public sealed class ThanhToan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Ngay { get; set; } = DateTime.Today;

    public decimal SoTien { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    /// <summary>
    /// Cùng một mã nghĩa là cùng một lần khách đưa tiền, được chia cho nhiều hoá đơn.
    /// Để trống là lần trả ghi thẳng vào một hoá đơn.
    /// </summary>
    public Guid? PhieuThuId { get; set; }
}
