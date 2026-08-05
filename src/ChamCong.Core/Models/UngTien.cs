namespace ChamCong.Models;

/// <summary>Một lần thợ ứng tiền trước, cuối kỳ trừ vào tiền công.</summary>
public sealed class UngTien
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ThoId { get; set; }

    public DateTime Ngay { get; set; } = DateTime.Today;

    public decimal SoTien { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    /// <summary>Lần sửa gần nhất, dùng để đồng bộ giữa điện thoại và máy tính.</summary>
    public DateTime SuaLuc { get; set; } = DateTime.UtcNow;
}
