namespace QuanLyDienNuoc.Models;

/// <summary>Toàn bộ dữ liệu của phần mềm, được lưu thành một file JSON.</summary>
public sealed class DuLieuApp
{
    public List<KhachHang> KhachHangs { get; set; } = new();

    public List<VatTu> VatTus { get; set; } = new();

    public List<HoaDon> HoaDons { get; set; } = new();
}
