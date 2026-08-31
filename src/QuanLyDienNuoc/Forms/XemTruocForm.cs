using System.Drawing.Printing;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Xem trước hoá đơn đúng như khi in ra giấy, rồi in thẳng từ đây.</summary>
public sealed class XemTruocForm : Form
{
    private readonly InHoaDon _taiLieu;
    private readonly PrintPreviewControl _xem = new();
    private readonly Label _lblTrang = new();

    public XemTruocForm(InHoaDon taiLieu)
    {
        _taiLieu = taiLieu;

        Text = "Xem trước hoá đơn";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1100, 820);
        MinimumSize = new Size(900, 700);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
    }

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
        };
        // Dòng có chữ thì tự cao theo chữ: xem "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "XEM TRƯỚC HOÁ ĐƠN",
                $"{_taiLieu.DocumentName} · {_taiLieu.SoTrang} trang",
                tuCao: true),
            0,
            0);

        _xem.Dock = DockStyle.Fill;
        _xem.AutoZoom = true;
        _xem.Columns = 1;
        _xem.Rows = 1;
        _xem.BackColor = Color.FromArgb(120, 124, 130);
        _xem.Document = _taiLieu;

        var vienXem = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 0), BackColor = Theme.Nen };
        vienXem.Controls.Add(_xem);

        var btnIn = Theme.Nut("IN HOÁ ĐƠN", Theme.Chinh, 220, 52, noTheoChu: true);
        btnIn.Click += (_, _) => In();

        var btnTruoc = Theme.NutPhu("◀ Trang trước", 170, 52, noTheoChu: true);
        btnTruoc.Click += (_, _) => DoiTrang(-1);

        var btnSau = Theme.NutPhu("Trang sau ▶", 170, 52, noTheoChu: true);
        btnSau.Click += (_, _) => DoiTrang(1);

        // Ba nút phóng to / thu nhỏ / vừa màn hình gom vào nút ba chấm: chỉ dùng khi muốn
        // ngó kỹ một chỗ, còn bình thường bản xem trước đã vừa khung sẵn.
        var viecPhong = Theme.NutBaCham("Phóng to, thu nhỏ bản xem trước", 52)
            .Viec("Phóng to", () => DoiPhong(1.25))
            .Viec("Thu nhỏ", () => DoiPhong(0.8))
            .Ngan()
            .Viec("Vừa màn hình", () =>
            {
                _xem.AutoZoom = true;
                _xem.Invalidate();
            });

        var btnDong = Theme.NutPhu("Đóng", 130, 52, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTrang.Font = Theme.FontSo;

        khung.Controls.Add(vienXem, 0, 1);
        khung.Controls.Add(
            Theme.ThanhDuoi(_lblTrang, btnIn, btnTruoc, btnSau, viecPhong.Nut, btnDong),
            0,
            2);
        Controls.Add(khung);

        CapNhatNhanTrang();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        // Chạm vào máy in ở đây để báo lỗi tử tế nếu máy chưa cài máy in nào.
        try
        {
            _xem.InvalidatePreview();
        }
        catch (Exception ex) when (ex is InvalidPrinterException or SystemException)
        {
            HopThoai.CanhBao(
                this,
                "Máy tính chưa cài máy in nào nên không xem trước được.\n\n" +
                "Cách xử lý: vào Settings → Bluetooth & devices → Printers & scanners → Add device,\n" +
                "thêm \"Microsoft Print to PDF\" rồi mở lại.");
            Close();
        }
    }

    private void DoiTrang(int buoc)
    {
        var trang = Math.Clamp(_xem.StartPage + buoc, 0, Math.Max(0, _taiLieu.SoTrang - 1));
        _xem.StartPage = trang;
        CapNhatNhanTrang();
    }

    private void DoiPhong(double heSo)
    {
        _xem.AutoZoom = false;
        _xem.Zoom = Math.Clamp(_xem.Zoom * heSo, 0.1, 4.0);
    }

    private void CapNhatNhanTrang() =>
        _lblTrang.Text = $"Trang {_xem.StartPage + 1}/{_taiLieu.SoTrang}";

    private void In()
    {
        try
        {
            using var hopThoai = new PrintDialog
            {
                Document = _taiLieu,
                UseEXDialog = true,
                AllowSomePages = true,
            };

            if (hopThoai.ShowDialog(this) == DialogResult.OK)
            {
                _taiLieu.Print();
                Close();
            }
        }
        catch (InvalidPrinterException)
        {
            HopThoai.CanhBao(this, "Máy tính chưa cài máy in nào. Hãy thêm máy in rồi in lại.");
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không in được:\n" + ex.Message);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                Close();
                return true;
            case Keys.Control | Keys.P:
                In();
                return true;
            case Keys.PageDown:
                DoiTrang(1);
                return true;
            case Keys.PageUp:
                DoiTrang(-1);
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
