namespace QuanLyDienNuoc.Models;

/// <summary>Một món trong bộ hàng thường dùng.</summary>
public sealed class DongBoHang
{
    /// <summary>Vật tư trong danh mục (nếu có) — để lấy đúng giá của khách khi thêm.</summary>
    public Guid? VatTuId { get; set; }

    public string TenHang { get; set; } = string.Empty;

    public string DonVi { get; set; } = string.Empty;

    public decimal SoLuong { get; set; } = 1m;
}

/// <summary>
/// Một bộ hàng thường dùng, ví dụ "Bộ lắp bồn nước" gồm 6 món. Chọn một lần là ra đủ
/// các dòng, khỏi phải nhập lại từng món cho mỗi công trình.
/// </summary>
public sealed class BoHang
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Ten { get; set; } = string.Empty;

    public string GhiChu { get; set; } = string.Empty;

    public List<DongBoHang> Dong { get; set; } = new();

    public override string ToString() => Ten;
}
