namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Một ô số liệu trong thẻ tổng quan, xếp theo bản thiết kế: tên nhóm tô màu ở trên,
/// con số to ở giữa, chú thích mờ ở dưới. Nhiều ô nằm cạnh nhau, ngăn nhau bằng vạch dọc.
/// </summary>
public sealed class OThongKe : Panel
{
    private static readonly Font FontGiaTri = new("Segoe UI", 17F, FontStyle.Bold);

    private readonly Label _lblGiaTri;
    private readonly Label _lblChuThich;

    public OThongKe(string nhan, Color mauNhan)
    {
        BackColor = Theme.Trang;
        Padding = new Padding(4, 0, 4, 0);

        var lblNhan = new Label
        {
            Text = nhan,
            Font = Theme.FontDam,
            ForeColor = mauNhan,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _lblGiaTri = new Label
        {
            Text = "0",
            Font = FontGiaTri,
            ForeColor = Theme.ChuDam,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _lblChuThich = new Label
        {
            Text = string.Empty,
            Font = Theme.FontPhu,
            ForeColor = Theme.XamNhat,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        // Neo theo thứ tự thêm vào: chú thích thêm trước nên nằm dưới cùng.
        Controls.Add(_lblChuThich);
        Controls.Add(_lblGiaTri);
        Controls.Add(lblNhan);

        Paint += (_, e) =>
        {
            if (!CoVachPhai)
            {
                return;
            }

            using var but = new Pen(Theme.Vien);
            e.Graphics.DrawLine(but, Width - 1, 6, Width - 1, Height - 6);
        };
    }

    /// <summary>Con số to ở giữa ô.</summary>
    public string GiaTri
    {
        get => _lblGiaTri.Text;
        set => _lblGiaTri.Text = value;
    }

    /// <summary>Màu của con số — mặc định màu chữ thường, số nợ thì cho đỏ.</summary>
    public Color MauGiaTri
    {
        get => _lblGiaTri.ForeColor;
        set => _lblGiaTri.ForeColor = value;
    }

    /// <summary>Dòng chữ mờ dưới con số.</summary>
    public string ChuThich
    {
        get => _lblChuThich.Text;
        set => _lblChuThich.Text = value;
    }

    /// <summary>Có kẻ vạch dọc ngăn với ô bên phải hay không (ô cuối thì không).</summary>
    public bool CoVachPhai { get; set; } = true;
}
