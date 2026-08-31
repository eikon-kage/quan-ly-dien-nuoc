using System.ComponentModel;
using QuanLyDienNuoc.BaoCao;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Lập hoá đơn hoàn hàng cho một hoá đơn bán: bảng trống, gõ tay từng món khách mang trả về.
/// Hoá đơn gốc không bị sửa một chữ — tờ hoàn là chứng từ riêng, hoàn cho nó — nên hoá đơn đã
/// in cho khách hay đã chốt vẫn hoàn được.
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

    // Gõ tên hàng có gợi ý: tên món trên hoá đơn gốc trước, rồi tới cả danh mục vật tư.
    private readonly AutoCompleteStringCollection _goiYTenHang = new();

    private readonly OChonNgay _dtNgay = new() { Font = Theme.FontNhap };

    private readonly TextBox _txtLyDo = Theme.O(420);
    private readonly Label _lblTong = Theme.NhanDaiDong();

    // Giữ tham chiếu: ToolTip không được control nào giữ hộ, bị dọn rác là mất lời mách.
    private readonly ToolTip _mach = new() { InitialDelay = 250, AutoPopDelay = 10000 };
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

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
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
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

        khung.Controls.Add(TaoThanhNhap(), 0, 1);

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(TaoLuoi()));
        khung.Controls.Add(vienLuoi, 0, 2);

        khung.Controls.Add(TaoThanhDuoi(), 0, 3);

        khung.Controls.Add(TaoThanhTrangThai(), 0, 4);
        Controls.Add(khung);
    }

    private Control TaoThanhNhap()
    {
        _dtNgay.Font = Theme.FontNhapTo;

        var btnThemDong = Theme.Nut("+  THÊM DÒNG", Theme.Chinh, 170, 40, noTheoChu: true);
        btnThemDong.Click += (_, _) => ThemDongTrongVaGo();

        var btnXoaDong = Theme.NutPhu("Xoá dòng đang chọn", 190, 40, noTheoChu: true);
        btnXoaDong.Click += (_, _) => XoaDongDangChon();

        // Hai nút ngồi riêng một nhóm `AutoSize` để nở theo chữ, lùi xuống đúng bằng chỗ nhãn
        // của mấy ô bên cạnh nên vẫn ngang hàng.
        var nhomNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 12, 0),
        };
        nhomNut.Controls.Add(btnThemDong);
        nhomNut.Controls.Add(btnXoaDong);

        _mach.SetToolTip(_txtLyDo, "Ví dụ: hàng lỗi, khách lấy thừa, sai chủng loại — sẽ in lên tờ hoàn hàng");
        _mach.SetToolTip(_dtNgay, "Ngày lập tờ hoàn — cũng là ngày ghi cho từng dòng hàng trên tờ");

        return Theme.HangO(
            Theme.ChinhNhat,
            Theme.Truong("NGÀY HOÀN", _dtNgay, 190, 40, 12),
            Theme.Truong("LÝ DO HOÀN", _txtLyDo, 420, 40, 12),
            nhomNut);
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
            _lblTrangThai.Text = "Hoá đơn gốc không còn trong sổ nữa nên không hoàn được.";
            return;
        }

        NapGoiYTenHang(goc);

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        _nguon.Add(DongTrong());
        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        CapNhatTong();

        var daHoan = HoanHang.TienDaHoan(_kho.HoaDonCuaKhach(goc.KhachHangId), goc.Id);
        _lblTrangThai.Text = "Gõ từng món khách mang trả về: tên hàng, đơn giá, số hoàn. Gõ tên "
            + "món đã bán cho khách thì đơn vị và giá tự điền theo giá đã bán."
            + (daHoan > 0m ? $" Hoá đơn này đã hoàn {So.Tien(daHoan)} ở những lần trước." : string.Empty);
    }

    /// <summary>
    /// Danh sách gợi ý tên hàng: món trên hoá đơn gốc lên trước (hay hoàn nhất), rồi tới cả
    /// danh mục vật tư cho món khách đổi từ lần khác.
    /// </summary>
    private void NapGoiYTenHang(HoaDon goc)
    {
        var ten = goc.ChiTiet
            .Where(c => c.SoLuong > 0m)
            .Select(c => c.TenHang)
            .Concat(_kho.DuLieu.VatTus.Select(v => v.Ten))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        _goiYTenHang.Clear();
        _goiYTenHang.AddRange(ten);
    }

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

    private void ThemDongTrongVaGo()
    {
        GiuMotDongTrongCuoiBang();

        var viTri = _nguon.Count - 1;
        if (viTri < 0 || viTri >= _luoi.Rows.Count)
        {
            return;
        }

        _luoi.CurrentCell = _luoi.Rows[viTri].Cells[0];
        _luoi.BeginEdit(selectAll: true);
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
    /// Gõ xong tên hàng thì tự điền đơn vị và đơn giá. Giá lấy đúng **giá đã bán** trên hoá đơn
    /// gốc trước — hoàn theo giá bán thì không bên nào hụt; món không có trên tờ gốc mới tra
    /// sang bảng giá riêng của khách. Ô nào người dùng đã gõ thì để nguyên, không đè lên.
    /// </summary>
    private void DienTheoTenHang(ChiTietHoaDon dong)
    {
        var ten = dong.TenHang.Trim();
        dong.TenHang = ten;
        if (ten.Length == 0 || HoaDonGoc is not { } goc)
        {
            return;
        }

        var dongGoc = goc.ChiTiet.FirstOrDefault(c => c.SoLuong > 0m && CungTen(c.TenHang, ten));
        if (dongGoc is not null)
        {
            dong.VatTuId = dongGoc.VatTuId;
            if (string.IsNullOrWhiteSpace(dong.DonVi))
            {
                dong.DonVi = dongGoc.DonVi;
            }

            if (dong.DonGia == 0m)
            {
                dong.DonGia = dongGoc.DonGia;
            }

            return;
        }

        if (_kho.TimVatTuTheoTen(ten) is not { } vatTu)
        {
            // Món tự gõ, không có trong danh mục: cứ để đấy, tờ hoàn ghi đúng chữ đã gõ.
            dong.VatTuId = null;
            return;
        }

        dong.VatTuId = vatTu.Id;
        if (string.IsNullOrWhiteSpace(dong.DonVi))
        {
            dong.DonVi = vatTu.DonVi;
        }

        if (dong.DonGia == 0m && _kho.TimKhach(goc.KhachHangId) is { } khach)
        {
            dong.DonGia = _kho.GiaCho(khach, vatTu);
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
            case Keys.Escape when !dangSuaO:
                Close();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
