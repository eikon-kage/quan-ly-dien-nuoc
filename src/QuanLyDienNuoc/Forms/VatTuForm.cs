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
        _cboNhom.DropDownStyle = ComboBoxStyle.DropDown;
        _cboNhom.Font = Theme.FontNhap;

        // Gõ được nhóm mới, mà gõ mấy chữ đầu của nhóm đã có thì nó tự điền nốt — khỏi
        // lệch thành "Ống nước" với "ống Nước" là hai nhóm.
        _cboNhom.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cboNhom.AutoCompleteSource = AutoCompleteSource.ListItems;
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
        var thanhTim = Theme.HangO(
            Theme.Nen,
            Theme.Truong("TÌM VẬT TƯ", _txtTim, 380),
            Theme.Truong("LỌC THEO NHÓM", _cboLoc, 230));

        // Lưới
        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(VatTu.Ten), "TÊN HÀNG", 300, chiDoc: false, toiThieu: 150),
            Theme.Cot(nameof(VatTu.MaTat), "MÃ TẮT", 110, chiDoc: false),
            Theme.Cot(nameof(VatTu.Nhom), "NHÓM", 150, chiDoc: false),
            Theme.Cot(nameof(VatTu.DonVi), "ĐƠN VỊ", 110, chiDoc: false),
            Theme.Cot(nameof(VatTu.DonGiaMacDinh), "GIÁ CHUNG", 150, "#,##0", canPhai: true, chiDoc: false, toiThieu: 116));
        Theme.ChoPhepGoSo(_luoi, nameof(VatTu.DonGiaMacDinh));
        _luoi.CellBeginEdit += (_, _) => _anhChupTruocKhiSua = _kho.ChupNhanh();
        _luoi.CellEndEdit += Luoi_CellEndEdit;
        _luoi.DataSource = _nguon;

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(_luoi));

        // Thanh dưới
        var btnXoa = Theme.NutPhu("Xoá vật tư", 170, 46, noTheoChu: true);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => Xoa();

        var btnDong = Theme.NutPhu("Đóng", 140, 46, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.Text = "Bấm đúp vào ô để sửa (kể cả mã tắt, nhóm)  ·  Ctrl+Z hoàn tác";

        khung.Controls.Add(nenThem, 0, 1);
        khung.Controls.Add(thanhTim, 0, 2);
        khung.Controls.Add(vienLuoi, 0, 3);
        khung.Controls.Add(Theme.ThanhDuoi(_lblTrangThai, btnXoa, btnDong), 0, 4);
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
                    || TimHang.Khop(vatTu.Nhom, null, _txtTim.Text)))
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

    /// <summary>Ô lọc chỉ bày những nhóm đang có thật trong danh mục, cộng mục "chưa đặt nhóm" khi cần.</summary>
    private void DungLaiDanhSachNhom()
    {
        var nhom = _kho.DuLieu.VatTus
            .Select(v => v.Nhom.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var muc = new List<string> { TatCaNhom };
        muc.AddRange(nhom);
        if (_kho.DuLieu.VatTus.Any(v => v.Nhom.Trim().Length == 0))
        {
            muc.Add(ChuaDatNhom);
        }

        if (_cboLoc.Items.Cast<string>().SequenceEqual(muc, StringComparer.Ordinal)
            && _cboNhom.Items.Cast<string>().SequenceEqual(nhom, StringComparer.Ordinal))
        {
            return;
        }

        _dangDungOLoc = true;

        var dangLoc = _cboLoc.SelectedItem as string;
        _cboLoc.Items.Clear();
        _cboLoc.Items.AddRange(muc.Cast<object>().ToArray());
        _cboLoc.SelectedItem = dangLoc is not null && muc.Contains(dangLoc, StringComparer.Ordinal)
            ? dangLoc
            : TatCaNhom;

        // Ô nhóm ở hàng thêm nhanh gõ tay được, nên giữ nguyên chữ đang gõ dở.
        var dangGo = _cboNhom.Text;
        _cboNhom.Items.Clear();
        _cboNhom.Items.AddRange(nhom.Cast<object>().ToArray());
        _cboNhom.Text = dangGo;

        _dangDungOLoc = false;
    }

    private bool KhopNhomDangLoc(VatTu vatTu) => (_cboLoc.SelectedItem as string) switch
    {
        null or TatCaNhom => true,
        ChuaDatNhom => vatTu.Nhom.Trim().Length == 0,
        var loc => string.Equals(vatTu.Nhom.Trim(), loc, StringComparison.CurrentCultureIgnoreCase),
    };

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
            Nhom = _cboNhom.Text.Trim(),
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

    private void Luoi_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (anhChup is null || anhChup == _kho.ChupNhanh())
        {
            return;
        }

        _kho.GhiNhan(anhChup, "Sửa danh mục vật tư", phatSuKien: false);
        _lblTrangThai.Text = "Đã lưu thay đổi. Bấm Ctrl+Z nếu muốn quay lại.";
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
