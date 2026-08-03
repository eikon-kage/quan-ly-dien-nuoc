using System.Globalization;
using QuanLyDienNuoc.Excel;

namespace QuanLyDienNuoc.Data;

/// <summary>Một bản sao lưu đã có trên đĩa.</summary>
public sealed record BanSaoLuu(string DuongDanJson, DateTime Luc, long KichThuoc, string? DuongDanExcel)
{
    public string TenHienThi => Luc.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public bool CoExcel => DuongDanExcel is not null && File.Exists(DuongDanExcel);
}

/// <summary>
/// Sao lưu dữ liệu ra thư mục riêng: mỗi bản gồm một file JSON (để khôi phục lại vào phần mềm)
/// và một file Excel nhiều trang (để mở xem, in, gửi đi mà không cần phần mềm này).
/// Mất máy hay hỏng file thì chép thư mục sao lưu về là chạy lại được.
/// </summary>
public static class SaoLuu
{
    private const string TienTo = "sao-luu-";
    private const string MauNgay = "yyyy-MM-dd-HHmm";

    /// <summary>Tạo một bản sao lưu mới rồi dọn bớt các bản cũ. Trả về bản vừa tạo.</summary>
    public static BanSaoLuu Tao(KhoDuLieu kho, CaiDat caiDat, DateTime? luc = null)
    {
        var thoiDiem = luc ?? DateTime.Now;
        var thuMuc = caiDat.ThuMucSaoLuuThat(kho.DuongDanFile);
        Directory.CreateDirectory(thuMuc);

        // Ghi lại dữ liệu đang có trong bộ nhớ để bản sao lưu chắc chắn là bản mới nhất.
        kho.Luu();

        var ten = TienTo + thoiDiem.ToString(MauNgay, CultureInfo.InvariantCulture);
        var fileJson = Path.Combine(thuMuc, ten + ".json");
        File.Copy(kho.DuongDanFile, fileJson, overwrite: true);

        string? fileExcel = null;
        if (caiDat.SaoLuuKemExcel)
        {
            fileExcel = Path.Combine(thuMuc, ten + ".xlsx");
            XuatToanBo.Xuat(kho.DuLieu, fileExcel, thoiDiem.Date);
        }

        DonBanCu(thuMuc, caiDat.SoBanSaoLuuGiuLai);

        caiDat.LanSaoLuuCuoi = thoiDiem;
        caiDat.Luu(CaiDat.DuongDanBenCanh(kho.DuongDanFile));

        return new BanSaoLuu(fileJson, thoiDiem, new FileInfo(fileJson).Length, fileExcel);
    }

    /// <summary>Sao lưu tự động: chỉ chạy khi đang bật và hôm nay chưa sao lưu lần nào.</summary>
    public static BanSaoLuu? TuDongNeuCan(KhoDuLieu kho, CaiDat caiDat, DateTime? homNay = null)
    {
        var bayGio = homNay ?? DateTime.Now;
        if (!caiDat.TuDongSaoLuu)
        {
            return null;
        }

        if (caiDat.LanSaoLuuCuoi is { } lanCuoi && lanCuoi.Date >= bayGio.Date)
        {
            return null;
        }

        return Tao(kho, caiDat, bayGio);
    }

    /// <summary>Các bản sao lưu đang có, bản mới nhất đứng đầu.</summary>
    public static List<BanSaoLuu> DanhSach(string thuMuc)
    {
        if (!Directory.Exists(thuMuc))
        {
            return new List<BanSaoLuu>();
        }

        var ketQua = new List<BanSaoLuu>();
        foreach (var file in Directory.EnumerateFiles(thuMuc, TienTo + "*.json"))
        {
            var thongTin = new FileInfo(file);
            var luc = DocThoiDiem(thongTin.Name) ?? thongTin.LastWriteTime;
            var excel = Path.ChangeExtension(file, ".xlsx");
            ketQua.Add(new BanSaoLuu(file, luc, thongTin.Length, File.Exists(excel) ? excel : null));
        }

        return ketQua.OrderByDescending(b => b.Luc).ToList();
    }

    /// <summary>
    /// Chép một bản sao lưu đè lên dữ liệu đang dùng rồi nạp lại.
    /// Bản đang dùng được cất trước khi đè, phòng khi chọn nhầm.
    /// </summary>
    public static void KhoiPhuc(KhoDuLieu kho, CaiDat caiDat, BanSaoLuu ban, DateTime? luc = null)
    {
        if (!File.Exists(ban.DuongDanJson))
        {
            throw new FileNotFoundException($"Không thấy file sao lưu:\n{ban.DuongDanJson}", ban.DuongDanJson);
        }

        var thoiDiem = luc ?? DateTime.Now;
        if (File.Exists(kho.DuongDanFile))
        {
            var thuMuc = caiDat.ThuMucSaoLuuThat(kho.DuongDanFile);
            Directory.CreateDirectory(thuMuc);
            var truocKhiKhoiPhuc = Path.Combine(
                thuMuc,
                "truoc-khi-khoi-phuc-" + thoiDiem.ToString(MauNgay, CultureInfo.InvariantCulture) + ".json");
            File.Copy(kho.DuongDanFile, truocKhiKhoiPhuc, overwrite: true);
        }

        File.Copy(ban.DuongDanJson, kho.DuongDanFile, overwrite: true);
        kho.Nap();
        kho.BaoDuLieuThayDoi();
    }

    private static void DonBanCu(string thuMuc, int giuLai)
    {
        if (giuLai <= 0)
        {
            return;
        }

        foreach (var ban in DanhSach(thuMuc).Skip(giuLai))
        {
            Xoa(ban.DuongDanJson);
            if (ban.DuongDanExcel is { } excel)
            {
                Xoa(excel);
            }
        }
    }

    private static void Xoa(string duongDan)
    {
        try
        {
            File.Delete(duongDan);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Bản cũ đang bị mở thì để lại, lần sau dọn tiếp.
        }
    }

    private static DateTime? DocThoiDiem(string tenFile)
    {
        var ten = Path.GetFileNameWithoutExtension(tenFile);
        if (!ten.StartsWith(TienTo, StringComparison.Ordinal))
        {
            return null;
        }

        var phan = ten[TienTo.Length..];
        return DateTime.TryParseExact(phan, MauNgay, CultureInfo.InvariantCulture, DateTimeStyles.None, out var luc)
            ? luc
            : null;
    }
}
