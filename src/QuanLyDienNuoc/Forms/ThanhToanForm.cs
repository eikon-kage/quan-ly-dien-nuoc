using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Ghi các lần khách trả tiền cho một hoá đơn.</summary>
public sealed class ThanhToanForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _hoaDonId;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<ThanhToan> _nguon = new();

    private readonly DateTimePicker _dtNgay = new() { Format = DateTimePickerFormat.Custom, CustomFormat = Theme.DangNgay, Font = Theme.FontNhap };
    private readonly TextBox _txtSoTien = Theme.O(200);
    private readonly TextBox _txtGhiChu = Theme.O(260);
    private readonly Label _lblTong = new();

    public ThanhToanForm(Guid hoaDonId)
    {
        _hoaDonId = hoaDonId;

        Text = "Thanh toán";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(960, 640);
        MinimumSize = new Size(900, 600);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    private HoaDon? HoaDon => _kho.TimHoaDon(_hoaDonId);

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

        var hoaDon = HoaDon;
        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "THANH TOÁN",
                hoaDon is null ? string.Empty : $"Hoá đơn {hoaDon.MaHoaDon} · mở ngày {hoaDon.NgayMo:dd/MM/yyyy}"),
            0,
            0);

        // Thanh nhập nhanh
        var nenNhap = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(14, 8, 14, 8) };
        var btnThem = Theme.Nut("+  GHI THANH TOÁN", Theme.Xanh, 230, 34);
        btnThem.Click += (_, _) => Them();

        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("NGÀY TRẢ", _dtNgay, 160));
        hang.Controls.Add(Theme.Truong("SỐ TIỀN", _txtSoTien, 200));
        hang.Controls.Add(Theme.Truong("GHI CHÚ", _txtGhiChu, 260));
        hang.Controls.Add(Theme.Truong(" ", btnThem, 230));
        nenNhap.Controls.Add(hang);

        _txtSoTien.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Them();
            }
        };

        // Lưới
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(ThanhToan.Ngay), "NGÀY TRẢ", 120, "dd/MM/yyyy"),
            Theme.Cot(nameof(ThanhToan.SoTien), "SỐ TIỀN", 150, "#,##0", canPhai: true),
            Theme.Cot(nameof(ThanhToan.GhiChu), "GHI CHÚ", 260));
        _luoi.DataSource = _nguon;

        // Thanh dưới
        var nenDuoi = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(0, 10, 0, 0) };

        var btnXoa = Theme.NutPhu("Xoá lần trả này", 210, 46);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => Xoa();

        var btnDong = Theme.NutPhu("Đóng", 140, 46);
        btnDong.Click += (_, _) => Close();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(btnXoa);
        trai.Controls.Add(btnDong);

        _lblTong.Dock = DockStyle.Right;
        _lblTong.Width = 620;
        _lblTong.Font = Theme.FontSo;
        _lblTong.TextAlign = ContentAlignment.MiddleRight;

        nenDuoi.Controls.Add(trai);
        nenDuoi.Controls.Add(_lblTong);

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(_luoi));

        var vienDuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 10), BackColor = Theme.Nen };
        vienDuoi.Controls.Add(nenDuoi);

        khung.Controls.Add(nenNhap, 0, 1);
        khung.Controls.Add(vienLuoi, 0, 2);
        khung.Controls.Add(vienDuoi, 0, 3);
        Controls.Add(khung);

        ActiveControl = _txtSoTien;
    }

    private void Nap()
    {
        var hoaDon = HoaDon;
        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        if (hoaDon is not null)
        {
            foreach (var lan in hoaDon.ThanhToans.OrderBy(t => t.Ngay))
            {
                _nguon.Add(lan);
            }
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        var tong = hoaDon?.TongTien ?? 0m;
        var daTra = hoaDon?.DaThanhToan ?? 0m;
        _lblTong.Text = $"Hoá đơn: {So.Tien(tong)}   ·   Đã trả: {So.Tien(daTra)}   ·   Còn lại: {So.Tien(tong - daTra)}";
    }

    private void Them()
    {
        if (HoaDon is not { } hoaDon)
        {
            return;
        }

        var soTien = So.Doc(_txtSoTien.Text);
        if (soTien <= 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập số tiền lớn hơn 0.");
            _txtSoTien.Focus();
            return;
        }

        var lan = new ThanhToan
        {
            Ngay = _dtNgay.Value.Date,
            SoTien = soTien,
            GhiChu = _txtGhiChu.Text.Trim(),
        };

        _kho.ThucHien($"Ghi thanh toán {So.Tien(soTien)}", () => hoaDon.ThanhToans.Add(lan), phatSuKien: false);

        _txtSoTien.Clear();
        _txtGhiChu.Clear();
        _txtSoTien.Focus();
        Nap();
    }

    private void Xoa()
    {
        if (HoaDon is not { } hoaDon || _luoi.CurrentRow?.DataBoundItem is not ThanhToan lan)
        {
            HopThoai.CanhBao(this, "Hãy chọn một lần trả tiền để xoá.");
            return;
        }

        // Khoản này là một phần của lần khách đưa tiền chung cho nhiều hoá đơn: xoá lẻ một
        // mảnh thì sổ còn lại nửa lần thu, nên xoá cả lần thu cho gọn.
        if (lan.PhieuThuId is { } phieuThuId)
        {
            var hoaDonCuaKhach = _kho.HoaDonCuaKhach(hoaDon.KhachHangId);
            var caLan = ThuTien.LichSu(hoaDonCuaKhach).FirstOrDefault(l => l.Ma == phieuThuId);
            var tongLan = caLan?.SoTien ?? lan.SoTien;
            var soHoaDon = caLan?.SoHoaDon ?? 1;

            if (!HopThoai.Hoi(
                    this,
                    $"Khoản {So.Tien(lan.SoTien)} này nằm trong một lần khách đưa {So.Tien(tongLan)} " +
                    $"ngày {lan.Ngay:dd/MM/yyyy}, chia cho {soHoaDon} hoá đơn.\n\n" +
                    "Xoá cả lần thu đó?"))
            {
                return;
            }

            _kho.ThucHien(
                $"Xoá lần thu {So.Tien(tongLan)}",
                () => ThuTien.Xoa(hoaDonCuaKhach, phieuThuId),
                phatSuKien: false);

            Nap();
            return;
        }

        if (!HopThoai.Hoi(this, $"Xoá lần trả {So.Tien(lan.SoTien)} ngày {lan.Ngay:dd/MM/yyyy}?"))
        {
            return;
        }

        _kho.ThucHien(
            $"Xoá thanh toán {So.Tien(lan.SoTien)}",
            () => hoaDon.ThanhToans.RemoveAll(t => t.Id == lan.Id),
            phatSuKien: false);

        Nap();
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
}
