using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
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
    private readonly TextBox _txtTim = Theme.O(440);
    private readonly CheckBox _chkCoDon = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTongKet = new();
    private readonly Label _lblTrangThai = new();
    private readonly Label _lblPhimTat = new();
    private readonly Label _lblNhacNo = new();
    private readonly Panel _nenNhacNo = new();
    private readonly Label _lblTenThe = new();

    // Bốn ô số liệu của thẻ tổng quan. Màu nhãn lấy đúng bốn màu bản thiết kế dùng cho
    // bốn nhóm số liệu, để nhìn một cái là phân biệt được nhóm nào.
    private readonly OThongKe _oKhach = new("Khách hàng", Theme.Chinh);
    private readonly OThongKe _oTongMua = new("Tổng mua", Theme.Cam);
    private readonly OThongKe _oDaThu = new("Đã thu", Theme.Xanh);
    private readonly OThongKe _oConNo = new("Còn nợ", Theme.Do);

    private ThanhBen _thanhBen = null!;
    private ThanhBen.MucBen _mucTrangChu = null!;

    /// <summary>Thỉnh thoảng ngó lại file dữ liệu xem máy khác có sửa không.</summary>
    private readonly System.Windows.Forms.Timer _dongHoNgoFile = new() { Interval = 20_000 };

    private bool _dangNap;
    private bool _daBaoFileBiSua;

    public MainForm()
    {
        Text = _kho.ChiXem
            ? "Quản lý đơn hàng – Cửa hàng điện nước  [CHỈ XEM]"
            : "Quản lý đơn hàng – Cửa hàng điện nước";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 720);
        Size = new Size(1440, 860);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        _kho.DuLieuThayDoi += Kho_DuLieuThayDoi;
        _kho.ThaoTacBiChan += Kho_ThaoTacBiChan;
        FormClosed += (_, _) =>
        {
            _kho.DuLieuThayDoi -= Kho_DuLieuThayDoi;
            _kho.ThaoTacBiChan -= Kho_ThaoTacBiChan;
            _dongHoNgoFile.Stop();
        };

        _dongHoNgoFile.Tick += (_, _) => NgoFileDuLieu();
        _dongHoNgoFile.Start();

        NapNam();
        NapDanhSach();
    }

    private int NamDangChon => _cboNam.SelectedItem is int nam ? nam : DateTime.Today.Year;

    private KhachHang? KhachDangChon => (_luoi.CurrentRow?.DataBoundItem as DongKhach)?.Khach;

    // ---------------- Giao diện ----------------

    /// <summary>
    /// Vỏ cửa sổ dựng theo bản thiết kế trên Figma: thanh bên trắng bên trái để đi lại giữa
    /// các phần, thanh tìm kiếm ở trên, rồi tới thẻ số liệu và thẻ bảng khách hàng.
    /// Tên phần mềm không nhắc lại trong khung: thanh cửa sổ của Windows đã có.
    /// </summary>
    private void TaoGiaoDien()
    {
        // Khu chính (neo Fill) phải thêm trước, thanh bên (neo trái) thêm sau: WinForms xếp
        // các control neo theo thứ tự từ cái thêm sau về cái thêm trước, nên cái thêm sau
        // chiếm cạnh của nó trước rồi cái Fill mới ăn phần còn lại. Thêm ngược lại thì khu
        // chính chiếm hết bề ngang, thanh bên không còn chỗ.
        Controls.Add(TaoKhuChinh());
        Controls.Add(TaoThanhBen());
    }

    private Control TaoThanhBen()
    {
        _thanhBen = new ThanhBen("Sổ điện nước", "Quản lý đơn hàng");

        _mucTrangChu = _thanhBen.Them("Trang chủ", KieuIcon.Nha, () => { });
        _thanhBen.Them("Sổ công nợ", KieuIcon.Tien, MoSoCongNo);
        _thanhBen.Them("Danh mục vật tư", KieuIcon.Thung, MoDanhMucVatTu);
        _thanhBen.Them("Bộ hàng thường dùng", KieuIcon.Bo, () =>
        {
            using var form = new BoHangForm();
            form.ShowDialog(this);
        });
        _thanhBen.Ngan();
        _thanhBen.Them("Sao lưu và khôi phục", KieuIcon.Luu, () =>
        {
            using var form = new SaoLuuForm();
            form.ShowDialog(this);
            NapDanhSach();
        });
        _thanhBen.Them("Nhật ký thay đổi", KieuIcon.DongHo, () =>
        {
            using var form = new NhatKyForm();
            form.ShowDialog(this);
        });

        _thanhBen.Chon(_mucTrangChu);
        return _thanhBen;
    }

    private Control TaoKhuChinh()
    {
        var khu = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        khu.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        khu.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        khu.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        khu.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khu.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        khu.Controls.Add(TaoThanhTren(), 0, 0);
        khu.Controls.Add(TaoTheTongQuan(), 0, 1);
        khu.Controls.Add(TaoThanhNhacNo(), 0, 2);
        khu.Controls.Add(TaoTheKhachHang(), 0, 3);
        khu.Controls.Add(TaoThanhTrangThai(), 0, 4);
        return khu;
    }

    /// <summary>Thanh trên: ô tìm khách bên trái, chọn năm và nút thêm khách bên phải.</summary>
    private Control TaoThanhTren()
    {
        var nen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Trang,
            Margin = new Padding(0),
            Padding = new Padding(24, 0, 24, 0),
        };
        nen.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var but = new Pen(Theme.Vien);
            e.Graphics.DrawLine(but, 0, p.Height - 1, p.Width, p.Height - 1);
        };

        _txtTim.TextChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        var hopTim = Theme.HopTim(_txtTim, "Tìm khách hàng theo tên, số điện thoại, địa chỉ", 440);
        hopTim.Location = new Point(24, 19);
        nen.Controls.Add(hopTim);

        _cboNam.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNam.FlatStyle = FlatStyle.Flat;
        _cboNam.Font = Theme.FontNhap;
        _cboNam.Width = 110;
        _cboNam.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        var btnThemKhach = Theme.Nut("+  Thêm khách hàng", Theme.Chinh, 220, 40);
        btnThemKhach.Click += (_, _) => ThemKhach();

        var lblNam = new Label
        {
            Text = "Năm",
            Font = Theme.FontThuong,
            ForeColor = Theme.Xam,
            AutoSize = true,
            Margin = new Padding(0, 10, 8, 0),
        };

        var benPhai = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            BackColor = Theme.Trang,
            Padding = new Padding(0, 19, 0, 0),
        };
        benPhai.Controls.Add(lblNam);
        benPhai.Controls.Add(_cboNam);
        benPhai.Controls.Add(btnThemKhach);
        nen.Controls.Add(benPhai);

        _cboNam.Margin = new Padding(0, 2, 16, 0);
        btnThemKhach.Margin = new Padding(0);
        return nen;
    }

    /// <summary>
    /// Thẻ số liệu đầu trang, xếp bốn ô cạnh nhau như khối "Overall Inventory" của bản thiết kế:
    /// bao nhiêu khách, mua bao nhiêu, đã thu bao nhiêu, còn nợ bao nhiêu.
    /// </summary>
    private Control TaoTheTongQuan()
    {
        var the = new Theme.The
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(24, 18, 24, 0),
            Padding = new Padding(20, 12, 20, 12),
        };

        _lblTenThe.Text = "Tổng quan";
        _lblTenThe.Font = Theme.FontTenThe;
        _lblTenThe.ForeColor = Theme.ChuDam;
        _lblTenThe.Dock = DockStyle.Top;
        _lblTenThe.Height = 30;
        _lblTenThe.TextAlign = ContentAlignment.MiddleLeft;

        var hang = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Trang,
        };
        for (var i = 0; i < 4; i++)
        {
            hang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        _oConNo.CoVachPhai = false;
        hang.Controls.Add(_oKhach, 0, 0);
        hang.Controls.Add(_oTongMua, 1, 0);
        hang.Controls.Add(_oDaThu, 2, 0);
        hang.Controls.Add(_oConNo, 3, 0);
        foreach (var o in new[] { _oKhach, _oTongMua, _oDaThu, _oConNo })
        {
            o.Dock = DockStyle.Fill;
            o.Margin = new Padding(0);
        }

        the.Controls.Add(hang);
        the.Controls.Add(_lblTenThe);
        return the;
    }

    /// <summary>Dải nhắc nợ: mở phần mềm lên là thấy ai đang nợ lâu, kèm nút mở sổ công nợ.</summary>
    private Control TaoThanhNhacNo()
    {
        _nenNhacNo.Dock = DockStyle.Fill;
        _nenNhacNo.Margin = new Padding(24, 14, 24, 0);
        // Chừa lề: nhãn bên trong tô nền đặc, phải nằm gọn trong phần thẳng của hình bo góc
        // chứ chạm vào bốn góc là lộ ra góc vuông.
        _nenNhacNo.Padding = new Padding(18, 6, 208, 6);
        _nenNhacNo.BackColor = Theme.ChinhNhat;

        // Bo góc cho khớp với các thẻ bên trên: xoá bằng màu nền cửa sổ rồi tô hình bo lên.
        // Nút bên trong lấy BackColor của dải này làm màu nền nên vẫn liền màu.
        _nenNhacNo.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            var g = e.Graphics;
            g.Clear(Theme.Nen);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var duong = Theme.DuongBo(new Rectangle(0, 0, p.Width - 1, p.Height - 1), Theme.BoThe);
            using var to = new SolidBrush(p.BackColor);
            g.FillPath(to, duong);
            using var but = new Pen(ControlPaint.Dark(p.BackColor, 0.06f));
            g.DrawPath(but, duong);
        };

        _lblNhacNo.Dock = DockStyle.Fill;
        _lblNhacNo.Font = Theme.FontDam;
        _lblNhacNo.TextAlign = ContentAlignment.MiddleLeft;

        var btnSoCongNo = Theme.NutPhu("Mở sổ công nợ", 186, 40);
        btnSoCongNo.ForeColor = Theme.Chinh;
        btnSoCongNo.Click += (_, _) => MoSoCongNo();

        // Đặt tay chứ không neo phải: neo phải thì nút cao bằng cả dải, mà nút nằm trên dải
        // màu nên nền của nó phải lấy đúng màu dải — neo vào panel trung gian là sai màu.
        void XepNut()
        {
            btnSoCongNo.Location = new Point(
                Math.Max(0, _nenNhacNo.Width - btnSoCongNo.Width - 14),
                Math.Max(0, (_nenNhacNo.Height - btnSoCongNo.Height) / 2));
        }

        _nenNhacNo.SizeChanged += (_, _) => XepNut();

        _nenNhacNo.Controls.Add(btnSoCongNo);
        _nenNhacNo.Controls.Add(_lblNhacNo);
        XepNut();
        return _nenNhacNo;
    }

    /// <summary>
    /// Thẻ bảng khách hàng: tên thẻ và ô lọc ở trên, bảng ở giữa, các nút việc ở chân thẻ —
    /// đúng cách bản thiết kế xếp thẻ "Products".
    /// </summary>
    private Control TaoTheKhachHang()
    {
        var the = new Theme.The
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(24, 14, 24, 12),
            Padding = new Padding(18, 12, 18, 10),
        };

        the.Controls.Add(TaoLuoi());
        the.Controls.Add(TaoChanThe());
        the.Controls.Add(TaoDauThe());
        return the;
    }

    private Control TaoDauThe()
    {
        var dau = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.Trang };

        var lblTen = Theme.TenThe("Khách hàng");
        lblTen.Location = new Point(2, 10);
        dau.Controls.Add(lblTen);

        _chkCoDon.Text = "Chỉ hiện khách có đơn trong năm";
        _chkCoDon.Font = Theme.FontThuong;
        _chkCoDon.ForeColor = Theme.Xam;
        _chkCoDon.AutoSize = true;
        _chkCoDon.Dock = DockStyle.Right;
        _chkCoDon.Padding = new Padding(0, 0, 4, 0);
        _chkCoDon.TextAlign = ContentAlignment.MiddleLeft;
        _chkCoDon.CheckedChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };
        dau.Controls.Add(_chkCoDon);
        return dau;
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
        _luoi.Dock = DockStyle.Fill;
        return _luoi;
    }

    private Control TaoChanThe()
    {
        var nen = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = Theme.Trang, Padding = new Padding(0, 10, 0, 0) };

        var btnMo = Theme.Nut("Mở đơn hàng", Theme.Chinh, 190, 42);
        btnMo.Click += (_, _) => MoDonHang();

        var btnThuTien = Theme.NutPhu("Thu tiền", 150, 42);
        btnThuTien.ForeColor = Theme.Xanh;
        btnThuTien.Click += (_, _) => ThuTienCuaKhach();

        var btnSua = Theme.NutPhu("Sửa khách", 140, 42);
        btnSua.Click += (_, _) => SuaKhach();

        var btnXoa = Theme.NutPhu("Xoá khách", 140, 42);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => XoaKhach();

        var trai = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            BackColor = Theme.Trang,
        };
        trai.Controls.Add(btnMo);
        trai.Controls.Add(btnThuTien);
        trai.Controls.Add(btnSua);
        trai.Controls.Add(btnXoa);

        _lblTongKet.Dock = DockStyle.Right;
        _lblTongKet.TextAlign = ContentAlignment.MiddleRight;
        _lblTongKet.Font = Theme.FontThuong;
        _lblTongKet.ForeColor = Theme.Xam;
        _lblTongKet.AutoSize = false;
        _lblTongKet.Width = 420;
        _lblTongKet.BackColor = Theme.Trang;

        nen.Controls.Add(trai);
        nen.Controls.Add(_lblTongKet);
        return nen;
    }

    /// <summary>
    /// Thanh dưới cùng chia đôi: bên trái là câu báo việc vừa làm (bị viết đè liên tục),
    /// bên phải là mấy phím tắt, cố định không ai đè lên. Từ khi bỏ hai nút Hoàn tác /
    /// Làm lại thì Ctrl+Z và Ctrl+Y chỉ còn được nhắc ở đây, mà nhắc thì phải nhắc suốt —
    /// để chung một dòng thì thêm một khách hàng là câu nhắc bay mất.
    /// </summary>
    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.XamNhat;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(26, 0, 0, 0);

        _lblPhimTat.Dock = DockStyle.Right;
        // Tự co theo chữ: đặt cứng bề rộng thì máy để cỡ chữ Windows lớn là cụt mất phím tắt.
        _lblPhimTat.AutoSize = true;
        _lblPhimTat.Font = Theme.FontPhu;
        _lblPhimTat.ForeColor = Theme.XamNhat;
        _lblPhimTat.TextAlign = ContentAlignment.MiddleRight;
        _lblPhimTat.Padding = new Padding(0, 0, 26, 0);
        _lblPhimTat.Text = _kho.ChiXem
            ? "Bấm đúp dòng khách để xem đơn hàng · F5 nạp lại · F6 sổ công nợ"
            : "Bấm đúp dòng khách để mở đơn hàng · Ctrl+Z hoàn tác · Ctrl+Y làm lại · F5 nạp lại · F6 sổ công nợ";

        if (_kho.ChiXem)
        {
            _lblTrangThai.ForeColor = Theme.Do;
            _lblTrangThai.Text = $"CHỈ XEM — {_kho.LyDoChiXem} · Dữ liệu: {_kho.DuongDanFile}";
        }
        else
        {
            _lblTrangThai.Text = $"Dữ liệu: {_kho.DuongDanFile}";
        }

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Margin = new Padding(0) };
        nen.Controls.Add(_lblTrangThai);
        nen.Controls.Add(_lblPhimTat);
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

        CapNhatTongQuan(nam);
        CapNhatNhacNo();
    }

    /// <summary>
    /// Viết lại bốn ô số liệu của thẻ tổng quan theo đúng danh sách đang hiện — lọc theo
    /// năm hay theo từ khoá tìm thì số liệu cũng chạy theo, để số trên thẻ và bảng dưới
    /// luôn khớp nhau.
    /// </summary>
    private void CapNhatTongQuan(int nam)
    {
        var tongMua = _nguon.Sum(d => d.TongTien);
        var tongTra = _nguon.Sum(d => d.DaTra);
        var conNo = tongMua - tongTra;
        var soKhachNo = _nguon.Count(d => d.ConLai > 0);
        var soDon = _nguon.Sum(d => d.SoHoaDon);

        _lblTenThe.Text = $"Tổng quan năm {nam}";

        _oKhach.GiaTri = _nguon.Count.ToString("#,##0");
        _oKhach.ChuThich = _chkCoDon.Checked ? "Đang lọc: chỉ khách có đơn" : "Trong danh sách";

        _oTongMua.GiaTri = So.Tien(tongMua);
        _oTongMua.ChuThich = $"{soDon:#,##0} hoá đơn";

        _oDaThu.GiaTri = So.Tien(tongTra);
        _oDaThu.ChuThich = tongMua > 0
            ? $"{tongTra / tongMua:P0} số tiền đã mua"
            : "Chưa có hoá đơn nào";

        _oConNo.GiaTri = So.Tien(conNo);
        _oConNo.MauGiaTri = conNo > 0 ? Theme.Do : Theme.ChuDam;
        _oConNo.ChuThich = soKhachNo > 0 ? $"{soKhachNo} khách còn nợ" : "Không ai còn nợ";

        _lblTongKet.Text = $"{_nguon.Count} khách hàng trong năm {nam}";
    }

    /// <summary>Tính lại dải nhắc nợ trên đầu màn hình (tính tất cả các năm, không riêng năm đang xem).</summary>
    private void CapNhatNhacNo()
    {
        var soNgay = _kho.CaiDat.SoNgayNhacNo;
        var congNo = CongNo.Tinh(_kho.DuLieu, nam: null, DateTime.Today);
        var quaHan = CongNo.QuaHan(congNo, soNgay);

        if (quaHan.Count > 0)
        {
            var lauNhat = quaHan[0];
            _nenNhacNo.BackColor = Color.FromArgb(253, 242, 224);
            _lblNhacNo.ForeColor = Color.FromArgb(150, 87, 16);
            _lblNhacNo.Text =
                $"⚠  {quaHan.Count} khách nợ quá {soNgay} ngày — tổng {So.Tien(quaHan.Sum(d => d.ConNo))}." +
                $"   Lâu nhất: {lauNhat.Khach.Ten} ({lauNhat.SoNgayNo} ngày, {So.Tien(lauNhat.ConNo)}).";
        }
        else if (congNo.Count > 0)
        {
            _nenNhacNo.BackColor = Theme.ChinhNhat;
            _lblNhacNo.ForeColor = Theme.Chinh;
            _lblNhacNo.Text =
                $"{congNo.Count} khách đang nợ, tổng {So.Tien(congNo.Sum(d => d.ConNo))} — chưa có ai quá {soNgay} ngày.";
        }
        else
        {
            _nenNhacNo.BackColor = Color.FromArgb(230, 246, 238);
            _lblNhacNo.ForeColor = Theme.Xanh;
            _lblNhacNo.Text = "Không có khách nào đang nợ.";
        }

        // Nhãn tô cùng màu dải, và vẽ lại cả nút bên trong: hình bo góc tô bằng BackColor
        // nên đổi màu là phải vẽ lại, không thì nút giữ nền màu cũ.
        _lblNhacNo.BackColor = _nenNhacNo.BackColor;
        _nenNhacNo.Invalidate(true);
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

    private void Kho_DuLieuThayDoi(object? sender, EventArgs e)
    {
        _daBaoFileBiSua = false;
        NapNam();
        NapDanhSach();
    }

    private void Kho_ThaoTacBiChan(object? sender, EventArgs e) => HopThoai.CanhBao(
        Form.ActiveForm ?? this,
        $"Đang mở ở chế độ CHỈ XEM nên không sửa được gì.\n\n{_kho.LyDoChiXem}.\n\n" +
        "Đóng phần mềm ở máy kia rồi mở lại là sửa được bình thường.");

    /// <summary>
    /// Máy khác vừa sửa file trong lúc mình đang mở: báo ngay ở thanh dưới để khỏi ngồi
    /// nhập tiếp trên số liệu cũ. Đang chỉ xem thì mời nạp lại luôn cho khỏi lạc hậu.
    /// </summary>
    private void NgoFileDuLieu()
    {
        if (_daBaoFileBiSua || !_kho.FileBiMayKhacSua())
        {
            return;
        }

        _daBaoFileBiSua = true;
        _lblTrangThai.ForeColor = Theme.Do;
        _lblTrangThai.Text =
            "⚠  File dữ liệu vừa bị máy khác sửa. Bấm F5 để nạp lại bản mới nhất trước khi nhập tiếp.";
    }

    private void NapLaiTuFile()
    {
        if (!_kho.FileBiMayKhacSua() && !_kho.ChiXem)
        {
            _lblTrangThai.Text = "File dữ liệu vẫn đúng bản đang mở, không cần nạp lại.";
            return;
        }

        _kho.NapLaiTuFile();
        _daBaoFileBiSua = false;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.Text = "Đã nạp lại dữ liệu mới nhất từ file.";
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

    private void ThuTienCuaKhach()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn khách hàng vừa đưa tiền.");
            return;
        }

        using var form = new ThuTienForm(khach.Id);
        form.ShowDialog(this);
        NapDanhSach();
        _lblTrangThai.Text = $"Đã cập nhật tiền của {khach.Ten}.";
    }

    private void ThemKhach()
    {
        using var form = new KhachHangForm(null);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } moi)
        {
            return;
        }

        // Dễ tạo trùng một người thành hai khách rồi chia đôi công nợ, nên hỏi lại trước.
        if (KiemTra.KhachTrungTen(_kho.DuLieu.KhachHangs, moi.Ten) is { } daCo
            && !HopThoai.Hoi(
                this,
                $"Đã có khách \"{daCo.Ten}\"" +
                (string.IsNullOrWhiteSpace(daCo.DienThoai) ? string.Empty : $" (ĐT {daCo.DienThoai})") +
                (string.IsNullOrWhiteSpace(daCo.DiaChi) ? string.Empty : $" — {daCo.DiaChi}") +
                ".\n\nVẫn thêm một khách nữa cùng tên?"))
        {
            ChonLaiKhach(daCo.Id);
            _lblTrangThai.Text = $"Đã có sẵn khách {daCo.Ten}, không thêm mới.";
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

    private void MoSoCongNo()
    {
        using var form = new CongNoForm(NamDangChon);
        form.ShowDialog(this);
        NapDanhSach();
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
            case Keys.F5:
                NapLaiTuFile();
                return true;
            case Keys.F6:
                MoSoCongNo();
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
