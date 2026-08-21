using System.Drawing.Drawing2D;

namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Màu, phông chữ và các mảnh giao diện dùng chung. Chữ to, dòng thưa cho dễ nhìn.
/// <para>
/// Bảng màu và cách xếp khối lấy theo bộ thiết kế "Inventory Management Dashboard" (Figma):
/// nền xám rất nhạt, nội dung nằm trong thẻ trắng bo góc, bảng kẻ dòng mảnh chứ không tô
/// đầu bảng màu đậm, và mỗi khu chỉ có duy nhất một nút tô màu đặc — nút việc chính.
/// </para>
/// <para>
/// Cỡ chữ vẫn giữ to như trước (chủ cửa hàng có tuổi), không hạ về 14px như bản thiết kế gốc.
/// </para>
/// </summary>
public static class Theme
{
    /// <summary>
    /// Kiểu ngày dùng cho mọi ô chọn ngày. Phải ép tay vì DateTimePicker lấy định dạng
    /// theo Windows chứ không theo ngôn ngữ phần mềm đặt — máy cài Windows tiếng Anh sẽ
    /// hiện 8/3/2026 thay vì 03/08/2026, rất dễ nhập nhầm ngày.
    /// </summary>
    public const string DangNgay = "dd/MM/yyyy";

    /// <summary>Độ bo góc của nút và ô nhập.</summary>
    public const int Bo = 8;

    /// <summary>Độ bo góc của thẻ trắng bọc nội dung.</summary>
    public const int BoThe = 12;

    // ---------- Màu (tên biến giữ nguyên như cũ để 17 cửa sổ không phải sửa gì) ----------

    /// <summary>Nền cả cửa sổ — xám rất nhạt để thẻ trắng nổi lên (grey-50).</summary>
    public static readonly Color Nen = Color.FromArgb(240, 241, 243);

    public static readonly Color Trang = Color.White;

    /// <summary>Màu việc chính (primary-600).</summary>
    public static readonly Color Chinh = Color.FromArgb(19, 102, 217);

    /// <summary>Màu chính lúc trỏ chuột lên (primary-500).</summary>
    public static readonly Color ChinhSang = Color.FromArgb(21, 112, 239);

    /// <summary>Xanh rất nhạt: nền dải nhắc, nền dòng đang chọn.</summary>
    public static readonly Color ChinhNhat = Color.FromArgb(232, 241, 253);

    public static readonly Color Xanh = Color.FromArgb(16, 167, 96);
    public static readonly Color Cam = Color.FromArgb(225, 145, 51);
    public static readonly Color Do = Color.FromArgb(218, 62, 51);

    /// <summary>Tím — bản thiết kế dùng cho nhóm số liệu thứ ba.</summary>
    public static readonly Color Tim = Color.FromArgb(132, 94, 188);

    /// <summary>Chữ phụ (grey-500).</summary>
    public static readonly Color Xam = Color.FromArgb(102, 112, 133);

    /// <summary>Chữ mờ hơn nữa: dòng chú thích dưới con số (grey-400).</summary>
    public static readonly Color XamNhat = Color.FromArgb(133, 141, 157);

    public static readonly Color Vien = Color.FromArgb(224, 226, 231);

    /// <summary>Chữ thường (grey-800).</summary>
    public static readonly Color Chu = Color.FromArgb(56, 62, 73);

    /// <summary>Chữ tiêu đề, đậm hơn chữ thường một bậc.</summary>
    public static readonly Color ChuDam = Color.FromArgb(29, 31, 44);

    // ---------- Phông chữ ----------

    public static readonly Font FontNhan = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontPhu = new("Segoe UI", 11F);
    public static readonly Font FontThuong = new("Segoe UI", 12F);
    public static readonly Font FontDam = new("Segoe UI", 12F, FontStyle.Bold);
    public static readonly Font FontNhap = new("Segoe UI", 13F);

    /// <summary>Cỡ chữ cho ô nhập cần bấm nhiều — ô chọn ngày và lịch bung ra của nó.</summary>
    public static readonly Font FontNhapTo = new("Segoe UI", 14F);
    public static readonly Font FontLuoi = new("Segoe UI", 12.5F);
    public static readonly Font FontLuoiDam = new("Segoe UI", 12.5F, FontStyle.Bold);
    public static readonly Font FontSo = new("Segoe UI", 15F, FontStyle.Bold);
    public static readonly Font FontTieuDe = new("Segoe UI", 19F, FontStyle.Bold);

    /// <summary>Tên thẻ ("Khách hàng", "Sổ công nợ") — nhỏ hơn tiêu đề cửa sổ.</summary>
    public static readonly Font FontTenThe = new("Segoe UI", 14.5F, FontStyle.Bold);

    // ---------- Vẽ ----------

    /// <summary>Đường viền bo bốn góc, dùng chung cho nút, ô nhập và thẻ.</summary>
    public static GraphicsPath DuongBo(Rectangle o, int bo)
    {
        var duong = new GraphicsPath();
        if (bo <= 0 || o.Width <= 0 || o.Height <= 0)
        {
            duong.AddRectangle(o);
            return duong;
        }

        var d = Math.Min(bo * 2, Math.Min(o.Width, o.Height));
        duong.AddArc(o.X, o.Y, d, d, 180, 90);
        duong.AddArc(o.Right - d, o.Y, d, d, 270, 90);
        duong.AddArc(o.Right - d, o.Bottom - d, d, d, 0, 90);
        duong.AddArc(o.X, o.Bottom - d, d, d, 90, 90);
        duong.CloseFigure();
        return duong;
    }

    // ---------- Nút ----------

    /// <summary>
    /// Nút bo góc tự vẽ. WinForms không bo được góc nút sẵn, mà cả bản thiết kế bo góc hết,
    /// nên phải vẽ tay: xoá nền bằng màu của khung cha rồi tô hình bo lên.
    /// </summary>
    private sealed class NutBo : Button
    {
        /// <summary>Cỡ chữ thấp nhất được phép hạ xuống — dưới nữa thì chủ cửa hàng không đọc được.</summary>
        private const float CoChuNhoNhat = 9.5F;

        private const int LeNgang = 10;
        private const int LeDoc = 3;

        private bool _troChuot;
        private bool _dangBam;
        private bool _noTheoChu;

        /// <summary>Phông đã hạ cỡ cho vừa nút. Chỉ phông này được dispose, phông của Theme là phông dùng chung.</summary>
        private Font? _fontHa;
        private string _khoaFontHa = "";

        public NutBo()
        {
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
        }

        /// <summary>Màu viền; để trống là nút tô đặc, không viền.</summary>
        public Color MauVien { get; set; } = Color.Empty;

        /// <summary>
        /// Cho nút **nở ra cho vừa chữ**: bề ngang bề cao đặt tay chỉ còn là mức thấp nhất.
        ///
        /// Vì sao cần: cỡ nút đặt bằng con số cứng (210x44), còn phông thì đặt bằng **điểm**
        /// (12pt) nên nó to theo cỡ hiển thị của Windows. Máy đặt 125% hay 150% là chữ dài
        /// thêm một phần tư, một nửa, mà nút vẫn 210 — chữ dài hơn ô thì bị cắt. `PhongVua`
        /// đỡ được bằng cách hạ cỡ chữ, nhưng hạ chữ trên máy người ta *cố tình* đặt chữ to
        /// là làm ngược điều họ muốn. Nở nút ra thì chữ giữ nguyên cỡ mà vẫn đọc được.
        ///
        /// Chỉ bật cho nút chữ dài, không bật sẵn cho mọi nút: nút nằm trong khung xếp tay
        /// mà tự nở là đè lên nút bên cạnh. Hàng nút của thanh tổng tiền nằm trong
        /// `FlowLayoutPanel` có `AutoSize` nên nở được thoải mái.
        /// </summary>
        public void NoTheoChu()
        {
            _noTheoChu = true;
            // GrowOnly (mặc định của Button): chỉ nới ra, không bao giờ co nhỏ hơn cỡ đặt tay.
            MinimumSize = new Size(Width, Height);
            AutoSize = true;
        }

        /// <summary>
        /// Bề ngang vừa đủ cho chữ nằm trên **một dòng**, cộng lề hai bên.
        ///
        /// Đo ở đây chứ không đo lúc dựng nút: lúc dựng chưa biết máy này hiển thị ở cỡ nào,
        /// còn lúc bố trí thì phông đã là phông thật, đo ra đúng số điểm ảnh thật.
        /// </summary>
        public override Size GetPreferredSize(Size proposedSize)
        {
            if (!_noTheoChu)
            {
                return base.GetPreferredSize(proposedSize);
            }

            var chu = TextRenderer.MeasureText(
                Text,
                Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);

            return new Size(
                Math.Max(MinimumSize.Width, chu.Width + (LeNgang * 2)),
                Math.Max(MinimumSize.Height, chu.Height + (LeDoc * 2)));
        }

        /// <summary>
        /// Vẽ ba chấm thay cho chữ. Vẽ tay chứ không dùng ký tự "⋯": phông Segoe UI thiếu
        /// nhiều ký tự ký hiệu, thiếu là Windows in ra ô vuông rỗng.
        /// </summary>
        public bool VeBaCham { get; set; }

        /// <summary>
        /// Phông để vẽ chữ lần này. Nút dựng bằng cỡ đặt tay, mà chữ thì dài ngắn khác nhau
        /// và máy khách có thể đặt cỡ chữ Windows to hơn — chữ không vừa là bị cắt mất một
        /// nửa, nhìn rất xấu. Nên đo trước: không vừa thì hạ cỡ chữ từng nửa điểm cho tới khi
        /// vừa. Đo một lần rồi nhớ lại, chỉ đo lại khi chữ hoặc cỡ nút đổi.
        /// </summary>
        private Font PhongVua(Graphics g)
        {
            var khoa = $"{Text}|{Font.Name}|{Font.Size}|{Font.Style}|{Width}x{Height}";
            if (_khoaFontHa == khoa)
            {
                return _fontHa ?? Font;
            }

            _khoaFontHa = khoa;
            _fontHa?.Dispose();
            _fontHa = null;

            var rong = Math.Max(1, Width - (LeNgang * 2));
            var cao = Math.Max(1, Height - (LeDoc * 2));
            if (Vua(g, Text, Font, rong, cao))
            {
                return Font;
            }

            for (var co = Font.Size - 0.5F; co >= CoChuNhoNhat; co -= 0.5F)
            {
                var thu = new Font(Font.FontFamily, co, Font.Style);
                if (Vua(g, Text, thu, rong, cao))
                {
                    _fontHa = thu;
                    return thu;
                }

                thu.Dispose();
            }

            _fontHa = new Font(Font.FontFamily, CoChuNhoNhat, Font.Style);
            return _fontHa;
        }

        private static bool Vua(Graphics g, string chu, Font phong, int rong, int cao)
        {
            var co = TextRenderer.MeasureText(
                g, chu, phong, new Size(rong, cao), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            return co.Width <= rong && co.Height <= cao;
        }

        /// <summary>Ba chấm tròn giữa nút, to nhỏ theo cỡ chữ của nút.</summary>
        private void VeChamTron(Graphics g)
        {
            var duongKinh = Math.Max(4, (int)Math.Round(Font.Size * 0.42F));
            var khe = duongKinh + Math.Max(3, duongKinh / 2);
            var tong = (duongKinh * 3) + (khe - duongKinh) * 2;
            var x = (Width - tong) / 2;
            var y = (Height - duongKinh) / 2;

            using var to = new SolidBrush(Enabled ? ForeColor : XamNhat);
            for (var i = 0; i < 3; i++)
            {
                g.FillEllipse(to, x + (i * khe), y, duongKinh, duongKinh);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontHa?.Dispose();
                _fontHa = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var khung = new Rectangle(0, 0, Width - 1, Height - 1);
            using var duong = DuongBo(khung, Bo);

            var nen = BackColor;
            if (!Enabled)
            {
                nen = nen == Trang ? Nen : Color.FromArgb(214, 219, 226);
            }
            else if (_dangBam)
            {
                nen = nen == Trang ? ChinhNhat : ControlPaint.Dark(nen, 0.03f);
            }
            else if (_troChuot)
            {
                nen = nen == Trang ? ChinhNhat : ControlPaint.Light(nen, 0.12f);
            }

            using (var to = new SolidBrush(nen))
            {
                g.FillPath(to, duong);
            }

            // Nút đang được bàn phím chọn thì viền đậm hơn — cửa sổ này dùng Tab và phím tắt nhiều.
            var mauVien = Focused && Enabled ? Chinh : MauVien;
            if (mauVien != Color.Empty)
            {
                using var but = new Pen(mauVien, Focused && Enabled ? 2F : 1F);
                g.DrawPath(but, duong);
            }

            if (VeBaCham)
            {
                VeChamTron(g);
                return;
            }

            g.SmoothingMode = SmoothingMode.Default;

            // Chừa lề hai bên và cho phép xuống dòng: máy để cỡ chữ hệ thống to thì chữ
            // dài ra, thà xuống dòng chứ đừng cắt cụt. Phông lấy cỡ đã đo cho vừa nút.
            var oChu = new Rectangle(
                LeNgang, LeDoc, Math.Max(1, Width - (LeNgang * 2)), Math.Max(1, Height - (LeDoc * 2)));
            TextRenderer.DrawText(
                g,
                Text,
                PhongVua(g),
                oChu,
                Enabled ? ForeColor : XamNhat,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.WordBreak
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
            _dangBam = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _dangBam = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dangBam = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }
    }

    /// <summary>Nút việc chính: tô đặc một màu, chữ trắng.</summary>
    /// <param name="noTheoChu">
    /// Cho nút nở ra cho vừa chữ, `rong` và `cao` thành mức thấp nhất — xem `NutBo.NoTheoChu`.
    /// Bật cho nút chữ dài đứng trong khung tự co (`FlowLayoutPanel` có `AutoSize`).
    /// </param>
    public static Button Nut(string chu, Color mau, int rong = 200, int cao = 46, bool noTheoChu = false)
    {
        var nut = new NutBo
        {
            Text = chu,
            Width = rong,
            Height = cao,
            BackColor = mau,
            ForeColor = Color.White,
            Font = FontDam,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0),
        };

        if (noTheoChu)
        {
            nut.NoTheoChu();
        }

        return nut;
    }

    /// <summary>Nút việc phụ: nền trắng, viền mảnh — theo bản thiết kế, cạnh nút chính.</summary>
    public static Button NutPhu(string chu, int rong = 180, int cao = 46)
    {
        return new NutBo
        {
            Text = chu,
            Width = rong,
            Height = cao,
            BackColor = Trang,
            ForeColor = Chu,
            MauVien = Vien,
            Font = FontDam,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0),
        };
    }

    // ---------- Nút ba chấm gom việc ----------

    /// <summary>
    /// Nhóm việc nằm sau một nút ba chấm. Trước đây dưới mỗi bảng là một hàng nút dài dằng
    /// dặc, mỗi nút một câu chữ — nhìn vào rối, mà cũng chẳng ai bấm quá hai cái đầu. Giữ
    /// ngoài một hai việc làm hằng ngày, còn lại gom vào đây, bấm mới mở ra.
    /// </summary>
    public sealed class NhomViec
    {
        private readonly ContextMenuStrip _menu;
        private readonly List<Action> _capNhatTruocKhiMo = new();

        // Giữ luôn tham chiếu: ToolTip không được control nào giữ hộ, bị dọn rác là mất chú
        // thích. Nhóm việc này thì nút giữ (qua chỗ bắt sự kiện Click), nên để đây là an toàn.
        private readonly ToolTip _mach = new() { InitialDelay = 250, AutoPopDelay = 8000 };

        internal NhomViec(string moTa, int rong, int cao)
        {
            _menu = new ContextMenuStrip { Font = FontNhap, ShowImageMargin = false };

            // Chữ và trạng thái bật/tắt của từng việc tính lại đúng lúc mở menu, chứ không
            // phải nhớ đi cập nhật mỗi khi dữ liệu đổi — đằng nào menu cũng đang đóng.
            _menu.Opening += (_, _) =>
            {
                foreach (var capNhat in _capNhatTruocKhiMo)
                {
                    capNhat();
                }
            };

            Nut = new NutBo
            {
                VeBaCham = true,
                Width = rong,
                Height = cao,
                BackColor = Trang,
                ForeColor = Chu,
                MauVien = Vien,
                Font = FontDam,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0),
                AccessibleName = moTa,
                TabStop = true,
            };
            _mach.SetToolTip(Nut, moTa);
            Nut.Click += (_, _) => Mo();
        }

        /// <summary>Nút để xếp vào hàng nút.</summary>
        public Button Nut { get; }

        /// <summary>Thêm một việc vào menu.</summary>
        /// <param name="chu">Chữ hiện trong menu.</param>
        /// <param name="chay">Việc chạy khi bấm.</param>
        /// <param name="mauChu">Màu chữ — để đỏ cho việc xoá.</param>
        /// <param name="bat">Tính lúc mở menu: trả về false thì việc đó mờ đi, không bấm được.</param>
        public NhomViec Viec(string chu, Action chay, Color mauChu = default, Func<bool>? bat = null)
        {
            return Viec(() => chu, chay, mauChu, bat);
        }

        /// <summary>
        /// Như trên nhưng chữ tính lại lúc mở menu — dùng cho việc đổi chiều theo trạng thái
        /// ("Chốt hoá đơn" / "Mở lại hoá đơn").
        /// </summary>
        public NhomViec Viec(Func<string> chu, Action chay, Color mauChu = default, Func<bool>? bat = null)
        {
            var muc = new ToolStripMenuItem(chu(), null, (_, _) => chay());
            if (mauChu != default)
            {
                muc.ForeColor = mauChu;
            }

            _menu.Items.Add(muc);
            _capNhatTruocKhiMo.Add(() =>
            {
                muc.Text = chu();
                if (bat is not null)
                {
                    muc.Enabled = bat();
                }
            });
            return this;
        }

        /// <summary>Vạch ngăn giữa hai nhóm việc.</summary>
        public NhomViec Ngan()
        {
            _menu.Items.Add(new ToolStripSeparator());
            return this;
        }

        /// <summary>Mở menu ra. Nút nằm sát đáy màn hình thì menu bung lên trên cho khỏi bị cắt.</summary>
        private void Mo()
        {
            var caoMenu = _menu.GetPreferredSize(Size.Empty).Height;
            var duoiNut = Nut.PointToScreen(new Point(0, Nut.Height)).Y;
            var vung = Screen.FromControl(Nut).WorkingArea;
            _menu.Show(Nut, duoiNut + caoMenu > vung.Bottom
                ? new Point(0, -caoMenu)
                : new Point(0, Nut.Height));
        }
    }

    /// <summary>
    /// Nút ba chấm để gom việc. <paramref name="moTa"/> hiện ra khi trỏ chuột vào, để người
    /// dùng biết trong đó có gì mà không phải bấm thử.
    /// </summary>
    public static NhomViec NutBaCham(string moTa, int cao = 46, int rong = 54)
    {
        return new NhomViec(moTa, rong, cao);
    }

    // ---------- Ô nhập ----------

    public static TextBox O(int rong)
    {
        return new TextBox
        {
            Width = rong,
            Font = FontNhap,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Trang,
            ForeColor = Chu,
        };
    }

    /// <summary>
    /// Bọc một ô nhập trong khung bo góc. TextBox của Windows chỉ có viền vuông kiểu hệ thống,
    /// muốn giống bản thiết kế thì phải bỏ viền của nó rồi tự vẽ khung ngoài.
    /// </summary>
    public static Panel HopO(TextBox o, int rong, int cao = 36)
    {
        o.BorderStyle = BorderStyle.None;
        o.BackColor = Trang;

        var hop = new Panel { Width = rong, Height = cao, BackColor = Nen };
        hop.Controls.Add(o);
        hop.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            e.Graphics.Clear(p.Parent?.BackColor ?? Nen);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var duong = DuongBo(new Rectangle(0, 0, p.Width - 1, p.Height - 1), Bo);
            using var to = new SolidBrush(Trang);
            e.Graphics.FillPath(to, duong);
            using var but = new Pen(o.Focused ? Chinh : Vien);
            e.Graphics.DrawPath(but, duong);
        };

        void XepO()
        {
            var caoChu = o.PreferredHeight;
            o.SetBounds(12, Math.Max(1, (hop.Height - caoChu) / 2), Math.Max(1, hop.Width - 24), caoChu);
        }

        hop.SizeChanged += (_, _) => XepO();
        o.FontChanged += (_, _) => XepO();
        o.GotFocus += (_, _) => hop.Invalidate();
        o.LostFocus += (_, _) => hop.Invalidate();
        XepO();
        return hop;
    }

    /// <summary>Ô tìm kiếm ở thanh trên: khung bo góc, có kính lúp và chữ mờ gợi ý.</summary>
    public static Panel HopTim(TextBox o, string goiY, int rong, int cao = 40)
    {
        o.PlaceholderText = goiY;
        var hop = HopO(o, rong, cao);

        // Kính lúp vẽ tay: không phụ thuộc bộ phông icon nào có sẵn trên máy khách.
        hop.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            var giua = p.Height / 2;
            using var but = new Pen(XamNhat, 1.6F);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawEllipse(but, 14, giua - 7, 11, 11);
            e.Graphics.DrawLine(but, 24, giua + 4, 28, giua + 8);
        };

        void XepO()
        {
            var caoChu = o.PreferredHeight;
            o.SetBounds(38, Math.Max(1, (hop.Height - caoChu) / 2), Math.Max(1, hop.Width - 50), caoChu);
        }

        hop.SizeChanged += (_, _) => XepO();
        XepO();
        return hop;
    }

    public static Label Nhan(string chu, Font? font = null, Color? mau = null)
    {
        return new Label
        {
            Text = chu,
            Font = font ?? FontThuong,
            ForeColor = mau ?? Chu,
            AutoSize = true,
            Margin = new Padding(0, 12, 8, 0),
        };
    }

    /// <summary>
    /// Nhãn của ô nhập. Tự vẽ chứ không để Label vẽ, vì hai chuyện:
    ///
    /// <para>
    /// 1. **Đo theo bề ngang thật.** Cỡ chữ phải đo lúc vẽ, trên đúng bề ngang của nhãn sau khi
    /// Windows phóng to giao diện. Đo sẵn lúc dựng là đem chiều dài chữ tính bằng điểm ảnh thật
    /// đi so với bề ngang chưa phóng — máy đặt cỡ chữ 125% là so lệch hẳn, rồi chữ bị cắt.
    /// </para>
    ///
    /// <para>
    /// 2. **Chừa chỗ cho dấu.** Chữ hoa tiếng Việt có dấu cả trên ("Ầ") lẫn dưới ("Ị"), cao hơn
    /// chữ hoa tiếng Anh. Ô nhãn chật một hai điểm ảnh là cắt mất dấu, mà cắt dấu thì đọc ra chữ
    /// khác.
    /// </para>
    /// </summary>
    private sealed class NhanO : Label
    {
        private Font? _fontHa;
        private string _khoaFontHa = string.Empty;

        public NhanO()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw,
                true);
            AutoSize = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                PhongVua(),
                ClientRectangle,
                ForeColor,
                TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontHa?.Dispose();
                _fontHa = null;
            }

            base.Dispose(disposing);
        }

        private Font PhongVua()
        {
            var khoa = $"{Text}|{Font.Size}|{ClientSize.Width}";
            if (_khoaFontHa == khoa)
            {
                return _fontHa ?? Font;
            }

            _khoaFontHa = khoa;
            _fontHa?.Dispose();

            var vua = PhongVuaBeNgang(Text, Font, ClientSize.Width);
            _fontHa = ReferenceEquals(vua, Font) ? null : vua;
            return vua;
        }
    }

    /// <summary>
    /// Phông vừa bề ngang cho một dòng chữ: chữ dài hơn chỗ có thì hạ cỡ từng nửa điểm cho vừa.
    /// Nhãn trên ô nhập đặt cứng theo bề ngang của ô, mà chữ tiếng Việt đủ dấu thì dài hơn ước
    /// lượng — không đo lại là cắt mất chữ.
    /// </summary>
    public static Font PhongVuaBeNgang(string chu, Font goc, int rong, float coNhoNhat = 8.5F)
    {
        if (string.IsNullOrEmpty(chu) || TextRenderer.MeasureText(chu, goc).Width <= rong)
        {
            return goc;
        }

        for (var co = goc.Size - 0.5F; co >= coNhoNhat; co -= 0.5F)
        {
            var thu = new Font(goc.FontFamily, co, goc.Style);
            if (TextRenderer.MeasureText(chu, thu).Width <= rong)
            {
                return thu;
            }

            thu.Dispose();
        }

        return new Font(goc.FontFamily, coNhoNhat, goc.Style);
    }

    /// <summary>Một ô nhập có nhãn nằm phía trên, gộp trong một panel để xếp bằng FlowLayoutPanel.</summary>
    /// <param name="cao">
    /// Chiều cao ô nhập. Để 0 là lấy mặc định (36 cho ô chữ, 34 cho nút và ô chọn ngày).
    /// Truyền số lớn hơn thì panel cao theo, dùng cho ô cần bấm nhiều như ô chọn ngày.
    /// </param>
    /// <param name="le">Khoảng cách sang ô kế tiếp. Hàng nhiều ô quá thì hạ xuống cho vừa màn hình.</param>
    public static Panel Truong(string nhan, Control dieuKhien, int rong, int cao = 0, int le = 18)
    {
        var caoO = cao > 0 ? cao : dieuKhien is TextBox { Multiline: false } ? 36 : 34;

        // Nhãn cao 24px chứ không phải 20: chữ hoa tiếng Việt có dấu cả trên lẫn dưới nên cao
        // hơn chữ hoa thường, chật một hai điểm ảnh là cắt mất dấu. Rồi cách ô nhập 6px.
        const int CaoNhan = 24;
        const int DinhO = CaoNhan + 6;

        var panel = new Panel
        {
            Width = rong,
            Height = Math.Max(66, DinhO + caoO + 4),
            Margin = new Padding(0, 0, le, 0),
        };

        var lbl = new NhanO
        {
            Text = nhan,
            Font = FontNhan,
            ForeColor = Xam,
            Location = new Point(0, 0),
            Size = new Size(rong, CaoNhan),
        };

        // TextBox thì bọc khung bo góc cho giống bản thiết kế; nút hay ô chọn ngày thì để
        // nguyên, đưa vào khung nữa là vẽ đè lên phần Windows tự vẽ.
        if (dieuKhien is TextBox { Multiline: false } o)
        {
            var hop = HopO(o, rong, caoO);
            hop.Location = new Point(0, DinhO);
            panel.Controls.Add(lbl);
            panel.Controls.Add(hop);
            return panel;
        }

        if (dieuKhien is ComboBox cbo)
        {
            cbo.FlatStyle = FlatStyle.Flat;
            cbo.BackColor = Trang;
        }

        dieuKhien.Width = rong;
        dieuKhien.Height = caoO;

        // ComboBox khoá chiều cao theo cỡ chữ, kéo cao không được. Ô nào thấp hơn mức chung của
        // hàng thì đặt vào giữa, để các ô trong một hàng nhìn ngang nhau.
        dieuKhien.Location = new Point(0, DinhO + Math.Max(0, (caoO - dieuKhien.Height) / 2));

        panel.Controls.Add(lbl);
        panel.Controls.Add(dieuKhien);
        return panel;
    }

    // ---------- Thẻ ----------

    /// <summary>
    /// Thẻ trắng bo góc, viền mảnh, bóng rất nhẹ — khối nền tảng của bản thiết kế.
    /// Nội dung xếp vào <see cref="Control.Controls"/> như panel thường.
    /// </summary>
    public sealed class The : Panel
    {
        public The()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
            BackColor = Trang;
            Padding = new Padding(20, 16, 20, 16);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Nen);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var khung = new Rectangle(0, 0, Width - 1, Height - 2);

            // Bóng "Shadow/xs" của bộ thiết kế: một vạch mờ ngay dưới thẻ, không hơn.
            using (var duongBong = DuongBo(new Rectangle(khung.X, khung.Y + 2, khung.Width, khung.Height), BoThe))
            using (var butBong = new Pen(Color.FromArgb(20, 16, 24, 40)))
            {
                g.DrawPath(butBong, duongBong);
            }

            using var duong = DuongBo(khung, BoThe);
            using (var to = new SolidBrush(Trang))
            {
                g.FillPath(to, duong);
            }

            using var but = new Pen(Vien);
            g.DrawPath(but, duong);
        }
    }

    /// <summary>
    /// Đặt một điều khiển (thường là bảng) vào thẻ trắng. Lề trong hẹp hơn thẻ thường: bảng
    /// tự có lề ở từng ô rồi, chừa rộng nữa là mất chỗ hiện dòng.
    /// </summary>
    public static Panel Khung(Control noiDung)
    {
        var the = new The
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(14, 10, 14, 10),
        };

        // Ô nhập nằm trong thẻ thì bỏ viền của nó: viền vuông của Windows nằm trong khung bo
        // góc nhìn thành hai lớp khung lồng nhau.
        if (noiDung is TextBox o)
        {
            o.BorderStyle = BorderStyle.None;
        }

        noiDung.Dock = DockStyle.Fill;
        the.Controls.Add(noiDung);
        return the;
    }

    /// <summary>Tên thẻ, in ở góc trên trái bên trong thẻ.</summary>
    public static Label TenThe(string chu)
    {
        return new Label
        {
            Text = chu,
            Font = FontTenThe,
            ForeColor = ChuDam,
            AutoSize = true,
        };
    }

    // ---------- Lưới ----------

    public static void ApDungLuoi(DataGridView luoi)
    {
        luoi.BackgroundColor = Trang;
        luoi.BorderStyle = BorderStyle.None;
        luoi.EnableHeadersVisualStyles = false;

        // Đầu bảng theo bản thiết kế: nền trắng, chữ xám, chỉ có một vạch kẻ dưới —
        // không còn dải xanh đậm chiếm hết bề ngang màn hình như trước.
        luoi.ColumnHeadersDefaultCellStyle.BackColor = Trang;
        luoi.ColumnHeadersDefaultCellStyle.ForeColor = Xam;
        luoi.ColumnHeadersDefaultCellStyle.SelectionBackColor = Trang;
        luoi.ColumnHeadersDefaultCellStyle.SelectionForeColor = Xam;
        luoi.ColumnHeadersDefaultCellStyle.Font = FontNhan;
        luoi.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 9, 8, 9);
        luoi.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        luoi.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

        // Đầu bảng **tự cao theo chữ**, không đặt cứng 46px. Tên cột dài trong cột hẹp thì
        // Windows cho nó xuống hai dòng, mà đầu bảng cao cố định là mất hẳn dòng dưới ("SỐ HĐ
        // NỢ" chỉ còn thấy "SỐ HĐ"). Lề trên dưới 9px để một dòng vẫn thoáng như cũ.
        luoi.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        luoi.RowTemplate.Height = 46;
        luoi.DefaultCellStyle.Font = FontLuoi;
        luoi.DefaultCellStyle.ForeColor = Chu;
        luoi.DefaultCellStyle.BackColor = Trang;
        luoi.DefaultCellStyle.SelectionBackColor = ChinhNhat;
        luoi.DefaultCellStyle.SelectionForeColor = ChuDam;
        luoi.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

        // Kẻ dòng rất nhạt thay cho sọc đậm: vẫn theo được dòng mà bảng nhìn nhẹ hơn.
        luoi.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
        luoi.GridColor = Vien;
        luoi.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        luoi.RowHeadersVisible = false;
        luoi.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        luoi.MultiSelect = false;
        luoi.AllowUserToAddRows = false;
        luoi.AllowUserToDeleteRows = false;
        luoi.AllowUserToResizeRows = false;
        luoi.AutoGenerateColumns = false;
        luoi.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        luoi.Dock = DockStyle.Fill;

        // Đầu bảng chỉ kẻ một vạch dưới, không kẻ dọc chia cột — đúng bản thiết kế. Phải tự vẽ
        // vì DataGridView chỉ cho chọn "kẻ kín bốn phía" hoặc "không kẻ gì".
        luoi.CellPainting += (_, e) =>
        {
            if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.Graphics is null)
            {
                return;
            }

            e.PaintBackground(e.CellBounds, false);
            e.PaintContent(e.CellBounds);
            using var but = new Pen(Vien);
            e.Graphics.DrawLine(
                but,
                e.CellBounds.Left,
                e.CellBounds.Bottom - 1,
                e.CellBounds.Right,
                e.CellBounds.Bottom - 1);
            e.Handled = true;
        };
    }

    public static DataGridViewTextBoxColumn Cot(
        string thuocTinh,
        string tieuDe,
        int tyLe = 100,
        string? dinhDang = null,
        bool canPhai = false,
        bool chiDoc = true)
    {
        var cot = new DataGridViewTextBoxColumn
        {
            Name = "col" + thuocTinh,
            DataPropertyName = thuocTinh,
            HeaderText = tieuDe,
            FillWeight = tyLe,
            ReadOnly = chiDoc,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };

        if (dinhDang is not null)
        {
            cot.DefaultCellStyle.Format = dinhDang;
        }

        if (canPhai)
        {
            cot.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            cot.DefaultCellStyle.Padding = new Padding(8, 4, 12, 4);

            // Tên cột phải căn cùng chiều với con số bên dưới, kèm đúng lề phải: cột tiền căn
            // phải mà tên cột nằm sát mép trái thì nhìn như hai cột khác nhau.
            cot.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            cot.HeaderCell.Style.Padding = new Padding(8, 0, 12, 0);
        }

        return cot;
    }

    /// <summary>
    /// Cho phép gõ "15.000" vào các cột số trên lưới mà vẫn hiểu đúng là 15000,
    /// và gõ được cả phép tính ngay trong ô: "3+2*4" ra 11.
    /// </summary>
    public static void ChoPhepGoSo(DataGridView luoi, params string[] thuocTinhSo)
    {
        luoi.CellParsing += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var thuocTinh = luoi.Columns[e.ColumnIndex].DataPropertyName;
            if (Array.IndexOf(thuocTinhSo, thuocTinh) < 0 || e.Value is not string chuoi)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(chuoi))
            {
                e.Value = 0m;
                e.ParsingApplied = true;
            }
            else if (So.TryTinh(chuoi, out var giaTri))
            {
                e.Value = giaTri;
                e.ParsingApplied = true;
            }
        };

        luoi.DataError += (_, e) =>
        {
            e.ThrowException = false;
            e.Cancel = true;
            HopThoai.CanhBao(
                luoi.FindForm(),
                "Giá trị vừa nhập không hợp lệ.\nVí dụ hợp lệ: 15000, 15.000, 2,5 — hoặc phép tính: 3+2*4");
        };
    }

    // ---------- Thanh tiêu đề của cửa sổ ----------

    /// <summary>
    /// Dải tiêu đề đầu mỗi cửa sổ con. Theo bản thiết kế thì nền trắng, chữ đen, kẻ một
    /// vạch dưới — thay cho dải xanh đậm cũ. Màu xanh giờ chỉ dành cho nút việc chính.
    /// </summary>
    /// <param name="tuCao">
    /// Cho thanh **tự cao theo chữ**, để đặt vào một dòng `AutoSize` của `TableLayoutPanel`.
    /// Dòng đặt cứng 92px là vừa khít ở cỡ hiển thị 100%: máy 125% là phụ đề bị cắt mất nửa
    /// dưới, đúng lỗi vừa gặp ở màn Nhập nhiều dòng.
    /// </param>
    public static Panel ThanhTieuDe(string tieuDe, string phuDe, bool tuCao = false)
    {
        // Xếp bằng neo trên chứ không đặt cứng toạ độ y = 14 và y = 52: cỡ chữ 19pt kèm dấu
        // tiếng Việt cao bao nhiêu thì tuỳ máy (Windows đặt cỡ chữ 125% là cao thêm một phần
        // tư). Đặt cứng thì tiêu đề tràn xuống đè lên phụ đề, mà tiêu đề nằm trên nên nó che
        // mất nửa trên của phụ đề — đúng chỗ vừa bị cắt.
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Trang, Padding = new Padding(24, 12, 24, 8) };
        if (tuCao)
        {
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        var lblTieuDe = new Label
        {
            Text = tieuDe,
            Font = FontTieuDe,
            ForeColor = ChuDam,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
        };

        var lblPhu = new Label
        {
            Text = phuDe,
            Font = FontPhu,
            ForeColor = Xam,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(2, 4, 0, 0),
        };

        // Neo trên thì cái thêm sau nằm trên: thêm phụ đề trước, tiêu đề sau.
        panel.Controls.Add(lblPhu);
        panel.Controls.Add(lblTieuDe);
        panel.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var but = new Pen(Vien);
            e.Graphics.DrawLine(but, 0, p.Height - 1, p.Width, p.Height - 1);
        };
        return panel;
    }
}
