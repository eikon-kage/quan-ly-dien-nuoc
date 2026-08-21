using System.Text.Json.Serialization;

namespace QuanLyDienNuoc.Models;

/// <summary>
/// Hoá đơn bán hàng, hay hoá đơn hoàn hàng — tờ chứng từ riêng ghi số hàng khách mang trả về.
/// Ghi ra file dữ liệu bằng chữ ("Ban", "HoanHang") chứ không phải số 0/1: mở file JSON ra
/// đọc bằng mắt là hiểu ngay, mà sau này thêm loại mới cũng không xô lệch số cũ.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoaiHoaDon
{
    /// <summary>Hoá đơn bán hàng thường — khách lấy hàng, ghi nợ hoặc trả tiền.</summary>
    Ban,

    /// <summary>
    /// Hoá đơn hoàn hàng: hoàn cho một hoá đơn bán. Các dòng hàng ghi số lượng âm nên tổng
    /// tiền âm, tự trừ vào nợ của khách; in ra giấy thì đổi lại thành số dương.
    /// </summary>
    HoanHang,
}
