using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Lập hoá đơn hoàn hàng cho một hoá đơn bán: bày ra từng dòng hàng của hoá đơn gốc, gõ số
/// hoàn vào dòng nào thì hoàn dòng đó. Hoá đơn gốc không bị sửa một chữ — tờ hoàn là chứng
/// từ riêng, hoàn cho nó — nên hoá đơn đã in cho khách hay đã chốt vẫn hoàn được.
/// </summary>
public sealed class HoanHangForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _hoaDonGocId;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<DongChon> _nguon = new();

    private readonly DateTimePicker _dtNgay = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = Theme.DangNgay,
        Font = Theme.FontNhap,
    };

    private readonly TextBox _txtLyDo = Theme.O(420);
    private readonly Label _lblTong = new();
    private readonly Label _lblTrangThai = new();

    public HoanHangForm(Guid hoaDonGocId)
    {
        _hoaDonGocId = hoaDonGocId;

        Text = "Hoàn hàng";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1160, 700);
        MinimumSize = new Size(1040, 620);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    /// <summary>Hoá đơn hoàn hàng vừa lập, để màn đơn hàng nhảy sang xem luôn tờ đó.</summary>
    public Guid? HoaDonHoanDaTao { get; private set; }

    private HoaDon? HoaDonGoc => _kho.TimHoaDon(_hoaDonGocId);

    private decimal TongTienHoan => _nguon.Sum(d => d.TienHoan);

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
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var goc = HoaDonGoc;
        var khach = goc is null ? null : _kho.TimKhach(goc.KhachHangId);
        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "HOÀN HÀNG",
                goc is null
                    ? string.Empty
                    : $"Hoàn cho hoá đơn {goc.MaHoaDon} · mở ngày {goc.NgayMo:dd/MM/yyyy}"
                      + (khach is null ? string.Empty : $" · {khach.Ten}")),
            0,
            0);

        khung.Controls.Add(TaoThanhNhap(), 0, 1);

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(TaoLuoi()));
        khung.Controls.Add(vienLuoi, 0, 2);

        var vienDuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 10), BackColor = Theme.Nen };
        vienDuoi.Controls.Add(TaoThanhDuoi());
        khung.Controls.Add(vienDuoi, 0, 3);

        khung.Controls.Add(TaoThanhTrangThai(), 0, 4);
        Controls.Add(khung);
    }

    private Control TaoThanhNhap()
    {
        var nen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ChinhNhat,
            Padding = new Padding(14, 8, 14, 8),
        };

        _dtNgay.Font = Theme.FontNhapTo;
        _dtNgay.CalendarFont = Theme.FontNhapTo;

        // Hoàn hết những gì khách còn giữ là việc hay gặp nhất (khách trả cả lô hàng chưa
        // dùng), nên để hẳn một nút thay vì bắt gõ số vào từng dòng.
        var btnHoanHet = Theme.Nut("ĐIỀN HOÀN HẾT", Theme.Chinh, 200, 40);
        btnHoanHet.Click += (_, _) => DienHoanHet();

        var btnXoaTrang = Theme.NutPhu("Bỏ hết số đã gõ", 180, 40);
        btnXoaTrang.Click += (_, _) => XoaTrangSoHoan();

        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("NGÀY HOÀN", _dtNgay, 180, 40, 12));
        hang.Controls.Add(Theme.Truong("LÝ DO HOÀN", _txtLyDo, 420, 40, 12));
        hang.Controls.Add(Theme.Truong(" ", btnHoanHet, 200, 40, 12));
        hang.Controls.Add(Theme.Truong(" ", btnXoaTrang, 180, 40, 12));

        var mach = new ToolTip { InitialDelay = 250, AutoPopDelay = 10000 };
        mach.SetToolTip(_txtLyDo, "Ví dụ: hàng lỗi, khách lấy thừa, sai chủng loại — sẽ in lên tờ hoàn hàng");

        nen.Controls.Add(hang);
        return nen;
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongChon.Ngay), "NGÀY LẤY", 90, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongChon.TenHang), "TÊN HÀNG", 240),
            Theme.Cot(nameof(DongChon.DonVi), "ĐƠN VỊ", 80),
            Theme.Cot(nameof(DongChon.DonGia), "ĐƠN GIÁ", 110, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongChon.DaMua), "ĐÃ LẤY", 90, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongChon.DaHoan), "ĐÃ HOÀN", 90, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongChon.ConHoanDuoc), "CÒN HOÀN ĐƯỢC", 120, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongChon.SoHoan), "SỐ HOÀN", 110, "#,##0.##", canPhai: true, chiDoc: false),
            Theme.Cot(nameof(DongChon.TienHoan), "TIỀN HOÀN", 140, "#,##0", canPhai: true));

        Theme.ChoPhepGoSo(_luoi, nameof(DongChon.SoHoan));

        _luoi.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is not { } kieu)
            {
                return;
            }

            var cot = _luoi.Columns[e.ColumnIndex].DataPropertyName;
            if (_luoi.Rows[e.RowIndex].DataBoundItem is not DongChon dong)
            {
                return;
            }

            // Ô để gõ tô vàng nhạt như dòng đang gõ dở ở màn đơn hàng — nhìn vào là biết gõ
            // vào cột nào, khỏi lần mò cả bảng.
            if (cot == nameof(DongChon.SoHoan))
            {
                kieu.BackColor = Color.FromArgb(255, 251, 230);
                kieu.SelectionBackColor = Color.FromArgb(250, 236, 190);
                kieu.SelectionForeColor = Theme.Chu;
                kieu.Font = Theme.FontLuoiDam;

                if (e.Value is decimal and 0m)
                {
                    e.Value = string.Empty;
                    e.FormattingApplied = true;
                }
            }

            if (cot == nameof(DongChon.TienHoan))
            {
                kieu.Font = Theme.FontLuoiDam;
                if (dong.SoHoan > 0m)
                {
                    kieu.ForeColor = Theme.Do;
                }
                else if (e.Value is decimal and 0m)
                {
                    e.Value = string.Empty;
                    e.FormattingApplied = true;
                }
            }

            // Món đã hoàn hết thì làm mờ cả dòng: còn bày ra để đối chiếu, nhưng gõ vào đó
            // cũng không hoàn thêm được nữa.
            if (dong.ConHoanDuoc <= 0m && cot != nameof(DongChon.SoHoan))
            {
                kieu.ForeColor = Theme.XamNhat;
            }
        };

        _luoi.CellEndEdit += (_, e) =>
        {
            if (e.RowIndex < 0 || _luoi.Rows[e.RowIndex].DataBoundItem is not DongChon dong)
            {
                return;
            }

            ChinhLaiSoHoan(dong);
            _nguon.ResetItem(e.RowIndex);
            CapNhatTong();
        };

        _luoi.DataSource = _nguon;
        return _luoi;
    }

    private Control TaoThanhDuoi()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(0, 10, 0, 0) };

        var btnTao = Theme.Nut("TẠO HOÁ ĐƠN HOÀN HÀNG", Theme.Cam, 300, 46);
        btnTao.Click += (_, _) => TaoHoaDonHoan();

        var btnDong = Theme.NutPhu("Đóng", 140, 46);
        btnDong.Click += (_, _) => Close();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnTao);
        trai.Controls.Add(btnDong);

        _lblTong.Dock = DockStyle.Right;
        _lblTong.Width = 560;
        _lblTong.Font = Theme.FontSo;
        _lblTong.ForeColor = Theme.Do;
        _lblTong.TextAlign = ContentAlignment.MiddleRight;

        nen.Controls.Add(trai);
        nen.Controls.Add(_lblTong);
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

    private void Nap()
    {
        if (HoaDonGoc is not { } goc)
        {
            // Máy khác vừa xoá hoá đơn gốc. Đừng gọi Close() ở đây: cửa sổ còn chưa hiện xong.
            _lblTrangThai.Text = "Hoá đơn gốc không còn trong sổ nữa nên không hoàn được.";
            return;
        }

        var hoaDonCuaKhach = _kho.HoaDonCuaKhach(goc.KhachHangId);

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        foreach (var dong in HoanHang.DongCoTheHoanCua(hoaDonCuaKhach, goc))
        {
            _nguon.Add(new DongChon(dong));
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        CapNhatTong();

        var conHoan = _nguon.Sum(d => d.ConHoanDuoc);
        var daHoan = HoanHang.TienDaHoan(hoaDonCuaKhach, goc.Id);

        _lblTrangThai.Text = conHoan <= 0m
            ? $"Hoá đơn {goc.MaHoaDon} đã hoàn hết hàng, không còn món nào hoàn được."
            : "Gõ số hoàn vào cột SỐ HOÀN của những dòng khách mang trả về"
              + (daHoan > 0m ? $" — hoá đơn này đã hoàn {So.Tien(daHoan)} ở những lần trước." : ".");
    }

    private void CapNhatTong()
    {
        var soMon = _nguon.Count(d => d.SoHoan > 0m);
        _lblTong.Text = $"{soMon} món  ·  tiền hoàn lại: {So.Tien(TongTienHoan)}";
    }

    /// <summary>
    /// Gõ quá số còn hoàn được thì sửa lại đúng số đó rồi nhắc một câu ở thanh dưới. Không
    /// bật hộp thoại chặn giữa: đang gõ liền tay cả bảng mà cứ phải với chuột đi tắt nó thì mất nhịp.
    /// </summary>
    private void ChinhLaiSoHoan(DongChon dong)
    {
        var goDuoc = dong.SoHoan;
        if (goDuoc < 0m)
        {
            dong.SoHoan = 0m;
            _lblTrangThai.Text = $"\"{dong.TenHang}\": số hoàn gõ số dương, "
                + "phần mềm tự ghi thành hàng trả về khi lập tờ hoàn.";
            return;
        }

        if (goDuoc > dong.ConHoanDuoc)
        {
            dong.SoHoan = dong.ConHoanDuoc;
            _lblTrangThai.Text = dong.ConHoanDuoc <= 0m
                ? $"\"{dong.TenHang}\" đã hoàn hết ở những lần trước rồi."
                : $"\"{dong.TenHang}\" chỉ còn hoàn được {So.Luong(dong.ConHoanDuoc)} "
                  + $"{dong.DonVi} — đã sửa lại đúng số đó.";
        }
    }

    private void DienHoanHet()
    {
        foreach (var dong in _nguon)
        {
            dong.SoHoan = dong.ConHoanDuoc;
        }

        _nguon.ResetBindings();
        CapNhatTong();
        _lblTrangThai.Text = TongTienHoan > 0m
            ? $"Đã điền hoàn hết: {So.Tien(TongTienHoan)}. Sửa lại từng dòng nếu khách chỉ trả một phần."
            : "Hoá đơn này không còn món nào hoàn được.";
    }

    private void XoaTrangSoHoan()
    {
        foreach (var dong in _nguon)
        {
            dong.SoHoan = 0m;
        }

        _nguon.ResetBindings();
        CapNhatTong();
        _lblTrangThai.Text = "Đã bỏ hết số vừa gõ.";
    }

    // ---------------- Lập hoá đơn hoàn hàng ----------------

    private void TaoHoaDonHoan()
    {
        if (HoaDonGoc is not { } goc || HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        var muc = _nguon
            .Where(d => d.SoHoan > 0m)
            .Select(d => new MucHoan(d.Goc, d.SoHoan))
            .ToList();

        if (muc.Count == 0)
        {
            _lblTrangThai.Text = "Chưa gõ số hoàn ở dòng nào nên chưa lập được tờ hoàn hàng.";
            _luoi.Focus();
            return;
        }

        var ma = _kho.TaoMaHoaDon(goc.KhachHangId, goc.Nam, LoaiHoaDon.HoanHang);
        var tienHoan = TongTienHoan;

        if (!HopThoai.Hoi(
                this,
                $"Lập hoá đơn hoàn hàng {ma} cho hoá đơn {goc.MaHoaDon}?\n\n"
                + $"{muc.Count} món · hoàn lại {So.Tien(tienHoan)}.\n\n"
                + "Hoá đơn gốc để nguyên, số tiền này trừ vào nợ của khách.\n(Ctrl+Z để bỏ.)"))
        {
            return;
        }

        var hoanHang = HoanHang.Tao(goc, muc, ma, _dtNgay.Value.Date, _txtLyDo.Text.Trim());

        _kho.ThucHien(
            $"Hoàn hàng {ma} cho hoá đơn {goc.MaHoaDon}",
            () => _kho.DuLieu.HoaDons.Add(hoanHang),
            phatSuKien: false);

        HoaDonHoanDaTao = hoanHang.Id;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var dangSuaO = _luoi.IsCurrentCellInEditMode;

        switch (keyData)
        {
            case Keys.Escape when !dangSuaO:
                Close();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Một dòng trên bảng chọn: dòng của hoá đơn gốc kèm số hoàn đang gõ.</summary>
    private sealed class DongChon
    {
        private readonly DongCoTheHoan _goc;

        public DongChon(DongCoTheHoan goc) => _goc = goc;

        public ChiTietHoaDon Goc => _goc.Dong;

        public DateTime Ngay => _goc.Dong.Ngay;

        public string TenHang => _goc.Dong.TenHang;

        public string DonVi => _goc.Dong.DonVi;

        public decimal DonGia => _goc.Dong.DonGia;

        public decimal DaMua => _goc.DaMua;

        public decimal DaHoan => _goc.DaHoan;

        public decimal ConHoanDuoc => _goc.ConHoanDuoc;

        public decimal SoHoan { get; set; }

        public decimal TienHoan => Math.Round(DonGia * SoHoan, 0, MidpointRounding.AwayFromZero);
    }
}
