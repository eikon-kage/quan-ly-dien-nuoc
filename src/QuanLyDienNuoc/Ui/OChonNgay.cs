using System.Drawing.Drawing2D;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Ô chọn ngày của phần mềm: gõ ngày bằng bàn phím như cũ, còn bảng lịch bung ra là
/// <see cref="BangLich"/> tự vẽ nên chữ luôn tiếng Việt.
/// <para>
/// Dùng thay cho <see cref="DateTimePicker"/> trần. Ô gõ bên trong vẫn là DateTimePicker của
/// Windows (gõ ngày, tháng, năm từng phần, mũi tên lên xuống chỉnh nhanh — quen tay rồi thì
/// nhanh hơn bấm lịch nhiều), nhưng bật <see cref="DateTimePicker.ShowUpDown"/> để <b>tắt hẳn
/// bảng lịch của Windows</b>: bảng ấy viết tên tháng, tên thứ theo cài đặt Region của máy, máy
/// cài Windows tiếng Anh là hiện "August 2026 — S M T W T F S".
/// </para>
/// <para>
/// Bên phải là nút lịch, bấm vào bung tờ lịch tiếng Việt. Bấm F4 hoặc Alt+↓ cũng bung.
/// </para>
/// </summary>
public sealed class OChonNgay : Control
{
    private readonly DateTimePicker _o = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = Theme.DangNgay,

        // BẮT BUỘC giữ true: đây là cách duy nhất tắt được bảng lịch tiếng Anh của Windows.
        ShowUpDown = true,
    };

    private readonly BangLich _lich = new();

    private CuaSoLich? _cuaSo;
    private bool _troTrenNut;
    private DateTime _lucThuLich = DateTime.MinValue;

    /// <summary>Ngày vừa đổi — thay cho <c>DateTimePicker.ValueChanged</c>.</summary>
    public event EventHandler? ValueChanged;

    public OChonNgay()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        Height = 34;
        Width = 190;

        _o.ValueChanged += (_, e) => ValueChanged?.Invoke(this, e);
        Controls.Add(_o);

        _lich.DaChon += (_, ngay) =>
        {
            Value = ngay;
            DongLich();
            _o.Focus();
        };
        _lich.DaHuy += (_, _) =>
        {
            DongLich();
            _o.Focus();
        };
    }

    /// <summary>Ngày đang chọn.</summary>
    public DateTime Value
    {
        get => _o.Value;
        set => _o.Value = value;
    }

    /// <summary>Bảng lịch có đang bung ra không — dùng cho ảnh chụp giao diện.</summary>
    public bool DangMoLich => _cuaSo is { Visible: true };

    /// <summary>Bề ngang phần nút lịch bên phải ô gõ.</summary>
    private int RongNut => Math.Max(34, Font.Height + 12);

    private Rectangle ONut => new(Width - RongNut, 0, RongNut, Height);

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _o.Font = Font;
        _lich.Font = Font;
        XepCho();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        XepCho();
    }

    private void XepCho()
    {
        _o.Bounds = new Rectangle(0, 0, Math.Max(0, Width - RongNut - 2), Height);

        // DateTimePicker có lúc không chịu cao bằng ô (Windows khoá theo cỡ chữ). Thấp hơn thì
        // đặt vào giữa, không thì nó nằm dính mép trên, lệch hẳn so với ô nhập bên cạnh.
        if (_o.Height < Height)
        {
            _o.Top = (Height - _o.Height) / 2;
        }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        Invalidate();
    }

    // ---------- Nút lịch ----------

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var khung = ONut;
        khung.Inflate(-1, -1);
        if (khung.Width <= 0 || khung.Height <= 0)
        {
            return;
        }

        using (var duong = Theme.DuongBo(khung, Theme.Bo))
        {
            using var to = new SolidBrush(_troTrenNut || DangMoLich ? Theme.ChinhNhat : Theme.Trang);
            g.FillPath(to, duong);
            using var but = new Pen(_troTrenNut || DangMoLich ? Theme.Chinh : Theme.Vien);
            g.DrawPath(but, duong);
        }

        VeHinhLich(g, khung);
    }

    /// <summary>Hình tờ lịch nhỏ trên mặt nút: khung, gáy trên và ba dòng kẻ.</summary>
    private static void VeHinhLich(Graphics g, Rectangle khung)
    {
        var canh = Math.Min(khung.Width, khung.Height) - 14;
        if (canh < 8)
        {
            return;
        }

        var to = new Rectangle(
            khung.X + ((khung.Width - canh) / 2),
            khung.Y + ((khung.Height - canh) / 2),
            canh,
            canh);

        using var but = new Pen(Theme.Chu, 1.4F);
        g.DrawRectangle(but, to);
        g.DrawLine(but, to.Left, to.Top + (canh / 3), to.Right, to.Top + (canh / 3));

        // Hai cái móc treo lịch.
        g.DrawLine(but, to.Left + (canh / 4), to.Top - 3, to.Left + (canh / 4), to.Top + 2);
        g.DrawLine(but, to.Right - (canh / 4), to.Top - 3, to.Right - (canh / 4), to.Top + 2);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        DoiTroTrenNut(ONut.Contains(e.Location));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        DoiTroTrenNut(false);
    }

    private void DoiTroTrenNut(bool tren)
    {
        if (_troTrenNut == tren)
        {
            return;
        }

        _troTrenNut = tren;
        Cursor = tren ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left && ONut.Contains(e.Location))
        {
            BatTatLich();
        }
    }

    protected override bool ProcessCmdKey(ref Message m, Keys phim)
    {
        // F4 và Alt+↓ là thói quen bung danh sách của Windows; ô ngày cũ cũng mở lịch bằng hai
        // phím này nên giữ nguyên.
        if (phim is Keys.F4 or (Keys.Alt | Keys.Down))
        {
            BatTatLich();
            return true;
        }

        return base.ProcessCmdKey(ref m, phim);
    }

    // ---------- Bung / đóng tờ lịch ----------

    private void BatTatLich()
    {
        // Bấm nút lúc lịch đang bung: cửa sổ lịch mất tiêu điểm nên đã tự thu lại trước khi nút
        // nhận được cú bấm này. Không nhớ lúc vừa thu thì cú bấm ấy lại bung lịch ra ngay, thành
        // ra bấm mấy cũng không đóng được.
        if (DangMoLich || (DateTime.UtcNow - _lucThuLich).TotalMilliseconds < 250)
        {
            DongLich();
            return;
        }

        MoLich();
    }

    /// <summary>Bung tờ lịch ngay dưới ô, không đủ chỗ thì lật lên trên.</summary>
    public void MoLich()
    {
        _lich.Font = Font;
        _lich.Size = _lich.CoVua();
        _lich.NgayChon = Value.Date;

        if (_cuaSo is null)
        {
            _cuaSo = new CuaSoLich(_lich);
            _cuaSo.VisibleChanged += (_, _) =>
            {
                if (_cuaSo is { Visible: false })
                {
                    _lucThuLich = DateTime.UtcNow;
                }

                Invalidate();
            };
        }

        _cuaSo.ClientSize = _lich.Size;
        _cuaSo.Location = ChoDatLich(_lich.Size);
        _cuaSo.Show(FindForm());
        _cuaSo.Activate();
        _lich.Focus();
        Invalidate();
    }

    /// <summary>Điểm đặt góc trên trái của tờ lịch, tính bằng toạ độ màn hình.</summary>
    private Point ChoDatLich(Size co)
    {
        var duoiO = PointToScreen(new Point(0, Height + 2));
        var manHinh = Screen.FromControl(this).WorkingArea;

        var y = duoiO.Y + co.Height <= manHinh.Bottom
            ? duoiO.Y
            : Math.Max(manHinh.Top, PointToScreen(Point.Empty).Y - 2 - co.Height);

        var x = Math.Max(manHinh.Left, Math.Min(duoiO.X, manHinh.Right - co.Width));

        return new Point(x, y);
    }

    private void DongLich()
    {
        _cuaSo?.Hide();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cuaSo?.Dispose();
            _cuaSo = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Cửa sổ con không viền chứa tờ lịch. Bấm ra chỗ khác là tự thu lại (chỉ ẩn đi chứ không
    /// bỏ, để lần sau bung ra không phải dựng lại).
    /// </summary>
    private sealed class CuaSoLich : Form
    {
        public CuaSoLich(BangLich lich)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            BackColor = Theme.Trang;
            ClientSize = lich.Size;
            lich.Location = Point.Empty;
            Controls.Add(lich);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
        }

        /// <summary>Đóng cửa sổ chính lúc lịch đang bung: chỉ thu lịch lại, đừng đóng theo.</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }
    }
}
