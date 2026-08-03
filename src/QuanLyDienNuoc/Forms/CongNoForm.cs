using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Sổ công nợ của cả cửa hàng: ai đang nợ, nợ bao nhiêu và nợ đã bao lâu.
/// Xếp sẵn theo nợ lâu nhất để biết cần gọi ai trước.
/// </summary>
public sealed class CongNoForm : Form
{
    private const string TatCaCacNam = "Tất cả các năm";

    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly BindingList<DongLuoi> _nguon = new();

    private readonly ComboBox _cboNam = new();
    private readonly TextBox _txtTim = Theme.O(300);
    private readonly NumericUpDown _numNgay = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTongKet = new();
    private readonly Label _lblTrangThai = new();

    private bool _dangNap;

    public CongNoForm(int? namBanDau = null)
    {
        Text = "Sổ công nợ";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1320, 780);
        MinimumSize = new Size(1100, 640);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        NapNam(namBanDau);
        Nap();
    }

    private int? NamDangChon => _cboNam.SelectedItem is int nam ? nam : null;

    private DongCongNo? DangChon => (_luoi.CurrentRow?.DataBoundItem as DongLuoi)?.Nguon;

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

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "SỔ CÔNG NỢ",
                "Xếp theo nợ lâu nhất. \"Số ngày nợ\" tính từ lần lấy hàng hoặc trả tiền gần nhất."),
            0,
            0);
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
                Nap();
            }
        };

        _txtTim.TextChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                Nap();
            }
        };

        _numNgay.Minimum = 0;
        _numNgay.Maximum = 3650;
        _numNgay.Increment = 15;
        _numNgay.Font = Theme.FontNhap;
        _numNgay.Value = 0;
        _numNgay.ValueChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                Nap();
            }
        };

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(Theme.Truong("NĂM", _cboNam, 190));
        trai.Controls.Add(Theme.Truong("TÌM KHÁCH HÀNG", _txtTim, 320));
        trai.Controls.Add(Theme.Truong("CHỈ HIỆN NỢ QUÁ (NGÀY)", _numNgay, 200));

        var btnQuaHan = Theme.NutPhu($"Nợ quá {_kho.CaiDat.SoNgayNhacNo} ngày", 230, 42);
        btnQuaHan.Margin = new Padding(0, 22, 10, 0);
        btnQuaHan.Click += (_, _) => _numNgay.Value = Math.Min(_numNgay.Maximum, _kho.CaiDat.SoNgayNhacNo);

        var btnTatCa = Theme.NutPhu("Xem tất cả", 160, 42);
        btnTatCa.Margin = new Padding(0, 22, 10, 0);
        btnTatCa.Click += (_, _) => _numNgay.Value = 0;

        var phai = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
        };
        phai.Controls.Add(btnTatCa);
        phai.Controls.Add(btnQuaHan);

        nen.Controls.Add(trai);
        nen.Controls.Add(phai);
        return nen;
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongLuoi.Ten), "KHÁCH HÀNG", 190),
            Theme.Cot(nameof(DongLuoi.DienThoai), "ĐIỆN THOẠI", 110),
            Theme.Cot(nameof(DongLuoi.SoHoaDonNo), "SỐ HĐ NỢ", 80, canPhai: true),
            Theme.Cot(nameof(DongLuoi.TongMua), "TỔNG MUA", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongLuoi.DaTra), "ĐÃ TRẢ", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongLuoi.ConNo), "CÒN NỢ", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongLuoi.PhatSinhCuoi), "PHÁT SINH CUỐI", 120, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongLuoi.TraCuoi), "TRẢ LẦN CUỐI", 120, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongLuoi.SoNgayNo), "SỐ NGÀY NỢ", 100, canPhai: true));

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

        var btnMo = Theme.Nut("MỞ ĐƠN HÀNG", Theme.Chinh, 220, 52);
        btnMo.Click += (_, _) => MoDonHang();

        var btnThuTien = Theme.Nut("THU TIỀN", Theme.Xanh, 180, 52);
        btnThuTien.Click += (_, _) => ThuTienCuaKhach();

        var btnNhac = Theme.Nut("SOẠN TIN NHẮC NỢ", Theme.Cam, 260, 52);
        btnNhac.Click += (_, _) => SoanTinNhac();

        var btnXuat = Theme.NutPhu("Xuất Excel", 170, 52);
        btnXuat.Click += (_, _) => XuatExcel();

        var btnDong = Theme.NutPhu("Đóng (Esc)", 150, 52);
        btnDong.Click += (_, _) => Close();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnMo);
        trai.Controls.Add(btnThuTien);
        trai.Controls.Add(btnNhac);
        trai.Controls.Add(btnXuat);
        trai.Controls.Add(btnDong);

        _lblTongKet.Dock = DockStyle.Right;
        _lblTongKet.TextAlign = ContentAlignment.MiddleRight;
        _lblTongKet.Font = Theme.FontSo;
        _lblTongKet.ForeColor = Theme.Do;
        _lblTongKet.AutoSize = false;
        _lblTongKet.Width = 560;

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
        _lblTrangThai.Text = "Bấm đúp vào một dòng để mở đơn hàng của khách đó.";

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        return nen;
    }

    // ---------------- Nạp dữ liệu ----------------

    private void NapNam(int? namBanDau)
    {
        _dangNap = true;
        _cboNam.Items.Clear();
        _cboNam.Items.Add(TatCaCacNam);
        foreach (var nam in _kho.DanhSachNam())
        {
            _cboNam.Items.Add(nam);
        }

        var viTri = namBanDau is { } n ? _cboNam.Items.IndexOf(n) : 0;
        _cboNam.SelectedIndex = viTri >= 0 ? viTri : 0;
        _dangNap = false;
    }

    private void Nap()
    {
        var dangChon = DangChon?.Khach.Id;
        var tuKhoa = _txtTim.Text;
        var toiThieu = (int)_numNgay.Value;

        var dong = CongNo.Tinh(_kho.DuLieu, NamDangChon, DateTime.Today)
            .Where(d => d.SoNgayNo >= toiThieu)
            .Where(d => ChuViet.Chua(d.Khach.Ten, tuKhoa)
                        || ChuViet.Chua(d.Khach.DienThoai, tuKhoa)
                        || ChuViet.Chua(d.Khach.DiaChi, tuKhoa))
            .ToList();

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        foreach (var d in dong)
        {
            _nguon.Add(new DongLuoi
            {
                Nguon = d,
                Ten = d.Khach.Ten,
                DienThoai = d.Khach.DienThoai,
                SoHoaDonNo = d.SoHoaDonNo,
                TongMua = d.TongMua,
                DaTra = d.DaTra,
                ConNo = d.ConNo,
                PhatSinhCuoi = d.PhatSinhCuoi,
                TraCuoi = d.TraCuoi,
                SoNgayNo = d.SoNgayNo,
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            for (var i = 0; i < _luoi.Rows.Count; i++)
            {
                if (_luoi.Rows[i].DataBoundItem is DongLuoi dl && dl.Nguon.Khach.Id == id)
                {
                    _luoi.CurrentCell = _luoi.Rows[i].Cells[0];
                    break;
                }
            }
        }

        var tongNo = dong.Sum(d => d.ConNo);
        var quaHan = dong.Count(d => d.SoNgayNo >= _kho.CaiDat.SoNgayNhacNo);
        _lblTongKet.Text = $"{dong.Count} khách đang nợ   ·   Tổng: {So.Tien(tongNo)}   ·   quá {_kho.CaiDat.SoNgayNhacNo} ngày: {quaHan} khách";
    }

    private void Luoi_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is not { } kieu)
        {
            return;
        }

        if (_luoi.Rows[e.RowIndex].DataBoundItem is not DongLuoi dong)
        {
            return;
        }

        var thuocTinh = _luoi.Columns[e.ColumnIndex].DataPropertyName;

        if (thuocTinh == nameof(DongLuoi.Ten))
        {
            kieu.Font = Theme.FontLuoiDam;
        }
        else if (thuocTinh == nameof(DongLuoi.ConNo))
        {
            kieu.Font = Theme.FontLuoiDam;
            kieu.ForeColor = Theme.Do;
        }
        else if (thuocTinh == nameof(DongLuoi.SoNgayNo))
        {
            kieu.Font = Theme.FontLuoiDam;
            kieu.ForeColor = dong.SoNgayNo switch
            {
                >= 90 => Color.FromArgb(155, 20, 20),
                >= 60 => Theme.Do,
                >= 30 => Theme.Cam,
                _ => Theme.Xam,
            };
        }
        else if (thuocTinh is nameof(DongLuoi.PhatSinhCuoi) or nameof(DongLuoi.TraCuoi) && e.Value is null)
        {
            e.Value = "—";
            e.FormattingApplied = true;
        }
    }

    // ---------------- Thao tác ----------------

    private void MoDonHang()
    {
        if (DangChon is not { } dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng trong danh sách.");
            return;
        }

        var nam = NamDangChon ?? DateTime.Today.Year;
        using var form = new DonHangForm(dong.Khach.Id, nam);
        form.ShowDialog(this);
        Nap();
    }

    private void ThuTienCuaKhach()
    {
        if (DangChon is not { } dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn khách hàng vừa đưa tiền.");
            return;
        }

        using var form = new ThuTienForm(dong.Khach.Id);
        form.ShowDialog(this);
        Nap();
        _lblTrangThai.Text = $"Đã cập nhật tiền của {dong.Khach.Ten}.";
    }

    private void SoanTinNhac()
    {
        if (DangChon is not { } dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn khách hàng cần nhắc nợ.");
            return;
        }

        var hoaDons = _kho.DuLieu.HoaDons
            .Where(h => h.KhachHangId == dong.Khach.Id && (NamDangChon is null || h.Nam == NamDangChon))
            .ToList();

        var noiDung = TinNhacNo.Soan(dong.Khach, hoaDons, DateTime.Today, ThongTinCuaHang.DocTuMau());

        using var form = new VanBanForm(
            "Tin nhắc nợ",
            $"{dong.Khach.Ten} — còn nợ {So.Tien(dong.ConNo)}, đã {dong.SoNgayNo} ngày. Sửa lại lời cho hợp rồi chép đi gửi.",
            noiDung);
        form.ShowDialog(this);
    }

    private void XuatExcel()
    {
        if (_nguon.Count == 0)
        {
            HopThoai.CanhBao(this, "Không có khách nào đang nợ để xuất.");
            return;
        }

        var nam = NamDangChon is { } n ? n.ToString() : "tat-ca-cac-nam";
        using var hopThoai = new SaveFileDialog
        {
            Title = "Xuất sổ công nợ",
            Filter = "File Excel (*.xlsx)|*.xlsx",
            FileName = $"So cong no {nam} - {DateTime.Today:dd-MM-yyyy}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (hopThoai.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            XuatToanBo.Xuat(_kho.DuLieu, hopThoai.FileName, DateTime.Today);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không xuất được file:\n" + ex.Message);
            return;
        }

        _lblTrangThai.Text = $"Đã xuất: {hopThoai.FileName}";
        HopThoai.Bao(this, $"Đã xuất xong:\n{hopThoai.FileName}\n\nFile có sẵn trang \"Công nợ\" cùng toàn bộ số liệu khác.");
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                Close();
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

    /// <summary>Một dòng công nợ trên lưới.</summary>
    private sealed class DongLuoi
    {
        public DongCongNo Nguon { get; set; } = null!;

        public string Ten { get; set; } = string.Empty;

        public string DienThoai { get; set; } = string.Empty;

        public int SoHoaDonNo { get; set; }

        public decimal TongMua { get; set; }

        public decimal DaTra { get; set; }

        public decimal ConNo { get; set; }

        public DateTime? PhatSinhCuoi { get; set; }

        public DateTime? TraCuoi { get; set; }

        public int SoNgayNo { get; set; }
    }
}
