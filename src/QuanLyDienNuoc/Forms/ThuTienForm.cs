using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Khách đưa một cục tiền trả cho nhiều hoá đơn: gõ số tiền là thấy ngay hoá đơn nào trừ
/// bao nhiêu (cũ nhất trả trước), bấm ghi một lần cho cả loạt.
/// </summary>
public sealed class ThuTienForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;

    private readonly DateTimePicker _dtNgay = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = Theme.DangNgay,
        Font = Theme.FontNhap,
    };

    private readonly TextBox _txtSoTien = Theme.O(220);
    private readonly TextBox _txtGhiChu = Theme.O(300);

    private readonly DataGridView _luoiPhanBo = new();
    private readonly BindingList<DongPhanBo> _nguonPhanBo = new();

    private readonly DataGridView _luoiLichSu = new();
    private readonly BindingList<DongLanThu> _nguonLichSu = new();

    private readonly Label _lblTomTat = new();
    private readonly Label _lblTrangThai = new();

    public ThuTienForm(Guid khachId)
    {
        _khachId = khachId;

        Text = "Thu tiền của khách";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1180, 780);
        MinimumSize = new Size(1040, 700);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    private KhachHang? Khach => _kho.TimKhach(_khachId);

    /// <summary>Mọi hoá đơn của khách, tính cả các năm trước — tiền trả cho nợ cũ trước.</summary>
    private List<HoaDon> HoaDons => _kho.HoaDonCuaKhach(_khachId);

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
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "THU TIỀN CỦA KHÁCH",
                "Gõ số tiền khách đưa, phần mềm tự chia cho các hoá đơn còn nợ — hoá đơn cũ nhất trả trước."),
            0,
            0);
        goc.Controls.Add(TaoThanhNhap(), 0, 1);
        goc.Controls.Add(TaoThanNoiDung(), 0, 2);
        goc.Controls.Add(TaoThanhDuoi(), 0, 3);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 4);

        Controls.Add(goc);
    }

    private Control TaoThanhNhap()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(14, 8, 14, 8) };

        var btnGhi = Theme.Nut("GHI THU TIỀN", Theme.Xanh, 220, 34);
        btnGhi.Click += (_, _) => Ghi();

        _txtSoTien.TextChanged += (_, _) => CapNhatPhanBo();
        _txtSoTien.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Ghi();
            }
        };

        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("NGÀY THU", _dtNgay, 160));
        hang.Controls.Add(Theme.Truong("SỐ TIỀN KHÁCH ĐƯA", _txtSoTien, 220));
        hang.Controls.Add(Theme.Truong("GHI CHÚ", _txtGhiChu, 300));
        hang.Controls.Add(Theme.Truong(" ", btnGhi, 220));

        nen.Controls.Add(hang);
        return nen;
    }

    private Control TaoThanNoiDung()
    {
        var than = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 8, 20, 0),
        };
        than.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        than.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        than.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        than.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));

        than.Controls.Add(Nhan("CHIA CHO CÁC HOÁ ĐƠN"), 0, 0);
        than.Controls.Add(Theme.Khung(TaoLuoiPhanBo()), 0, 1);
        than.Controls.Add(Nhan("CÁC LẦN ĐÃ THU CỦA KHÁCH NÀY"), 0, 2);
        than.Controls.Add(Theme.Khung(TaoLuoiLichSu()), 0, 3);
        return than;
    }

    private static Label Nhan(string chu) => new()
    {
        Text = chu,
        Font = Theme.FontDam,
        ForeColor = Theme.Xam,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private Control TaoLuoiPhanBo()
    {
        Theme.ApDungLuoi(_luoiPhanBo);
        _luoiPhanBo.ReadOnly = true;
        _luoiPhanBo.Columns.AddRange(
            Theme.Cot(nameof(DongPhanBo.Ma), "MÃ HĐ", 110),
            Theme.Cot(nameof(DongPhanBo.NgayMo), "MỞ NGÀY", 110, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongPhanBo.TongTien), "TỔNG HĐ", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongPhanBo.ConNo), "ĐANG NỢ", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongPhanBo.TraLanNay), "TRẢ LẦN NÀY", 140, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongPhanBo.ConLaiSau), "CÒN LẠI SAU KHI TRẢ", 160, "#,##0", canPhai: true));

        _luoiPhanBo.DataSource = _nguonPhanBo;
        _luoiPhanBo.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is not { } kieu)
            {
                return;
            }

            var cot = _luoiPhanBo.Columns[e.ColumnIndex].DataPropertyName;
            if (cot == nameof(DongPhanBo.TraLanNay) && e.Value is decimal tra)
            {
                kieu.Font = Theme.FontLuoiDam;
                kieu.ForeColor = tra > 0 ? Theme.Xanh : Theme.Xam;
            }
            else if (cot == nameof(DongPhanBo.ConLaiSau) && e.Value is decimal conLai)
            {
                kieu.ForeColor = conLai > 0 ? Theme.Do : Theme.Xanh;
            }
        };

        return _luoiPhanBo;
    }

    private Control TaoLuoiLichSu()
    {
        Theme.ApDungLuoi(_luoiLichSu);
        _luoiLichSu.ReadOnly = true;
        _luoiLichSu.Columns.AddRange(
            Theme.Cot(nameof(DongLanThu.Ngay), "NGÀY THU", 110, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongLanThu.SoTien), "SỐ TIỀN", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongLanThu.HoaDon), "CHIA CHO HOÁ ĐƠN", 240),
            Theme.Cot(nameof(DongLanThu.GhiChu), "GHI CHÚ", 220));

        _luoiLichSu.DataSource = _nguonLichSu;
        return _luoiLichSu;
    }

    private Control TaoThanhDuoi()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 8, 20, 10) };

        var btnXoa = Theme.NutPhu("Xoá lần thu đã chọn", 240, 46);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => XoaLanThu();

        var btnDong = Theme.NutPhu("Đóng", 120, 46);
        btnDong.Click += (_, _) => Close();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnXoa);
        trai.Controls.Add(btnDong);

        _lblTomTat.Dock = DockStyle.Right;
        _lblTomTat.Width = 640;
        _lblTomTat.Font = Theme.FontSo;
        _lblTomTat.TextAlign = ContentAlignment.MiddleRight;

        nen.Controls.Add(trai);
        nen.Controls.Add(_lblTomTat);
        return nen;
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);
        _lblTrangThai.Text = "Enter để ghi · Esc để đóng · Ctrl+Z hoàn tác";

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        return nen;
    }

    // ---------------- Nạp dữ liệu ----------------

    private void Nap()
    {
        if (Khach is null)
        {
            Close();
            return;
        }

        NapLichSu();
        CapNhatPhanBo();
        ActiveControl = _txtSoTien;
    }

    private void NapLichSu()
    {
        _nguonLichSu.RaiseListChangedEvents = false;
        _nguonLichSu.Clear();

        foreach (var lan in ThuTien.LichSu(HoaDons))
        {
            _nguonLichSu.Add(new DongLanThu
            {
                Nguon = lan,
                Ngay = lan.Ngay,
                SoTien = lan.SoTien,
                HoaDon = lan.SoHoaDon > 1 ? $"{lan.SoHoaDon} hoá đơn: {lan.MoTaHoaDon}" : lan.MoTaHoaDon,
                GhiChu = lan.GhiChu,
            });
        }

        _nguonLichSu.RaiseListChangedEvents = true;
        _nguonLichSu.ResetBindings();
    }

    /// <summary>Tính lại bảng chia tiền theo số đang gõ, chưa ghi gì vào sổ.</summary>
    private void CapNhatPhanBo()
    {
        var hoaDons = HoaDons;
        var soTien = So.Doc(_txtSoTien.Text);
        var ketQua = ThuTien.Chia(hoaDons, soTien);
        var theoHoaDon = ketQua.PhanBo.ToDictionary(p => p.HoaDon.Id, p => p.SoTien);

        _nguonPhanBo.RaiseListChangedEvents = false;
        _nguonPhanBo.Clear();

        foreach (var hoaDon in ThuTien.XepTuCuNhat(hoaDons).Where(h => h.ConLai > 0m))
        {
            var tra = theoHoaDon.GetValueOrDefault(hoaDon.Id);
            _nguonPhanBo.Add(new DongPhanBo
            {
                Ma = hoaDon.DaChot ? hoaDon.MaHoaDon + " (chốt)" : hoaDon.MaHoaDon,
                NgayMo = hoaDon.NgayMo,
                TongTien = hoaDon.TongTien,
                ConNo = hoaDon.ConLai,
                TraLanNay = tra,
                ConLaiSau = hoaDon.ConLai - tra,
            });
        }

        _nguonPhanBo.RaiseListChangedEvents = true;
        _nguonPhanBo.ResetBindings();

        var tongNo = hoaDons.Sum(h => h.ConLai);
        var ten = Khach?.Ten ?? string.Empty;
        _lblTomTat.Text = $"{ten}   ·   đang nợ {So.Tien(tongNo)}"
                          + (soTien > 0 ? $"   ·   trả {So.Tien(ketQua.DaPhanBo)}, còn nợ {So.Tien(tongNo - ketQua.DaPhanBo)}" : string.Empty)
                          + (ketQua.ConDu > 0 ? $"   ·   thừa {So.Tien(ketQua.ConDu)}" : string.Empty);
    }

    // ---------------- Thao tác ----------------

    private void Ghi()
    {
        if (Khach is not { } khach || HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        var soTien = So.Doc(_txtSoTien.Text);
        if (soTien <= 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập số tiền lớn hơn 0.");
            _txtSoTien.Focus();
            _txtSoTien.SelectAll();
            return;
        }

        var hoaDons = HoaDons;
        if (hoaDons.Count == 0)
        {
            HopThoai.CanhBao(this, $"{khach.Ten} chưa có hoá đơn nào để ghi tiền vào.");
            return;
        }

        var ketQua = ThuTien.Chia(hoaDons, soTien);

        if (ketQua.ConDu > 0m)
        {
            var moiNhat = ThuTien.XepTuCuNhat(hoaDons)[^1];
            var traTruoc = HopThoai.Hoi(
                this,
                $"Khách đưa {So.Tien(soTien)} nhưng chỉ còn nợ {So.Tien(ketQua.DaPhanBo)}, thừa {So.Tien(ketQua.ConDu)}.\n\n" +
                $"Ghi chỗ thừa vào hoá đơn mới nhất ({moiNhat.MaHoaDon}) coi như trả trước?\n\n" +
                "Chọn Không nếu chỉ ghi đúng phần khách đang nợ.");

            if (traTruoc)
            {
                ketQua = ThuTien.Chia(hoaDons, soTien, ghiDuVaoHoaDonMoiNhat: true);
            }
            else if (ketQua.PhanBo.Count == 0)
            {
                HopThoai.Bao(this, $"{khach.Ten} không còn nợ khoản nào nên chưa ghi gì cả.");
                return;
            }
        }

        var ngay = _dtNgay.Value.Date;
        var ghiChu = _txtGhiChu.Text.Trim();
        var daPhanBo = ketQua.DaPhanBo;
        var soHoaDon = ketQua.PhanBo.Count;

        _kho.ThucHien(
            $"Thu {So.Tien(daPhanBo)} của {khach.Ten} cho {soHoaDon} hoá đơn",
            () => ThuTien.Ghi(ketQua, ngay, ghiChu),
            phatSuKien: false);

        _txtSoTien.Clear();
        _txtGhiChu.Clear();
        _txtSoTien.Focus();
        Nap();

        _lblTrangThai.Text = $"Đã ghi {So.Tien(daPhanBo)} ngày {ngay:dd/MM/yyyy}, chia cho {soHoaDon} hoá đơn. Bấm Ctrl+Z nếu muốn bỏ.";
    }

    private void XoaLanThu()
    {
        if (HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        if (_luoiLichSu.CurrentRow?.DataBoundItem is not DongLanThu dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn một lần thu tiền trong bảng dưới để xoá.");
            return;
        }

        var lan = dong.Nguon;
        var moTaHoaDon = lan.SoHoaDon > 1 ? $" (chia cho {lan.SoHoaDon} hoá đơn)" : string.Empty;
        if (!HopThoai.Hoi(
                this,
                $"Xoá lần thu {So.Tien(lan.SoTien)} ngày {lan.Ngay:dd/MM/yyyy}{moTaHoaDon}?\n\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien(
            $"Xoá lần thu {So.Tien(lan.SoTien)}",
            () => ThuTien.Xoa(HoaDons, lan.Ma),
            phatSuKien: false);

        Nap();
        _lblTrangThai.Text = $"Đã xoá lần thu {So.Tien(lan.SoTien)}. Bấm Ctrl+Z để lấy lại.";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Z:
                _kho.HoanTac();
                Nap();
                return true;
            case Keys.Control | Keys.Y:
                _kho.LamLai();
                Nap();
                return true;
            case Keys.Escape:
                Close();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Một dòng trong bảng chia tiền cho các hoá đơn.</summary>
    private sealed class DongPhanBo
    {
        public string Ma { get; set; } = string.Empty;

        public DateTime NgayMo { get; set; }

        public decimal TongTien { get; set; }

        public decimal ConNo { get; set; }

        public decimal TraLanNay { get; set; }

        public decimal ConLaiSau { get; set; }
    }

    /// <summary>Một dòng trong bảng các lần đã thu.</summary>
    private sealed class DongLanThu
    {
        public LanThuTien Nguon { get; set; } = null!;

        public DateTime Ngay { get; set; }

        public decimal SoTien { get; set; }

        public string HoaDon { get; set; } = string.Empty;

        public string GhiChu { get; set; } = string.Empty;
    }
}
