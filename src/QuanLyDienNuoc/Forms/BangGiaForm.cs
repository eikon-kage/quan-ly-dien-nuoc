using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Bảng giá riêng của một khách: mỗi vật tư có thể có giá khác giá chung.</summary>
public sealed class BangGiaForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly Guid _khachId;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<DongGia> _nguon = new();
    private readonly TextBox _txtTim = Theme.O(360);
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    private string? _anhChupTruocKhiSua;

    public BangGiaForm(Guid khachId)
    {
        _khachId = khachId;

        Text = "Bảng giá của khách";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1020, 700);
        MinimumSize = new Size(900, 600);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        Nap();
    }

    private KhachHang? Khach => _kho.TimKhach(_khachId);

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                $"BẢNG GIÁ RIÊNG – {Khach?.Ten}",
                "Bỏ trống hoặc để 0 ở cột GIÁ RIÊNG nghĩa là dùng giá chung của cửa hàng",
                tuCao: true),
            0,
            0);

        _txtTim.TextChanged += (_, _) => Nap();
        var thanhTim = Theme.HangO(Theme.Nen, Theme.Truong("TÌM VẬT TƯ", _txtTim, 380));

        Theme.ApDungLuoi(_luoi);
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongGia.Ten), "TÊN HÀNG", 280, toiThieu: 150),
            Theme.Cot(nameof(DongGia.Nhom), "NHÓM", 130),
            Theme.Cot(nameof(DongGia.DonVi), "ĐƠN VỊ", 90),
            Theme.Cot(nameof(DongGia.GiaChung), "GIÁ CHUNG", 130, "#,##0", canPhai: true, toiThieu: 116),
            Theme.Cot(nameof(DongGia.GiaRieng), "GIÁ RIÊNG", 130, "#,##0", canPhai: true, chiDoc: false, toiThieu: 116));
        Theme.ChoPhepGoSo(_luoi, nameof(DongGia.GiaRieng));

        _luoi.CellBeginEdit += (_, _) => _anhChupTruocKhiSua = _kho.ChupNhanh();
        _luoi.CellEndEdit += Luoi_CellEndEdit;
        _luoi.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (_luoi.Columns[e.ColumnIndex].DataPropertyName == nameof(DongGia.GiaRieng)
                && e.CellStyle is { } kieu)
            {
                kieu.Font = Theme.FontLuoiDam;
                if (e.Value is decimal gia && gia <= 0)
                {
                    e.Value = string.Empty;
                    e.FormattingApplied = true;
                }
                else
                {
                    kieu.ForeColor = Theme.Chinh;
                }
            }
        };
        _luoi.DataSource = _nguon;

        var vienLuoi = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 6, 20, 0), BackColor = Theme.Nen };
        vienLuoi.Controls.Add(Theme.Khung(_luoi));

        var btnBoGia = Theme.NutPhu("Bỏ giá riêng của dòng này", 280, 46, noTheoChu: true);
        btnBoGia.Click += (_, _) => BoGiaRieng();

        var btnDong = Theme.NutPhu("Đóng", 140, 46, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;

        khung.Controls.Add(thanhTim, 0, 1);
        khung.Controls.Add(vienLuoi, 0, 2);
        khung.Controls.Add(Theme.ThanhDuoi(_lblTrangThai, btnBoGia, btnDong), 0, 3);
        Controls.Add(khung);
    }

    private void Nap()
    {
        if (Khach is not { } khach)
        {
            Close();
            return;
        }

        var dangChon = (_luoi.CurrentRow?.DataBoundItem as DongGia)?.VT.Id;

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();

        foreach (var vatTu in _kho.DuLieu.VatTus.OrderBy(v => v.Ten, StringComparer.CurrentCultureIgnoreCase))
        {
            if (!ChuViet.Chua(vatTu.Ten, _txtTim.Text))
            {
                continue;
            }

            khach.BangGiaRieng.TryGetValue(vatTu.Id, out var giaRieng);
            _nguon.Add(new DongGia
            {
                VT = vatTu,
                Ten = vatTu.Ten,
                Nhom = _kho.TenNhom(vatTu),
                DonVi = vatTu.DonVi,
                GiaChung = vatTu.DonGiaMacDinh,
                GiaRieng = giaRieng,
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();

        if (dangChon is { } id)
        {
            for (var i = 0; i < _luoi.Rows.Count; i++)
            {
                if (_luoi.Rows[i].DataBoundItem is DongGia dong && dong.VT.Id == id)
                {
                    _luoi.CurrentCell = _luoi.Rows[i].Cells["col" + nameof(DongGia.GiaRieng)];
                    break;
                }
            }
        }

        _lblTrangThai.Text = $"{khach.BangGiaRieng.Count(g => g.Value > 0)} mặt hàng có giá riêng";
    }

    private void Luoi_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        var anhChup = _anhChupTruocKhiSua;
        _anhChupTruocKhiSua = null;

        if (anhChup is null
            || e.RowIndex < 0
            || Khach is not { } khach
            || _luoi.Rows[e.RowIndex].DataBoundItem is not DongGia dong)
        {
            return;
        }

        khach.BangGiaRieng.TryGetValue(dong.VT.Id, out var giaCu);
        if (giaCu == dong.GiaRieng)
        {
            return;
        }

        if (dong.GiaRieng > 0)
        {
            khach.BangGiaRieng[dong.VT.Id] = dong.GiaRieng;
        }
        else
        {
            khach.BangGiaRieng.Remove(dong.VT.Id);
        }

        _kho.GhiNhan(anhChup, $"Đặt giá riêng cho \"{dong.Ten}\"", phatSuKien: false);
        _lblTrangThai.Text = dong.GiaRieng > 0
            ? $"Đã đặt giá {So.Tien(dong.GiaRieng)} cho \"{dong.Ten}\""
            : $"Đã bỏ giá riêng của \"{dong.Ten}\"";
    }

    private void BoGiaRieng()
    {
        var hang = _luoi.CurrentRow;
        if (Khach is not { } khach || hang?.DataBoundItem is not DongGia dong)
        {
            return;
        }

        if (!khach.BangGiaRieng.ContainsKey(dong.VT.Id))
        {
            return;
        }

        _kho.ThucHien(
            $"Bỏ giá riêng của \"{dong.Ten}\"",
            () => khach.BangGiaRieng.Remove(dong.VT.Id),
            phatSuKien: false);

        dong.GiaRieng = 0m;
        _luoi.InvalidateRow(hang.Index);
        _lblTrangThai.Text = $"Đã bỏ giá riêng của \"{dong.Ten}\"";
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
                case Keys.Escape:
                    Close();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private sealed class DongGia
    {
        public VatTu VT { get; set; } = null!;

        public string Ten { get; set; } = string.Empty;

        public string Nhom { get; set; } = string.Empty;

        public string DonVi { get; set; } = string.Empty;

        public decimal GiaChung { get; set; }

        public decimal GiaRieng { get; set; }
    }
}
