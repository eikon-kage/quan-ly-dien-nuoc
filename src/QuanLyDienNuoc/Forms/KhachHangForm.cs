using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Hộp thoại thêm mới / sửa thông tin một khách hàng.</summary>
public sealed class KhachHangForm : Form
{
    private readonly TextBox _txtTen = Theme.O(520);
    private readonly TextBox _txtDienThoai = Theme.O(250);
    private readonly TextBox _txtDiaChi = Theme.O(520);
    private readonly TextBox _txtGhiChu = new()
    {
        Font = Theme.FontNhap,
        BorderStyle = BorderStyle.FixedSingle,
        Multiline = true,
        Height = 90,
        Width = 520,
    };

    public KhachHangForm(KhachHang? goc)
    {
        Text = goc is null ? "Thêm khách hàng" : "Sửa khách hàng";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(600, 560);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        if (goc is not null)
        {
            _txtTen.Text = goc.Ten;
            _txtDienThoai.Text = goc.DienThoai;
            _txtDiaChi.Text = goc.DiaChi;
            _txtGhiChu.Text = goc.GhiChu;
        }

        TaoGiaoDien(goc);
    }

    /// <summary>Thông tin khách sau khi bấm Lưu.</summary>
    public KhachHang? KetQua { get; private set; }

    private void TaoGiaoDien(KhachHang? goc)
    {
        var goc2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
        };
        goc2.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        goc2.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc2.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

        goc2.Controls.Add(
            Theme.ThanhTieuDe(goc is null ? "THÊM KHÁCH HÀNG" : "SỬA KHÁCH HÀNG", "Chỉ tên khách là bắt buộc"),
            0,
            0);

        var than = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(24, 18, 24, 0),
            BackColor = Theme.Nen,
        };

        var truongGhiChu = new Panel { Width = 520, Height = 124, Margin = new Padding(0, 0, 0, 6) };
        truongGhiChu.Controls.Add(new Label
        {
            Text = "GHI CHÚ",
            Font = Theme.FontNhan,
            ForeColor = Theme.Xam,
            Location = new Point(0, 0),
            Size = new Size(520, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        });
        _txtGhiChu.Location = new Point(0, 26);
        truongGhiChu.Controls.Add(_txtGhiChu);

        than.Controls.Add(Theme.Truong("TÊN KHÁCH HÀNG *", _txtTen, 520));
        than.Controls.Add(Theme.Truong("ĐIỆN THOẠI", _txtDienThoai, 520));
        than.Controls.Add(Theme.Truong("ĐỊA CHỈ", _txtDiaChi, 520));
        than.Controls.Add(truongGhiChu);

        var btnLuu = Theme.Nut("LƯU", Theme.Xanh, 160, 48);
        btnLuu.Click += (_, _) => Luu(goc);

        var btnHuy = Theme.NutPhu("Huỷ", 140, 48);
        btnHuy.Click += (_, _) => DialogResult = DialogResult.Cancel;

        var duoi = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 14, 24, 0),
            BackColor = Theme.Nen,
        };
        duoi.Controls.Add(btnLuu);
        duoi.Controls.Add(btnHuy);

        goc2.Controls.Add(than, 0, 1);
        goc2.Controls.Add(duoi, 0, 2);
        Controls.Add(goc2);

        AcceptButton = btnLuu;
        CancelButton = btnHuy;
        ActiveControl = _txtTen;
    }

    private void Luu(KhachHang? goc)
    {
        var ten = _txtTen.Text.Trim();
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập tên khách hàng.");
            _txtTen.Focus();
            return;
        }

        KetQua = new KhachHang
        {
            Id = goc?.Id ?? Guid.NewGuid(),
            Ten = ten,
            DienThoai = _txtDienThoai.Text.Trim(),
            DiaChi = _txtDiaChi.Text.Trim(),
            GhiChu = _txtGhiChu.Text.Trim(),
            NgayTao = goc?.NgayTao ?? DateTime.Today,
            BangGiaRieng = goc?.BangGiaRieng ?? new Dictionary<Guid, decimal>(),
        };

        DialogResult = DialogResult.OK;
    }
}
