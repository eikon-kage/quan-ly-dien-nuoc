using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Thêm, đổi tên, xoá nhóm hàng. Nhóm là bản ghi riêng nên đổi tên ở đây là mọi mặt hàng
/// trong nhóm đổi theo; xoá nhóm thì hàng trong nhóm trở về "chưa đặt nhóm", không mất gì.
/// </summary>
public sealed class NhomHangForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<DongNhom> _nguon = new();
    private readonly TextBox _txtTen = Theme.O(320);
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    private string? _anhChupTruocKhiSua;
    private string _tenTruocKhiSua = string.Empty;

    public NhomHangForm()
    {
        Text = "Nhóm hàng";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(680, 520);
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
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại — cùng nếp với các
        // màn khác, xem "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "NHÓM HÀNG",
                "Nhóm để lọc danh mục vật tư cho gọn (Ống nước, Điện, Đèn…)  ·  đổi tên nhóm ở đây "
                + "là đổi cho mọi mặt hàng trong nhóm",
                tuCao: true),
            0,
            0);

        // Thêm nhanh
        var btnThem = Theme.Nut("+  THÊM NHÓM", Theme.Xanh, 200, 34, noTheoChu: true);
        btnThem.Click += (_, _) => Them();
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
            Theme.Truong("TÊN NHÓM", _txtTen, 340),
            nhomNut);

        _txtTen.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Them();
            }
        };

        // Lưới
        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongNhom.Ten), "TÊN NHÓM", 300, chiDoc: false, toiThieu: 150),
            Theme.Cot(nameof(DongNhom.SoMatHang), "SỐ MẶT HÀNG", 150, "#,##0", canPhai: true));
        _luoi.CellBeginEdit += Luoi_CellBeginEdit;
        _luoi.CellEndEdit += Luoi_CellEndEdit;
        _luoi.DataSource = _nguon;

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(_luoi));

        // Thanh dưới
        var btnXoa = Theme.NutPhu("Xoá nhóm", 170, 46, noTheoChu: true);
        btnXoa.ForeColor = Theme.Do;
        btnXoa.Click += (_, _) => Xoa();

        var btnDong = Theme.NutPhu("Đóng", 140, 46, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.Text = "Bấm đúp vào tên nhóm để sửa  ·  Ctrl+Z hoàn tác";

        khung.Controls.Add(nenThem, 0, 1);
        khung.Controls.Add(vienLuoi, 0, 2);
        khung.Controls.Add(Theme.ThanhDuoi(_lblTrangThai, btnXoa, btnDong), 0, 3);
        Controls.Add(khung);

        ActiveControl = _txtTen;
    }

    private void Nap(Guid? chon = null)
    {
        var dangChon = chon ?? (_luoi.CurrentRow?.DataBoundItem as DongNhom)?.N.Id;

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var nhom in _kho.NhomTheoTen())
        {
            _nguon.Add(new DongNhom
            {
                N = nhom,
                SoMatHang = _kho.DuLieu.VatTus.Count(v => v.NhomId == nhom.Id),
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            for (var i = 0; i < _luoi.Rows.Count; i++)
            {
                if (_luoi.Rows[i].DataBoundItem is DongNhom dong && dong.N.Id == id)
                {
                    _luoi.CurrentCell = _luoi.Rows[i].Cells[0];
                    break;
                }
            }
        }
    }

    private void Them()
    {
        var ten = _txtTen.Text.Trim();
        if (ten.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập tên nhóm.");
            _txtTen.Focus();
            return;
        }

        if (_kho.TimNhomTheoTen(ten) is not null)
        {
            HopThoai.CanhBao(this, $"Đã có nhóm \"{ten}\".");
            return;
        }

        var moi = new NhomHang { Ten = ten };
        _kho.ThucHien($"Thêm nhóm hàng \"{ten}\"", () => _kho.DuLieu.NhomHangs.Add(moi), phatSuKien: false);

        _txtTen.Clear();
        _txtTen.Focus();

        Nap(moi.Id);
        _lblTrangThai.Text = $"Đã thêm nhóm \"{ten}\"";
    }

    private void Xoa()
    {
        if (_luoi.CurrentRow?.DataBoundItem is not DongNhom dong)
        {
            HopThoai.CanhBao(this, "Hãy chọn một nhóm để xoá.");
            return;
        }

        var canhBao = dong.SoMatHang > 0
            ? $"\n\n{dong.SoMatHang} mặt hàng đang ở nhóm này sẽ thành \"chưa đặt nhóm\". "
              + "Tên hàng, mã tắt, giá vẫn giữ nguyên."
            : string.Empty;

        if (!HopThoai.Hoi(this, $"Xoá nhóm \"{dong.N.Ten}\"?{canhBao}\n\n(Ctrl+Z để lấy lại.)"))
        {
            return;
        }

        _kho.ThucHien($"Xoá nhóm hàng \"{dong.N.Ten}\"", () =>
        {
            foreach (var vatTu in _kho.DuLieu.VatTus.Where(v => v.NhomId == dong.N.Id))
            {
                vatTu.NhomId = null;
            }

            _kho.DuLieu.NhomHangs.Remove(dong.N);
        }, phatSuKien: false);

        Nap();
        _lblTrangThai.Text = $"Đã xoá nhóm \"{dong.N.Ten}\". Bấm Ctrl+Z để lấy lại.";
    }

    private void Luoi_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        _anhChupTruocKhiSua = _kho.ChupNhanh();
        _tenTruocKhiSua = (_luoi.Rows[e.RowIndex].DataBoundItem as DongNhom)?.N.Ten ?? string.Empty;
    }

    private void Luoi_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (_luoi.Rows[e.RowIndex].DataBoundItem is not DongNhom dong)
        {
            return;
        }

        // Tên nhóm để trống, hay trùng nhóm khác, thì trả về tên cũ ngay — hai nhóm cùng tên
        // là lúc lọc không biết đâu là đâu.
        var ten = dong.N.Ten.Trim();
        var loi = ten.Length == 0
            ? "Tên nhóm không được để trống."
            : _kho.DuLieu.NhomHangs.Any(n => n.Id != dong.N.Id
                && string.Equals(n.Ten, ten, StringComparison.CurrentCultureIgnoreCase))
                ? $"Đã có nhóm \"{ten}\"."
                : null;

        if (loi is not null)
        {
            dong.N.Ten = _tenTruocKhiSua;
            Nap(dong.N.Id);
            HopThoai.CanhBao(this, loi);
            return;
        }

        dong.N.Ten = ten;

        if (anhChup is null || anhChup == _kho.ChupNhanh())
        {
            return;
        }

        _kho.GhiNhan(anhChup, $"Đổi tên nhóm hàng thành \"{ten}\"", phatSuKien: false);
        Nap(dong.N.Id);
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

    /// <summary>Một dòng của lưới: nhóm thật cộng số mặt hàng đang ở trong nhóm.</summary>
    private sealed class DongNhom
    {
        public NhomHang N { get; init; } = null!;

        /// <summary>Sửa ở lưới là ghi thẳng vào bản ghi nhóm.</summary>
        public string Ten
        {
            get => N.Ten;
            set => N.Ten = value;
        }

        public int SoMatHang { get; init; }
    }
}
