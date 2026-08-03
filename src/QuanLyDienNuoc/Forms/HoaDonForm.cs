using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Hộp thoại tạo mới / sửa thông tin chung của một hoá đơn.</summary>
public sealed class HoaDonForm : Form
{
    private readonly TextBox _txtMa = Theme.O(240);
    private readonly DateTimePicker _dtNgayMo = new() { Format = DateTimePickerFormat.Short, Font = Theme.FontNhap };
    private readonly TextBox _txtGhiChu = new()
    {
        Font = Theme.FontNhap,
        BorderStyle = BorderStyle.FixedSingle,
        Multiline = true,
        Height = 90,
        Width = 460,
    };

    public HoaDonForm(HoaDon? goc, string maMacDinh, int nam)
    {
        Text = goc is null ? "Tạo hoá đơn mới" : "Sửa hoá đơn";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(540, 470);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        _txtMa.Text = goc?.MaHoaDon ?? maMacDinh;
        _dtNgayMo.Value = goc?.NgayMo ?? DateTime.Today;
        _txtGhiChu.Text = goc?.GhiChu ?? string.Empty;

        TaoGiaoDien(goc, nam);
    }

    public ThongTinHoaDon? KetQua { get; private set; }

    private void TaoGiaoDien(HoaDon? goc, int nam)
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
        };
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

        khung.Controls.Add(
            Theme.ThanhTieuDe(goc is null ? "TẠO HOÁ ĐƠN MỚI" : "SỬA HOÁ ĐƠN", $"Hoá đơn thuộc năm {nam}"),
            0,
            0);

        var than = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(24, 18, 24, 0),
            BackColor = Theme.Nen,
        };

        var truongGhiChu = new Panel { Width = 460, Height = 124 };
        truongGhiChu.Controls.Add(new Label
        {
            Text = "GHI CHÚ",
            Font = Theme.FontNhan,
            ForeColor = Theme.Xam,
            Location = new Point(0, 0),
            Size = new Size(460, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        });
        _txtGhiChu.Location = new Point(0, 26);
        truongGhiChu.Controls.Add(_txtGhiChu);

        than.Controls.Add(Theme.Truong("MÃ HOÁ ĐƠN", _txtMa, 240));
        than.Controls.Add(Theme.Truong("NGÀY MỞ HOÁ ĐƠN", _dtNgayMo, 240));
        than.Controls.Add(truongGhiChu);

        var btnLuu = Theme.Nut("LƯU", Theme.Xanh, 160, 48);
        btnLuu.Click += (_, _) => Luu();

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

        khung.Controls.Add(than, 0, 1);
        khung.Controls.Add(duoi, 0, 2);
        Controls.Add(khung);

        AcceptButton = btnLuu;
        CancelButton = btnHuy;
        ActiveControl = _txtMa;
    }

    private void Luu()
    {
        var ma = _txtMa.Text.Trim();
        if (ma.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập mã hoá đơn.");
            _txtMa.Focus();
            return;
        }

        KetQua = new ThongTinHoaDon(ma, _dtNgayMo.Value.Date, _txtGhiChu.Text.Trim());
        DialogResult = DialogResult.OK;
    }

    public sealed record ThongTinHoaDon(string MaHoaDon, DateTime NgayMo, string GhiChu);
}
