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
    private readonly Label _lblTong = new();
    private readonly Label _lblDaTra = new();
    private readonly Label _lblConLai = new();
    private readonly Label _lblTrangThai = new();

    private readonly int _namBanDau;
    private readonly List<VatTu> _danhMucHang = new();

    /// <summary>Thanh nhập nhanh trên bảng — tắt cả thanh khi đang xem tờ hoàn hàng.</summary>
    private Control _thanhThemNhanh = null!;

    private Guid? _hoaDonId;
    private bool _dangNap;
    private bool _sanSang;
    private string? _anhChupTruocKhiSua;

    /// <summary>
    /// Dòng trống ở cuối lưới để gõ thẳng hàng mới như trong Excel. Nó chưa nằm trong hoá đơn:
    /// gõ xong tên hàng và số lượng, bấm Enter (hoặc rời sang dòng khác) mới ghi vào sổ.
    /// </summary>
    private ChiTietHoaDon? _dongNhap;

    /// <summary>Chặn ghi chồng khi việc ghi dòng nhập đang chờ chạy hoặc đang chạy dở.</summary>
    private bool _dangGhiDongNhap;

    /// <summary>
    /// Thanh dưới đang hiện lời nhắc "đang chọn N dòng". Nhớ lại để lúc bỏ chọn thì trả về lời
    /// nhắc thường của màn hình, chứ không xoá mất thông báo của việc vừa làm.
    /// </summary>
    private bool _dangNhacNhom;

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

    /// <summary>Gõ dở một dòng ở cuối bảng rồi đóng cửa sổ thì nhắc lại, khỏi mất công gõ lại.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!e.Cancel
            && DongNhapCoChu()
            && !HopThoai.Hoi(this, "Dòng cuối bảng đang gõ dở, chưa ghi vào sổ.\n\nVẫn đóng cửa sổ?"))
        {
            e.Cancel = true;
            DatConTroDongNhap(_dongNhap is { TenHang.Length: 0 } ? OCanSua.TenHang : OCanSua.SoLuong);
        }

        base.OnFormClosing(e);
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
            RowCount = 3,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        goc.Controls.Add(TaoTieuDe(), 0, 0);
        goc.Controls.Add(TaoThanNoiDung(), 0, 1);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 2);

        Controls.Add(goc);
    }

    /// <summary>
    /// Dải tiêu đề: tên khách bên trái, còn bên phải là đúng những thứ phải với tay tới suốt
    /// ngày — đang xem hoá đơn nào, thêm hoá đơn, in. Trước đây ba thứ này nằm ở hai thanh
    /// ngang riêng bên dưới, cộng lại chiếm 138px ngay trên bảng hàng.
    /// </summary>
    private Control TaoTieuDe()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Chinh };

        // Tên và địa chỉ cắt bằng "…" chứ không chạy dài: bên phải là hàng chọn hoá đơn, tên
        // khách dài mà cứ chạy tiếp là hai thứ chồng lên nhau.
        _lblTenKhach.Font = Theme.FontTieuDe;
        _lblTenKhach.ForeColor = Color.White;
        _lblTenKhach.AutoSize = false;
        _lblTenKhach.AutoEllipsis = true;
        _lblTenKhach.Location = new Point(24, 14);
        _lblTenKhach.Height = 34;
        _lblTenKhach.TextAlign = ContentAlignment.MiddleLeft;

        _lblLienHe.Font = Theme.FontPhu;
        _lblLienHe.ForeColor = Color.FromArgb(205, 224, 247);
        _lblLienHe.AutoSize = false;
        _lblLienHe.AutoEllipsis = true;
        _lblLienHe.Location = new Point(26, 52);
        _lblLienHe.Height = 24;
        _lblLienHe.TextAlign = ContentAlignment.MiddleLeft;

        _cboNam.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNam.Font = Theme.FontNhap;
        _cboNam.Width = 96;
        _cboNam.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapHoaDon(null);
            }
        };

        _cboHoaDon.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboHoaDon.Font = Theme.FontNhap;
        _cboHoaDon.Width = 300;
        _cboHoaDon.SelectedIndexChanged += (_, _) =>
        {
            if (_dangNap || !_sanSang)
            {
                return;
            }

            _hoaDonId = (_cboHoaDon.SelectedItem as DongHoaDon)?.HD.Id;
            NapChiTiet();
        };

        // Trên dải xanh thì nút trắng chữ xanh mới là nút nổi nhất — tô xanh đặc lên nền xanh
        // là chìm mất.
        var btnMoi = Theme.NutPhu("+  Hoá đơn mới", 176, 44);
        btnMoi.ForeColor = Theme.Chinh;
        btnMoi.Click += (_, _) => TaoHoaDon();

        var btnIn = Theme.Nut("IN / XEM TRƯỚC", Theme.Cam, 186, 44);
        btnIn.Click += (_, _) => XemTruocVaIn();

        var btnDong = Theme.NutPhu("Đóng", 110, 44);
        btnDong.Click += (_, _) => Close();

        // Một menu duy nhất cho mọi việc còn lại của khách và của hoá đơn — chia nhóm bằng vạch
        // ngăn. Cái nào cả tháng mới bấm một lần thì không đáng một nút riêng ngoài màn hình.
        var viecKhac = Theme.NutBaCham("Việc khác với khách và hoá đơn này", 44)
            .Viec("Thu tiền của khách", MoThuTien, Theme.Xanh)
            .Viec("Trả cho hoá đơn này", MoThanhToan, bat: () => HoaDonHienTai is { LaHoanHang: false })
            .Ngan()
            .Viec(
                "Hoàn hàng cho hoá đơn này",
                MoHoanHang,
                Theme.Cam,
                () => HoaDonHienTai is { LaHoanHang: false, ChiTiet.Count: > 0 })
            .Ngan()
            .Viec(
                () => HoaDonHienTai is { DaChot: true } ? "Mở lại hoá đơn" : "Chốt hoá đơn",
                DoiTrangThaiChot,
                bat: () => HoaDonHienTai is not null)
            .Viec("Sửa mã / ngày hoá đơn", SuaHoaDon, bat: () => HoaDonHienTai is not null)
            .Viec("Xoá hoá đơn này", XoaHoaDon, Theme.Do, () => HoaDonHienTai is not null)
            .Ngan()
            .Viec("Bảng giá riêng của khách", MoBangGia)
            .Viec("Soạn tin nhắc nợ", SoanTinNhacNo)
            .Ngan()
            .Viec("Hoàn tác        Ctrl+Z", HoanTac, bat: () => _kho.CoTheHoanTac)
            .Viec("Làm lại          Ctrl+Y", LamLai, bat: () => _kho.CoTheLamLai)
            .Ngan()
            .Viec("Xuất hoá đơn ra Excel", XuatExcel)
            .Viec("Nhập hàng từ file Excel", NhapTuExcel);

        // Nền của hàng nút phải đúng màu dải tiêu đề: nút bo góc tự xoá nền bằng màu khung cha,
        // sai màu là lộ ra bốn góc vuông.
        var hangNut = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = Theme.Chinh,
            Padding = new Padding(0, 22, 20, 0),
        };

        // Ô chọn khoá chiều cao theo cỡ chữ, thấp hơn nút — chừa lề trên cho nó nằm giữa hàng.
        var leCbo = Math.Max(0, (44 - _cboNam.PreferredHeight) / 2);
        _cboNam.Margin = new Padding(0, leCbo, 14, 0);
        _cboHoaDon.Margin = new Padding(0, leCbo, 14, 0);
        btnMoi.Margin = new Padding(0, 0, 12, 0);
        btnIn.Margin = new Padding(0, 0, 12, 0);
        viecKhac.Nut.Margin = new Padding(0, 0, 12, 0);
        btnDong.Margin = new Padding(0);

        hangNut.Controls.Add(btnDong);
        hangNut.Controls.Add(viecKhac.Nut);
        hangNut.Controls.Add(btnIn);
        hangNut.Controls.Add(btnMoi);
        hangNut.Controls.Add(_cboHoaDon);
        hangNut.Controls.Add(_cboNam);

        var mach = new ToolTip { InitialDelay = 250 };
        mach.SetToolTip(_cboNam, "Năm đang xem");
        mach.SetToolTip(_cboHoaDon, "Hoá đơn đang xem");

        void XepChu()
        {
            var rong = Math.Max(140, nen.Width - Math.Max(hangNut.Width, hangNut.PreferredSize.Width) - 44);
            _lblTenKhach.Width = rong;
            _lblLienHe.Width = rong;
        }

        nen.Resize += (_, _) => XepChu();
        nen.Controls.Add(_lblTenKhach);
        nen.Controls.Add(_lblLienHe);
        nen.Controls.Add(hangNut);
        XepChu();
        return nen;
    }

    private Control TaoThanNoiDung()
    {
        var than = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 8, 20, 10) };
        than.Controls.Add(TaoCotChiTiet());
        return than;
    }

    private Control TaoCotChiTiet()
    {
        var cot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
        };
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        cot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cot.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        _thanhThemNhanh = TaoThanhThemNhanh();
        cot.Controls.Add(_thanhThemNhanh, 0, 0);
        cot.Controls.Add(Theme.Khung(TaoLuoiChiTiet()), 0, 1);
        cot.Controls.Add(TaoThanhTongTien(), 0, 2);
        return cot;
    }

    private Control TaoThanhThemNhanh()
    {
        var nen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ChinhNhat,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0),
        };

        _dtNgay.Format = DateTimePickerFormat.Custom;
        _dtNgay.CustomFormat = Theme.DangNgay;

        // Ô ngày là chỗ bấm nhiều nhất trong hàng nhập nên cho to hẳn: chữ 14pt, ô cao 40px,
        // và lịch bung ra cũng đặt cỡ chữ riêng cho ô ngày dễ bấm hơn.
        _dtNgay.Font = Theme.FontNhapTo;
        _dtNgay.CalendarFont = Theme.FontNhapTo;

        // Đổi ngày ở đây thì dòng trống cuối bảng ăn theo luôn, miễn là chưa gõ gì vào nó.
        _dtNgay.ValueChanged += (_, _) =>
        {
            if (_dangNap || _dongNhap is not { } nhap || DongNhapCoChu())
            {
                return;
            }

            nhap.Ngay = _dtNgay.Value.Date;
            LamMoiDongNhap();
        };

        _cboHang.DropDownStyle = ComboBoxStyle.DropDown;
        _cboHang.Font = Theme.FontNhap;

        // Không gợi ý gì trong lúc gõ. Danh sách vẫn nằm sẵn trong ô, muốn chọn thì bấm mũi tên
        // mở ra — nhưng gõ tới đâu bung tới đó rồi hỏi "ý anh là ... phải không" thì đang nhập
        // liền tay bị cắt nhịp. Gõ tắt để riêng cho màn "Nhập nhiều dòng".
        _cboHang.AutoCompleteMode = AutoCompleteMode.None;

        // Rời ô mới điền đơn vị và đơn giá, và chỉ khi tên **khớp hẳn** một mặt hàng trong danh
        // mục. Không đoán, không hỏi.
        _cboHang.Leave += (_, _) => DienTheoDanhMuc();
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

        var btnThem = Theme.Nut("+  THÊM DÒNG", Theme.Xanh, 180, 40);
        btnThem.Click += (_, _) => ThemDong();

        var btnTraLai = Theme.Nut("−  TRẢ LẠI", Theme.Do, 150, 40);
        btnTraLai.Click += (_, _) => ThemDong(traLai: true);

        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Margin = new Padding(0),
        };
        // Nhãn để đúng một hai chữ cho khỏi cắt, còn cách gõ thì để trong chú thích hiện ra khi
        // trỏ chuột vào ô — nhãn dài như "ĐƠN GIÁ (tính được: 3+2*4)" không bao giờ vừa ô.
        var mach = new ToolTip { InitialDelay = 250, AutoPopDelay = 10000 };
        mach.SetToolTip(_cboHang, "Gõ tắt cũng ra: \"o27\", \"27 ong\"");
        mach.SetToolTip(_txtDonGia, "Gõ được cả phép tính, ví dụ: 3+2*4");
        mach.SetToolTip(_txtSoLuong, "Số âm là trả lại hàng, ví dụ: -2");

        // Cả hàng phải vừa màn hình hẹp nhất (laptop 1366 mở toàn màn) — không vừa là hàng nút
        // bị đẩy ra ngoài, mọc thanh cuộn ngang rồi cắt mất nút.
        const int CaoO = 40;
        const int Le = 12;
        hang.Controls.Add(Theme.Truong("NGÀY LẤY", _dtNgay, 180, CaoO, Le));
        hang.Controls.Add(Theme.Truong("TÊN HÀNG", _cboHang, 240, CaoO, Le));
        hang.Controls.Add(Theme.Truong("ĐƠN VỊ", _txtDonVi, 90, CaoO, Le));
        hang.Controls.Add(Theme.Truong("ĐƠN GIÁ", _txtDonGia, 120, CaoO, Le));
        hang.Controls.Add(Theme.Truong("SỐ LƯỢNG", _txtSoLuong, 110, CaoO, Le));

        // "THÀNH TIỀN" là nhãn dài nhất hàng: chừa đủ 130px cho nó, khỏi phải hạ cỡ chữ.
        hang.Controls.Add(Theme.Truong("THÀNH TIỀN", _lblTamTinh, 130, CaoO, Le));
        hang.Controls.Add(Theme.Truong(" ", btnThem, 180, CaoO, Le));
        hang.Controls.Add(Theme.Truong(" ", btnTraLai, 150, CaoO, Le));

        // Gõ xong tên hàng, Enter là sang thẳng ô số lượng — đơn vị với đơn giá phần mềm tự
        // điền theo danh mục, gõ tay chỉ khi cần sửa. Enter ở số lượng là ghi dòng.
        GanPhimEnter(_cboHang, _txtSoLuong);
        GanPhimEnter(_txtDonVi);
        GanPhimEnter(_txtDonGia);
        GanPhimEnter(_txtSoLuong);

        nen.Controls.Add(hang);
        return nen;
    }

    /// <summary>
    /// Tên vừa gõ khớp hẳn một mặt hàng trong danh mục thì điền hộ đơn vị và đơn giá của khách.
    /// Chỉ điền vào ô đang trống — người dùng đã gõ giá riêng thì đừng ghi đè.
    /// </summary>
    private void DienTheoDanhMuc()
    {
        if (_dangNap || Khach is not { } khach)
        {
            return;
        }

        var ten = _cboHang.Text.Trim();
        if (ten.Length == 0 || _kho.TimVatTuTheoTen(ten) is not { } vatTu)
        {
            return;
        }

        if (_txtDonVi.Text.Trim().Length == 0)
        {
            _txtDonVi.Text = vatTu.DonVi;
        }

        if (So.Tinh(_txtDonGia.Text) <= 0m)
        {
            _txtDonGia.Text = So.Tien(_kho.GiaCho(khach, vatTu));
        }
    }

    /// <summary>
    /// Ô số nhận cả phép tính ("3+2*4"). Gõ chữ vào đó thì **xoá trắng luôn** rồi nhắc một câu:
    /// để nguyên chữ vô nghĩa trong ô thì người ta gõ tiếp vào giữa nó, ra một chuỗi sai nữa.
    /// </summary>
    private bool OSoHopLe(TextBox o, string tenO)
    {
        var chu = o.Text.Trim();
        if (chu.Length == 0 || So.TryTinh(chu, out _))
        {
            return true;
        }

        _lblTrangThai.Text = $"Ô {tenO} phải là số — đã xoá \"{chu}\". Gõ số, hoặc phép tính như 3+2*4.";
        o.Clear();
        o.Focus();
        return false;
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

        // Bấm một lần vào ô là sửa được luôn, không phải bấm lần nữa hay nhấn F2 — nhập cả hoá
        // đơn dài thì mỗi ô tiết kiệm một cú bấm là đỡ hẳn tay. Vẫn giữ EditMode là
        // EditOnKeystrokeOrF2 chứ không đổi sang EditOnEnter: EditOnEnter mở ô sửa cả khi chỉ đi
        // bằng mũi tên, lúc đó lưới lúc nào cũng coi như đang gõ dở nên mất hết phím tắt Ctrl+Z,
        // Delete, Ctrl+A, Alt+↑/↓ và Esc.
        _luoiCT.CellMouseClick += (_, e) =>
        {
            // Ctrl/Shift+bấm là đang gom nhóm dòng để xoá hay chuyển, mở ô sửa lúc đó là phá mất
            // nhóm vừa chọn. Chuột phải để dành cho menu. Ô đang sửa rồi thì cũng bỏ qua, không
            // thì bấm vào giữa chữ để đặt con trỏ lại bị chọn hết cả ô.
            if (e.Button != MouseButtons.Left
                || e.RowIndex < 0
                || e.ColumnIndex < 0
                || (ModifierKeys & (Keys.Control | Keys.Shift)) != 0
                || _luoiCT.IsCurrentCellInEditMode)
            {
                return;
            }

            _luoiCT.BeginEdit(selectAll: true);
        };

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

            var dongCuaO = _luoiCT.Rows[e.RowIndex].DataBoundItem as ChiTietHoaDon;

            // Dòng khách trả lại hàng: số âm, tô đỏ cho khỏi đọc nhầm thành hàng đã lấy.
            if (dongCuaO is { LaTraLai: true }
                && cot is nameof(ChiTietHoaDon.SoLuong) or nameof(ChiTietHoaDon.ThanhTien))
            {
                kieu.ForeColor = Theme.Do;
            }

            if (!LaDongNhap(dongCuaO))
            {
                return;
            }

            // Dòng đang gõ dở: tô vàng nhạt cho khác hẳn hàng đã ghi vào sổ, và giấu mấy số 0
            // chưa nhập đi để nhìn vào là biết ô nào còn trống.
            kieu.BackColor = Color.FromArgb(255, 251, 230);
            kieu.SelectionBackColor = Color.FromArgb(250, 236, 190);
            kieu.SelectionForeColor = Theme.Chu;

            // Lời nhắc chỉ hiện khi con trỏ chưa đứng ở ô đó — đứng vào là ô trống trơn để gõ,
            // không dính chữ nhắc vào nội dung.
            var oDangChon = _luoiCT.CurrentCell is { } oHienTai
                && oHienTai.RowIndex == e.RowIndex
                && oHienTai.ColumnIndex == e.ColumnIndex;

            if (cot == nameof(ChiTietHoaDon.TenHang) && !oDangChon && e.Value is string { Length: 0 })
            {
                e.Value = "Gõ tên hàng ở đây rồi Enter…";
                kieu.ForeColor = Theme.Xam;
                e.FormattingApplied = true;
            }
            else if (cot is nameof(ChiTietHoaDon.DonGia) or nameof(ChiTietHoaDon.SoLuong)
                         or nameof(ChiTietHoaDon.ThanhTien)
                     && e.Value is decimal and 0m)
            {
                e.Value = string.Empty;
                e.FormattingApplied = true;
            }
        };

        // Lời nhắc ở dòng gõ dở ẩn hiện theo chỗ con trỏ đang đứng nên phải vẽ lại dòng đó.
        _luoiCT.CurrentCellChanged += (_, _) =>
        {
            if (_dongNhap is not null && _luoiCT.Rows.Count > 0)
            {
                _luoiCT.InvalidateRow(_luoiCT.Rows.Count - 1);
            }
        };

        // Rời dòng đang gõ dở sang dòng khác thì ghi luôn vào sổ, giống bảng tính.
        _luoiCT.RowValidated += (_, e) =>
        {
            if (_dangNap || e.RowIndex < 0 || e.RowIndex >= _luoiCT.Rows.Count)
            {
                return;
            }

            if (LaDongNhap(_luoiCT.Rows[e.RowIndex].DataBoundItem as ChiTietHoaDon))
            {
                HenGhiDongNhap(doPhimEnter: false);
            }
        };

        // Cho chọn nhiều dòng (Ctrl+bấm từng dòng, Shift+bấm cả dải) rồi xoá hoặc chuyển cả
        // nhóm một lượt — hoá đơn dài mà xoá từng dòng một thì mỏi tay.
        _luoiCT.MultiSelect = true;
        _luoiCT.ContextMenuStrip = TaoMenuChuot();

        // Bấm chuột phải lên dòng nào thì chọn luôn dòng đó, để lệnh trong menu áp vào đúng dòng
        // người dùng đang trỏ tới chứ không phải dòng đang chọn từ trước.
        _luoiCT.CellMouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            // Trừ khi dòng đó đang nằm trong nhóm đã chọn: đặt lại con trỏ là Windows bỏ hết dấu
            // chọn của các dòng khác, chọn 5 dòng rồi bấm chuột phải vào giữa nhóm thì lệnh
            // "xoá dòng đã chọn" chỉ còn xoá đúng một dòng.
            if (_luoiCT.Rows[e.RowIndex].Selected)
            {
                return;
            }

            _luoiCT.CurrentCell = _luoiCT.Rows[e.RowIndex].Cells[e.ColumnIndex];
        };

        // Chọn nhiều dòng thì nhắc ngay ở thanh dưới: đang chọn mấy dòng, thành bao nhiêu tiền,
        // bấm gì để xoá / chuyển cả nhóm. Không có lời nhắc này thì chọn xong cũng không biết
        // mình đang giữ đúng mấy dòng.
        _luoiCT.SelectionChanged += (_, _) => NhacNhomDangChon();

        _luoiCT.DataSource = _nguonCT;
        return _luoiCT;
    }

    /// <summary>Một việc làm được với dòng (hoặc cả nhóm dòng) đang chọn trên lưới.</summary>
    /// <param name="Chu">Chữ hiện trong menu — tính lại lúc mở menu để kèm được số dòng đang chọn.</param>
    private sealed record ViecDong(Func<string> Chu, Action Lam, Color Mau = default);

    /// <summary>Một dòng menu: tên việc bên trái, phím tắt lùi về phía phải.</summary>
    private static string MucMenu(string ten, string phimTat) => ten.PadRight(30) + phimTat;

    /// <summary>
    /// Danh sách việc làm với dòng đang chọn, dùng chung cho menu chuột phải trên lưới và nút ⋯
    /// dưới bảng — trước đây hai chỗ chép tay hai lần, lệch nhau một việc là người dùng tưởng
    /// phần mềm làm được ở chỗ này mà không làm được ở chỗ kia. Phần tử null là vạch ngăn.
    /// <para>
    /// Chữ của việc xoá và chuyển kèm luôn số dòng đang chọn ("Xoá 5 dòng đã chọn"), để trước
    /// khi bấm là biết lệnh sắp ăn vào mấy dòng — chọn nhầm cả dải thì thấy ngay ở đây.
    /// </para>
    /// </summary>
    private IReadOnlyList<ViecDong?> ViecVoiDongDangChon() => new ViecDong?[]
    {
        new(() => MucMenu("Chèn dòng lên trên", "Ctrl+Enter"), () => ThemDong(chen: true)),
        new(() => MucMenu("Chèn dòng xuống dưới", "Ctrl+Shift+Enter"), () => ThemDong(chen: true, chenDuoi: true)),
        null,
        new(() => MucMenu("Chọn tất cả dòng", "Ctrl+A"), ChonTatCaDong),
        new(() => MucMenu($"Chuyển {NhomDangChon()} lên", "Alt+↑"), () => ChuyenDong(xuong: false)),
        new(() => MucMenu($"Chuyển {NhomDangChon()} xuống", "Alt+↓"), () => ChuyenDong(xuong: true)),
        null,
        new(() => MucMenu($"Xoá {NhomDangChon()} đã chọn", "Delete"), XoaDong, Theme.Do),
    };

    /// <summary>"5 dòng" khi đang giữ cả nhóm, còn một dòng thì chỉ là "dòng" — để ghép vào menu.</summary>
    private string NhomDangChon()
    {
        var so = DongDaChon().Count;
        return so >= 2 ? $"{so} dòng" : "dòng";
    }

    /// <summary>Menu chuột phải trên lưới chi tiết: chèn, chọn cả bảng, đổi chỗ, xoá dòng.</summary>
    private ContextMenuStrip TaoMenuChuot()
    {
        // Cùng cỡ chữ với menu của nút ba chấm ở thanh tổng tiền — hai chỗ này cùng một danh
        // sách việc, chữ lệch cỡ nhau là nhìn ra ngay.
        var menu = new ContextMenuStrip { Font = Theme.FontNhap, ShowImageMargin = false };
        var capNhatChu = new List<Action>();

        foreach (var viec in ViecVoiDongDangChon())
        {
            if (viec is null)
            {
                menu.Items.Add(new ToolStripSeparator());
                continue;
            }

            var muc = new ToolStripMenuItem(viec.Chu(), null, (_, _) => viec.Lam());
            if (viec.Mau != default)
            {
                muc.ForeColor = viec.Mau;
            }

            menu.Items.Add(muc);

            // Chữ tính lại mỗi lần mở menu, vì số dòng đang chọn đổi liên tục.
            capNhatChu.Add(() => muc.Text = viec.Chu());
        }

        menu.Opening += (_, _) =>
        {
            foreach (var capNhat in capNhatChu)
            {
                capNhat();
            }
        };

        return menu;
    }

    private Control TaoThanhTongTien()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(0, 8, 0, 0) };

        // Nhập nhiều dòng để ngoài chứ không nằm trong menu: gõ cả đơn hàng bằng một dòng
        // "ống 27 x10, co 90 x5" là cách nhập nhanh nhất, mà nằm trong menu thì không ai thấy.
        // Đặt ở đây, ngay dưới bảng, vì hàng ô nhập phía trên đã đủ rộng cho màn 1366.
        // `noTheoChu`: chữ này dài mười lăm ký tự, mà 210px chỉ vừa khít ở cỡ hiển thị 100%
        // — máy đặt 125% là chữ tràn ra ngoài rồi bị cắt. Cho nút nở theo chữ thay vì hạ cỡ
        // chữ xuống: người đặt chữ to là người cần chữ to.
        var btnNhieuDong = Theme.Nut("NHẬP NHIỀU DÒNG", Theme.Chinh, 210, 44, noTheoChu: true);
        btnNhieuDong.Margin = new Padding(0, 0, 10, 0);
        btnNhieuDong.Click += (_, _) => NhapNhieuDong();

        // Đúng những việc của menu chuột phải trên lưới, để ai không quen chuột phải vẫn tìm
        // được. Trước đây là hai nút chữ dài chiếm hết góc trái dưới bảng.
        var viecDong = Theme.NutBaCham("Việc với dòng đang chọn", 44);
        foreach (var viec in ViecVoiDongDangChon())
        {
            if (viec is null)
            {
                viecDong.Ngan();
                continue;
            }

            viecDong.Viec(viec.Chu, viec.Lam, viec.Mau);
        }

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnNhieuDong);
        trai.Controls.Add(viecDong.Nut);

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
        _nguonCT = new BindingList<ChiTietHoaDon>(new List<ChiTietHoaDon>(dong));

        // Tờ hoàn hàng không sửa từng dòng trên lưới: số hoàn phải khớp với hoá đơn gốc, sửa
        // tay ở đây là hoàn quá số khách đã lấy mà không ai chặn. Muốn khác thì xoá tờ, lập lại.
        _luoiCT.ReadOnly = _kho.ChiXem || hoaDon is { DaChot: true } or { LaHoanHang: true };

        // Xem tờ hoàn hàng thì tắt luôn cả thanh nhập nhanh: để nó sáng đèn rồi chặn lúc bấm
        // thì người dùng gõ xong cả dòng mới biết là không ghi được vào đây.
        // Chế độ chỉ xem thì vẫn để thanh sáng đèn: bấm vào có hộp thoại nói rõ vì sao không
        // ghi được, chứ ô xám ngoét thì người dùng chẳng biết hỏi ai.
        _thanhThemNhanh.Enabled = hoaDon is not { LaHoanHang: true };

        // Dòng trống cuối lưới để gõ thẳng hàng mới. Ngày lấy theo dòng cuối cùng đang có, có vậy
        // gõ liền mấy dòng cùng ngày mới khỏi phải sửa lại ngày từng dòng. Đang gõ dở mà bảng nạp
        // lại (hoàn tác, sửa ngày, đổi hoá đơn…) thì giữ nguyên dòng đó, khỏi mất công gõ lại.
        _dongNhap = _luoiCT.ReadOnly
            ? null
            : DongNhapCoChu()
                ? _dongNhap
                : new ChiTietHoaDon { Ngay = dong.Count > 0 ? dong[^1].Ngay : _dtNgay.Value.Date };

        if (_dongNhap is { } nhap)
        {
            _nguonCT.Add(nhap);
        }

        _luoiCT.DataSource = _nguonCT;

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

        _dangNhacNhom = false;
        _lblTrangThai.Text = NhanCoBan();

        CapNhatTong();
    }

    private void CapNhatTong()
    {
        var hoaDon = HoaDonHienTai;
        var tong = hoaDon?.TongTien ?? 0m;
        var daTra = hoaDon?.DaThanhToan ?? 0m;

        // Tờ hoàn hàng: trong sổ là số âm để trừ vào nợ, nhưng bày ra cho người đọc thì nói
        // thẳng "hoàn lại bao nhiêu, trừ vào nợ" chứ không bắt người ta tự hiểu dấu trừ.
        if (hoaDon is { LaHoanHang: true })
        {
            _lblTong.Text = $"Tiền hoàn lại: {So.Tien(hoaDon.TienHoan)}";
            _lblDaTra.Text = $"{hoaDon.ChiTiet.Count} món hoàn";
            _lblConLai.Text = "Trừ vào nợ của khách";
            return;
        }

        _lblTong.Text = $"Tổng cộng: {So.Tien(tong)}";
        _lblDaTra.Text = $"Đã trả: {So.Tien(daTra)}";
        _lblConLai.Text = $"Còn lại: {So.Tien(tong - daTra)}";
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
        if (Khach is null || HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        var ten = _cboHang.Text.Trim();
        if (ten.Length == 0)
        {
            // Nhắc một câu rồi đưa con trỏ về ô còn thiếu. Nhập cả chục dòng liền tay mà cứ
            // thiếu một ô là bật hộp thoại chặn giữa thì mất nhịp, phải với chuột đi tắt nó.
            _lblTrangThai.Text = "Chưa gõ tên hàng nên chưa ghi được dòng nào.";
            _cboHang.Focus();
            return;
        }

        if (!OSoHopLe(_txtDonGia, "ĐƠN GIÁ") || !OSoHopLe(_txtSoLuong, "SỐ LƯỢNG"))
        {
            return;
        }

        var soLuong = So.Tinh(_txtSoLuong.Text);
        if (traLai)
        {
            soLuong = -Math.Abs(soLuong);
        }

        if (soLuong == 0)
        {
            _lblTrangThai.Text = $"Dòng \"{ten}\" chưa có số lượng nên chưa ghi vào sổ. "
                + "Gõ được cả phép tính (3+2*4); khách trả lại hàng thì gõ số âm hoặc bấm TRẢ LẠI.";
            _txtSoLuong.Focus();
            _txtSoLuong.SelectAll();
            return;
        }

        // Chèn thì dòng mới lấy ngày của dòng đang chọn, nếu không nó sẽ bị xếp sang chỗ khác.
        var moc = chen ? _luoiCT.CurrentRow?.DataBoundItem as ChiTietHoaDon : null;
        if (LaDongNhap(moc))
        {
            moc = null;
        }

        var dongMoi = GhiDongHang(
            moc?.Ngay.Date ?? _dtNgay.Value.Date,
            ten,
            _txtDonVi.Text.Trim(),
            So.Tinh(_txtDonGia.Text),
            soLuong,
            ghiChu: string.Empty,
            _cboHang.SelectedItem as VatTu,
            moc,
            chenDuoi,
            out var canSua);

        if (dongMoi is null)
        {
            switch (canSua)
            {
                case OCanSua.DonGia:
                    _txtDonGia.Focus();
                    _txtDonGia.SelectAll();
                    break;
                case OCanSua.SoLuong:
                    _txtSoLuong.Focus();
                    _txtSoLuong.SelectAll();
                    break;
                case OCanSua.TenHang:
                    _cboHang.Focus();
                    break;
            }

            return;
        }

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

    /// <summary>Ô cần quay lại sửa khi người dùng bấm "Không" ở một câu hỏi kiểm tra.</summary>
    private enum OCanSua
    {
        KhongCo,
        TenHang,
        DonGia,
        SoLuong,
    }

    /// <summary>
    /// Kiểm tra rồi ghi một dòng hàng vào hoá đơn đang mở (chưa có thì tự tạo hoá đơn mới), gói
    /// gọn trong đúng một bước hoàn tác. Dùng chung cho thanh nhập nhanh và dòng gõ thẳng ở cuối
    /// lưới. Trả về null khi người dùng bấm "Không" ở một câu hỏi — <paramref name="canSua"/>
    /// cho biết nên đưa con trỏ về ô nào để sửa lại.
    /// </summary>
    private ChiTietHoaDon? GhiDongHang(
        DateTime ngay,
        string ten,
        string donVi,
        decimal donGia,
        decimal soLuong,
        string ghiChu,
        VatTu? vatTuChon,
        ChiTietHoaDon? moc,
        bool chenDuoi,
        out OCanSua canSua)
    {
        canSua = OCanSua.KhongCo;
        if (Khach is not { } khach)
        {
            return null;
        }

        var hoaDonDangChon = HoaDonHienTai;
        if (hoaDonDangChon is { DaChot: true })
        {
            HopThoai.CanhBao(this, "Hoá đơn này đã chốt. Hãy bấm \"Mở lại hoá đơn\" trước khi thêm hàng.");
            return null;
        }

        if (hoaDonDangChon is { LaHoanHang: true })
        {
            HopThoai.CanhBao(this, ChanSuaToHoan);
            return null;
        }

        var taoHoaDonMoi = hoaDonDangChon is null;
        HoaDon hoaDon = hoaDonDangChon ?? new HoaDon
        {
            KhachHangId = _khachId,
            Nam = NamDangChon,
            MaHoaDon = _kho.TaoMaHoaDon(_khachId, NamDangChon),
            NgayMo = ngay,
        };

        var vatTu = vatTuChon;
        if (vatTu is null || !string.Equals(vatTu.Ten, ten, StringComparison.CurrentCultureIgnoreCase))
        {
            vatTu = _kho.TimVatTuTheoTen(ten);
        }

        if (_kho.CaiDat.CanhBaoDongTrung
            && KiemTra.DongTrung(hoaDonDangChon, ngay, ten, soLuong) is { } dongTrung
            && !HopThoai.Hoi(
                this,
                $"Hoá đơn đã có sẵn dòng y hệt:\n\n" +
                $"{dongTrung.TenHang} × {So.Luong(dongTrung.SoLuong)} ngày {dongTrung.Ngay:dd/MM/yyyy}\n\n" +
                "Vẫn thêm thêm một dòng nữa?"))
        {
            return null;
        }

        if (KiemTra.LechGia(_kho.HoaDonCuaKhach(_khachId), ten, vatTu?.Id, donGia, _kho.CaiDat.NguongLechGia)
                is { } lech
            && !HopThoai.Hoi(
                this,
                $"Lần gần nhất bán \"{ten}\" cho {khach.Ten} (ngày {lech.Ngay:dd/MM/yyyy}) là {So.Tien(lech.GiaCu)}.\n" +
                $"Lần này nhập {So.Tien(donGia)} — lệch {PhanTramLech(lech.GiaCu, donGia)}%.\n\n" +
                "Giá này có đúng không?"))
        {
            canSua = OCanSua.DonGia;
            return null;
        }

        if (KiemTra.TraLaiQuaSoDaMua(_kho.HoaDonCuaKhach(_khachId), ten, vatTu?.Id, soLuong) is { } dangGiu
            && !HopThoai.Hoi(
                this,
                $"Sổ đang ghi khách giữ {So.Luong(dangGiu)} \"{ten}\", " +
                $"lần này trả lại {So.Luong(Math.Abs(soLuong))}.\n\n" +
                "Vẫn ghi trả lại chừng này?"))
        {
            canSua = OCanSua.SoLuong;
            return null;
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
            GhiChu = ghiChu,
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

        return dongMoi;
    }

    private void LuoiCT_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (e.RowIndex < 0)
        {
            return;
        }

        // Dòng đang gõ dở chưa nằm trong sổ nên chẳng có gì để hoàn tác — chỉ điền nốt giúp
        // những ô còn trống.
        if (LaDongNhap(_luoiCT.Rows[e.RowIndex].DataBoundItem as ChiTietHoaDon))
        {
            HoanThienDongNhap(_luoiCT.Columns[e.ColumnIndex].DataPropertyName);
            return;
        }

        if (anhChup is null)
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

    // ---------------- Dòng gõ thẳng ở cuối lưới ----------------

    /// <summary>Dòng trống ở cuối lưới, tức là hàng đang gõ dở chứ chưa ghi vào sổ.</summary>
    private bool LaDongNhap(ChiTietHoaDon? dong) => dong is not null && ReferenceEquals(dong, _dongNhap);

    /// <summary>Dòng đang gõ dở đã có chữ nghĩa gì chưa, hay vẫn còn trống trơn.</summary>
    private bool DongNhapCoChu() => _dongNhap is { } nhap
        && (nhap.TenHang.Trim().Length > 0
            || nhap.SoLuong != 0m
            || nhap.DonGia != 0m
            || nhap.DonVi.Trim().Length > 0
            || nhap.GhiChu.Trim().Length > 0);

    /// <summary>
    /// Gõ xong tên hàng ở dòng cuối thì tra danh mục ngay: gõ tắt ra tên đầy đủ, đồng thời điền
    /// sẵn đơn vị và giá của chính khách này để chỉ còn phải gõ số lượng.
    /// </summary>
    private void HoanThienDongNhap(string thuocTinh)
    {
        if (thuocTinh != nameof(ChiTietHoaDon.TenHang)
            || _dongNhap is not { } nhap
            || Khach is not { } khach)
        {
            return;
        }

        var ten = nhap.TenHang.Trim();
        if (ten.Length == 0)
        {
            return;
        }

        // Khớp hẳn tên trong danh mục thì điền hộ đơn vị với đơn giá, còn không thì để nguyên
        // đúng chữ người dùng gõ. Gõ tắt là việc của màn "Nhập nhiều dòng".
        var vatTu = _kho.TimVatTuTheoTen(ten);
        if (vatTu is null)
        {
            return;
        }

        nhap.TenHang = vatTu.Ten;
        nhap.VatTuId = vatTu.Id;
        if (nhap.DonVi.Trim().Length == 0)
        {
            nhap.DonVi = vatTu.DonVi;
        }

        if (nhap.DonGia <= 0m)
        {
            nhap.DonGia = _kho.GiaCho(khach, vatTu);
        }

        LamMoiDongNhap();
    }

    /// <summary>
    /// Hẹn ghi dòng đang gõ dở sau khi lưới xử lý xong phím / chuột. Ghi ngay giữa chừng sự kiện
    /// của lưới thì việc nạp lại bảng sẽ đá ngược vào chính sự kiện đang chạy.
    /// </summary>
    /// <param name="doPhimEnter">
    /// Người dùng chủ động bấm Enter — lúc đó thiếu ô nào thì đưa con trỏ về ô đó, còn khi chỉ
    /// vô tình rời dòng thì để con trỏ yên chỗ người dùng vừa bấm.
    /// </param>
    private void HenGhiDongNhap(bool doPhimEnter)
    {
        // Đang đóng cửa sổ mà còn hẹn việc thì lúc chạy chẳng còn gì để ghi vào.
        if (_dangGhiDongNhap || _dongNhap is null || !IsHandleCreated || IsDisposed || Disposing)
        {
            return;
        }

        _dangGhiDongNhap = true;
        BeginInvoke(new Action(() =>
        {
            try
            {
                if (!IsDisposed)
                {
                    GhiDongNhap(doPhimEnter);
                }
            }
            finally
            {
                _dangGhiDongNhap = false;
            }
        }));
    }

    /// <summary>Ghi dòng đang gõ dở ở cuối lưới thành một dòng hàng thật trong hoá đơn.</summary>
    private void GhiDongNhap(bool doPhimEnter)
    {
        if (_dongNhap is not { } nhap)
        {
            return;
        }

        if (!DongNhapCoChu())
        {
            if (doPhimEnter)
            {
                DatConTroDongNhap(OCanSua.TenHang);
            }

            return;
        }

        var ten = nhap.TenHang.Trim();
        if (ten.Length == 0)
        {
            _lblTrangThai.Text = "Dòng cuối chưa có tên hàng nên chưa ghi vào sổ.";
            if (doPhimEnter)
            {
                DatConTroDongNhap(OCanSua.TenHang);
            }

            return;
        }

        if (nhap.SoLuong == 0m)
        {
            _lblTrangThai.Text = $"Dòng \"{ten}\" chưa có số lượng nên chưa ghi vào sổ. "
                + "Gõ số lượng rồi Enter (số âm là khách trả lại).";
            if (doPhimEnter)
            {
                DatConTroDongNhap(OCanSua.SoLuong);
            }

            return;
        }

        if (HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        // Cất nội dung ra rồi xoá trắng dòng gõ dở trước khi ghi: ghi xong bảng nạp lại là có ngay
        // dòng trống mới để gõ tiếp, chứ không lặp lại y nguyên dòng vừa vào sổ.
        var banCat = new ChiTietHoaDon();
        ChepNoiDung(nhap, banCat);
        XoaTrangDongNhap(nhacLai: false);

        var dongMoi = GhiDongHang(
            banCat.Ngay.Date,
            ten,
            banCat.DonVi.Trim(),
            banCat.DonGia,
            banCat.SoLuong,
            banCat.GhiChu.Trim(),
            vatTuChon: null,
            moc: null,
            chenDuoi: false,
            out var canSua);

        if (dongMoi is null)
        {
            // Người dùng bấm "Không" ở một câu hỏi: trả lại dòng gõ dở y như cũ để sửa tiếp.
            ChepNoiDung(banCat, nhap);
            LamMoiDongNhap();
            if (canSua != OCanSua.KhongCo)
            {
                DatConTroDongNhap(canSua);
            }

            return;
        }

        // Ghi xong đã có dòng trống mới ở cuối; đưa con trỏ về đó để gõ tiếp dòng nữa.
        if (doPhimEnter)
        {
            DatConTroDongNhap(OCanSua.TenHang);
        }
    }

    /// <summary>Đưa con trỏ về một ô của dòng đang gõ dở và mở luôn chế độ sửa.</summary>
    private void DatConTroDongNhap(OCanSua o)
    {
        if (_dongNhap is not { } nhap || _luoiCT.ReadOnly)
        {
            return;
        }

        var thuocTinh = o switch
        {
            OCanSua.DonGia => nameof(ChiTietHoaDon.DonGia),
            OCanSua.SoLuong => nameof(ChiTietHoaDon.SoLuong),
            _ => nameof(ChiTietHoaDon.TenHang),
        };

        for (var dong = 0; dong < _luoiCT.Rows.Count; dong++)
        {
            if (!ReferenceEquals(_luoiCT.Rows[dong].DataBoundItem, nhap))
            {
                continue;
            }

            for (var cot = 0; cot < _luoiCT.Columns.Count; cot++)
            {
                if (_luoiCT.Columns[cot].DataPropertyName != thuocTinh)
                {
                    continue;
                }

                _luoiCT.CurrentCell = _luoiCT.Rows[dong].Cells[cot];
                _luoiCT.BeginEdit(selectAll: true);
                return;
            }

            return;
        }
    }

    /// <summary>Xoá trắng dòng đang gõ dở, giữ lại ngày để gõ lại từ đầu cho nhanh.</summary>
    private void XoaTrangDongNhap(bool nhacLai = true)
    {
        if (_dongNhap is not { } nhap)
        {
            return;
        }

        nhap.VatTuId = null;
        nhap.TenHang = string.Empty;
        nhap.DonVi = string.Empty;
        nhap.DonGia = 0m;
        nhap.SoLuong = 0m;
        nhap.GhiChu = string.Empty;

        LamMoiDongNhap();

        if (nhacLai)
        {
            _lblTrangThai.Text = "Đã xoá trắng dòng đang gõ dở.";
        }
    }

    /// <summary>Vẽ lại dòng đang gõ dở sau khi sửa thẳng vào đối tượng phía sau nó.</summary>
    private void LamMoiDongNhap()
    {
        if (_dongNhap is not { } nhap)
        {
            return;
        }

        var viTri = _nguonCT.IndexOf(nhap);
        if (viTri >= 0)
        {
            _nguonCT.ResetItem(viTri);
        }
    }

    /// <summary>Chép nội dung một dòng hàng sang dòng khác (giữ nguyên Id của dòng nhận).</summary>
    private static void ChepNoiDung(ChiTietHoaDon tu, ChiTietHoaDon sang)
    {
        sang.Ngay = tu.Ngay;
        sang.VatTuId = tu.VatTuId;
        sang.TenHang = tu.TenHang;
        sang.DonVi = tu.DonVi;
        sang.DonGia = tu.DonGia;
        sang.SoLuong = tu.SoLuong;
        sang.GhiChu = tu.GhiChu;
    }

    /// <summary>
    /// Các dòng thật đang được chọn, theo đúng thứ tự hiện trên bảng. Không chọn gì thì lấy
    /// dòng con trỏ đang đứng. Dòng vàng cuối bảng (dòng đang gõ dở) không tính.
    /// </summary>
    private List<ChiTietHoaDon> DongDaChon()
    {
        var ds = new List<ChiTietHoaDon>();
        foreach (DataGridViewRow hang in _luoiCT.Rows)
        {
            if (hang.Selected && hang.DataBoundItem is ChiTietHoaDon dong && !LaDongNhap(dong))
            {
                ds.Add(dong);
            }
        }

        if (ds.Count == 0 && _luoiCT.CurrentRow?.DataBoundItem is ChiTietHoaDon hienTai && !LaDongNhap(hienTai))
        {
            ds.Add(hienTai);
        }

        return ds;
    }

    /// <summary>
    /// Chọn lại đúng những dòng vừa làm việc với, để bấm Alt+↑ / Alt+↓ liên tiếp là cả nhóm
    /// đi tiếp chứ không phải chọn lại từ đầu mỗi lần.
    /// </summary>
    private void ChonLaiCacDong(IReadOnlyCollection<ChiTietHoaDon> dong)
    {
        if (dong.Count <= 1)
        {
            return;
        }

        var id = dong.Select(c => c.Id).ToHashSet();
        foreach (DataGridViewRow hang in _luoiCT.Rows)
        {
            if (hang.DataBoundItem is ChiTietHoaDon c && id.Contains(c.Id))
            {
                hang.Selected = true;
            }
        }
    }

    /// <summary>
    /// Chọn hết các dòng thật của hoá đơn (Ctrl+A). Dòng vàng cuối bảng không chọn: nó chưa vào
    /// sổ nên xoá hay chuyển nó cùng cả nhóm đều vô nghĩa.
    /// <para>
    /// Không dùng <c>SelectAll</c> của Windows, và cũng không dời con trỏ: dời con trỏ khỏi dòng
    /// vàng là phần mềm ghi luôn dòng đang gõ dở vào sổ, mà người dùng chỉ muốn chọn dòng.
    /// </para>
    /// </summary>
    private void ChonTatCaDong()
    {
        var so = 0;
        foreach (DataGridViewRow hang in _luoiCT.Rows)
        {
            var laDongThat = hang.DataBoundItem is ChiTietHoaDon dong && !LaDongNhap(dong);
            hang.Selected = laDongThat;
            if (laDongThat)
            {
                so++;
            }
        }

        if (so == 0)
        {
            _lblTrangThai.Text = "Hoá đơn chưa có dòng hàng nào để chọn.";
            return;
        }

        NhacNhomDangChon();
    }

    /// <summary>
    /// Nhắc ở thanh dưới số dòng đang chọn và tổng tiền của nhóm đó. Chỉ nhắc từ hai dòng trở
    /// lên — chọn một dòng là chuyện thường, nhắc cũng bằng không. Bỏ chọn thì trả lại lời nhắc
    /// thường của màn hình.
    /// </summary>
    private void NhacNhomDangChon()
    {
        if (_dangNap || !_sanSang)
        {
            return;
        }

        var dong = DongDaChon();
        if (dong.Count >= 2)
        {
            _lblTrangThai.Text = $"Đang chọn {dong.Count} dòng · {So.Tien(dong.Sum(c => c.ThanhTien))}"
                + " — Delete xoá cả nhóm, Alt+↑ / Alt+↓ chuyển cả nhóm.";
            _dangNhacNhom = true;
        }
        else if (_dangNhacNhom)
        {
            _lblTrangThai.Text = NhanCoBan();
            _dangNhacNhom = false;
        }
    }

    /// <summary>Lời nhắc thường của thanh dưới, theo tình trạng hoá đơn đang xem.</summary>
    private string NhanCoBan() => HoaDonHienTai switch
    {
        null => "Chưa có hoá đơn nào — gõ dòng hàng đầu tiên là phần mềm tự mở hoá đơn mới.",
        { LaHoanHang: true } hoaDon => $"{hoaDon.MaHoaDon} là hoá đơn hoàn hàng: "
            + $"hoàn lại {So.Tien(hoaDon.TienHoan)}, đã trừ vào nợ của khách. "
            + "Sửa lại thì xoá tờ này rồi lập tờ hoàn mới.",
        { DaChot: true } hoaDon => $"Hoá đơn {hoaDon.MaHoaDon} đã chốt nên không sửa được. "
            + "Muốn thêm hàng thì mở nút ⋯ rồi chọn \"Mở lại hoá đơn\".",
        _ => "Chọn nhiều dòng: Ctrl+bấm thêm từng dòng, Shift+bấm cả dải, Ctrl+A cả bảng — "
            + "rồi Delete xoá hoặc Alt+↑ / Alt+↓ chuyển cả nhóm.",
    };

    private void XoaDong()
    {
        var canXoa = DongDaChon();

        // Không chọn dòng thật nào mà con trỏ đang đứng ở dòng vàng cuối bảng thì chỉ cần xoá
        // trắng nó, khỏi hỏi han gì — nó chưa vào sổ. Phải xét sau khi đã lấy nhóm dòng đang
        // chọn: Ctrl+A rồi Delete là con trỏ vẫn ở dòng vàng, mà việc cần làm là xoá cả nhóm.
        if (canXoa.Count == 0 && LaDongNhap(_luoiCT.CurrentRow?.DataBoundItem as ChiTietHoaDon))
        {
            XoaTrangDongNhap();
            return;
        }

        if (HoaDonHienTai is not { } hoaDon)
        {
            return;
        }

        if (hoaDon.DaChot)
        {
            HopThoai.CanhBao(this, "Hoá đơn đã chốt, không xoá được dòng.");
            return;
        }

        if (hoaDon.LaHoanHang)
        {
            HopThoai.CanhBao(this, ChanSuaToHoan);
            return;
        }

        if (canXoa.Count == 0)
        {
            _lblTrangThai.Text = "Hãy chọn dòng hàng cần xoá (Ctrl+bấm để chọn thêm, Shift+bấm để chọn cả dải).";
            return;
        }

        var moTa = canXoa.Count == 1
            ? $"Xoá dòng \"{canXoa[0].TenHang}\" ngày {canXoa[0].Ngay:dd/MM/yyyy}?"
            : $"Xoá {canXoa.Count} dòng đã chọn?";
        if (!HopThoai.Hoi(this, $"{moTa}\n\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        // Nhớ trước dòng liền kề để xoá xong con trỏ đứng ngay chỗ cũ, khỏi nhảy về đầu bảng —
        // xoá mấy dòng ở giữa một hoá đơn dài mới đỡ phải cuộn lại từ đầu mỗi lần.
        var thuTu = ThuTuDong.TheoThuTu(hoaDon.ChiTiet);
        var idXoa = canXoa.Select(c => c.Id).ToHashSet();
        var viTriCuoi = thuTu.FindIndex(c => c.Id == canXoa[^1].Id);
        var dongKe = thuTu.Skip(viTriCuoi + 1).FirstOrDefault(c => !idXoa.Contains(c.Id))
            ?? thuTu.Take(Math.Max(0, viTriCuoi)).LastOrDefault(c => !idXoa.Contains(c.Id));

        var tenViec = canXoa.Count == 1 ? $"Xoá dòng \"{canXoa[0].TenHang}\"" : $"Xoá {canXoa.Count} dòng hàng";
        _kho.ThucHien(tenViec, () => hoaDon.ChiTiet.RemoveAll(c => idXoa.Contains(c.Id)), phatSuKien: false);

        NapHoaDon(_hoaDonId);
        NapChiTiet(dongKe?.Id);
        _lblTrangThai.Text = canXoa.Count == 1
            ? $"Đã xoá dòng {canXoa[0].TenHang}. Bấm Ctrl+Z để lấy lại."
            : $"Đã xoá {canXoa.Count} dòng. Bấm Ctrl+Z để lấy lại.";
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

        if (hoaDon is { LaHoanHang: true })
        {
            HopThoai.CanhBao(this, ChanSuaToHoan);
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

        // Chặn ngay từ đây, đừng để người dùng gõ xong cả chục dòng rồi mới báo không ghi được.
        if (HoaDonHienTai is { LaHoanHang: true })
        {
            HopThoai.CanhBao(this, ChanSuaToHoan);
            return;
        }

        using var form = new NhapNhieuDongForm(_khachId, _dtNgay.Value.Date);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua.Count == 0)
        {
            return;
        }

        GhiNhieuDong(form.KetQua, $"Nhập nhanh {form.KetQua.Count} dòng");
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

        if (hoaDon.LaHoanHang)
        {
            HopThoai.CanhBao(this, ChanSuaToHoan);
            return;
        }

        var canChuyen = DongDaChon();
        if (canChuyen.Count == 0)
        {
            _lblTrangThai.Text = "Hãy chọn dòng muốn chuyển (Ctrl+bấm để chọn thêm, Shift+bấm để chọn cả dải).";
            return;
        }

        // Chuyển thử trước rồi mới ghi lịch sử, để không đẻ ra bước hoàn tác rỗng khi dòng đã
        // nằm ở đầu / cuối ngày của nó.
        var truoc = _kho.ChupNhanh();
        var soDaChuyen = ThuTuDong.ChuyenNhom(hoaDon.ChiTiet, canChuyen.Select(c => c.Id), xuong);

        if (soDaChuyen == 0)
        {
            var dau = canChuyen[0];
            _lblTrangThai.Text = canChuyen.Count == 1
                ? $"Dòng \"{dau.TenHang}\" đã ở {(xuong ? "cuối" : "đầu")} ngày "
                  + $"{dau.Ngay:dd/MM/yyyy} rồi. Muốn sang ngày khác thì sửa ô NGÀY."
                : $"Nhóm dòng đã chọn đã ở {(xuong ? "cuối" : "đầu")} ngày của nó rồi.";
            return;
        }

        var tenViec = canChuyen.Count == 1
            ? $"Chuyển dòng \"{canChuyen[0].TenHang}\" {(xuong ? "xuống" : "lên")}"
            : $"Chuyển {soDaChuyen} dòng {(xuong ? "xuống" : "lên")}";
        _kho.GhiNhan(truoc, tenViec, phatSuKien: false);

        NapChiTiet(canChuyen[0].Id);
        ChonLaiCacDong(canChuyen);
        _lblTrangThai.Text = canChuyen.Count == 1
            ? $"Đã chuyển dòng {canChuyen[0].TenHang} {(xuong ? "xuống dưới" : "lên trên")}. Bấm Ctrl+Z nếu muốn quay lại."
            : $"Đã chuyển {soDaChuyen} dòng {(xuong ? "xuống dưới" : "lên trên")}. Bấm Ctrl+Z nếu muốn quay lại.";
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

    /// <summary>
    /// Câu nhắc khi người dùng định sửa dòng hàng ngay trên tờ hoàn hàng. Số hoàn phải khớp
    /// với hoá đơn gốc nên tờ hoàn chỉ lập một lần, sửa thì xoá đi lập lại.
    /// </summary>
    private const string ChanSuaToHoan =
        "Đây là hoá đơn hoàn hàng nên không sửa dòng hàng ở đây được.\n\n"
        + "Muốn hoàn thêm hoặc hoàn khác đi: chọn hoá đơn bán ở ô trên rồi mở lại \"Hoàn hàng "
        + "cho hoá đơn này\", hoặc xoá tờ hoàn này rồi lập lại.";

    /// <summary>Lập tờ hoàn hàng cho hoá đơn đang xem, rồi nhảy sang xem luôn tờ vừa lập.</summary>
    private void MoHoanHang()
    {
        if (HoaDonHienTai is not { } hoaDon)
        {
            HopThoai.CanhBao(this, "Chưa có hoá đơn nào để hoàn hàng.");
            return;
        }

        if (hoaDon.LaHoanHang)
        {
            HopThoai.CanhBao(
                this,
                "Đây đã là hoá đơn hoàn hàng.\n\nChọn hoá đơn bán ở ô trên rồi hoàn hàng cho nó.");
            return;
        }

        if (hoaDon.ChiTiet.Count == 0)
        {
            HopThoai.CanhBao(this, "Hoá đơn chưa có dòng hàng nào nên chưa có gì để hoàn.");
            return;
        }

        if (HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        using var form = new HoanHangForm(hoaDon.Id);
        if (form.ShowDialog(this) != DialogResult.OK || form.HoaDonHoanDaTao is not { } id)
        {
            return;
        }

        NapHoaDon(id);

        var toHoan = _kho.TimHoaDon(id);
        _lblTrangThai.Text = toHoan is null
            ? "Đã lập hoá đơn hoàn hàng."
            : $"Đã lập hoá đơn hoàn hàng {toHoan.MaHoaDon}: hoàn lại {So.Tien(toHoan.TienHoan)} "
              + $"cho hoá đơn {hoaDon.MaHoaDon}, đã trừ vào nợ của khách. Bấm Ctrl+Z nếu muốn bỏ.";
    }

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

        var hoaDonGoc = hoaDon.HoaDonGocId is { } gocId ? _kho.TimHoaDon(gocId) : null;

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
            using var taiLieu = new InHoaDon(hoaDon, khach, ThongTinCuaHang.DocTuMau(), hoaDonGoc: hoaDonGoc);
            using var form = new XemTruocForm(taiLieu);
            form.ShowDialog(this);
            _lblTrangThai.Text = hoaDon.LaHoanHang
                ? $"Hoá đơn hoàn hàng {hoaDon.MaHoaDon}: {taiLieu.SoTrang} trang."
                : $"Hoá đơn {hoaDon.MaHoaDon}: {taiLieu.SoTrang} trang.";
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

        var tenGoiY = TenFileHopLe(
            $"{(hoaDon.LaHoanHang ? "HoanHang" : "HoaDon")} {hoaDon.MaHoaDon} - {khach.Ten}.xls");
        using var hopThoai = new SaveFileDialog
        {
            Title = hoaDon.LaHoanHang ? "Xuất hoá đơn hoàn hàng ra Excel" : "Xuất hoá đơn ra Excel",
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
            XuatHoaDon.Xuat(
                hoaDon,
                khach,
                hopThoai.FileName,
                ngayIn: DateTime.Today,
                hoaDonGoc: hoaDon.HoaDonGocId is { } gocId ? _kho.TimHoaDon(gocId) : null);
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

    /// <param name="tiepTheo">
    /// Ô nhảy tới khi bấm Enter. Để trống thì Enter ghi luôn dòng hàng.
    /// </param>
    private void GanPhimEnter(Control dieuKhien, Control? tiepTheo = null)
    {
        dieuKhien.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            // Danh sách gợi ý đang bung: Enter là để chọn dòng trong đó, đừng cướp phím.
            if (s is ComboBox cbo && cbo.DroppedDown)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (tiepTheo is not null)
            {
                tiepTheo.Focus();
                if (tiepTheo is TextBox o)
                {
                    o.SelectAll();
                }

                return;
            }

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
            case Keys.Control | Keys.A when !dangSuaO && _luoiCT.Focused:
                ChonTatCaDong();
                return true;
            // Enter ở dòng trống cuối lưới: chốt dòng đang gõ dở thành hàng thật.
            case Keys.Enter when _luoiCT.ContainsFocus
                                 && LaDongNhap(_luoiCT.CurrentRow?.DataBoundItem as ChiTietHoaDon):
                _luoiCT.EndEdit();
                HenGhiDongNhap(doPhimEnter: true);
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
            $"{HD.MaHoaDon}  ·  {HD.NgayMo:dd/MM/yyyy}"
            + (HD.LaHoanHang ? "  ·  hoàn hàng" : string.Empty)
            + (HD.DaChot ? "  ·  đã chốt" : string.Empty);
    }
}
