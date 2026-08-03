using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Danh mục vật tư chung của cửa hàng và giá chung của từng mặt hàng.</summary>
public sealed class VatTuForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<VatTu> _nguon = new();
    private readonly TextBox _txtTim = Theme.O(360);

    private readonly TextBox _txtTen = Theme.O(320);
    private readonly TextBox _txtDonVi = Theme.O(120);
    private readonly TextBox _txtGia = Theme.O(160);
    private readonly Label _lblTrangThai = new();

    private string? _anhChupTruocKhiSua;

    public VatTuForm()
    {
        Text = "Danh mục vật tư";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1060, 720);
        MinimumSize = new Size(940, 620);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "DANH MỤC VẬT TƯ",
                "Đây là giá chung. Giá của từng khách đặt riêng trong màn hình đơn hàng của khách đó."),
            0,
            0);

        // Thêm nhanh
        var nenThem = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(14, 8, 14, 8) };
        var btnThem = Theme.Nut("+  THÊM VẬT TƯ", Theme.Xanh, 200, 34);
        btnThem.Click += (_, _) => Them();
        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("TÊN HÀNG", _txtTen, 340));
        hang.Controls.Add(Theme.Truong("ĐƠN VỊ", _txtDonVi, 130));
        hang.Controls.Add(Theme.Truong("GIÁ CHUNG", _txtGia, 170));
        hang.Controls.Add(Theme.Truong(" ", btnThem, 200));
        nenThem.Controls.Add(hang);

        foreach (var o in new[] { _txtTen, _txtDonVi, _txtGia })
        {
            o.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Them();
                }
            };
        }

        // Tìm kiếm
        _txtTim.TextChanged += (_, _) => Nap();
        var thanhTim = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 10, 20, 0) };
        var traiTim = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        traiTim.Controls.Add(Theme.Truong("TÌM VẬT TƯ", _txtTim, 380));
        thanhTim.Controls.Add(traiTim);

        // Lưới
        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(VatTu.Ten), "TÊN HÀNG", 320, chiDoc: false),
            Theme.Cot(nameof(VatTu.DonVi), "ĐƠN VỊ", 110, chiDoc: false),
            Theme.Cot(nameof(VatTu.DonGiaMacDinh), "GIÁ CHUNG", 150, "#,##0", canPhai: true, chiDoc: false));
        Theme.ChoPhepGoSo(_luoi, nameof(VatTu.DonGiaMacDinh));
        _luoi.CellBeginEdit += (_, _) => _anhChupTruocKhiSua = _kho.ChupNhanh();
        _luoi.CellEndEdit += Luoi_CellEndEdit;
        _luoi.DataSource = _nguon;

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(_luoi));

        // Thanh dưới
        var btnXoa = Theme.NutPhu("Xoá vật tư", 170, 46);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => Xoa();

        var btnDong = Theme.NutPhu("Đóng", 140, 46);
        btnDong.Click += (_, _) => Close();

        var traiDuoi = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        traiDuoi.Controls.Add(btnXoa);
        traiDuoi.Controls.Add(btnDong);

        _lblTrangThai.Dock = DockStyle.Right;
        _lblTrangThai.Width = 520;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleRight;
        _lblTrangThai.Text = "Bấm đúp vào ô để sửa · Ctrl+Z hoàn tác";

        var nenDuoi = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 12, 20, 10) };
        nenDuoi.Controls.Add(traiDuoi);
        nenDuoi.Controls.Add(_lblTrangThai);

        khung.Controls.Add(nenThem, 0, 1);
        khung.Controls.Add(thanhTim, 0, 2);
        khung.Controls.Add(vienLuoi, 0, 3);
        khung.Controls.Add(nenDuoi, 0, 4);
        Controls.Add(khung);

        ActiveControl = _txtTen;
    }

    private void Nap(Guid? chon = null)
    {
        var dangChon = chon ?? (_luoi.CurrentRow?.DataBoundItem as VatTu)?.Id;

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var vatTu in _kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            if (ChuViet.Chua(vatTu.Ten, _txtTim.Text))
            {
                _nguon.Add(vatTu);
            }
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            for (var i = 0; i < _luoi.Rows.Count; i++)
            {
                if (_luoi.Rows[i].DataBoundItem is VatTu vatTu && vatTu.Id == id)
                {
                    _luoi.CurrentCell = _luoi.Rows[i].Cells[0];
                    break;
                }
            }
        }
    }

    private void Them()
    {
        var ten = _txtTen.Text.Trim();
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập tên hàng.");
            _txtTen.Focus();
            return;
        }

        if (_kho.TimVatTuTheoTen(ten) is not null)
        {
            HopThoai.CanhBao(this, $"Đã có mặt hàng \"{ten}\" trong danh mục.");
            return;
        }

        var moi = new VatTu
        {
            Ten = ten,
            DonVi = _txtDonVi.Text.Trim(),
            DonGiaMacDinh = So.Doc(_txtGia.Text),
        };

        _kho.ThucHien($"Thêm vật tư \"{ten}\"", () => _kho.DuLieu.VatTus.Add(moi), phatSuKien: false);

        _txtTen.Clear();
        _txtDonVi.Clear();
        _txtGia.Clear();
        _txtTen.Focus();

        Nap(moi.Id);
        _lblTrangThai.Text = $"Đã thêm \"{ten}\"";
    }

    private void Xoa()
    {
        if (_luoi.CurrentRow?.DataBoundItem is not VatTu vatTu)
        {
            HopThoai.CanhBao(this, "Hãy chọn một mặt hàng để xoá.");
            return;
        }

        if (!HopThoai.Hoi(
                this,
                $"Xoá \"{vatTu.Ten}\" khỏi danh mục?\n\n" +
                "Các dòng hàng đã ghi trong hoá đơn vẫn giữ nguyên tên và giá cũ.\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá vật tư \"{vatTu.Ten}\"", () =>
        {
            _kho.DuLieu.VatTus.Remove(vatTu);
            foreach (var khach in _kho.DuLieu.KhachHangs)
            {
                khach.BangGiaRieng.Remove(vatTu.Id);
            }
        }, phatSuKien: false);

        Nap();
        _lblTrangThai.Text = $"Đã xoá \"{vatTu.Ten}\". Bấm Ctrl+Z để lấy lại.";
    }

    private void Luoi_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (anhChup is null || anhChup == _kho.ChupNhanh())
        {
            return;
        }

        _kho.GhiNhan(anhChup, "Sửa danh mục vật tư", phatSuKien: false);
        _lblTrangThai.Text = "Đã lưu thay đổi. Bấm Ctrl+Z nếu muốn quay lại.";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_luoi.IsCurrentCellInEditMode)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Z:
                    _lblTrangThai.Text = _kho.HoanTac() is { } moTa ? $"Đã hoàn tác: {moTa}" : "Không còn gì để hoàn tác.";
                    Nap();
                    return true;
                case Keys.Control | Keys.Y:
                    _lblTrangThai.Text = _kho.LamLai() is { } moTaLai ? $"Đã làm lại: {moTaLai}" : "Không còn gì để làm lại.";
                    Nap();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
