using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Excel;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Vẽ bảng kê hàng trong ngày của một khách thành **một tấm ảnh PNG** để gửi Zalo.
/// <para>
/// Ảnh chứ không phải file Excel hay PDF: khách xem trên điện thoại, ảnh hiện thẳng trong khung
/// chat, không phải tải về rồi tìm phần mềm mở. Bề ngang cố định
/// <see cref="RongAnh"/> px — vừa khít bề ngang màn hình điện thoại, chữ đọc được mà không phải
/// phóng to; chiều cao co theo số dòng hàng.
/// </para>
/// </summary>
public static class AnhBangKeNgay
{
    /// <summary>Bề ngang ảnh, tính bằng điểm ảnh.</summary>
    public const int RongAnh = 1000;

    /// <summary>Lề trái phải chừa trắng.</summary>
    private const int Le = 40;

    private const int RongTrong = RongAnh - (Le * 2);

    /// <summary>Bề ngang sáu cột của bảng, theo phần trăm phần trong lề.</summary>
    private static readonly float[] TyLeCot = { 7f, 38f, 9f, 10f, 16f, 20f };

    private static readonly string[] TieuDeCot = { "TT", "TÊN HÀNG", "ĐVT", "SL", "ĐƠN GIÁ", "THÀNH TIỀN" };

    private static readonly Font FontTenCuaHang = new("Segoe UI", 17F, FontStyle.Bold);
    private static readonly Font FontNhoCuaHang = new("Segoe UI", 10.5F);
    private static readonly Font FontTenTo = new("Segoe UI", 22F, FontStyle.Bold);
    private static readonly Font FontNgay = new("Segoe UI", 13F);
    private static readonly Font FontKhach = new("Segoe UI", 14F, FontStyle.Bold);
    private static readonly Font FontPhuKhach = new("Segoe UI", 11F);
    private static readonly Font FontDauBang = new("Segoe UI", 11F, FontStyle.Bold);
    private static readonly Font FontO = new("Segoe UI", 12F);
    private static readonly Font FontODam = new("Segoe UI", 12F, FontStyle.Bold);
    private static readonly Font FontGhiChuDong = new("Segoe UI", 10F, FontStyle.Italic);
    private static readonly Font FontNhomTo = new("Segoe UI", 10.5F, FontStyle.Bold);
    private static readonly Font FontTong = new("Segoe UI", 14F, FontStyle.Bold);
    private static readonly Font FontBangChu = new("Segoe UI", 11F, FontStyle.Italic);
    private static readonly Font FontDongTien = new("Segoe UI", 13F, FontStyle.Bold);
    private static readonly Font FontChan = new("Segoe UI", 10F);

    private static readonly Color NenDauBang = Theme.ChinhNhat;
    private static readonly Color NenDongLe = Color.FromArgb(249, 250, 251);
    private static readonly Color NenNhomTo = Color.FromArgb(240, 242, 245);

    /// <summary>Dựng ảnh bảng kê. Người gọi giữ và <c>Dispose</c> ảnh trả về.</summary>
    /// <param name="lucLap">Giờ ghi ở chân ảnh; để trống là lấy giờ máy.</param>
    public static Bitmap Ve(BangKeNgay bang, ThongTinCuaHang cuaHang, DateTime? lucLap = null)
    {
        var luc = lucLap ?? DateTime.Now;

        // Đo trước để biết ảnh cao bao nhiêu: dòng tên hàng dài thì xuống hai dòng, không đo
        // trước mà đoán chiều cao là ảnh hoặc bị cắt mất chân, hoặc thừa một khoảng trắng dài.
        using (var anhDo = new Bitmap(1, 1))
        using (var gDo = Graphics.FromImage(anhDo))
        {
            gDo.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var cao = Dung(gDo, bang, cuaHang, luc, ve: false);

            var anh = new Bitmap(RongAnh, cao, PixelFormat.Format32bppArgb);
            anh.SetResolution(96f, 96f);
            using var g = Graphics.FromImage(anh);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Dung(g, bang, cuaHang, luc, ve: true);
            return anh;
        }
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

    /// <summary>Tên file gợi ý cho bảng kê: bỏ những ký tự Windows không cho đặt tên.</summary>
    public static string TenFile(BangKeNgay bang)
    {
        var ten = $"Bang ke {bang.Khach.Ten} {bang.Ngay:dd-MM-yyyy}.png";
        foreach (var kyTu in Path.GetInvalidFileNameChars())
        {
            ten = ten.Replace(kyTu, ' ');
        }

        return ten;
    }

    /// <summary>
    /// Vẽ (hoặc chỉ đo) toàn bộ tấm ảnh và trả về chiều cao cần dùng. Một hàm cho cả hai lượt
    /// để không bao giờ lệch nhau: sửa bố cục ở lượt vẽ mà quên lượt đo là ảnh cắt mất chữ.
    /// </summary>
    private static int Dung(Graphics g, BangKeNgay bang, ThongTinCuaHang cuaHang, DateTime luc, bool ve)
    {
        var y = 30;

        // ---------- Đầu ảnh: cửa hàng nào gửi ----------
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
        var cot = ViTriCot();
        y = VeBang(g, bang, cot, y, ve);

        y += 18;

        // ---------- Mấy dòng tiền dưới bảng ----------
        if (bang.TienHang > 0m)
        {
            y = Chu(
                g,
                ve,
                $"Bằng chữ: {DocSo.DocTien(bang.TienHang)}.",
                FontBangChu,
                Theme.Xam,
                y,
                rong: RongTrong);
            y += 10;
        }

        if (bang.DaTraTrongNgay != 0m)
        {
            y = Chu(g, ve, $"Khách trả trong ngày: {So.Tien(bang.DaTraTrongNgay)} đ", FontDongTien, Theme.Xanh, y);
            y += 6;
        }

        y = Chu(g, ve, ChuConNo(bang), FontDongTien, bang.ConNo > 0m ? Theme.Do : Theme.Xanh, y);

        // ---------- Chân ảnh ----------
        y += 26;
        y = Chu(
            g,
            ve,
            "Anh/chị xem giúp cửa hàng, có chỗ nào chưa khớp thì nhắn lại để em kiểm tra lại sổ.",
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

    /// <summary>Dòng "còn nợ" ở cuối: khách trả trước dư thì nói rõ là cửa hàng đang giữ tiền.</summary>
    private static string ChuConNo(BangKeNgay bang)
    {
        var moc = $"tính đến ngày {bang.MocNo:dd/MM/yyyy}";

        return bang.ConNo switch
        {
            > 0m => $"Còn nợ {moc}: {So.Tien(bang.ConNo)} đ",
            < 0m => $"Khách đã trả trước, cửa hàng còn giữ {moc}: {So.Tien(-bang.ConNo)} đ",
            _ => $"Không còn nợ {moc}.",
        };
    }

    /// <summary>Mép trái của bảy đường dọc: sáu cột nên có bảy mốc, mốc cuối là mép phải bảng.</summary>
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

    private static int VeBang(Graphics g, BangKeNgay bang, int[] cot, int yDau, bool ve)
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
                    4 or 5 => canPhai,
                    _ => canGiuaO,
                });
            }

            VeVachDoc(g, cot, y, caoDauBang);
        }

        y += caoDauBang;

        // Chỉ ghi mã tờ khi khách lấy ở nhiều tờ trong cùng ngày. Một tờ mà cũng ghi thì thêm
        // một dòng chữ chẳng nói lên điều gì, khách đọc lại tưởng là mục hàng.
        var ghiMaTo = bang.MaHoaDons.Count > 1;
        var toDangBay = string.Empty;
        var soThuTu = 0;

        foreach (var dong in bang.Dong)
        {
            if (ghiMaTo && dong.HoaDon.MaHoaDon != toDangBay)
            {
                toDangBay = dong.HoaDon.MaHoaDon;
                y = VeDongNhomTo(g, dong.HoaDon.LaHoanHang, toDangBay, y, ve);
                soThuTu = 0;
            }

            soThuTu++;
            y = VeDongHang(g, dong, cot, soThuTu, y, ve, canTrai, canGiuaO, canPhai);
        }

        // ---------- Dòng tổng ----------
        const int caoTong = 52;
        if (ve)
        {
            using var choi = new SolidBrush(NenDauBang);
            g.FillRectangle(choi, Le, y, RongTrong, caoTong);

            using var mucChu = new SolidBrush(Theme.ChuDam);
            var oChu = new Rectangle(Le + 10, y, cot[5] - Le - 20, caoTong);
            g.DrawString("TỔNG TIỀN HÀNG TRONG NGÀY", FontTong, mucChu, oChu, canPhai);

            using var mucTien = new SolidBrush(Theme.Chinh);
            var oTien = new Rectangle(cot[5] + 6, y, cot[6] - cot[5] - 12, caoTong);
            g.DrawString(So.Tien(bang.TienHang), FontTong, mucTien, oTien, canPhai);

            // Dòng tổng chỉ cần một vạch ngăn giữa dòng chữ và số tiền: kẻ đủ sáu cột thì
            // vạch cắt ngang chính dòng chữ "TỔNG TIỀN HÀNG TRONG NGÀY".
            using var but = new Pen(Theme.Vien);
            g.DrawLine(but, cot[5], y, cot[5], y + caoTong);
        }

        y += caoTong;

        // ---------- Khung ngoài ----------
        if (ve)
        {
            using var but = new Pen(Theme.Vien);
            g.DrawRectangle(but, Le, yDau, RongTrong, y - yDau);
        }

        return y;
    }

    /// <summary>Dòng ngăn ghi tờ hoá đơn, khi ngày ấy khách lấy hàng ở nhiều tờ.</summary>
    private static int VeDongNhomTo(Graphics g, bool laHoanHang, string maHoaDon, int y, bool ve)
    {
        const int cao = 34;
        if (ve)
        {
            using var choi = new SolidBrush(NenNhomTo);
            g.FillRectangle(choi, Le, y, RongTrong, cao);

            using var muc = new SolidBrush(Theme.Chu);
            var chu = laHoanHang ? $"Tờ hoàn hàng {maHoaDon}" : $"Hoá đơn {maHoaDon}";
            g.DrawString(
                chu,
                FontNhomTo,
                muc,
                new Rectangle(Le + 10, y, RongTrong - 20, cao),
                new StringFormat { LineAlignment = StringAlignment.Center });
        }

        return y + cao;
    }

    private static int VeDongHang(
        Graphics g,
        DongBangKe dong,
        int[] cot,
        int soThuTu,
        int y,
        bool ve,
        StringFormat canTrai,
        StringFormat canGiuaO,
        StringFormat canPhai)
    {
        var rongTen = cot[2] - cot[1] - 12;
        var tenHang = dong.Dong.TenHang + (dong.LaHoanTra ? "   (khách trả lại)" : string.Empty);

        // Tên hàng dài thì xuống dòng chứ không cắt cụt: bảng kê gửi khách mà mất nửa tên hàng
        // là khách không đối chiếu được với hàng đã nhận.
        var caoTen = (int)Math.Ceiling(g.MeasureString(tenHang, FontO, rongTen).Height);
        var caoGhiChu = string.IsNullOrWhiteSpace(dong.Dong.GhiChu)
            ? 0
            : (int)Math.Ceiling(g.MeasureString(dong.Dong.GhiChu, FontGhiChuDong, rongTen).Height) + 2;
        var cao = Math.Max(40, caoTen + caoGhiChu + 14);

        if (ve)
        {
            if (soThuTu % 2 == 0)
            {
                using var choi = new SolidBrush(NenDongLe);
                g.FillRectangle(choi, Le + 1, y, RongTrong - 1, cao);
            }

            var mauChu = dong.LaHoanTra ? Theme.Do : Theme.Chu;
            using var muc = new SolidBrush(mauChu);

            g.DrawString(
                soThuTu.ToString(),
                FontO,
                muc,
                new Rectangle(cot[0], y, cot[1] - cot[0], cao),
                canGiuaO);

            // Tên hàng bám mép trên của ô (không căn giữa chiều dọc) để dòng ghi chú đi ngay
            // dưới nó, chứ không trôi ra giữa ô.
            g.DrawString(
                tenHang,
                FontO,
                muc,
                new RectangleF(cot[1] + 6, y + 7, rongTen, caoTen));

            if (caoGhiChu > 0)
            {
                using var mucGhiChu = new SolidBrush(Theme.Xam);
                g.DrawString(
                    dong.Dong.GhiChu,
                    FontGhiChuDong,
                    mucGhiChu,
                    new RectangleF(cot[1] + 6, y + 7 + caoTen, rongTen, caoGhiChu));
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

            g.DrawString(
                So.Tien(dong.Dong.DonGia),
                FontO,
                muc,
                new Rectangle(cot[4] + 6, y, cot[5] - cot[4] - 12, cao),
                canPhai);

            g.DrawString(
                So.Tien(dong.ThanhTien),
                FontODam,
                muc,
                new Rectangle(cot[5] + 6, y, cot[6] - cot[5] - 12, cao),
                canPhai);

            VeVachDoc(g, cot, y, cao);

            using var but = new Pen(Theme.Vien);
            g.DrawLine(but, Le, y + cao, RongAnh - Le, y + cao);
        }

        return y + cao;
    }

    /// <summary>
    /// Năm vạch dọc ngăn sáu cột, chỉ trong đúng chiều cao của một dòng. Kẻ liền một mạch từ
    /// đầu bảng xuống đáy thì vạch cắt ngang cả dòng ghi mã tờ lẫn dòng tổng — hai dòng ấy
    /// không chia cột.
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
