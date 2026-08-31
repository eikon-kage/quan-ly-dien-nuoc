using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Danh mục vật tư chung của cửa hàng và giá chung của từng mặt hàng.</summary>
public sealed class VatTuForm : Form
{
    private const string TatCaNhom = "Tất cả nhóm";
    private const string ChuaDatNhom = "(chưa đặt nhóm)";

    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<VatTu> _nguon = new();
    private readonly TextBox _txtTim = Theme.O(360);

    private readonly TextBox _txtTen = Theme.O(320);
    private readonly TextBox _txtDonVi = Theme.O(120);
    private readonly TextBox _txtGia = Theme.O(160);
    private readonly ComboBox _cboNhom = new();
    private readonly ComboBox _cboLoc = new();
    private readonly ComboBox _cboGan = new();

    /// <summary>Cột NHÓM là ô chọn; danh sách nhóm dựng lại mỗi lần nạp lưới.</summary>
    private readonly DataGridViewComboBoxColumn _cotNhom = new()
    {
        Name = "colNhomId",
        DataPropertyName = nameof(VatTu.NhomId),
        HeaderText = "NHÓM",
        FillWeight = 150,
        MinimumWidth = 120,
        SortMode = DataGridViewColumnSortMode.NotSortable,
        FlatStyle = FlatStyle.Flat,
        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing,
        DisplayMember = nameof(MucNhom.Ten),
        ValueMember = nameof(MucNhom.Id),
    };
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    private string? _anhChupTruocKhiSua;

    /// <summary>Đang dựng lại danh sách nhóm thì đừng để sự kiện của ô lọc gọi <see cref="Nap"/> lồng vào nhau.</summary>
    private bool _dangDungOLoc;

    public VatTuForm()
    {
        Text = "Danh mục vật tư";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1160, 720);
        MinimumSize = new Size(1000, 620);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

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
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "DANH MỤC VẬT TƯ",
                "Giá chung của cửa hàng  ·  \"mã tắt\" để gõ tắt lúc nhập hàng (o27 → Ống nhựa PVC D27)  "
                + "·  \"nhóm\" để lọc danh mục cho gọn",
                tuCao: true),
            0,
            0);

        // Thêm nhanh
        var btnThem = Theme.Nut("+  THÊM VẬT TƯ", Theme.Xanh, 200, 34, noTheoChu: true);
        btnThem.Click += (_, _) => Them();
        // Chọn nhóm trong danh sách chứ không gõ tay: nhóm mới thì tạo ở màn "Quản lý nhóm",
        // để khỏi lỡ gõ lệch ra hai nhóm gần giống nhau.
        _cboNhom.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboNhom.Font = Theme.FontNhap;
        var nhomNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 18, 0),
        };
        nhomNut.Controls.Add(btnThem);

        var nenThem = Theme.HangO(
            Theme.ChinhNhat,
            Theme.Truong("TÊN HÀNG", _txtTen, 340),
            Theme.Truong("ĐƠN VỊ", _txtDonVi, 140),
            Theme.Truong("NHÓM", _cboNhom, 190),
            Theme.Truong("GIÁ CHUNG", _txtGia, 175),
            nhomNut);

        foreach (Control o in new Control[] { _txtTen, _txtDonVi, _txtGia, _cboNhom })
        {
            o.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Them();
                }
            };
        }

        // Tìm kiếm
        _txtTim.TextChanged += (_, _) => Nap();
        _cboLoc.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboLoc.Font = Theme.FontNhap;
        _cboLoc.SelectedIndexChanged += (_, _) =>
        {
            if (!_dangDungOLoc)
            {
                Nap();
            }
        };
        // Gắn nhóm cho cả loạt hàng đang chọn — nhanh hơn sửa từng ô một.
        _cboGan.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboGan.Font = Theme.FontNhap;
        var btnGan = Theme.Nut("GẮN CHO HÀNG ĐANG CHỌN", Theme.Chinh, 240, 34, noTheoChu: true);
        btnGan.Click += (_, _) => GanNhom();
        var ganNut = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, Theme.DinhOTrongTruong, 18, 0),
        };
        ganNut.Controls.Add(btnGan);

        var thanhTim = Theme.HangO(
            Theme.Nen,
            Theme.Truong("TÌM VẬT TƯ", _txtTim, 380),
            Theme.Truong("LỌC THEO NHÓM", _cboLoc, 230),
            Theme.Truong("GẮN NHÓM", _cboGan, 230),
            ganNut);

        // Lưới
        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(VatTu.Ten), "TÊN HÀNG", 300, chiDoc: false, toiThieu: 150),
            Theme.Cot(nameof(VatTu.MaTat), "MÃ TẮT", 110, chiDoc: false),
            _cotNhom,
            Theme.Cot(nameof(VatTu.DonVi), "ĐƠN VỊ", 110, chiDoc: false),
            Theme.Cot(nameof(VatTu.DonGiaMacDinh), "GIÁ CHUNG", 150, "#,##0", canPhai: true, chiDoc: false, toiThieu: 116));
        Theme.ChoPhepGoSo(_luoi, nameof(VatTu.DonGiaMacDinh));
        _luoi.MultiSelect = true;
        _luoi.CellBeginEdit += (_, _) => _anhChupTruocKhiSua = _kho.ChupNhanh();
        _luoi.CellEndEdit += Luoi_CellEndEdit;

        // Ô chọn nhóm: chọn xong là ghi luôn vào sổ, khỏi phải bấm sang ô khác mới lưu.
        _luoi.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_luoi.IsCurrentCellDirty && _luoi.CurrentCell is DataGridViewComboBoxCell)
            {
                _luoi.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _luoi.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && _luoi.Columns[e.ColumnIndex] == _cotNhom)
            {
                GhiNhanSua("Đổi nhóm của mặt hàng");
            }
        };

        // Vào ô nhóm là bung sẵn danh sách, đỡ một cú bấm.
        _luoi.EditingControlShowing += (_, e) =>
        {
            if (e.Control is ComboBox cbo && _luoi.CurrentCell is DataGridViewComboBoxCell)
            {
                BeginInvoke(() => cbo.DroppedDown = true);
            }
        };

        // Nhóm vừa bị xoá ở màn bên kia thì ô để trống, không bung hộp lỗi của Windows.
        _luoi.DataError += (_, e) => e.ThrowException = false;
        _luoi.DataSource = _nguon;

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(_luoi));

        // Thanh dưới
        var btnXoa = Theme.NutPhu("Xoá vật tư", 170, 46, noTheoChu: true);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => Xoa();

        var btnNhom = Theme.NutPhu("Quản lý nhóm…", 200, 46, noTheoChu: true);
        btnNhom.Click += (_, _) => MoQuanLyNhom();

        var btnDong = Theme.NutPhu("Đóng", 140, 46, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.Text = "Bấm đúp vào ô để sửa (kể cả mã tắt, nhóm)  ·  Ctrl+Z hoàn tác";

        khung.Controls.Add(nenThem, 0, 1);
        khung.Controls.Add(thanhTim, 0, 2);
        khung.Controls.Add(vienLuoi, 0, 3);
        khung.Controls.Add(Theme.ThanhDuoi(_lblTrangThai, btnNhom, btnXoa, btnDong), 0, 4);
        Controls.Add(khung);

        ActiveControl = _txtTen;
    }

    private void Nap(Guid? chon = null)
    {
        var dangChon = chon ?? (_luoi.CurrentRow?.DataBoundItem as VatTu)?.Id;

        DungLaiDanhSachNhom();

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var vatTu in _kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            // Gõ vào ô tìm cũng ra theo nhóm: gõ "dien" là ra cả nhóm Điện.
            if (KhopNhomDangLoc(vatTu)
                && (TimHang.Khop(vatTu.Ten, vatTu.MaTat, _txtTim.Text)
                    || TimHang.Khop(_kho.TenNhom(vatTu), null, _txtTim.Text)))
            {
                _nguon.Add(vatTu);
            }
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            for (var i = 0; i < _luoi.Rows.Count; i++)
            {
                if (_luoi.Rows[i].DataBoundItem is VatTu vatTu && vatTu.Id == id)
                {
                    _luoi.CurrentCell = _luoi.Rows[i].Cells[0];
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Dựng lại mọi ô chọn nhóm theo danh sách nhóm hiện có: ô lọc, ô nhóm ở hàng thêm nhanh,
    /// ô gắn hàng loạt và cột NHÓM trong lưới. Thứ đang chọn còn thì giữ nguyên.
    /// </summary>
    private void DungLaiDanhSachNhom()
    {
        var nhom = _kho.NhomTheoTen();

        var mucChon = new List<MucNhom> { new() { Ten = ChuaDatNhom } };
        mucChon.AddRange(nhom.Select(n => new MucNhom { Id = n.Id, Ten = n.Ten }));

        var mucLoc = new List<MucNhom> { new() { TatCa = true, Ten = TatCaNhom } };
        mucLoc.AddRange(nhom.Select(n => new MucNhom { Id = n.Id, Ten = n.Ten }));
        mucLoc.Add(new MucNhom { Ten = ChuaDatNhom });

        // Nạp lại lưới sau mỗi chữ gõ vào ô tìm, mà danh sách nhóm thì hầu như không đổi:
        // giữ nguyên các ô chọn cho khỏi nhấp nháy và khỏi mất nhóm đang chọn.
        if (_cotNhom.DataSource is List<MucNhom> dangCo
            && dangCo.Select(m => (m.Id, m.Ten)).SequenceEqual(mucChon.Select(m => (m.Id, m.Ten))))
        {
            return;
        }

        _dangDungOLoc = true;

        DatMuc(_cboLoc, mucLoc);
        DatMuc(_cboNhom, mucChon);
        DatMuc(_cboGan, mucChon);

        // Cột trong lưới phải gán lại nguồn, không thì ô chọn còn bày nhóm đã xoá.
        _cotNhom.DataSource = mucChon;
        _cotNhom.DisplayMember = nameof(MucNhom.Ten);
        _cotNhom.ValueMember = nameof(MucNhom.Id);

        _dangDungOLoc = false;
    }

    /// <summary>Đổ lại danh sách nhóm vào một ô chọn, cố giữ nhóm ô đó đang chọn.</summary>
    private static void DatMuc(ComboBox o, List<MucNhom> muc)
    {
        var dangChon = o.SelectedItem as MucNhom;

        o.Items.Clear();
        o.Items.AddRange(muc.Cast<object>().ToArray());
        o.SelectedItem = muc.FirstOrDefault(m => m.Id == dangChon?.Id && m.TatCa == dangChon?.TatCa) ?? muc[0];
    }

    private bool KhopNhomDangLoc(VatTu vatTu) => _cboLoc.SelectedItem switch
    {
        MucNhom { TatCa: true } => true,
        MucNhom muc => vatTu.NhomId == muc.Id,
        _ => true,
    };

    /// <summary>Gắn nhóm đang chọn ở ô GẮN NHÓM cho mọi dòng đang chọn trong lưới.</summary>
    private void GanNhom()
    {
        var hang = _luoi.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => r.DataBoundItem)
            .OfType<VatTu>()
            .ToList();

        if (hang.Count == 0)
        {
            HopThoai.CanhBao(this, "Hãy chọn mặt hàng trong bảng trước (giữ Ctrl hoặc Shift để chọn nhiều dòng).");
            return;
        }

        if (_cboGan.SelectedItem is not MucNhom muc)
        {
            HopThoai.CanhBao(this, "Hãy chọn nhóm muốn gắn.");
            return;
        }

        var vao = muc.Id is null ? "về chưa đặt nhóm" : $"vào nhóm \"{muc.Ten}\"";
        var moTa = hang.Count == 1
            ? $"Gắn \"{hang[0].Ten}\" {vao}"
            : $"Gắn {hang.Count} mặt hàng {vao}";

        _kho.ThucHien(moTa, () =>
        {
            foreach (var vatTu in hang)
            {
                vatTu.NhomId = muc.Id;
            }
        }, phatSuKien: false);

        Nap(hang[0].Id);
        _lblTrangThai.Text = $"Đã {char.ToLowerInvariant(moTa[0])}{moTa[1..]}. Bấm Ctrl+Z để hoàn tác.";
    }

    private void MoQuanLyNhom()
    {
        using var form = new NhomHangForm();
        form.ShowDialog(this);

        // Vừa thêm/đổi tên/xoá nhóm bên kia thì mọi ô chọn nhóm ở đây phải bày lại.
        Nap();
    }

    private void Them()
    {
        var ten = _txtTen.Text.Trim();
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập tên hàng.");
            _txtTen.Focus();
            return;
        }

        if (_kho.TimVatTuTheoTen(ten) is not null)
        {
            HopThoai.CanhBao(this, $"Đã có mặt hàng \"{ten}\" trong danh mục.");
            return;
        }

        var moi = new VatTu
        {
            Ten = ten,
            DonVi = _txtDonVi.Text.Trim(),
            NhomId = (_cboNhom.SelectedItem as MucNhom)?.Id,
            DonGiaMacDinh = So.Doc(_txtGia.Text),
        };

        _kho.ThucHien($"Thêm vật tư \"{ten}\"", () => _kho.DuLieu.VatTus.Add(moi), phatSuKien: false);

        _txtTen.Clear();
        _txtDonVi.Clear();
        _txtGia.Clear();

        // Ô nhóm giữ nguyên: nhập một loạt hàng cùng nhóm là chuyện thường.
        _txtTen.Focus();

        Nap(moi.Id);
        _lblTrangThai.Text = $"Đã thêm \"{ten}\"";
    }

    private void Xoa()
    {
        if (_luoi.CurrentRow?.DataBoundItem is not VatTu vatTu)
        {
            HopThoai.CanhBao(this, "Hãy chọn một mặt hàng để xoá.");
            return;
        }

        if (!HopThoai.Hoi(
                this,
                $"Xoá \"{vatTu.Ten}\" khỏi danh mục?\n\n" +
                "Các dòng hàng đã ghi trong hoá đơn vẫn giữ nguyên tên và giá cũ.\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá vật tư \"{vatTu.Ten}\"", () =>
        {
            _kho.DuLieu.VatTus.Remove(vatTu);
            foreach (var khach in _kho.DuLieu.KhachHangs)
            {
                khach.BangGiaRieng.Remove(vatTu.Id);
            }
        }, phatSuKien: false);

        Nap();
        _lblTrangThai.Text = $"Đã xoá \"{vatTu.Ten}\". Bấm Ctrl+Z để lấy lại.";
    }

    private void Luoi_CellEndEdit(object? sender, DataGridViewCellEventArgs e) => GhiNhanSua("Sửa danh mục vật tư");

    /// <summary>Ghi một bước hoàn tác nếu ô vừa sửa có đổi gì thật.</summary>
    private void GhiNhanSua(string moTa)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (anhChup is null || anhChup == _kho.ChupNhanh())
        {
            return;
        }

        _kho.GhiNhan(anhChup, moTa, phatSuKien: false);
        _lblTrangThai.Text = "Đã lưu thay đổi. Bấm Ctrl+Z nếu muốn quay lại.";
    }

    /// <summary>Một mục trong ô chọn nhóm: nhóm thật, mục "chưa đặt nhóm", hay mục "tất cả nhóm" của ô lọc.</summary>
    private sealed class MucNhom
    {
        public Guid? Id { get; init; }

        public string Ten { get; init; } = string.Empty;

        /// <summary>Chỉ ô lọc có mục này; nó không phải một nhóm nên đừng lẫn với "chưa đặt nhóm".</summary>
        public bool TatCa { get; init; }

        public override string ToString() => Ten;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_luoi.IsCurrentCellInEditMode)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.Z:
                    _lblTrangThai.Text = _kho.HoanTac() is { } moTa ? $"Đã hoàn tác: {moTa}" : "Không còn gì để hoàn tác.";
                    Nap();
                    return true;
                case Keys.Control | Keys.Y:
                    _lblTrangThai.Text = _kho.LamLai() is { } moTaLai ? $"Đã làm lại: {moTaLai}" : "Không còn gì để làm lại.";
                    Nap();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
