using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Đọc hoá đơn Excel rồi nhập các dòng hàng vào phần mềm.
/// <para>
/// Mẫu giấy của cửa hàng để trang đầu và các trang sau ở hai file riêng (<c>trang-1.xls</c> có
/// phần đầu với tên khách, <c>trang-sau.xls</c> chỉ có bảng hàng), nên một tờ hoá đơn dài nằm
/// ở nhiều file. Màn hình này gom chúng thành một lô: thêm trang 1 trước, rồi thêm tiếp từng
/// trang sau, thứ tự thêm vào là thứ tự trang. Tên khách của cả tờ đọc ở trang 1.
/// </para>
/// <para>
/// Trên giấy chỉ có "Ngày … tháng …", năm thì mẫu in sẵn "năm 20........." nên hay bỏ trống —
/// vì vậy có ô chọn NĂM ngay lúc nhập file, ghép với ngày/tháng đọc được thành ngày lấy hàng.
/// </para>
/// <para>
/// Tờ hoàn hàng cũng là một đơn hàng, có file Excel riêng và nhập vào y như hoá đơn bán —
/// chỉ khác là tiền của nó trừ đi. Đọc thấy tên tờ là hoàn hàng thì màn hình này chuyển sang
/// nhập vào tờ hoàn (tạo tờ mới hoặc chọn tờ hoàn đang mở), chứ không đổ hàng hoàn vào hoá
/// đơn bán.
/// </para>
/// </summary>
public sealed class NhapExcelForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;
    private readonly int _nam;
    private readonly Guid? _dichBanDau;

    private readonly DataGridView _luoiBang = new();
    private readonly BindingList<DongBang> _nguonBang = new();
    private readonly DataGridView _luoiXem = new();
    private readonly BindingList<ChiTietHoaDon> _nguonXem = new();

    private readonly TextBox _txtFile = Theme.O(360);
    private readonly ComboBox _cboNam = new();
    private readonly DateTimePicker _dtNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = Theme.DangNgay, Font = Theme.FontNhap };
    private readonly ComboBox _cboDich = new();
    // Hai nhãn này chứa **câu**, không phải nhãn ngắn: cho tự xuống dòng và tự cao theo chữ,
    // chứ nhét câu vào một dòng cao cứng là cắt mất đuôi ngay ở cỡ chữ 125%.
    // Giữ tham chiếu: ToolTip không được control nào giữ hộ, bị dọn rác là mất lời mách.
    private readonly ToolTip _mach = new() { InitialDelay = 250, AutoPopDelay = 8000 };

    private readonly Label _lblTomTat = Theme.NhanDaiDong(string.Empty, Theme.FontDam, Theme.Xam);
    private readonly Button _btnXemCaLo = Theme.NutPhu("Xem cả lô", 150, 30, noTheoChu: true);

    /// <summary>
    /// Các dòng hàng sẽ nhập vào hoá đơn: dòng của <b>mọi trang đang tích</b>, theo thứ tự lô.
    /// Giữ riêng khỏi bảng xem trước bên phải vì bảng đó chỉ bày một file — nhập theo bảng ấy
    /// thì bấm Nhập lúc đang xem một file là mất hàng của các file còn lại.
    /// </summary>
    private readonly List<ChiTietHoaDon> _dongSeNhap = new();

    /// <summary>
    /// Bảng bên phải đang bày cả lô, hay chỉ file đang chọn bên trái. Mặc định là chỉ một file:
    /// lô gom nhiều file thì đổ chung vào một bảng dài, nhìn không ra dòng nào của file nào.
    /// </summary>
    private bool _xemCaLo;
    private readonly Label _lblCanhBao = Theme.NhanDaiDong(string.Empty, Theme.FontPhu, Theme.Cam);

    private bool _dangNap;

    /// <summary>Loại của các bảng đang tích: tờ bán, tờ hoàn, hay tích lẫn cả hai.</summary>
    private LoaiToNhap _loai = LoaiToNhap.KhongCo;

    /// <summary>Thứ tự trang của lô đang tích: có trang 1 chưa, trang 1 có đứng đầu không.</summary>
    private XetThuTuTrang _thuTu = XetThuTuTrang.KhongCo;

    /// <summary>
    /// Mã hoá đơn gốc ghi trên các bảng hoàn đang tích ("Hoàn cho hoá đơn HD2026-02"). Đọc theo
    /// đúng những bảng đang tích chứ không quét cả file: bỏ tích một tờ là bỏ luôn mã của nó.
    /// </summary>
    private string? _maGoc;

    /// <summary>Các bảng đang tích ghi hoàn cho nhiều hoá đơn khác nhau.</summary>
    private bool _lonMaGoc;

    private bool _daNapDich;

    public NhapExcelForm(Guid khachId, int nam, Guid? hoaDonDangChon, string duongDanFile)
    {
        _khachId = khachId;
        _nam = nam;
        _dichBanDau = hoaDonDangChon;

        Text = "Nhập hoá đơn từ Excel";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1400, 840);
        MinimumSize = new Size(1240, 760);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        // Nạp trang đầu rồi mới dựng ô "NHẬP VÀO": danh sách hoá đơn đích tuỳ vào tờ này là
        // tờ bán hay tờ hoàn.
        ThemTrang(duongDanFile);
    }

    /// <summary>Số dòng hàng đã nhập được sau khi bấm Nhập.</summary>
    public int SoDongDaNhap { get; private set; }

    /// <summary>Hoá đơn đã nhận dữ liệu, để màn hình gọi mở đúng hoá đơn đó.</summary>
    public Guid? HoaDonDaNhap { get; private set; }

    /// <summary>
    /// Số tiền hoàn của riêng lần nhập này (số dương). Nhập thêm vào tờ hoàn đã có sẵn thì đây
    /// không phải tổng của cả tờ, nên nơi gọi nói về lần nhập vừa rồi thì lấy con số này.
    /// </summary>
    public decimal TienHoanDaNhap { get; private set; }

    private KhachHang? Khach => _kho.TimKhach(_khachId);

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ hai bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "NHẬP HOÁ ĐƠN TỪ EXCEL",
                "Thêm trang 1 trước, rồi các trang sau  ·  nhập vào hoá đơn bán hay hoàn hàng",
                tuCao: true),
            0,
            0);

        khung.Controls.Add(TaoThanhChon(), 0, 1);
        khung.Controls.Add(TaoThanNoiDung(), 0, 2);
        khung.Controls.Add(TaoThanhCanhBao(), 0, 3);
        khung.Controls.Add(TaoThanhDuoi(), 0, 4);

        Controls.Add(khung);
    }

    /// <summary>
    /// Hàng chọn file. Hai cái nút không nằm trong <see cref="Theme.Truong"/> nữa: nút ở đó bị
    /// khoá cứng bề ngang nên chữ dài hơn nút là cắt ("Bỏ trang này" còn thấy "Bỏ trang").
    /// </summary>
    private Control TaoThanhChon()
    {
        _txtFile.ReadOnly = true;
        _txtFile.BackColor = Color.White;
        _txtFile.TabStop = false;

        var btnThemTrang = Theme.Nut("+ Thêm trang", Theme.Chinh, 168, 34, noTheoChu: true);
        btnThemTrang.Click += (_, _) => ChonThemTrang();

        var btnBoTrang = Theme.NutPhu("Bỏ trang", 132, 34, noTheoChu: true);
        btnBoTrang.Click += (_, _) => BoTrangDangChon();

        // Năm không có trên giấy (mẫu in sẵn "năm 20.........") nên phải chọn ở đây. Bày sẵn
        // vài năm quanh năm của sổ đang mở: hoá đơn cũ của cửa hàng là giấy của mấy năm trước.
        _cboNam.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNam.Font = Theme.FontNhap;
        for (var nam = _nam + 1; nam >= _nam - 8; nam--)
        {
            _cboNam.Items.Add(nam);
        }

        _cboNam.SelectedItem = _nam;
        _cboNam.SelectedIndexChanged += (_, _) =>
        {
            if (_dangNap)
            {
                return;
            }

            // Đổi năm là đổi ngày trên giấy của cả lô: ngày/tháng vẫn đọc từ file, chỉ ghép
            // lại với năm mới.
            if (NgayTrenGiayCuaLo() is { } ngay)
            {
                _dtNgay.Value = ngay;
            }

            CapNhatXemTruoc();
        };

        _dtNgay.Value = DateTime.Today;
        _dtNgay.ValueChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                CapNhatXemTruoc();
            }
        };

        _cboDich.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboDich.Font = Theme.FontNhap;

        // Nhãn "NGÀY LẤY HÀNG CHO CÁC DÒNG" dài gấp rưỡi ô của nó nên bị cắt cụt. Nhãn nói
        // gọn, còn câu đầy đủ để vào lời mách khi trỏ chuột vào ô.
        _mach.SetToolTip(
            _dtNgay,
            "Ngày lấy hàng cho những dòng không có mốc ngày viết trên giấy.");

        var nhomNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 18, 0),
        };
        nhomNut.Controls.Add(btnThemTrang);
        nhomNut.Controls.Add(btnBoTrang);

        return Theme.HangO(
            Theme.ChinhNhat,
            Theme.Truong("CÁC FILE TRONG LÔ", _txtFile, 340),
            nhomNut,
            Theme.Truong("NĂM CỦA TỜ", _cboNam, 124),
            Theme.Truong("NGÀY LẤY HÀNG", _dtNgay, 200),
            Theme.Truong("NHẬP VÀO", _cboDich, 290));
    }

    /// <summary>Năm người dùng chọn cho tờ giấy này.</summary>
    private int NamChon => _cboNam.SelectedItem is int nam ? nam : _nam;

    private Control TaoThanNoiDung()
    {
        var than = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 8, 20, 0),
        };
        // Chia theo phần trăm chứ không cột trái cứng 660px: cỡ chữ to lên thì cả hai bảng
        // cùng rộng thêm theo cửa sổ.
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        // Danh sách trang trong lô, theo đúng thứ tự đã thêm vào
        var cotTrai = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Nen,
            Margin = new Padding(0, 0, 16, 0),
        };
        cotTrai.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cotTrai.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cotTrai.Controls.Add(
            Theme.NhanDaiDong("CÁC TRANG TRONG LÔ  ·  tích để lấy", Theme.FontDam, Theme.Xam),
            0,
            0);

        Theme.ApDungLuoi(_luoiBang);
        var cotChon = new DataGridViewCheckBoxColumn
        {
            Name = "colChon",
            DataPropertyName = nameof(DongBang.Chon),
            HeaderText = "LẤY",
            FillWeight = 50,

            // Ô tích phải đủ rộng để thấy và bấm trúng: cột chia theo tỷ lệ nên bảng chật là nó
            // co lại còn một vạch, người dùng không biết là có chỗ tích, tưởng file nào thêm
            // vào cũng phải nhập.
            MinimumWidth = 66,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        _luoiBang.Columns.Add(cotChon);

        // Bảy cột chứ không phải chín: chín cột trong khoảng hẹp này là cột nào cũng còn vài
        // chục điểm ảnh, đọc ra "to1.x...", "T..." — nhìn thì có chữ mà chẳng biết chữ gì. Tên
        // bảng trong file và tên khách đọc trên trang đưa vào lời mách của dòng.
        // FILE đứng ngay sau ô tích: tích là tích lấy file ấy hay không, hai thứ phải nhìn thấy
        // cùng lúc.
        _luoiBang.Columns.AddRange(
            Theme.Cot(nameof(DongBang.TenFile), "FILE", 180, toiThieu: 110),
            Theme.Cot(nameof(DongBang.SoTrang), "TRANG", 60, canPhai: true),
            Theme.Cot(nameof(DongBang.LoaiTrang), "MẪU", 95, toiThieu: 92),
            Theme.Cot(nameof(DongBang.Loai), "LOẠI", 90, toiThieu: 88),
            Theme.Cot(nameof(DongBang.SoDong), "DÒNG", 70, canPhai: true),
            Theme.Cot(nameof(DongBang.Tong), "TIỀN", 110, "#,##0", canPhai: true, toiThieu: 100));

        _luoiBang.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _nguonBang.Count)
            {
                var bang = _nguonBang[e.RowIndex];
                var ten = bang.TenKhach.Length > 0 ? bang.TenKhach : "(trang này không ghi tên)";
                e.ToolTipText = $"File: {bang.TenFile}\nBảng trong file: {bang.Ten}\nTên khách đọc được: {ten}";
            }
        };

        // Trang 1 mang tên khách và phải đứng đầu lô, nên tô đậm cho khác các trang nối tiếp.
        _luoiBang.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _nguonBang.Count || e.CellStyle is not { } kieu)
            {
                return;
            }

            if (_nguonBang[e.RowIndex].Trang.Loai == LoaiTrangGiay.Trang1)
            {
                kieu.Font = Theme.FontLuoiDam;
            }
        };
        _luoiBang.DataSource = _nguonBang;
        _luoiBang.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                _luoiBang.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _luoiBang.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && !_dangNap)
            {
                CapNhatXemTruoc();
            }
        };

        // Bấm vào file nào thì bảng bên phải bày đúng file ấy: lô mấy file đổ chung vào một
        // bảng dài thì nhìn không ra dòng nào của file nào, mà đây đúng là chỗ để dò lại xem
        // file vừa thêm có đúng tờ mình định nhập không.
        _luoiBang.SelectionChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                CapNhatXemTruoc();
            }
        };
        cotTrai.Controls.Add(Theme.Khung(_luoiBang), 0, 1);

        // Xem trước các dòng sẽ nhập
        var cotPhai = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Nen,
        };
        cotPhai.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cotPhai.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cotPhai.Controls.Add(TaoDauBangXem(), 0, 0);

        Theme.ApDungLuoi(_luoiXem);
        _luoiXem.ReadOnly = true;
        // Bề ngang thấp nhất đặt theo **nội dung dài nhất sẽ hiện**: cột ngày mà hẹp hơn
        // "25/02/2026" thì cả cột đọc ra "25...", nhìn dòng nào cũng như dòng nào.
        _luoiXem.Columns.AddRange(
            Theme.Cot(nameof(ChiTietHoaDon.Ngay), "NGÀY", 100, Theme.DangNgay, toiThieu: 104),
            Theme.Cot(nameof(ChiTietHoaDon.TenHang), "TÊN HÀNG", 250, toiThieu: 150),
            Theme.Cot(nameof(ChiTietHoaDon.DonVi), "ĐƠN VỊ", 90),
            Theme.Cot(nameof(ChiTietHoaDon.SoLuong), "SỐ LƯỢNG", 110, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(ChiTietHoaDon.DonGia), "ĐƠN GIÁ", 120, "#,##0", canPhai: true, toiThieu: 104),
            Theme.Cot(nameof(ChiTietHoaDon.ThanhTien), "THÀNH TIỀN", 145, "#,##0", canPhai: true, toiThieu: 120));
        _luoiXem.DataSource = _nguonXem;
        cotPhai.Controls.Add(Theme.Khung(_luoiXem), 0, 1);

        than.Controls.Add(cotTrai, 0, 0);
        than.Controls.Add(cotPhai, 1, 0);
        return than;
    }

    /// <summary>
    /// Đầu bảng xem trước: câu tóm tắt và nút đổi giữa "chỉ file đang chọn" với "cả lô". Bảng
    /// bên phải mặc định bày một file, nên phải có chỗ xem lại toàn bộ những gì sắp vào sổ.
    /// </summary>
    private Control TaoDauBangXem()
    {
        var nen = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Nen,
            Margin = new Padding(0),
        };
        nen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nen.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        nen.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _btnXemCaLo.Margin = new Padding(10, 0, 0, 4);
        _btnXemCaLo.Click += (_, _) =>
        {
            _xemCaLo = !_xemCaLo;
            CapNhatXemTruoc();
        };

        nen.Controls.Add(_lblTomTat, 0, 0);
        nen.Controls.Add(_btnXemCaLo, 1, 0);
        return nen;
    }

    /// <summary>
    /// Dải nhắc trước khi nhập. Mỗi lời nhắc một dòng riêng và dải <b>tự cao theo số dòng</b>:
    /// nối nhau trong một dải cao cứng 66px thì từ lời nhắc thứ hai trở đi là bị cắt mất.
    /// </summary>
    private Control TaoThanhCanhBao()
    {
        var nen = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Nen,
            Padding = new Padding(22, 2, 22, 2),
        };
        nen.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        nen.Controls.Add(_lblCanhBao, 0, 0);
        return nen;
    }

    private Control TaoThanhDuoi()
    {
        var btnNhap = Theme.Nut("NHẬP VÀO HOÁ ĐƠN", Theme.Xanh, 260, 52, noTheoChu: true);
        btnNhap.Click += (_, _) => Nhap();

        var btnHuy = Theme.NutPhu("Huỷ", 140, 52, noTheoChu: true);
        btnHuy.Click += (_, _) => Close();

        return Theme.ThanhDuoi(
            Theme.GhiChuPhai("Các dòng nhập vào vẫn sửa được như bình thường  ·  Ctrl+Z để hoàn tác"),
            btnNhap,
            btnHuy);
    }

    /// <summary>
    /// Câu nhắc khi người dùng tích lẫn tờ bán với tờ hoàn: một tờ cộng vào nợ của khách, tờ
    /// kia trừ ra, dồn cả hai vào một hoá đơn là sổ ghi ngược dấu.
    /// </summary>
    private const string ChanLonLoai =
        "Đang tích lẫn cả bảng bán hàng và bảng hoàn hàng. Hai loại này vào hai hoá đơn khác "
        + "nhau (tờ bán cộng vào nợ, tờ hoàn trừ ra) nên hãy nhập từng loại một lượt.";

    /// <summary>
    /// Câu nhắc khi các bảng đang tích ghi hoàn cho những hoá đơn khác nhau: cả nhóm chỉ vào
    /// được một tờ hoàn, mà tờ hoàn chỉ nối vào một hoá đơn bán — dồn cả nhóm vào một mã là
    /// trừ số đã hoàn vào hoá đơn không phải nó.
    /// </summary>
    private const string ChanLonMaGoc =
        "Các bảng đang tích ghi hoàn cho những hoá đơn khác nhau. Mỗi tờ hoàn chỉ nối vào một "
        + "hoá đơn bán nên hãy tích riêng từng hoá đơn gốc một lượt.";

    /// <summary>
    /// Dựng lại ô "NHẬP VÀO" theo loại tờ đang tích: tờ bán chỉ nhập vào hoá đơn bán, tờ hoàn
    /// chỉ nhập vào tờ hoàn — một tờ cộng vào nợ của khách, tờ kia trừ ra, đổ lẫn vào nhau là
    /// sổ sai dấu. Hoá đơn đã chốt cũng bày ra để người dùng thấy vì sao chưa nhập được vào đó.
    /// </summary>
    private void NapDich()
    {
        var dangNapCu = _dangNap;
        _dangNap = true;

        // Giữ lại chỗ đang chọn khi đổi loại tờ: lần đầu thì lấy hoá đơn màn đơn hàng đang mở.
        var dangChon = _daNapDich ? (_cboDich.SelectedItem as MucDich)?.Id : _dichBanDau;

        // Tờ hoàn thuộc đúng năm của hoá đơn nó hoàn cho, có thể khác năm đang xem — nhãn phải
        // nói đúng năm đó, không thì người dùng tưởng tờ mới nằm ở năm đang mở.
        var namToMoi = _loai.LaHoanHang ? GocTrenGiay()?.Nam ?? _nam : _nam;

        _cboDich.Items.Clear();
        _cboDich.Items.Add(new MucDich(
            null,
            _loai.LaHoanHang
                ? $"— Tạo hoá đơn hoàn hàng mới ({namToMoi}) —"
                : $"— Tạo hoá đơn mới ({namToMoi}) —"));

        foreach (var hoaDon in _kho.HoaDonCuaKhach(_khachId, _nam)
            .Where(h => h.LaHoanHang == _loai.LaHoanHang))
        {
            var tien = hoaDon.LaHoanHang ? hoaDon.TienHoan : hoaDon.TongTien;
            _cboDich.Items.Add(new MucDich(
                hoaDon.Id,
                $"{hoaDon.MaHoaDon} · mở {hoaDon.NgayMo:dd/MM/yyyy} · {So.Tien(tien)}"
                + (hoaDon.DaChot ? " · đã chốt" : string.Empty)));
        }

        _cboDich.SelectedIndex = 0;
        if (dangChon is { } id)
        {
            for (var i = 0; i < _cboDich.Items.Count; i++)
            {
                if (_cboDich.Items[i] is MucDich muc && muc.Id == id)
                {
                    _cboDich.SelectedIndex = i;
                    break;
                }
            }
        }

        _daNapDich = true;
        _dangNap = dangNapCu;
    }

    private void ChonThemTrang()
    {
        using var hopThoai = new OpenFileDialog
        {
            Title = _nguonBang.Count == 0
                ? "Chọn file trang 1 của tờ hoá đơn"
                : "Chọn file trang tiếp theo của tờ hoá đơn",
            Filter = "File Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Tất cả các file (*.*)|*.*",
        };

        if (hopThoai.ShowDialog(this) == DialogResult.OK)
        {
            ThemTrang(hopThoai.FileName);
        }
    }

    /// <summary>
    /// Đọc một file rồi nối các trang trong đó vào cuối lô. Thứ tự thêm vào là thứ tự trang,
    /// nên một tờ hoá đơn dài thì thêm file trang 1 trước, xong thêm tiếp từng trang sau.
    /// </summary>
    private void ThemTrang(string duongDan)
    {
        var tenFile = Path.GetFileName(duongDan);

        // Thêm hai lần cùng một file là hàng vào sổ hai lần mà trên sổ không còn dấu vết nào
        // để nhận ra, nên hỏi lại trước.
        if (_nguonBang.Any(b => string.Equals(b.TenFile, tenFile, StringComparison.OrdinalIgnoreCase))
            && !HopThoai.Hoi(
                this,
                $"File \"{tenFile}\" đã có trong lô.\n\nThêm nữa là những dòng đó vào sổ hai lần. "
                + "Vẫn thêm?"))
        {
            return;
        }

        KetQuaDocExcel ketQua;
        try
        {
            ketQua = DocHoaDon.Doc(duongDan, _dtNgay.Value.Date, NamChon);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, $"Không đọc được file:\n{duongDan}\n\n{ex.Message}");
            return;
        }

        if (ketQua.Trang.Count == 0)
        {
            HopThoai.CanhBao(
                this,
                $"Không tìm thấy dòng hàng nào trong file \"{tenFile}\".\n\n" +
                "File cần có một dòng tiêu đề gồm TÊN HÀNG, ĐVT, SỐ LƯỢNG, ĐƠN GIÁ, THÀNH TIỀN, " +
                "và ít nhất một dòng đã điền tên hàng hoặc số lượng. File mẫu chưa điền gì thì " +
                "không có dòng nào để lấy.");
            return;
        }

        _dangNap = true;
        _nguonBang.RaiseListChangedEvents = false;

        var dongDauCuaFile = _nguonBang.Count;
        foreach (var trang in ketQua.Trang)
        {
            _nguonBang.Add(TaoDongBang(trang));
        }

        DanhSoLaiTrang();
        _nguonBang.RaiseListChangedEvents = true;
        _nguonBang.ResetBindings();

        // Chuyển sang đứng ở file vừa thêm để bảng bên phải bày ngay file đó: thêm file xong
        // thì việc tiếp theo bao giờ cũng là dò lại xem có đúng tờ mình định nhập không.
        if (dongDauCuaFile < _luoiBang.Rows.Count)
        {
            _luoiBang.CurrentCell = _luoiBang.Rows[dongDauCuaFile].Cells[1];
        }

        // Ngày ghi trên giấy dùng làm gợi ý cho ngày lấy hàng của những dòng không có mốc ngày.
        if (NgayTrenGiayCuaLo() is { } ngay)
        {
            _dtNgay.Value = ngay;
        }

        _txtFile.Text = TenFileTrongLo();
        _dangNap = false;
        CapNhatXemTruoc();
    }

    private static DongBang TaoDongBang(TrangDoc trang) => new()
    {
        Trang = trang,
        Chon = true,
        TenFile = trang.TenFile,
        Ten = trang.TenSheet,
        LoaiTrang = trang.Loai == LoaiTrangGiay.Trang1 ? "Trang 1" : "Trang sau",
        TenKhach = trang.TenKhach ?? string.Empty,
        Loai = trang.LaHoanHang ? "Hoàn hàng" : "Bán hàng",
        SoDong = trang.Dong.Count,
        Tong = trang.TongTien,
    };

    /// <summary>Bỏ trang đang chọn ra khỏi lô — chọn nhầm file thì không phải mở lại màn hình.</summary>
    private void BoTrangDangChon()
    {
        if (_luoiBang.CurrentRow?.DataBoundItem is not DongBang bang)
        {
            HopThoai.CanhBao(this, "Hãy chọn một trang trong bảng bên trái rồi bấm bỏ trang.");
            return;
        }

        _dangNap = true;
        _nguonBang.Remove(bang);
        DanhSoLaiTrang();
        _nguonBang.ResetBindings();
        _txtFile.Text = TenFileTrongLo();
        _dangNap = false;
        CapNhatXemTruoc();
    }

    /// <summary>
    /// Đánh lại số trang theo những trang đang tích: bỏ tích một trang giữa lô thì các trang
    /// dưới lùi số lên, để cột TRANG luôn đúng thứ tự sẽ nhập vào sổ.
    /// </summary>
    private void DanhSoLaiTrang()
    {
        var so = 0;
        foreach (var bang in _nguonBang)
        {
            bang.SoTrang = bang.Chon ? (++so).ToString() : "—";
        }

        // DongBang không báo đổi giá trị nên lưới không tự vẽ lại: bỏ tích một trang mà cột
        // TRANG vẫn giữ số cũ thì người dùng đọc sai thứ tự sắp nhập.
        _luoiBang.Refresh();
    }

    /// <summary>Tên các file đang góp trang vào lô, theo thứ tự thêm vào.</summary>
    private string TenFileTrongLo()
    {
        var ten = _nguonBang
            .Select(b => b.TenFile)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ten.Count == 0 ? "(chưa có trang nào)" : string.Join("  ·  ", ten);
    }

    /// <summary>
    /// Ngày đọc được trên giấy của lô: ưu tiên dòng "Ngày … tháng …" ở chân tờ, không có thì
    /// lấy mốc ngày đầu tiên viết ở cột số thứ tự. Năm luôn lấy từ ô NĂM CỦA TỜ.
    /// </summary>
    private DateTime? NgayTrenGiayCuaLo()
    {
        foreach (var bang in _nguonBang.Where(b => b.Chon))
        {
            var trang = bang.Trang;

            if (trang.NgayTrongThang is { } ngay && trang.ThangTrenGiay is { } thang)
            {
                return GhepNamDaChon(ngay, thang);
            }

            if (trang.NgayThangCuaDong.Count > 0)
            {
                var moc = trang.NgayThangCuaDong.OrderBy(m => m.Key).First().Value;
                return GhepNamDaChon(moc.Ngay, moc.Thang);
            }
        }

        return null;
    }

    /// <summary>Ghép ngày/tháng đọc trên giấy với năm đang chọn ở ô NĂM CỦA TỜ.</summary>
    private DateTime GhepNamDaChon(int ngay, int thang)
    {
        var nam = NamChon;
        return new DateTime(nam, thang, Math.Min(ngay, DateTime.DaysInMonth(nam, thang)));
    }

    private void CapNhatXemTruoc()
    {
        var ngay = _dtNgay.Value.Date;
        DanhSoLaiTrang();
        var dangTich = _nguonBang.Where(b => b.Chon).ToList();

        // Thứ tự trang xét theo đúng nhóm đang tích: bỏ tích trang 1 là lô mất tên khách, tích
        // thêm trang 1 của tờ khác là hai tờ dồn vào một hoá đơn.
        _thuTu = ThuTuTrangGiay.Xet(dangTich.Select(b => b.Trang));

        var loaiMoi = LoaiToNhap.Xet(dangTich.Select(b => b.Trang));

        // Bỏ tích hết, hay đang tích lẫn cả hai loại, thì giữ nguyên danh sách hoá đơn đích của
        // loại đang chọn: tích lại là chọn tiếp được, không mất chỗ đang chọn.
        var theoLoaiCu = dangTich.Count == 0 || loaiMoi.LonLoai;
        var laToHoan = theoLoaiCu ? _loai.LaHoanHang : loaiMoi.LaHoanHang;

        // Mã hoá đơn gốc lấy ở đúng những bảng hoàn đang tích: đổi chỗ tích là đổi hoá đơn gốc,
        // nên ô "NHẬP VÀO" phải dựng lại theo (nhãn năm của tờ hoàn mới đi theo hoá đơn đó).
        var (maGoc, lonMaGoc) = MaGocTrenGiay(dangTich.Where(b => b.Trang.LaHoanHang).Select(b => b.Trang));
        var doiLoai = !_daNapDich || laToHoan != _loai.LaHoanHang || maGoc != _maGoc;

        _loai = loaiMoi with { LaHoanHang = laToHoan };
        _maGoc = maGoc;
        _lonMaGoc = lonMaGoc;
        if (doiLoai)
        {
            NapDich();
        }

        // Chấm ngày cho dòng của **mọi** trang trong lô, kể cả trang chưa tích: bảng bên phải
        // bày được cả file chưa tích để xem trước rồi mới quyết định lấy hay không.
        foreach (var bang in _nguonBang)
        {
            ChamNgayChoDong(bang.Trang, ngay);
        }

        _dongSeNhap.Clear();
        _dongSeNhap.AddRange(dangTich.SelectMany(b => b.Trang.Dong));
        var canhBao = dangTich.SelectMany(b => b.Trang.CanhBao).ToList();

        BayLenBangXem(dangTich.Count);

        if (_loai.LonLoai)
        {
            _lblCanhBao.ForeColor = Theme.Do;
            _lblCanhBao.Text = "⚠ " + ChanLonLoai;
            return;
        }

        if (_lonMaGoc)
        {
            _lblCanhBao.ForeColor = Theme.Do;
            _lblCanhBao.Text = "⚠ " + ChanLonMaGoc;
            return;
        }

        if (_thuTu.Chan is { } chanThuTu)
        {
            _lblCanhBao.ForeColor = Theme.Do;
            _lblCanhBao.Text = "⚠ " + chanThuTu;
            return;
        }

        // Mấy câu nhắc này không chặn nhập, nhưng phải nói ra: sai chỗ nào thì cũng là hàng vào
        // sổ của người khác, hoặc vào năm khác, mà trên sổ không còn dấu vết nào để dò lại.
        var nhac = new List<string>();
        if (_thuTu.Nhac is { } nhacThuTu)
        {
            nhac.Add(nhacThuTu);
        }

        if (LechTenKhach() is { } lechTen)
        {
            nhac.Add(lechTen);
        }

        if (LechNam() is { } lechNam)
        {
            nhac.Add(lechNam);
        }

        if (canhBao.Count > 0)
        {
            nhac.Add($"{canhBao.Count} dòng cần xem lại: {string.Join("  ·  ", canhBao.Take(2))}"
                + (canhBao.Count > 2 ? "  ·  ..." : string.Empty));
        }

        // Mỗi lời nhắc một dòng: nối nhau bằng dấu chấm giữa thì mấy lời nhắc thành một dải
        // chữ dài không ai đọc hết, mà dòng nào cũng quan trọng như nhau.
        _lblCanhBao.ForeColor = Theme.Cam;
        _lblCanhBao.Text = nhac.Count == 0 ? string.Empty : "⚠ " + string.Join("\n⚠ ", nhac);
    }

    /// <summary>
    /// Chấm ngày lấy hàng cho từng dòng của một trang: dòng nào nằm dưới một mốc ngày viết ở
    /// cột số thứ tự thì giữ đúng ngày đó (ghép với năm đang chọn), dòng không có mốc mới lấy
    /// ngày chung của cả lô.
    /// </summary>
    private void ChamNgayChoDong(TrangDoc trang, DateTime ngayChung)
    {
        for (var i = 0; i < trang.Dong.Count; i++)
        {
            trang.Dong[i].Ngay = trang.NgayThangCuaDong.TryGetValue(i, out var moc)
                ? GhepNamDaChon(moc.Ngay, moc.Thang)
                : ngayChung;
        }
    }

    /// <summary>
    /// Đổ dòng lên bảng bên phải: chỉ file đang chọn bên trái, hoặc cả lô nếu người dùng bấm
    /// "Xem cả lô". Xem một file thì bày cả trang chưa tích của file ấy — đang cân nhắc có lấy
    /// file này không thì phải nhìn thấy nó có gì đã.
    /// </summary>
    private void BayLenBangXem(int soTrangTich)
    {
        var fileDangXem = _xemCaLo ? null : FileDangChon();
        var dong = fileDangXem is null
            ? _dongSeNhap
            : _nguonBang
                .Where(b => string.Equals(b.TenFile, fileDangXem, StringComparison.OrdinalIgnoreCase))
                .SelectMany(b => b.Trang.Dong)
                .ToList();

        _nguonXem.RaiseListChangedEvents = false;
        _nguonXem.Clear();
        foreach (var d in dong)
        {
            _nguonXem.Add(d);
        }

        _nguonXem.RaiseListChangedEvents = true;
        _nguonXem.ResetBindings();

        _btnXemCaLo.Text = _xemCaLo ? "Xem file đang chọn" : "Xem cả lô";
        _btnXemCaLo.Visible = _nguonBang.Count > 0;

        // Dòng hoàn mang số lượng âm nên tổng của tờ hoàn là số âm; nói "hoàn lại 90.000đ"
        // dễ đọc hơn là bày ra "-90.000đ".
        var tong = _dongSeNhap.Sum(d => d.ThanhTien);
        var soNgay = _dongSeNhap.Select(d => d.Ngay.Date).Distinct().Count();
        var keTrang = soTrangTich > 1 ? $"{soTrangTich} TRANG · " : string.Empty;
        var keNgay = soNgay > 1 ? $" · {soNgay} NGÀY" : string.Empty;
        var caLo = _loai.LaHoanHang
            ? $"{keTrang}SẼ NHẬP {_dongSeNhap.Count} DÒNG HOÀN{keNgay} · HOÀN LẠI {So.Tien(-tong)} (TRỪ VÀO NỢ)"
            : $"{keTrang}SẼ NHẬP {_dongSeNhap.Count} DÒNG{keNgay} · TỔNG {So.Tien(tong)}";

        // Xem một file thì câu tóm tắt phải nói cả hai con số: bảng đang bày bao nhiêu, và cả
        // lô sắp vào sổ bao nhiêu — không thì nhìn bảng một file mà tưởng đó là tất cả.
        _lblTomTat.Text = fileDangXem is null
            ? caLo
            : $"ĐANG XEM {fileDangXem} · {dong.Count} DÒNG · {So.Tien(dong.Sum(d => d.ThanhTien))}"
              + $"   —   CẢ LÔ: {caLo}";
    }

    /// <summary>Tên file của dòng đang chọn ở bảng trang bên trái.</summary>
    private string? FileDangChon()
    {
        var ten = (_luoiBang.CurrentRow?.DataBoundItem as DongBang)?.TenFile;
        return string.IsNullOrEmpty(ten) ? null : ten;
    }

    /// <summary>
    /// Tên khách đọc ở trang 1 mà khác khách đang mở thì nói ra. Màn hình này nhập vào sổ của
    /// khách đang mở chứ không phải khách ghi trên giấy — lấy nhầm tờ là nợ sang tên người khác.
    /// </summary>
    private string? LechTenKhach()
    {
        if (_thuTu.TenKhach is not { } tenGiay || Khach is not { } khach)
        {
            return null;
        }

        var giay = ChuViet.BoDau(tenGiay).Trim();
        var so = ChuViet.BoDau(khach.Ten).Trim();

        return string.Equals(giay, so, StringComparison.CurrentCultureIgnoreCase)
            ? null
            : $"Giấy ghi khách \"{tenGiay}\" mà đang nhập vào sổ của \"{khach.Ten}\".";
    }

    /// <summary>Giấy có ghi rõ năm mà khác năm đang chọn: năm chọn thắng, nhưng phải nói ra.</summary>
    private string? LechNam()
    {
        var namGiay = _nguonBang
            .Where(b => b.Chon)
            .Select(b => b.Trang.NamTrenGiay)
            .FirstOrDefault(n => n is not null);

        return namGiay is { } nam && nam != NamChon
            ? $"Giấy ghi năm {nam} mà ô NĂM CỦA TỜ đang chọn {NamChon}."
            : null;
    }

    /// <summary>
    /// Mã hoá đơn gốc ghi trên nhóm bảng đang tích, kèm cờ báo nhóm ghi nhiều mã khác nhau.
    /// Đọc theo từng bảng chứ không lấy mã đầu tiên của cả file: file có hai tờ hoàn cho hai
    /// hoá đơn thì bỏ tích tờ này phải bỏ luôn mã của nó.
    /// </summary>
    private static (string? Ma, bool Lon) MaGocTrenGiay(IEnumerable<TrangDoc> trang)
    {
        var ma = trang
            .Select(t => t.MaHoaDonGoc)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m!.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return (ma.FirstOrDefault(), ma.Count > 1);
    }

    /// <summary>Hoá đơn bán mà các bảng đang tích ghi là hoàn cho, tìm trong sổ theo mã trên giấy.</summary>
    private HoaDon? GocTrenGiay() => _lonMaGoc
        ? null
        : BaoCao.HoanHang.TimHoaDonGoc(_kho.HoaDonCuaKhach(_khachId), _maGoc);

    /// <summary>
    /// Lý do hoàn in trên các bảng đang tích — ghi chú của tờ hoàn in ra giấy chính là câu này,
    /// nên cũng phải theo bảng đang tích chứ không phải lý do của tờ nào đó trong file.
    /// </summary>
    private string LyDoTrenGiay() => (_nguonBang
        .Where(b => b.Chon)
        .Select(b => b.Trang.LyDoHoan)
        .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty).Trim();

    private void Nhap()
    {
        if (Khach is null)
        {
            return;
        }

        if (_dongSeNhap.Count == 0)
        {
            HopThoai.CanhBao(this, "Chưa có dòng nào để nhập. Hãy tích chọn ít nhất một trang.");
            return;
        }

        if (_thuTu.Chan is { } chanThuTu)
        {
            HopThoai.CanhBao(this, chanThuTu);
            return;
        }

        if (_loai.LonLoai)
        {
            HopThoai.CanhBao(this, ChanLonLoai);
            return;
        }

        if (_lonMaGoc)
        {
            HopThoai.CanhBao(this, ChanLonMaGoc);
            return;
        }

        if (_cboDich.SelectedItem is not MucDich dich)
        {
            return;
        }

        var laToHoan = _loai.LaHoanHang;
        var ngay = _dtNgay.Value.Date;

        // Lấy theo danh sách của cả lô chứ không theo bảng bên phải: bảng ấy chỉ đang bày một
        // file. Ngày của từng dòng cũng đã chấm sẵn ở đó — dòng có mốc ngày trên giấy giữ ngày
        // của nó, dòng không có mới lấy ngày chung — copy lại đây là mất mốc.
        var dongDoc = _dongSeNhap.Select(d => new ChiTietHoaDon
        {
            Ngay = d.Ngay.Date,
            TenHang = d.TenHang,
            DonVi = d.DonVi,
            DonGia = d.DonGia,
            SoLuong = d.SoLuong,
            VatTuId = _kho.TimVatTuTheoTen(d.TenHang)?.Id,
        }).ToList();

        var hoaDon = dich.Id is { } id ? _kho.TimHoaDon(id) : null;
        var taoMoi = hoaDon is null;

        if (hoaDon is not null && hoaDon.DaChot)
        {
            HopThoai.CanhBao(this, "Hoá đơn này đã chốt. Hãy mở lại hoá đơn trước khi nhập thêm hàng.");
            return;
        }

        // Ô "NHẬP VÀO" chỉ bày hoá đơn cùng loại, nhưng máy khác vừa đổi sổ thì vẫn xét lại:
        // đổ tờ hoàn vào hoá đơn bán là số tiền đảo dấu, không ai nhận ra.
        if (hoaDon is not null && hoaDon.LaHoanHang != laToHoan)
        {
            HopThoai.CanhBao(
                this,
                laToHoan
                    ? "Đây là tờ hoàn hàng nên phải nhập vào hoá đơn hoàn hàng. "
                      + "Hãy chọn lại ở ô NHẬP VÀO."
                    : "Không nhập hàng bán vào hoá đơn hoàn hàng được. Hãy chọn hoá đơn bán hàng.");
            return;
        }

        // Tờ hoàn mới nối vào hoá đơn nó hoàn cho theo mã ghi trên giấy; nhập thêm vào tờ hoàn
        // có sẵn thì theo chỗ nối của tờ đó.
        var gocTrenGiay = laToHoan ? GocTrenGiay() : null;
        var goc = laToHoan
            ? taoMoi
                ? gocTrenGiay
                : hoaDon!.HoaDonGocId is { } gocCuId ? _kho.TimHoaDon(gocCuId) : null
            : null;

        // Giấy ghi hoàn cho hoá đơn này mà tờ hoàn đang chọn nối vào hoá đơn khác thì dừng lại:
        // nhập vào đây là trừ số đã hoàn vào hoá đơn không phải nó, mà hoá đơn trên giấy vẫn
        // để 0 nên còn hoàn được lần thứ hai. Ô "NHẬP VÀO" tự chọn sẵn tờ đang mở nên chuyện
        // này xảy ra chỉ vì không để ý, không phải vì người dùng cố tình.
        if (laToHoan && !taoMoi && !string.IsNullOrWhiteSpace(_maGoc) && gocTrenGiay?.Id != goc?.Id)
        {
            HopThoai.CanhBao(
                this,
                $"Giấy ghi hoàn cho hoá đơn {_maGoc}, mà tờ hoàn {hoaDon!.MaHoaDon} đang "
                + (goc is null ? "không nối vào hoá đơn nào" : $"nối vào hoá đơn {goc.MaHoaDon}")
                + ".\n\nNhập vào đây thì số hoàn trừ vào hoá đơn không phải nó. Hãy chọn "
                + "\"Tạo hoá đơn hoàn hàng mới\" ở ô NHẬP VÀO, hoặc chọn tờ hoàn của đúng "
                + "hoá đơn đó.");
            return;
        }

        // Giấy ghi mã mà tra trong sổ không ra (hoá đơn của khách khác, hay gõ sai mã): tờ hoàn
        // vẫn nhập được và nợ vẫn đúng, nhưng cột ĐÃ HOÀN của hoá đơn kia vẫn để 0 nên hoàn
        // được lần thứ hai — nói trước một câu để người dùng còn kịp xem lại mã.
        if (laToHoan && !string.IsNullOrWhiteSpace(_maGoc) && goc is null && !HopThoai.Hoi(
            this,
            $"Giấy ghi hoàn cho hoá đơn {_maGoc} mà trong sổ của khách này không có mã đó "
            + "(hoá đơn của khách khác, hay giấy ghi sai mã?).\n\nNhập tiếp thì tờ hoàn đứng "
            + "riêng: nợ của khách vẫn trừ đúng, nhưng hoá đơn kia không biết đã hoàn nên vẫn "
            + "hoàn được lần nữa.\n\nVẫn nhập?"))
        {
            return;
        }

        // Biết hoàn cho hoá đơn nào thì ghép từng dòng vào đúng dòng của hoá đơn đó, để màn
        // hình hoàn hàng cộng đúng cột ĐÃ HOÀN và không cho hoàn lần nữa số đã hoàn bằng file.
        var dongMoi = dongDoc;
        var canhBaoGhep = new List<string>();
        if (goc is not null)
        {
            var ghep = BaoCao.HoanHang.GhepVaoHoaDonGoc(_kho.HoaDonCuaKhach(_khachId), goc, dongDoc);
            dongMoi = ghep.Dong;
            canhBaoGhep = ghep.CanhBao;
        }

        hoaDon ??= new HoaDon
        {
            KhachHangId = _khachId,
            Loai = laToHoan ? LoaiHoaDon.HoanHang : LoaiHoaDon.Ban,

            // Tờ hoàn thuộc đúng năm của hoá đơn nó hoàn cho — hai tờ phải cùng năm mới đối
            // chiếu được với nhau.
            HoaDonGocId = goc?.Id,
            Nam = goc?.Nam ?? _nam,
            MaHoaDon = _kho.TaoMaHoaDon(
                _khachId,
                goc?.Nam ?? _nam,
                laToHoan ? LoaiHoaDon.HoanHang : LoaiHoaDon.Ban),
            NgayMo = dongDoc.Count > 0 ? dongDoc.Min(d => d.Ngay) : ngay,

            // Ghi chú của tờ hoàn in ra giấy thành lý do hoàn, nên lấy đúng lý do đọc được
            // trong file chứ không ghi "nhập từ file nào" lên tờ đưa khách.
            GhiChu = laToHoan
                ? LyDoTrenGiay()
                : "Nhập từ " + TenFileTrongLo(),
        };

        var moTaViec = laToHoan
            ? $"Nhập {dongDoc.Count} dòng hoàn hàng từ Excel"
            : $"Nhập {dongDoc.Count} dòng từ Excel";

        _kho.ThucHien(moTaViec, () =>
        {
            if (taoMoi)
            {
                _kho.DuLieu.HoaDons.Add(hoaDon);
            }

            hoaDon.ChiTiet.AddRange(dongMoi);
        }, phatSuKien: false);

        // Đếm theo số dòng trên giấy: ghép vào hoá đơn gốc có thể tách một dòng thành hai khi
        // hoá đơn gốc bán món đó ở hai ngày, người dùng không cần biết chuyện tách đó.
        SoDongDaNhap = dongDoc.Count;
        HoaDonDaNhap = hoaDon.Id;
        TienHoanDaNhap = -dongMoi.Sum(d => d.ThanhTien);

        var loiNhac = laToHoan
            ? $"Đã nhập {dongDoc.Count} dòng vào hoá đơn hoàn hàng {hoaDon.MaHoaDon}"
              + (taoMoi ? " (tờ hoàn mới tạo)." : ".")
              + $"\n\nHoàn lại {So.Tien(TienHoanDaNhap)}, đã trừ vào nợ của khách"
              + (goc is null ? "." : $" — hoàn cho hoá đơn {goc.MaHoaDon}.")
            : $"Đã nhập {dongDoc.Count} dòng vào hoá đơn {hoaDon.MaHoaDon}"
              + (taoMoi ? " (hoá đơn mới tạo)." : ".");

        if (canhBaoGhep.Count > 0)
        {
            loiNhac += "\n\n⚠ Cần xem lại:\n· " + string.Join("\n· ", canhBaoGhep.Take(3))
                + (canhBaoGhep.Count > 3 ? $"\n· … và {canhBaoGhep.Count - 3} chỗ nữa." : string.Empty);
        }

        HopThoai.Bao(this, loiNhac + "\n\nNếu nhập nhầm, bấm Ctrl+Z để hoàn tác.");

        DialogResult = DialogResult.OK;
        Close();
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

    private sealed class DongBang
    {
        public TrangDoc Trang { get; set; } = null!;

        public bool Chon { get; set; }

        /// <summary>Số trang trong lô, đánh lại mỗi lần thêm, bỏ hay đổi tích một trang.</summary>
        public string SoTrang { get; set; } = string.Empty;

        public string TenFile { get; set; } = string.Empty;

        public string Ten { get; set; } = string.Empty;

        /// <summary>"Trang 1" hay "Trang sau" — mẫu giấy nào, xét theo có phần đầu hay không.</summary>
        public string LoaiTrang { get; set; } = string.Empty;

        /// <summary>Tên khách đọc ở phần đầu. Trang nối tiếp không có phần đầu nên để trống.</summary>
        public string TenKhach { get; set; } = string.Empty;

        /// <summary>Tờ bán hay tờ hoàn — để người dùng thấy vì sao ô "NHẬP VÀO" đổi danh sách.</summary>
        public string Loai { get; set; } = string.Empty;

        public int SoDong { get; set; }

        public decimal Tong { get; set; }
    }

    private sealed record MucDich(Guid? Id, string Nhan)
    {
        public override string ToString() => Nhan;
    }
}
