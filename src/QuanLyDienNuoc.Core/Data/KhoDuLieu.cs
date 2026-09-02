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

    /// <summary>Trạng thái file lúc đọc/ghi lần cuối, để biết máy khác có sửa file hay không.</summary>
    private (DateTime Luc, long KichThuoc)? _dauVetFile;

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

    /// <summary>Phát ra khi người dùng định sửa gì đó trong lúc đang mở ở chế độ chỉ xem.</summary>
    public event EventHandler? ThaoTacBiChan;

    public string DuongDanFile { get; }

    /// <summary>
    /// Đang mở ở chế độ chỉ xem (máy khác đang giữ file): mọi thay đổi bị chặn và không ghi file.
    /// </summary>
    public bool ChiXem { get; private set; }

    /// <summary>Vì sao đang chỉ xem, để màn hình nói lại cho người dùng.</summary>
    public string LyDoChiXem { get; private set; } = string.Empty;

    /// <summary>
    /// Hỏi khi sắp ghi mà file đã bị máy khác sửa. Trả về true là ghi đè bằng dữ liệu đang mở
    /// (bản của máy kia được cất lại trước), false là bỏ thay đổi và nạp lại file.
    /// Không gán thì <see cref="Luu"/> ném <see cref="XungDotDuLieuException"/>.
    /// </summary>
    public Func<XungDotFile, bool>? HoiKhiFileBiMayKhacSua { get; set; }

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
            DanhMucMau.BoSung(DuLieu);
            Luu();
            return;
        }

        var json = File.ReadAllText(DuongDanFile, Encoding.UTF8);
        DuLieu = JsonSerializer.Deserialize<DuLieuApp>(json, TuyChonJson) ?? new DuLieuApp();
        GhiNhoDauVetFile();
    }

    /// <summary>Bỏ những gì đang có trong bộ nhớ và đọc lại file — dùng khi máy khác vừa sửa file.</summary>
    public void NapLaiTuFile()
    {
        _hoanTac.Clear();
        _lamLai.Clear();
        Nap();
        DuLieuThayDoi?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Chuyển sang chế độ chỉ xem: không cho sửa, không ghi file.</summary>
    public void BatChiXem(string lyDo)
    {
        ChiXem = true;
        LyDoChiXem = lyDo;
    }

    public void LuuCaiDat() => CaiDat.Luu(CaiDat.DuongDanBenCanh(DuongDanFile));

    /// <summary>Báo cho các màn hình đang mở nạp lại, dùng sau khi khôi phục từ bản sao lưu.</summary>
    public void BaoDuLieuThayDoi() => DuLieuThayDoi?.Invoke(this, EventArgs.Empty);

    /// <summary>File trên đĩa đã khác lần mình đọc/ghi gần nhất — tức là máy khác vừa sửa.</summary>
    public bool FileBiMayKhacSua()
    {
        if (_dauVetFile is not { } dauVet)
        {
            return false;
        }

        var thongTin = new FileInfo(DuongDanFile);
        return thongTin.Exists
               && (thongTin.LastWriteTimeUtc != dauVet.Luc || thongTin.Length != dauVet.KichThuoc);
    }

    /// <summary>Ghi dữ liệu ra file. Trả về false khi không ghi (chỉ xem, hoặc bỏ vì máy khác đã sửa).</summary>
    public bool Luu()
    {
        if (ChiXem)
        {
            return false;
        }

        if (FileBiMayKhacSua())
        {
            var xungDot = new XungDotFile(
                DuongDanFile,
                new FileInfo(DuongDanFile).LastWriteTime,
                DuongDanFile + $".maykhac-{DateTime.Now:yyyy-MM-dd-HHmmss}.json");

            if (HoiKhiFileBiMayKhacSua is not { } hoi)
            {
                throw new XungDotDuLieuException(xungDot);
            }

            if (!hoi(xungDot))
            {
                NapLaiTuFile();
                return false;
            }

            // Ghi đè thì cất bản của máy kia lại, mất công cả ngày của người ta thì không lấy lại được.
            CatBanCuaMayKhac(xungDot.DuongDanCatBanMayKhac);
        }

        GhiRaFile();
        return true;
    }

    private void GhiRaFile()
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
        GhiNhoDauVetFile();
    }

    private void CatBanCuaMayKhac(string duongDan)
    {
        try
        {
            File.Copy(DuongDanFile, duongDan, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Không cất được thì vẫn cho ghi tiếp: bản .bak cạnh file dữ liệu còn giữ được một bước.
        }
    }

    private void GhiNhoDauVetFile()
    {
        var thongTin = new FileInfo(DuongDanFile);
        _dauVetFile = thongTin.Exists ? (thongTin.LastWriteTimeUtc, thongTin.Length) : null;
    }

    private void BaoThaoTacBiChan() => ThaoTacBiChan?.Invoke(this, EventArgs.Empty);

    // ---------- Lịch sử hoàn tác (chỉ tồn tại trong phiên đang mở) ----------

    /// <summary>Chụp lại trạng thái hiện tại để có thể quay về sau này.</summary>
    public string ChupNhanh() => JsonSerializer.Serialize(DuLieu, TuyChonJson);

    /// <summary>
    /// Bản sao rời của dữ liệu hiện tại — để thử trước một thay đổi (xem nó thêm bao nhiêu hàng)
    /// mà không đụng gì vào sổ thật.
    /// </summary>
    public DuLieuApp ChupNhanhDuLieu() =>
        JsonSerializer.Deserialize<DuLieuApp>(ChupNhanh(), TuyChonJson) ?? new DuLieuApp();

    /// <summary>Chạy một thay đổi và ghi vào lịch sử hoàn tác.</summary>
    public void ThucHien(string moTa, Action thayDoi, bool phatSuKien = true)
    {
        if (ChiXem)
        {
            BaoThaoTacBiChan();
            return;
        }

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
        if (ChiXem)
        {
            // Sửa thẳng trên lưới thì thay đổi đã nằm trong bộ nhớ rồi, phải trả lại như cũ.
            KhoiPhuc(truoc);
            BaoThaoTacBiChan();
            return;
        }

        _hoanTac.Add(new BuocLichSu(truoc, moTa));
        if (_hoanTac.Count > SoBuocHoanTac)
        {
            _hoanTac.RemoveAt(0);
        }

        _lamLai.Clear();

        // Máy khác vừa sửa file và người dùng chọn bỏ thay đổi: dữ liệu đã nạp lại, đừng ghi nhật ký.
        if (!Luu())
        {
            return;
        }

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

    public NhomHang? TimNhom(Guid? id) => id is { } ma ? DuLieu.NhomHangs.FirstOrDefault(n => n.Id == ma) : null;

    public NhomHang? TimNhomTheoTen(string ten) => DuLieu.NhomHangs
        .FirstOrDefault(n => string.Equals(n.Ten, ten.Trim(), StringComparison.CurrentCultureIgnoreCase));

    /// <summary>Tên nhóm của mặt hàng, chuỗi rỗng nếu hàng chưa đặt nhóm hoặc nhóm đã bị xoá.</summary>
    public string TenNhom(VatTu vatTu) => TimNhom(vatTu.NhomId)?.Ten ?? string.Empty;

    /// <summary>Các nhóm hàng, xếp theo tên — thứ tự chung của mọi ô chọn nhóm.</summary>
    public List<NhomHang> NhomTheoTen() => DuLieu.NhomHangs
        .OrderBy(n => n.Ten, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

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

    /// <summary>
    /// Mã hoá đơn kế tiếp của khách trong năm: "HD2026-03" cho hoá đơn bán hàng, "HH2026-01"
    /// cho hoá đơn hoàn hàng. Hai loại đánh số riêng để nhìn mã là biết ngay tờ nào là tờ nào,
    /// và lập tờ hoàn cũng không làm nhảy số hoá đơn bán.
    /// </summary>
    public string TaoMaHoaDon(Guid khachId, int nam, LoaiHoaDon loai = LoaiHoaDon.Ban)
    {
        var soDaCo = DuLieu.HoaDons.Count(h => h.KhachHangId == khachId && h.Nam == nam && h.Loai == loai);
        return $"{(loai == LoaiHoaDon.HoanHang ? "HH" : "HD")}{nam}-{soDaCo + 1:00}";
    }

    private sealed record BuocLichSu(string AnhChup, string MoTa);
}
