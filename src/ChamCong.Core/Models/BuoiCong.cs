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
    /// Số công của buổi này, bình thường là <see cref="CongMotBuoi"/>. Về sớm thì ghi 0.25,
    /// làm thêm thì ghi 0.75.
    /// </summary>
    public decimal SoCong { get; set; } = CongMotBuoi;

    /// <summary>
    /// Một buổi đi làm đầy đủ đáng bằng này công — <b>một ngày đi đủ cả sáng lẫn chiều là
    /// một công</b>, không phải hai.
    /// <para>
    /// Đó là cách cả nghề nói và cũng là cách tiền được tính: <see cref="Tho.TienMotCong"/>
    /// là tiền của một <i>ngày</i> công, nên đếm mỗi buổi một công thì cuối kỳ thợ nào cũng
    /// thành tiền gấp đôi. Sổ vẫn ghi theo <i>buổi</i> vì buổi mới là thứ được chấm — chỉ có
    /// giá trị của một buổi là nửa công.
    /// </para>
    /// </summary>
    public const decimal CongMotBuoi = 0.5m;

    /// <summary>
    /// Tiền một công của lúc chấm buổi này. Để trống thì tính theo giá hiện tại của thợ.
    /// Nhờ vậy tăng lương thợ không làm đổi bảng lương của những tháng đã qua.
    /// </summary>
    public decimal? TienMotCong { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    /// <summary>Lần sửa gần nhất, dùng để đồng bộ giữa điện thoại và máy tính.</summary>
    public DateTime SuaLuc { get; set; } = DateTime.UtcNow;
}
