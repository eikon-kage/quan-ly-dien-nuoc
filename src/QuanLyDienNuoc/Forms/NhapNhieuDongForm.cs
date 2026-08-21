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
        /*
          Mọi dòng có chữ trong đó đều `AutoSize`, chỉ bảng xem trước ăn phần còn lại. Trước
          đây năm dòng đặt cứng 92 / 150 / — / 56 / 80 px: vừa khít ở cỡ hiển thị 100%, còn
          máy đặt 125% thì chữ to lên mà ô vẫn thế nên phụ đề bị cắt mất nửa dưới và dòng gợi
          ý bị cắt mất đuôi. Xem "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        */
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Tiêu đề gọi đúng tên cái nút vừa bấm ("NHẬP NHIỀU DÒNG"), không thêm chữ "cùng lúc"
        // nữa: thanh tiêu đề cửa sổ đã nói câu đủ, mà 19pt thì mỗi chữ thêm là một quãng dài.
        // Cách gõ để xuống dưới, nằm cạnh ô gõ — chỗ người ta thật sự cần đọc nó.
        goc.Controls.Add(
            Theme.ThanhTieuDe("NHẬP NHIỀU DÒNG", $"Hàng lấy ngày {_ngay:dd/MM/yyyy}", tuCao: true),
            0,
            0);

        goc.Controls.Add(KhoiGoVaoDay(), 0, 1);

        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;

        // Tỷ lệ cột nới cho hai cột hẹp nhất (ĐƠN VỊ, SỐ LƯỢNG): ở cỡ chữ to, cột hẹp là tên
        // cột phải xuống hai dòng, mà sáu cột mỗi cột một kiểu cao thì đầu bảng nhìn lỗm chỗm.
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongXem.TenHang), "TÊN HÀNG", 300),
            Theme.Cot(nameof(DongXem.DonVi), "ĐƠN VỊ", 115),
            Theme.Cot(nameof(DongXem.DonGia), "ĐƠN GIÁ", 135, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongXem.SoLuong), "SỐ LƯỢNG", 135, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongXem.ThanhTien), "THÀNH TIỀN", 150, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongXem.TinhTrang), "TÌNH TRẠNG", 200));
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

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 12, 20, 6), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        goc.Controls.Add(vien, 0, 2);

        // Neo phải và `AutoSize`: dòng cao đúng bằng chữ 15pt của máy đó, không phải 56px cứng.
        _lblTong.AutoSize = true;
        _lblTong.Anchor = AnchorStyles.Right;
        _lblTong.Font = Theme.FontSo;
        _lblTong.ForeColor = Theme.Chinh;
        _lblTong.Margin = new Padding(20, 8, 24, 8);
        goc.Controls.Add(_lblTong, 0, 3);

        // `noTheoChu`: chữ dài mười bảy ký tự trong nút rộng cứng 280px là vừa khít ở 100%.
        var btnThem = Theme.Nut("THÊM VÀO HOÁ ĐƠN", Theme.Xanh, 280, 52, noTheoChu: true);
        btnThem.Click += (_, _) => Xong();

        var btnHuy = Theme.NutPhu("Huỷ", 120, 52);
        btnHuy.Click += (_, _) => Close();

        var nut = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Padding = new Padding(20, 4, 20, 12),
        };
        nut.Controls.Add(btnThem);
        nut.Controls.Add(btnHuy);
        goc.Controls.Add(nut, 0, 4);

        Controls.Add(goc);
    }

    /// <summary>
    /// Khối xanh nhạt: nhãn, ô gõ, rồi mấy dòng chỉ cách gõ.
    ///
    /// Cách gõ tách thành **mỗi luật một dòng ngắn** thay cho một dòng dài 110 ký tự như
    /// trước. Dòng dài ấy vừa bị cắt mất đuôi trên máy cỡ chữ to (nhãn `AutoSize = false`,
    /// cao 24px, dài hơn khung là mất chữ), mà kể cả đọc được đủ thì ba luật nhồi một dòng
    /// cũng không ai đọc hết. Mỗi dòng dưới 55 ký tự nên còn nguyên ở cỡ 150%.
    /// </summary>
    private Control KhoiGoVaoDay()
    {
        var khoi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.ChinhNhat,
            Padding = new Padding(20, 12, 20, 14),
        };
        khoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khoi.Controls.Add(
            new Label
            {
                Text = "GÕ VÀO ĐÂY  —  mỗi món cách nhau bằng dấu phẩy",
                Font = Theme.FontNhan,
                ForeColor = Theme.ChuDam,
                AutoSize = true,
                Margin = new Padding(2, 0, 0, 8),
            },
            0,
            0);

        _txtDong.Multiline = true;
        _txtDong.ScrollBars = ScrollBars.Vertical;
        _txtDong.Font = Theme.FontNhap;
        _txtDong.TextChanged += (_, _) => XemTruoc();

        // Cao đúng ba dòng chữ **của máy này**: `Font.Height` là số điểm ảnh thật ở cỡ hiển
        // thị đang dùng, nên máy 125% thì ô cũng cao thêm một phần tư. Cộng lề trong của thẻ.
        var oNhap = new Panel
        {
            Dock = DockStyle.Top,
            Height = (Theme.FontNhap.Height * 3) + 24,
            BackColor = Theme.ChinhNhat,
            Margin = new Padding(0),
        };
        oNhap.Controls.Add(Theme.Khung(_txtDong));
        khoi.Controls.Add(oNhap, 0, 1);

        var chiDan = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(2, 8, 0, 0),
        };
        foreach (var dong in new[]
        {
            "Số lượng viết sau chữ x:   ống 27 x10, co 90 x5",
            "Giá viết sau @, gõ tắt được:   keo x1 @8k, bồn x1 @2tr5",
            "Khách trả lại thì số lượng âm:   ống 27 x-2",
        })
        {
            chiDan.Controls.Add(new Label
            {
                Text = dong,
                Font = Theme.FontPhu,
                ForeColor = Theme.Xam,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3),
            });
        }

        khoi.Controls.Add(chiDan, 0, 2);
        return khoi;
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
