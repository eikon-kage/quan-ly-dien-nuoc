using System.Drawing.Drawing2D;

namespace QuanLyDienNuoc.Ui;

/// <summary>Mấy hình vẽ nhỏ đứng trước mục trong thanh bên.</summary>
public enum KieuIcon
{
    Nha,
    Bang,
    Thung,
    Bo,
    Tien,
    Luu,
    DongHo,
}

/// <summary>
/// Thanh bên trái theo bản thiết kế: nền trắng, tên phần mềm ở trên, dưới là danh sách mục
/// bấm được. Mục đang mở tô nền xanh nhạt, chữ và hình cùng màu xanh.
/// <para>
/// Hình của từng mục vẽ bằng nét chứ không dùng phông icon: máy khách có thể thiếu bộ phông
/// icon của Windows, thiếu là hiện ra ô vuông rỗng.
/// </para>
/// </summary>
public sealed class ThanhBen : Panel
{
    private readonly FlowLayoutPanel _dsMuc = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Padding = new Padding(12, 10, 12, 10),
        BackColor = Theme.Trang,
    };

    private readonly List<MucBen> _muc = new();

    public ThanhBen(string ten, string phuDe)
    {
        Dock = DockStyle.Left;

        // Bề ngang theo chữ **của máy này**: 268px chỉ vừa ở cỡ hiển thị 100%, máy đặt 125% là
        // tên mục dài ("Bộ hàng thường dùng") bị cắt cụt bằng dấu "…".
        Width = Math.Max(268, (Theme.FontThuong.Height * 13) + 70);
        BackColor = Theme.Trang;

        // Danh sách mục (neo Fill) thêm trước, logo (neo trên) thêm sau — xem chú thích thứ
        // tự neo ở MainForm.TaoGiaoDien.
        Controls.Add(_dsMuc);
        Controls.Add(TaoLogo(ten, phuDe));

        Paint += (_, e) =>
        {
            using var but = new Pen(Theme.Vien);
            e.Graphics.DrawLine(but, Width - 1, 0, Width - 1, Height);
        };
    }

    /// <summary>Thêm một mục vào thanh bên. Trả về mục vừa thêm để gọi <see cref="Chon"/>.</summary>
    public MucBen Them(string chu, KieuIcon icon, Action khiBam)
    {
        var muc = new MucBen(chu, icon)
        {
            Width = Width - 30,
            Margin = new Padding(0, 0, 0, 4),
        };
        muc.Click += (_, _) => khiBam();
        _muc.Add(muc);
        _dsMuc.Controls.Add(muc);
        return muc;
    }

    /// <summary>Một khoảng trống ngăn nhóm mục, như bản thiết kế ngăn nhóm dưới cùng.</summary>
    public void Ngan()
    {
        _dsMuc.Controls.Add(new Panel { Width = Width - 30, Height = 14, BackColor = Theme.Trang });
    }

    /// <summary>Đánh dấu một mục là đang mở, các mục còn lại trở về bình thường.</summary>
    public void Chon(MucBen muc)
    {
        foreach (var m in _muc)
        {
            m.DangMo = m == muc;
        }
    }

    private static Panel TaoLogo(string ten, string phuDe)
    {
        // Hai dòng chữ neo trên chứ không đặt cứng ở y = 24 và y = 50: cỡ chữ to lên là dòng
        // tên tràn xuống đè lên dòng phụ đề — xem "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        var fontTen = new Font("Segoe UI", 15F, FontStyle.Bold);
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = Math.Max(92, fontTen.Height + Theme.FontPhu.Height + 48),
            BackColor = Theme.Trang,
            Padding = new Padding(74, 22, 8, 8),
        };

        var lblTen = new Label
        {
            Text = ten,
            Font = fontTen,
            ForeColor = Theme.Chinh,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
        };

        var lblPhu = new Label
        {
            Text = phuDe,
            Font = Theme.FontPhu,
            ForeColor = Theme.XamNhat,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(2, 2, 0, 0),
        };

        // Neo trên thì cái thêm sau nằm trên: thêm phụ đề trước, tên sau.
        panel.Controls.Add(lblPhu);
        panel.Controls.Add(lblTen);
        panel.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var o = new Rectangle(20, Math.Max(10, (panel.Height - 42) / 2), 42, 42);
            using var duong = Theme.DuongBo(o, 10);
            using var to = new LinearGradientBrush(o, Theme.Chinh, Theme.Xanh, LinearGradientMode.ForwardDiagonal);
            g.FillPath(to, duong);

            // Giọt nước trắng giữa ô — nhắc nghề của cửa hàng, không cần file ảnh nào.
            using var toTrang = new SolidBrush(Theme.Trang);
            var giot = new GraphicsPath();
            giot.AddLine(o.X + 21, o.Y + 11, o.X + 29, o.Y + 24);
            giot.AddArc(o.X + 13, o.Y + 18, 16, 16, 340, 220);
            giot.CloseFigure();
            g.FillPath(toTrang, giot);
            giot.Dispose();
        };
        return panel;
    }

    /// <summary>Một mục trong thanh bên: hình vẽ bên trái, chữ bên phải, cả dải bấm được.</summary>
    public sealed class MucBen : Button
    {
        private readonly KieuIcon _icon;
        private bool _troChuot;
        private bool _dangMo;

        public MucBen(string chu, KieuIcon icon)
        {
            _icon = icon;
            Text = chu;

            // Cao theo chữ của máy này: 48px vừa khít ở cỡ 100%, cỡ to hơn là cắt mất dấu.
            Height = Math.Max(48, Theme.FontThuong.Height + 28);
            Font = Theme.FontThuong;
            ForeColor = Theme.Chu;
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
            UseVisualStyleBackColor = false;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
        }

        /// <summary>Mục đang mở: nền xanh nhạt, chữ xanh đậm.</summary>
        public bool DangMo
        {
            get => _dangMo;
            set
            {
                _dangMo = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Trang);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_dangMo || _troChuot)
            {
                using var duong = Theme.DuongBo(new Rectangle(0, 0, Width - 1, Height - 1), Theme.Bo);
                using var to = new SolidBrush(_dangMo ? Theme.ChinhNhat : Color.FromArgb(246, 247, 249));
                g.FillPath(to, duong);
            }

            var mau = _dangMo ? Theme.Chinh : Theme.Xam;
            VeIcon(g, _icon, new Rectangle(14, (Height - 22) / 2, 22, 22), mau);

            g.SmoothingMode = SmoothingMode.Default;
            var oChu = new Rectangle(48, 0, Math.Max(1, Width - 56), Height);
            TextRenderer.DrawText(
                g,
                Text,
                _dangMo ? Theme.FontDam : Font,
                oChu,
                _dangMo ? Theme.Chinh : Theme.Chu,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _troChuot = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _troChuot = false;
            Invalidate();
            base.OnMouseLeave(e);
        }
    }

    /// <summary>Vẽ hình của một mục bằng nét, gói trong ô vuông <paramref name="o"/>.</summary>
    public static void VeIcon(Graphics g, KieuIcon icon, Rectangle o, Color mau)
    {
        var che = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var but = new Pen(mau, 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        switch (icon)
        {
            case KieuIcon.Nha:
                g.DrawLines(but, new[]
                {
                    new Point(o.X + 1, o.Y + 10),
                    new Point(o.X + 11, o.Y + 1),
                    new Point(o.Right - 1, o.Y + 10),
                });
                g.DrawLines(but, new[]
                {
                    new Point(o.X + 3, o.Y + 10),
                    new Point(o.X + 3, o.Bottom - 1),
                    new Point(o.Right - 3, o.Bottom - 1),
                    new Point(o.Right - 3, o.Y + 10),
                });
                break;

            case KieuIcon.Bang:
                g.DrawRectangle(but, o.X + 2, o.Y + 2, o.Width - 5, o.Height - 5);
                g.DrawLine(but, o.X + 2, o.Y + 8, o.Right - 3, o.Y + 8);
                g.DrawLine(but, o.X + 2, o.Y + 14, o.Right - 3, o.Y + 14);
                g.DrawLine(but, o.X + 9, o.Y + 8, o.X + 9, o.Bottom - 3);
                break;

            case KieuIcon.Thung:
                g.DrawLines(but, new[]
                {
                    new Point(o.X + 2, o.Y + 6),
                    new Point(o.X + 2, o.Bottom - 2),
                    new Point(o.Right - 2, o.Bottom - 2),
                    new Point(o.Right - 2, o.Y + 6),
                });
                g.DrawRectangle(but, o.X + 1, o.Y + 2, o.Width - 3, 4);
                g.DrawLine(but, o.X + 8, o.Y + 11, o.Right - 8, o.Y + 11);
                break;

            case KieuIcon.Bo:
                g.DrawRectangle(but, o.X + 1, o.Y + 1, 8, 8);
                g.DrawRectangle(but, o.X + 12, o.Y + 1, 8, 8);
                g.DrawRectangle(but, o.X + 1, o.Y + 12, 8, 8);
                g.DrawRectangle(but, o.X + 12, o.Y + 12, 8, 8);
                break;

            case KieuIcon.Tien:
                g.DrawRectangle(but, o.X + 1, o.Y + 4, o.Width - 3, o.Height - 9);
                g.DrawEllipse(but, o.X + 7, o.Y + 8, 7, 6);
                g.DrawLine(but, o.X + 4, o.Y + 7, o.X + 4, o.Y + 15);
                g.DrawLine(but, o.Right - 3, o.Y + 7, o.Right - 3, o.Y + 15);
                break;

            case KieuIcon.Luu:
                g.DrawLines(but, new[]
                {
                    new Point(o.X + 1, o.Y + 13),
                    new Point(o.X + 1, o.Bottom - 2),
                    new Point(o.Right - 1, o.Bottom - 2),
                    new Point(o.Right - 1, o.Y + 13),
                });
                g.DrawLine(but, o.X + 11, o.Y + 1, o.X + 11, o.Y + 11);
                g.DrawLines(but, new[]
                {
                    new Point(o.X + 7, o.Y + 7),
                    new Point(o.X + 11, o.Y + 11),
                    new Point(o.X + 15, o.Y + 7),
                });
                break;

            case KieuIcon.DongHo:
                g.DrawEllipse(but, o.X + 1, o.Y + 1, o.Width - 3, o.Height - 3);
                g.DrawLine(but, o.X + 11, o.Y + 6, o.X + 11, o.Y + 11);
                g.DrawLine(but, o.X + 11, o.Y + 11, o.X + 15, o.Y + 13);
                break;
        }

        g.SmoothingMode = che;
    }
}
