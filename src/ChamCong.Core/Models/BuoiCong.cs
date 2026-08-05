namespace ChamCong.Models;

/// <summary>
/// Một buổi công đã chấm cho một thợ. Mỗi (thợ, ngày, buổi) chỉ có tối đa một bản ghi.
/// </summary>
public sealed class BuoiCong
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ThoId { get; set; }

    /// <summary>Ngày làm, chỉ lấy phần ngày (giờ luôn là 00:00).</summary>
    public DateTime Ngay { get; set; } = DateTime.Today;

    public BuoiLam Buoi { get; set; }

    /// <summary>
    /// Số công của buổi này, bình thường là 1. Về sớm thì ghi 0.5, làm thêm thì ghi 1.5.
    /// </summary>
    public decimal SoCong { get; set; } = 1m;

    /// <summary>
    /// Tiền một công của lúc chấm buổi này. Để trống thì tính theo giá hiện tại của thợ.
    /// Nhờ vậy tăng lương thợ không làm đổi bảng lương của những tháng đã qua.
    /// </summary>
    public decimal? TienMotCong { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    /// <summary>Lần sửa gần nhất, dùng để đồng bộ giữa điện thoại và máy tính.</summary>
    public DateTime SuaLuc { get; set; } = DateTime.UtcNow;
}
