using System.Collections.Specialized;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Cuối buổi, gom hàng một khách đã lấy trong ngày thành một tấm ảnh bảng kê rồi chép vào bộ
/// nhớ máy để dán thẳng sang Zalo cho khách xem lại.
/// <para>
/// Hai nút <b>Hôm nay</b> / <b>Hôm qua</b> để ngay đầu màn: quá nửa số lần dùng là hai ngày ấy
/// (chốt sổ cuối buổi, hoặc sáng hôm sau mới nhớ ra chưa gửi). Muốn ngày khác thì vẫn có ô lịch
/// bên cạnh.
/// </para>
/// </summary>
public sealed class TongHopNgayForm : Form
{
    /// <summary>Thư mục cất ảnh bảng kê, nằm cạnh file dữ liệu.</summary>
    private const string TenThuMucAnh = "BangKeNgay";

    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;
    private readonly DateTime _homNay;

    /// <summary>Giờ ghi ở chân ảnh; chỉ khác null khi chụp ảnh giao diện (xem hàm dựng).</summary>
    private readonly DateTime? _lucLap;

    private readonly OChonNgay _dtNgay = new();
    private readonly PictureBox _xem = new();
    private readonly Label _lblTrong = Theme.NhanDaiDong();
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    private Button _btnChep = null!;
    private Button _btnLuu = null!;

    private BangKeNgay? _bangKe;
    private Bitmap? _anh;
    private bool _dangNap;

    /// <param name="ngay">Ngày mở ra sẵn; để trống là hôm nay.</param>
    /// <param name="homNay">
    /// Ngày coi như "hôm nay" — chỉ để chụp ảnh giao diện trên máy dựng tự động ra ảnh giống
    /// nhau mọi lần chạy. Phần mềm chạy thật thì để trống, lấy ngày của máy.
    /// </param>
    public TongHopNgayForm(Guid khachId, DateTime? ngay = null, DateTime? homNay = null)
    {
        _khachId = khachId;
        _homNay = (homNay ?? DateTime.Today).Date;
        _lucLap = homNay is null ? null : _homNay.AddHours(17).AddMinutes(20);

        Text = "Tổng hợp hàng trong ngày";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1020, 860);
        MinimumSize = new Size(880, 700);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        _dangNap = true;
        _dtNgay.Value = (ngay ?? _homNay).Date;
        _dangNap = false;
        NapBangKe();
    }

    private KhachHang? Khach => _kho.TimKhach(_khachId);

    private DateTime NgayDangXem => _dtNgay.Value.Date;

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        // Mọi dải chữ tự cao theo cỡ chữ, chỉ khung xem ảnh ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "TỔNG HỢP HÀNG TRONG NGÀY",
                $"{Khach?.Ten} — ra một tấm ảnh bảng kê để gửi Zalo cho khách",
                tuCao: true),
            0,
            0);
        khung.Controls.Add(TaoThanhChonNgay(), 0, 1);
        khung.Controls.Add(TaoKhungXem(), 0, 2);

        _btnChep = Theme.Nut("CHÉP ẢNH ĐỂ DÁN VÀO ZALO", Theme.Chinh, 320, 52, noTheoChu: true);
        _btnChep.Click += (_, _) => ChepAnh();

        _btnLuu = Theme.NutPhu("Lưu ảnh ra file...", 200, 52, noTheoChu: true);
        _btnLuu.Click += (_, _) => LuuRaFile();

        var btnDong = Theme.NutPhu("Đóng", 130, 52, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        khung.Controls.Add(Theme.ThanhDuoi(null, _btnChep, _btnLuu, btnDong), 0, 3);
        khung.Controls.Add(Theme.ThanhTrangThai(_lblTrangThai), 0, 4);
        Controls.Add(khung);
    }

    private Control TaoThanhChonNgay()
    {
        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 12, 20, 4),
        };

        var btnHomNay = Theme.Nut("HÔM NAY", Theme.Xanh, 150, 46, noTheoChu: true);
        btnHomNay.Click += (_, _) => ChonNgay(_homNay);

        var btnHomQua = Theme.Nut("HÔM QUA", Theme.Cam, 150, 46, noTheoChu: true);
        btnHomQua.Click += (_, _) => ChonNgay(_homNay.AddDays(-1));

        var nhan = Theme.Nhan("Ngày khác:", Theme.FontNhan, Theme.Xam);
        nhan.AutoSize = true;
        nhan.Margin = new Padding(16, 14, 10, 0);

        _dtNgay.Margin = new Padding(0, 6, 0, 0);
        _dtNgay.ValueChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapBangKe();
            }
        };

        hang.Controls.Add(btnHomNay);
        hang.Controls.Add(btnHomQua);
        hang.Controls.Add(nhan);
        hang.Controls.Add(_dtNgay);
        return hang;
    }

    private Control TaoKhungXem()
    {
        var vien = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 8, 20, 8),
            BackColor = Theme.Nen,
        };

        // Ảnh thu cả tấm vào khung (Zoom) chứ không cuộn: người dùng chỉ cần liếc xem đúng khách,
        // đúng ngày, đủ mấy dòng hàng — đọc kỹ từng con số thì đã có chính cái bảng ở màn trước.
        _xem.Dock = DockStyle.Fill;
        _xem.SizeMode = PictureBoxSizeMode.Zoom;
        _xem.BackColor = Color.FromArgb(120, 124, 130);

        _lblTrong.Dock = DockStyle.Fill;
        _lblTrong.TextAlign = ContentAlignment.MiddleCenter;
        _lblTrong.Font = Theme.FontNhap;
        _lblTrong.ForeColor = Theme.Xam;
        _lblTrong.BackColor = Theme.Trang;
        _lblTrong.Visible = false;

        vien.Controls.Add(_lblTrong);
        vien.Controls.Add(_xem);
        return vien;
    }

    private void ChonNgay(DateTime ngay)
    {
        if (NgayDangXem == ngay.Date)
        {
            return;
        }

        _dtNgay.Value = ngay.Date;
    }

    private void NapBangKe()
    {
        if (Khach is not { } khach)
        {
            Close();
            return;
        }

        // Còn nợ luôn tính đến hôm nay, kể cả khi đang tổng hợp cho hôm qua: khách đọc bảng kê
        // cũ vẫn muốn biết ngay lúc này mình còn nợ bao nhiêu.
        _bangKe = TongHopNgay.Lam(khach, _kho.HoaDonCuaKhach(khach.Id), NgayDangXem, mocNo: _homNay);

        DoiAnh(null);

        if (_bangKe.Trong)
        {
            _lblTrong.Text =
                $"Ngày {NgayDangXem:dd/MM/yyyy} khách {khach.Ten} không lấy hàng, cũng không trả tiền.\n\n" +
                "Chọn ngày khác ở trên.";
            _lblTrong.Visible = true;
            _xem.Visible = false;
            _btnChep.Enabled = false;
            _btnLuu.Enabled = false;
            _lblTrangThai.Text = $"Ngày {NgayDangXem:dd/MM/yyyy}: không có gì để gửi khách.";
            return;
        }

        try
        {
            DoiAnh(AnhBangKeNgay.Ve(_bangKe, ThongTinCuaHang.DocTuMau(), _lucLap));
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không dựng được ảnh bảng kê:\n" + ex.Message);
            return;
        }

        _lblTrong.Visible = false;
        _xem.Visible = true;
        _btnChep.Enabled = true;
        _btnLuu.Enabled = true;
        _lblTrangThai.Text =
            $"Ngày {NgayDangXem:dd/MM/yyyy}: {_bangKe.Dong.Count} dòng hàng, " +
            $"tiền hàng {So.Tien(_bangKe.TienHang)} đ.";
    }

    private void DoiAnh(Bitmap? anhMoi)
    {
        _xem.Image = anhMoi;
        _anh?.Dispose();
        _anh = anhMoi;
    }

    /// <summary>
    /// Chép ảnh vào bộ nhớ máy để dán vào Zalo, đồng thời cất luôn một bản PNG cạnh file dữ liệu.
    /// <para>
    /// Bỏ vào bộ nhớ cả tấm ảnh lẫn đường dẫn file: Zalo trên máy tính nhận ảnh dán thẳng bằng
    /// Ctrl+V, còn phần mềm khác (mail, Word) lại chỉ nhận file đính kèm — có sẵn cả hai thì dán
    /// vào đâu cũng ra.
    /// </para>
    /// </summary>
    private void ChepAnh()
    {
        if (_anh is not { } anh || _bangKe is not { } bangKe)
        {
            return;
        }

        try
        {
            var duongDan = Path.Combine(ThuMucAnh(), AnhBangKeNgay.TenFile(bangKe));
            AnhBangKeNgay.LuuPng(anh, duongDan);

            var goi = new DataObject();
            goi.SetImage(anh);
            goi.SetFileDropList(new StringCollection { duongDan });

            // `copy: true`: giữ ảnh lại trong bộ nhớ máy cả sau khi đóng phần mềm, chứ không mất
            // ngay lúc thoát — người ta hay chép xong mới đi mở Zalo.
            Clipboard.SetDataObject(goi, copy: true);

            _lblTrangThai.Text = $"Đã chép ảnh. Mở Zalo, bấm Ctrl+V là ra. Bản lưu: {duongDan}";
            HopThoai.Bao(
                this,
                "Đã chép ảnh bảng kê vào bộ nhớ máy.\n\n" +
                "Mở Zalo, chọn khách rồi bấm Ctrl+V (hoặc chuột phải → Dán) là ảnh vào khung chat.\n\n" +
                "Một bản ảnh cũng đã lưu ở:\n" + duongDan);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(
                this,
                "Không chép được ảnh vào bộ nhớ máy:\n" + ex.Message +
                "\n\nDùng nút \"Lưu ảnh ra file...\" rồi kéo file vào Zalo cũng gửi được.");
        }
    }

    private void LuuRaFile()
    {
        if (_anh is not { } anh || _bangKe is not { } bangKe)
        {
            return;
        }

        using var hopThoai = new SaveFileDialog
        {
            Title = "Lưu ảnh bảng kê",
            Filter = "Ảnh PNG (*.png)|*.png",
            FileName = AnhBangKeNgay.TenFile(bangKe),
            InitialDirectory = ThuMucAnh(),
        };

        if (hopThoai.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            AnhBangKeNgay.LuuPng(anh, hopThoai.FileName);
            _lblTrangThai.Text = "Đã lưu ảnh: " + hopThoai.FileName;
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không lưu được ảnh:\n" + ex.Message);
        }
    }

    /// <summary>Thư mục cất ảnh bảng kê: cạnh file dữ liệu, để sao lưu cả thư mục là có luôn.</summary>
    private string ThuMucAnh()
    {
        var thuMucDuLieu = Path.GetDirectoryName(_kho.DuongDanFile);
        return string.IsNullOrEmpty(thuMucDuLieu)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), TenThuMucAnh)
            : Path.Combine(thuMucDuLieu, TenThuMucAnh);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        DoiAnh(null);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                Close();
                return true;
            case Keys.Control | Keys.C:
                ChepAnh();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
