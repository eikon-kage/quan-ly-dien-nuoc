using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Bộ hàng thường dùng: "Bộ lắp bồn nước" gồm 6 món, chọn một lần là ra đủ các dòng.
/// Vừa để quản lý, vừa để chọn khi đang nhập hàng cho khách.
/// </summary>
public sealed class BoHangForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly bool _deChon;

    private readonly BindingList<BoHang> _nguonBo = new();
    private readonly DataGridView _luoiBo = new();
    private BindingList<DongBoHang> _nguonMon = new();
    private readonly DataGridView _luoiMon = new();

    private readonly ComboBox _cboHang = new();
    private readonly TextBox _txtSoLuong = Theme.O(110);
    private readonly Label _lblTrangThai = new();

    private string? _anhChupTruocKhiSua;
    private bool _dangNap;

    public BoHangForm(bool deChon = false)
    {
        _deChon = deChon;

        Text = deChon ? "Chọn bộ hàng" : "Bộ hàng thường dùng";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1150, 720);
        MinimumSize = new Size(980, 600);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        NapDanhMucHang();
        NapBo(null);
    }

    /// <summary>Bộ hàng người dùng chọn, chỉ có giá trị khi mở ở chế độ chọn và bấm Dùng.</summary>
    public BoHang? BoDaChon { get; private set; }

    private BoHang? BoHienTai => _luoiBo.CurrentRow?.DataBoundItem as BoHang;

    // ---------------- Giao diện ----------------

    private void TaoGiaoDien()
    {
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "BỘ HÀNG THƯỜNG DÙNG",
                _deChon
                    ? "Chọn một bộ rồi bấm Dùng bộ này — các món sẽ được thêm vào hoá đơn đang mở."
                    : "Gom các món hay đi cùng nhau thành một bộ để khỏi phải nhập lại từng lần."),
            0,
            0);
        goc.Controls.Add(TaoThan(), 0, 1);
        goc.Controls.Add(TaoThanhDuoi(), 0, 2);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 3);

        Controls.Add(goc);
    }

    private Control TaoThan()
    {
        var than = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 10, 20, 6),
        };
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        than.Controls.Add(TaoCotBo(), 0, 0);
        than.Controls.Add(TaoCotMon(), 1, 0);
        return than;
    }

    private Control TaoCotBo()
    {
        var cot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
            Margin = new Padding(0, 0, 16, 0),
        };
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        cot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        var lbl = new Label
        {
            Text = "CÁC BỘ HÀNG",
            Font = Theme.FontDam,
            ForeColor = Theme.Xam,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        Theme.ApDungLuoi(_luoiBo);
        _luoiBo.ReadOnly = true;
        var cotSoMon = Theme.Cot("SoMon", "SỐ MÓN", 90, canPhai: true);
        cotSoMon.DataPropertyName = string.Empty;
        _luoiBo.Columns.AddRange(Theme.Cot(nameof(BoHang.Ten), "TÊN BỘ", 240), cotSoMon);
        _luoiBo.DataSource = _nguonBo;
        _luoiBo.SelectionChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapMon();
            }
        };
        _luoiBo.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1 && _luoiBo.Rows[e.RowIndex].DataBoundItem is BoHang bo)
            {
                e.Value = bo.Dong.Count;
                e.FormattingApplied = true;
            }
        };

        var btnThem = Theme.Nut("+  Bộ mới", Theme.Chinh, 150, 44);
        btnThem.Click += (_, _) => ThemBo();

        var viecBo = Theme.NutBaCham("Việc khác với bộ đang chọn", 44)
            .Viec("Đổi tên bộ", DoiTenBo)
            .Ngan()
            .Viec("Xoá bộ này", XoaBo, Theme.Do);

        var nut = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        nut.Controls.Add(btnThem);
        nut.Controls.Add(viecBo.Nut);

        cot.Controls.Add(lbl, 0, 0);
        cot.Controls.Add(Theme.Khung(_luoiBo), 0, 1);
        cot.Controls.Add(nut, 0, 2);
        return cot;
    }

    private Control TaoCotMon()
    {
        var cot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        cot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        var lbl = new Label
        {
            Text = "CÁC MÓN TRONG BỘ  —  sửa số lượng ngay trên lưới",
            Font = Theme.FontDam,
            ForeColor = Theme.Xam,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _cboHang.DropDownStyle = ComboBoxStyle.DropDown;
        _cboHang.Font = Theme.FontNhap;
        _txtSoLuong.Text = "1";

        var btnThemMon = Theme.Nut("+  Thêm món", Theme.Xanh, 170, 32);
        btnThemMon.Click += (_, _) => ThemMon();

        var thanhThem = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(14, 6, 14, 6) };
        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("TÊN HÀNG", _cboHang, 330));
        hang.Controls.Add(Theme.Truong("SỐ LƯỢNG", _txtSoLuong, 120));
        hang.Controls.Add(Theme.Truong(" ", btnThemMon, 170));
        thanhThem.Controls.Add(hang);

        Theme.ApDungLuoi(_luoiMon);
        _luoiMon.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoiMon.Columns.AddRange(
            Theme.Cot(nameof(DongBoHang.TenHang), "TÊN HÀNG", 320, chiDoc: false),
            Theme.Cot(nameof(DongBoHang.DonVi), "ĐƠN VỊ", 100, chiDoc: false),
            Theme.Cot(nameof(DongBoHang.SoLuong), "SỐ LƯỢNG", 120, "#,##0.##", canPhai: true, chiDoc: false));
        Theme.ChoPhepGoSo(_luoiMon, nameof(DongBoHang.SoLuong));
        _luoiMon.CellBeginEdit += (_, _) => _anhChupTruocKhiSua = _kho.ChupNhanh();
        _luoiMon.CellEndEdit += (_, _) =>
        {
            var anhChup = _anhChupTruocKhiSua;
            _anhChupTruocKhiSua = null;
            if (anhChup is null || anhChup == _kho.ChupNhanh())
            {
                return;
            }

            _kho.GhiNhan(anhChup, "Sửa bộ hàng", phatSuKien: false);
            _lblTrangThai.Text = "Đã lưu thay đổi.";
        };
        _luoiMon.DataSource = _nguonMon;

        var btnXoaMon = Theme.NutPhu("Xoá món (Delete)", 220, 44);
        btnXoaMon.ForeColor = Theme.Do;
        btnXoaMon.Click += (_, _) => XoaMon();

        var nut = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        nut.Controls.Add(btnXoaMon);

        cot.Controls.Add(lbl, 0, 0);
        cot.Controls.Add(thanhThem, 0, 1);
        cot.Controls.Add(Theme.Khung(_luoiMon), 0, 2);
        cot.Controls.Add(nut, 0, 3);
        return cot;
    }

    private Control TaoThanhDuoi()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 6, 20, 10) };
        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };

        if (_deChon)
        {
            var btnDung = Theme.Nut("DÙNG BỘ NÀY", Theme.Xanh, 250, 52);
            btnDung.Click += (_, _) => Dung();
            hang.Controls.Add(btnDung);
        }

        var btnDong = Theme.NutPhu("Đóng (Esc)", 160, 52);
        btnDong.Click += (_, _) => Close();
        hang.Controls.Add(btnDong);

        nen.Controls.Add(hang);
        return nen;
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);
        _lblTrangThai.Text = "Bấm đúp vào ô để sửa · Delete xoá món đang chọn";

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        return nen;
    }

    // ---------------- Nạp dữ liệu ----------------

    private void NapDanhMucHang()
    {
        _cboHang.Items.Clear();
        foreach (var vatTu in _kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            _cboHang.Items.Add(vatTu);
        }
    }

    private void NapBo(Guid? chon)
    {
        _dangNap = true;
        _nguonBo.RaiseListChangedEvents = false;
        _nguonBo.Clear();
        foreach (var bo in _kho.DuLieu.BoHangs.OrderBy(b => b.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            _nguonBo.Add(bo);
        }

        _nguonBo.RaiseListChangedEvents = true;
        _nguonBo.ResetBindings();

        if (chon is { } id)
        {
            for (var i = 0; i < _luoiBo.Rows.Count; i++)
            {
                if (_luoiBo.Rows[i].DataBoundItem is BoHang bo && bo.Id == id)
                {
                    _luoiBo.CurrentCell = _luoiBo.Rows[i].Cells[0];
                    break;
                }
            }
        }

        _dangNap = false;
        NapMon();
    }

    private void NapMon()
    {
        _nguonMon = new BindingList<DongBoHang>(BoHienTai?.Dong ?? new List<DongBoHang>());
        _luoiMon.DataSource = _nguonMon;
        _luoiMon.ReadOnly = BoHienTai is null;
    }

    // ---------------- Thao tác ----------------

    private void ThemBo()
    {
        if (NhapChuoiForm.Hoi(this, "Bộ hàng mới", "TÊN BỘ HÀNG (ví dụ: Bộ lắp bồn nước)") is not { } ten)
        {
            return;
        }

        var bo = new BoHang { Ten = ten };
        _kho.ThucHien($"Tạo bộ hàng {ten}", () => _kho.DuLieu.BoHangs.Add(bo), phatSuKien: false);
        NapBo(bo.Id);
        _lblTrangThai.Text = $"Đã tạo bộ \"{ten}\". Thêm các món vào bên phải.";
    }

    private void DoiTenBo()
    {
        if (BoHienTai is not { } bo)
        {
            HopThoai.CanhBao(this, "Hãy chọn một bộ hàng.");
            return;
        }

        if (NhapChuoiForm.Hoi(this, "Đổi tên bộ hàng", "TÊN BỘ HÀNG", bo.Ten) is not { } ten)
        {
            return;
        }

        _kho.ThucHien($"Đổi tên bộ hàng thành {ten}", () => bo.Ten = ten, phatSuKien: false);
        NapBo(bo.Id);
    }

    private void XoaBo()
    {
        if (BoHienTai is not { } bo)
        {
            HopThoai.CanhBao(this, "Hãy chọn một bộ hàng để xoá.");
            return;
        }

        if (!HopThoai.Hoi(this, $"Xoá bộ hàng \"{bo.Ten}\"?\n\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá bộ hàng {bo.Ten}", () => _kho.DuLieu.BoHangs.Remove(bo), phatSuKien: false);
        NapBo(null);
        _lblTrangThai.Text = $"Đã xoá bộ \"{bo.Ten}\".";
    }

    private void ThemMon()
    {
        if (BoHienTai is not { } bo)
        {
            HopThoai.CanhBao(this, "Hãy chọn (hoặc tạo) một bộ hàng trước.");
            return;
        }

        var ten = _cboHang.Text.Trim();
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy chọn hoặc gõ tên hàng.");
            _cboHang.Focus();
            return;
        }

        var soLuong = So.Tinh(_txtSoLuong.Text);
        if (soLuong <= 0)
        {
            soLuong = 1m;
        }

        var vatTu = _cboHang.SelectedItem as VatTu ?? _kho.TimVatTuTheoTen(ten);
        var mon = new DongBoHang
        {
            VatTuId = vatTu?.Id,
            TenHang = vatTu?.Ten ?? ten,
            DonVi = vatTu?.DonVi ?? string.Empty,
            SoLuong = soLuong,
        };

        _kho.ThucHien($"Thêm \"{mon.TenHang}\" vào bộ {bo.Ten}", () => bo.Dong.Add(mon), phatSuKien: false);
        NapBo(bo.Id);

        _cboHang.SelectedIndex = -1;
        _cboHang.Text = string.Empty;
        _txtSoLuong.Text = "1";
        _cboHang.Focus();
        _lblTrangThai.Text = $"Đã thêm {mon.TenHang} × {So.Luong(soLuong)}.";
    }

    private void XoaMon()
    {
        if (BoHienTai is not { } bo || _luoiMon.CurrentRow?.DataBoundItem is not DongBoHang mon)
        {
            HopThoai.CanhBao(this, "Hãy chọn món cần xoá.");
            return;
        }

        _kho.ThucHien($"Xoá \"{mon.TenHang}\" khỏi bộ {bo.Ten}", () => bo.Dong.Remove(mon), phatSuKien: false);
        NapBo(bo.Id);
        _lblTrangThai.Text = $"Đã xoá {mon.TenHang}. Bấm Ctrl+Z để lấy lại.";
    }

    private void Dung()
    {
        if (BoHienTai is not { } bo)
        {
            HopThoai.CanhBao(this, "Hãy chọn một bộ hàng.");
            return;
        }

        if (bo.Dong.Count == 0)
        {
            HopThoai.CanhBao(this, "Bộ này chưa có món nào.");
            return;
        }

        BoDaChon = bo;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var dangSuaO = _luoiMon.IsCurrentCellInEditMode;

        switch (keyData)
        {
            case Keys.Escape when !dangSuaO:
                Close();
                return true;
            case Keys.Delete when !dangSuaO && _luoiMon.Focused:
                XoaMon();
                return true;
            case Keys.Control | Keys.Z when !dangSuaO:
                _kho.HoanTac();
                NapBo(BoHienTai?.Id);
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
