using System.Text.Json.Serialization;

namespace QuanLyDienNuoc.Models;

/// <summary>
/// Hoá đơn mua hàng của một khách. Một hoá đơn kéo dài nhiều ngày:
/// mỗi lần khách lấy hàng thì thêm dòng vào <see cref="ChiTiet"/>.
/// <para>
/// Khách mang hàng trả về thì có hai đường: ghi thẳng một dòng số lượng âm vào hoá đơn đang
/// mở (trả lại ngay, chưa in hoá đơn), hoặc lập một hoá đơn hoàn hàng riêng
/// (<see cref="LoaiHoaDon.HoanHang"/>) hoàn cho hoá đơn đã in — có tờ chứng từ để đưa khách
/// mà không phải sửa vào hoá đơn cũ.
/// </para>
/// </summary>
public sealed class HoaDon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid KhachHangId { get; set; }

    public string MaHoaDon { get; set; } = string.Empty;

    /// <summary>Hoá đơn bán hàng hay hoá đơn hoàn hàng.</summary>
    public LoaiHoaDon Loai { get; set; } = LoaiHoaDon.Ban;

    /// <summary>
    /// Hoá đơn bán mà tờ hoàn hàng này hoàn cho. Chỉ hoá đơn hoàn hàng mới có; hoá đơn gốc
    /// bị xoá thì để nguyên id cũ, bản in chỉ mất dòng "hoàn cho hoá đơn ...".
    /// </summary>
    public Guid? HoaDonGocId { get; set; }

    public int Nam { get; set; } = DateTime.Today.Year;

    public DateTime NgayMo { get; set; } = DateTime.Today;

    /// <summary>Có giá trị khi hoá đơn đã chốt (không cho sửa nữa).</summary>
    public DateTime? NgayChot { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    public List<ChiTietHoaDon> ChiTiet { get; set; } = new();

    public List<ThanhToan> ThanhToans { get; set; } = new();

    [JsonIgnore]
    public decimal TongTien => ChiTiet.Sum(c => c.ThanhTien);

    [JsonIgnore]
    public decimal DaThanhToan => ThanhToans.Sum(t => t.SoTien);

    [JsonIgnore]
    public decimal ConLai => TongTien - DaThanhToan;

    [JsonIgnore]
    public bool DaChot => NgayChot.HasValue;

    [JsonIgnore]
    public bool LaHoanHang => Loai == LoaiHoaDon.HoanHang;

    /// <summary>Số tiền phải hoàn lại khách — số dương, để hiện lên màn hình và in ra giấy.</summary>
    [JsonIgnore]
    public decimal TienHoan => -TongTien;

    /// <summary>
    /// Dấu của số lượng và số tiền khi in ra giấy: tờ hoá đơn hoàn hàng ghi số dương, vì cả
    /// tờ giấy đã nói là hoàn rồi — in kèm dấu trừ nữa thì khách đọc thành hoàn của hoàn.
    /// Trong sổ thì vẫn là số âm để tự trừ vào nợ.
    /// </summary>
    [JsonIgnore]
    public int DauInRaGiay => LaHoanHang ? -1 : 1;
}
