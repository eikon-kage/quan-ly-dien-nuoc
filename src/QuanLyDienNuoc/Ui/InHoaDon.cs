using System.Drawing.Printing;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Vẽ hoá đơn ra giấy đúng như mẫu Excel của cửa hàng. Dùng cho cả xem trước lẫn in thật,
/// không cần máy có Excel. Toạ độ tính theo đơn vị 1/100 inch của máy in.
/// <para>
/// Hoá đơn hoàn hàng in cùng bố cục, chỉ khác tên tờ, dòng "hoàn cho hoá đơn ..." và các con
/// số ghi thành số dương — trong sổ chúng là số âm để tự trừ vào nợ.
/// </para>
/// </summary>
public sealed class InHoaDon : PrintDocument
{
    private static readonly float[] TyLeCot = { 6f, 34f, 8f, 14f, 16f, 16f };
    private static readonly string[] TieuDeCot = { "TT", "TÊN HÀNG", "ĐVT", "SỐ LƯỢNG", "ĐƠN GIÁ", "THÀNH TIỀN" };

    private readonly Font _fontTenCuaHang = new("Times New Roman", 13F, FontStyle.Bold);
    private readonly Font _fontNho = new("Times New Roman", 9.5F);
    private readonly Font _fontTieuDe = new("Times New Roman", 16F, FontStyle.Bold);
    private readonly Font _fontPhuDe = new("Times New Roman", 9.5F, FontStyle.Italic);
    private readonly Font _fontThuong = new("Times New Roman", 11F);
    private readonly Font _fontDam = new("Times New Roman", 11F, FontStyle.Bold);
    private readonly Font _fontBang = new("Times New Roman", 10.5F);
    private readonly Font _fontBangDam = new("Times New Roman", 10.5F, FontStyle.Bold);

    private readonly List<List<DongTrenTo>> _trang;
    private readonly HoaDon _hoaDon;
    private readonly HoaDon? _hoaDonGoc;
    private readonly KhachHang _khach;
    private readonly ThongTinCuaHang _cuaHang;
    private readonly DateTime _ngayIn;

    private int _trangHienTai;

    /// <param name="hoaDonGoc">
    /// Hoá đơn bán mà tờ hoàn hàng này hoàn cho — chỉ để in dòng nhắc, hoá đơn bán thì bỏ trống.
    /// </param>
    public InHoaDon(
        HoaDon hoaDon,
        KhachHang khach,
        ThongTinCuaHang cuaHang,
        DateTime? ngayIn = null,
        HoaDon? hoaDonGoc = null)
    {
        _hoaDon = hoaDon;
        _hoaDonGoc = hoaDonGoc;
        _khach = khach;
        _cuaHang = cuaHang;
        _ngayIn = ngayIn ?? DateTime.Today;
        _trang = XuatHoaDon.LenTrang(hoaDon.ChiTiet);

        DocumentName = hoaDon.LaHoanHang
            ? $"Hoá đơn hoàn hàng {hoaDon.MaHoaDon} - {khach.Ten}"
            : $"Hoá đơn {hoaDon.MaHoaDon} - {khach.Ten}";
        DefaultPageSettings.Margins = new Margins(60, 50, 50, 50);
        ChonKhoA4();
    }

    public int SoTrang => _trang.Count;

    /// <summary>
    /// Vẽ một trang ra Graphics bất kỳ. Dùng để xuất bản in thành ảnh mà không cần máy in
    /// (phục vụ việc kiểm tra bố cục trên máy dựng tự động).
    /// </summary>
    public void VeTrangRaAnh(Graphics g, Rectangle khungLe, int soTrang)
    {
        if (soTrang < 0 || soTrang >= _trang.Count)
        {
            return;
        }

        VeMotTrang(g, khungLe, soTrang);
    }

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        _trangHienTai = 0;
        base.OnBeginPrint(e);
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);

        if (e.Graphics is not { } g)
        {
            return;
        }

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        VeMotTrang(g, e.MarginBounds, _trangHienTai);

        _trangHienTai++;
        e.HasMorePages = _trangHienTai < _trang.Count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fontTenCuaHang.Dispose();
            _fontNho.Dispose();
            _fontTieuDe.Dispose();
            _fontPhuDe.Dispose();
            _fontThuong.Dispose();
            _fontDam.Dispose();
            _fontBang.Dispose();
            _fontBangDam.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ChonKhoA4()
    {
        try
        {
            foreach (PaperSize kho in PrinterSettings.PaperSizes)
            {
                if (kho.Kind == PaperKind.A4)
                {
                    DefaultPageSettings.PaperSize = kho;
                    return;
                }
            }
        }
        catch (InvalidPrinterException)
        {
            // Máy chưa cài máy in nào thì cứ để khổ mặc định.
        }
    }

    private void VeMotTrang(Graphics g, Rectangle khung, int soTrang)
    {
        var laTrangDau = soTrang == 0;
        var laTrangCuoi = soTrang == _trang.Count - 1;
        var dong = _trang[soTrang];
        var soDongToiDa = laTrangDau ? MauHoaDon.Trang1.SoDongMoiTrang : MauHoaDon.TrangSau.SoDongMoiTrang;

        using var but = new SolidBrush(Color.Black);
        using var butMo = new SolidBrush(Color.FromArgb(90, 90, 90));
        using var viet = new Pen(Color.Black, 1f);
        using var vietManh = new Pen(Color.FromArgb(70, 70, 70), 0.6f);

        var top = (float)khung.Top;
        if (laTrangDau)
        {
            top = VePhanDau(g, khung, but, butMo);
        }
        else
        {
            VeSoTrang(g, khung, butMo, soTrang);
            top = khung.Top + 26f;
        }

        // Chừa chỗ cho phần chân trang.
        const float CaoDongTong = 28f;
        const float CaoBangChu = 24f;
        const float CaoChuKy = 78f;
        var dayBang = khung.Bottom - CaoDongTong - CaoBangChu - CaoChuKy;

        const float CaoTieuDeBang = 32f;
        var caoDong = (dayBang - top - CaoTieuDeBang) / soDongToiDa;
        var mocCot = TinhMocCot(khung);

        VeBang(g, mocCot, top, CaoTieuDeBang, caoDong, soDongToiDa, dong, but, viet, vietManh);

        var yTong = top + CaoTieuDeBang + (caoDong * soDongToiDa);
        VeChanTrang(g, khung, mocCot, yTong, CaoDongTong, CaoBangChu, dong, laTrangCuoi, but, viet);
    }

    private float VePhanDau(Graphics g, Rectangle khung, Brush but, Brush butMo)
    {
        // Phần đầu của mẫu giấy là hai khối ô gộp, khối nào cũng căn giữa ô gộp của nó chứ không
        // dán vào lề: nửa trái (gộp cột TT → ĐVT) là tên cửa hàng, địa chỉ, điện thoại; nửa phải
        // (gộp cột SỐ LƯỢNG → THÀNH TIỀN) là số tài khoản ngân hàng. Cắt đôi đúng ở mốc cột của
        // bảng bên dưới nên phần đầu thẳng hàng với bảng, y như khi mở file Excel xuất ra.
        using var canhGiua = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter,
        };

        var mocCot = TinhMocCot(khung);
        var giuaTo = mocCot[MauHoaDon.CotSoLuong];
        var rongTrai = giuaTo - khung.Left;
        var rongPhai = khung.Left + khung.Width - giuaTo;
        float y = khung.Top;

        RectangleF OTrai(float cao) => new(khung.Left, y, rongTrai, cao);
        RectangleF OPhai(float cao) => new(giuaTo, y, rongPhai, cao);

        g.DrawString(_cuaHang.Ten, _fontTenCuaHang, but, OTrai(24f), canhGiua);
        g.DrawString(_cuaHang.NganhNghe1, _fontNho, but, OPhai(24f), canhGiua);
        y += 24f;

        g.DrawString(_cuaHang.DiaChi, _fontNho, but, OTrai(20f), canhGiua);
        g.DrawString(_cuaHang.NganhNghe2, _fontNho, but, OPhai(20f), canhGiua);
        y += 20f;

        // Tên tờ giấy: hoá đơn hoàn hàng phải thấy ngay từ trên cùng, không thể để khách với
        // cửa hàng đọc nửa tờ mới biết đây không phải hoá đơn bán.
        var tieuDe = _hoaDon.LaHoanHang ? "HOÁ ĐƠN HOÀN HÀNG" : _cuaHang.TieuDe;
        var phuDe = _hoaDon.LaHoanHang ? "(Khách trả lại hàng)" : _cuaHang.PhuDe;

        // Mẫu giấy mới không có ô tên tờ, hai dòng này chỉ là tên chủ tài khoản ngân hàng với số
        // điện thoại — in cỡ chữ thường, mực đen như hai dòng trên. Chỉ tên tờ thật (mẫu cũ, hoặc
        // tờ hoàn hàng) mới in to kèm phụ đề nghiêng.
        var laTenToThat = _hoaDon.LaHoanHang || _cuaHang.CoTenTo;

        g.DrawString(_cuaHang.DienThoai, _fontNho, but, OTrai(30f), canhGiua);
        g.DrawString(tieuDe, laTenToThat ? _fontTieuDe : _fontNho, but, OPhai(30f), canhGiua);
        y += 32f;

        g.DrawString(
            phuDe,
            laTenToThat ? _fontPhuDe : _fontNho,
            laTenToThat ? butMo : but,
            new RectangleF(giuaTo, y - 6f, rongPhai, 18f),
            canhGiua);

        g.DrawString($"Tên khách hàng: {_khach.Ten}", _fontThuong, but, khung.Left, y);
        y += 22f;

        var diaChi = string.IsNullOrWhiteSpace(_khach.DiaChi) ? new string('.', 60) : _khach.DiaChi;
        g.DrawString($"Địa chỉ: {diaChi}", _fontThuong, but, khung.Left, y);
        y += 26f;

        if (_hoaDon.LaHoanHang && DongNhacHoanHang() is { } nhac)
        {
            // Lý do hoàn là chữ người dùng tự gõ, dài bao nhiêu cũng được — đóng khung lại cho
            // nó cắt bằng "…" thay vì chạy tràn ra khỏi lề phải của tờ giấy.
            using var motDong = new StringFormat
            {
                FormatFlags = StringFormatFlags.NoWrap,
                Trimming = StringTrimming.EllipsisCharacter,
            };

            g.DrawString(nhac, _fontThuong, but, new RectangleF(khung.Left, y, khung.Width, 22f), motDong);
            y += 24f;
        }

        return y;
    }

    private void VeSoTrang(Graphics g, Rectangle khung, Brush but, int soTrang)
    {
        using var canhPhai = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString(
            $"{_khach.Ten} — {(_hoaDon.LaHoanHang ? "hoá đơn hoàn hàng" : "hoá đơn")} {_hoaDon.MaHoaDon}"
            + $" — trang {soTrang + 1}/{_trang.Count}",
            _fontNho,
            but,
            new RectangleF(khung.Left, khung.Top, khung.Width, 18f),
            canhPhai);
    }

    private static float[] TinhMocCot(Rectangle khung)
    {
        var tong = TyLeCot.Sum();
        var moc = new float[TyLeCot.Length + 1];
        moc[0] = khung.Left;
        for (var i = 0; i < TyLeCot.Length; i++)
        {
            moc[i + 1] = moc[i] + (khung.Width * TyLeCot[i] / tong);
        }

        return moc;
    }

    private void VeBang(
        Graphics g,
        float[] mocCot,
        float top,
        float caoTieuDe,
        float caoDong,
        int soDongToiDa,
        List<DongTrenTo> dong,
        Brush but,
        Pen viet,
        Pen vietManh)
    {
        using var canhGiua = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter,
        };
        using var canhTrai = new StringFormat(canhGiua) { Alignment = StringAlignment.Near };
        using var canhPhai = new StringFormat(canhGiua) { Alignment = StringAlignment.Far };

        var trai = mocCot[0];
        var phai = mocCot[^1];
        var day = top + caoTieuDe + (caoDong * soDongToiDa);

        // Tiêu đề bảng
        for (var c = 0; c < TieuDeCot.Length; c++)
        {
            var o = new RectangleF(mocCot[c], top, mocCot[c + 1] - mocCot[c], caoTieuDe);
            g.DrawString(TieuDeCot[c], _fontBangDam, but, o, canhGiua);
        }

        // Khung ngoài và các đường kẻ
        g.DrawRectangle(viet, trai, top, phai - trai, day - top);
        g.DrawLine(viet, trai, top + caoTieuDe, phai, top + caoTieuDe);
        for (var i = 1; i < soDongToiDa; i++)
        {
            var y = top + caoTieuDe + (caoDong * i);
            g.DrawLine(vietManh, trai, y, phai, y);
        }

        for (var c = 1; c < mocCot.Length - 1; c++)
        {
            g.DrawLine(viet, mocCot[c], top, mocCot[c], day);
        }

        // Nội dung
        var dau = _hoaDon.DauInRaGiay;
        for (var i = 0; i < dong.Count; i++)
        {
            var y = top + caoTieuDe + (caoDong * i);

            RectangleF O(int cot) => new(mocCot[cot] + 4f, y, mocCot[cot + 1] - mocCot[cot] - 8f, caoDong);

            var ct = dong[i].Hang;

            // Mốc ngày ghi vào ô số thứ tự của dòng hàng đầu tiên lấy hôm ấy, y như tờ giấy chủ
            // cửa hàng viết tay và y như file Excel xuất ra — tờ in với file xuất phải là cùng
            // một tờ. Ngày in đậm cho nổi lên giữa cột số.
            if (dong[i].Moc is { } moc)
            {
                g.DrawString($"{moc.Day}/{moc.Month}", _fontBangDam, but, O(0), canhGiua);
            }
            else
            {
                g.DrawString(dong[i].SoThuTu.ToString(), _fontBang, but, O(0), canhGiua);
            }

            g.DrawString(ct.TenHang, _fontBang, but, O(1), canhTrai);
            g.DrawString(ct.DonVi, _fontBang, but, O(2), canhGiua);
            g.DrawString(So.Luong(ct.SoLuong * dau), _fontBang, but, O(3), canhPhai);

            if (ct.DonGia != 0)
            {
                g.DrawString(So.Tien(ct.DonGia), _fontBang, but, O(4), canhPhai);
            }

            g.DrawString(So.Tien(ct.ThanhTien * dau), _fontBang, but, O(5), canhPhai);
        }
    }

    private void VeChanTrang(
        Graphics g,
        Rectangle khung,
        float[] mocCot,
        float yTong,
        float caoDongTong,
        float caoBangChu,
        List<DongTrenTo> dong,
        bool laTrangCuoi,
        Brush but,
        Pen viet)
    {
        using var canhGiua = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var canhTrai = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        using var canhPhai = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

        var trai = mocCot[0];
        var phai = mocCot[^1];

        // Dòng tổng
        g.DrawRectangle(viet, trai, yTong, phai - trai, caoDongTong);
        g.DrawLine(viet, mocCot[5], yTong, mocCot[5], yTong + caoDongTong);

        var dau = _hoaDon.DauInRaGiay;
        var tien = (laTrangCuoi
            ? _hoaDon.TongTien
            : dong.Sum(d => d.Hang.ThanhTien)) * dau;
        g.DrawString(
            (laTrangCuoi, _hoaDon.LaHoanHang) switch
            {
                (false, _) => "CỘNG TRANG NÀY",
                (true, true) => "TỔNG TIỀN HOÀN LẠI",
                (true, false) => "TỔNG CỘNG",
            },
            _fontBangDam,
            but,
            new RectangleF(trai + 6f, yTong, mocCot[5] - trai - 12f, caoDongTong),
            canhTrai);
        g.DrawString(
            So.Tien(tien),
            _fontBangDam,
            but,
            new RectangleF(mocCot[5] + 4f, yTong, phai - mocCot[5] - 8f, caoDongTong),
            canhPhai);

        var y = yTong + caoDongTong + 6f;

        if (!laTrangCuoi)
        {
            return;
        }

        g.DrawString(
            $"Thành tiền (bằng chữ): {DocSo.DocTien(_hoaDon.TongTien * dau)}",
            _fontThuong,
            but,
            new RectangleF(trai, y, phai - trai, caoBangChu),
            canhTrai);
        y += caoBangChu + 8f;

        var nuaKhung = (phai - trai) / 2f;
        g.DrawString(
            $"Ngày  {_ngayIn.Day}   tháng  {_ngayIn.Month}   năm {_ngayIn.Year}",
            _fontThuong,
            but,
            new RectangleF(trai + nuaKhung, y, nuaKhung, 20f),
            canhGiua);
        y += 22f;

        g.DrawString(
            _hoaDon.LaHoanHang ? "KHÁCH TRẢ HÀNG" : "KHÁCH HÀNG",
            _fontDam,
            but,
            new RectangleF(trai, y, nuaKhung, 20f),
            canhGiua);
        g.DrawString(
            _hoaDon.LaHoanHang ? "NGƯỜI NHẬN HÀNG" : "NGƯỜI BÁN HÀNG",
            _fontDam,
            but,
            new RectangleF(trai + nuaKhung, y, nuaKhung, 20f),
            canhGiua);
    }

    /// <summary>Dòng nhắc trên tờ hoàn hàng: hoàn cho hoá đơn nào, vì sao hoàn.</summary>
    private string? DongNhacHoanHang()
    {
        var phan = new List<string>();
        if (_hoaDonGoc is { } goc)
        {
            phan.Add($"Hoàn cho hoá đơn {goc.MaHoaDon} ngày {goc.NgayMo:dd/MM/yyyy}");
        }

        if (!string.IsNullOrWhiteSpace(_hoaDon.GhiChu))
        {
            phan.Add($"lý do: {_hoaDon.GhiChu.Trim()}");
        }

        return phan.Count == 0 ? null : string.Join("   ·   ", phan);
    }
}
