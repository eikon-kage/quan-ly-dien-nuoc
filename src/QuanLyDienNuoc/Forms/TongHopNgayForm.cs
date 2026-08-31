using System.Collections.Specialized;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Cuối buổi, gom hàng một khách đã lấy trong ngày thành ảnh bảng kê rồi chép vào bộ nhớ máy để
/// dán thẳng sang Zalo cho khách xem lại. Bảng kê chỉ ghi tên hàng và số lượng, không ghi giá.
/// <para>
/// Hai nút <b>Hôm nay</b> / <b>Hôm qua</b> để ngay đầu màn: quá nửa số lần dùng là hai ngày ấy
/// (chốt sổ cuối buổi, hoặc sáng hôm sau mới nhớ ra chưa gửi). Muốn ngày khác thì vẫn có ô lịch
/// bên cạnh.
/// </para>
/// <para>
/// Ngày khách lấy nhiều hàng thì bảng kê dài quá một tấm ảnh, phần mềm cắt ra nhiều tấm và
/// thanh <i>Ảnh trước / Ảnh sau</i> hiện ra để xem lần lượt. Chép hay lưu thì được cả bộ.
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
    private readonly Label _lblSoAnh = Theme.Nhan("", Theme.FontNhan, Theme.ChuDam);

    /// <summary>Ảnh của từng trang bảng kê, theo đúng thứ tự gửi cho khách.</summary>
    private readonly List<Bitmap> _anhs = new();

    private Control _thanhTrang = null!;
    private Button _btnTruoc = null!;
    private Button _btnSau = null!;
    private Button _btnChep = null!;
    private Button _btnLuu = null!;

    private BangKeNgay? _bangKe;
    private int _trang;
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
            RowCount = 6,
            BackColor = Theme.Nen,
        };
        // Mọi dải chữ tự cao theo cỡ chữ, chỉ khung xem ảnh ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "TỔNG HỢP HÀNG TRONG NGÀY",
                $"{Khach?.Ten} — ảnh bảng kê để gửi Zalo: chỉ tên hàng và số lượng, không có giá",
                tuCao: true),
            0,
            0);
        khung.Controls.Add(TaoThanhChonNgay(), 0, 1);
        khung.Controls.Add(TaoKhungXem(), 0, 2);

        _thanhTrang = TaoThanhTrang();
        khung.Controls.Add(_thanhTrang, 0, 3);

        _btnChep = Theme.Nut("CHÉP ẢNH ĐỂ DÁN VÀO ZALO", Theme.Chinh, 320, 52, noTheoChu: true);
        _btnChep.Click += (_, _) => ChepAnh();

        _btnLuu = Theme.NutPhu("Lưu ảnh ra file...", 200, 52, noTheoChu: true);
        _btnLuu.Click += (_, _) => LuuRaFile();

        var btnDong = Theme.NutPhu("Đóng", 130, 52, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        khung.Controls.Add(Theme.ThanhDuoi(null, _btnChep, _btnLuu, btnDong), 0, 4);
        khung.Controls.Add(Theme.ThanhTrangThai(_lblTrangThai), 0, 5);
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

    /// <summary>
    /// Thanh lật ảnh, chỉ hiện khi bảng kê dài phải cắt ra nhiều tấm — một tấm mà cũng bày hai
    /// nút bấm không được thì người dùng lại đi tìm xem mình làm sai chỗ nào.
    /// </summary>
    private Control TaoThanhTrang()
    {
        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 2, 20, 2),
            Visible = false,
        };

        _btnTruoc = Theme.NutPhu("‹ ẢNH TRƯỚC", 170, 44, noTheoChu: true);
        _btnTruoc.Click += (_, _) => LatAnh(-1);

        _btnSau = Theme.NutPhu("ẢNH SAU ›", 170, 44, noTheoChu: true);
        _btnSau.Click += (_, _) => LatAnh(1);

        _lblSoAnh.AutoSize = true;
        _lblSoAnh.Margin = new Padding(16, 12, 16, 0);

        hang.Controls.Add(_btnTruoc);
        hang.Controls.Add(_lblSoAnh);
        hang.Controls.Add(_btnSau);
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

        _bangKe = TongHopNgay.Lam(khach, _kho.HoaDonCuaKhach(khach.Id), NgayDangXem, mocNo: _homNay);

        DoiAnh(null);

        if (_bangKe.Dong.Count == 0)
        {
            // Trước đây chỉ chặn khi ngày ấy vừa không có hàng vừa không có tiền trả. Giờ bảng
            // kê không ghi tiền nữa, nên ngày chỉ có phiếu thu tiền cũng không dựng được tấm ảnh
            // nào có nội dung.
            _lblTrong.Text =
                $"Ngày {NgayDangXem:dd/MM/yyyy} khách {khach.Ten} không lấy hàng.\n\n" +
                "Chọn ngày khác ở trên.";
            _lblTrong.Visible = true;
            _xem.Visible = false;
            _btnChep.Enabled = false;
            _btnLuu.Enabled = false;
            _lblTrangThai.Text = $"Ngày {NgayDangXem:dd/MM/yyyy}: không có hàng để gửi khách.";
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

        var soAnh = _anhs.Count > 1 ? $", cắt ra {_anhs.Count} tấm ảnh" : string.Empty;
        _lblTrangThai.Text = $"Ngày {NgayDangXem:dd/MM/yyyy}: {_bangKe.Dong.Count} dòng hàng{soAnh}.";
    }

    /// <summary>Thay cả bộ ảnh đang giữ; truyền null là dọn sạch.</summary>
    private void DoiAnh(List<Bitmap>? anhMoi)
    {
        _xem.Image = null;
        foreach (var anh in _anhs)
        {
            anh.Dispose();
        }

        _anhs.Clear();
        if (anhMoi is not null)
        {
            _anhs.AddRange(anhMoi);
        }

        _trang = 0;
        BayAnhDangXem();
    }

    private void LatAnh(int buoc)
    {
        _trang = Math.Clamp(_trang + buoc, 0, Math.Max(0, _anhs.Count - 1));
        BayAnhDangXem();
    }

    private void BayAnhDangXem()
    {
        _xem.Image = _trang < _anhs.Count ? _anhs[_trang] : null;

        _thanhTrang.Visible = _anhs.Count > 1;
        _lblSoAnh.Text = $"Ảnh {_trang + 1} / {_anhs.Count}";
        _btnTruoc.Enabled = _trang > 0;
        _btnSau.Enabled = _trang < _anhs.Count - 1;
    }

    /// <summary>
    /// Chép ảnh vào bộ nhớ máy để dán vào Zalo, đồng thời cất luôn một bản PNG cạnh file dữ liệu.
    /// <para>
    /// Bỏ vào bộ nhớ cả tấm ảnh lẫn đường dẫn file: Zalo trên máy tính nhận ảnh dán thẳng bằng
    /// Ctrl+V, còn phần mềm khác (mail, Word) lại chỉ nhận file đính kèm — có sẵn cả hai thì dán
    /// vào đâu cũng ra. Bảng kê cắt ra nhiều tấm thì danh sách file có đủ cả bộ (dán một lần ra
    /// hết), còn ảnh dán thẳng là tấm đang xem.
    /// </para>
    /// </summary>
    private void ChepAnh()
    {
        if (_anhs.Count == 0 || _bangKe is not { } bangKe)
        {
            return;
        }

        try
        {
            var duongDans = LuuCaBo(bangKe, ThuMucAnh());

            var danhSachFile = new StringCollection();
            foreach (var duongDan in duongDans)
            {
                danhSachFile.Add(duongDan);
            }

            var goi = new DataObject();
            goi.SetImage(_anhs[Math.Min(_trang, _anhs.Count - 1)]);
            goi.SetFileDropList(danhSachFile);

            // `copy: true`: giữ ảnh lại trong bộ nhớ máy cả sau khi đóng phần mềm, chứ không mất
            // ngay lúc thoát — người ta hay chép xong mới đi mở Zalo.
            Clipboard.SetDataObject(goi, copy: true);

            var thuMuc = Path.GetDirectoryName(duongDans[0]) ?? ThuMucAnh();
            if (_anhs.Count == 1)
            {
                _lblTrangThai.Text = $"Đã chép ảnh. Mở Zalo, bấm Ctrl+V là ra. Bản lưu: {duongDans[0]}";
                HopThoai.Bao(
                    this,
                    "Đã chép ảnh bảng kê vào bộ nhớ máy.\n\n" +
                    "Mở Zalo, chọn khách rồi bấm Ctrl+V (hoặc chuột phải → Dán) là ảnh vào khung chat.\n\n" +
                    "Một bản ảnh cũng đã lưu ở:\n" + duongDans[0]);
            }
            else
            {
                _lblTrangThai.Text =
                    $"Đã chép {_anhs.Count} ảnh (bảng kê dài, cắt ra nhiều tấm). Bản lưu ở: {thuMuc}";
                HopThoai.Bao(
                    this,
                    $"Bảng kê ngày này dài, phần mềm cắt ra {_anhs.Count} tấm ảnh.\n\n" +
                    $"Đã chép cả {_anhs.Count} tấm vào bộ nhớ máy: mở Zalo, chọn khách rồi bấm Ctrl+V — " +
                    "Zalo nhận đủ cả bộ, gửi lần lượt theo đúng thứ tự.\n\n" +
                    "Zalo bản cũ chỉ nhận một tấm thì mở thư mục dưới đây rồi kéo lần lượt từng " +
                    "file vào khung chat (tên file có đánh số trang):\n" + thuMuc);
            }
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
        if (_anhs.Count == 0 || _bangKe is not { } bangKe)
        {
            return;
        }

        using var hopThoai = new SaveFileDialog
        {
            Title = _anhs.Count > 1 ? $"Lưu {_anhs.Count} ảnh bảng kê" : "Lưu ảnh bảng kê",
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
            if (_anhs.Count == 1)
            {
                AnhBangKeNgay.LuuPng(_anhs[0], hopThoai.FileName);
                _lblTrangThai.Text = "Đã lưu ảnh: " + hopThoai.FileName;
                return;
            }

            // Nhiều tấm: lấy tên người dùng vừa gõ làm gốc rồi đánh số từng tấm, chứ không đè
            // cả bộ vào một file.
            var thuMuc = Path.GetDirectoryName(hopThoai.FileName) ?? ThuMucAnh();
            var goc = Path.GetFileNameWithoutExtension(hopThoai.FileName);
            for (var i = 0; i < _anhs.Count; i++)
            {
                AnhBangKeNgay.LuuPng(
                    _anhs[i],
                    Path.Combine(thuMuc, $"{goc} (trang {i + 1} trong {_anhs.Count}).png"));
            }

            _lblTrangThai.Text = $"Đã lưu {_anhs.Count} ảnh vào: {thuMuc}";
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không lưu được ảnh:\n" + ex.Message);
        }
    }

    /// <summary>Lưu cả bộ ảnh vào một thư mục và trả về đường dẫn từng file, theo đúng thứ tự.</summary>
    private List<string> LuuCaBo(BangKeNgay bangKe, string thuMuc)
    {
        var duongDans = new List<string>(_anhs.Count);
        for (var i = 0; i < _anhs.Count; i++)
        {
            var duongDan = Path.Combine(thuMuc, AnhBangKeNgay.TenFile(bangKe, i, _anhs.Count));
            AnhBangKeNgay.LuuPng(_anhs[i], duongDan);
            duongDans.Add(duongDan);
        }

        return duongDans;
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

            // Lật ảnh bằng PageUp/PageDown, nhưng nhường lại cho ô chọn ngày khi con trỏ đang
            // ở trong đó: ở ô ngày, hai phím ấy là chỉnh tháng.
            case Keys.PageUp when !_dtNgay.ContainsFocus:
                LatAnh(-1);
                return true;
            case Keys.PageDown when !_dtNgay.ContainsFocus:
                LatAnh(1);
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
