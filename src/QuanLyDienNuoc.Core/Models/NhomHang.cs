namespace QuanLyDienNuoc.Models;

/// <summary>
/// Một nhóm hàng của cửa hàng: "Ống nước", "Điện", "Đèn"... Nhóm là bản ghi riêng có mã,
/// nên đổi tên nhóm là mọi mặt hàng trong nhóm đổi theo, không sợ lệch tên.
/// </summary>
public sealed class NhomHang
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Ten { get; set; } = string.Empty;

    public override string ToString() => Ten;
}
