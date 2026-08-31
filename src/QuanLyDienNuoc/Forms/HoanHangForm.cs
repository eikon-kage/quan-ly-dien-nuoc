using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Lập hoá đơn hoàn hàng cho một hoá đơn bán: bảng trống, nhập từng món khách mang trả về.
/// Hoá đơn gốc không bị sửa một chữ — tờ hoàn là chứng từ riêng, hoàn cho nó — nên hoá đơn đã
/// in cho khách hay đã chốt vẫn hoàn được.
/// <para>
/// Nhập <b>y như màn đơn hàng</b>: một thanh nhập ở trên (TÊN HÀNG · ĐƠN VỊ · ĐƠN GIÁ · SỐ HOÀN
/// · TIỀN HOÀN), gõ tên hàng rồi Enter là sang ô số hoàn, Enter nữa là món xuống bảng. Đơn vị
/// và đơn giá tự điền, ô số nhận cả phép tính. Bảng dưới vẫn sửa được và vẫn còn dòng trống
/// cuối để gõ thẳng, nên ai quen lối cũ không mất gì.
/// </para>
/// <para>
/// Trước đây màn này bày sẵn từng dòng của hoá đơn gốc và chỉ cho gõ số hoàn vào những dòng
/// ấy. Nay tờ hoàn nhập riêng: khách trả về món đổi từ lần khác, hay hai bên thoả lại giá lúc
/// hoàn, thì tờ hoàn vẫn ghi đúng những gì đã bàn. Đổi lại, phần mềm không còn tự chặn hoàn
/// quá số đã bán — người lập tự soát. Gõ tên món đã bán cho khách thì đơn vị và giá vẫn tự
/// điền theo giá đã bán, khỏi phải nhớ.
/// </para>
/// </summary>
public sealed class HoanHangForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _hoaDonGocId;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<ChiTietHoaDon> _nguon = new();

    // Thanh nhập một món hoàn, xếp và bấm y như thanh nhập nhanh của màn đơn hàng.
    private readonly ComboBox _cboHang = new();
    private readonly TextBox _txtDonVi = Theme.O(120);
    private readonly TextBox _txtDonGia = Theme.O(150);
    private readonly TextBox _txtSoHoan = Theme.O(120);
    private readonly Label _lblTamTinh = new();

    // Món để gợi ý khi gõ tên: món trên hoá đơn gốc trước, rồi tới cả danh mục vật tư.
    private readonly List<MonGoiY> _monGoiY = new();

    // Gõ tên hàng thẳng trên lưới thì vẫn có gợi ý, lấy đúng danh sách trên.
    private readonly AutoCompleteStringCollection _goiYTenHang = new();

    private readonly OChonNgay _dtNgay = new() { Font = Theme.FontNhap };

    private readonly TextBox _txtLyDo = Theme.O(420);
    private readonly Label _lblTong = Theme.NhanDaiDong();

    // Giữ tham chiếu: ToolTip không được control nào giữ hộ, bị dọn rác là mất lời mách.
    private readonly ToolTip _mach = new() { InitialDelay = 250, AutoPopDelay = 10000 };
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    /// <summary>Đang tự điền vào các ô nhập — đừng để sự kiện của chính mình gọi vòng lại.</summary>
    private bool _dangNap;

    public HoanHangForm(Guid hoaDonGocId)
    {
        _hoaDonGocId = hoaDonGocId;

        Text = "Hoàn hàng";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1160, 700);
        MinimumSize = new Size(1040, 620);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    /// <summary>Hoá đơn hoàn hàng vừa lập, để màn đơn hàng nhảy sang xem luôn tờ đó.</summary>
    public Guid? HoaDonHoanDaTao { get; private set; }

    /// <summary>
    /// Mở cửa sổ ra là con trỏ nằm sẵn ở ô TÊN HÀNG, gõ được ngay — như màn đơn hàng. Đặt ở
    /// đây chứ không ở lúc nạp dữ liệu: cửa sổ chưa hiện thì gọi Focus() không có tác dụng.
    /// </summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_cboHang.Enabled)
        {
            _cboHang.Focus();
        }
    }

    private HoaDon? HoaDonGoc => _kho.TimHoaDon(_hoaDonGocId);

    /// <summary>Các dòng đã gõ tên hàng — dòng trống ở cuối bảng không tính là món hoàn.</summary>
    private List<ChiTietHoaDon> DongDaGo =>
        _nguon.Where(d => !string.IsNullOrWhiteSpace(d.TenHang)).ToList();

    private decimal TongTienHoan => DongDaGo.Sum(d => d.ThanhTien);

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var goc = HoaDonGoc;
        var khach = goc is null ? null : _kho.TimKhach(goc.KhachHangId);
        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "HOÀN HÀNG",
                goc is null
                    ? string.Empty
                    : $"Hoàn cho hoá đơn {goc.MaHoaDon} · mở ngày {goc.NgayMo:dd/MM/yyyy}"
                      + (khach is null ? string.Empty : $" · {khach.Ten}"),
                tuCao: true),
            0,
            0);

        khung.Controls.Add(TaoThanhToHoan(), 0, 1);
        khung.Controls.Add(TaoThanhNhapMon(), 0, 2);

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(TaoLuoi()));
        khung.Controls.Add(vienLuoi, 0, 3);

        khung.Controls.Add(TaoThanhDuoi(), 0, 4);

        khung.Controls.Add(TaoThanhTrangThai(), 0, 5);
        Controls.Add(khung);
    }

    /// <summary>
    /// Đầu tờ hoàn: ngày và lý do. Hai thứ này thuộc <b>cả tờ</b> nên để riêng một hàng trên,
    /// không lẫn vào thanh nhập từng món ở dưới.
    /// </summary>
    private Control TaoThanhToHoan()
    {
        // Ô ngày là chỗ bấm nhiều nhất nên cho to hẳn: chữ 14pt, ô cao 40px. Tờ lịch bung ra ăn
        // theo cỡ chữ này nên cũng to và dễ bấm hơn.
        _dtNgay.Font = Theme.FontNhapTo;

        _mach.SetToolTip(_txtLyDo, "Ví dụ: hàng lỗi, khách lấy thừa, sai chủng loại — sẽ in lên tờ hoàn hàng");
        _mach.SetToolTip(_dtNgay, "Ngày lập tờ hoàn — cũng là ngày ghi cho từng dòng hàng trên tờ");

        return Theme.HangO(
            Theme.Nen,
            Theme.Truong("NGÀY HOÀN", _dtNgay, 190, 40, 12),
            Theme.Truong("LÝ DO HOÀN", _txtLyDo, 420, 40, 12));
    }

    /// <summary>
    /// Thanh nhập một món hoàn — <b>xếp và bấm y như thanh nhập nhanh của màn đơn hàng</b>: gõ
    /// tên hàng, Enter là sang thẳng ô SỐ HOÀN (đơn vị và đơn giá phần mềm tự điền, gõ tay chỉ
    /// khi cần sửa), Enter nữa là món xuống bảng và các ô dọn sạch để gõ món tiếp.
    /// </summary>
    private Control TaoThanhNhapMon()
    {
        _cboHang.DropDownStyle = ComboBoxStyle.DropDown;
        _cboHang.Font = Theme.FontNhap;

        // Không gợi ý gì trong lúc gõ, giống hệt màn đơn hàng: danh sách vẫn nằm sẵn trong ô,
        // muốn chọn thì bấm mũi tên mở ra — chứ gõ tới đâu bung tới đó rồi hỏi "ý anh là ...
        // phải không" thì đang nhập liền tay bị cắt nhịp. Gõ thẳng trên lưới thì vẫn có gợi ý.
        _cboHang.AutoCompleteMode = AutoCompleteMode.None;

        // Rời ô mới điền đơn vị và đơn giá, và chỉ khi tên **khớp hẳn** một món gợi ý. Không
        // đoán, không hỏi.
        _cboHang.Leave += (_, _) => DienTheoMonGoiY();
        _cboHang.SelectedIndexChanged += (_, _) =>
        {
            if (_dangNap || _cboHang.SelectedItem is not MonGoiY mon)
            {
                return;
            }

            _txtDonVi.Text = mon.DonVi;
            _txtDonGia.Text = So.Tien(mon.DonGia);
            TinhTamTinh();
        };

        _txtDonGia.TextChanged += (_, _) => TinhTamTinh();
        _txtSoHoan.TextChanged += (_, _) => TinhTamTinh();
        _txtDonGia.Leave += (_, _) => Theme.ChotPhepTinh(_txtDonGia, So.Tien);
        _txtSoHoan.Leave += (_, _) => Theme.ChotPhepTinh(_txtSoHoan, So.Luong);

        _lblTamTinh.Font = Theme.FontSo;
        _lblTamTinh.ForeColor = Theme.Do;
        _lblTamTinh.Text = "0";
        _lblTamTinh.TextAlign = ContentAlignment.MiddleRight;
        _lblTamTinh.AutoSize = false;

        var btnThem = Theme.Nut("+  THÊM DÒNG", Theme.Xanh, 180, 40, noTheoChu: true);
        btnThem.Click += (_, _) => ThemMon();

        var btnXoaDong = Theme.NutPhu("Xoá dòng đang chọn", 190, 40, noTheoChu: true);
        btnXoaDong.Click += (_, _) => XoaDongDangChon();

        // Nhãn để đúng một hai chữ cho khỏi cắt, còn cách gõ thì để trong chú thích hiện ra khi
        // trỏ chuột vào ô.
        _mach.SetToolTip(_cboHang, "Món trên hoá đơn gốc và cả danh mục vật tư — bấm mũi tên để mở danh sách");
        _mach.SetToolTip(_txtDonGia, "Gõ được cả phép tính, ví dụ: 3+2*4");
        _mach.SetToolTip(_txtSoHoan, "Số khách mang trả về — gõ số dương, vào sổ phần mềm tự ghi thành số âm");

        // Hai nút ngồi riêng một nhóm `AutoSize` để nở theo chữ, lùi xuống đúng bằng chỗ nhãn
        // của mấy ô bên cạnh nên vẫn ngang hàng.
        var nhomNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 12, 0),
        };
        nhomNut.Controls.Add(btnThem);
        nhomNut.Controls.Add(btnXoaDong);

        // Bề rộng lấy đúng của màn đơn hàng để hai màn nhìn ra một nhà; hàng ô tự xuống dòng khi
        // cửa sổ hẹp nên không lo nút bị đẩy ra ngoài mép.
        const int CaoO = 40;
        const int Le = 12;
        var thanh = Theme.HangO(
            Theme.ChinhNhat,
            Theme.Truong("TÊN HÀNG", _cboHang, 240, CaoO, Le),
            Theme.Truong("ĐƠN VỊ", _txtDonVi, 95, CaoO, Le),
            Theme.Truong("ĐƠN GIÁ", _txtDonGia, 125, CaoO, Le),
            Theme.Truong("SỐ HOÀN", _txtSoHoan, 115, CaoO, Le),
            Theme.Truong("TIỀN HOÀN", _lblTamTinh, 135, CaoO, Le),
            nhomNut);

        Theme.GanPhimEnter(_cboHang, _txtSoHoan, ThemMon);
        Theme.GanPhimEnter(_txtDonVi, null, ThemMon);
        Theme.GanPhimEnter(_txtDonGia, null, ThemMon);
        Theme.GanPhimEnter(_txtSoHoan, null, ThemMon);

        return thanh;
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        // Phím Delete xoá luôn dòng đang chọn, như mọi bảng gõ tay khác — vẫn còn nút "Xoá dòng
        // đang chọn" cho người không quen phím.
        _luoi.AllowUserToDeleteRows = true;

        // Bấm một lần vào ô là sửa được luôn, không phải bấm lần nữa hay nhấn F2: gõ cả bảng
        // thì mỗi ô tiết kiệm một cú bấm là đỡ hẳn tay.
        _luoi.CellMouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left
                || e.RowIndex < 0
                || e.ColumnIndex < 0
                || _luoi.IsCurrentCellInEditMode)
            {
                return;
            }

            _luoi.BeginEdit(selectAll: true);
        };

        // Không có cột ngày: cả tờ hoàn ghi theo đúng NGÀY HOÀN ở trên, khỏi phải gõ lại từng
        // dòng một ngày y hệt nhau.
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(ChiTietHoaDon.TenHang), "TÊN HÀNG", 280, chiDoc: false, toiThieu: 150),
            Theme.Cot(nameof(ChiTietHoaDon.DonVi), "ĐƠN VỊ", 90, chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.DonGia), "ĐƠN GIÁ", 125, "#,##0", canPhai: true, chiDoc: false, toiThieu: 104),
            Theme.Cot(nameof(ChiTietHoaDon.SoLuong), "SỐ HOÀN", 110, "#,##0.##", canPhai: true, chiDoc: false),
            Theme.Cot(nameof(ChiTietHoaDon.ThanhTien), "TIỀN HOÀN", 145, "#,##0", canPhai: true, toiThieu: 116),
            Theme.Cot(nameof(ChiTietHoaDon.GhiChu), "GHI CHÚ", 170, chiDoc: false, toiThieu: 110));

        Theme.ChoPhepGoSo(_luoi, nameof(ChiTietHoaDon.DonGia), nameof(ChiTietHoaDon.SoLuong));

        // Gợi ý tên hàng ngay trong ô đang gõ. Ô sửa của lưới dùng chung một TextBox cho mọi
        // cột nên phải tắt gợi ý lại khi sang cột khác, không thì gõ ghi chú cũng bị gợi ý.
        _luoi.EditingControlShowing += (_, e) =>
        {
            if (e.Control is not TextBox o)
            {
                return;
            }

            if (_luoi.CurrentCell?.OwningColumn.DataPropertyName == nameof(ChiTietHoaDon.TenHang))
            {
                o.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                o.AutoCompleteSource = AutoCompleteSource.CustomSource;
                o.AutoCompleteCustomSource = _goiYTenHang;
            }
            else
            {
                o.AutoCompleteMode = AutoCompleteMode.None;
                o.AutoCompleteSource = AutoCompleteSource.None;
            }
        };

        _luoi.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.CellStyle is not { } kieu)
            {
                return;
            }

            var cot = _luoi.Columns[e.ColumnIndex].DataPropertyName;
            if (_luoi.Rows[e.RowIndex].DataBoundItem is not ChiTietHoaDon dong)
            {
                return;
            }

            if (cot == nameof(ChiTietHoaDon.ThanhTien))
            {
                kieu.Font = Theme.FontLuoiDam;
                kieu.ForeColor = dong.ThanhTien > 0m ? Theme.Do : Theme.Chu;
            }

            // Số 0 chưa gõ thì để trống hẳn: nhìn vào là biết ô nào còn thiếu.
            if (cot is nameof(ChiTietHoaDon.DonGia) or nameof(ChiTietHoaDon.SoLuong)
                    or nameof(ChiTietHoaDon.ThanhTien)
                && e.Value is decimal and 0m)
            {
                e.Value = string.Empty;
                e.FormattingApplied = true;
            }

            if (!string.IsNullOrWhiteSpace(dong.TenHang))
            {
                return;
            }

            // Dòng trống cuối bảng: tô vàng nhạt như dòng đang gõ dở ở màn đơn hàng, kèm lời
            // nhắc — nhắc ẩn đi khi con trỏ đứng vào đúng ô đó để khỏi dính vào chữ đang gõ.
            kieu.BackColor = Color.FromArgb(255, 251, 230);
            kieu.SelectionBackColor = Color.FromArgb(250, 236, 190);
            kieu.SelectionForeColor = Theme.Chu;

            var oDangChon = _luoi.CurrentCell is { } oHienTai
                && oHienTai.RowIndex == e.RowIndex
                && oHienTai.ColumnIndex == e.ColumnIndex;

            if (cot == nameof(ChiTietHoaDon.TenHang) && !oDangChon && e.Value is string { Length: 0 })
            {
                e.Value = "Gõ tên món khách trả về…";
                kieu.ForeColor = Theme.Xam;
                e.FormattingApplied = true;
            }
        };

        // Lời nhắc ở dòng trống ẩn hiện theo chỗ con trỏ đang đứng nên phải vẽ lại dòng đó.
        _luoi.CurrentCellChanged += (_, _) =>
        {
            if (_luoi.CurrentCell is { RowIndex: >= 0 } o && o.RowIndex < _luoi.Rows.Count)
            {
                _luoi.InvalidateRow(o.RowIndex);
            }
        };

        _luoi.CellEndEdit += (_, e) =>
        {
            if (e.RowIndex < 0 || _luoi.Rows[e.RowIndex].DataBoundItem is not ChiTietHoaDon dong)
            {
                return;
            }

            if (_luoi.Columns[e.ColumnIndex].DataPropertyName == nameof(ChiTietHoaDon.TenHang))
            {
                DienTheoTenHang(dong);
            }

            ChinhLaiSoAm(dong);
            _nguon.ResetItem(e.RowIndex);
            CapNhatTong();

            // Thêm dòng trống sau khi lưới đã gõ xong hẳn: chen vào giữa lúc còn đang sửa ô là
            // lưới vẽ lại ngay dưới chân con trỏ.
            BeginInvoke(GiuMotDongTrongCuoiBang);
        };

        _luoi.UserDeletedRow += (_, _) =>
        {
            CapNhatTong();
            BeginInvoke(GiuMotDongTrongCuoiBang);
        };

        _luoi.DataSource = _nguon;
        return _luoi;
    }

    private Control TaoThanhDuoi()
    {
        var btnTao = Theme.Nut("TẠO HOÁ ĐƠN HOÀN HÀNG", Theme.Cam, 300, 46, noTheoChu: true);
        btnTao.Click += (_, _) => TaoHoaDonHoan();

        var btnDong = Theme.NutPhu("Đóng", 140, 46, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTong.Font = Theme.FontSo;
        _lblTong.ForeColor = Theme.Do;

        return Theme.ThanhDuoi(_lblTong, btnTao, btnDong);
    }

    private Control TaoThanhTrangThai()
    {
        return Theme.ThanhTrangThai(_lblTrangThai);
    }

    // ---------------- Nạp dữ liệu ----------------

    private void Nap()
    {
        if (HoaDonGoc is not { } goc)
        {
            // Máy khác vừa xoá hoá đơn gốc. Đừng gọi Close() ở đây: cửa sổ còn chưa hiện xong.
            _luoi.Enabled = false;
            _cboHang.Enabled = false;
            _txtDonVi.Enabled = false;
            _txtDonGia.Enabled = false;
            _txtSoHoan.Enabled = false;
            _lblTrangThai.Text = "Hoá đơn gốc không còn trong sổ nữa nên không hoàn được.";
            return;
        }

        NapMonGoiY(goc);

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        _nguon.Add(DongTrong());
        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        CapNhatTong();

        var daHoan = HoanHang.TienDaHoan(_kho.HoaDonCuaKhach(goc.KhachHangId), goc.Id);
        _lblTrangThai.Text = "Nhập từng món khách mang trả về ở thanh trên: gõ tên hàng, Enter "
            + "sang ô SỐ HOÀN, Enter nữa là món xuống bảng. Đơn vị và đơn giá tự điền theo giá "
            + "đã bán."
            + (daHoan > 0m ? $" Hoá đơn này đã hoàn {So.Tien(daHoan)} ở những lần trước." : string.Empty);
    }

    /// <summary>
    /// Danh sách món để gợi ý, kèm đơn vị và giá của từng món: món trên hoá đơn gốc lên trước
    /// (hay hoàn nhất, và mang đúng <b>giá đã bán</b> cho khách), rồi tới cả danh mục vật tư
    /// cho món khách đổi từ lần khác — món trong danh mục lấy giá ở bảng giá riêng của khách.
    /// <para>
    /// Dựng sẵn một lần lúc mở cửa sổ, để cả ô TÊN HÀNG ở thanh nhập, gợi ý gõ trên lưới và
    /// việc tự điền đơn vị/đơn giá đều đi qua đúng một danh sách này.
    /// </para>
    /// </summary>
    private void NapMonGoiY(HoaDon goc)
    {
        var khach = _kho.TimKhach(goc.KhachHangId);

        // Trùng tên thì món đứng trước thắng — so tên đã bỏ dấu, y như lúc tra tên về sau.
        var daCo = new HashSet<string>(StringComparer.Ordinal);
        _monGoiY.Clear();

        foreach (var chiTiet in goc.ChiTiet.Where(c => c.SoLuong > 0m))
        {
            var ten = chiTiet.TenHang.Trim();
            if (ten.Length == 0 || !daCo.Add(ChuViet.BoDau(ten).Trim()))
            {
                continue;
            }

            _monGoiY.Add(new MonGoiY(ten, chiTiet.DonVi, chiTiet.DonGia, chiTiet.VatTuId));
        }

        foreach (var vatTu in _kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            var ten = vatTu.Ten.Trim();
            if (ten.Length == 0 || !daCo.Add(ChuViet.BoDau(ten).Trim()))
            {
                continue;
            }

            var gia = khach is null ? vatTu.DonGiaMacDinh : _kho.GiaCho(khach, vatTu);
            _monGoiY.Add(new MonGoiY(ten, vatTu.DonVi, gia, vatTu.Id));
        }

        _goiYTenHang.Clear();
        _goiYTenHang.AddRange(_monGoiY.Select(m => m.Ten).ToArray());

        _dangNap = true;
        _cboHang.Items.Clear();
        foreach (var mon in _monGoiY)
        {
            _cboHang.Items.Add(mon);
        }

        _dangNap = false;
    }

    /// <summary>Món gợi ý cùng tên với chuỗi vừa gõ. Hoá đơn gốc xếp trước nên khớp trước.</summary>
    private MonGoiY? TimMon(string? ten) => string.IsNullOrWhiteSpace(ten)
        ? null
        : _monGoiY.FirstOrDefault(m => CungTen(m.Ten, ten));

    private ChiTietHoaDon DongTrong() => new() { Ngay = _dtNgay.Value.Date };

    private void CapNhatTong()
    {
        var soMon = DongDaGo.Count;
        _lblTong.Text = $"{soMon} món  ·  tiền hoàn lại: {So.Tien(TongTienHoan)}";
    }

    /// <summary>
    /// Luôn để đúng một dòng trống ở cuối bảng để gõ tiếp, như dòng gõ dở của màn đơn hàng —
    /// khỏi phải bấm "Thêm dòng" giữa chừng.
    /// </summary>
    private void GiuMotDongTrongCuoiBang()
    {
        if (_nguon.Count > 0 && string.IsNullOrWhiteSpace(_nguon[^1].TenHang))
        {
            return;
        }

        _nguon.Add(DongTrong());
    }

    /// <summary>
    /// Ghi món đang gõ ở thanh nhập xuống bảng rồi dọn ô để gõ món tiếp — đúng nút "+ THÊM DÒNG"
    /// của màn đơn hàng. Thiếu ô nào thì nhắc một câu ở thanh dưới và đưa con trỏ về đúng ô ấy:
    /// nhập cả chục món liền tay mà cứ thiếu một ô là bật hộp thoại chặn giữa thì mất nhịp.
    /// </summary>
    private void ThemMon()
    {
        if (HoaDonGoc is null || HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        var ten = _cboHang.Text.Trim();
        if (ten.Length == 0)
        {
            _lblTrangThai.Text = "Chưa gõ tên món khách trả về nên chưa ghi được dòng nào.";
            _cboHang.Focus();
            return;
        }

        if (!Theme.OSoHopLe(_txtDonGia, "ĐƠN GIÁ", Nhac) || !Theme.OSoHopLe(_txtSoHoan, "SỐ HOÀN", Nhac))
        {
            return;
        }

        // Gõ số âm thì lấy trị tuyệt đối: vào sổ phần mềm đã tự ghi thành số âm, gõ âm nữa là
        // hoàn ngược. Cùng luật với dòng gõ thẳng trên lưới — xem ChinhLaiSoAm.
        var soHoan = Math.Abs(So.Tinh(_txtSoHoan.Text));
        if (soHoan == 0m)
        {
            _lblTrangThai.Text = $"\"{ten}\" chưa có số hoàn nên chưa ghi vào bảng. "
                + "Gõ số khách mang trả về (gõ được cả phép tính: 3+2*4).";
            _txtSoHoan.Focus();
            _txtSoHoan.SelectAll();
            return;
        }

        var dong = new ChiTietHoaDon
        {
            // Cả tờ ghi theo đúng NGÀY HOÀN ở trên, nên dòng nào cũng lấy ngày ấy.
            Ngay = _dtNgay.Value.Date,
            VatTuId = TimMon(ten)?.VatTuId,
            TenHang = ten,
            DonVi = _txtDonVi.Text.Trim(),
            DonGia = Math.Abs(So.Tinh(_txtDonGia.Text)),
            SoLuong = soHoan,
        };

        ThemVaoBang(dong);

        // Sẵn sàng cho món tiếp theo; ngày và lý do là của cả tờ nên giữ nguyên.
        _dangNap = true;
        _cboHang.SelectedIndex = -1;
        _cboHang.Text = string.Empty;
        _txtDonVi.Clear();
        _txtDonGia.Clear();
        _txtSoHoan.Clear();
        _dangNap = false;
        TinhTamTinh();
        _cboHang.Focus();

        _lblTrangThai.Text = $"Đã ghi \"{dong.TenHang}\": {So.Luong(soHoan)} {dong.DonVi} · "
            + $"{So.Tien(dong.ThanhTien)}. Gõ tiếp món nữa, hay bấm TẠO HOÁ ĐƠN HOÀN HÀNG.";
    }

    /// <summary>
    /// Chèn món vừa nhập vào bảng, <b>ngay trên dòng trống cuối</b> — dòng trống ấy vẫn để đấy
    /// cho ai muốn gõ thẳng trên lưới, nên món mới không được nhảy xuống dưới nó.
    /// </summary>
    private void ThemVaoBang(ChiTietHoaDon dong)
    {
        var viTri = _nguon.Count > 0 && string.IsNullOrWhiteSpace(_nguon[^1].TenHang)
            ? _nguon.Count - 1
            : _nguon.Count;

        _nguon.Insert(viTri, dong);
        GiuMotDongTrongCuoiBang();
        CapNhatTong();

        // Kéo bảng xuống chỗ dòng vừa ghi: nhập vài chục món thì món mới phải nhìn thấy được.
        // Đặt ô hiện tại thôi, không gọi Focus() — con trỏ phải ở lại thanh nhập để gõ món tiếp.
        if (viTri < _luoi.Rows.Count)
        {
            _luoi.CurrentCell = _luoi.Rows[viTri].Cells[0];
        }
    }

    /// <summary>Tiền hoàn của món đang gõ ở thanh nhập, tính lại theo từng chữ số vừa gõ.</summary>
    private void TinhTamTinh()
    {
        var tien = Math.Round(
            Math.Abs(So.Tinh(_txtDonGia.Text)) * Math.Abs(So.Tinh(_txtSoHoan.Text)),
            0,
            MidpointRounding.AwayFromZero);
        _lblTamTinh.Text = So.Tien(tien);
    }

    private void Nhac(string chu) => _lblTrangThai.Text = chu;

    /// <summary>
    /// Tên vừa gõ ở thanh nhập khớp hẳn một món gợi ý thì điền hộ đơn vị và đơn giá. Chỉ điền
    /// vào ô đang trống — người dùng đã gõ giá riêng thì đừng ghi đè.
    /// </summary>
    private void DienTheoMonGoiY()
    {
        if (_dangNap || TimMon(_cboHang.Text.Trim()) is not { } mon)
        {
            return;
        }

        if (_txtDonVi.Text.Trim().Length == 0)
        {
            _txtDonVi.Text = mon.DonVi;
        }

        if (So.Tinh(_txtDonGia.Text) <= 0m)
        {
            _txtDonGia.Text = So.Tien(mon.DonGia);
        }
    }

    private void XoaDongDangChon()
    {
        if (_luoi.CurrentRow?.DataBoundItem is not ChiTietHoaDon dong)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dong.TenHang))
        {
            _lblTrangThai.Text = "Dòng này còn trống, chưa có gì để xoá.";
            return;
        }

        _nguon.Remove(dong);
        GiuMotDongTrongCuoiBang();
        CapNhatTong();
        _lblTrangThai.Text = $"Đã bỏ dòng \"{dong.TenHang}\" khỏi tờ hoàn.";
    }

    /// <summary>
    /// Gõ xong tên hàng <b>thẳng trên lưới</b> thì tự điền đơn vị và đơn giá, đi qua đúng danh
    /// sách món gợi ý mà thanh nhập đang dùng: giá lấy đúng **giá đã bán** trên hoá đơn gốc
    /// trước — hoàn theo giá bán thì không bên nào hụt; món không có trên tờ gốc mới tra sang
    /// bảng giá riêng của khách. Ô nào người dùng đã gõ thì để nguyên, không đè lên.
    /// </summary>
    private void DienTheoTenHang(ChiTietHoaDon dong)
    {
        var ten = dong.TenHang.Trim();
        dong.TenHang = ten;
        if (TimMon(ten) is not { } mon)
        {
            // Món tự gõ, không có trên tờ gốc cũng không có trong danh mục: cứ để đấy, tờ hoàn
            // ghi đúng chữ đã gõ.
            dong.VatTuId = null;
            return;
        }

        dong.VatTuId = mon.VatTuId;
        if (string.IsNullOrWhiteSpace(dong.DonVi))
        {
            dong.DonVi = mon.DonVi;
        }

        if (dong.DonGia == 0m)
        {
            dong.DonGia = mon.DonGia;
        }
    }

    /// <summary>
    /// Số hoàn và đơn giá gõ số dương; phần mềm tự đổi thành hàng trả về khi lập tờ. Gõ số âm
    /// thì lấy trị tuyệt đối rồi nhắc một câu ở thanh dưới, không bật hộp thoại chặn giữa lúc
    /// đang gõ liền tay cả bảng.
    /// </summary>
    private void ChinhLaiSoAm(ChiTietHoaDon dong)
    {
        if (dong.SoLuong >= 0m && dong.DonGia >= 0m)
        {
            return;
        }

        dong.SoLuong = Math.Abs(dong.SoLuong);
        dong.DonGia = Math.Abs(dong.DonGia);
        _lblTrangThai.Text = $"\"{dong.TenHang}\": số hoàn và đơn giá gõ số dương, "
            + "phần mềm tự ghi thành hàng trả về khi lập tờ hoàn.";
    }

    private static bool CungTen(string a, string b) =>
        ChuViet.BoDau(a).Trim() == ChuViet.BoDau(b).Trim();

    private void DuaConTroToi(ChiTietHoaDon dong, string thuocTinh)
    {
        var viTri = _nguon.IndexOf(dong);
        var cot = _luoi.Columns
            .Cast<DataGridViewColumn>()
            .FirstOrDefault(c => c.DataPropertyName == thuocTinh);

        if (viTri < 0 || viTri >= _luoi.Rows.Count || cot is null)
        {
            return;
        }

        _luoi.CurrentCell = _luoi.Rows[viTri].Cells[cot.Index];
        _luoi.Focus();
    }

    // ---------------- Lập hoá đơn hoàn hàng ----------------

    private void TaoHoaDonHoan()
    {
        if (HoaDonGoc is not { } goc || HopThoai.ChanKhiChiXem(this, _kho))
        {
            return;
        }

        var dong = DongDaGo;
        if (dong.Count == 0)
        {
            _lblTrangThai.Text = "Chưa gõ món nào nên chưa lập được tờ hoàn hàng.";
            _luoi.Focus();
            return;
        }

        // Dòng gõ số mà quên tên hàng thì bỏ qua lặng lẽ là mất luôn món khách trả — nhắc để
        // gõ nốt tên, chứ không tự đoán món nào.
        if (_nguon.FirstOrDefault(d =>
                string.IsNullOrWhiteSpace(d.TenHang) && (d.SoLuong > 0m || d.DonGia > 0m)) is { } thieuTen)
        {
            _lblTrangThai.Text = "Có dòng đã gõ số mà chưa có tên hàng — gõ nốt tên, hoặc xoá dòng đó đi.";
            DuaConTroToi(thieuTen, nameof(ChiTietHoaDon.TenHang));
            return;
        }

        if (dong.FirstOrDefault(d => d.SoLuong <= 0m) is { } thieuSo)
        {
            _lblTrangThai.Text = $"\"{thieuSo.TenHang}\" chưa có số hoàn — gõ số vào cột SỐ HOÀN, "
                + "hoặc xoá dòng đó đi.";
            DuaConTroToi(thieuSo, nameof(ChiTietHoaDon.SoLuong));
            return;
        }

        if (dong.FirstOrDefault(d => d.DonGia <= 0m) is { } thieuGia
            && !HopThoai.Hoi(
                this,
                $"\"{thieuGia.TenHang}\" chưa có đơn giá nên món này hoàn 0đ.\n\nVẫn lập tờ hoàn?"))
        {
            DuaConTroToi(thieuGia, nameof(ChiTietHoaDon.DonGia));
            return;
        }

        var ma = _kho.TaoMaHoaDon(goc.KhachHangId, goc.Nam, LoaiHoaDon.HoanHang);
        var tienHoan = TongTienHoan;
        var ngay = _dtNgay.Value.Date;

        if (!HopThoai.Hoi(
                this,
                $"Lập hoá đơn hoàn hàng {ma} cho hoá đơn {goc.MaHoaDon}?\n\n"
                + $"{dong.Count} món · hoàn lại {So.Tien(tienHoan)}.\n\n"
                + "Hoá đơn gốc để nguyên, số tiền này trừ vào nợ của khách.\n(Ctrl+Z để bỏ.)"))
        {
            return;
        }

        // Cả tờ ghi theo đúng ngày hoàn, khỏi bắt gõ lại ngày ở từng dòng.
        foreach (var mot in dong)
        {
            mot.Ngay = ngay;
        }

        var hoanHang = HoanHang.TaoTuDongGo(goc, dong, ma, ngay, _txtLyDo.Text.Trim());

        _kho.ThucHien(
            $"Hoàn hàng {ma} cho hoá đơn {goc.MaHoaDon}",
            () => _kho.DuLieu.HoaDons.Add(hoanHang),
            phatSuKien: false);

        HoaDonHoanDaTao = hoanHang.Id;
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        var dangSuaO = _luoi.IsCurrentCellInEditMode;

        switch (keyData)
        {
            // F3 về ô TÊN HÀNG của thanh nhập, y như màn đơn hàng: đang xem giữa bảng mà muốn
            // gõ món nữa thì một phím là về chỗ nhập.
            case Keys.F3:
                _cboHang.Focus();
                _cboHang.SelectAll();
                return true;
            case Keys.Escape when !dangSuaO:
                Close();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// Một món trong danh sách gợi ý của ô TÊN HÀNG: tên, đơn vị, đơn giá và mã vật tư (món tự
    /// gõ ngoài danh mục thì không có mã). <c>ToString</c> chỉ trả về tên vì ô nhập lấy đúng
    /// chuỗi này làm chữ trong ô — kèm thêm giá vào đây là giá lọt cả vào tên hàng của tờ hoàn.
    /// </summary>
    private sealed record MonGoiY(string Ten, string DonVi, decimal DonGia, Guid? VatTuId)
    {
        public override string ToString() => Ten;
    }
}
