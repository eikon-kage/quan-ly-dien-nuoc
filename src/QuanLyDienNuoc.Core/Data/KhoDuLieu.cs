using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.Data;

/// <summary>
/// Kho dữ liệu dùng chung của toàn ứng dụng. Dữ liệu nằm trong bộ nhớ và được ghi ra
/// một file JSON sau mỗi thay đổi. Mọi thay đổi nên đi qua <see cref="ThucHien"/> hoặc
/// <see cref="GhiNhan"/> để có thể Hoàn tác / Làm lại (Ctrl+Z / Ctrl+Y).
/// </summary>
public sealed class KhoDuLieu
{
    /// <summary>Số bước hoàn tác giữ trong một phiên làm việc.</summary>
    private const int SoBuocHoanTac = 50;

    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly List<BuocLichSu> _hoanTac = new();
    private readonly List<BuocLichSu> _lamLai = new();

    /// <summary>Kho dùng chung cho toàn ứng dụng, trỏ vào file dữ liệu thật của máy.</summary>
    public static KhoDuLieu Instance { get; } = new KhoDuLieu(DuongDanMacDinh());

    /// <summary>
    /// Tạo kho trỏ vào một file bất kỳ. Ứng dụng dùng <see cref="Instance"/>;
    /// hàm dựng này để test có thể trỏ vào thư mục tạm thay vì dữ liệu thật.
    /// </summary>
    public KhoDuLieu(string duongDanFile)
    {
        DuongDanFile = duongDanFile;
        NhatKy = new NhatKy(NhatKy.DuongDanBenCanh(duongDanFile));
        CaiDat = CaiDat.Doc(CaiDat.DuongDanBenCanh(duongDanFile));

        var thuMuc = Path.GetDirectoryName(duongDanFile);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }
    }

    private static string DuongDanMacDinh()
    {
        // Cho phép trỏ dữ liệu sang chỗ khác, dùng khi chụp ảnh giao diện trên máy dựng tự động.
        var chiDinh = Environment.GetEnvironmentVariable("QLDN_FILE_DULIEU");
        if (!string.IsNullOrWhiteSpace(chiDinh))
        {
            return chiDinh;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuanLyDienNuoc",
            "dulieu.json");
    }

    /// <summary>Phát ra sau khi dữ liệu đổi theo cách mà màn hình phải nạp lại (gồm cả hoàn tác).</summary>
    public event EventHandler? DuLieuThayDoi;

    public string DuongDanFile { get; }

    /// <summary>Nhật ký thay đổi, ghi ra file riêng nên hoàn tác không xoá mất.</summary>
    public NhatKy NhatKy { get; }

    /// <summary>Cài đặt (nhắc nợ, sao lưu, cảnh báo nhập sai), lưu ở file riêng.</summary>
    public CaiDat CaiDat { get; private set; }

    public DuLieuApp DuLieu { get; private set; } = new();

    public bool CoTheHoanTac => _hoanTac.Count > 0;

    public bool CoTheLamLai => _lamLai.Count > 0;

    public string MoTaHoanTac => _hoanTac.Count > 0 ? _hoanTac[^1].MoTa : string.Empty;

    public string MoTaLamLai => _lamLai.Count > 0 ? _lamLai[^1].MoTa : string.Empty;

    // ---------- Nạp / lưu ----------

    public void Nap()
    {
        CaiDat = CaiDat.Doc(CaiDat.DuongDanBenCanh(DuongDanFile));

        if (!File.Exists(DuongDanFile))
        {
            DuLieu = new DuLieuApp();
            TaoDanhMucMau();
            Luu();
            return;
        }

        var json = File.ReadAllText(DuongDanFile, Encoding.UTF8);
        DuLieu = JsonSerializer.Deserialize<DuLieuApp>(json, TuyChonJson) ?? new DuLieuApp();
    }

    public void LuuCaiDat() => CaiDat.Luu(CaiDat.DuongDanBenCanh(DuongDanFile));

    /// <summary>Báo cho các màn hình đang mở nạp lại, dùng sau khi khôi phục từ bản sao lưu.</summary>
    public void BaoDuLieuThayDoi() => DuLieuThayDoi?.Invoke(this, EventArgs.Empty);

    public void Luu()
    {
        var json = JsonSerializer.Serialize(DuLieu, TuyChonJson);
        var fileTam = DuongDanFile + ".tmp";
        File.WriteAllText(fileTam, json, new UTF8Encoding(false));

        // Giữ lại một bản sao của lần lưu trước để phòng hỏng file.
        if (File.Exists(DuongDanFile))
        {
            File.Copy(DuongDanFile, DuongDanFile + ".bak", overwrite: true);
        }

        File.Move(fileTam, DuongDanFile, overwrite: true);
    }

    // ---------- Lịch sử hoàn tác (chỉ tồn tại trong phiên đang mở) ----------

    /// <summary>Chụp lại trạng thái hiện tại để có thể quay về sau này.</summary>
    public string ChupNhanh() => JsonSerializer.Serialize(DuLieu, TuyChonJson);

    /// <summary>Chạy một thay đổi và ghi vào lịch sử hoàn tác.</summary>
    public void ThucHien(string moTa, Action thayDoi, bool phatSuKien = true)
    {
        var truoc = ChupNhanh();
        thayDoi();
        GhiNhan(truoc, moTa, phatSuKien);
    }

    /// <summary>
    /// Ghi nhận một thay đổi đã xảy ra, với <paramref name="truoc"/> là ảnh chụp lấy
    /// trước khi sửa (dùng cho việc sửa trực tiếp trên lưới).
    /// </summary>
    public void GhiNhan(string truoc, string moTa, bool phatSuKien = true)
    {
        _hoanTac.Add(new BuocLichSu(truoc, moTa));
        if (_hoanTac.Count > SoBuocHoanTac)
        {
            _hoanTac.RemoveAt(0);
        }

        _lamLai.Clear();
        Luu();
        NhatKy.Ghi(moTa);

        if (phatSuKien)
        {
            DuLieuThayDoi?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Quay lại trạng thái trước thao tác gần nhất. Trả về mô tả thao tác vừa bỏ.</summary>
    public string? HoanTac()
    {
        if (_hoanTac.Count == 0)
        {
            return null;
        }

        var buoc = _hoanTac[^1];
        _hoanTac.RemoveAt(_hoanTac.Count - 1);
        _lamLai.Add(new BuocLichSu(ChupNhanh(), buoc.MoTa));

        KhoiPhuc(buoc.AnhChup);
        NhatKy.Ghi("Hoàn tác", buoc.MoTa);
        return buoc.MoTa;
    }

    /// <summary>Làm lại thao tác vừa hoàn tác. Trả về mô tả thao tác được làm lại.</summary>
    public string? LamLai()
    {
        if (_lamLai.Count == 0)
        {
            return null;
        }

        var buoc = _lamLai[^1];
        _lamLai.RemoveAt(_lamLai.Count - 1);
        _hoanTac.Add(new BuocLichSu(ChupNhanh(), buoc.MoTa));

        KhoiPhuc(buoc.AnhChup);
        NhatKy.Ghi("Làm lại", buoc.MoTa);
        return buoc.MoTa;
    }

    private void KhoiPhuc(string anhChup)
    {
        DuLieu = JsonSerializer.Deserialize<DuLieuApp>(anhChup, TuyChonJson) ?? new DuLieuApp();
        Luu();
        DuLieuThayDoi?.Invoke(this, EventArgs.Empty);
    }

    // ---------- Truy vấn ----------

    public IReadOnlyList<int> DanhSachNam()
    {
        var nams = DuLieu.HoaDons.Select(h => h.Nam).ToHashSet();
        nams.Add(DateTime.Today.Year);
        return nams.OrderByDescending(n => n).ToList();
    }

    public KhachHang? TimKhach(Guid id) => DuLieu.KhachHangs.FirstOrDefault(k => k.Id == id);

    public HoaDon? TimHoaDon(Guid id) => DuLieu.HoaDons.FirstOrDefault(h => h.Id == id);

    public VatTu? TimVatTu(Guid id) => DuLieu.VatTus.FirstOrDefault(v => v.Id == id);

    public VatTu? TimVatTuTheoTen(string ten) => DuLieu.VatTus
        .FirstOrDefault(v => string.Equals(v.Ten, ten.Trim(), StringComparison.CurrentCultureIgnoreCase));

    /// <summary>Mọi hoá đơn của khách, tính cả các năm trước — dùng để tra giá lần trước và nhắc nợ.</summary>
    public List<HoaDon> HoaDonCuaKhach(Guid khachId) => DuLieu.HoaDons
        .Where(h => h.KhachHangId == khachId)
        .OrderByDescending(h => h.NgayMo)
        .ToList();

    public List<HoaDon> HoaDonCuaKhach(Guid khachId, int nam) => DuLieu.HoaDons
        .Where(h => h.KhachHangId == khachId && h.Nam == nam)
        .OrderByDescending(h => h.NgayMo)
        .ToList();

    /// <summary>Giá áp cho khách này: ưu tiên giá riêng, không có thì lấy giá mặc định.</summary>
    public decimal GiaCho(KhachHang khach, VatTu vatTu) =>
        khach.BangGiaRieng.TryGetValue(vatTu.Id, out var gia) && gia > 0 ? gia : vatTu.DonGiaMacDinh;

    public string TaoMaHoaDon(Guid khachId, int nam)
    {
        var soDaCo = DuLieu.HoaDons.Count(h => h.KhachHangId == khachId && h.Nam == nam);
        return $"HD{nam}-{soDaCo + 1:00}";
    }

    private void TaoDanhMucMau()
    {
        void Them(string ten, string donVi, decimal gia) =>
            DuLieu.VatTus.Add(new VatTu { Ten = ten, DonVi = donVi, DonGiaMacDinh = gia });

        Them("Ống nhựa PVC D21", "Cây", 32000);
        Them("Ống nhựa PVC D27", "Cây", 45000);
        Them("Ống nhựa PVC D34", "Cây", 62000);
        Them("Co nối PVC D21", "Cái", 4000);
        Them("Tê PVC D21", "Cái", 5000);
        Them("Keo dán ống 100g", "Lọ", 25000);
        Them("Van khoá nước D21", "Cái", 55000);
        Them("Vòi rửa inox", "Cái", 250000);
        Them("Dây điện Cadivi 2x1.5", "Mét", 12000);
        Them("Dây điện Cadivi 2x2.5", "Mét", 18000);
        Them("Ống ruột gà D20", "Mét", 6000);
        Them("Ổ cắm đôi 3 chấu", "Cái", 65000);
        Them("Công tắc đơn", "Cái", 32000);
        Them("Aptomat 1 pha 20A", "Cái", 95000);
        Them("Bóng đèn LED bulb 9W", "Bóng", 45000);
        Them("Máng đèn LED 1m2", "Bộ", 130000);
    }

    private sealed record BuocLichSu(string AnhChup, string MoTa);
}
