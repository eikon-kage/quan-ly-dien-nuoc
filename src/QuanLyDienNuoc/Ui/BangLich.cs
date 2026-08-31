using System.Drawing.Drawing2D;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Tờ lịch tháng do phần mềm tự vẽ, chữ tiếng Việt: "Tháng 8, 2026 — T2 T3 T4 T5 T6 T7 CN".
/// <para>
/// Bảng lịch bung ra của Windows lấy tên tháng, tên thứ theo <b>cài đặt Region của máy</b> chứ
/// không theo ngôn ngữ phần mềm đặt, nên máy cài Windows tiếng Anh thì chủ cửa hàng thấy
/// "August 2026 — S M T W T F S". Vẽ lấy thì máy nào cũng ra tiếng Việt, mà lại theo đúng màu
/// và cỡ chữ của <see cref="Theme"/>.
/// </para>
/// <para>Cách xếp ngày lên lưới nằm ở <see cref="LichViet"/> để test được trên máy không có Windows.</para>
/// </summary>
public sealed class BangLich : Control
{
    /// <summary>Người dùng đã chọn xong một ngày (bấm chuột hoặc Enter) — ô chọn ngày đóng lịch lại.</summary>
    public event EventHandler<DateTime>? DaChon;

    /// <summary>Người dùng bỏ qua (Esc).</summary>
    public event EventHandler? DaHuy;

    private DateTime _ngayChon = DateTime.Today;
    private DateTime _thangXem = DateTime.Today;
    private DateTime? _oTro;
    private int _nutTro = -1;

    /// <summary>Bốn nút lật ở đầu bảng: lùi năm, lùi tháng, tới tháng, tới năm.</summary>
    private static readonly string[] ChuNutLat = { "‹‹", "‹", "›", "››" };
    private static readonly int[] BuocNutLat = { -12, -1, 1, 12 };

    public BangLich()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        TabStop = true;
        Font = Theme.FontNhap;
        BackColor = Theme.Trang;
        ForeColor = Theme.Chu;
        Size = CoVua();
    }

    /// <summary>Ngày đang chọn. Đặt vào thì bảng lật sang tháng chứa ngày ấy.</summary>
    public DateTime NgayChon
    {
        get => _ngayChon;
        set
        {
            _ngayChon = value.Date;
            _thangXem = _ngayChon;
            Invalidate();
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        Size = CoVua();
    }

    // ---------- Kích thước: mọi số đo bám theo chiều cao dòng chữ để máy đặt cỡ hiển thị
    // ---------- 125%, 150% vẫn ra bảng cân đối chứ không vỡ chữ.

    private int Le => Math.Max(8, Font.Height / 2);

    private int CaoDau => Font.Height + 20;

    private int CaoThu => Font.Height + 10;

    private int CaoDongNgay => Font.Height + 14;

    private int CaoChan => Font.Height + 16;

    private int RongO => Math.Max(42, Font.Height + 22);

    /// <summary>Cỡ vừa khít nội dung — ô chọn ngày mở lịch đúng bằng cỡ này.</summary>
    public Size CoVua() => new(
        (RongO * LichViet.SoCot) + (Le * 2),
        CaoDau + CaoThu + (CaoDongNgay * LichViet.SoHang) + CaoChan + Le);

    /// <summary>Ô của một nút lật: hai nút lùi sát trái, hai nút tới sát phải, tên tháng ở giữa.</summary>
    private Rectangle ONutLat(int chiSo)
    {
        var rongNut = RongO * 3 / 4;
        var x = chiSo < 2
            ? Le + (rongNut * chiSo)
            : Width - Le - (rongNut * (4 - chiSo));
        return new Rectangle(x, 4, rongNut, CaoDau - 8);
    }

    /// <summary>Dòng "Hôm nay" ở chân bảng.</summary>
    private Rectangle OHomNay => new(Le, Height - CaoChan - (Le / 2), Width - (Le * 2), CaoChan);

    private Rectangle OCuaNgay(int chiSo)
    {
        var top = CaoDau + CaoThu + (CaoDongNgay * (chiSo / LichViet.SoCot));
        return new Rectangle(Le + (RongO * (chiSo % LichViet.SoCot)), top, RongO, CaoDongNgay);
    }

    // ---------- Vẽ ----------

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Trang);

        using var canhGiua = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        VeDau(g, canhGiua);
        VeTenThu(g, canhGiua);
        VeCacNgay(g, canhGiua);
        VeChan(g, canhGiua);

        using var butVien = new Pen(Theme.Vien);
        g.DrawRectangle(butVien, 0, 0, Width - 1, Height - 1);
    }

    private void VeDau(Graphics g, StringFormat canhGiua)
    {
        using var fontDam = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);
        using var toChu = new SolidBrush(Theme.ChuDam);

        var dongDau = new Rectangle(Le, 0, Width - (Le * 2), CaoDau);

        for (var i = 0; i < ChuNutLat.Length; i++)
        {
            var oNut = ONutLat(i);

            if (_nutTro == i)
            {
                using var toNen = new SolidBrush(Theme.ChinhNhat);
                using var duong = Theme.DuongBo(oNut, Theme.Bo);
                g.FillPath(toNen, duong);
            }

            using var toNut = new SolidBrush(_nutTro == i ? Theme.Chinh : Theme.Xam);
            g.DrawString(ChuNutLat[i], fontDam, toNut, oNut, canhGiua);
        }

        g.DrawString(LichViet.TieuDeThang(_thangXem), fontDam, toChu, dongDau, canhGiua);
    }

    private void VeTenThu(Graphics g, StringFormat canhGiua)
    {
        using var fontThu = new Font(Font.FontFamily, Math.Max(7F, Font.Size - 1.5F), FontStyle.Bold);

        for (var c = 0; c < LichViet.SoCot; c++)
        {
            // Cột chủ nhật màu đỏ như lịch treo tường, để mắt bắt được ngay đầu và cuối tuần.
            using var to = new SolidBrush(c == LichViet.SoCot - 1 ? Theme.Do : Theme.Xam);
            g.DrawString(
                LichViet.TenThu[c],
                fontThu,
                to,
                new Rectangle(Le + (RongO * c), CaoDau, RongO, CaoThu),
                canhGiua);
        }

        using var but = new Pen(Theme.Vien);
        g.DrawLine(but, Le, CaoDau + CaoThu - 1, Width - Le, CaoDau + CaoThu - 1);
    }

    private void VeCacNgay(Graphics g, StringFormat canhGiua)
    {
        var luoi = LichViet.Luoi(_thangXem);
        using var fontDam = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);

        for (var i = 0; i < luoi.Count; i++)
        {
            var ngay = luoi[i];
            var o = OCuaNgay(i);
            var trongThang = LichViet.TrongThang(ngay, _thangXem);
            var daChon = ngay == _ngayChon;
            var laHomNay = ngay == DateTime.Today;

            var oVe = Rectangle.Inflate(o, -3, -2);

            if (daChon)
            {
                using var to = new SolidBrush(Theme.Chinh);
                using var duong = Theme.DuongBo(oVe, Theme.Bo);
                g.FillPath(to, duong);
            }
            else if (_oTro == ngay)
            {
                using var to = new SolidBrush(Theme.ChinhNhat);
                using var duong = Theme.DuongBo(oVe, Theme.Bo);
                g.FillPath(to, duong);
            }

            // Hôm nay có viền riêng, để tìm được chỗ mình đang đứng dù đang chọn ngày khác.
            if (laHomNay && !daChon)
            {
                using var but = new Pen(Theme.Chinh);
                using var duong = Theme.DuongBo(oVe, Theme.Bo);
                g.DrawPath(but, duong);
            }

            var mau = daChon
                ? Theme.Trang
                : !trongThang
                    ? Theme.XamNhat
                    : LichViet.Cot(ngay) == LichViet.SoCot - 1
                        ? Theme.Do
                        : Theme.Chu;

            using var toChu = new SolidBrush(mau);
            g.DrawString(
                ngay.Day.ToString(),
                daChon || laHomNay ? fontDam : Font,
                toChu,
                o,
                canhGiua);
        }
    }

    private void VeChan(Graphics g, StringFormat canhGiua)
    {
        var oChan = OHomNay;

        using var but = new Pen(Theme.Vien);
        g.DrawLine(but, Le, oChan.Top, Width - Le, oChan.Top);

        using var to = new SolidBrush(_nutTro == 4 ? Theme.Chinh : Theme.Xam);
        g.DrawString($"Hôm nay: {LichViet.ThuVaNgay(DateTime.Today)}", Font, to, oChan, canhGiua);
    }

    // ---------- Bấm chuột ----------

    /// <summary>Ô ngày nằm dưới con trỏ, không có thì null.</summary>
    private DateTime? NgayTaiCho(Point diem)
    {
        var luoi = LichViet.Luoi(_thangXem);
        for (var i = 0; i < luoi.Count; i++)
        {
            if (OCuaNgay(i).Contains(diem))
            {
                return luoi[i];
            }
        }

        return null;
    }

    /// <summary>Nút đang trỏ: 0..3 là bốn nút lật tháng/năm, 4 là dòng "Hôm nay", -1 là không nút nào.</summary>
    private int NutTaiCho(Point diem)
    {
        for (var i = 0; i < ChuNutLat.Length; i++)
        {
            if (ONutLat(i).Contains(diem))
            {
                return i;
            }
        }

        return OHomNay.Contains(diem) ? 4 : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var ngay = NgayTaiCho(e.Location);
        var nut = NutTaiCho(e.Location);
        if (ngay == _oTro && nut == _nutTro)
        {
            return;
        }

        _oTro = ngay;
        _nutTro = nut;
        Cursor = ngay is not null || nut >= 0 ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _oTro = null;
        _nutTro = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var nut = NutTaiCho(e.Location);
        if (nut >= 0 && nut < BuocNutLat.Length)
        {
            _thangXem = LichViet.DoiThang(_thangXem, BuocNutLat[nut]);
            _oTro = null;
            Invalidate();
            return;
        }

        if (nut == 4)
        {
            Chot(DateTime.Today);
            return;
        }

        if (NgayTaiCho(e.Location) is { } ngay)
        {
            Chot(ngay);
        }
    }

    // ---------- Bàn phím ----------

    protected override bool IsInputKey(Keys keyData) => keyData switch
    {
        Keys.Left or Keys.Right or Keys.Up or Keys.Down => true,
        Keys.PageUp or Keys.PageDown or Keys.Home or Keys.Enter or Keys.Escape => true,
        _ => base.IsInputKey(keyData),
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var buoc = e.KeyCode switch
        {
            Keys.Left => -1,
            Keys.Right => 1,
            Keys.Up => -LichViet.SoCot,
            Keys.Down => LichViet.SoCot,
            _ => 0,
        };

        if (buoc != 0)
        {
            DoiNgayXem(_ngayChon.AddDays(buoc));
            e.Handled = true;
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.PageUp:
                DoiNgayXem(LichViet.DoiThang(_ngayChon, e.Shift ? -12 : -1));
                e.Handled = true;
                break;
            case Keys.PageDown:
                DoiNgayXem(LichViet.DoiThang(_ngayChon, e.Shift ? 12 : 1));
                e.Handled = true;
                break;
            case Keys.Home:
                DoiNgayXem(DateTime.Today);
                e.Handled = true;
                break;
            case Keys.Enter or Keys.Space:
                Chot(_ngayChon);
                e.Handled = true;
                break;
            case Keys.Escape:
                DaHuy?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }

    /// <summary>Di con trỏ ngày mà chưa chốt: bảng lật tháng theo nếu bước ra khỏi tháng đang xem.</summary>
    private void DoiNgayXem(DateTime ngay)
    {
        _ngayChon = ngay.Date;
        _thangXem = _ngayChon;
        Invalidate();
    }

    private void Chot(DateTime ngay)
    {
        _ngayChon = ngay.Date;
        _thangXem = _ngayChon;
        Invalidate();
        DaChon?.Invoke(this, _ngayChon);
    }
}
