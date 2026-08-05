namespace ChamCong.Models;

/// <summary>Một người thợ. Mỗi thợ có tiền công một buổi riêng.</summary>
public sealed class Tho
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Ten { get; set; } = string.Empty;

    public string DienThoai { get; set; } = string.Empty;

    /// <summary>
    /// Tiền công của một buổi (một công). Đây là giá đang áp dụng; các buổi đã chấm
    /// giữ lại giá của lúc chấm nên tăng lương không làm sai bảng lương tháng trước.
    /// </summary>
    public decimal TienMotCong { get; set; }

    /// <summary>Thợ đã nghỉ thì bỏ đánh dấu này, không hiện ra màn hình chấm công nữa.</summary>
    public bool DangLam { get; set; } = true;

    public string GhiChu { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.Today;

    /// <summary>Lần sửa gần nhất, dùng để đồng bộ giữa điện thoại và máy tính.</summary>
    public DateTime SuaLuc { get; set; } = DateTime.UtcNow;

    public override string ToString() => Ten;
}
