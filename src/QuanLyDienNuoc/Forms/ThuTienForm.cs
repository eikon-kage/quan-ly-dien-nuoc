using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Khách đưa tiền: gõ số tiền là thấy ngay hoá đơn nào trừ bao nhiêu, bấm ghi một lần cho cả
/// loạt.
/// <para>
/// Ô <b>TRẢ CHO</b> gộp luôn việc "trả cho hoá đơn này" — trước đây là một cửa sổ riêng
/// (<c>ThanhToanForm</c>) với đúng ba ô ngày / số tiền / ghi chú y hệt màn này, chỉ khác chỗ
/// tiền ghi thẳng vào một hoá đơn thay vì chia từ hoá đơn cũ nhất. Hai cửa sổ cho cùng một
/// việc "khách trả tiền" thì chủ cửa hàng phải đoán bấm cái nào, nên nay còn một.
/// </para>
/// </summary>
public sealed class ThuTienForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;

    private readonly OChonNgay _dtNgay = new() { Font = Theme.FontNhap };

    // Ô tiền là ô duy nhất bắt buộc gõ trên màn này nên cho chữ to hơn hẳn mấy ô kia.
    private readonly TextBox _txtSoTien = Theme.O(240);
    private readonly TextBox _txtGhiChu = Theme.O(320);

    /// <summary>Trả cho cả sổ (chia từ hoá đơn cũ nhất) hay ghi thẳng vào một hoá đơn.</summary>
    private readonly ComboBox _cboHoaDon = new();

    /// <summary>Đang đổ lại danh sách hoá đơn, đừng tính lại bảng chia tiền giữa chừng.</summary>
    private bool _dangNap;

    // Giữ tham chiếu: ToolTip không được control nào giữ hộ, bị dọn rác là mất lời mách.
    private readonly ToolTip _mach = new() { InitialDelay = 250, AutoPopDelay = 8000 };

    private readonly DataGridView _luoiPhanBo = new();
    private readonly BindingList<DongPhanBo> _nguonPhanBo = new();

    private readonly DataGridView _luoiLichSu = new();
    private readonly BindingList<DongLanThu> _nguonLichSu = new();

    private readonly Label _lblTomTat = Theme.NhanDaiDong();
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    /// <summary>Nút mở / đóng danh sách các lần đã thu. Chữ trên nút đổi theo trạng thái.</summary>
    private readonly Button _btnLichSu = Theme.NutPhu("Xem các lần đã thu", 260, 38, noTheoChu: true);

    /// <summary>Cả khối lịch sử: bảng và nút xoá. Ẩn hẳn cho tới khi người dùng mở ra.</summary>
    private Control _khoiLichSu = null!;

    public ThuTienForm(Guid khachId)
    {
        _khachId = khachId;

        Text = "Thu tiền của khách";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1280, 780);
        MinimumSize = new Size(1060, 700);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    private KhachHang? Khach => _kho.TimKhach(_khachId);

    /// <summary>Mọi hoá đơn của khách, tính cả các năm trước — tiền trả cho nợ cũ trước.</summary>
    private List<HoaDon> HoaDons => _kho.HoaDonCuaKhach(_khachId);

    /// <summary>Hoá đơn người dùng chỉ đích danh ở ô TRẢ CHO; <c>null</c> là chia cho cả sổ.</summary>
    private Guid? HoaDonChon => (_cboHoaDon.SelectedItem as MucHoaDon)?.Id;

    // ---------------- Giao diện ----------------

    private void TaoGiaoDien()
    {
        /*
          Màn này chỉ để làm **một việc**: gõ số tiền khách đưa rồi ghi. Nên trên màn chỉ còn
          hàng ô nhập và bảng chia tiền cho các hoá đơn — hai thứ dùng ngay lúc gõ. Danh sách
          các lần đã thu trước đây là thứ thỉnh thoảng mới giở ra xem, trước đây chiếm hẳn một
          bảng nửa dưới màn hình, nay nằm sau một cái nút, mở ra mới hiện.
        */
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng chia tiền ăn phần còn lại: xem
        // "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "THU TIỀN CỦA KHÁCH",
                Khach is { } khach
                    ? $"{khach.Ten}  ·  tiền chia cho các hoá đơn còn nợ, cũ nhất trả trước"
                    : "Tiền chia cho các hoá đơn còn nợ, cũ nhất trả trước",
                tuCao: true),
            0,
            0);
        goc.Controls.Add(TaoThanhNhap(), 0, 1);
        goc.Controls.Add(TaoBangChiaTien(), 0, 2);
        goc.Controls.Add(TaoThanhMoLichSu(), 0, 3);

        _khoiLichSu = TaoKhoiLichSu();
        goc.Controls.Add(_khoiLichSu, 0, 4);

        goc.Controls.Add(TaoThanhDuoi(), 0, 5);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 6);

        Controls.Add(goc);
    }

    /// <summary>
    /// Hàng ô nhập — phần chính của màn hình. Ô tiền để chữ to (<see cref="Theme.FontNhapTo"/>)
    /// và cả hàng cao hơn hàng ô thường: đây là chỗ duy nhất phải gõ, mà gõ nhầm một số 0 là
    /// sai cả sổ công nợ.
    /// </summary>
    private Control TaoThanhNhap()
    {
        const int CaoO = 44;

        var btnGhi = Theme.Nut("GHI THU TIỀN", Theme.Xanh, 230, CaoO, noTheoChu: true);
        btnGhi.Click += (_, _) => Ghi();

        _dtNgay.Font = Theme.FontNhapTo;
        _txtSoTien.Font = Theme.FontNhapTo;
        _txtSoTien.TextAlign = HorizontalAlignment.Right;

        _cboHoaDon.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboHoaDon.Font = Theme.FontNhap;
        _cboHoaDon.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangNap)
            {
                CapNhatPhanBo();
            }
        };

        _mach.SetToolTip(
            _cboHoaDon,
            "Để \"các hoá đơn còn nợ\" thì tiền tự trừ từ hoá đơn cũ nhất.\n"
            + "Chọn đích danh một hoá đơn khi khách nói rõ trả cho tờ nào.");

        _mach.SetToolTip(_txtSoTien, "Gõ được cả phép tính, ví dụ: 2tr5, 1500+300");
        _mach.SetToolTip(_txtGhiChu, "Ví dụ: trả qua chuyển khoản, trả tiền mặt tại cửa hàng");

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

        var nhomNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 18, 0),
        };
        nhomNut.Controls.Add(btnGhi);

        return Theme.HangO(
            Theme.ChinhNhat,
            Theme.Truong("NGÀY THU", _dtNgay, 170, CaoO),
            Theme.Truong("SỐ TIỀN KHÁCH ĐƯA", _txtSoTien, 230, CaoO),
            Theme.Truong("TRẢ CHO", _cboHoaDon, 290, CaoO),
            Theme.Truong("GHI CHÚ", _txtGhiChu, 210, CaoO),
            nhomNut);
    }

    /// <summary>Bảng chia tiền: gõ tới đâu thấy ngay hoá đơn nào trừ bao nhiêu.</summary>
    private Control TaoBangChiaTien()
    {
        var than = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 8, 20, 0),
        };
        than.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        than.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        than.Controls.Add(Nhan("CHIA CHO CÁC HOÁ ĐƠN"), 0, 0);
        than.Controls.Add(Theme.Khung(TaoLuoiPhanBo()), 0, 1);
        return than;
    }

    /// <summary>
    /// Một hàng chỉ có cái nút mở danh sách các lần đã thu. Chữ trên nút nói luôn có bao nhiêu
    /// lần, để chưa mở cũng biết có gì bên trong mà mở.
    /// </summary>
    private Control TaoThanhMoLichSu()
    {
        _btnLichSu.ForeColor = Theme.Chinh;
        _btnLichSu.Margin = new Padding(0);
        _btnLichSu.Click += (_, _) => HienLichSu(!_khoiLichSu.Visible);

        var nen = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 10, 20, 0),
        };
        nen.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        nen.Controls.Add(_btnLichSu, 0, 0);
        return nen;
    }

    /// <summary>
    /// Khối lịch sử, ẩn sẵn. Bảng cao đúng sáu dòng chữ <b>của máy này</b> chứ không phải một
    /// con số điểm ảnh đặt tay — mở ra là xem lướt vài lần thu gần nhất, muốn xem kỹ thì kéo.
    /// Nút xoá nằm luôn trong khối: chưa mở danh sách thì cũng chẳng chọn được dòng nào để xoá.
    /// </summary>
    private Control TaoKhoiLichSu()
    {
        var btnXoa = Theme.NutPhu("Xoá lần thu đã chọn", 240, 40, noTheoChu: true);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Margin = new Padding(0, 8, 0, 0);
        btnXoa.Click += (_, _) => XoaLanThu();

        var oLuoi = new Panel
        {
            Dock = DockStyle.Top,
            Height = (Theme.FontLuoi.Height * 6) + 70,
            BackColor = Theme.Nen,
            Margin = new Padding(0),
        };
        oLuoi.Controls.Add(Theme.Khung(TaoLuoiLichSu()));

        var khoi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 8, 20, 0),
            Visible = false,
        };
        khoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khoi.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khoi.Controls.Add(oLuoi, 0, 0);
        khoi.Controls.Add(btnXoa, 0, 1);
        return khoi;
    }

    /// <summary>Mở hay đóng khối lịch sử, và viết lại chữ trên nút cho khớp.</summary>
    private void HienLichSu(bool hien)
    {
        _khoiLichSu.Visible = hien;
        CapNhatNutLichSu();
    }

    private void CapNhatNutLichSu()
    {
        var soLan = _nguonLichSu.Count;
        _btnLichSu.Enabled = soLan > 0;
        _btnLichSu.Text = soLan == 0
            ? "Khách chưa có lần thu tiền nào"
            : _khoiLichSu.Visible
                ? "Đóng danh sách các lần đã thu"
                : $"Xem {soLan} lần đã thu trước đây";
    }

    private static Label Nhan(string chu) => Theme.NhanDaiDong(chu, Theme.FontDam, Theme.Xam);

    private Control TaoLuoiPhanBo()
    {
        Theme.ApDungLuoi(_luoiPhanBo);
        _luoiPhanBo.ReadOnly = true;
        _luoiPhanBo.Columns.AddRange(
            Theme.Cot(nameof(DongPhanBo.Ma), "MÃ HĐ", 110),
            Theme.Cot(nameof(DongPhanBo.NgayMo), "MỞ NGÀY", 115, "dd/MM/yyyy", toiThieu: 104),
            Theme.Cot(nameof(DongPhanBo.TongTien), "TỔNG HĐ", 130, "#,##0", canPhai: true, toiThieu: 110),
            Theme.Cot(nameof(DongPhanBo.ConNo), "ĐANG NỢ", 130, "#,##0", canPhai: true, toiThieu: 110),
            Theme.Cot(nameof(DongPhanBo.TraLanNay), "TRẢ LẦN NÀY", 140, "#,##0", canPhai: true, toiThieu: 116),
            Theme.Cot(nameof(DongPhanBo.ConLaiSau), "CÒN LẠI SAU KHI TRẢ", 165, "#,##0", canPhai: true, toiThieu: 130));

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
            Theme.Cot(nameof(DongLanThu.Ngay), "NGÀY THU", 115, "dd/MM/yyyy", toiThieu: 104),
            Theme.Cot(nameof(DongLanThu.SoTien), "SỐ TIỀN", 130, "#,##0", canPhai: true, toiThieu: 110),
            Theme.Cot(nameof(DongLanThu.HoaDon), "CHIA CHO HOÁ ĐƠN", 240, toiThieu: 150),
            Theme.Cot(nameof(DongLanThu.GhiChu), "GHI CHÚ", 220, toiThieu: 120));

        _luoiLichSu.DataSource = _nguonLichSu;
        return _luoiLichSu;
    }

    /// <summary>
    /// Dải cuối: chỉ còn nút Đóng và dòng tiền. Nút "Xoá lần thu" chuyển vào khối lịch sử —
    /// nó chỉ có nghĩa khi đang nhìn danh sách ấy, để ngoài đây thì bấm vào là báo lỗi.
    /// </summary>
    private Control TaoThanhDuoi()
    {
        var btnDong = Theme.NutPhu("Đóng", 130, 46, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTomTat.Font = Theme.FontSo;

        return Theme.ThanhDuoi(_lblTomTat, btnDong);
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Text = "Enter để ghi · Esc để đóng · Ctrl+Z hoàn tác";
        return Theme.ThanhTrangThai(_lblTrangThai);
    }

    // ---------------- Nạp dữ liệu ----------------

    private void Nap()
    {
        if (Khach is null)
        {
            Close();
            return;
        }

        NapHoaDonDeChon();
        NapLichSu();
        CapNhatPhanBo();
        ActiveControl = _txtSoTien;
    }

    /// <summary>
    /// Đổ danh sách hoá đơn còn nợ vào ô TRẢ CHO, giữ nguyên lựa chọn cũ nếu hoá đơn ấy vẫn
    /// còn nợ. Trả xong hết một hoá đơn thì nó rời danh sách, ô quay về "các hoá đơn còn nợ".
    /// </summary>
    private void NapHoaDonDeChon()
    {
        var dangChon = HoaDonChon;

        _dangNap = true;
        _cboHoaDon.Items.Clear();
        _cboHoaDon.Items.Add(new MucHoaDon(null, "Các hoá đơn còn nợ  ·  cũ nhất trước"));

        // Tờ hoàn hàng không nhận tiền trả (nó là khoản cửa hàng nợ lại khách) nên không bày ra.
        foreach (var hoaDon in ThuTien.XepTuCuNhat(HoaDons).Where(h => !h.LaHoanHang && h.ConLai > 0m))
        {
            _cboHoaDon.Items.Add(new MucHoaDon(
                hoaDon.Id,
                $"{hoaDon.MaHoaDon}  ·  còn nợ {So.Tien(hoaDon.ConLai)}"));
        }

        var viTri = dangChon is { } id ? TimMuc(id) : 0;
        _cboHoaDon.SelectedIndex = viTri >= 0 ? viTri : 0;
        _dangNap = false;
    }

    private int TimMuc(Guid id)
    {
        for (var i = 0; i < _cboHoaDon.Items.Count; i++)
        {
            if (_cboHoaDon.Items[i] is MucHoaDon muc && muc.Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Những hoá đơn được nhận tiền lần này: cả sổ, hoặc đúng một tờ khi khách nói rõ trả cho
    /// hoá đơn nào.
    /// </summary>
    private List<HoaDon> HoaDonNhanTien(List<HoaDon> hoaDons) =>
        HoaDonChon is { } id ? hoaDons.Where(h => h.Id == id).ToList() : hoaDons;

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
        CapNhatNutLichSu();
    }

    /// <summary>Tính lại bảng chia tiền theo số đang gõ, chưa ghi gì vào sổ.</summary>
    private void CapNhatPhanBo()
    {
        var hoaDons = HoaDons;
        var soTien = So.Doc(_txtSoTien.Text);

        // Bảng vẫn bày **cả sổ** để thấy toàn cảnh nợ, nhưng tiền chỉ chia trong nhóm hoá đơn
        // được nhận: chọn đích danh một tờ thì cột TRẢ LẦN NÀY chỉ có số ở đúng tờ ấy.
        var ketQua = ThuTien.Chia(HoaDonNhanTien(hoaDons), soTien);
        var theoHoaDon = ketQua.PhanBo.ToDictionary(p => p.HoaDon.Id, p => p.SoTien);

        _nguonPhanBo.RaiseListChangedEvents = false;
        _nguonPhanBo.Clear();

        // Lấy cả tờ hoàn hàng (đang nợ âm) chứ không chỉ hoá đơn còn nợ: cộng cột ĐANG NỢ của
        // bảng này phải ra đúng con số "đang nợ" ở dòng tóm tắt, không thì chủ cửa hàng đọc hai
        // chỗ ra hai số rồi không biết tin số nào. Tiền được chia vào hoá đơn còn nợ, tờ hoàn
        // đứng đó cho thấy vì sao nợ ít hơn tổng các hoá đơn.
        foreach (var hoaDon in ThuTien.XepTuCuNhat(hoaDons).Where(h => h.ConLai != 0m))
        {
            var tra = theoHoaDon.GetValueOrDefault(hoaDon.Id);
            _nguonPhanBo.Add(new DongPhanBo
            {
                Ma = hoaDon.LaHoanHang
                    ? hoaDon.MaHoaDon + " (hoàn hàng)"
                    : hoaDon.DaChot ? hoaDon.MaHoaDon + " (chốt)" : hoaDon.MaHoaDon,
                NgayMo = hoaDon.NgayMo,
                TongTien = hoaDon.TongTien,
                ConNo = hoaDon.ConLai,
                TraLanNay = tra,
                ConLaiSau = hoaDon.ConLai - tra,
            });
        }

        _nguonPhanBo.RaiseListChangedEvents = true;
        _nguonPhanBo.ResetBindings();

        // Tên khách đã nằm ở phụ đề rồi, dòng này chỉ còn tiền: câu ngắn thì con số mới nổi.
        var tongNo = hoaDons.Sum(h => h.ConLai);
        _lblTomTat.Text = $"Đang nợ {So.Tien(tongNo)}"
                          + (soTien > 0 ? $"   ·   trả {So.Tien(ketQua.DaPhanBo)}, còn {So.Tien(tongNo - ketQua.DaPhanBo)}" : string.Empty)
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

        var nhanTien = HoaDonNhanTien(hoaDons);
        if (nhanTien.Count == 0)
        {
            HopThoai.CanhBao(this, "Hoá đơn vừa chọn không còn nợ. Chọn lại ở ô TRẢ CHO.");
            return;
        }

        var ketQua = ThuTien.Chia(nhanTien, soTien);

        if (ketQua.ConDu > 0m)
        {
            // Chọn đích danh một hoá đơn thì chỗ thừa cũng ghi vào đúng tờ ấy (thành trả
            // trước), chứ nhảy sang tờ khác là ghi vào chỗ khách không nhắc tới.
            var nhanDu = ThuTien.XepTuCuNhat(nhanTien)[^1];
            var conNo = HoaDonChon is null
                ? $"chỉ còn nợ {So.Tien(ketQua.DaPhanBo)}"
                : $"hoá đơn {nhanDu.MaHoaDon} chỉ còn nợ {So.Tien(ketQua.DaPhanBo)}";

            var traTruoc = HopThoai.Hoi(
                this,
                $"Khách đưa {So.Tien(soTien)} nhưng {conNo}, thừa {So.Tien(ketQua.ConDu)}.\n\n" +
                $"Ghi chỗ thừa vào hoá đơn {nhanDu.MaHoaDon} coi như trả trước?\n\n" +
                "Chọn Không nếu chỉ ghi đúng phần khách đang nợ.");

            if (traTruoc)
            {
                ketQua = ThuTien.Chia(nhanTien, soTien, ghiDuVaoHoaDonMoiNhat: true);
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
            HopThoai.CanhBao(this, "Hãy chọn một lần thu tiền trong danh sách để xoá.");
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

    /// <summary>Một mục trong ô TRẢ CHO: cả sổ (<see cref="Id"/> rỗng) hoặc một hoá đơn.</summary>
    private sealed record MucHoaDon(Guid? Id, string Chu)
    {
        // ComboBox lấy ToString() làm chữ hiện trong ô.
        public override string ToString() => Chu;
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
