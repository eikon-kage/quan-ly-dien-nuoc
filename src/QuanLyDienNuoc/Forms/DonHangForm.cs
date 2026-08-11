using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Đơn hàng của một khách trong một năm: chọn hoá đơn ở thanh trên, cả màn hình còn lại là
/// các dòng hàng đã lấy theo từng ngày. Thêm nhanh ở thanh trên, sửa trực tiếp trên lưới như Excel.
/// </summary>
public sealed class DonHangForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;

    private readonly ComboBox _cboHoaDon = new();
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
    private readonly Button _btnChot = Theme.NutPhu("Chốt hoá đơn", 180, 42);

    private readonly int _namBanDau;
    private readonly List<VatTu> _danhMucHang = new();

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
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(TaoTieuDe(), 0, 0);
        goc.Controls.Add(TaoThanhCongCu(), 0, 1);
        goc.Controls.Add(TaoThanhHoaDon(), 0, 2);
        goc.Controls.Add(TaoThanNoiDung(), 0, 3);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 4);

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

        var btnBangGia = Theme.NutPhu("Bảng giá của khách", 200, 42);
        btnBangGia.Margin = new Padding(0, 8, 10, 0);
        btnBangGia.Click += (_, _) => MoBangGia();

        var btnThanhToan = Theme.NutPhu("Trả cho hoá đơn này", 210, 42);
        btnThanhToan.Margin = new Padding(0, 8, 10, 0);
        btnThanhToan.Click += (_, _) => MoThanhToan();

        var btnThuTien = Theme.Nut("THU TIỀN CỦA KHÁCH", Theme.Xanh, 250, 42);
        btnThuTien.Margin = new Padding(0, 8, 10, 0);
        btnThuTien.Click += (_, _) => MoThuTien();

        var btnNhacNo = Theme.NutPhu("Nhắc nợ", 140, 42);
        btnNhacNo.Margin = new Padding(0, 8, 10, 0);
        btnNhacNo.Click += (_, _) => SoanTinNhacNo();

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
        phai.Controls.Add(btnNhacNo);
        phai.Controls.Add(btnThanhToan);
        phai.Controls.Add(btnThuTien);
        phai.Controls.Add(_btnLamLai);
        phai.Controls.Add(_btnHoanTac);

        nen.Controls.Add(trai);
        nen.Controls.Add(phai);
        return nen;
    }

    private Control TaoThanNoiDung()
    {
        var than = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 0, 20, 10) };
        than.Controls.Add(TaoCotChiTiet());
        return than;
    }

    /// <summary>
    /// Thanh chọn hoá đơn. Trước đây là cả một cột lưới rộng 440px bên trái, nhưng cột đó chỉ
    /// để chọn xem hoá đơn nào — mã, ngày, tổng tiền, còn nợ của hoá đơn đang xem thì tiêu đề
    /// lưới chi tiết và thanh tổng tiền phía dưới đã ghi rồi. Gom lại thành một ô chọn, phần
    /// màn hình lấy lại được trả cho lưới chi tiết, chỗ thật sự phải nhìn cả ngày.
    /// </summary>
    private Control TaoThanhHoaDon()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 0, 20, 8) };

        var lbl = Theme.Nhan("HOÁ ĐƠN:", Theme.FontNhan, Theme.Xam);
        lbl.Margin = new Padding(0, 16, 10, 0);

        _cboHoaDon.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboHoaDon.Font = Theme.FontNhap;
        _cboHoaDon.Width = 330;
        _cboHoaDon.Margin = new Padding(0, 8, 20, 0);
        _cboHoaDon.SelectedIndexChanged += (_, _) =>
        {
            if (_dangNap || !_sanSang)
            {
                return;
            }

            _hoaDonId = (_cboHoaDon.SelectedItem as DongHoaDon)?.HD.Id;
            NapChiTiet();
        };

        var btnMoi = Theme.Nut("+  Hoá đơn mới", Theme.Chinh, 200, 42);
        btnMoi.Margin = new Padding(0, 8, 10, 0);
        btnMoi.Click += (_, _) => TaoHoaDon();

        _btnChot.Margin = new Padding(0, 8, 10, 0);
        _btnChot.Click += (_, _) => DoiTrangThaiChot();

        var btnIn = Theme.Nut("IN / XEM TRƯỚC", Theme.Cam, 200, 42);
        btnIn.Margin = new Padding(0, 8, 10, 0);
        btnIn.Click += (_, _) => XemTruocVaIn();

        // Sửa mã, xoá hoá đơn và hai việc Excel gom vào menu: cả năm mới đụng tới vài lần,
        // để ngoài thì chen mất chỗ của hai nút dùng hằng ngày là thêm hoá đơn và in.
        var btnKhac = Theme.NutPhu("Việc khác  ▾", 170, 42);
        btnKhac.Margin = new Padding(0, 8, 10, 0);
        btnKhac.Click += (s, _) => MoMenuHoaDon((Control)s!);

        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
        };
        hang.Controls.Add(lbl);
        hang.Controls.Add(_cboHoaDon);
        hang.Controls.Add(btnMoi);
        hang.Controls.Add(_btnChot);
        hang.Controls.Add(btnIn);
        hang.Controls.Add(btnKhac);

        nen.Controls.Add(hang);
        return nen;
    }

    private void MoMenuHoaDon(Control nut)
    {
        var menu = new ContextMenuStrip { Font = Theme.FontThuong };
        menu.Items.Add("Sửa mã / ngày hoá đơn", null, (_, _) => SuaHoaDon());
        menu.Items.Add("Xoá hoá đơn này", null, (_, _) => XoaHoaDon());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Xuất Excel", null, (_, _) => XuatExcel());
        menu.Items.Add("Nhập từ Excel", null, (_, _) => NhapTuExcel());

        menu.Show(nut, new Point(0, nut.Height));
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
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
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

        _dtNgay.Format = DateTimePickerFormat.Custom;
        _dtNgay.CustomFormat = Theme.DangNgay;
        _dtNgay.Font = Theme.FontNhap;

        _cboHang.DropDownStyle = ComboBoxStyle.DropDown;
        _cboHang.Font = Theme.FontNhap;

        // Tự lọc theo kiểu gõ tắt thay cho gợi ý mặc định của Windows (chỉ khớp đúng đầu chữ và cần đủ dấu).
        _cboHang.AutoCompleteMode = AutoCompleteMode.None;
        _cboHang.TextUpdate += (_, _) => LocDanhMucHang();
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
        _txtDonGia.Leave += (_, _) => ChotPhepTinh(_txtDonGia, So.Tien);
        _txtSoLuong.Leave += (_, _) => ChotPhepTinh(_txtSoLuong, So.Luong);

        _lblTamTinh.Font = Theme.FontSo;
        _lblTamTinh.ForeColor = Theme.Chinh;
        _lblTamTinh.Text = "0";
        _lblTamTinh.TextAlign = ContentAlignment.MiddleRight;
        _lblTamTinh.AutoSize = false;

        var btnThem = Theme.Nut("+  THÊM DÒNG", Theme.Xanh, 190, 34);
        btnThem.Click += (_, _) => ThemDong();

        var btnTraLai = Theme.Nut("−  TRẢ LẠI", Theme.Do, 160, 34);
        btnTraLai.Click += (_, _) => ThemDong(traLai: true);

        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Margin = new Padding(0),
        };
        hang.Controls.Add(Theme.Truong("NGÀY LẤY", _dtNgay, 150));
        hang.Controls.Add(Theme.Truong("TÊN HÀNG (gõ tắt cũng ra: \"o27\", \"27 ong\")", _cboHang, 330));
        hang.Controls.Add(Theme.Truong("ĐƠN VỊ", _txtDonVi, 110));
        hang.Controls.Add(Theme.Truong("ĐƠN GIÁ (tính được: 3+2*4)", _txtDonGia, 150));
        hang.Controls.Add(Theme.Truong("SỐ LƯỢNG (số âm là trả lại)", _txtSoLuong, 120));
        hang.Controls.Add(Theme.Truong("THÀNH TIỀN", _lblTamTinh, 170));
        hang.Controls.Add(Theme.Truong(" ", btnThem, 190));
        hang.Controls.Add(Theme.Truong(" ", btnTraLai, 160));

        GanPhimEnter(_cboHang);
        GanPhimEnter(_txtDonVi);
        GanPhimEnter(_txtDonGia);
        GanPhimEnter(_txtSoLuong);

        var btnNhieuDong = Theme.NutPhu("Nhập nhiều dòng…", 200, 38);
        btnNhieuDong.Click += (_, _) => NhapNhieuDong();

        var btnBoHang = Theme.NutPhu("Bộ hàng thường dùng", 230, 38);
        btnBoHang.Click += (_, _) => ChonBoHang();

        var btnChepNgay = Theme.NutPhu("Chép lại một ngày", 200, 38);
        btnChepNgay.Click += (_, _) => ChepNgay();

        var btnNhanDoi = Theme.NutPhu("Nhân đôi dòng (Ctrl+D)", 240, 38);
        btnNhanDoi.Click += (_, _) => NhanDoiDong();

        var hangNut = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Margin = new Padding(0),
        };
        hangNut.Controls.Add(btnNhieuDong);
        hangNut.Controls.Add(btnBoHang);
        hangNut.Controls.Add(btnChepNgay);
        hangNut.Controls.Add(btnNhanDoi);

        var xep = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.ChinhNhat,
        };
        xep.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        xep.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        xep.Controls.Add(hang, 0, 0);
        xep.Controls.Add(hangNut, 0, 1);

        nen.Controls.Add(xep);
        return nen;
    }

    /// <summary>Lọc danh mục theo kiểu gõ tắt và bung gợi ý ngay khi đang gõ.</summary>
    private void LocDanhMucHang()
    {
        if (_dangNap)
        {
            return;
        }

        var dangGo = _cboHang.Text;
        var khop = _danhMucHang
            .Select(v => (VatTu: v, Diem: TimHang.Diem(v.Ten, v.MaTat, dangGo)))
            .Where(x => x.Diem > 0)
            .OrderByDescending(x => x.Diem)
            .ThenBy(x => x.VatTu.Ten.Length)
            .ThenBy(x => x.VatTu.Ten, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => x.VatTu)
            .Take(50)
            .ToList();

        _dangNap = true;
        _cboHang.BeginUpdate();
        _cboHang.Items.Clear();
        foreach (var vatTu in khop)
        {
            _cboHang.Items.Add(vatTu);
        }

        _cboHang.EndUpdate();
        _cboHang.Text = dangGo;
        _dangNap = false;

        _cboHang.DroppedDown = dangGo.Length > 0 && khop.Count > 0;
        Cursor.Current = Cursors.Default;
        _cboHang.SelectionStart = dangGo.Length;
        _cboHang.SelectionLength = 0;
    }

    /// <summary>Sau khi rời ô, thay phép tính bằng kết quả để nhìn là thấy con số thật.</summary>
    private static void ChotPhepTinh(TextBox o, Func<decimal, string> dinhDang)
    {
        var chu = o.Text.Trim();
        if (chu.Length == 0 || So.TryDoc(chu, out _))
        {
            return;
        }

        if (So.TryTinh(chu, out var giaTri))
        {
            o.Text = dinhDang(giaTri);
        }
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

            if (e.CellStyle is not { } kieu)
            {
                return;
            }

            var cot = _luoiCT.Columns[e.ColumnIndex].DataPropertyName;
            if (cot == nameof(ChiTietHoaDon.ThanhTien))
            {
                kieu.Font = Theme.FontLuoiDam;
                kieu.BackColor = Color.FromArgb(248, 250, 253);
            }

            // Dòng khách trả lại hàng: số âm, tô đỏ cho khỏi đọc nhầm thành hàng đã lấy.
            if (_luoiCT.Rows[e.RowIndex].DataBoundItem is ChiTietHoaDon { LaTraLai: true }
                && cot is nameof(ChiTietHoaDon.SoLuong) or nameof(ChiTietHoaDon.ThanhTien))
            {
                kieu.ForeColor = Theme.Do;
            }
        };

        _luoiCT.ContextMenuStrip = TaoMenuChuot();

        // Bấm chuột phải lên dòng nào thì chọn luôn dòng đó, để lệnh trong menu áp vào đúng dòng
        // người dùng đang trỏ tới chứ không phải dòng đang chọn từ trước.
        _luoiCT.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                _luoiCT.CurrentCell = _luoiCT.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
        };

        _luoiCT.DataSource = _nguonCT;
        return _luoiCT;
    }

    /// <summary>Menu chuột phải trên lưới chi tiết: chèn, nhân đôi, đổi chỗ, xoá dòng.</summary>
    private ContextMenuStrip TaoMenuChuot()
    {
        var menu = new ContextMenuStrip { Font = Theme.FontThuong };

        void Them(string chu, Action lam) => menu.Items.Add(chu, null, (_, _) => lam());

        Them("Chèn dòng lên trên          Ctrl+Enter", () => ThemDong(chen: true));
        Them("Chèn dòng xuống dưới     Ctrl+Shift+Enter", () => ThemDong(chen: true, chenDuoi: true));
        menu.Items.Add(new ToolStripSeparator());
        Them("Nhân đôi dòng                 Ctrl+D", NhanDoiDong);
        Them("Chuyển lên                        Alt+↑", () => ChuyenDong(xuong: false));
        Them("Chuyển xuống                   Alt+↓", () => ChuyenDong(xuong: true));
        menu.Items.Add(new ToolStripSeparator());
        Them("Xoá dòng                            Delete", XoaDong);

        return menu;
    }

    private Control TaoThanhTongTien()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(0, 8, 0, 0) };

        var btnChen = Theme.NutPhu("⤒  Chèn dòng (Ctrl+Enter)", 260, 46);
        btnChen.Margin = new Padding(0, 0, 10, 0);
        btnChen.Click += (_, _) => ThemDong(chen: true);

        var btnXoaDong = Theme.NutPhu("Xoá dòng (Delete)", 210, 46);
        btnXoaDong.ForeColor = Theme.Do;
        btnXoaDong.Click += (_, _) => XoaDong();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnChen);
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
        _lblTrangThai.Text = "Enter thêm dòng vào cuối · Ctrl+Enter chèn lên trên dòng đang chọn · Alt+↑/↓ đổi chỗ dòng · "
            + "F3 về ô Tên hàng · Bấm đúp để sửa · Ctrl+D nhân đôi · Delete xoá dòng · Ctrl+Z hoàn tác · Ctrl+Y làm lại";

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
        _danhMucHang.Clear();
        _danhMucHang.AddRange(_kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase));

        _dangNap = true;
        var dangGo = _cboHang.Text;
        _cboHang.Items.Clear();
        foreach (var vatTu in _danhMucHang)
        {
            _cboHang.Items.Add(vatTu);
        }

        _cboHang.Text = dangGo;
        _dangNap = false;
    }

    /// <summary>Mặt hàng khớp nhất với chuỗi đang gõ, kể cả gõ tắt. Trả về kèm điểm khớp.</summary>
    private (VatTu VatTu, int Diem)? TimHangGanNhat(string ten) => _danhMucHang
        .Select(v => (VatTu: v, Diem: TimHang.Diem(v.Ten, v.MaTat, ten)))
        .Where(x => x.Diem > 0)
        .OrderByDescending(x => x.Diem)
        .ThenBy(x => x.VatTu.Ten.Length)
        .Cast<(VatTu VatTu, int Diem)?>()
        .FirstOrDefault();

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
        _cboHoaDon.BeginUpdate();
        _cboHoaDon.Items.Clear();

        foreach (var hoaDon in _kho.HoaDonCuaKhach(_khachId, NamDangChon))
        {
            _cboHoaDon.Items.Add(new DongHoaDon { HD = hoaDon });
        }

        _cboHoaDon.EndUpdate();

        _hoaDonId = null;
        if (_cboHoaDon.Items.Count > 0)
        {
            var viTri = 0;
            if (chon is { } id)
            {
                for (var i = 0; i < _cboHoaDon.Items.Count; i++)
                {
                    if (((DongHoaDon)_cboHoaDon.Items[i]!).HD.Id == id)
                    {
                        viTri = i;
                        break;
                    }
                }
            }

            _cboHoaDon.SelectedIndex = viTri;
            _hoaDonId = ((DongHoaDon)_cboHoaDon.Items[viTri]!).HD.Id;
        }

        _cboHoaDon.Enabled = _cboHoaDon.Items.Count > 0;
        _dangNap = false;
        NapChiTiet();
    }

    private void NapChiTiet(Guid? chonDong = null)
    {
        var hoaDon = HoaDonHienTai;
        var dong = hoaDon is null
            ? new List<ChiTietHoaDon>()
            : ThuTuDong.TheoThuTu(hoaDon.ChiTiet);

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
        var thanhTien = Math.Round(So.Tinh(_txtDonGia.Text) * So.Tinh(_txtSoLuong.Text), 0, MidpointRounding.AwayFromZero);
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

    /// <summary>
    /// Thêm một dòng hàng. <paramref name="traLai"/> là khách trả hàng về: số lượng ghi số âm
    /// nên thành tiền trừ bớt vào hoá đơn, in ra có dấu trừ.
    /// <para>
    /// <paramref name="chen"/> thì dòng mới nằm ngay cạnh dòng đang chọn trên lưới thay vì
    /// xuống cuối bảng, và lấy luôn ngày của dòng đó — có vậy nó mới đứng yên đúng chỗ.
    /// </para>
    /// </summary>
    private void ThemDong(bool traLai = false, bool chen = false, bool chenDuoi = false)
    {
        if (Khach is not { } khach || HopThoai.ChanKhiChiXem(this, _kho))
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

        var soLuong = So.Tinh(_txtSoLuong.Text);
        if (traLai)
        {
            soLuong = -Math.Abs(soLuong);
        }

        if (soLuong == 0)
        {
            HopThoai.CanhBao(
                this,
                "Hãy nhập số lượng khác 0.\n\n" +
                "Gõ được cả phép tính, ví dụ: 3+2*4.\n" +
                "Khách trả lại hàng thì bấm nút TRẢ LẠI, hoặc gõ số âm: -2");
            _txtSoLuong.Focus();
            _txtSoLuong.SelectAll();
            return;
        }

        var donGia = So.Tinh(_txtDonGia.Text);
        var donVi = _txtDonVi.Text.Trim();

        // Chèn thì dòng mới lấy ngày của dòng đang chọn, nếu không nó sẽ bị xếp sang chỗ khác.
        var moc = chen ? _luoiCT.CurrentRow?.DataBoundItem as ChiTietHoaDon : null;
        var ngay = moc?.Ngay.Date ?? _dtNgay.Value.Date;

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

        // Gõ tắt: chuỗi vừa gõ không có trong danh mục nhưng khớp một mặt hàng.
        // Mã tắt khớp hẳn thì dùng luôn, còn khớp mờ thì hỏi lại — tránh gõ tắt lại đẻ ra hàng mới.
        if (vatTu is null && TimHangGanNhat(ten) is { } goiY)
        {
            var dungGoiY = goiY.Diem >= 90
                || HopThoai.Hoi(
                    this,
                    $"Danh mục chưa có \"{ten}\".\n\nÝ anh là \"{goiY.VatTu.Ten}\" phải không?\n\n" +
                    $"Chọn Không nếu muốn thêm \"{ten}\" thành mặt hàng mới.");

            if (dungGoiY)
            {
                vatTu = goiY.VatTu;
                ten = vatTu.Ten;
                if (donVi.Length == 0)
                {
                    donVi = vatTu.DonVi;
                }

                if (donGia <= 0m)
                {
                    donGia = _kho.GiaCho(khach, vatTu);
                }
            }
        }

        if (_kho.CaiDat.CanhBaoDongTrung
            && KiemTra.DongTrung(hoaDonDangChon, ngay, ten, soLuong) is { } dongTrung
            && !HopThoai.Hoi(
                this,
                $"Hoá đơn đã có sẵn dòng y hệt:\n\n" +
                $"{dongTrung.TenHang} × {So.Luong(dongTrung.SoLuong)} ngày {dongTrung.Ngay:dd/MM/yyyy}\n\n" +
                "Vẫn thêm thêm một dòng nữa?"))
        {
            return;
        }

        if (KiemTra.LechGia(_kho.HoaDonCuaKhach(_khachId), ten, vatTu?.Id, donGia, _kho.CaiDat.NguongLechGia)
                is { } lech
            && !HopThoai.Hoi(
                this,
                $"Lần gần nhất bán \"{ten}\" cho {khach.Ten} (ngày {lech.Ngay:dd/MM/yyyy}) là {So.Tien(lech.GiaCu)}.\n" +
                $"Lần này nhập {So.Tien(donGia)} — lệch {PhanTramLech(lech.GiaCu, donGia)}%.\n\n" +
                "Giá này có đúng không?"))
        {
            _txtDonGia.Focus();
            _txtDonGia.SelectAll();
            return;
        }

        if (KiemTra.TraLaiQuaSoDaMua(_kho.HoaDonCuaKhach(_khachId), ten, vatTu?.Id, soLuong) is { } dangGiu
            && !HopThoai.Hoi(
                this,
                $"Sổ đang ghi khách giữ {So.Luong(dangGiu)} \"{ten}\", " +
                $"lần này trả lại {So.Luong(Math.Abs(soLuong))}.\n\n" +
                "Vẫn ghi trả lại chừng này?"))
        {
            _txtSoLuong.Focus();
            _txtSoLuong.SelectAll();
            return;
        }

        // Hỏi trước khi ghi để mọi thay đổi nằm gọn trong một bước hoàn tác.
        var vatTuMoi = vatTu is null;
        var luuGiaRieng = vatTuMoi;

        // Dòng trả lại chỉ trả hàng về, không phải lần bán mới nên đừng đổi giá riêng của khách.
        if (vatTu is not null && donGia > 0 && soLuong > 0)
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

        var moTa = (soLuong, moc) switch
        {
            ( < 0, null) => $"Trả lại \"{ten}\" ngày {ngay:dd/MM/yyyy}",
            ( < 0, not null) => $"Chèn dòng trả lại \"{ten}\" ngày {ngay:dd/MM/yyyy}",
            (_, null) => $"Thêm \"{ten}\" ngày {ngay:dd/MM/yyyy}",
            _ => $"Chèn \"{ten}\" ngày {ngay:dd/MM/yyyy}",
        };

        _kho.ThucHien(moTa, () =>
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

            if (moc is null)
            {
                hoaDon.ChiTiet.Add(dongMoi);
            }
            else
            {
                ThuTuDong.Chen(hoaDon.ChiTiet, dongMoi, moc.Id, chenDuoi);
            }
        }, phatSuKien: false);

        if (vatTuMoi)
        {
            NapDanhMucHang();
        }

        _hoaDonId = hoaDon.Id;
        NapHoaDon(hoaDon.Id);
        NapChiTiet(dongMoi.Id);

        var viecDaLam = (soLuong < 0, moc is not null) switch
        {
            (true, false) => "Đã ghi trả lại",
            (true, true) => "Đã chèn dòng trả lại",
            (false, false) => "Đã thêm",
            _ => "Đã chèn",
        };

        _lblTrangThai.Text = $"{viecDaLam}: {ten} × {So.Luong(Math.Abs(soLuong))} = {So.Tien(dongMoi.ThanhTien)}"
            + (moc is null
                ? string.Empty
                : $" — {(chenDuoi ? "ngay dưới" : "ngay trên")} dòng \"{moc.TenHang}\", ngày {ngay:dd/MM/yyyy}")
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

        // Nhớ trước dòng liền kề để xoá xong con trỏ đứng ngay chỗ cũ, khỏi nhảy về đầu bảng —
        // xoá mấy dòng ở giữa một hoá đơn dài mới đỡ phải cuộn lại từ đầu mỗi lần.
        var thuTu = ThuTuDong.TheoThuTu(hoaDon.ChiTiet);
        var viTri = thuTu.FindIndex(c => c.Id == dong.Id);
        var dongKe = viTri + 1 < thuTu.Count ? thuTu[viTri + 1]
            : viTri > 0 ? thuTu[viTri - 1]
            : null;

        _kho.ThucHien($"Xoá dòng \"{dong.TenHang}\"", () => hoaDon.ChiTiet.RemoveAll(c => c.Id == dong.Id), phatSuKien: false);

        NapHoaDon(_hoaDonId);
        NapChiTiet(dongKe?.Id);
        _lblTrangThai.Text = $"Đã xoá dòng {dong.TenHang}. Bấm Ctrl+Z để lấy lại.";
    }

    private static int PhanTramLech(decimal giaCu, decimal giaMoi) =>
        giaCu == 0m ? 0 : (int)Math.Round(Math.Abs(giaMoi - giaCu) / giaCu * 100m, MidpointRounding.AwayFromZero);

    // ---------------- Nhập nhanh nhiều dòng ----------------

    /// <summary>Hoá đơn để ghi thêm dòng; chưa có thì tự tạo. Trả về null nếu không ghi được.</summary>
    private HoaDon? HoaDonDeGhi(DateTime ngay, out bool taoMoi)
    {
        taoMoi = false;
        var hoaDon = HoaDonHienTai;

        if (hoaDon is { DaChot: true })
        {
            HopThoai.CanhBao(this, "Hoá đơn này đã chốt. Hãy bấm \"Mở lại hoá đơn\" trước khi thêm hàng.");
            return null;
        }

        if (hoaDon is not null)
        {
            return hoaDon;
        }

        taoMoi = true;
        return new HoaDon
        {
            KhachHangId = _khachId,
            Nam = NamDangChon,
            MaHoaDon = _kho.TaoMaHoaDon(_khachId, NamDangChon),
            NgayMo = ngay,
        };
    }

    /// <summary>Ghi một loạt dòng vào hoá đơn trong đúng một bước hoàn tác.</summary>
    private void GhiNhieuDong(List<ChiTietHoaDon> dongMoi, string moTa)
    {
        if (dongMoi.Count == 0)
        {
            return;
        }

        var ngay = dongMoi.Min(d => d.Ngay);
        var hoaDon = HoaDonDeGhi(ngay, out var taoMoi);
        if (hoaDon is null)
        {
            return;
        }

        _kho.ThucHien(moTa, () =>
        {
            if (taoMoi)
            {
                _kho.DuLieu.HoaDons.Add(hoaDon);
            }

            foreach (var dong in dongMoi)
            {
                // Tên hàng chưa có trong danh mục thì thêm luôn, giống như khi thêm từng dòng.
                if (dong.VatTuId is null && dong.TenHang.Length > 0)
                {
                    var vatTu = _kho.TimVatTuTheoTen(dong.TenHang);
                    if (vatTu is null)
                    {
                        vatTu = new VatTu { Ten = dong.TenHang, DonVi = dong.DonVi, DonGiaMacDinh = dong.DonGia };
                        _kho.DuLieu.VatTus.Add(vatTu);
                    }

                    dong.VatTuId = vatTu.Id;
                }

                hoaDon.ChiTiet.Add(dong);
            }
        }, phatSuKien: false);

        NapDanhMucHang();
        _hoaDonId = hoaDon.Id;
        NapHoaDon(hoaDon.Id);

        var tong = dongMoi.Sum(d => d.ThanhTien);
        _lblTrangThai.Text = $"Đã thêm {dongMoi.Count} dòng, tạm tính {So.Tien(tong)}. Bấm Ctrl+Z nếu muốn bỏ.";
    }

    private void NhapNhieuDong()
    {
        if (Khach is null)
        {
            return;
        }

        using var form = new NhapNhieuDongForm(_khachId, _dtNgay.Value.Date);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua.Count == 0)
        {
            return;
        }

        GhiNhieuDong(form.KetQua, $"Nhập nhanh {form.KetQua.Count} dòng");
    }

    private void ChonBoHang()
    {
        if (Khach is not { } khach)
        {
            return;
        }

        using var form = new BoHangForm(deChon: true);
        if (form.ShowDialog(this) != DialogResult.OK || form.BoDaChon is not { } bo)
        {
            NapDanhMucHang();
            return;
        }

        var ngay = _dtNgay.Value.Date;
        var dongMoi = new List<ChiTietHoaDon>();
        foreach (var mon in bo.Dong)
        {
            var vatTu = mon.VatTuId is { } id ? _kho.TimVatTu(id) : _kho.TimVatTuTheoTen(mon.TenHang);
            dongMoi.Add(new ChiTietHoaDon
            {
                Ngay = ngay,
                VatTuId = vatTu?.Id,
                TenHang = vatTu?.Ten ?? mon.TenHang,
                DonVi = string.IsNullOrWhiteSpace(mon.DonVi) ? vatTu?.DonVi ?? string.Empty : mon.DonVi,
                DonGia = vatTu is null ? 0m : _kho.GiaCho(khach, vatTu),
                SoLuong = mon.SoLuong,
            });
        }

        GhiNhieuDong(dongMoi, $"Thêm bộ hàng {bo.Ten}");
    }

    private void ChepNgay()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            HopThoai.CanhBao(this, "Chưa có hoá đơn nào để chép.");
            return;
        }

        if (hoaDon.ChiTiet.Count == 0)
        {
            HopThoai.CanhBao(this, "Hoá đơn này chưa có dòng hàng nào để chép.");
            return;
        }

        using var form = new ChepNgayForm(hoaDon, _dtNgay.Value.Date);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var nguon = hoaDon.ChiTiet.Where(c => c.Ngay.Date == form.NgayNguon).ToList();
        var dich = form.NgayDich;
        var dongMoi = nguon.Select(c => new ChiTietHoaDon
        {
            Ngay = dich,
            VatTuId = c.VatTuId,
            TenHang = c.TenHang,
            DonVi = c.DonVi,
            DonGia = c.DonGia,
            SoLuong = c.SoLuong,
            GhiChu = c.GhiChu,
        }).ToList();

        GhiNhieuDong(dongMoi, $"Chép {dongMoi.Count} dòng ngày {form.NgayNguon:dd/MM/yyyy} sang {dich:dd/MM/yyyy}");
    }

    private void NhanDoiDong()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            return;
        }

        if (hoaDon.DaChot)
        {
            HopThoai.CanhBao(this, "Hoá đơn đã chốt, không thêm dòng được.");
            return;
        }

        if (_luoiCT.CurrentRow?.DataBoundItem is not ChiTietHoaDon dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn dòng muốn nhân đôi.");
            return;
        }

        var ban = new ChiTietHoaDon
        {
            Ngay = dong.Ngay,
            VatTuId = dong.VatTuId,
            TenHang = dong.TenHang,
            DonVi = dong.DonVi,
            DonGia = dong.DonGia,
            SoLuong = dong.SoLuong,
            GhiChu = dong.GhiChu,
        };

        // Bản sao nằm ngay dưới dòng gốc cho dễ nhìn, khỏi phải mò xuống cuối bảng.
        _kho.ThucHien(
            $"Nhân đôi dòng \"{dong.TenHang}\"",
            () => ThuTuDong.Chen(hoaDon.ChiTiet, ban, dong.Id, chenDuoi: true),
            phatSuKien: false);

        NapHoaDon(hoaDon.Id);
        NapChiTiet(ban.Id);
        _lblTrangThai.Text = $"Đã nhân đôi dòng {dong.TenHang}. Sửa lại số lượng nếu cần.";
    }

    /// <summary>Đổi chỗ dòng đang chọn với dòng liền kề, để xếp lại thứ tự in ra giấy.</summary>
    private void ChuyenDong(bool xuong)
    {
        if (HoaDonHienTai is not { } hoaDon || HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        if (hoaDon.DaChot)
        {
            HopThoai.CanhBao(this, "Hoá đơn đã chốt, không đổi thứ tự dòng được.");
            return;
        }

        if (_luoiCT.CurrentRow?.DataBoundItem is not ChiTietHoaDon dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn dòng muốn chuyển.");
            return;
        }

        // Chuyển thử trước rồi mới ghi lịch sử, để không đẻ ra bước hoàn tác rỗng khi dòng
        // đã nằm ở đầu / cuối ngày của nó.
        var truoc = _kho.ChupNhanh();
        if (!ThuTuDong.Chuyen(hoaDon.ChiTiet, dong.Id, xuong))
        {
            _lblTrangThai.Text = $"Dòng \"{dong.TenHang}\" đã ở {(xuong ? "cuối" : "đầu")} ngày "
                + $"{dong.Ngay:dd/MM/yyyy} rồi. Muốn sang ngày khác thì sửa ô NGÀY.";
            return;
        }

        _kho.GhiNhan(truoc, $"Chuyển dòng \"{dong.TenHang}\" {(xuong ? "xuống" : "lên")}", phatSuKien: false);

        NapChiTiet(dong.Id);
        _lblTrangThai.Text = $"Đã chuyển dòng {dong.TenHang} {(xuong ? "xuống dưới" : "lên trên")}. "
            + "Bấm Ctrl+Z nếu muốn quay lại.";
    }

    private void SoanTinNhacNo()
    {
        if (Khach is not { } khach)
        {
            return;
        }

        var hoaDons = _kho.HoaDonCuaKhach(_khachId);
        var conNo = hoaDons.Sum(h => h.ConLai);
        if (conNo <= 0m)
        {
            HopThoai.Bao(this, $"{khach.Ten} không còn nợ khoản nào.");
            return;
        }

        var noiDung = TinNhacNo.Soan(khach, hoaDons, DateTime.Today, ThongTinCuaHang.DocTuMau());
        using var form = new VanBanForm(
            "Tin nhắc nợ",
            $"{khach.Ten} — còn nợ {So.Tien(conNo)}. Sửa lại lời cho hợp rồi chép đi gửi.",
            noiDung);
        form.ShowDialog(this);
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

    private void MoThuTien()
    {
        if (Khach is null)
        {
            return;
        }

        using var form = new ThuTienForm(_khachId);
        form.ShowDialog(this);
        NapHoaDon(_hoaDonId);
        _lblTrangThai.Text = "Đã cập nhật tiền khách trả.";
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
            case Keys.Control | Keys.D when !dangSuaO:
                NhanDoiDong();
                return true;
            case Keys.Control | Keys.Enter when !dangSuaO:
                ThemDong(chen: true);
                return true;
            case Keys.Control | Keys.Shift | Keys.Enter when !dangSuaO:
                ThemDong(chen: true, chenDuoi: true);
                return true;
            // Chỉ nhận khi đang đứng ở lưới, để Alt+↑/↓ vẫn mở được danh sách gợi ý tên hàng.
            case Keys.Alt | Keys.Up when !dangSuaO && _luoiCT.Focused:
                ChuyenDong(xuong: false);
                return true;
            case Keys.Alt | Keys.Down when !dangSuaO && _luoiCT.Focused:
                ChuyenDong(xuong: true);
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

    /// <summary>
    /// Một dòng trong ô chọn hoá đơn. Chỉ mã và ngày mở, không kèm tiền: tiền của hoá đơn
    /// đang xem đã nằm ở thanh tổng dưới lưới, mà tiền thì đổi liên tục theo từng dòng hàng
    /// vừa gõ — chép vào đây là phải nhớ cập nhật lại từng lần.
    /// </summary>
    private sealed class DongHoaDon
    {
        public HoaDon HD { get; set; } = null!;

        public override string ToString() =>
            $"{HD.MaHoaDon}  ·  {HD.NgayMo:dd/MM/yyyy}" + (HD.DaChot ? "  ·  đã chốt" : string.Empty);
    }
}
