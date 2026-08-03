using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Màn hình chính: danh sách khách hàng theo năm, mở ra đơn hàng của từng khách.</summary>
public sealed class MainForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly BindingList<DongKhach> _nguon = new();

    private readonly ComboBox _cboNam = new();
    private readonly TextBox _txtTim = Theme.O(320);
    private readonly CheckBox _chkCoDon = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTongKet = new();
    private readonly Label _lblTrangThai = new();
    private readonly Button _btnHoanTac = Theme.NutPhu("↶  Hoàn tác", 160);
    private readonly Button _btnLamLai = Theme.NutPhu("↷  Làm lại", 150);

    private bool _dangNap;

    public MainForm()
    {
        Text = "Quản lý đơn hàng – Cửa hàng điện nước";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 720);
        Size = new Size(1440, 860);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        _kho.DuLieuThayDoi += Kho_DuLieuThayDoi;
        FormClosed += (_, _) => _kho.DuLieuThayDoi -= Kho_DuLieuThayDoi;

        NapNam();
        NapDanhSach();
    }

    private int NamDangChon => _cboNam.SelectedItem is int nam ? nam : DateTime.Today.Year;

    private KhachHang? KhachDangChon => (_luoi.CurrentRow?.DataBoundItem as DongKhach)?.Khach;

    // ---------------- Giao diện ----------------

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
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(Theme.ThanhTieuDe("QUẢN LÝ ĐƠN HÀNG – CỬA HÀNG ĐIỆN NƯỚC", "Chọn năm, chọn khách hàng rồi bấm Mở đơn hàng (hoặc bấm đúp vào dòng khách)"), 0, 0);
        goc.Controls.Add(TaoThanhCongCu(), 0, 1);
        goc.Controls.Add(TaoLuoi(), 0, 2);
        goc.Controls.Add(TaoThanhDuoi(), 0, 3);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 4);

        Controls.Add(goc);
    }

    private Control TaoThanhCongCu()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 12, 20, 8) };

        _cboNam.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNam.Font = Theme.FontNhap;
        _cboNam.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        _txtTim.TextChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        _chkCoDon.Text = "Chỉ hiện khách có đơn trong năm";
        _chkCoDon.Font = Theme.FontThuong;
        _chkCoDon.AutoSize = true;
        _chkCoDon.Margin = new Padding(4, 34, 0, 0);
        _chkCoDon.CheckedChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        var benTrai = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
        };
        benTrai.Controls.Add(Theme.Truong("NĂM", _cboNam, 130));
        benTrai.Controls.Add(Theme.Truong("TÌM KHÁCH HÀNG (tên, số điện thoại, địa chỉ)", _txtTim, 380));
        benTrai.Controls.Add(_chkCoDon);

        var btnVatTu = Theme.NutPhu("Danh mục vật tư", 190);
        btnVatTu.Click += (_, _) => MoDanhMucVatTu();

        var btnThemKhach = Theme.Nut("+  Thêm khách hàng", Theme.Xanh, 220);
        btnThemKhach.Click += (_, _) => ThemKhach();

        _btnHoanTac.Click += (_, _) => HoanTac();
        _btnLamLai.Click += (_, _) => LamLai();

        var benPhai = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 22, 0, 0),
        };
        benPhai.Controls.Add(btnVatTu);
        benPhai.Controls.Add(btnThemKhach);
        benPhai.Controls.Add(_btnLamLai);
        benPhai.Controls.Add(_btnHoanTac);

        nen.Controls.Add(benTrai);
        nen.Controls.Add(benPhai);
        return nen;
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongKhach.Ten), "KHÁCH HÀNG", 200),
            Theme.Cot(nameof(DongKhach.DienThoai), "ĐIỆN THOẠI", 110),
            Theme.Cot(nameof(DongKhach.DiaChi), "ĐỊA CHỈ", 190),
            Theme.Cot(nameof(DongKhach.SoHoaDon), "SỐ HĐ", 70, canPhai: true),
            Theme.Cot(nameof(DongKhach.TongTien), "TỔNG MUA", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKhach.DaTra), "ĐÃ TRẢ", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKhach.ConLai), "CÒN NỢ", 130, "#,##0", canPhai: true));

        _luoi.DataSource = _nguon;
        _luoi.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                MoDonHang();
            }
        };
        _luoi.CellFormatting += Luoi_CellFormatting;

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 10), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        return vien;
    }

    private Control TaoThanhDuoi()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 8, 20, 10) };

        var btnMo = Theme.Nut("MỞ ĐƠN HÀNG", Theme.Chinh, 230, 52);
        btnMo.Click += (_, _) => MoDonHang();

        var btnSua = Theme.NutPhu("Sửa khách", 150, 52);
        btnSua.Click += (_, _) => SuaKhach();

        var btnXoa = Theme.NutPhu("Xoá khách", 150, 52);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => XoaKhach();

        var trai = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
        };
        trai.Controls.Add(btnMo);
        trai.Controls.Add(btnSua);
        trai.Controls.Add(btnXoa);

        _lblTongKet.Dock = DockStyle.Right;
        _lblTongKet.TextAlign = ContentAlignment.MiddleRight;
        _lblTongKet.Font = Theme.FontSo;
        _lblTongKet.ForeColor = Theme.Chu;
        _lblTongKet.AutoSize = false;
        _lblTongKet.Width = 640;

        nen.Controls.Add(trai);
        nen.Controls.Add(_lblTongKet);
        return nen;
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);
        _lblTrangThai.Text = $"Ctrl+Z hoàn tác · Ctrl+Y làm lại · Dữ liệu: {_kho.DuongDanFile}";

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        return nen;
    }

    // ---------------- Nạp dữ liệu ----------------

    private void NapNam()
    {
        var namCu = _cboNam.SelectedItem as int?;
        _dangNap = true;
        _cboNam.Items.Clear();
        foreach (var nam in _kho.DanhSachNam())
        {
            _cboNam.Items.Add(nam);
        }

        var can = namCu ?? DateTime.Today.Year;
        var viTri = _cboNam.Items.IndexOf(can);
        _cboNam.SelectedIndex = viTri >= 0 ? viTri : 0;
        _dangNap = false;
    }

    private void NapDanhSach()
    {
        var dangChon = KhachDangChon?.Id;
        var nam = NamDangChon;
        var tuKhoa = _txtTim.Text;

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var khach in _kho.DuLieu.KhachHangs.OrderBy(k => k.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            if (!ChuViet.Chua(khach.Ten, tuKhoa)
                && !ChuViet.Chua(khach.DienThoai, tuKhoa)
                && !ChuViet.Chua(khach.DiaChi, tuKhoa))
            {
                continue;
            }

            var hoaDons = _kho.DuLieu.HoaDons.Where(h => h.KhachHangId == khach.Id && h.Nam == nam).ToList();
            if (_chkCoDon.Checked && hoaDons.Count == 0)
            {
                continue;
            }

            var tong = hoaDons.Sum(h => h.TongTien);
            var daTra = hoaDons.Sum(h => h.DaThanhToan);

            _nguon.Add(new DongKhach
            {
                Khach = khach,
                Ten = khach.Ten,
                DienThoai = khach.DienThoai,
                DiaChi = khach.DiaChi,
                SoHoaDon = hoaDons.Count,
                TongTien = tong,
                DaTra = daTra,
                ConLai = tong - daTra,
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            ChonLaiKhach(id);
        }

        var tongMua = _nguon.Sum(d => d.TongTien);
        var tongTra = _nguon.Sum(d => d.DaTra);
        _lblTongKet.Text =
            $"Năm {nam}   ·   {_nguon.Count} khách   ·   Tổng mua: {So.Tien(tongMua)}   ·   Còn nợ: {So.Tien(tongMua - tongTra)}";

        CapNhatNutLichSu();
    }

    private void ChonLaiKhach(Guid id)
    {
        for (var i = 0; i < _luoi.Rows.Count; i++)
        {
            if (_luoi.Rows[i].DataBoundItem is DongKhach dong && dong.Khach.Id == id)
            {
                _luoi.CurrentCell = _luoi.Rows[i].Cells[0];
                return;
            }
        }
    }

    private void CapNhatNutLichSu()
    {
        _btnHoanTac.Enabled = _kho.CoTheHoanTac;
        _btnLamLai.Enabled = _kho.CoTheLamLai;
        _btnHoanTac.ForeColor = _btnHoanTac.Enabled ? Theme.Chinh : Theme.Xam;
        _btnLamLai.ForeColor = _btnLamLai.Enabled ? Theme.Chinh : Theme.Xam;
    }

    private void Kho_DuLieuThayDoi(object? sender, EventArgs e)
    {
        NapNam();
        NapDanhSach();
    }

    private void Luoi_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (e.CellStyle is not { } kieu)
        {
            return;
        }

        var thuocTinh = _luoi.Columns[e.ColumnIndex].DataPropertyName;
        if (thuocTinh == nameof(DongKhach.ConLai) && e.Value is decimal conLai)
        {
            kieu.Font = Theme.FontLuoiDam;
            kieu.ForeColor = conLai > 0 ? Theme.Do : Theme.Xam;
        }
        else if (thuocTinh == nameof(DongKhach.Ten))
        {
            kieu.Font = Theme.FontLuoiDam;
        }
    }

    // ---------------- Thao tác ----------------

    private void MoDonHang()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng trong danh sách.");
            return;
        }

        using var form = new DonHangForm(khach.Id, NamDangChon);
        form.ShowDialog(this);
    }

    private void ThemKhach()
    {
        using var form = new KhachHangForm(null);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } moi)
        {
            return;
        }

        _kho.ThucHien($"Thêm khách hàng {moi.Ten}", () => _kho.DuLieu.KhachHangs.Add(moi), phatSuKien: false);
        NapDanhSach();
        ChonLaiKhach(moi.Id);
        _lblTrangThai.Text = $"Đã thêm khách hàng {moi.Ten}.";
    }

    private void SuaKhach()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng để sửa.");
            return;
        }

        using var form = new KhachHangForm(khach);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } sua)
        {
            return;
        }

        _kho.ThucHien($"Sửa khách hàng {sua.Ten}", () =>
        {
            khach.Ten = sua.Ten;
            khach.DienThoai = sua.DienThoai;
            khach.DiaChi = sua.DiaChi;
            khach.GhiChu = sua.GhiChu;
        }, phatSuKien: false);

        NapDanhSach();
        _lblTrangThai.Text = $"Đã cập nhật khách hàng {khach.Ten}.";
    }

    private void XoaKhach()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng để xoá.");
            return;
        }

        var soHoaDon = _kho.DuLieu.HoaDons.Count(h => h.KhachHangId == khach.Id);
        var canhBao = soHoaDon > 0
            ? $"\n\nKhách này đang có {soHoaDon} hoá đơn, xoá khách sẽ xoá luôn các hoá đơn đó."
            : string.Empty;

        if (!HopThoai.Hoi(this, $"Xoá khách hàng \"{khach.Ten}\"?{canhBao}\n\n(Có thể bấm Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá khách hàng {khach.Ten}", () =>
        {
            _kho.DuLieu.HoaDons.RemoveAll(h => h.KhachHangId == khach.Id);
            _kho.DuLieu.KhachHangs.Remove(khach);
        }, phatSuKien: false);

        NapDanhSach();
        _lblTrangThai.Text = $"Đã xoá khách hàng {khach.Ten}. Bấm Ctrl+Z để lấy lại.";
    }

    private void MoDanhMucVatTu()
    {
        using var form = new VatTuForm();
        form.ShowDialog(this);
    }

    private void HoanTac()
    {
        var moTa = _kho.HoanTac();
        _lblTrangThai.Text = moTa is null
            ? "Không còn thao tác nào để hoàn tác."
            : $"Đã hoàn tác: {moTa}   (Ctrl+Y để làm lại)";
    }

    private void LamLai()
    {
        var moTa = _kho.LamLai();
        _lblTrangThai.Text = moTa is null
            ? "Không còn thao tác nào để làm lại."
            : $"Đã làm lại: {moTa}";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Z:
                HoanTac();
                return true;
            case Keys.Control | Keys.Y:
                LamLai();
                return true;
            case Keys.Control | Keys.N:
                ThemKhach();
                return true;
            case Keys.F3:
                _txtTim.Focus();
                _txtTim.SelectAll();
                return true;
            case Keys.Enter when _luoi.Focused:
                MoDonHang();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Một dòng khách hàng trên lưới, kèm số liệu của năm đang xem.</summary>
    private sealed class DongKhach
    {
        public KhachHang Khach { get; set; } = null!;

        public string Ten { get; set; } = string.Empty;

        public string DienThoai { get; set; } = string.Empty;

        public string DiaChi { get; set; } = string.Empty;

        public int SoHoaDon { get; set; }

        public decimal TongTien { get; set; }

        public decimal DaTra { get; set; }

        public decimal ConLai { get; set; }
    }
}
