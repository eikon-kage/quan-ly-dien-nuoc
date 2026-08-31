using System.Drawing.Drawing2D;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Ô chọn ngày của phần mềm: gõ ngày bằng bàn phím, hoặc bấm nút lịch bung
/// <see cref="BangLich"/> ra chọn. Chữ trên ô và trong lịch đều tiếng Việt trên mọi máy.
/// <para>
/// Không dùng <see cref="DateTimePicker"/> của Windows, vì hai chỗ hỏng:
/// </para>
/// <list type="number">
/// <item>bảng lịch bung ra lấy tên tháng, tên thứ theo <b>cài đặt Region của máy</b> chứ không
/// theo ngôn ngữ phần mềm đặt — máy cài Windows tiếng Anh là hiện "August 2026 — S M T W T F S";</item>
/// <item>ô gõ của nó viết chữ <b>dính sát viền trái</b>, không có lề. Ở cỡ chữ to của phần mềm
/// (chủ cửa hàng có tuổi) thì chữ số đầu của ngày trông như bị cắt cụt, mà nới ô rộng ra cũng
/// không đỡ: chỗ thừa rơi hết về bên phải.</item>
/// </list>
/// <para>
/// Nên ruột ô là <see cref="TextBox"/> thường, đặt trong khung bo góc do ô tự vẽ — giống hệt các
/// ô nhập khác trong phần mềm. Gõ kiểu gì cũng nhận (<c>3/8</c>, <c>3-8-26</c>, <c>31082026</c>),
/// xem <see cref="NgayViet"/>. Phím ↑↓ chỉnh từng ngày, PageUp/PageDown chỉnh từng tháng,
/// F4 hoặc Alt+↓ bung lịch.
/// </para>
/// </summary>
public sealed class OChonNgay : Control
{
    private readonly TextBox _o = new()
    {
        BorderStyle = BorderStyle.None,
        BackColor = Theme.Trang,
        ForeColor = Theme.Chu,
    };

    private readonly BangLich _lich = new();

    private CuaSoLich? _cuaSo;
    private DateTime _ngay = DateTime.Today;
    private bool _dangDatChu;
    private bool _troTrenNut;
    private DateTime _lucThuLich = DateTime.MinValue;

    /// <summary>Ngày vừa đổi — thay cho <c>DateTimePicker.ValueChanged</c>.</summary>
    public event EventHandler? ValueChanged;

    public OChonNgay()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint | ControlStyles.ResizeRedraw,
            true);

        Height = 34;
        Width = 190;
        BackColor = Theme.Trang;

        DatChu();
        DoiRongToiThieu();

        _o.Leave += (_, _) =>
        {
            DocChuDaGo();
            Invalidate();
        };
        _o.Enter += (_, _) =>
        {
            // Vào ô là bôi đen sẵn: gõ đè lên luôn, khỏi phải xoá chữ cũ.
            _o.SelectAll();
            Invalidate();
        };
        _o.KeyDown += KhiGoPhim;
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
        get => _ngay;
        set
        {
            var moi = value.Date;
            if (moi == _ngay)
            {
                // Vẫn viết lại chữ: có thể người dùng đang gõ dở một chữ không đọc được.
                DatChu();
                return;
            }

            _ngay = moi;
            DatChu();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Bảng lịch có đang bung ra không.</summary>
    public bool DangMoLich => _cuaSo is { Visible: true };

    /// <summary>Bề ngang phần nút lịch bên phải, nằm trong khung.</summary>
    private int RongNut => Math.Max(30, Font.Height + 8);

    private Rectangle ONut => new(Width - RongNut - 2, 1, RongNut, Height - 2);

    /// <summary>Lề trái của chữ trong khung — đúng bằng lề của các ô nhập khác.</summary>
    private const int LeChu = 10;

    /// <summary>
    /// Bề ngang hẹp nhất mà chữ ngày còn đủ chỗ: lề trái, chữ "00/00/0000" theo đúng cỡ chữ
    /// đang dùng, rồi tới nút lịch. Phải đo chứ không đặt số cứng — máy đặt cỡ hiển thị 125%
    /// thì chữ nở ra mà bề ngang ghi trong từng màn hình thì không.
    /// </summary>
    public int RongToiThieu =>
        LeChu + TextRenderer.MeasureText("00/00/0000", Font).Width + 8 + RongNut + 4;

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _o.Font = Font;
        _lich.Font = Font;
        DoiRongToiThieu();
        XepCho();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        XepCho();
    }

    /// <summary>
    /// Khoá bề ngang hẹp nhất vào <see cref="Control.MinimumSize"/>: màn hình nào đặt ô hẹp hơn
    /// thì ô tự giữ lại đủ rộng, và <c>Theme.Truong</c> nới khung có nhãn theo.
    /// </summary>
    private void DoiRongToiThieu() => MinimumSize = new Size(RongToiThieu, 0);

    private void XepCho()
    {
        // TextBox một dòng cao theo cỡ chữ, kéo cao không được — đặt nó vào giữa khung.
        var rong = Math.Max(0, Width - LeChu - RongNut - 6);
        _o.Bounds = new Rectangle(LeChu, Math.Max(1, (Height - _o.Height) / 2), rong, _o.Height);
    }

    // ---------- Chữ trong ô ----------

    private void DatChu()
    {
        _dangDatChu = true;
        _o.Text = NgayViet.Viet(_ngay);
        _o.SelectionStart = _o.TextLength;
        _dangDatChu = false;
    }

    /// <summary>
    /// Đọc chữ vừa gõ. Gõ sai thì trả ô về ngày cũ chứ không đoán bừa — vào sổ sai ngày thì
    /// đến cuối tháng đối chiếu mới lòi ra, lúc ấy không ai nhớ hôm ấy lấy hàng gì.
    /// </summary>
    private void DocChuDaGo()
    {
        if (_dangDatChu)
        {
            return;
        }

        if (NgayViet.TryDoc(_o.Text, _ngay, out var ngay))
        {
            Value = ngay;
            return;
        }

        DatChu();
    }

    private void KhiGoPhim(object? nguoiGui, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                // Không đặt Handled: nhiều màn hình lấy Enter làm phím ghi sổ, nuốt mất là
                // gõ xong ngày rồi bấm Enter không thấy gì xảy ra.
                DocChuDaGo();
                _o.SelectAll();
                return;

            case Keys.Escape:
                DatChu();
                return;

            // ↑↓ chỉnh từng ngày, PageUp/PageDown chỉnh từng tháng — giữ đúng thói quen của ô
            // ngày cũ, vốn có cặp nút ↑↓ của Windows.
            case Keys.Up:
                DoiNgay(1);
                e.Handled = true;
                return;

            case Keys.Down when e.Alt:
                BatTatLich();
                e.Handled = true;
                return;

            case Keys.Down:
                DoiNgay(-1);
                e.Handled = true;
                return;

            case Keys.PageUp:
                DoiThang(-1);
                e.Handled = true;
                return;

            case Keys.PageDown:
                DoiThang(1);
                e.Handled = true;
                return;

            case Keys.F4:
                BatTatLich();
                e.Handled = true;
                return;
        }
    }

    private void DoiNgay(int so)
    {
        DocChuDaGo();
        Value = _ngay.AddDays(so);
        _o.SelectAll();
    }

    private void DoiThang(int so)
    {
        DocChuDaGo();
        Value = LichViet.DoiThang(_ngay, so);
        _o.SelectAll();
    }

    // ---------- Khung và nút lịch ----------

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Nen);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var khung = new Rectangle(0, 0, Width - 1, Height - 1);
        if (khung.Width <= 0 || khung.Height <= 0)
        {
            return;
        }

        // Khung trắng bo góc, viền sáng lên lúc đang gõ — y như Theme.HopO của các ô nhập khác.
        using (var duong = Theme.DuongBo(khung, Theme.Bo))
        {
            using var to = new SolidBrush(Theme.Trang);
            g.FillPath(to, duong);
            using var but = new Pen(_o.Focused ? Theme.Chinh : Theme.Vien);
            g.DrawPath(but, duong);
        }

        var oNut = ONut;
        if (_troTrenNut || DangMoLich)
        {
            using var duong = Theme.DuongBo(Rectangle.Inflate(oNut, -2, -3), Theme.Bo);
            using var to = new SolidBrush(Theme.ChinhNhat);
            g.FillPath(to, duong);
        }

        VeHinhLich(g, oNut, _troTrenNut || DangMoLich ? Theme.Chinh : Theme.Xam);
    }

    /// <summary>Hình tờ lịch nhỏ trên mặt nút: khung, gáy trên và hai cái móc treo.</summary>
    private static void VeHinhLich(Graphics g, Rectangle khung, Color mau)
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

        using var but = new Pen(mau, 1.4F);
        g.DrawRectangle(but, to);
        g.DrawLine(but, to.Left, to.Top + (canh / 3), to.Right, to.Top + (canh / 3));
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

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (ONut.Contains(e.Location))
        {
            BatTatLich();
            return;
        }

        // Bấm vào chỗ trống trong khung cũng là bấm vào ô gõ.
        _o.Focus();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _o.Focus();
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
        DocChuDaGo();

        _lich.Font = Font;
        _lich.Size = _lich.CoVua();
        _lich.NgayChon = _ngay;

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
