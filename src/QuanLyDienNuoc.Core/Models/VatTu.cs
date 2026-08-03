namespace QuanLyDienNuoc.Models;

/// <summary>Một mặt hàng / nguyên vật liệu trong danh mục của cửa hàng.</summary>
public sealed class VatTu
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Ten { get; set; } = string.Empty;

    public string DonVi { get; set; } = string.Empty;

    /// <summary>Gõ tắt do cửa hàng tự đặt: gõ "o27" ở ô tên hàng là ra "Ống nhựa PVC D27".</summary>
    public string MaTat { get; set; } = string.Empty;

    /// <summary>Giá dùng khi khách chưa có giá riêng.</summary>
    public decimal DonGiaMacDinh { get; set; }

    public override string ToString() => Ten;
}
