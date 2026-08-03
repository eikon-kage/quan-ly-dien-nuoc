namespace QuanLyDienNuoc.Ui;

/// <summary>Màu, phông chữ và các mảnh giao diện dùng chung. Chữ to, dòng thưa cho dễ nhìn.</summary>
public static class Theme
{
    /// <summary>
    /// Kiểu ngày dùng cho mọi ô chọn ngày. Phải ép tay vì DateTimePicker lấy định dạng
    /// theo Windows chứ không theo ngôn ngữ phần mềm đặt — máy cài Windows tiếng Anh sẽ
    /// hiện 8/3/2026 thay vì 03/08/2026, rất dễ nhập nhầm ngày.
    /// </summary>
    public const string DangNgay = "dd/MM/yyyy";

    public static readonly Color Nen = Color.FromArgb(242, 245, 249);
    public static readonly Color Trang = Color.White;
    public static readonly Color Chinh = Color.FromArgb(21, 101, 192);
    public static readonly Color ChinhNhat = Color.FromArgb(232, 240, 251);
    public static readonly Color Xanh = Color.FromArgb(46, 125, 50);
    public static readonly Color Cam = Color.FromArgb(216, 122, 20);
    public static readonly Color Do = Color.FromArgb(198, 40, 40);
    public static readonly Color Xam = Color.FromArgb(96, 105, 118);
    public static readonly Color Vien = Color.FromArgb(214, 221, 230);
    public static readonly Color Chu = Color.FromArgb(28, 33, 40);

    public static readonly Font FontNhan = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontPhu = new("Segoe UI", 11F);
    public static readonly Font FontThuong = new("Segoe UI", 12F);
    public static readonly Font FontDam = new("Segoe UI", 12F, FontStyle.Bold);
    public static readonly Font FontNhap = new("Segoe UI", 13F);
    public static readonly Font FontLuoi = new("Segoe UI", 12.5F);
    public static readonly Font FontLuoiDam = new("Segoe UI", 12.5F, FontStyle.Bold);
    public static readonly Font FontSo = new("Segoe UI", 15F, FontStyle.Bold);
    public static readonly Font FontTieuDe = new("Segoe UI", 19F, FontStyle.Bold);

    // ---------- Nút ----------

    public static Button Nut(string chu, Color mau, int rong = 200, int cao = 46)
    {
        var nut = new Button
        {
            Text = chu,
            Width = rong,
            Height = cao,
            BackColor = mau,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = FontDam,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0, 0, 10, 0),
        };
        nut.FlatAppearance.BorderSize = 0;
        nut.FlatAppearance.MouseOverBackColor = ControlPaint.Light(mau, 0.2f);
        nut.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(mau, 0.05f);
        return nut;
    }

    public static Button NutPhu(string chu, int rong = 180, int cao = 46)
    {
        var nut = new Button
        {
            Text = chu,
            Width = rong,
            Height = cao,
            BackColor = Trang,
            ForeColor = Chinh,
            FlatStyle = FlatStyle.Flat,
            Font = FontDam,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0, 0, 10, 0),
        };
        nut.FlatAppearance.BorderSize = 1;
        nut.FlatAppearance.BorderColor = Vien;
        nut.FlatAppearance.MouseOverBackColor = ChinhNhat;
        return nut;
    }

    // ---------- Ô nhập ----------

    public static TextBox O(int rong)
    {
        return new TextBox
        {
            Width = rong,
            Font = FontNhap,
            BorderStyle = BorderStyle.FixedSingle,
        };
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

    /// <summary>Một ô nhập có nhãn nằm phía trên, gộp trong một panel để xếp bằng FlowLayoutPanel.</summary>
    public static Panel Truong(string nhan, Control dieuKhien, int rong)
    {
        var panel = new Panel
        {
            Width = rong,
            Height = 66,
            Margin = new Padding(0, 0, 14, 0),
        };

        var lbl = new Label
        {
            Text = nhan,
            Font = FontNhan,
            ForeColor = Xam,
            Location = new Point(0, 0),
            Size = new Size(rong, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        dieuKhien.Location = new Point(0, 26);
        dieuKhien.Width = rong;
        dieuKhien.Height = 32;

        panel.Controls.Add(lbl);
        panel.Controls.Add(dieuKhien);
        return panel;
    }

    // ---------- Lưới ----------

    public static void ApDungLuoi(DataGridView luoi)
    {
        luoi.BackgroundColor = Trang;
        luoi.BorderStyle = BorderStyle.None;
        luoi.EnableHeadersVisualStyles = false;
        luoi.ColumnHeadersDefaultCellStyle.BackColor = Chinh;
        luoi.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        luoi.ColumnHeadersDefaultCellStyle.SelectionBackColor = Chinh;
        luoi.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        luoi.ColumnHeadersDefaultCellStyle.Font = FontLuoiDam;
        luoi.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        luoi.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        luoi.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        luoi.ColumnHeadersHeight = 50;
        luoi.RowTemplate.Height = 44;
        luoi.DefaultCellStyle.Font = FontLuoi;
        luoi.DefaultCellStyle.ForeColor = Chu;
        luoi.DefaultCellStyle.BackColor = Trang;
        luoi.DefaultCellStyle.SelectionBackColor = Color.FromArgb(198, 220, 246);
        luoi.DefaultCellStyle.SelectionForeColor = Chu;
        luoi.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        luoi.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 254);
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

    public static Panel ThanhTieuDe(string tieuDe, string phuDe)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Chinh };

        var lblTieuDe = new Label
        {
            Text = tieuDe,
            Font = FontTieuDe,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(24, 16),
        };

        var lblPhu = new Label
        {
            Text = phuDe,
            Font = FontPhu,
            ForeColor = Color.FromArgb(205, 224, 247),
            AutoSize = true,
            Location = new Point(26, 52),
        };

        panel.Controls.Add(lblTieuDe);
        panel.Controls.Add(lblPhu);
        return panel;
    }

    public static Panel Khung(Control noiDung)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Trang,
            Padding = new Padding(1),
            Margin = new Padding(0),
        };
        panel.Controls.Add(noiDung);
        panel.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Vien, ButtonBorderStyle.Solid);
        };
        return panel;
    }
}
