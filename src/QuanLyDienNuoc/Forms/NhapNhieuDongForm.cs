using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Gõ một dòng tự do như ghi sổ ngoài công trình rồi tách thành nhiều dòng hàng:
/// <c>ống 27 x10, co 90 x5, keo x1</c>. Bắt buộc xem trước để soát lại giá trước khi ghi.
/// </summary>
public sealed class NhapNhieuDongForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;
    private readonly DateTime _ngay;

    private readonly TextBox _txtDong = new();
    private readonly BindingList<DongXem> _nguon = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTong = new();

    public NhapNhieuDongForm(Guid khachId, DateTime ngay, string goSan = "")
    {
        _khachId = khachId;
        _ngay = ngay.Date;

        Text = "Nhập nhiều dòng cùng lúc";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1060, 720);
        MinimumSize = new Size(900, 600);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        _txtDong.Text = goSan;
        XemTruoc();
    }

    /// <summary>Các dòng hàng đã dựng xong, chỉ có giá trị khi bấm Thêm.</summary>
    public List<ChiTietHoaDon> KetQua { get; } = new();

    private KhachHang? Khach => _kho.TimKhach(_khachId);

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
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "NHẬP NHIỀU DÒNG CÙNG LÚC",
                $"Ngày lấy hàng: {_ngay:dd/MM/yyyy}. Cách nhau bằng dấu phẩy, số lượng viết sau chữ x."),
            0,
            0);

        _txtDong.Multiline = true;
        _txtDong.ScrollBars = ScrollBars.Vertical;
        _txtDong.Font = Theme.FontNhap;
        _txtDong.BorderStyle = BorderStyle.FixedSingle;
        _txtDong.Dock = DockStyle.Fill;
        _txtDong.TextChanged += (_, _) => XemTruoc();

        var oNen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(20, 10, 20, 10) };
        var nhan = new Label
        {
            Text = "GÕ VÀO ĐÂY   —   ví dụ:  ống 27 x10, co 90 x5, keo dán ống x1 @8000   ·   trả lại thì viết số âm: ống 27 x-2",
            Font = Theme.FontNhan,
            ForeColor = Theme.Xam,
            Dock = DockStyle.Top,
            Height = 24,
        };
        var oKhung = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat };
        oKhung.Controls.Add(Theme.Khung(_txtDong));
        oNen.Controls.Add(oKhung);
        oNen.Controls.Add(nhan);
        goc.Controls.Add(oNen, 0, 1);

        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongXem.TenHang), "TÊN HÀNG", 300),
            Theme.Cot(nameof(DongXem.DonVi), "ĐƠN VỊ", 90),
            Theme.Cot(nameof(DongXem.DonGia), "ĐƠN GIÁ", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongXem.SoLuong), "SỐ LƯỢNG", 110, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongXem.ThanhTien), "THÀNH TIỀN", 140, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongXem.TinhTrang), "TÌNH TRẠNG", 190));
        _luoi.DataSource = _nguon;
        _luoi.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is not { } kieu)
            {
                return;
            }

            if (_luoi.Columns[e.ColumnIndex].DataPropertyName == nameof(DongXem.TinhTrang)
                && _luoi.Rows[e.RowIndex].DataBoundItem is DongXem dong)
            {
                kieu.ForeColor = dong.CanChuY ? Theme.Cam : Theme.Xam;
            }
        };

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 6), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        goc.Controls.Add(vien, 0, 2);

        _lblTong.Dock = DockStyle.Fill;
        _lblTong.Font = Theme.FontSo;
        _lblTong.ForeColor = Theme.Chinh;
        _lblTong.TextAlign = ContentAlignment.MiddleRight;
        _lblTong.Padding = new Padding(0, 0, 24, 0);
        goc.Controls.Add(_lblTong, 0, 3);

        var btnThem = Theme.Nut("THÊM VÀO HOÁ ĐƠN", Theme.Xanh, 280, 52);
        btnThem.Click += (_, _) => Xong();

        var btnHuy = Theme.NutPhu("Huỷ (Esc)", 160, 52);
        btnHuy.Click += (_, _) => Close();

        var nut = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(20, 4, 20, 10) };
        nut.Controls.Add(btnThem);
        nut.Controls.Add(btnHuy);
        goc.Controls.Add(nut, 0, 4);

        Controls.Add(goc);
    }

    private void XemTruoc()
    {
        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        if (Khach is { } khach)
        {
            foreach (var muc in DongNhapNhanh.Tach(_txtDong.Text))
            {
                var vatTu = TimVatTu(muc.Ten);
                var gia = muc.DonGia ?? (vatTu is null ? 0m : _kho.GiaCho(khach, vatTu));

                _nguon.Add(new DongXem
                {
                    VatTu = vatTu,
                    TenHang = vatTu?.Ten ?? muc.Ten,
                    DonVi = vatTu?.DonVi ?? string.Empty,
                    DonGia = gia,
                    SoLuong = muc.SoLuong,
                    ThanhTien = Math.Round(gia * muc.SoLuong, 0, MidpointRounding.AwayFromZero),
                    TinhTrang = MoTaTinhTrang(vatTu, gia, muc.SoLuong),
                    CanChuY = vatTu is null || gia <= 0m,
                });
            }
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        var tong = _nguon.Sum(d => d.ThanhTien);
        _lblTong.Text = $"{_nguon.Count} dòng   ·   Tạm tính: {So.Tien(tong)}";
    }

    private static string MoTaTinhTrang(VatTu? vatTu, decimal gia, decimal soLuong) => vatTu switch
    {
        null => "Hàng mới — sẽ thêm vào danh mục",
        _ when gia <= 0m => "Chưa có giá — nhớ sửa lại",
        _ when soLuong < 0m => "Khách trả lại — trừ vào hoá đơn",
        _ => "Có sẵn, dùng giá của khách",
    };

    /// <summary>Khớp tên gõ tắt với danh mục, lấy mặt hàng khớp nhất.</summary>
    private VatTu? TimVatTu(string ten) => _kho.DuLieu.VatTus
        .Select(v => (VatTu: v, Diem: TimHang.Diem(v.Ten, v.MaTat, ten)))
        .Where(x => x.Diem > 0)
        .OrderByDescending(x => x.Diem)
        .ThenBy(x => x.VatTu.Ten.Length)
        .Select(x => x.VatTu)
        .FirstOrDefault();

    private void Xong()
    {
        if (_nguon.Count == 0)
        {
            HopThoai.CanhBao(this, "Chưa gõ được dòng nào. Ví dụ: ống 27 x10, co 90 x5");
            return;
        }

        var thieuGia = _nguon.Count(d => d.DonGia <= 0m);
        if (thieuGia > 0
            && !HopThoai.Hoi(this, $"Có {thieuGia} dòng chưa có đơn giá.\n\nVẫn thêm rồi sửa giá sau trên lưới?"))
        {
            return;
        }

        KetQua.Clear();
        foreach (var dong in _nguon)
        {
            KetQua.Add(new ChiTietHoaDon
            {
                Ngay = _ngay,
                VatTuId = dong.VatTu?.Id,
                TenHang = dong.TenHang,
                DonVi = dong.DonVi,
                DonGia = dong.DonGia,
                SoLuong = dong.SoLuong,
            });
        }

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

    /// <summary>Một dòng trên bảng xem trước.</summary>
    private sealed class DongXem
    {
        public VatTu? VatTu { get; set; }

        public string TenHang { get; set; } = string.Empty;

        public string DonVi { get; set; } = string.Empty;

        public decimal DonGia { get; set; }

        public decimal SoLuong { get; set; }

        public decimal ThanhTien { get; set; }

        public string TinhTrang { get; set; } = string.Empty;

        public bool CanChuY { get; set; }
    }
}
