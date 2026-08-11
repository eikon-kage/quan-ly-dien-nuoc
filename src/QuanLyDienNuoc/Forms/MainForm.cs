using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Màn hình chính: danh sách khách hàng theo năm, mở ra đơn hàng của từng khách.</summary>
public sealed class MainForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly BindingList<DongKhach> _nguon = new();

    private readonly ComboBox _cboNam = new();
    private readonly TextBox _txtTim = Theme.O(320);
    private readonly CheckBox _chkCoDon = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTongKet = new();
    private readonly Label _lblTrangThai = new();
    private readonly Label _lblPhimTat = new();
    private readonly Label _lblNhacNo = new();
    private readonly Panel _nenNhacNo = new();

    /// <summary>Thỉnh thoảng ngó lại file dữ liệu xem máy khác có sửa không.</summary>
    private readonly System.Windows.Forms.Timer _dongHoNgoFile = new() { Interval = 20_000 };

    private bool _dangNap;
    private bool _daBaoFileBiSua;

    public MainForm()
    {
        Text = _kho.ChiXem
            ? "Quản lý đơn hàng – Cửa hàng điện nước  [CHỈ XEM]"
            : "Quản lý đơn hàng – Cửa hàng điện nước";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 720);
        Size = new Size(1440, 860);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        _kho.DuLieuThayDoi += Kho_DuLieuThayDoi;
        _kho.ThaoTacBiChan += Kho_ThaoTacBiChan;
        FormClosed += (_, _) =>
        {
            _kho.DuLieuThayDoi -= Kho_DuLieuThayDoi;
            _kho.ThaoTacBiChan -= Kho_ThaoTacBiChan;
            _dongHoNgoFile.Stop();
        };

        _dongHoNgoFile.Tick += (_, _) => NgoFileDuLieu();
        _dongHoNgoFile.Start();

        NapNam();
        NapDanhSach();
    }

    private int NamDangChon => _cboNam.SelectedItem is int nam ? nam : DateTime.Today.Year;

    private KhachHang? KhachDangChon => (_luoi.CurrentRow?.DataBoundItem as DongKhach)?.Khach;

    // ---------------- Giao diện ----------------

    private void TaoGiaoDien()
    {
        // Không có thanh tiêu đề xanh như các cửa sổ con: đây là cửa sổ mở suốt ngày, tên
        // phần mềm đã nằm sẵn trên thanh cửa sổ của Windows, nhắc lại chỉ ăn mất một dải
        // màn hình mà không thêm thông tin gì.
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        goc.Controls.Add(TaoThanhCongCu(), 0, 0);
        goc.Controls.Add(TaoThanhNhacNo(), 0, 1);
        goc.Controls.Add(TaoLuoi(), 0, 2);
        goc.Controls.Add(TaoThanhDuoi(), 0, 3);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 4);

        Controls.Add(goc);
    }

    /// <summary>
    /// Thanh trên cùng: chọn năm, tìm khách, và các nút mở thêm cửa sổ. Nền trắng kẻ một
    /// vạch dưới để vẫn ra dáng thanh công cụ khi không còn dải tiêu đề đè lên trên.
    /// </summary>
    private Control TaoThanhCongCu()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Trang, Padding = new Padding(20, 14, 20, 10) };
        nen.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var but = new Pen(Theme.Vien);
            e.Graphics.DrawLine(but, 0, p.Height - 1, p.Width, p.Height - 1);
        };

        _cboNam.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNam.Font = Theme.FontNhap;
        _cboNam.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        _txtTim.TextChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        _chkCoDon.Text = "Chỉ hiện khách có đơn trong năm";
        _chkCoDon.Font = Theme.FontThuong;
        _chkCoDon.AutoSize = true;
        _chkCoDon.Margin = new Padding(4, 34, 0, 0);
        _chkCoDon.CheckedChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                NapDanhSach();
            }
        };

        var benTrai = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
        };
        benTrai.Controls.Add(Theme.Truong("NĂM", _cboNam, 130));
        benTrai.Controls.Add(Theme.Truong("TÌM KHÁCH HÀNG (tên, số điện thoại, địa chỉ)", _txtTim, 380));
        benTrai.Controls.Add(_chkCoDon);

        var btnTienIch = Theme.NutPhu("Tiện ích  ▾", 170);
        btnTienIch.Click += (s, _) => MoMenuTienIch((Control)s!);

        var btnThemKhach = Theme.Nut("+  Thêm khách hàng", Theme.Xanh, 220);
        btnThemKhach.Click += (_, _) => ThemKhach();

        var benPhai = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 22, 0, 0),
        };
        benPhai.Controls.Add(btnTienIch);
        benPhai.Controls.Add(btnThemKhach);

        nen.Controls.Add(benTrai);
        nen.Controls.Add(benPhai);
        return nen;
    }

    /// <summary>Dải nhắc nợ ngay dưới thanh công cụ: mở phần mềm lên là thấy ai đang nợ lâu.</summary>
    private Control TaoThanhNhacNo()
    {
        _nenNhacNo.Dock = DockStyle.Fill;
        _nenNhacNo.Padding = new Padding(20, 6, 20, 6);
        _nenNhacNo.BackColor = Theme.Nen;

        _lblNhacNo.Dock = DockStyle.Fill;
        _lblNhacNo.Font = Theme.FontDam;
        _lblNhacNo.TextAlign = ContentAlignment.MiddleLeft;
        _lblNhacNo.Padding = new Padding(14, 0, 0, 0);

        var btnSoCongNo = Theme.Nut("MỞ SỔ CÔNG NỢ", Theme.Cam, 230, 44);
        btnSoCongNo.Dock = DockStyle.Right;
        btnSoCongNo.Margin = new Padding(0);
        btnSoCongNo.Click += (_, _) => MoSoCongNo();

        _nenNhacNo.Controls.Add(_lblNhacNo);
        _nenNhacNo.Controls.Add(btnSoCongNo);
        return _nenNhacNo;
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongKhach.Ten), "KHÁCH HÀNG", 200),
            Theme.Cot(nameof(DongKhach.DienThoai), "ĐIỆN THOẠI", 110),
            Theme.Cot(nameof(DongKhach.DiaChi), "ĐỊA CHỈ", 190),
            Theme.Cot(nameof(DongKhach.SoHoaDon), "SỐ HĐ", 70, canPhai: true),
            Theme.Cot(nameof(DongKhach.TongTien), "TỔNG MUA", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKhach.DaTra), "ĐÃ TRẢ", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKhach.ConLai), "CÒN NỢ", 130, "#,##0", canPhai: true));

        _luoi.DataSource = _nguon;
        _luoi.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                MoDonHang();
            }
        };
        _luoi.CellFormatting += Luoi_CellFormatting;

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 10), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        return vien;
    }

    private Control TaoThanhDuoi()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 8, 20, 10) };

        var btnMo = Theme.Nut("MỞ ĐƠN HÀNG", Theme.Chinh, 230, 52);
        btnMo.Click += (_, _) => MoDonHang();

        var btnThuTien = Theme.Nut("THU TIỀN", Theme.Xanh, 180, 52);
        btnThuTien.Click += (_, _) => ThuTienCuaKhach();

        var btnSua = Theme.NutPhu("Sửa khách", 150, 52);
        btnSua.Click += (_, _) => SuaKhach();

        var btnXoa = Theme.NutPhu("Xoá khách", 150, 52);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => XoaKhach();

        var trai = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
        };
        trai.Controls.Add(btnMo);
        trai.Controls.Add(btnThuTien);
        trai.Controls.Add(btnSua);
        trai.Controls.Add(btnXoa);

        _lblTongKet.Dock = DockStyle.Right;
        _lblTongKet.TextAlign = ContentAlignment.MiddleRight;
        _lblTongKet.Font = Theme.FontSo;
        _lblTongKet.ForeColor = Theme.Chu;
        _lblTongKet.AutoSize = false;
        _lblTongKet.Width = 640;

        nen.Controls.Add(trai);
        nen.Controls.Add(_lblTongKet);
        return nen;
    }

    /// <summary>
    /// Thanh dưới cùng chia đôi: bên trái là câu báo việc vừa làm (bị viết đè liên tục),
    /// bên phải là mấy phím tắt, cố định không ai đè lên. Từ khi bỏ hai nút Hoàn tác /
    /// Làm lại thì Ctrl+Z và Ctrl+Y chỉ còn được nhắc ở đây, mà nhắc thì phải nhắc suốt —
    /// để chung một dòng thì thêm một khách hàng là câu nhắc bay mất.
    /// </summary>
    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);

        _lblPhimTat.Dock = DockStyle.Right;
        // Tự co theo chữ: đặt cứng bề rộng thì máy để cỡ chữ Windows lớn là cụt mất phím tắt.
        _lblPhimTat.AutoSize = true;
        _lblPhimTat.Font = Theme.FontPhu;
        _lblPhimTat.ForeColor = Theme.Xam;
        _lblPhimTat.TextAlign = ContentAlignment.MiddleRight;
        _lblPhimTat.Padding = new Padding(0, 0, 22, 0);
        _lblPhimTat.Text = _kho.ChiXem
            ? "Bấm đúp dòng khách để xem đơn hàng · F5 nạp lại · F6 sổ công nợ"
            : "Bấm đúp dòng khách để mở đơn hàng · Ctrl+Z hoàn tác · Ctrl+Y làm lại · F5 nạp lại · F6 sổ công nợ";

        if (_kho.ChiXem)
        {
            _lblTrangThai.ForeColor = Theme.Do;
            _lblTrangThai.Text = $"CHỈ XEM — {_kho.LyDoChiXem} · Dữ liệu: {_kho.DuongDanFile}";
        }
        else
        {
            _lblTrangThai.Text = $"Dữ liệu: {_kho.DuongDanFile}";
        }

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        nen.Controls.Add(_lblPhimTat);
        return nen;
    }

    // ---------------- Nạp dữ liệu ----------------

    private void NapNam()
    {
        var namCu = _cboNam.SelectedItem as int?;
        _dangNap = true;
        _cboNam.Items.Clear();
        foreach (var nam in _kho.DanhSachNam())
        {
            _cboNam.Items.Add(nam);
        }

        var can = namCu ?? DateTime.Today.Year;
        var viTri = _cboNam.Items.IndexOf(can);
        _cboNam.SelectedIndex = viTri >= 0 ? viTri : 0;
        _dangNap = false;
    }

    private void NapDanhSach()
    {
        var dangChon = KhachDangChon?.Id;
        var nam = NamDangChon;
        var tuKhoa = _txtTim.Text;

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var khach in _kho.DuLieu.KhachHangs.OrderBy(k => k.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            if (!ChuViet.Chua(khach.Ten, tuKhoa)
                && !ChuViet.Chua(khach.DienThoai, tuKhoa)
                && !ChuViet.Chua(khach.DiaChi, tuKhoa))
            {
                continue;
            }

            var hoaDons = _kho.DuLieu.HoaDons.Where(h => h.KhachHangId == khach.Id && h.Nam == nam).ToList();
            if (_chkCoDon.Checked && hoaDons.Count == 0)
            {
                continue;
            }

            var tong = hoaDons.Sum(h => h.TongTien);
            var daTra = hoaDons.Sum(h => h.DaThanhToan);

            _nguon.Add(new DongKhach
            {
                Khach = khach,
                Ten = khach.Ten,
                DienThoai = khach.DienThoai,
                DiaChi = khach.DiaChi,
                SoHoaDon = hoaDons.Count,
                TongTien = tong,
                DaTra = daTra,
                ConLai = tong - daTra,
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            ChonLaiKhach(id);
        }

        var tongMua = _nguon.Sum(d => d.TongTien);
        var tongTra = _nguon.Sum(d => d.DaTra);
        _lblTongKet.Text =
            $"Năm {nam}   ·   {_nguon.Count} khách   ·   Tổng mua: {So.Tien(tongMua)}   ·   Còn nợ: {So.Tien(tongMua - tongTra)}";

        CapNhatNhacNo();
    }

    /// <summary>Tính lại dải nhắc nợ trên đầu màn hình (tính tất cả các năm, không riêng năm đang xem).</summary>
    private void CapNhatNhacNo()
    {
        var soNgay = _kho.CaiDat.SoNgayNhacNo;
        var congNo = CongNo.Tinh(_kho.DuLieu, nam: null, DateTime.Today);
        var quaHan = CongNo.QuaHan(congNo, soNgay);

        if (quaHan.Count > 0)
        {
            var lauNhat = quaHan[0];
            _nenNhacNo.BackColor = Color.FromArgb(255, 243, 224);
            _lblNhacNo.ForeColor = Color.FromArgb(150, 75, 0);
            _lblNhacNo.Text =
                $"⚠  {quaHan.Count} khách nợ quá {soNgay} ngày — tổng {So.Tien(quaHan.Sum(d => d.ConNo))}." +
                $"   Lâu nhất: {lauNhat.Khach.Ten} ({lauNhat.SoNgayNo} ngày, {So.Tien(lauNhat.ConNo)}).";
        }
        else if (congNo.Count > 0)
        {
            _nenNhacNo.BackColor = Theme.ChinhNhat;
            _lblNhacNo.ForeColor = Theme.Chinh;
            _lblNhacNo.Text =
                $"{congNo.Count} khách đang nợ, tổng {So.Tien(congNo.Sum(d => d.ConNo))} — chưa có ai quá {soNgay} ngày.";
        }
        else
        {
            _nenNhacNo.BackColor = Color.FromArgb(232, 245, 233);
            _lblNhacNo.ForeColor = Theme.Xanh;
            _lblNhacNo.Text = "Không có khách nào đang nợ.";
        }
    }

    private void ChonLaiKhach(Guid id)
    {
        for (var i = 0; i < _luoi.Rows.Count; i++)
        {
            if (_luoi.Rows[i].DataBoundItem is DongKhach dong && dong.Khach.Id == id)
            {
                _luoi.CurrentCell = _luoi.Rows[i].Cells[0];
                return;
            }
        }
    }

    private void Kho_DuLieuThayDoi(object? sender, EventArgs e)
    {
        _daBaoFileBiSua = false;
        NapNam();
        NapDanhSach();
    }

    private void Kho_ThaoTacBiChan(object? sender, EventArgs e) => HopThoai.CanhBao(
        Form.ActiveForm ?? this,
        $"Đang mở ở chế độ CHỈ XEM nên không sửa được gì.\n\n{_kho.LyDoChiXem}.\n\n" +
        "Đóng phần mềm ở máy kia rồi mở lại là sửa được bình thường.");

    /// <summary>
    /// Máy khác vừa sửa file trong lúc mình đang mở: báo ngay ở thanh dưới để khỏi ngồi
    /// nhập tiếp trên số liệu cũ. Đang chỉ xem thì mời nạp lại luôn cho khỏi lạc hậu.
    /// </summary>
    private void NgoFileDuLieu()
    {
        if (_daBaoFileBiSua || !_kho.FileBiMayKhacSua())
        {
            return;
        }

        _daBaoFileBiSua = true;
        _lblTrangThai.ForeColor = Theme.Do;
        _lblTrangThai.Text =
            "⚠  File dữ liệu vừa bị máy khác sửa. Bấm F5 để nạp lại bản mới nhất trước khi nhập tiếp.";
    }

    private void NapLaiTuFile()
    {
        if (!_kho.FileBiMayKhacSua() && !_kho.ChiXem)
        {
            _lblTrangThai.Text = "File dữ liệu vẫn đúng bản đang mở, không cần nạp lại.";
            return;
        }

        _kho.NapLaiTuFile();
        _daBaoFileBiSua = false;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.Text = "Đã nạp lại dữ liệu mới nhất từ file.";
    }

    private void Luoi_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (e.CellStyle is not { } kieu)
        {
            return;
        }

        var thuocTinh = _luoi.Columns[e.ColumnIndex].DataPropertyName;
        if (thuocTinh == nameof(DongKhach.ConLai) && e.Value is decimal conLai)
        {
            kieu.Font = Theme.FontLuoiDam;
            kieu.ForeColor = conLai > 0 ? Theme.Do : Theme.Xam;
        }
        else if (thuocTinh == nameof(DongKhach.Ten))
        {
            kieu.Font = Theme.FontLuoiDam;
        }
    }

    // ---------------- Thao tác ----------------

    private void MoDonHang()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng trong danh sách.");
            return;
        }

        using var form = new DonHangForm(khach.Id, NamDangChon);
        form.ShowDialog(this);
    }

    private void ThuTienCuaKhach()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn khách hàng vừa đưa tiền.");
            return;
        }

        using var form = new ThuTienForm(khach.Id);
        form.ShowDialog(this);
        NapDanhSach();
        _lblTrangThai.Text = $"Đã cập nhật tiền của {khach.Ten}.";
    }

    private void ThemKhach()
    {
        using var form = new KhachHangForm(null);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } moi)
        {
            return;
        }

        // Dễ tạo trùng một người thành hai khách rồi chia đôi công nợ, nên hỏi lại trước.
        if (KiemTra.KhachTrungTen(_kho.DuLieu.KhachHangs, moi.Ten) is { } daCo
            && !HopThoai.Hoi(
                this,
                $"Đã có khách \"{daCo.Ten}\"" +
                (string.IsNullOrWhiteSpace(daCo.DienThoai) ? string.Empty : $" (ĐT {daCo.DienThoai})") +
                (string.IsNullOrWhiteSpace(daCo.DiaChi) ? string.Empty : $" — {daCo.DiaChi}") +
                ".\n\nVẫn thêm một khách nữa cùng tên?"))
        {
            ChonLaiKhach(daCo.Id);
            _lblTrangThai.Text = $"Đã có sẵn khách {daCo.Ten}, không thêm mới.";
            return;
        }

        _kho.ThucHien($"Thêm khách hàng {moi.Ten}", () => _kho.DuLieu.KhachHangs.Add(moi), phatSuKien: false);
        NapDanhSach();
        ChonLaiKhach(moi.Id);
        _lblTrangThai.Text = $"Đã thêm khách hàng {moi.Ten}.";
    }

    private void SuaKhach()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng để sửa.");
            return;
        }

        using var form = new KhachHangForm(khach);
        if (form.ShowDialog(this) != DialogResult.OK || form.KetQua is not { } sua)
        {
            return;
        }

        _kho.ThucHien($"Sửa khách hàng {sua.Ten}", () =>
        {
            khach.Ten = sua.Ten;
            khach.DienThoai = sua.DienThoai;
            khach.DiaChi = sua.DiaChi;
            khach.GhiChu = sua.GhiChu;
        }, phatSuKien: false);

        NapDanhSach();
        _lblTrangThai.Text = $"Đã cập nhật khách hàng {khach.Ten}.";
    }

    private void XoaKhach()
    {
        if (KhachDangChon is not { } khach)
        {
            HopThoai.CanhBao(this, "Hãy chọn một khách hàng để xoá.");
            return;
        }

        var soHoaDon = _kho.DuLieu.HoaDons.Count(h => h.KhachHangId == khach.Id);
        var canhBao = soHoaDon > 0
            ? $"\n\nKhách này đang có {soHoaDon} hoá đơn, xoá khách sẽ xoá luôn các hoá đơn đó."
            : string.Empty;

        if (!HopThoai.Hoi(this, $"Xoá khách hàng \"{khach.Ten}\"?{canhBao}\n\n(Có thể bấm Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá khách hàng {khach.Ten}", () =>
        {
            _kho.DuLieu.HoaDons.RemoveAll(h => h.KhachHangId == khach.Id);
            _kho.DuLieu.KhachHangs.Remove(khach);
        }, phatSuKien: false);

        NapDanhSach();
        _lblTrangThai.Text = $"Đã xoá khách hàng {khach.Ten}. Bấm Ctrl+Z để lấy lại.";
    }

    private void MoDanhMucVatTu()
    {
        using var form = new VatTuForm();
        form.ShowDialog(this);
    }

    private void MoSoCongNo()
    {
        using var form = new CongNoForm(NamDangChon);
        form.ShowDialog(this);
        NapDanhSach();
    }

    private void MoMenuTienIch(Control nut)
    {
        var menu = new ContextMenuStrip { Font = Theme.FontThuong };
        menu.Items.Add("Sổ công nợ", null, (_, _) => MoSoCongNo());
        menu.Items.Add("Danh mục vật tư", null, (_, _) => MoDanhMucVatTu());
        menu.Items.Add("Bộ hàng thường dùng", null, (_, _) =>
        {
            using var form = new BoHangForm();
            form.ShowDialog(this);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sao lưu và khôi phục", null, (_, _) =>
        {
            using var form = new SaoLuuForm();
            form.ShowDialog(this);
            NapDanhSach();
        });
        menu.Items.Add("Nhật ký thay đổi", null, (_, _) =>
        {
            using var form = new NhatKyForm();
            form.ShowDialog(this);
        });

        menu.Show(nut, new Point(0, nut.Height));
    }

    private void HoanTac()
    {
        var moTa = _kho.HoanTac();
        _lblTrangThai.Text = moTa is null
            ? "Không còn thao tác nào để hoàn tác."
            : $"Đã hoàn tác: {moTa}   (Ctrl+Y để làm lại)";
    }

    private void LamLai()
    {
        var moTa = _kho.LamLai();
        _lblTrangThai.Text = moTa is null
            ? "Không còn thao tác nào để làm lại."
            : $"Đã làm lại: {moTa}";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Z:
                HoanTac();
                return true;
            case Keys.Control | Keys.Y:
                LamLai();
                return true;
            case Keys.Control | Keys.N:
                ThemKhach();
                return true;
            case Keys.F3:
                _txtTim.Focus();
                _txtTim.SelectAll();
                return true;
            case Keys.F5:
                NapLaiTuFile();
                return true;
            case Keys.F6:
                MoSoCongNo();
                return true;
            case Keys.Enter when _luoi.Focused:
                MoDonHang();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Một dòng khách hàng trên lưới, kèm số liệu của năm đang xem.</summary>
    private sealed class DongKhach
    {
        public KhachHang Khach { get; set; } = null!;

        public string Ten { get; set; } = string.Empty;

        public string DienThoai { get; set; } = string.Empty;

        public string DiaChi { get; set; } = string.Empty;

        public int SoHoaDon { get; set; }

        public decimal TongTien { get; set; }

        public decimal DaTra { get; set; }

        public decimal ConLai { get; set; }
    }
}
