using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Nhật ký thay đổi: lúc nào sửa gì. Ghi ra file riêng nên hoàn tác không xoá mất —
/// khách thắc mắc "sao hôm trước giá khác" là có chỗ tra lại.
/// </summary>
public sealed class NhatKyForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly BindingList<DongLuoi> _nguon = new();

    private readonly TextBox _txtTim = Theme.O(360);
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTrangThai = new();

    public NhatKyForm()
    {
        Text = "Nhật ký thay đổi";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1000, 720);
        MinimumSize = new Size(820, 560);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    private void TaoGiaoDien()
    {
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(
            Theme.ThanhTieuDe("NHẬT KÝ THAY ĐỔI", "Mọi lần thêm, sửa, xoá đều được ghi lại kèm giờ."),
            0,
            0);

        _txtTim.TextChanged += (_, _) => Nap();
        var thanhTim = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 8, 20, 4) };
        var hang = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        hang.Controls.Add(Theme.Truong("TÌM TRONG NHẬT KÝ", _txtTim, 380));
        thanhTim.Controls.Add(hang);
        goc.Controls.Add(thanhTim, 0, 1);

        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongLuoi.Luc), "LÚC", 150, "dd/MM/yyyy HH:mm:ss"),
            Theme.Cot(nameof(DongLuoi.MoTa), "THAY ĐỔI", 320),
            Theme.Cot(nameof(DongLuoi.ChiTiet), "CHI TIẾT", 300));
        _luoi.DataSource = _nguon;

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 10), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        goc.Controls.Add(vien, 0, 2);

        var btnMoFile = Theme.NutPhu("Mở file nhật ký", 220, 48);
        btnMoFile.Click += (_, _) => MoFile();

        var btnLamMoi = Theme.NutPhu("Nạp lại", 140, 48);
        btnLamMoi.Click += (_, _) => Nap();

        var btnDong = Theme.NutPhu("Đóng (Esc)", 150, 48);
        btnDong.Click += (_, _) => Close();

        var nut = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(20, 6, 20, 6) };
        nut.Controls.Add(btnMoFile);
        nut.Controls.Add(btnLamMoi);
        nut.Controls.Add(btnDong);
        goc.Controls.Add(nut, 0, 3);

        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);
        var nenTrangThai = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nenTrangThai.Controls.Add(_lblTrangThai);
        goc.Controls.Add(nenTrangThai, 0, 4);

        Controls.Add(goc);
    }

    private void Nap()
    {
        var tuKhoa = _txtTim.Text;
        var muc = _kho.NhatKy.Doc(2000)
            .Where(m => ChuViet.Chua(m.MoTa, tuKhoa) || ChuViet.Chua(m.ChiTiet, tuKhoa))
            .ToList();

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        foreach (var m in muc)
        {
            _nguon.Add(new DongLuoi { Luc = m.Luc, MoTa = m.MoTa, ChiTiet = m.ChiTiet });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        _lblTrangThai.Text = $"{muc.Count} mục   ·   file: {_kho.NhatKy.DuongDanFile}";
    }

    private void MoFile()
    {
        if (!File.Exists(_kho.NhatKy.DuongDanFile))
        {
            HopThoai.CanhBao(this, "Chưa có nhật ký nào được ghi.");
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_kho.NhatKy.DuongDanFile)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            HopThoai.CanhBao(this, "Không mở được file:\n" + ex.Message);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private sealed class DongLuoi
    {
        public DateTime Luc { get; set; }

        public string MoTa { get; set; } = string.Empty;

        public string ChiTiet { get; set; } = string.Empty;
    }
}
