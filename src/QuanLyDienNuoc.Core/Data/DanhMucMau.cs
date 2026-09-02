using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Data;

/// <summary>
/// Danh mục vật tư điện nước dựng sẵn: các mặt hàng và hãng phổ biến ở Việt Nam, kèm đơn vị,
/// mã tắt và một mức giá tham khảo. Máy mới chưa có dữ liệu thì <see cref="KhoDuLieu.Nap"/>
/// điền cả danh mục này vào cho có cái mà nhập hàng ngay; cửa hàng đang dùng rồi thì bấm
/// "Điền danh mục mẫu" ở màn Danh mục vật tư để <see cref="BoSung"/> thêm phần còn thiếu.
///
/// Giá ở đây chỉ là giá tham khảo để đỡ phải gõ từ 0, cửa hàng sửa lại theo giá thật của mình.
/// </summary>
public static class DanhMucMau
{
    /// <summary>Một dòng của danh mục dựng sẵn.</summary>
    private sealed record Hang(string Nhom, string Ten, string DonVi, string MaTat, decimal Gia);

    private const string OngNuoc = "Ống nước";
    private const string PhuKienOng = "Phụ kiện ống nước";
    private const string VanNuoc = "Van & đồng hồ nước";
    private const string VeSinh = "Thiết bị vệ sinh";
    private const string DayDien = "Dây & cáp điện";
    private const string Dien = "Điện";
    private const string OngLuon = "Ống luồn & phụ kiện điện";
    private const string Den = "Đèn";
    private const string MayBom = "Máy nước nóng & bơm";
    private const string BonNuoc = "Bồn nước";
    private const string VatTuPhu = "Vật tư phụ";

    private static readonly Hang[] DanhSach =
    {
        // ---------- Ống nước ----------
        new(OngNuoc, "Ống nhựa PVC D21", "Cây", "o21", 32_000),
        new(OngNuoc, "Ống nhựa PVC D27", "Cây", "o27", 45_000),
        new(OngNuoc, "Ống nhựa PVC D34", "Cây", "o34", 62_000),
        new(OngNuoc, "Ống nhựa PVC D42", "Cây", "o42", 85_000),
        new(OngNuoc, "Ống nhựa PVC D48", "Cây", "o48", 105_000),
        new(OngNuoc, "Ống nhựa PVC D60", "Cây", "o60", 150_000),
        new(OngNuoc, "Ống nhựa PVC D90 thoát nước", "Cây", "o90", 235_000),
        new(OngNuoc, "Ống nhựa PVC D110 thoát nước", "Cây", "o110", 320_000),
        new(OngNuoc, "Ống PPR Vesbo D20", "Cây", "ppr20", 95_000),
        new(OngNuoc, "Ống PPR Vesbo D25", "Cây", "ppr25", 135_000),
        new(OngNuoc, "Ống PPR Dekko D32", "Cây", "ppr32", 195_000),
        new(OngNuoc, "Ống HDPE D32", "Mét", "hdpe32", 28_000),
        new(OngNuoc, "Ống HDPE D50", "Mét", "hdpe50", 62_000),
        new(OngNuoc, "Ống mềm inox 60cm", "Cái", "omem60", 35_000),
        new(OngNuoc, "Ống mềm inox 1m", "Cái", "omem1", 48_000),
        new(OngNuoc, "Ống thoát mềm lavabo", "Cái", "othoat", 45_000),

        // ---------- Phụ kiện ống nước ----------
        new(PhuKienOng, "Co nối PVC D21", "Cái", "co21", 4_000),
        new(PhuKienOng, "Co nối PVC D27", "Cái", "co27", 6_000),
        new(PhuKienOng, "Co nối PVC D34", "Cái", "co34", 9_000),
        new(PhuKienOng, "Co nối PVC D48", "Cái", "co48", 16_000),
        new(PhuKienOng, "Tê PVC D21", "Cái", "te21", 5_000),
        new(PhuKienOng, "Tê PVC D27", "Cái", "te27", 8_000),
        new(PhuKienOng, "Tê PVC D34", "Cái", "te34", 12_000),
        new(PhuKienOng, "Nối thẳng PVC D21", "Cái", "nt21", 3_500),
        new(PhuKienOng, "Nối thẳng PVC D27", "Cái", "nt27", 5_000),
        new(PhuKienOng, "Nối ren trong PVC D21", "Cái", "nrt21", 6_000),
        new(PhuKienOng, "Nối ren ngoài PVC D21", "Cái", "nrn21", 6_000),
        new(PhuKienOng, "Nối giảm PVC D27 ra D21", "Cái", "ng2721", 5_500),
        new(PhuKienOng, "Rắc co PVC D27", "Cái", "racco27", 15_000),
        new(PhuKienOng, "Bịt đầu PVC D21", "Cái", "bit21", 2_500),
        new(PhuKienOng, "Cút chữ U PVC D21", "Cái", "cutu21", 7_000),
        new(PhuKienOng, "Măng sông PPR D20", "Cái", "ms20", 9_000),
        new(PhuKienOng, "Co PPR D20", "Cái", "coppr20", 11_000),
        new(PhuKienOng, "Tê PPR D25", "Cái", "teppr25", 18_000),
        new(PhuKienOng, "Kẹp ống PVC D21", "Cái", "kepo21", 1_500),
        new(PhuKienOng, "Kẹp ống PVC D27", "Cái", "kepo27", 2_000),
        new(PhuKienOng, "Phễu thu sàn inox 10x10", "Cái", "pheu10", 65_000),
        new(PhuKienOng, "Phễu thu sàn chống mùi", "Cái", "pheucm", 95_000),
        new(PhuKienOng, "Gioăng cao su D21", "Cái", "gioang21", 1_000),

        // ---------- Van & đồng hồ nước ----------
        new(VanNuoc, "Van khoá nước PVC D21", "Cái", "van21", 25_000),
        new(VanNuoc, "Van khoá nước đồng D21", "Cái", "vand21", 55_000),
        new(VanNuoc, "Van khoá nước đồng D27", "Cái", "vand27", 85_000),
        new(VanNuoc, "Van bi đồng Minh Hoà D15", "Cái", "vanbi15", 75_000),
        new(VanNuoc, "Van bi đồng Minh Hoà D20", "Cái", "vanbi20", 95_000),
        new(VanNuoc, "Van một chiều đồng D21", "Cái", "van1c21", 110_000),
        new(VanNuoc, "Van góc inox", "Cái", "vangoc", 55_000),
        new(VanNuoc, "Van phao cơ bồn nước", "Cái", "vanphao", 95_000),
        new(VanNuoc, "Van phao điện chống cạn", "Bộ", "vanphaod", 185_000),
        new(VanNuoc, "Van xả bồn cầu", "Bộ", "vanxa", 145_000),
        new(VanNuoc, "Đồng hồ nước Sanwa D15", "Cái", "dhn15", 320_000),
        new(VanNuoc, "Đồng hồ nước Unik D20", "Cái", "dhn20", 420_000),

        // ---------- Thiết bị vệ sinh ----------
        new(VeSinh, "Vòi rửa inox 304", "Cái", "voiinox", 250_000),
        new(VeSinh, "Vòi rửa bát nóng lạnh Inax", "Cái", "voibat", 850_000),
        new(VeSinh, "Vòi lavabo lạnh Viglacera", "Cái", "voilv", 350_000),
        new(VeSinh, "Vòi lavabo nóng lạnh TOTO", "Cái", "voilvnl", 1_450_000),
        new(VeSinh, "Sen tắm nóng lạnh Caesar", "Bộ", "sennl", 1_250_000),
        new(VeSinh, "Sen tắm cây Inax", "Bộ", "sencay", 3_200_000),
        new(VeSinh, "Bát sen tăng áp", "Cái", "batsen", 120_000),
        new(VeSinh, "Dây sen inox 1m5", "Cái", "dsen", 85_000),
        new(VeSinh, "Vòi xịt vệ sinh", "Bộ", "voixit", 185_000),
        new(VeSinh, "Xi phông lavabo", "Bộ", "xiphong", 135_000),
        new(VeSinh, "Chậu rửa lavabo Viglacera", "Cái", "lavabo", 850_000),
        new(VeSinh, "Bồn cầu 2 khối Viglacera", "Bộ", "boncau2k", 2_450_000),
        new(VeSinh, "Bồn cầu 1 khối Inax", "Bộ", "boncau1k", 5_600_000),
        new(VeSinh, "Nắp bồn cầu êm", "Cái", "napbc", 280_000),
        new(VeSinh, "Gương soi 45x60", "Cái", "guong", 320_000),
        new(VeSinh, "Kệ kính góc inox", "Cái", "kekinh", 165_000),
        new(VeSinh, "Móc treo khăn inox", "Cái", "mockhan", 75_000),
        new(VeSinh, "Hộp đựng giấy vệ sinh", "Cái", "hopgiay", 95_000),

        // ---------- Dây & cáp điện ----------
        new(DayDien, "Dây điện Cadivi 1x1.5", "Mét", "d115", 7_000),
        new(DayDien, "Dây điện Cadivi 1x2.5", "Mét", "d125", 11_000),
        new(DayDien, "Dây điện Cadivi 1x4", "Mét", "d14", 17_000),
        new(DayDien, "Dây điện Cadivi 2x1.5", "Mét", "d215", 12_000),
        new(DayDien, "Dây điện Cadivi 2x2.5", "Mét", "d225", 18_000),
        new(DayDien, "Dây điện Cadivi 2x4", "Mét", "d24", 28_000),
        new(DayDien, "Dây điện Cadivi 3x2.5", "Mét", "d325", 26_000),
        new(DayDien, "Dây đơn cứng Cadivi VC 1.5 (cuộn 100m)", "Cuộn", "vc15", 620_000),
        new(DayDien, "Dây đơn cứng Cadivi VC 2.5 (cuộn 100m)", "Cuộn", "vc25", 980_000),
        new(DayDien, "Cáp điện Trần Phú 2x6", "Mét", "cap26", 42_000),
        new(DayDien, "Cáp điện Trần Phú 4x10", "Mét", "cap410", 135_000),
        new(DayDien, "Cáp ngầm Trần Phú 4x16", "Mét", "cap416", 210_000),
        new(DayDien, "Dây tiếp địa 1x6", "Mét", "dtd6", 25_000),
        new(DayDien, "Dây cáp tivi RG6", "Mét", "rg6", 8_000),
        new(DayDien, "Dây mạng Cat6 (cuộn 100m)", "Cuộn", "cat6", 850_000),

        // ---------- Điện ----------
        new(Dien, "Aptomat 1 pha 20A", "Cái", "at20", 95_000),
        new(Dien, "Aptomat 1 pha 32A", "Cái", "at32", 105_000),
        new(Dien, "Aptomat 1 pha 40A", "Cái", "at40", 135_000),
        new(Dien, "Aptomat 2 pha 63A", "Cái", "at63", 285_000),
        new(Dien, "Aptomat chống giật 2P 32A", "Cái", "atcg32", 620_000),
        new(Dien, "Cầu dao tổng 3 pha 100A", "Cái", "cd100", 1_250_000),
        new(Dien, "Tủ điện âm tường 6 đường", "Cái", "tudien6", 285_000),
        new(Dien, "Tủ điện nổi 4 đường", "Cái", "tudien4", 165_000),
        new(Dien, "Ổ cắm đơn 3 chấu Panasonic", "Cái", "oc1", 55_000),
        new(Dien, "Ổ cắm đôi 3 chấu", "Cái", "oc2", 65_000),
        new(Dien, "Ổ cắm đôi kèm công tắc", "Cái", "occt", 95_000),
        new(Dien, "Ổ cắm chống nước ngoài trời", "Cái", "occn", 185_000),
        new(Dien, "Ổ cắm âm sàn", "Bộ", "ocsan", 450_000),
        new(Dien, "Ổ cắm kéo dài 5m", "Cái", "ockd", 145_000),
        new(Dien, "Công tắc đơn", "Cái", "ct1", 32_000),
        new(Dien, "Công tắc đôi Panasonic", "Cái", "ct2", 58_000),
        new(Dien, "Công tắc ba Panasonic", "Cái", "ct3", 78_000),
        new(Dien, "Công tắc cầu thang 2 chiều", "Cái", "ctct", 65_000),
        new(Dien, "Mặt che 1 lỗ Panasonic", "Cái", "mat1", 18_000),
        new(Dien, "Mặt che 3 lỗ Panasonic", "Cái", "mat3", 25_000),
        new(Dien, "Đế âm nhựa chữ nhật", "Cái", "deam", 12_000),
        new(Dien, "Ổn áp LiOA 3kVA", "Cái", "onap3", 3_250_000),
        new(Dien, "Chuông cửa có dây", "Bộ", "chuong", 145_000),
        new(Dien, "Quạt hút mùi nhà vệ sinh", "Cái", "quathut", 320_000),
        new(Dien, "Quạt thông gió âm trần", "Cái", "quatgio", 480_000),

        // ---------- Ống luồn & phụ kiện điện ----------
        new(OngLuon, "Ống ruột gà D20", "Mét", "rg20", 6_000),
        new(OngLuon, "Ống ruột gà D25", "Mét", "rg25", 8_000),
        new(OngLuon, "Ống luồn dây điện PVC D20", "Cây", "ol20", 28_000),
        new(OngLuon, "Ống luồn dây điện PVC D25", "Cây", "ol25", 38_000),
        new(OngLuon, "Máng cáp nhựa 24x14", "Cây", "mangcap24", 45_000),
        new(OngLuon, "Máng cáp nhựa 39x18", "Cây", "mangcap39", 68_000),
        new(OngLuon, "Kẹp ống ruột gà D20", "Cái", "keprg20", 1_200),
        new(OngLuon, "Hộp nối dây điện", "Cái", "hopnoi", 15_000),
        new(OngLuon, "Băng dính điện Nano", "Cuộn", "bangdien", 8_000),
        new(OngLuon, "Đầu cos bít 2.5 (túi 100 cái)", "Túi", "daucos", 35_000),
        new(OngLuon, "Dây rút nhựa 20cm (túi 100 cái)", "Túi", "dayrut", 22_000),

        // ---------- Đèn ----------
        new(Den, "Bóng đèn LED bulb 5W Điện Quang", "Bóng", "led5", 35_000),
        new(Den, "Bóng đèn LED bulb 9W", "Bóng", "led9", 45_000),
        new(Den, "Bóng đèn LED bulb 12W Rạng Đông", "Bóng", "led12", 62_000),
        new(Den, "Đèn LED âm trần 9W", "Bộ", "amtran9", 85_000),
        new(Den, "Đèn LED âm trần 12W Philips", "Bộ", "amtran12", 165_000),
        new(Den, "Đèn tuýp LED 1m2 Rạng Đông", "Bóng", "tuyp12", 95_000),
        new(Den, "Máng đèn LED 1m2", "Bộ", "mangden1", 130_000),
        new(Den, "Máng đèn LED đôi 1m2", "Bộ", "mangden2", 235_000),
        new(Den, "Đèn ốp trần tròn 18W", "Bộ", "optran18", 285_000),
        new(Den, "Đèn ốp trần vuông 24W", "Bộ", "optran24", 385_000),
        new(Den, "Đèn LED panel 600x600", "Bộ", "panel60", 520_000),
        new(Den, "Đèn LED dây 5m", "Cuộn", "leddayn", 120_000),
        new(Den, "Đèn pha LED 100W", "Bộ", "phaled100", 450_000),
        new(Den, "Đèn cảm ứng chuyển động", "Bộ", "camung", 320_000),
        new(Den, "Đèn sạc tích điện", "Cái", "densac", 285_000),
        new(Den, "Đèn gương phòng tắm", "Bộ", "denguong", 245_000),
        new(Den, "Đèn chùm trang trí phòng khách", "Bộ", "denchum", 3_500_000),

        // ---------- Máy nước nóng & bơm ----------
        new(MayBom, "Bình nóng lạnh Ariston 20L", "Cái", "bnl20", 2_650_000),
        new(MayBom, "Bình nóng lạnh Ferroli 30L", "Cái", "bnl30", 3_150_000),
        new(MayBom, "Bình nóng lạnh Picenza 15L", "Cái", "bnl15", 2_150_000),
        new(MayBom, "Máy nước nóng trực tiếp Panasonic", "Cái", "mnntt", 3_850_000),
        new(MayBom, "Máy nước nóng năng lượng mặt trời Sơn Hà 180L", "Bộ", "nlmt180", 12_500_000),
        new(MayBom, "Máy bơm nước Panasonic 125W", "Cái", "bom125", 1_850_000),
        new(MayBom, "Máy bơm tăng áp Shimizu", "Cái", "bomtangap", 2_450_000),
        new(MayBom, "Máy bơm chìm hút nước thải", "Cái", "bomchim", 1_650_000),
        new(MayBom, "Bình tích áp Varem 24L", "Cái", "bta24", 1_850_000),
        new(MayBom, "Rơ le áp lực máy bơm", "Cái", "role", 185_000),
        new(MayBom, "Bộ lọc nước đầu nguồn", "Bộ", "locdaunguon", 1_250_000),
        new(MayBom, "Máy lọc nước RO Kangaroo 9 lõi", "Bộ", "roka9", 5_850_000),

        // ---------- Bồn nước ----------
        new(BonNuoc, "Bồn nước inox Sơn Hà 1000L đứng", "Cái", "bon1000", 4_250_000),
        new(BonNuoc, "Bồn nước inox Tân Á 2000L ngang", "Cái", "bon2000", 7_850_000),
        new(BonNuoc, "Bồn nước inox Toàn Mỹ 500L", "Cái", "bon500", 2_650_000),
        new(BonNuoc, "Bồn nhựa Đại Thành 500L", "Cái", "bonnhua500", 1_650_000),
        new(BonNuoc, "Bồn tự hoại nhựa 1000L", "Cái", "bontuhoai", 3_850_000),
        new(BonNuoc, "Chân bồn nước 1000L", "Bộ", "chanbon", 1_450_000),
        new(BonNuoc, "Thang inox lên bồn", "Cái", "thangbon", 850_000),

        // ---------- Vật tư phụ ----------
        new(VatTuPhu, "Keo dán ống 100g", "Lọ", "keo100", 25_000),
        new(VatTuPhu, "Keo dán ống 500g", "Lọ", "keo500", 95_000),
        new(VatTuPhu, "Băng tan (cao su non)", "Cuộn", "bangtan", 3_000),
        new(VatTuPhu, "Keo silicon trắng", "Tuýp", "silicontrang", 55_000),
        new(VatTuPhu, "Keo silicon trong A500", "Tuýp", "silicontrong", 75_000),
        new(VatTuPhu, "Súng bắn keo silicon", "Cái", "sungkeo", 85_000),
        new(VatTuPhu, "Băng keo bạc", "Cuộn", "keobac", 25_000),
        new(VatTuPhu, "Vít nở nhựa 6mm (túi 100 cái)", "Túi", "vitno6", 25_000),
        new(VatTuPhu, "Vít thạch cao 3cm (hộp)", "Hộp", "vittc", 45_000),
        new(VatTuPhu, "Bát nở sắt M8", "Cái", "batno8", 6_000),
        new(VatTuPhu, "Cưa cắt ống nhựa", "Cái", "cuaong", 65_000),
        new(VatTuPhu, "Kìm cắt ống PPR", "Cái", "kimong", 185_000),
        new(VatTuPhu, "Kìm điện cách điện", "Cái", "kimdien", 145_000),
        new(VatTuPhu, "Tuốc nơ vít 2 đầu", "Cái", "tocnovit", 55_000),
        new(VatTuPhu, "Bút thử điện", "Cái", "butthu", 25_000),
        new(VatTuPhu, "Thước dây 5m", "Cái", "thuocday", 45_000),
        new(VatTuPhu, "Đá cắt 100mm", "Viên", "dacat", 8_000),
    };

    /// <summary>Số mặt hàng trong danh mục dựng sẵn.</summary>
    public static int SoMatHang => DanhSach.Length;

    /// <summary>Tên các nhóm hàng của danh mục dựng sẵn, theo thứ tự xuất hiện.</summary>
    public static IReadOnlyList<string> TenCacNhom => DanhSach.Select(h => h.Nhom).Distinct().ToList();

    /// <summary>Kết quả một lần điền danh mục, để màn hình nói lại cho người dùng.</summary>
    public sealed record KetQua(int SoNhomThem, int SoHangThem, int SoHangDaCo)
    {
        public bool CoThemGi => SoNhomThem > 0 || SoHangThem > 0;
    }

    /// <summary>
    /// Thêm vào <paramref name="du"/> những nhóm và mặt hàng của danh mục dựng sẵn mà cửa hàng
    /// chưa có. Hàng đã có (so tên, bỏ dấu, không phân biệt hoa thường) thì giữ nguyên hoàn toàn:
    /// giá, đơn vị, mã tắt, nhóm cửa hàng tự đặt không bị danh mục mẫu ghi đè.
    /// Mã tắt đã bị hàng khác dùng thì để trống, không để hai hàng cùng một mã tắt.
    /// </summary>
    public static KetQua BoSung(DuLieuApp du)
    {
        var tenDaCo = du.VatTus
            .Select(v => ChuViet.BoDau(v.Ten).Trim())
            .Where(t => t.Length > 0)
            .ToHashSet();

        var maTatDaCo = du.VatTus
            .Select(v => ChuViet.BoDau(v.MaTat).Trim())
            .Where(m => m.Length > 0)
            .ToHashSet();

        // Nhóm cùng tên thì dùng lại nhóm của cửa hàng, đừng tạo thêm nhóm gần giống nhau.
        var nhomTheoTen = new Dictionary<string, Guid>();
        foreach (var nhom in du.NhomHangs)
        {
            nhomTheoTen.TryAdd(ChuViet.BoDau(nhom.Ten).Trim(), nhom.Id);
        }

        var soNhomThem = 0;
        var soHangThem = 0;
        var soHangDaCo = 0;

        foreach (var hang in DanhSach)
        {
            var khoaTen = ChuViet.BoDau(hang.Ten).Trim();
            if (!tenDaCo.Add(khoaTen))
            {
                soHangDaCo++;
                continue;
            }

            var khoaNhom = ChuViet.BoDau(hang.Nhom).Trim();
            if (!nhomTheoTen.TryGetValue(khoaNhom, out var nhomId))
            {
                var nhom = new NhomHang { Ten = hang.Nhom };
                du.NhomHangs.Add(nhom);
                nhomTheoTen[khoaNhom] = nhom.Id;
                nhomId = nhom.Id;
                soNhomThem++;
            }

            var maTat = ChuViet.BoDau(hang.MaTat).Trim();
            du.VatTus.Add(new VatTu
            {
                Ten = hang.Ten,
                DonVi = hang.DonVi,
                NhomId = nhomId,
                MaTat = maTatDaCo.Add(maTat) ? hang.MaTat : string.Empty,
                DonGiaMacDinh = hang.Gia,
            });
            soHangThem++;
        }

        return new KetQua(soNhomThem, soHangThem, soHangDaCo);
    }
}
