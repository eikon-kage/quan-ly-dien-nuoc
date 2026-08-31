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
    /// <summary>Cả sổ sau khi lọc. Lưới chỉ nhận đúng một trang trong này.</summary>
    private readonly List<DongLuoi> _tatCa = new();

    private readonly BindingList<DongLuoi> _nguon = new();
    private readonly ThanhPhanTrang _phanTrang = new();

    private readonly ComboBox _cboNam = new();
    private readonly TextBox _txtTim = Theme.O(300);
    private readonly NumericUpDown _numNgay = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTongKet = Theme.NhanDaiDong();
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

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
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "SỔ CÔNG NỢ",
                "Xếp theo nợ lâu nhất  ·  \"số ngày nợ\" tính từ lần lấy hàng hay trả tiền gần nhất",
                tuCao: true),
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

        var btnQuaHan = Theme.NutPhu($"Nợ quá {_kho.CaiDat.SoNgayNhacNo} ngày", 230, 42, noTheoChu: true);
        btnQuaHan.Click += (_, _) => _numNgay.Value = Math.Min(_numNgay.Maximum, _kho.CaiDat.SoNgayNhacNo);

        var btnTatCa = Theme.NutPhu("Xem tất cả", 160, 42, noTheoChu: true);
        btnTatCa.Click += (_, _) => _numNgay.Value = 0;

        // Hai nút lọc nhanh ngồi riêng một nhóm `AutoSize` để nở theo chữ, lùi xuống đúng bằng
        // chỗ nhãn của mấy ô bên cạnh nên vẫn ngang hàng.
        var nhomNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 18, 0),
        };
        nhomNut.Controls.Add(btnQuaHan);
        nhomNut.Controls.Add(btnTatCa);

        return Theme.HangO(
            Theme.Nen,
            Theme.Truong("NĂM", _cboNam, 190),
            Theme.Truong("TÌM KHÁCH HÀNG", _txtTim, 320),
            Theme.Truong("NỢ QUÁ (NGÀY)", _numNgay, 200),
            nhomNut);
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongLuoi.Ten), "KHÁCH HÀNG", 190, toiThieu: 140),
            Theme.Cot(nameof(DongLuoi.DienThoai), "ĐIỆN THOẠI", 115, toiThieu: 110),
            Theme.Cot(nameof(DongLuoi.SoHoaDonNo), "SỐ HĐ NỢ", 85, canPhai: true),
            Theme.Cot(nameof(DongLuoi.TongMua), "TỔNG MUA", 130, "#,##0", canPhai: true, toiThieu: 110),
            Theme.Cot(nameof(DongLuoi.DaTra), "ĐÃ TRẢ", 120, "#,##0", canPhai: true, toiThieu: 110),
            Theme.Cot(nameof(DongLuoi.ConNo), "CÒN NỢ", 130, "#,##0", canPhai: true, toiThieu: 110),
            Theme.Cot(nameof(DongLuoi.PhatSinhCuoi), "PHÁT SINH CUỐI", 125, "dd/MM/yyyy", toiThieu: 104),
            Theme.Cot(nameof(DongLuoi.TraCuoi), "TRẢ LẦN CUỐI", 125, "dd/MM/yyyy", toiThieu: 104),
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
        var btnMo = Theme.Nut("MỞ ĐƠN HÀNG", Theme.Chinh, 220, 52, noTheoChu: true);
        btnMo.Click += (_, _) => MoDonHang();

        var btnThuTien = Theme.Nut("THU TIỀN", Theme.Xanh, 180, 52, noTheoChu: true);
        btnThuTien.Click += (_, _) => ThuTienCuaKhach();

        // Soạn tin và xuất Excel không phải việc hằng ngày: gom vào nút ba chấm cho hàng nút
        // dưới bảng còn lại đúng hai việc hay làm là mở đơn và thu tiền.
        var viecKhac = Theme.NutBaCham("Việc khác với khách đang chọn", 52)
            .Viec("Soạn tin nhắc nợ", SoanTinNhac)
            .Ngan()
            .Viec("Xuất sổ công nợ ra Excel", XuatExcel);

        var btnDong = Theme.NutPhu("Đóng", 120, 52, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTongKet.Font = Theme.FontSo;
        _lblTongKet.ForeColor = Theme.Do;
        _lblTongKet.Margin = new Padding(16, 0, 0, 0);

        _phanTrang.Margin = new Padding(0, 4, 12, 0);
        _phanTrang.DoiTrang += (_, _) => HienTrang();

        // Số trang và dòng tổng đứng cùng bên phải, gói chung một nhóm `AutoSize` để cả hai
        // cao đúng bằng chữ của máy này.
        var phai = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
        };
        phai.Controls.Add(_phanTrang);
        phai.Controls.Add(_lblTongKet);

        return Theme.ThanhDuoi(phai, btnMo, btnThuTien, viecKhac.Nut, btnDong);
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Text = "Bấm đúp vào một dòng để mở đơn hàng của khách đó.";
        return Theme.ThanhTrangThai(_lblTrangThai);
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

        _tatCa.Clear();
        foreach (var d in dong)
        {
            _tatCa.Add(new DongLuoi
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

        // Khách đang chọn nằm trang nào thì mở đúng trang ấy.
        _phanTrang.DatTong(_tatCa.Count);
        if (dangChon is { } cu)
        {
            var viTri = _tatCa.FindIndex(d => d.Nguon.Khach.Id == cu);
            if (viTri >= 0)
            {
                _phanTrang.VeTrang(PhanTrang.TrangCuaDong(viTri));
            }
        }

        HienTrang();

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

    /// <summary>Đổ đúng trang đang xem vào lưới.</summary>
    private void HienTrang()
    {
        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        foreach (var dong in _phanTrang.Cat(_tatCa))
        {
            _nguon.Add(dong);
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();
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
        if (_tatCa.Count == 0)
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
