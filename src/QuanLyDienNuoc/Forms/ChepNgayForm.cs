using System.ComponentModel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Chép lại toàn bộ hàng của một ngày sang ngày khác. Khách quen hay lấy đúng bộ hàng cũ
/// nên khỏi phải gõ lại từng món.
/// </summary>
public sealed class ChepNgayForm : Form
{
    private readonly HoaDon _hoaDon;
    private readonly BindingList<DongNgay> _nguon = new();
    private readonly DataGridView _luoi = new();
    private readonly DateTimePicker _dtDich = new();

    public ChepNgayForm(HoaDon hoaDon, DateTime ngayDich)
    {
        _hoaDon = hoaDon;

        Text = "Chép lại một ngày";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 640);
        MinimumSize = new Size(700, 520);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        _dtDich.Value = ngayDich.Date;
        TaoGiaoDien();
        Nap();
    }

    /// <summary>Ngày được chọn để chép đi, chỉ có giá trị khi bấm Chép.</summary>
    public DateTime NgayNguon { get; private set; }

    public DateTime NgayDich => _dtDich.Value.Date;

    private void TaoGiaoDien()
    {
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "CHÉP LẠI MỘT NGÀY",
                $"Hoá đơn {_hoaDon.MaHoaDon} — chọn ngày muốn chép, các dòng hàng của ngày đó sẽ được thêm lại."),
            0,
            0);

        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongNgay.Ngay), "NGÀY", 120, "dd/MM/yyyy"),
            Theme.Cot(nameof(DongNgay.SoDong), "SỐ DÒNG", 90, canPhai: true),
            Theme.Cot(nameof(DongNgay.TongTien), "THÀNH TIỀN", 140, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongNgay.TomTat), "GỒM CÓ", 380));
        _luoi.DataSource = _nguon;
        _luoi.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                Xong();
            }
        };

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 6), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        goc.Controls.Add(vien, 0, 1);

        _dtDich.Format = DateTimePickerFormat.Custom;
        _dtDich.CustomFormat = Theme.DangNgay;
        _dtDich.Font = Theme.FontNhap;
        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(20, 10, 20, 0) };
        hang.Controls.Add(Theme.Truong("CHÉP SANG NGÀY", _dtDich, 200));
        goc.Controls.Add(hang, 0, 2);

        var btnChep = Theme.Nut("CHÉP CÁC DÒNG NÀY", Theme.Xanh, 280, 52);
        btnChep.Click += (_, _) => Xong();

        var btnHuy = Theme.NutPhu("Huỷ (Esc)", 160, 52);
        btnHuy.Click += (_, _) => Close();

        var nut = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(20, 4, 20, 10) };
        nut.Controls.Add(btnChep);
        nut.Controls.Add(btnHuy);
        goc.Controls.Add(nut, 0, 3);

        Controls.Add(goc);
    }

    private void Nap()
    {
        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var nhom in _hoaDon.ChiTiet.GroupBy(c => c.Ngay.Date).OrderByDescending(g => g.Key))
        {
            var ten = nhom
                .Select(c => c.TenHang)
                .Take(4)
                .ToList();
            var tomTat = string.Join(", ", ten) + (nhom.Count() > ten.Count ? "…" : string.Empty);

            _nguon.Add(new DongNgay
            {
                Ngay = nhom.Key,
                SoDong = nhom.Count(),
                TongTien = nhom.Sum(c => c.ThanhTien),
                TomTat = tomTat,
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();
    }

    private void Xong()
    {
        if (_luoi.CurrentRow?.DataBoundItem is not DongNgay dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn ngày muốn chép lại.");
            return;
        }

        if (dong.Ngay == NgayDich
            && !HopThoai.Hoi(this, "Ngày chép sang trùng với ngày nguồn, hoá đơn sẽ có hai bộ dòng giống nhau.\n\nVẫn chép?"))
        {
            return;
        }

        NgayNguon = dong.Ngay;
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

    private sealed class DongNgay
    {
        public DateTime Ngay { get; set; }

        public int SoDong { get; set; }

        public decimal TongTien { get; set; }

        public string TomTat { get; set; } = string.Empty;
    }
}
