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
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

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
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "NHẬT KÝ THAY ĐỔI",
                "Mọi lần thêm, sửa, xoá đều được ghi lại kèm giờ.",
                tuCao: true),
            0,
            0);

        _txtTim.TextChanged += (_, _) => Nap();
        goc.Controls.Add(Theme.HangO(Theme.Nen, Theme.Truong("TÌM TRONG NHẬT KÝ", _txtTim, 380)), 0, 1);

        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongLuoi.Luc), "LÚC", 175, "dd/MM/yyyy HH:mm:ss", toiThieu: 176),
            Theme.Cot(nameof(DongLuoi.MoTa), "THAY ĐỔI", 320),
            Theme.Cot(nameof(DongLuoi.ChiTiet), "CHI TIẾT", 300));
        _luoi.DataSource = _nguon;

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 10), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        goc.Controls.Add(vien, 0, 2);

        var btnMoFile = Theme.NutPhu("Mở file nhật ký", 220, 48, noTheoChu: true);
        btnMoFile.Click += (_, _) => MoFile();

        var btnLamMoi = Theme.NutPhu("Nạp lại", 140, 48, noTheoChu: true);
        btnLamMoi.Click += (_, _) => Nap();

        var btnDong = Theme.NutPhu("Đóng", 120, 48, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        goc.Controls.Add(Theme.ThanhDuoi(null, btnMoFile, btnLamMoi, btnDong), 0, 3);

        goc.Controls.Add(Theme.ThanhTrangThai(_lblTrangThai), 0, 4);

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
