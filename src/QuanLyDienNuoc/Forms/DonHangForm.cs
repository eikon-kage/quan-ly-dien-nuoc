using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Đơn hàng của một khách trong một năm: bên trái là các hoá đơn, bên phải là các dòng hàng
/// đã lấy theo từng ngày. Thêm nhanh ở thanh trên, sửa trực tiếp trên lưới như Excel.
/// </summary>
public sealed class DonHangForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;

    private readonly BindingList<DongHoaDon> _nguonHD = new();
    private readonly DataGridView _luoiHD = new();
    private readonly DataGridView _luoiCT = new();
    private BindingList<ChiTietHoaDon> _nguonCT = new();

    private readonly ComboBox _cboNam = new();
    private readonly DateTimePicker _dtNgay = new();
    private readonly ComboBox _cboHang = new();
    private readonly TextBox _txtDonVi = Theme.O(120);
    private readonly TextBox _txtDonGia = Theme.O(150);
    private readonly TextBox _txtSoLuong = Theme.O(120);
    private readonly Label _lblTamTinh = new();

    private readonly Label _lblTenKhach = new();
    private readonly Label _lblLienHe = new();
    private readonly Label _lblTieuDeCT = new();
    private readonly Label _lblTong = new();
    private readonly Label _lblDaTra = new();
    private readonly Label _lblConLai = new();
    private readonly Label _lblTrangThai = new();

    private readonly Button _btnHoanTac = Theme.NutPhu("↶  Hoàn tác", 160, 42);
    private readonly Button _btnLamLai = Theme.NutPhu("↷  Làm lại", 150, 42);
    private readonly Button _btnChot = Theme.NutPhu("Chốt hoá đơn", 200, 44);

    private readonly int _namBanDau;

    private Guid? _hoaDonId;
    private bool _dangNap;
    private bool _sanSang;
    private string? _anhChupTruocKhiSua;

    public DonHangForm(Guid khachId, int nam)
    {
        _khachId = khachId;
        _namBanDau = nam;

        Text = "Đơn hàng của khách";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1250, 760);
        Size = new Size(1500, 900);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        _kho.DuLieuThayDoi += Kho_DuLieuThayDoi;
        FormClosed += (_, _) => _kho.DuLieuThayDoi -= Kho_DuLieuThayDoi;
    }

    /// <summary>Nạp dữ liệu khi cửa sổ đã dựng xong để lưới chọn được dòng.</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        _sanSang = true;
        NapNam(_namBanDau);
        NapDanhMucHang();
        NapHoaDon(null);
        _cboHang.Focus();
    }

    private KhachHang? Khach => _kho.TimKhach(_khachId);

    private HoaDon? HoaDonHienTai => _hoaDonId is { } id ? _kho.TimHoaDon(id) : null;

    private int NamDangChon => _cboNam.SelectedItem is int nam ? nam : DateTime.Today.Year;

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
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(TaoTieuDe(), 0, 0);
        goc.Controls.Add(TaoThanhCongCu(), 0, 1);
        goc.Controls.Add(TaoThanNoiDung(), 0, 2);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 3);

        Controls.Add(goc);
    }

    private Control TaoTieuDe()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Chinh };

        _lblTenKhach.Font = Theme.FontTieuDe;
        _lblTenKhach.ForeColor = Color.White;
        _lblTenKhach.AutoSize = true;
        _lblTenKhach.Location = new Point(24, 16);

        _lblLienHe.Font = Theme.FontPhu;
        _lblLienHe.ForeColor = Color.FromArgb(205, 224, 247);
        _lblLienHe.AutoSize = true;
        _lblLienHe.Location = new Point(26, 52);

        var btnDong = Theme.NutPhu("Đóng (Esc)", 150, 44);
        btnDong.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDong.Location = new Point(nen.Width - 174, 24);
        btnDong.Click += (_, _) => Close();
        nen.Resize += (_, _) => btnDong.Location = new Point(nen.Width - 174, 24);

        nen.Controls.Add(_lblTenKhach);
        nen.Controls.Add(_lblLienHe);
        nen.Controls.Add(btnDong);
        return nen;
    }

    private Control TaoThanhCongCu()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 10, 20, 6) };

        _cboNam.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNam.Font = Theme.FontNhap;
        _cboNam.Width = 120;
        _cboNam.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapHoaDon(null);
            }
        };

        var lblNam = Theme.Nhan("Năm:", Theme.FontDam);
        lblNam.Margin = new Padding(0, 14, 8, 0);

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(lblNam);
        _cboNam.Margin = new Padding(0, 8, 24, 0);
        trai.Controls.Add(_cboNam);

        var btnBangGia = Theme.NutPhu("Bảng giá của khách", 220, 42);
        btnBangGia.Margin = new Padding(0, 8, 10, 0);
        btnBangGia.Click += (_, _) => MoBangGia();

        var btnThanhToan = Theme.Nut("Thanh toán", Theme.Xanh, 170, 42);
        btnThanhToan.Margin = new Padding(0, 8, 10, 0);
        btnThanhToan.Click += (_, _) => MoThanhToan();

        _btnHoanTac.Margin = new Padding(0, 8, 10, 0);
        _btnLamLai.Margin = new Padding(0, 8, 10, 0);
        _btnHoanTac.Click += (_, _) => HoanTac();
        _btnLamLai.Click += (_, _) => LamLai();

        var phai = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
        };
        phai.Controls.Add(btnBangGia);
        phai.Controls.Add(btnThanhToan);
        phai.Controls.Add(_btnLamLai);
        phai.Controls.Add(_btnHoanTac);

        nen.Controls.Add(trai);
        nen.Controls.Add(phai);
        return nen;
    }

    private Control TaoThanNoiDung()
    {
        var than = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 0, 20, 10),
        };
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440));
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        than.Controls.Add(TaoCotHoaDon(), 0, 0);
        than.Controls.Add(TaoCotChiTiet(), 1, 0);
        return than;
    }

    private Control TaoCotHoaDon()
    {
        var cot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
            Margin = new Padding(0, 0, 16, 0),
        };
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        cot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));

        var lbl = new Label
        {
            Text = "HOÁ ĐƠN CỦA KHÁCH",
            Font = Theme.FontDam,
            ForeColor = Theme.Xam,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        Theme.ApDungLuoi(_luoiHD);
        _luoiHD.ReadOnly = true;
        _luoiHD.Columns.AddRange(
            Theme.Cot(nameof(DongHoaDon.Ma), "MÃ HĐ", 100),
            Theme.Cot(nameof(DongHoaDon.NgayMo), "MỞ NGÀY", 100, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongHoaDon.TongTien), "TỔNG", 110, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongHoaDon.ConLai), "CÒN NỢ", 110, "#,##0", canPhai: true));
        _luoiHD.DataSource = _nguonHD;
        _luoiHD.SelectionChanged += (_, _) =>
        {
            if (_dangNap || !_sanSang)
            {
                return;
            }

            _hoaDonId = (_luoiHD.CurrentRow?.DataBoundItem as DongHoaDon)?.HD.Id;
            NapChiTiet();
        };
        _luoiHD.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                SuaHoaDon();
            }
        };
        _luoiHD.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (_luoiHD.Columns[e.ColumnIndex].DataPropertyName == nameof(DongHoaDon.ConLai)
                && e.Value is decimal conLai
                && e.CellStyle is { } kieu)
            {
                kieu.ForeColor = conLai > 0 ? Theme.Do : Theme.Xanh;
                kieu.Font = Theme.FontLuoiDam;
            }
        };

        var btnMoi = Theme.Nut("+  Hoá đơn mới", Theme.Chinh, 200, 44);
        btnMoi.Margin = new Padding(0, 6, 10, 6);
        btnMoi.Click += (_, _) => TaoHoaDon();

        var btnSua = Theme.NutPhu("Sửa", 100, 44);
        btnSua.Margin = new Padding(0, 6, 10, 6);
        btnSua.Click += (_, _) => SuaHoaDon();

        var btnXoa = Theme.NutPhu("Xoá", 100, 44);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Margin = new Padding(0, 6, 10, 6);
        btnXoa.Click += (_, _) => XoaHoaDon();

        _btnChot.Margin = new Padding(0, 6, 10, 6);
        _btnChot.Click += (_, _) => DoiTrangThaiChot();

        var btnIn = Theme.Nut("IN / XEM TRƯỚC", Theme.Cam, 200, 44);
        btnIn.Margin = new Padding(0, 6, 10, 6);
        btnIn.Click += (_, _) => XemTruocVaIn();

        var btnXuatExcel = Theme.NutPhu("Xuất Excel", 145, 44);
        btnXuatExcel.Margin = new Padding(0, 6, 10, 6);
        btnXuatExcel.Click += (_, _) => XuatExcel();

        var btnNhapExcel = Theme.NutPhu("Nhập từ Excel", 175, 44);
        btnNhapExcel.Margin = new Padding(0, 6, 10, 6);
        btnNhapExcel.Click += (_, _) => NhapTuExcel();

        var nut = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, WrapContents = true };
        nut.Controls.Add(btnMoi);
        nut.Controls.Add(btnSua);
        nut.Controls.Add(btnXoa);
        nut.Controls.Add(_btnChot);
        nut.Controls.Add(btnIn);
        nut.Controls.Add(btnXuatExcel);
        nut.Controls.Add(btnNhapExcel);

        cot.Controls.Add(lbl, 0, 0);
        cot.Controls.Add(Theme.Khung(_luoiHD), 0, 1);
        cot.Controls.Add(nut, 0, 2);
        return cot;
    }

    private Control TaoCotChiTiet()
    {
        var cot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        cot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        _lblTieuDeCT.Text = "CHI TIẾT HÀNG ĐÃ LẤY";
        _lblTieuDeCT.Font = Theme.FontDam;
        _lblTieuDeCT.ForeColor = Theme.Xam;
        _lblTieuDeCT.Dock = DockStyle.Fill;
        _lblTieuDeCT.TextAlign = ContentAlignment.MiddleLeft;

        cot.Controls.Add(_lblTieuDeCT, 0, 0);
        cot.Controls.Add(TaoThanhThemNhanh(), 0, 1);
        cot.Controls.Add(Theme.Khung(TaoLuoiChiTiet()), 0, 2);
        cot.Controls.Add(TaoThanhTongTien(), 0, 3);
        return cot;
    }

    private Control TaoThanhThemNhanh()
    {
        var nen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ChinhNhat,
            Padding = new Padding(14, 8, 14, 8),
        };

        _dtNgay.Format = DateTimePickerFormat.Short;
        _dtNgay.Font = Theme.FontNhap;

        _cboHang.DropDownStyle = ComboBoxStyle.DropDown;
        _cboHang.Font = Theme.FontNhap;
        _cboHang.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cboHang.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cboHang.SelectedIndexChanged += (_, _) =>
        {
            if (_dangNap || _cboHang.SelectedItem is not VatTu vatTu || Khach is not { } khach)
            {
                return;
            }

            _txtDonVi.Text = vatTu.DonVi;
            _txtDonGia.Text = So.Tien(_kho.GiaCho(khach, vatTu));
            TinhTamTinh();
        };

        _txtDonGia.TextChanged += (_, _) => TinhTamTinh();
        _txtSoLuong.TextChanged += (_, _) => TinhTamTinh();

        _lblTamTinh.Font = Theme.FontSo;
        _lblTamTinh.ForeColor = Theme.Chinh;
        _lblTamTinh.Text = "0";
        _lblTamTinh.TextAlign = ContentAlignment.MiddleRight;
        _lblTamTinh.AutoSize = false;

        var btnThem = Theme.Nut("+  THÊM DÒNG", Theme.Xanh, 190, 34);
        btnThem.Click += (_, _) => ThemDong();

        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
        };
        hang.Controls.Add(Theme.Truong("NGÀY LẤY", _dtNgay, 150));
        hang.Controls.Add(Theme.Truong("TÊN HÀNG (gõ để tìm, chưa có thì gõ mới)", _cboHang, 330));
        hang.Controls.Add(Theme.Truong("ĐƠN VỊ", _txtDonVi, 110));
        hang.Controls.Add(Theme.Truong("ĐƠN GIÁ", _txtDonGia, 150));
        hang.Controls.Add(Theme.Truong("SỐ LƯỢNG", _txtSoLuong, 120));
        hang.Controls.Add(Theme.Truong("THÀNH TIỀN", _lblTamTinh, 170));
        hang.Controls.Add(Theme.Truong(" ", btnThem, 190));

        GanPhimEnter(_cboHang);
        GanPhimEnter(_txtDonVi);
        GanPhimEnter(_txtDonGia);
        GanPhimEnter(_txtSoLuong);

        nen.Controls.Add(hang);
        return nen;
    }

    private Control TaoLuoiChiTiet()
    {
        Theme.ApDungLuoi(_luoiCT);
        _luoiCT.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoiCT.Columns.AddRange(
            Theme.Cot(nameof(ChiTietHoaDon.Ngay), "NGÀY", 90, "dd/MM/yyyy", chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.TenHang), "TÊN HÀNG", 260, chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.DonVi), "ĐƠN VỊ", 80, chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.DonGia), "ĐƠN GIÁ", 120, "#,##0", canPhai: true, chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.SoLuong), "SỐ LƯỢNG", 100, "#,##0.##", canPhai: true, chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.ThanhTien), "THÀNH TIỀN", 140, "#,##0", canPhai: true),
            Theme.Cot(nameof(ChiTietHoaDon.GhiChu), "GHI CHÚ", 150, chiDoc: false));

        Theme.ChoPhepGoSo(_luoiCT, nameof(ChiTietHoaDon.DonGia), nameof(ChiTietHoaDon.SoLuong));

        _luoiCT.CellBeginEdit += (_, _) => _anhChupTruocKhiSua = _kho.ChupNhanh();
        _luoiCT.CellEndEdit += LuoiCT_CellEndEdit;
        _luoiCT.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (_luoiCT.Columns[e.ColumnIndex].DataPropertyName == nameof(ChiTietHoaDon.ThanhTien)
                && e.CellStyle is { } kieu)
            {
                kieu.Font = Theme.FontLuoiDam;
                kieu.BackColor = Color.FromArgb(248, 250, 253);
            }
        };

        _luoiCT.DataSource = _nguonCT;
        return _luoiCT;
    }

    private Control TaoThanhTongTien()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(0, 8, 0, 0) };

        var btnXoaDong = Theme.NutPhu("Xoá dòng (Delete)", 210, 46);
        btnXoaDong.ForeColor = Theme.Do;
        btnXoaDong.Click += (_, _) => XoaDong();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnXoaDong);

        void SetNhan(Label lbl, Color mau)
        {
            lbl.Font = Theme.FontSo;
            lbl.ForeColor = mau;
            lbl.AutoSize = false;
            lbl.Width = 250;
            lbl.Height = 44;
            lbl.TextAlign = ContentAlignment.MiddleRight;
            lbl.Margin = new Padding(0, 0, 12, 0);
        }

        SetNhan(_lblTong, Theme.Chu);
        SetNhan(_lblDaTra, Theme.Xanh);
        SetNhan(_lblConLai, Theme.Do);

        var phai = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
        };
        phai.Controls.Add(_lblConLai);
        phai.Controls.Add(_lblDaTra);
        phai.Controls.Add(_lblTong);

        nen.Controls.Add(trai);
        nen.Controls.Add(phai);
        return nen;
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);
        _lblTrangThai.Text = "Enter để thêm dòng · F3 về ô Tên hàng · Bấm đúp vào ô để sửa · Delete xoá dòng · Ctrl+Z hoàn tác · Ctrl+Y làm lại";

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        return nen;
    }

    // ---------------- Nạp dữ liệu ----------------

    private void NapNam(int nam)
    {
        _dangNap = true;
        _cboNam.Items.Clear();
        foreach (var n in _kho.DanhSachNam())
        {
            _cboNam.Items.Add(n);
        }

        if (!_cboNam.Items.Contains(nam))
        {
            _cboNam.Items.Insert(0, nam);
        }

        _cboNam.SelectedIndex = Math.Max(0, _cboNam.Items.IndexOf(nam));
        _dangNap = false;
    }

    private void NapDanhMucHang()
    {
        _dangNap = true;
        var dangGo = _cboHang.Text;
        _cboHang.Items.Clear();
        foreach (var vatTu in _kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            _cboHang.Items.Add(vatTu);
        }

        _cboHang.Text = dangGo;
        _dangNap = false;
    }

    private void NapHoaDon(Guid? chon)
    {
        if (Khach is not { } khach)
        {
            Close();
            return;
        }

        _lblTenKhach.Text = khach.Ten;
        _lblLienHe.Text = string.Join("   ·   ", new[]
        {
            string.IsNullOrWhiteSpace(khach.DienThoai) ? null : "ĐT: " + khach.DienThoai,
            string.IsNullOrWhiteSpace(khach.DiaChi) ? null : khach.DiaChi,
            string.IsNullOrWhiteSpace(khach.GhiChu) ? null : khach.GhiChu,
        }.Where(s => s is not null));

        _dangNap = true;
        _nguonHD.RaiseListChangedEvents = false;
        _nguonHD.Clear();

        foreach (var hoaDon in _kho.HoaDonCuaKhach(_khachId, NamDangChon))
        {
            _nguonHD.Add(new DongHoaDon
            {
                HD = hoaDon,
                Ma = hoaDon.DaChot ? hoaDon.MaHoaDon + " (chốt)" : hoaDon.MaHoaDon,
                NgayMo = hoaDon.NgayMo,
                TongTien = hoaDon.TongTien,
                ConLai = hoaDon.ConLai,
            });
        }

        _nguonHD.RaiseListChangedEvents = true;
        _nguonHD.ResetBindings();

        _hoaDonId = null;
        if (_nguonHD.Count > 0)
        {
            var viTri = 0;
            if (chon is { } id)
            {
                for (var i = 0; i < _nguonHD.Count; i++)
                {
                    if (_nguonHD[i].HD.Id == id)
                    {
                        viTri = i;
                        break;
                    }
                }
            }

            _luoiHD.CurrentCell = _luoiHD.Rows[viTri].Cells[0];
            _hoaDonId = _nguonHD[viTri].HD.Id;
        }

        _dangNap = false;
        NapChiTiet();
    }

    private void NapChiTiet(Guid? chonDong = null)
    {
        var hoaDon = HoaDonHienTai;
        var dong = hoaDon?.ChiTiet
            .OrderBy(c => c.Ngay)
            .ThenBy(c => c.TenHang, StringComparer.CurrentCultureIgnoreCase)
            .ToList() ?? new List<ChiTietHoaDon>();

        _dangNap = true;
        _nguonCT = new BindingList<ChiTietHoaDon>(dong);
        _luoiCT.DataSource = _nguonCT;
        _luoiCT.ReadOnly = hoaDon is null || hoaDon.DaChot;

        if (chonDong is { } id)
        {
            for (var i = 0; i < _luoiCT.Rows.Count; i++)
            {
                if (_luoiCT.Rows[i].DataBoundItem is ChiTietHoaDon ct && ct.Id == id)
                {
                    _luoiCT.CurrentCell = _luoiCT.Rows[i].Cells[1];
                    break;
                }
            }
        }

        _dangNap = false;

        _lblTieuDeCT.Text = hoaDon is null
            ? "CHI TIẾT HÀNG ĐÃ LẤY — chưa có hoá đơn nào, thêm dòng hàng sẽ tự tạo hoá đơn mới"
            : $"CHI TIẾT HOÁ ĐƠN {hoaDon.MaHoaDon}   ·   mở ngày {hoaDon.NgayMo:dd/MM/yyyy}   ·   {dong.Count} dòng"
              + (hoaDon.DaChot ? "   ·   ĐÃ CHỐT (không sửa được)" : string.Empty);

        _btnChot.Text = hoaDon is { DaChot: true } ? "Mở lại hoá đơn" : "Chốt hoá đơn";
        _btnChot.Enabled = hoaDon is not null;

        CapNhatTong();
        CapNhatNutLichSu();
    }

    /// <summary>Cập nhật lại tiền của các hoá đơn ở cột trái mà không rời ô đang chọn.</summary>
    private void CapNhatTomTatHoaDon()
    {
        foreach (var dong in _nguonHD)
        {
            dong.TongTien = dong.HD.TongTien;
            dong.ConLai = dong.HD.ConLai;
        }

        _luoiHD.Invalidate();
    }

    private void CapNhatTong()
    {
        var hoaDon = HoaDonHienTai;
        var tong = hoaDon?.TongTien ?? 0m;
        var daTra = hoaDon?.DaThanhToan ?? 0m;

        _lblTong.Text = $"Tổng cộng: {So.Tien(tong)}";
        _lblDaTra.Text = $"Đã trả: {So.Tien(daTra)}";
        _lblConLai.Text = $"Còn lại: {So.Tien(tong - daTra)}";
    }

    private void CapNhatNutLichSu()
    {
        _btnHoanTac.Enabled = _kho.CoTheHoanTac;
        _btnLamLai.Enabled = _kho.CoTheLamLai;
        _btnHoanTac.ForeColor = _btnHoanTac.Enabled ? Theme.Chinh : Theme.Xam;
        _btnLamLai.ForeColor = _btnLamLai.Enabled ? Theme.Chinh : Theme.Xam;
    }

    private void TinhTamTinh()
    {
        var thanhTien = Math.Round(So.Doc(_txtDonGia.Text) * So.Doc(_txtSoLuong.Text), 0, MidpointRounding.AwayFromZero);
        _lblTamTinh.Text = So.Tien(thanhTien);
    }

    private void Kho_DuLieuThayDoi(object? sender, EventArgs e)
    {
        if (Khach is null)
        {
            Close();
            return;
        }

        var namCu = NamDangChon;
        NapNam(namCu);
        NapDanhMucHang();
        NapHoaDon(_hoaDonId);
    }

    // ---------------- Thao tác trên dòng hàng ----------------

    private void ThemDong()
    {
        if (Khach is not { } khach)
        {
            return;
        }

        var ten = _cboHang.Text.Trim();
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy chọn hoặc gõ tên hàng.");
            _cboHang.Focus();
            return;
        }

        var soLuong = So.Doc(_txtSoLuong.Text);
        if (soLuong <= 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập số lượng lớn hơn 0.");
            _txtSoLuong.Focus();
            _txtSoLuong.SelectAll();
            return;
        }

        var donGia = So.Doc(_txtDonGia.Text);
        var donVi = _txtDonVi.Text.Trim();
        var ngay = _dtNgay.Value.Date;

        var hoaDonDangChon = HoaDonHienTai;
        if (hoaDonDangChon is { DaChot: true })
        {
            HopThoai.CanhBao(this, "Hoá đơn này đã chốt. Hãy bấm \"Mở lại hoá đơn\" trước khi thêm hàng.");
            return;
        }

        var taoHoaDonMoi = hoaDonDangChon is null;
        HoaDon hoaDon = hoaDonDangChon ?? new HoaDon
        {
            KhachHangId = _khachId,
            Nam = NamDangChon,
            MaHoaDon = _kho.TaoMaHoaDon(_khachId, NamDangChon),
            NgayMo = ngay,
        };

        var vatTu = _cboHang.SelectedItem as VatTu;
        if (vatTu is null || !string.Equals(vatTu.Ten, ten, StringComparison.CurrentCultureIgnoreCase))
        {
            vatTu = _kho.TimVatTuTheoTen(ten);
        }

        // Hỏi trước khi ghi để mọi thay đổi nằm gọn trong một bước hoàn tác.
        var vatTuMoi = vatTu is null;
        var luuGiaRieng = vatTuMoi;
        if (vatTu is not null && donGia > 0)
        {
            var coGiaCu = khach.BangGiaRieng.TryGetValue(vatTu.Id, out var giaCu) && giaCu > 0;
            if (!coGiaCu)
            {
                luuGiaRieng = true;
            }
            else if (giaCu != donGia)
            {
                luuGiaRieng = HopThoai.Hoi(
                    this,
                    $"Giá \"{ten}\" của khách {khach.Ten} đang là {So.Tien(giaCu)}.\n" +
                    $"Lần này nhập {So.Tien(donGia)}.\n\nDùng giá mới cho những lần sau?");
            }
        }

        var dongMoi = new ChiTietHoaDon
        {
            Ngay = ngay,
            TenHang = ten,
            DonVi = donVi,
            DonGia = donGia,
            SoLuong = soLuong,
        };

        _kho.ThucHien($"Thêm \"{ten}\" ngày {ngay:dd/MM/yyyy}", () =>
        {
            if (vatTu is null)
            {
                vatTu = new VatTu { Ten = ten, DonVi = donVi, DonGiaMacDinh = donGia };
                _kho.DuLieu.VatTus.Add(vatTu);
            }
            else if (string.IsNullOrWhiteSpace(vatTu.DonVi) && donVi.Length > 0)
            {
                vatTu.DonVi = donVi;
            }

            if (luuGiaRieng && donGia > 0)
            {
                khach.BangGiaRieng[vatTu.Id] = donGia;
            }

            dongMoi.VatTuId = vatTu.Id;

            if (taoHoaDonMoi)
            {
                _kho.DuLieu.HoaDons.Add(hoaDon);
            }

            hoaDon.ChiTiet.Add(dongMoi);
        }, phatSuKien: false);

        if (vatTuMoi)
        {
            NapDanhMucHang();
        }

        _hoaDonId = hoaDon.Id;
        NapHoaDon(hoaDon.Id);
        NapChiTiet(dongMoi.Id);

        _lblTrangThai.Text = $"Đã thêm: {ten} × {So.Luong(soLuong)} = {So.Tien(dongMoi.ThanhTien)}"
            + (taoHoaDonMoi ? $" (tự tạo hoá đơn {hoaDon.MaHoaDon})" : string.Empty);

        // Sẵn sàng cho dòng tiếp theo, giữ nguyên ngày để nhập nhanh nhiều dòng cùng ngày.
        _dangNap = true;
        _cboHang.SelectedIndex = -1;
        _cboHang.Text = string.Empty;
        _txtDonVi.Clear();
        _txtDonGia.Clear();
        _txtSoLuong.Clear();
        _dangNap = false;
        TinhTamTinh();
        _cboHang.Focus();
    }

    private void LuoiCT_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (anhChup is null || e.RowIndex < 0)
        {
            return;
        }

        // Không ghi bước hoàn tác nếu người dùng không đổi gì.
        if (anhChup == _kho.ChupNhanh())
        {
            return;
        }

        var thuocTinh = _luoiCT.Columns[e.ColumnIndex].DataPropertyName;
        var dong = _luoiCT.Rows[e.RowIndex].DataBoundItem as ChiTietHoaDon;
        _kho.GhiNhan(anhChup, $"Sửa {TenCotDeDoc(thuocTinh)}", phatSuKien: false);

        _luoiCT.InvalidateRow(e.RowIndex);
        CapNhatTomTatHoaDon();
        CapNhatTong();

        if (thuocTinh == nameof(ChiTietHoaDon.Ngay) && dong is not null)
        {
            // Ngày đổi thì xếp lại cho đúng thứ tự.
            NapChiTiet(dong.Id);
        }

        _lblTrangThai.Text = "Đã lưu thay đổi. Bấm Ctrl+Z nếu muốn quay lại.";
        CapNhatNutLichSu();
    }

    private static string TenCotDeDoc(string thuocTinh) => thuocTinh switch
    {
        nameof(ChiTietHoaDon.Ngay) => "ngày",
        nameof(ChiTietHoaDon.TenHang) => "tên hàng",
        nameof(ChiTietHoaDon.DonVi) => "đơn vị",
        nameof(ChiTietHoaDon.DonGia) => "đơn giá",
        nameof(ChiTietHoaDon.SoLuong) => "số lượng",
        nameof(ChiTietHoaDon.GhiChu) => "ghi chú",
        _ => "dòng hàng",
    };

    private void XoaDong()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            return;
        }

        if (hoaDon.DaChot)
        {
            HopThoai.CanhBao(this, "Hoá đơn đã chốt, không xoá được dòng.");
            return;
        }

        if (_luoiCT.CurrentRow?.DataBoundItem is not ChiTietHoaDon dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn dòng hàng cần xoá.");
            return;
        }

        if (!HopThoai.Hoi(this, $"Xoá dòng \"{dong.TenHang}\" ngày {dong.Ngay:dd/MM/yyyy}?\n\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá dòng \"{dong.TenHang}\"", () => hoaDon.ChiTiet.RemoveAll(c => c.Id == dong.Id), phatSuKien: false);

        NapHoaDon(_hoaDonId);
        _lblTrangThai.Text = $"Đã xoá dòng {dong.TenHang}. Bấm Ctrl+Z để lấy lại.";
    }

    // ---------------- Thao tác trên hoá đơn ----------------

    private void TaoHoaDon()
    {
        using var form = new HoaDonForm(null, _kho.TaoMaHoaDon(_khachId, NamDangChon), NamDangChon);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } thongTin)
        {
            return;
        }

        var hoaDon = new HoaDon
        {
            KhachHangId = _khachId,
            Nam = NamDangChon,
            MaHoaDon = thongTin.MaHoaDon,
            NgayMo = thongTin.NgayMo,
            GhiChu = thongTin.GhiChu,
        };

        _kho.ThucHien($"Tạo hoá đơn {hoaDon.MaHoaDon}", () => _kho.DuLieu.HoaDons.Add(hoaDon), phatSuKien: false);
        NapHoaDon(hoaDon.Id);
        _lblTrangThai.Text = $"Đã tạo hoá đơn {hoaDon.MaHoaDon}.";
        _cboHang.Focus();
    }

    private void SuaHoaDon()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            HopThoai.CanhBao(this, "Chưa có hoá đơn nào để sửa.");
            return;
        }

        using var form = new HoaDonForm(hoaDon, hoaDon.MaHoaDon, hoaDon.Nam);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } thongTin)
        {
            return;
        }

        _kho.ThucHien($"Sửa hoá đơn {hoaDon.MaHoaDon}", () =>
        {
            hoaDon.MaHoaDon = thongTin.MaHoaDon;
            hoaDon.NgayMo = thongTin.NgayMo;
            hoaDon.GhiChu = thongTin.GhiChu;
        }, phatSuKien: false);

        NapHoaDon(hoaDon.Id);
        _lblTrangThai.Text = $"Đã cập nhật hoá đơn {hoaDon.MaHoaDon}.";
    }

    private void XoaHoaDon()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            return;
        }

        if (!HopThoai.Hoi(
                this,
                $"Xoá hoá đơn {hoaDon.MaHoaDon} cùng {hoaDon.ChiTiet.Count} dòng hàng?\n\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá hoá đơn {hoaDon.MaHoaDon}", () => _kho.DuLieu.HoaDons.Remove(hoaDon), phatSuKien: false);
        NapHoaDon(null);
        _lblTrangThai.Text = $"Đã xoá hoá đơn {hoaDon.MaHoaDon}. Bấm Ctrl+Z để lấy lại.";
    }

    private void DoiTrangThaiChot()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            return;
        }

        var dangChot = hoaDon.DaChot;
        _kho.ThucHien(
            dangChot ? $"Mở lại hoá đơn {hoaDon.MaHoaDon}" : $"Chốt hoá đơn {hoaDon.MaHoaDon}",
            () => hoaDon.NgayChot = dangChot ? null : DateTime.Today,
            phatSuKien: false);

        NapHoaDon(hoaDon.Id);
        _lblTrangThai.Text = dangChot
            ? $"Đã mở lại hoá đơn {hoaDon.MaHoaDon}."
            : $"Đã chốt hoá đơn {hoaDon.MaHoaDon}.";
    }

    // ---------------- In và Excel ----------------

    private void XemTruocVaIn()
    {
        if (HoaDonHienTai is not { } hoaDon || Khach is not { } khach)
        {
            HopThoai.CanhBao(this, "Chưa có hoá đơn nào để in.");
            return;
        }

        if (hoaDon.ChiTiet.Count == 0)
        {
            HopThoai.CanhBao(this, "Hoá đơn chưa có dòng hàng nào.");
            return;
        }

        // Không có máy in nào thì bản xem trước không dựng được khổ giấy.
        if (System.Drawing.Printing.PrinterSettings.InstalledPrinters.Count == 0)
        {
            HopThoai.CanhBao(
                this,
                "Máy tính chưa cài máy in nào nên chưa xem trước được.\n\n" +
                "Vào Settings → Bluetooth & devices → Printers & scanners → Add device,\n" +
                "thêm \"Microsoft Print to PDF\" là dùng được ngay (in ra file PDF).");
            return;
        }

        try
        {
            using var taiLieu = new InHoaDon(hoaDon, khach, ThongTinCuaHang.DocTuMau());
            using var form = new XemTruocForm(taiLieu);
            form.ShowDialog(this);
            _lblTrangThai.Text = $"Hoá đơn {hoaDon.MaHoaDon}: {taiLieu.SoTrang} trang.";
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không xem trước được:\n" + ex.Message);
        }
    }

    private void XuatExcel()
    {
        if (HoaDonHienTai is not { } hoaDon || Khach is not { } khach)
        {
            HopThoai.CanhBao(this, "Chưa có hoá đơn nào để xuất.");
            return;
        }

        var tenGoiY = TenFileHopLe($"HoaDon {hoaDon.MaHoaDon} - {khach.Ten}.xls");
        using var hopThoai = new SaveFileDialog
        {
            Title = "Xuất hoá đơn ra Excel",
            Filter = "File Excel (*.xls)|*.xls",
            FileName = tenGoiY,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (hopThoai.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            XuatHoaDon.Xuat(hoaDon, khach, hopThoai.FileName, ngayIn: DateTime.Today);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không xuất được file:\n" + ex.Message);
            return;
        }

        _lblTrangThai.Text = $"Đã xuất: {hopThoai.FileName}";

        if (HopThoai.Hoi(this, $"Đã xuất xong:\n{hopThoai.FileName}\n\nMở file lên xem luôn không?"))
        {
            MoFile(hopThoai.FileName);
        }
    }

    private void NhapTuExcel()
    {
        if (Khach is null)
        {
            return;
        }

        using var chonFile = new OpenFileDialog
        {
            Title = "Chọn file hoá đơn Excel cần nhập",
            Filter = "File Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Tất cả các file (*.*)|*.*",
        };

        if (chonFile.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        using var form = new NhapExcelForm(_khachId, NamDangChon, _hoaDonId, chonFile.FileName);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        NapHoaDon(form.HoaDonDaNhap ?? _hoaDonId);
        _lblTrangThai.Text = $"Đã nhập {form.SoDongDaNhap} dòng từ Excel. Bấm Ctrl+Z nếu muốn bỏ.";
    }

    private void MoFile(string duongDan)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(duongDan)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            HopThoai.CanhBao(this, "Không mở được file (máy chưa cài Excel hoặc WPS?):\n" + ex.Message);
        }
    }

    private static string TenFileHopLe(string ten)
    {
        foreach (var kyTu in Path.GetInvalidFileNameChars())
        {
            ten = ten.Replace(kyTu, ' ');
        }

        return ten;
    }

    private void MoThanhToan()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            HopThoai.CanhBao(this, "Chưa có hoá đơn nào để ghi thanh toán.");
            return;
        }

        using var form = new ThanhToanForm(hoaDon.Id);
        form.ShowDialog(this);
        NapHoaDon(hoaDon.Id);
    }

    private void MoBangGia()
    {
        if (Khach is not { } khach)
        {
            return;
        }

        using var form = new BangGiaForm(khach.Id);
        form.ShowDialog(this);
        NapDanhMucHang();
    }

    // ---------------- Hoàn tác ----------------

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

    private void GanPhimEnter(Control dieuKhien)
    {
        dieuKhien.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            if (s is ComboBox cbo && cbo.DroppedDown)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            ThemDong();
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var dangSuaO = _luoiCT.IsCurrentCellInEditMode;

        switch (keyData)
        {
            case Keys.Control | Keys.Z when !dangSuaO:
                HoanTac();
                return true;
            case Keys.Control | Keys.Y when !dangSuaO:
                LamLai();
                return true;
            case Keys.Delete when !dangSuaO && _luoiCT.Focused:
                XoaDong();
                return true;
            case Keys.F3:
                _cboHang.Focus();
                _cboHang.SelectAll();
                return true;
            case Keys.Escape when !dangSuaO:
                Close();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Một dòng hoá đơn ở cột bên trái.</summary>
    private sealed class DongHoaDon
    {
        public HoaDon HD { get; set; } = null!;

        public string Ma { get; set; } = string.Empty;

        public DateTime NgayMo { get; set; }

        public decimal TongTien { get; set; }

        public decimal ConLai { get; set; }
    }
}
