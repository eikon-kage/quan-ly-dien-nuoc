using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Excel;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Vẽ bảng kê hàng trong ngày của một khách thành **ảnh PNG** để gửi Zalo.
/// <para>
/// Ảnh chứ không phải file Excel hay PDF: khách xem trên điện thoại, ảnh hiện thẳng trong khung
/// chat, không phải tải về rồi tìm phần mềm mở. Bề ngang cố định
/// <see cref="RongAnh"/> px — vừa khít bề ngang màn hình điện thoại, chữ đọc được mà không phải
/// phóng to; chiều cao co theo số dòng hàng, dài quá thì cắt ra nhiều tấm
/// (<see cref="CaoToiDa"/>).
/// </para>
/// <para>
/// Bảng kê này **chỉ ghi hàng và số lượng**, không ghi đơn giá, thành tiền, tổng tiền hay còn
/// nợ. Nó dùng để khách đối chiếu đúng số hàng đã nhận; chuyện tiền nong nói riêng, chứ gửi
/// vào một khung chat mà cả nhà khách đọc được thì không tiện.
/// </para>
/// </summary>
public static class AnhBangKeNgay
{
    /// <summary>Bề ngang ảnh, tính bằng điểm ảnh.</summary>
    public const int RongAnh = 1000;

    /// <summary>
    /// Chiều cao tối đa một tấm ảnh. Dài hơn thì cắt sang tấm sau: Zalo thu ảnh dài thành một
    /// vệt nhỏ trong khung chat, khách phải bấm mở ra mới đọc được, mà mở ra thì chữ đã bị nén nhoè.
    /// </summary>
    public const int CaoToiDa = 2000;

    /// <summary>Lề trái phải chừa trắng.</summary>
    private const int Le = 40;

    private const int RongTrong = RongAnh - (Le * 2);

    /// <summary>Chiều cao dòng ghi mã tờ hoá đơn.</summary>
    private const int CaoDongNhomTo = 34;

    /// <summary>Bề ngang bốn cột của bảng, theo phần trăm phần trong lề.</summary>
    private static readonly float[] TyLeCot = { 8f, 58f, 14f, 20f };

    private static readonly string[] TieuDeCot = { "TT", "TÊN HÀNG", "ĐVT", "SỐ LƯỢNG" };

    private static readonly Font FontTenCuaHang = new("Segoe UI", 17F, FontStyle.Bold);
    private static readonly Font FontNhoCuaHang = new("Segoe UI", 10.5F);
    private static readonly Font FontTenTo = new("Segoe UI", 22F, FontStyle.Bold);
    private static readonly Font FontNgay = new("Segoe UI", 13F);
    private static readonly Font FontTrang = new("Segoe UI", 12F, FontStyle.Bold);
    private static readonly Font FontKhach = new("Segoe UI", 14F, FontStyle.Bold);
    private static readonly Font FontPhuKhach = new("Segoe UI", 11F);
    private static readonly Font FontDauBang = new("Segoe UI", 11F, FontStyle.Bold);
    private static readonly Font FontO = new("Segoe UI", 12F);
    private static readonly Font FontODam = new("Segoe UI", 12F, FontStyle.Bold);
    private static readonly Font FontGhiChuDong = new("Segoe UI", 10F, FontStyle.Italic);
    private static readonly Font FontNhomTo = new("Segoe UI", 10.5F, FontStyle.Bold);
    private static readonly Font FontChan = new("Segoe UI", 10F);

    private static readonly Color NenDauBang = Theme.ChinhNhat;
    private static readonly Color NenDongLe = Color.FromArgb(249, 250, 251);
    private static readonly Color NenNhomTo = Color.FromArgb(240, 242, 245);

    /// <summary>
    /// Dựng ảnh bảng kê: một tấm nếu vừa, nhiều tấm nếu dài quá. Người gọi giữ và
    /// <c>Dispose</c> mọi ảnh trả về.
    /// </summary>
    /// <param name="lucLap">Giờ ghi ở chân ảnh; để trống là lấy giờ máy.</param>
    public static List<Bitmap> Ve(BangKeNgay bang, ThongTinCuaHang cuaHang, DateTime? lucLap = null)
    {
        var luc = lucLap ?? DateTime.Now;

        // Đo trước để biết ảnh cao bao nhiêu và cắt trang ở đâu: dòng tên hàng dài thì xuống hai
        // dòng, không đo trước mà đoán chiều cao là ảnh hoặc bị cắt mất chân, hoặc thừa một
        // khoảng trắng dài.
        using var anhDo = new Bitmap(1, 1);
        using var gDo = Graphics.FromImage(anhDo);
        gDo.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var trangs = ChiaTrang(gDo, bang, cuaHang, luc);
        var anhs = new List<Bitmap>(trangs.Count);

        try
        {
            foreach (var trang in trangs)
            {
                var cao = Dung(gDo, bang, cuaHang, luc, trang, ve: false);

                var anh = new Bitmap(RongAnh, cao, PixelFormat.Format32bppArgb);
                anh.SetResolution(96f, 96f);
                using var g = Graphics.FromImage(anh);
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                Dung(g, bang, cuaHang, luc, trang, ve: true);
                anhs.Add(anh);
            }
        }
        catch
        {
            foreach (var anh in anhs)
            {
                anh.Dispose();
            }

            throw;
        }

        return anhs;
    }

    /// <summary>Lưu ảnh ra file PNG, tự tạo thư mục nếu chưa có.</summary>
    public static void LuuPng(Bitmap anh, string duongDan)
    {
        var thuMuc = Path.GetDirectoryName(duongDan);
        if (!string.IsNullOrEmpty(thuMuc))
        {
            Directory.CreateDirectory(thuMuc);
        }

        anh.Save(duongDan, ImageFormat.Png);
    }

    /// <summary>
    /// Tên file gợi ý cho bảng kê: bỏ những ký tự Windows không cho đặt tên. Nhiều trang thì
    /// đánh số ngay trong tên file, để lúc gửi biết kéo file nào trước file nào sau.
    /// </summary>
    public static string TenFile(BangKeNgay bang, int trang = 0, int soTrang = 1)
    {
        var danhSo = soTrang > 1 ? $" (trang {trang + 1} trong {soTrang})" : string.Empty;
        var ten = $"Bang ke {bang.Khach.Ten} {bang.Ngay:dd-MM-yyyy}{danhSo}.png";
        foreach (var kyTu in Path.GetInvalidFileNameChars())
        {
            ten = ten.Replace(kyTu, ' ');
        }

        return ten;
    }

    // ---------------------------------------------------------------------------------------
    // Chia trang
    // ---------------------------------------------------------------------------------------

    /// <summary>Một khối trên ảnh: dòng hàng, hoặc dòng ghi mã tờ hoá đơn.</summary>
    /// <param name="Tiep">Dòng mã tờ ghi lại ở đầu trang sau, vì tờ bị cắt ngang giữa hai trang.</param>
    private sealed record Khoi(
        int Cao,
        string? MaTo,
        bool LaHoanHang,
        bool Tiep,
        DongBangKe? Hang,
        int SoThuTu,
        int CaoTen,
        int CaoGhiChu);

    private sealed record Trang(List<Khoi> Khoi, int SoHieu, int TongTrang);

    private static List<Trang> ChiaTrang(
        Graphics g,
        BangKeNgay bang,
        ThongTinCuaHang cuaHang,
        DateTime luc)
    {
        var cot = ViTriCot();
        var khoi = DungKhoi(g, bang, cot);

        // Chỗ còn lại cho dòng hàng: cả tấm trừ đầu ảnh, dòng tiêu đề cột và chân ảnh. Đo bằng
        // một trang giả có đủ đầu đủ chân nhưng không dòng nào — hơn là cộng tay từng khoảng
        // cách rồi lệch dần mỗi lần sửa bố cục.
        var trangRong = new Trang(new List<Khoi>(), 0, 9);
        var choTrong = Math.Max(CaoDongNhomTo, CaoToiDa - Dung(g, bang, cuaHang, luc, trangRong, ve: false));

        // Chỉ ghi mã tờ khi khách lấy ở nhiều tờ trong cùng ngày. Một tờ mà cũng ghi thì thêm
        // một dòng chữ chẳng nói lên điều gì, khách đọc lại tưởng là mục hàng.
        var ghiMaTo = bang.MaHoaDons.Count > 1;

        var doan = ChiaTrangAnh.Chia(
            khoi.Select(k => new KhoiAnh(k.Cao, k.MaTo is not null)).ToList(),
            choTrong,
            ghiMaTo ? CaoDongNhomTo : 0);

        // Ngày trống (không dòng hàng nào) vẫn ra đúng một tấm: màn hình đã chặn trước, nhưng
        // vào đây mà trả về danh sách rỗng thì người gọi không có ảnh nào để bày.
        if (doan.Count == 0)
        {
            return new List<Trang> { new(new List<Khoi>(), 0, 1) };
        }

        var trangs = new List<Trang>(doan.Count);
        var toDangBay = (Khoi?)null;

        for (var i = 0; i < doan.Count; i++)
        {
            var trongTrang = doan[i].Select(v => khoi[v]).ToList();

            // Tờ bị cắt ngang giữa hai trang: ghi lại mã tờ ở đầu trang sau, kèm chữ "(tiếp)"
            // để khách không tưởng là một tờ khác trùng mã.
            if (toDangBay is { } dauTo && trongTrang[0].MaTo is null)
            {
                trongTrang.Insert(0, dauTo with { Tiep = true });
            }

            foreach (var k in trongTrang.Where(k => k.MaTo is not null && !k.Tiep))
            {
                toDangBay = k;
            }

            trangs.Add(new Trang(trongTrang, i, doan.Count));
        }

        return trangs;
    }

    /// <summary>Đo từng dòng hàng (và dòng mã tờ) thành khối để chia trang.</summary>
    private static List<Khoi> DungKhoi(Graphics g, BangKeNgay bang, int[] cot)
    {
        var khoi = new List<Khoi>();
        var ghiMaTo = bang.MaHoaDons.Count > 1;
        var toDangBay = string.Empty;
        var soThuTu = 0;
        var rongTen = cot[2] - cot[1] - 12;

        foreach (var dong in bang.Dong)
        {
            if (ghiMaTo && dong.HoaDon.MaHoaDon != toDangBay)
            {
                toDangBay = dong.HoaDon.MaHoaDon;
                khoi.Add(new Khoi(CaoDongNhomTo, toDangBay, dong.HoaDon.LaHoanHang, false, null, 0, 0, 0));
                soThuTu = 0;
            }

            soThuTu++;

            // Tên hàng dài thì xuống dòng chứ không cắt cụt: bảng kê gửi khách mà mất nửa tên
            // hàng là khách không đối chiếu được với hàng đã nhận.
            var caoTen = (int)Math.Ceiling(g.MeasureString(TenHang(dong), FontO, rongTen).Height);
            var caoGhiChu = string.IsNullOrWhiteSpace(dong.Dong.GhiChu)
                ? 0
                : (int)Math.Ceiling(g.MeasureString(dong.Dong.GhiChu, FontGhiChuDong, rongTen).Height) + 2;
            var cao = Math.Max(40, caoTen + caoGhiChu + 14);

            khoi.Add(new Khoi(cao, null, false, false, dong, soThuTu, caoTen, caoGhiChu));
        }

        return khoi;
    }

    private static string TenHang(DongBangKe dong) =>
        dong.Dong.TenHang + (dong.LaHoanTra ? "   (khách trả lại)" : string.Empty);

    // ---------------------------------------------------------------------------------------
    // Vẽ một trang
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Vẽ (hoặc chỉ đo) một tấm ảnh và trả về chiều cao cần dùng. Một hàm cho cả hai lượt
    /// để không bao giờ lệch nhau: sửa bố cục ở lượt vẽ mà quên lượt đo là ảnh cắt mất chữ.
    /// </summary>
    private static int Dung(
        Graphics g,
        BangKeNgay bang,
        ThongTinCuaHang cuaHang,
        DateTime luc,
        Trang trang,
        bool ve)
    {
        var y = 30;

        // ---------- Đầu ảnh: cửa hàng nào gửi ----------
        // Đầu ảnh ghi đủ trên **mọi** trang: mỗi trang là một tấm ảnh riêng trong khung chat,
        // khách mở tấm thứ ba ra mà không thấy tên mình với ngày thì không biết là của ai.
        y = Chu(g, ve, cuaHang.Ten, FontTenCuaHang, Theme.ChuDam, y, canGiua: true);

        var lienHe = string.Join("  ·  ", new[] { cuaHang.DiaChi, cuaHang.DienThoai }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()));
        if (lienHe.Length > 0)
        {
            y = Chu(g, ve, lienHe, FontNhoCuaHang, Theme.Xam, y + 2, canGiua: true);
        }

        y += 18;
        if (ve)
        {
            using var but = new Pen(Theme.Vien);
            g.DrawLine(but, Le, y, RongAnh - Le, y);
        }

        y += 22;

        // ---------- Tên tờ và ngày ----------
        y = Chu(g, ve, "BẢNG KÊ HÀNG TRONG NGÀY", FontTenTo, Theme.Chinh, y, canGiua: true);
        y = Chu(g, ve, $"Ngày {bang.Ngay:dd/MM/yyyy}", FontNgay, Theme.Chu, y + 4, canGiua: true);

        if (trang.TongTrang > 1)
        {
            y = Chu(
                g,
                ve,
                $"Ảnh {trang.SoHieu + 1} trong {trang.TongTrang}",
                FontTrang,
                Theme.Cam,
                y + 4,
                canGiua: true);
        }

        y += 20;

        // ---------- Khách nào ----------
        y = Chu(g, ve, $"Khách hàng: {bang.Khach.Ten}", FontKhach, Theme.ChuDam, y);

        var phuKhach = string.Join("  ·  ", new[] { bang.Khach.DiaChi, bang.Khach.DienThoai }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()));
        if (phuKhach.Length > 0)
        {
            y = Chu(g, ve, phuKhach, FontPhuKhach, Theme.Xam, y + 2);
        }

        y += 14;

        // ---------- Bảng hàng ----------
        y = VeBang(g, trang, ViTriCot(), y, ve);

        // ---------- Chân ảnh ----------
        y += 26;
        var laTrangCuoi = trang.SoHieu == trang.TongTrang - 1;
        y = Chu(
            g,
            ve,
            laTrangCuoi
                ? "Anh/chị xem giúp cửa hàng, có chỗ nào chưa khớp thì nhắn lại để em kiểm tra lại sổ."
                : "Hàng trong ngày còn nữa — xem tiếp ở ảnh sau.",
            FontChan,
            Theme.Xam,
            y,
            canGiua: true,
            rong: RongTrong);

        var chuChan = $"Bảng kê lập lúc {luc:HH:mm} ngày {luc:dd/MM/yyyy}";
        if (!string.IsNullOrWhiteSpace(cuaHang.Ten))
        {
            chuChan += $" — {cuaHang.Ten.Trim()}";
        }

        y = Chu(g, ve, chuChan, FontChan, Theme.XamNhat, y + 2, canGiua: true);

        return y + 30;
    }

    /// <summary>Mép trái của năm đường dọc: bốn cột nên có năm mốc, mốc cuối là mép phải bảng.</summary>
    private static int[] ViTriCot()
    {
        var moc = new int[TyLeCot.Length + 1];
        moc[0] = Le;

        var congDon = 0f;
        for (var i = 0; i < TyLeCot.Length; i++)
        {
            congDon += TyLeCot[i];
            moc[i + 1] = Le + (int)Math.Round(RongTrong * congDon / 100f);
        }

        // Ép mốc cuối đúng mép phải: cộng dồn số làm tròn có thể lệch một hai điểm ảnh, mà lệch
        // thì đường kẻ dọc cuối không trùng khung bảng.
        moc[^1] = RongAnh - Le;
        return moc;
    }

    private static int VeBang(Graphics g, Trang trang, int[] cot, int yDau, bool ve)
    {
        var y = yDau;
        var canPhai = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        var canGiuaO = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var canTrai = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

        // ---------- Dòng tiêu đề cột ----------
        const int caoDauBang = 42;
        if (ve)
        {
            using var choi = new SolidBrush(NenDauBang);
            g.FillRectangle(choi, Le, y, RongTrong, caoDauBang);

            using var mucChu = new SolidBrush(Theme.ChuDam);
            for (var i = 0; i < TieuDeCot.Length; i++)
            {
                var o = new Rectangle(cot[i] + 6, y, cot[i + 1] - cot[i] - 12, caoDauBang);
                g.DrawString(TieuDeCot[i], FontDauBang, mucChu, o, i switch
                {
                    1 => canTrai,
                    3 => canPhai,
                    _ => canGiuaO,
                });
            }

            VeVachDoc(g, cot, y, caoDauBang);
        }

        y += caoDauBang;

        foreach (var khoi in trang.Khoi)
        {
            y = khoi.MaTo is { } maTo
                ? VeDongNhomTo(g, khoi.LaHoanHang, maTo, khoi.Tiep, y, ve)
                : VeDongHang(g, khoi, cot, y, ve, canGiuaO, canPhai);
        }

        // ---------- Khung ngoài ----------
        if (ve)
        {
            using var but = new Pen(Theme.Vien);
            g.DrawRectangle(but, Le, yDau, RongTrong, y - yDau);
        }

        return y;
    }

    /// <summary>Dòng ngăn ghi tờ hoá đơn, khi ngày ấy khách lấy hàng ở nhiều tờ.</summary>
    private static int VeDongNhomTo(Graphics g, bool laHoanHang, string maHoaDon, bool tiep, int y, bool ve)
    {
        if (ve)
        {
            using var choi = new SolidBrush(NenNhomTo);
            g.FillRectangle(choi, Le, y, RongTrong, CaoDongNhomTo);

            using var muc = new SolidBrush(Theme.Chu);
            var chu = laHoanHang ? $"Tờ hoàn hàng {maHoaDon}" : $"Hoá đơn {maHoaDon}";
            if (tiep)
            {
                chu += "  (tiếp)";
            }

            g.DrawString(
                chu,
                FontNhomTo,
                muc,
                new Rectangle(Le + 10, y, RongTrong - 20, CaoDongNhomTo),
                new StringFormat { LineAlignment = StringAlignment.Center });
        }

        return y + CaoDongNhomTo;
    }

    private static int VeDongHang(
        Graphics g,
        Khoi khoi,
        int[] cot,
        int y,
        bool ve,
        StringFormat canGiuaO,
        StringFormat canPhai)
    {
        if (khoi.Hang is not { } dong)
        {
            return y;
        }

        var cao = khoi.Cao;

        if (ve)
        {
            if (khoi.SoThuTu % 2 == 0)
            {
                using var choi = new SolidBrush(NenDongLe);
                g.FillRectangle(choi, Le + 1, y, RongTrong - 1, cao);
            }

            var mauChu = dong.LaHoanTra ? Theme.Do : Theme.Chu;
            using var muc = new SolidBrush(mauChu);

            g.DrawString(
                khoi.SoThuTu.ToString(),
                FontO,
                muc,
                new Rectangle(cot[0], y, cot[1] - cot[0], cao),
                canGiuaO);

            // Tên hàng bám mép trên của ô (không căn giữa chiều dọc) để dòng ghi chú đi ngay
            // dưới nó, chứ không trôi ra giữa ô.
            g.DrawString(
                TenHang(dong),
                FontO,
                muc,
                new RectangleF(cot[1] + 6, y + 7, cot[2] - cot[1] - 12, khoi.CaoTen));

            if (khoi.CaoGhiChu > 0)
            {
                using var mucGhiChu = new SolidBrush(Theme.Xam);
                g.DrawString(
                    dong.Dong.GhiChu,
                    FontGhiChuDong,
                    mucGhiChu,
                    new RectangleF(cot[1] + 6, y + 7 + khoi.CaoTen, cot[2] - cot[1] - 12, khoi.CaoGhiChu));
            }

            g.DrawString(
                dong.Dong.DonVi,
                FontO,
                muc,
                new Rectangle(cot[2], y, cot[3] - cot[2], cao),
                canGiuaO);

            g.DrawString(
                So.Luong(dong.Dong.SoLuong),
                FontODam,
                muc,
                new Rectangle(cot[3] + 6, y, cot[4] - cot[3] - 12, cao),
                canPhai);

            VeVachDoc(g, cot, y, cao);

            using var but = new Pen(Theme.Vien);
            g.DrawLine(but, Le, y + cao, RongAnh - Le, y + cao);
        }

        return y + cao;
    }

    /// <summary>
    /// Ba vạch dọc ngăn bốn cột, chỉ trong đúng chiều cao của một dòng. Kẻ liền một mạch từ
    /// đầu bảng xuống đáy thì vạch cắt ngang cả dòng ghi mã tờ — dòng ấy không chia cột.
    /// </summary>
    private static void VeVachDoc(Graphics g, int[] cot, int y, int cao)
    {
        using var but = new Pen(Theme.Vien);
        for (var i = 1; i < cot.Length - 1; i++)
        {
            g.DrawLine(but, cot[i], y, cot[i], y + cao);
        }
    }

    /// <summary>
    /// Một dòng chữ: vẽ nếu đang ở lượt vẽ, và trả về mép dưới của nó để dòng sau đi tiếp.
    /// </summary>
    private static int Chu(
        Graphics g,
        bool ve,
        string chu,
        Font font,
        Color mau,
        int y,
        bool canGiua = false,
        int rong = 0)
    {
        var beNgang = rong > 0 ? rong : RongTrong;
        var kichThuoc = g.MeasureString(chu, font, beNgang);
        var cao = (int)Math.Ceiling(kichThuoc.Height);

        if (ve)
        {
            using var muc = new SolidBrush(mau);
            var dinhDang = new StringFormat
            {
                Alignment = canGiua ? StringAlignment.Center : StringAlignment.Near,
            };
            g.DrawString(chu, font, muc, new RectangleF(Le, y, beNgang, cao), dinhDang);
        }

        return y + cao;
    }
}
