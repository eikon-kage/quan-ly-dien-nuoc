using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Đọc một file hoá đơn Excel rồi nhập các dòng hàng vào phần mềm.
/// Cho chọn bảng nào trong file, ngày lấy hàng và nhập vào hoá đơn nào.
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

    private readonly TextBox _txtFile = Theme.O(560);
    private readonly DateTimePicker _dtNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = Theme.DangNgay, Font = Theme.FontNhap };
    private readonly ComboBox _cboDich = new();
    private readonly Label _lblTomTat = new();
    private readonly Label _lblCanhBao = new();

    private KetQuaDocExcel? _ketQua;
    private bool _dangNap;

    /// <summary>Loại của các bảng đang tích: tờ bán, tờ hoàn, hay tích lẫn cả hai.</summary>
    private LoaiToNhap _loai = LoaiToNhap.KhongCo;

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
        ClientSize = new Size(1240, 840);
        MinimumSize = new Size(1100, 760);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        // Nạp file trước rồi mới dựng ô "NHẬP VÀO": danh sách hoá đơn đích tuỳ vào file này là
        // tờ bán hay tờ hoàn.
        NapFile(duongDanFile);
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
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "NHẬP HOÁ ĐƠN TỪ EXCEL",
                "Chọn bảng cần lấy, đặt ngày lấy hàng rồi nhập vào hoá đơn bán "
                + "hoặc hoá đơn hoàn hàng của khách"),
            0,
            0);

        khung.Controls.Add(TaoThanhChon(), 0, 1);
        khung.Controls.Add(TaoThanNoiDung(), 0, 2);
        khung.Controls.Add(TaoThanhCanhBao(), 0, 3);
        khung.Controls.Add(TaoThanhDuoi(), 0, 4);

        Controls.Add(khung);
    }

    private Control TaoThanhChon()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(14, 8, 14, 8) };

        _txtFile.ReadOnly = true;
        _txtFile.BackColor = Color.White;

        var btnChonFile = Theme.Nut("Chọn file khác", Theme.Chinh, 180, 34);
        btnChonFile.Click += (_, _) => ChonFileKhac();

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

        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("FILE EXCEL", _txtFile, 460));
        hang.Controls.Add(Theme.Truong(" ", btnChonFile, 180));
        hang.Controls.Add(Theme.Truong("NGÀY LẤY HÀNG CHO CÁC DÒNG", _dtNgay, 220));
        hang.Controls.Add(Theme.Truong("NHẬP VÀO", _cboDich, 300));

        nen.Controls.Add(hang);
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
            Padding = new Padding(20, 8, 20, 0),
        };
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));
        than.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Danh sách bảng tìm thấy trong file
        var cotTrai = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Nen,
            Margin = new Padding(0, 0, 16, 0),
        };
        cotTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        cotTrai.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cotTrai.Controls.Add(
            new Label
            {
                Text = "CÁC BẢNG TÌM THẤY (tích để lấy)",
                Font = Theme.FontDam,
                ForeColor = Theme.Xam,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            },
            0,
            0);

        Theme.ApDungLuoi(_luoiBang);
        var cotChon = new DataGridViewCheckBoxColumn
        {
            Name = "colChon",
            DataPropertyName = nameof(DongBang.Chon),
            HeaderText = "LẤY",
            FillWeight = 45,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        _luoiBang.Columns.Add(cotChon);
        _luoiBang.Columns.AddRange(
            Theme.Cot(nameof(DongBang.Ten), "BẢNG", 120),
            Theme.Cot(nameof(DongBang.Loai), "LOẠI", 90),
            Theme.Cot(nameof(DongBang.SoDong), "DÒNG", 55, canPhai: true),
            Theme.Cot(nameof(DongBang.Tong), "TIỀN", 105, "#,##0", canPhai: true));
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
        cotTrai.Controls.Add(Theme.Khung(_luoiBang), 0, 1);

        // Xem trước các dòng sẽ nhập
        var cotPhai = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Nen,
        };
        cotPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        cotPhai.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblTomTat.Font = Theme.FontDam;
        _lblTomTat.ForeColor = Theme.Xam;
        _lblTomTat.Dock = DockStyle.Fill;
        _lblTomTat.TextAlign = ContentAlignment.MiddleLeft;
        cotPhai.Controls.Add(_lblTomTat, 0, 0);

        Theme.ApDungLuoi(_luoiXem);
        _luoiXem.ReadOnly = true;
        _luoiXem.Columns.AddRange(
            Theme.Cot(nameof(ChiTietHoaDon.TenHang), "TÊN HÀNG", 260),
            Theme.Cot(nameof(ChiTietHoaDon.DonVi), "ĐƠN VỊ", 80),
            Theme.Cot(nameof(ChiTietHoaDon.SoLuong), "SỐ LƯỢNG", 100, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(ChiTietHoaDon.DonGia), "ĐƠN GIÁ", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(ChiTietHoaDon.ThanhTien), "THÀNH TIỀN", 140, "#,##0", canPhai: true));
        _luoiXem.DataSource = _nguonXem;
        cotPhai.Controls.Add(Theme.Khung(_luoiXem), 0, 1);

        than.Controls.Add(cotTrai, 0, 0);
        than.Controls.Add(cotPhai, 1, 0);
        return than;
    }

    private Control TaoThanhCanhBao()
    {
        _lblCanhBao.Dock = DockStyle.Fill;
        _lblCanhBao.Font = Theme.FontPhu;
        _lblCanhBao.ForeColor = Theme.Cam;
        _lblCanhBao.TextAlign = ContentAlignment.MiddleLeft;
        _lblCanhBao.Padding = new Padding(22, 0, 22, 0);

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen };
        nen.Controls.Add(_lblCanhBao);
        return nen;
    }

    private Control TaoThanhDuoi()
    {
        var btnNhap = Theme.Nut("NHẬP VÀO HOÁ ĐƠN", Theme.Xanh, 260, 52);
        btnNhap.Click += (_, _) => Nhap();

        var btnHuy = Theme.NutPhu("Huỷ", 140, 52);
        btnHuy.Click += (_, _) => Close();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnNhap);
        trai.Controls.Add(btnHuy);

        var ghiChu = new Label
        {
            Dock = DockStyle.Right,
            Width = 520,
            Font = Theme.FontPhu,
            ForeColor = Theme.Xam,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "Các dòng nhập vào vẫn sửa được như bình thường · Ctrl+Z để hoàn tác",
        };

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 12, 20, 10) };
        nen.Controls.Add(trai);
        nen.Controls.Add(ghiChu);
        return nen;
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

    private void ChonFileKhac()
    {
        using var hopThoai = new OpenFileDialog
        {
            Title = "Chọn file hoá đơn Excel",
            Filter = "File Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Tất cả các file (*.*)|*.*",
        };

        if (hopThoai.ShowDialog(this) == DialogResult.OK)
        {
            NapFile(hopThoai.FileName);
        }
    }

    private void NapFile(string duongDan)
    {
        _txtFile.Text = duongDan;

        try
        {
            _ketQua = DocHoaDon.Doc(duongDan, _dtNgay.Value.Date);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, $"Không đọc được file:\n{duongDan}\n\n{ex.Message}");
            _ketQua = null;
        }

        _dangNap = true;
        _nguonBang.RaiseListChangedEvents = false;
        _nguonBang.Clear();

        if (_ketQua is not null)
        {
            foreach (var trang in _ketQua.Trang)
            {
                _nguonBang.Add(new DongBang
                {
                    Trang = trang,
                    Chon = true,
                    Ten = trang.TenSheet,
                    Loai = trang.LaHoanHang ? "Hoàn hàng" : "Bán hàng",
                    SoDong = trang.Dong.Count,
                    Tong = trang.TongTien,
                });
            }
        }

        _nguonBang.RaiseListChangedEvents = true;
        _nguonBang.ResetBindings();

        // Ngày ghi trên hoá đơn dùng làm gợi ý cho ngày lấy hàng.
        if (_ketQua?.NgayTrenHoaDon is { } ngay)
        {
            _dtNgay.Value = ngay;
        }

        _dangNap = false;
        CapNhatXemTruoc();

        if (_ketQua is not null && _ketQua.Trang.Count == 0)
        {
            HopThoai.CanhBao(
                this,
                "Không tìm thấy bảng hàng nào trong file này.\n\n" +
                "File cần có một dòng tiêu đề gồm TÊN HÀNG, ĐVT, SỐ LƯỢNG, ĐƠN GIÁ, THÀNH TIỀN.");
        }
    }

    private void CapNhatXemTruoc()
    {
        var ngay = _dtNgay.Value.Date;
        var dangTich = _nguonBang.Where(b => b.Chon).ToList();

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

        _nguonXem.RaiseListChangedEvents = false;
        _nguonXem.Clear();

        var canhBao = new List<string>();
        foreach (var bang in dangTich)
        {
            foreach (var dong in bang.Trang.Dong)
            {
                dong.Ngay = ngay;
                _nguonXem.Add(dong);
            }

            canhBao.AddRange(bang.Trang.CanhBao);
        }

        _nguonXem.RaiseListChangedEvents = true;
        _nguonXem.ResetBindings();

        // Dòng hoàn mang số lượng âm nên tổng của tờ hoàn là số âm; nói "hoàn lại 90.000đ"
        // dễ đọc hơn là bày ra "-90.000đ".
        var tong = _nguonXem.Sum(d => d.ThanhTien);
        _lblTomTat.Text = _loai.LaHoanHang
            ? $"SẼ NHẬP {_nguonXem.Count} DÒNG HOÀN · HOÀN LẠI {So.Tien(-tong)} (TRỪ VÀO NỢ)"
            : $"SẼ NHẬP {_nguonXem.Count} DÒNG · TỔNG {So.Tien(tong)}";

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

        _lblCanhBao.ForeColor = Theme.Cam;
        _lblCanhBao.Text = canhBao.Count == 0
            ? string.Empty
            : $"⚠ {canhBao.Count} dòng cần xem lại: {string.Join("  ·  ", canhBao.Take(2))}"
              + (canhBao.Count > 2 ? "  ·  ..." : string.Empty);
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

        if (_nguonXem.Count == 0)
        {
            HopThoai.CanhBao(this, "Chưa có dòng nào để nhập. Hãy tích chọn ít nhất một bảng.");
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
        var dongDoc = _nguonXem.Select(d => new ChiTietHoaDon
        {
            Ngay = ngay,
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
            NgayMo = ngay,

            // Ghi chú của tờ hoàn in ra giấy thành lý do hoàn, nên lấy đúng lý do đọc được
            // trong file chứ không ghi "nhập từ file nào" lên tờ đưa khách.
            GhiChu = laToHoan
                ? LyDoTrenGiay()
                : "Nhập từ " + Path.GetFileName(_txtFile.Text),
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

        public string Ten { get; set; } = string.Empty;

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
