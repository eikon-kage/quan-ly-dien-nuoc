namespace ChamCong.Models;

/// <summary>Toàn bộ dữ liệu chấm công, được lưu thành một file JSON.</summary>
public sealed class DuLieuChamCong
{
    public List<Tho> Thos { get; set; } = new();

    public List<BuoiCong> BuoiCongs { get; set; } = new();

    public List<UngTien> UngTiens { get; set; } = new();
}
