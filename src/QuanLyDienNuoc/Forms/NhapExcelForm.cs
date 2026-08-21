using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Đọc một file hoá đơn Excel rồi nhập các dòng hàng vào phần mềm.
/// Cho chọn bảng nào trong file, ngày lấy hàng và nhập vào hoá đơn nào.
/// </summary>
public sealed class NhapExcelForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;
    private readonly int _nam;

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

    public NhapExcelForm(Guid khachId, int nam, Guid? hoaDonDangChon, string duongDanFile)
    {
        _khachId = khachId;
        _nam = nam;

        Text = "Nhập hoá đơn từ Excel";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1240, 840);
        MinimumSize = new Size(1100, 760);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        NapDich(hoaDonDangChon);
        NapFile(duongDanFile);
    }

    /// <summary>Số dòng hàng đã nhập được sau khi bấm Nhập.</summary>
    public int SoDongDaNhap { get; private set; }

    /// <summary>Hoá đơn đã nhận dữ liệu, để màn hình gọi mở đúng hoá đơn đó.</summary>
    public Guid? HoaDonDaNhap { get; private set; }

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
                "Chọn bảng cần lấy, đặt ngày lấy hàng rồi nhập vào hoá đơn của khách"),
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
            Theme.Cot(nameof(DongBang.Ten), "BẢNG", 130),
            Theme.Cot(nameof(DongBang.SoDong), "DÒNG", 60, canPhai: true),
            Theme.Cot(nameof(DongBang.Tong), "TIỀN", 110, "#,##0", canPhai: true));
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

    private void NapDich(Guid? hoaDonDangChon)
    {
        _dangNap = true;
        _cboDich.Items.Clear();
        _cboDich.Items.Add(new MucDich(null, $"— Tạo hoá đơn mới ({_nam}) —"));

        // Không cho nhập hàng vào tờ hoàn hàng: số hoàn phải khớp với hoá đơn gốc, đổ thêm
        // hàng vào đó là tờ hoàn nói một chuyện khác hẳn hoá đơn nó hoàn cho.
        foreach (var hoaDon in _kho.HoaDonCuaKhach(_khachId, _nam).Where(h => !h.LaHoanHang))
        {
            _cboDich.Items.Add(new MucDich(
                hoaDon.Id,
                $"{hoaDon.MaHoaDon} · mở {hoaDon.NgayMo:dd/MM/yyyy} · {So.Tien(hoaDon.TongTien)}"));
        }

        _cboDich.SelectedIndex = 0;
        if (hoaDonDangChon is { } id)
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

        _dangNap = false;
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

        _nguonXem.RaiseListChangedEvents = false;
        _nguonXem.Clear();

        var canhBao = new List<string>();
        foreach (var bang in _nguonBang.Where(b => b.Chon))
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

        var tong = _nguonXem.Sum(d => d.ThanhTien);
        _lblTomTat.Text = $"SẼ NHẬP {_nguonXem.Count} DÒNG · TỔNG {So.Tien(tong)}";

        _lblCanhBao.Text = canhBao.Count == 0
            ? string.Empty
            : $"⚠ {canhBao.Count} dòng cần xem lại: {string.Join("  ·  ", canhBao.Take(2))}"
              + (canhBao.Count > 2 ? "  ·  ..." : string.Empty);
    }

    private void Nhap()
    {
        if (Khach is not { } khach)
        {
            return;
        }

        if (_nguonXem.Count == 0)
        {
            HopThoai.CanhBao(this, "Chưa có dòng nào để nhập. Hãy tích chọn ít nhất một bảng.");
            return;
        }

        if (_cboDich.SelectedItem is not MucDich dich)
        {
            return;
        }

        var ngay = _dtNgay.Value.Date;
        var dongMoi = _nguonXem.Select(d => new ChiTietHoaDon
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

        if (hoaDon is { LaHoanHang: true })
        {
            HopThoai.CanhBao(this, "Không nhập hàng vào hoá đơn hoàn hàng được. Hãy chọn hoá đơn bán hàng.");
            return;
        }

        hoaDon ??= new HoaDon
        {
            KhachHangId = _khachId,
            Nam = _nam,
            MaHoaDon = _kho.TaoMaHoaDon(_khachId, _nam),
            NgayMo = ngay,
            GhiChu = "Nhập từ " + Path.GetFileName(_txtFile.Text),
        };

        _kho.ThucHien($"Nhập {dongMoi.Count} dòng từ Excel", () =>
        {
            if (taoMoi)
            {
                _kho.DuLieu.HoaDons.Add(hoaDon);
            }

            hoaDon.ChiTiet.AddRange(dongMoi);
        }, phatSuKien: false);

        SoDongDaNhap = dongMoi.Count;
        HoaDonDaNhap = hoaDon.Id;

        HopThoai.Bao(
            this,
            $"Đã nhập {dongMoi.Count} dòng vào hoá đơn {hoaDon.MaHoaDon}"
            + (taoMoi ? " (hoá đơn mới tạo)." : ".")
            + "\n\nNếu nhập nhầm, bấm Ctrl+Z để hoàn tác.");

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

        public int SoDong { get; set; }

        public decimal Tong { get; set; }
    }

    private sealed record MucDich(Guid? Id, string Nhan)
    {
        public override string ToString() => Nhan;
    }
}
