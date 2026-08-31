using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Nhập <b>một</b> khách hàng từ file: một tờ hoá đơn của cửa hàng là của đúng một khách — tên
/// khách ghi ở đầu trang 1, các dòng hàng nằm ở cả tờ — nên nhập một tờ là vào sổ một khách
/// kèm hoá đơn đầu tiên của khách đó.
/// <para>
/// Mẫu giấy để trang đầu và các trang sau ở hai file riêng, nên màn hình này gom nhiều file
/// thành một lô: thêm file trang 1 trước (mẫu <c>trang-1.xls</c>), rồi thêm tiếp từng file
/// trang sau (mẫu <c>trang-sau.xls</c>). Thứ tự thêm vào là thứ tự trang.
/// </para>
/// <para>
/// Tên và địa chỉ đọc được vẫn sửa được ngay trên màn hình trước khi ghi vào sổ: chữ trên tờ
/// giấy hay viết tắt, mà tên khách là chỗ cả sổ công nợ về sau dựa vào.
/// </para>
/// </summary>
public sealed class NhapKhachForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly IReadOnlyList<KhachHang> _khachDaCo;
    private readonly int _nam;

    private readonly DataGridView _luoiBang = new();
    private readonly BindingList<DongBang> _nguonBang = new();
    private readonly DataGridView _luoiXem = new();
    private readonly BindingList<ChiTietHoaDon> _nguonXem = new();

    private readonly TextBox _txtFile = Theme.O(330);
    private readonly TextBox _txtTen = Theme.O(260);
    private readonly TextBox _txtDienThoai = Theme.O(140);
    private readonly TextBox _txtDiaChi = Theme.O(300);
    private readonly TextBox _txtGhiChu = Theme.O(220);
    private readonly ComboBox _cboNam = new();
    private readonly DateTimePicker _dtNgay = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = Theme.DangNgay,
        Font = Theme.FontNhap,
    };

    // Giữ tham chiếu: ToolTip không được control nào giữ hộ, bị dọn rác là mất lời mách.
    private readonly ToolTip _mach = new() { InitialDelay = 250, AutoPopDelay = 8000 };

    // Hai nhãn này chứa **câu**, không phải nhãn ngắn: cho tự xuống dòng và tự cao theo chữ,
    // chứ nhét câu vào một dòng cao cứng là cắt mất đuôi ngay ở cỡ chữ 125%.
    private readonly Label _lblTomTat = Theme.NhanDaiDong(string.Empty, Theme.FontDam, Theme.Xam);
    private readonly Label _lblCanhBao = Theme.NhanDaiDong(string.Empty, Theme.FontPhu, Theme.Cam);
    private readonly Button _btnNhap = Theme.Nut("NHẬP KHÁCH VÀ HOÁ ĐƠN", Theme.Xanh, 290, 52, noTheoChu: true);
    private readonly Button _btnXemCaLo = Theme.NutPhu("Xem cả lô", 150, 30, noTheoChu: true);

    /// <summary>
    /// Các dòng hàng sẽ ghi vào sổ: dòng của <b>mọi trang đang tích</b>, theo thứ tự lô. Giữ
    /// riêng khỏi bảng xem trước bên phải vì bảng đó chỉ bày một file — nhập theo bảng ấy thì
    /// bấm Nhập lúc đang xem một file là mất hàng của các file còn lại.
    /// </summary>
    private readonly List<ChiTietHoaDon> _dongSeNhap = new();

    private bool _dangNap;

    /// <summary>
    /// Bảng bên phải đang bày cả lô, hay chỉ file đang chọn bên trái. Mặc định là chỉ một file:
    /// lô gom nhiều file thì đổ chung vào một bảng dài, nhìn không ra dòng nào của file nào.
    /// </summary>
    private bool _xemCaLo;

    /// <summary>Xét lô đang tích: tên khách trên giấy, khách cũ trùng tên, chỗ chặn nếu có.</summary>
    private XetToKhach _xet = XetToKhach.KhongCo;

    /// <summary>
    /// Người dùng đã tự tay sửa ô tên, nên thêm trang mới không được đè lên: sửa tên viết tắt
    /// trên giấy thành tên đầy đủ rồi thêm nốt trang sau là mất công sửa.
    /// </summary>
    private bool _tuTayGoTen;

    private bool _tuTayGoDiaChi;

    public NhapKhachForm(IReadOnlyList<KhachHang> khachDaCo, int nam, string? duongDanFile = null)
    {
        _khachDaCo = khachDaCo;
        _nam = nam;

        Text = "Nhập một khách hàng từ file";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1400, 860);
        MinimumSize = new Size(1240, 780);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        if (!string.IsNullOrWhiteSpace(duongDanFile))
        {
            ThemTrang(duongDanFile);
        }
        else
        {
            CapNhatXemTruoc();
        }
    }

    /// <summary>Khách sẽ thêm vào sổ, chỉ có sau khi bấm Nhập.</summary>
    public KhachHang? KhachMoi { get; private set; }

    /// <summary>Hoá đơn đầu tiên của khách, dựng từ các dòng hàng trên tờ giấy.</summary>
    public HoaDon? HoaDonMoi { get; private set; }

    /// <summary>Năm người dùng chọn cho tờ giấy này.</summary>
    private int NamChon => _cboNam.SelectedItem is int nam ? nam : _nam;

    private string TenDaGo => _txtTen.Text.Trim();

    private void TaoGiaoDien()
    {
        /*
          Mọi dòng có chữ trong đó đều `AutoSize`, chỉ hai bảng ăn phần còn lại. Sáu dòng trước
          đây đặt cứng 92 / 96 / 96 / — / 66 / 84 px: vừa khít ở cỡ hiển thị 100%, còn máy đặt
          125% thì chữ to lên mà ô vẫn thế nên phụ đề bị cắt mất nửa dưới và dải cảnh báo mất
          hẳn dòng thứ hai. Xem "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        */
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Nen,
        };
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Phụ đề gọn lại còn một câu: câu cũ dài 137 ký tự, ở cỡ chữ to là tràn quá bề ngang
        // cửa sổ rồi cụt đuôi. Cách thêm trang đã nói ngay ở nhãn của hàng nút bên dưới.
        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "NHẬP MỘT KHÁCH HÀNG TỪ FILE",
                "Một tờ hoá đơn là của một khách  ·  thêm file trang 1 trước, rồi các trang sau",
                tuCao: true),
            0,
            0);

        khung.Controls.Add(TaoThanhChon(), 0, 1);
        khung.Controls.Add(TaoThanhKhach(), 0, 2);
        khung.Controls.Add(TaoThanNoiDung(), 0, 3);
        khung.Controls.Add(TaoThanhCanhBao(), 0, 4);
        khung.Controls.Add(TaoThanhDuoi(), 0, 5);

        Controls.Add(khung);
    }

    /// <summary>
    /// Hàng chọn file. Ba cái nút không nằm trong <see cref="Theme.Truong"/> nữa: nút ở đó bị
    /// khoá cứng bề ngang nên chữ dài hơn nút là cắt ("Bỏ trang này" còn thấy "Bỏ trang"). Nay
    /// chúng ngồi riêng một nhóm <c>AutoSize</c> để nở theo chữ, lùi xuống đúng bằng chỗ nhãn
    /// của mấy ô bên cạnh nên vẫn ngang hàng.
    /// </summary>
    private Control TaoThanhChon()
    {
        _txtFile.ReadOnly = true;
        _txtFile.BackColor = Color.White;
        _txtFile.TabStop = false;
        _txtFile.Text = "(chưa có trang nào)";

        var btnThemTrang = Theme.Nut("+ Thêm trang", Theme.Chinh, 168, 34, noTheoChu: true);
        btnThemTrang.Click += (_, _) => ChonThemTrang();

        var btnBoTrang = Theme.NutPhu("Bỏ trang", 132, 34, noTheoChu: true);
        btnBoTrang.Click += (_, _) => BoTrangDangChon();

        // Chưa có tờ giấy nào trên máy thì tải hai file mẫu về điền — chính mẫu giấy cửa hàng
        // đang dùng, khỏi phải đoán phần mềm chờ file kiểu gì.
        var btnMau = Theme.NutPhu("Tải file mẫu", 154, 34, noTheoChu: true);
        btnMau.ForeColor = Theme.Chinh;
        btnMau.Click += (_, _) => TaiFileMau();

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

        // Nhãn "NGÀY LẤY HÀNG CHO CÁC DÒNG" dài gấp rưỡi ô của nó nên bị cắt còn "NGÀY LẤY
        // HÀNG CHO ...". Nhãn nói gọn, còn câu đầy đủ để vào lời mách khi trỏ chuột vào ô.
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
        nhomNut.Controls.Add(btnMau);

        return Theme.HangO(
            Theme.ChinhNhat,
            Theme.Truong("CÁC FILE TRONG LÔ", _txtFile, 320),
            nhomNut,
            Theme.Truong("NĂM CỦA TỜ", _cboNam, 124),
            Theme.Truong("NGÀY LẤY HÀNG", _dtNgay, 200));
    }

    /// <summary>
    /// Hàng ô thông tin khách. Điền sẵn theo phần đầu trang 1 nhưng vẫn sửa được: chữ trên giấy
    /// hay viết tắt, mà tên khách là chỗ cả sổ công nợ về sau dựa vào.
    /// </summary>
    private Control TaoThanhKhach()
    {
        _txtTen.Font = Theme.FontNhap;
        _txtTen.TextChanged += (_, _) =>
        {
            if (_dangNap)
            {
                return;
            }

            _tuTayGoTen = true;
            CapNhatCanhBao();
        };

        _txtDiaChi.TextChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                _tuTayGoDiaChi = true;
            }
        };

        // "TÊN KHÁCH HÀNG (đọc ở trang 1)" dài hơn ô 260px nên bị cắt còn "TÊN KHÁCH HÀNG (đọc
        // ở tr...". Nhãn còn hai chữ chính, phần trong ngoặc để vào lời mách.
        _mach.SetToolTip(_txtTen, "Đọc sẵn ở đầu trang 1, sửa lại được trước khi ghi vào sổ.");

        return Theme.HangO(
            Theme.Trang,
            Theme.Truong("TÊN KHÁCH HÀNG", _txtTen, 280),
            Theme.Truong("ĐIỆN THOẠI", _txtDienThoai, 150),
            Theme.Truong("ĐỊA CHỈ", _txtDiaChi, 300),
            Theme.Truong("GHI CHÚ", _txtGhiChu, 220));
    }

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
        // Chia theo phần trăm chứ không cột trái cứng 600px: cỡ chữ to lên thì cả hai bảng
        // cùng rộng thêm theo cửa sổ, chứ không phải một bên cố định còn chữ thì nở ra.
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

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
            Theme.NhanDaiDong(
                "CÁC TRANG TRONG LÔ  ·  tích để lấy",
                Theme.FontDam,
                Theme.Xam),
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

        // Sáu cột chứ không phải tám: tám cột trong khoảng hẹp này là cột nào cũng còn vài chục
        // điểm ảnh, đọc ra "to1.x...", "m...", "T..." — nhìn thì có chữ mà chẳng biết chữ gì.
        // Tên bảng trong file và tên khách đọc trên trang bỏ khỏi bảng, đưa vào lời mách của
        // dòng: tên khách đã nằm sẵn ở ô TÊN KHÁCH HÀNG ngay phía trên rồi.
        // FILE đứng ngay sau ô tích: tích là tích lấy file ấy hay không, hai thứ phải nhìn thấy
        // cùng lúc.
        _luoiBang.Columns.AddRange(
            Theme.Cot(nameof(DongBang.TenFile), "FILE", 200, toiThieu: 120),
            Theme.Cot(nameof(DongBang.SoTrang), "TRANG", 60, canPhai: true),
            Theme.Cot(nameof(DongBang.LoaiTrang), "MẪU", 95, toiThieu: 92),
            Theme.Cot(nameof(DongBang.SoDong), "DÒNG", 70, canPhai: true),
            Theme.Cot(nameof(DongBang.Tong), "TIỀN", 115, "#,##0", canPhai: true, toiThieu: 100));

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
    /// Dải nhắc trước khi ghi vào sổ. Mỗi lời nhắc một dòng riêng và dải <b>tự cao theo số
    /// dòng</b>: trước đây mấy lời nhắc nối nhau bằng dấu chấm giữa thành một dải chữ dài, nhét
    /// vào ô cao cứng 66px, nên từ lời nhắc thứ hai trở đi là bị cắt mất — đúng chỗ nói hàng
    /// sắp vào nhầm sổ ai.
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
        _btnNhap.Click += (_, _) => Nhap();

        var btnHuy = Theme.NutPhu("Huỷ", 140, 52, noTheoChu: true);
        btnHuy.Click += (_, _) => DialogResult = DialogResult.Cancel;

        CancelButton = btnHuy;
        return Theme.ThanhDuoi(
            Theme.GhiChuPhai("Khách và hoá đơn vào sổ cùng một lượt  ·  Ctrl+Z hoàn tác cả hai"),
            _btnNhap,
            btnHuy);
    }

    // ---------------- Lô trang ----------------

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

    // ---------------- Xem trước ----------------

    private void CapNhatXemTruoc()
    {
        var ngay = _dtNgay.Value.Date;
        DanhSoLaiTrang();
        var dangTich = _nguonBang.Where(b => b.Chon).ToList();

        // Thứ tự trang xét theo đúng nhóm đang tích: bỏ tích trang 1 là lô mất tên khách, tích
        // thêm trang 1 của tờ khác là hai tờ dồn vào một hoá đơn.
        _xet = NhapKhachTuTo.Xet(dangTich.Select(b => b.Trang), _khachDaCo);

        // Tên và địa chỉ trên giấy điền sẵn vào ô, trừ khi người dùng đã tự tay sửa.
        _dangNap = true;
        if (!_tuTayGoTen)
        {
            _txtTen.Text = _xet.TenTrenGiay ?? string.Empty;
        }

        if (!_tuTayGoDiaChi)
        {
            _txtDiaChi.Text = _xet.DiaChiTrenGiay ?? string.Empty;
        }

        _dangNap = false;

        // Chấm ngày cho dòng của **mọi** trang trong lô, kể cả trang chưa tích: bảng bên phải
        // bày được cả file chưa tích để xem trước rồi mới quyết định lấy hay không.
        foreach (var bang in _nguonBang)
        {
            ChamNgayChoDong(bang.Trang, ngay);
        }

        _dongSeNhap.Clear();
        _dongSeNhap.AddRange(dangTich.SelectMany(b => b.Trang.Dong));

        BayLenBangXem(dangTich.Count);
        CapNhatCanhBao();
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

        var soNgay = _dongSeNhap.Select(d => d.Ngay.Date).Distinct().Count();
        var keTrang = soTrangTich > 1 ? $"{soTrangTich} TRANG · " : string.Empty;
        var keNgay = soNgay > 1 ? $" · {soNgay} NGÀY" : string.Empty;
        var caLo = _dongSeNhap.Count == 0
            ? "CHƯA TÍCH TRANG NÀO — SẼ KHÔNG NHẬP DÒNG HÀNG NÀO"
            : $"{keTrang}SẼ NHẬP 1 KHÁCH · {_dongSeNhap.Count} DÒNG HÀNG{keNgay} · "
              + $"TỔNG {So.Tien(_dongSeNhap.Sum(d => d.ThanhTien))}";

        if (_nguonBang.Count == 0)
        {
            _lblTomTat.Text =
                "Chưa có trang nào trong lô. Bấm \"+ Thêm trang...\" và chọn file trang 1 của tờ hoá đơn.";
            return;
        }

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
    /// Dải cảnh báo và nút Nhập. Tách riêng khỏi <see cref="CapNhatXemTruoc"/> để gõ lại ô tên
    /// là chấm lại ngay chuyện trùng khách cũ, không phải đọc lại cả lô.
    /// </summary>
    private void CapNhatCanhBao()
    {
        var ten = TenDaGo;
        _btnNhap.Enabled = _dongSeNhap.Count > 0 && ten.Length > 0 && _xet.Chan is null;

        if (_xet.Chan is { } chan)
        {
            _lblCanhBao.ForeColor = Theme.Do;
            _lblCanhBao.Text = "⚠ " + chan;
            return;
        }

        // Mấy câu nhắc này không chặn nhập, nhưng phải nói ra: sai chỗ nào thì cũng là hàng vào
        // sổ của người khác, hoặc vào năm khác, mà trên sổ không còn dấu vết nào để dò lại.
        var nhac = new List<string>();
        if (ten.Length > 0 && BaoCao.KiemTra.KhachTrungTen(_khachDaCo, ten) is { } cu)
        {
            nhac.Add($"Trong sổ đã có khách \"{cu.Ten}\". Nhập nữa là một người thành hai khách, "
                + "công nợ bị chia đôi.");
        }
        else if (_xet.Nhac is { } nhacLo)
        {
            nhac.Add(nhacLo);
        }

        if (ten.Length > 0 && !NhapKhachTuTo.GiongTenKhach(ten))
        {
            nhac.Add($"\"{ten}\" trông không giống tên khách — xem lại trước khi ghi vào sổ.");
        }

        if (LechNam() is { } lechNam)
        {
            nhac.Add(lechNam);
        }

        var canhBaoDong = _nguonBang.Where(b => b.Chon).SelectMany(b => b.Trang.CanhBao).ToList();
        if (canhBaoDong.Count > 0)
        {
            nhac.Add($"{canhBaoDong.Count} dòng cần xem lại: {string.Join("  ·  ", canhBaoDong.Take(2))}"
                + (canhBaoDong.Count > 2 ? "  ·  ..." : string.Empty));
        }

        // Mỗi lời nhắc một dòng: nối nhau bằng dấu chấm giữa thì ba lời nhắc thành một dải
        // chữ dài không ai đọc hết, mà dòng nào cũng quan trọng như nhau.
        _lblCanhBao.ForeColor = Theme.Cam;
        _lblCanhBao.Text = nhac.Count == 0 ? string.Empty : "⚠ " + string.Join("\n⚠ ", nhac);
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

    // ---------------- Ghi vào sổ ----------------

    private void Nhap()
    {
        if (_dongSeNhap.Count == 0)
        {
            HopThoai.CanhBao(this, "Chưa có dòng nào để nhập. Hãy tích chọn ít nhất một trang.");
            return;
        }

        if (_xet.Chan is { } chan)
        {
            HopThoai.CanhBao(this, chan);
            return;
        }

        var ten = TenDaGo;
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(
                this,
                "Chưa có tên khách hàng.\n\nTên đọc ở phần đầu trang 1; trang 1 để trống chỗ đó "
                + "thì gõ tay vào ô TÊN KHÁCH HÀNG.");
            _txtTen.Focus();
            return;
        }

        if (BaoCao.KiemTra.KhachTrungTen(_khachDaCo, ten) is { } cu && !HopThoai.Hoi(
            this,
            $"Trong sổ đã có khách \"{cu.Ten}\".\n\nThêm nữa là một người thành hai khách, công "
            + "nợ bị chia đôi. Muốn nhập tờ này vào sổ của khách đã có thì mở Đơn hàng của khách "
            + "rồi bấm \"Nhập từ Excel\".\n\nVẫn thêm khách mới?"))
        {
            return;
        }

        // Lấy theo danh sách của cả lô chứ không theo bảng bên phải: bảng ấy chỉ đang bày một
        // file. Ngày của từng dòng cũng đã chấm sẵn ở đó — dòng có mốc ngày trên giấy giữ ngày
        // của nó, dòng không có mới lấy ngày chung — copy lại đây là mất mốc.
        var dong = _dongSeNhap.Select(d => new ChiTietHoaDon
        {
            Ngay = d.Ngay.Date,
            TenHang = d.TenHang,
            DonVi = d.DonVi,
            DonGia = d.DonGia,
            SoLuong = d.SoLuong,
            VatTuId = _kho.TimVatTuTheoTen(d.TenHang)?.Id,
        }).ToList();

        var khach = new KhachHang
        {
            Ten = ten,
            DienThoai = _txtDienThoai.Text.Trim(),
            DiaChi = _txtDiaChi.Text.Trim(),
            GhiChu = _txtGhiChu.Text.Trim(),
            NgayTao = DateTime.Today,
        };

        // Hoá đơn thuộc năm của tờ giấy, không phải năm sổ đang mở: ngày của từng dòng cũng đã
        // ghép theo năm ấy, để hai chỗ lệch nhau thì mở sổ năm nay không thấy hoá đơn đâu.
        var hoaDon = new HoaDon
        {
            KhachHangId = khach.Id,
            Loai = LoaiHoaDon.Ban,
            Nam = NamChon,
            MaHoaDon = _kho.TaoMaHoaDon(khach.Id, NamChon),
            NgayMo = dong.Min(d => d.Ngay),
            GhiChu = "Nhập từ " + TenFileTrongLo(),
        };
        hoaDon.ChiTiet.AddRange(dong);

        KhachMoi = khach;
        HoaDonMoi = hoaDon;
        DialogResult = DialogResult.OK;
    }

    // ---------------- File mẫu ----------------

    private void TaiFileMau()
    {
        using var hopThoai = new FolderBrowserDialog
        {
            Description = "Chọn thư mục để lưu hai file mẫu hoá đơn",
            UseDescriptionForTitle = true,
        };

        if (hopThoai.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string trang1;
        string trangSau;
        try
        {
            (trang1, trangSau) = NhapKhachTuTo.XuatFileMau(hopThoai.SelectedPath);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không lưu được file mẫu:\n" + ex.Message);
            return;
        }

        if (HopThoai.Hoi(
                this,
                $"Đã lưu hai file mẫu vào:\n{hopThoai.SelectedPath}\n\n" +
                $"· {Path.GetFileName(trang1)} — trang đầu: điền tên khách ở đầu tờ, rồi điền "
                + "từng dòng hàng vào bảng (cột đầu là số thứ tự).\n" +
                $"· {Path.GetFileName(trangSau)} — hàng nhiều quá một trang thì chép file này "
                + "ra thành trang 2, trang 3…\n\n" +
                "Điền xong quay lại đây, bấm \"+ Thêm trang...\" và thêm trang 1 trước.\n\n" +
                "Mở thư mục lên xem luôn không?"))
        {
            MoFile(hopThoai.SelectedPath);
        }
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
            HopThoai.CanhBao(this, "Không mở được thư mục:\n" + ex.Message);
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

        public int SoDong { get; set; }

        public decimal Tong { get; set; }
    }
}
