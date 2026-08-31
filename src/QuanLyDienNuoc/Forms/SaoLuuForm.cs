using System.ComponentModel;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Sao lưu và khôi phục dữ liệu. Mỗi bản sao lưu gồm một file JSON (để nạp ngược lại vào
/// phần mềm) và một file Excel nhiều trang (để mở xem bằng Excel/WPS mà không cần phần mềm).
/// </summary>
public sealed class SaoLuuForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;
    private readonly BindingList<DongLuoi> _nguon = new();

    private readonly TextBox _txtThuMuc = Theme.O(560);
    private readonly NumericUpDown _numGiuLai = new();
    private readonly CheckBox _chkTuDong = new();
    private readonly CheckBox _chkKemExcel = new();
    private readonly DataGridView _luoi = new();
    private readonly Label _lblTrangThai = Theme.NhanDaiDong();

    private bool _dangNap;

    public SaoLuuForm()
    {
        Text = "Sao lưu dữ liệu";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(960, 640);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();
        NapCaiDat();
        NapDanhSach();
    }

    private BanSaoLuu? DangChon => (_luoi.CurrentRow?.DataBoundItem as DongLuoi)?.Ban;

    // ---------------- Giao diện ----------------

    private void TaoGiaoDien()
    {
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        // Dòng nào có chữ thì tự cao theo chữ, chỉ bảng ăn phần còn lại: xem "Chữ bị cắt"
        // trong docs/giao-dien-may-tinh.md.
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        goc.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "SAO LƯU DỮ LIỆU",
                "Mất máy hay hỏng file là mất hết  ·  nên để thư mục sao lưu ở USB hay Google Drive",
                tuCao: true),
            0,
            0);
        goc.Controls.Add(TaoBangCaiDat(), 0, 1);
        goc.Controls.Add(TaoLuoi(), 0, 2);
        goc.Controls.Add(TaoThanhDuoi(), 0, 3);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 4);

        Controls.Add(goc);
    }

    /// <summary>
    /// Khối cài đặt sao lưu. Hai hàng ô xếp bằng <see cref="Theme.HangO"/> chứ không đặt toạ độ
    /// cứng <c>Location = (20, 10)</c> và <c>(20, 88)</c> trong một dòng cao cứng 190px: cỡ chữ
    /// to lên là hàng dưới đè lên hàng trên rồi cả hai cùng tràn khỏi dải xanh.
    /// </summary>
    private Control TaoBangCaiDat()
    {
        var nen = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.ChinhNhat,
        };
        nen.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        nen.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var btnChon = Theme.NutPhu("Chọn thư mục…", 190, 34, noTheoChu: true);
        btnChon.Margin = new Padding(0, Theme.DinhOTrongTruong, 0, 0);
        btnChon.Click += (_, _) => ChonThuMuc();

        _numGiuLai.Minimum = 1;
        _numGiuLai.Maximum = 365;
        _numGiuLai.Font = Theme.FontNhap;
        _numGiuLai.ValueChanged += (_, _) => LuuCaiDat();

        _chkTuDong.Text = "Tự sao lưu mỗi ngày khi mở phần mềm";
        _chkTuDong.Font = Theme.FontThuong;
        _chkTuDong.AutoSize = true;
        _chkTuDong.Margin = new Padding(0, Theme.DinhOTrongTruong + 4, 24, 0);
        _chkTuDong.CheckedChanged += (_, _) => LuuCaiDat();

        _chkKemExcel.Text = "Kèm file Excel (mở xem được không cần phần mềm)";
        _chkKemExcel.Font = Theme.FontThuong;
        _chkKemExcel.AutoSize = true;
        _chkKemExcel.Margin = new Padding(0, Theme.DinhOTrongTruong + 4, 0, 0);
        _chkKemExcel.CheckedChanged += (_, _) => LuuCaiDat();

        nen.Controls.Add(
            Theme.HangO(Theme.ChinhNhat, Theme.Truong("THƯ MỤC SAO LƯU", _txtThuMuc, 600), btnChon),
            0,
            0);
        nen.Controls.Add(
            Theme.HangO(
                Theme.ChinhNhat,
                Theme.Truong("GIỮ LẠI BAO NHIÊU BẢN", _numGiuLai, 220),
                _chkTuDong,
                _chkKemExcel),
            0,
            1);
        return nen;
    }

    private Control TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongLuoi.Luc), "SAO LƯU LÚC", 155, "dd/MM/yyyy HH:mm", toiThieu: 150),
            Theme.Cot(nameof(DongLuoi.SoKhach), "SỐ KHÁCH", 95, canPhai: true),
            Theme.Cot(nameof(DongLuoi.KichThuoc), "DUNG LƯỢNG", 115, canPhai: true),
            Theme.Cot(nameof(DongLuoi.CoExcel), "CÓ EXCEL", 95),
            Theme.Cot(nameof(DongLuoi.DuongDan), "FILE", 420, toiThieu: 160));

        _luoi.DataSource = _nguon;
        _luoi.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                MoThuMuc();
            }
        };

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 10), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_luoi));
        return vien;
    }

    private Control TaoThanhDuoi()
    {
        var btnSaoLuu = Theme.Nut("SAO LƯU NGAY", Theme.Xanh, 230, 52, noTheoChu: true);
        btnSaoLuu.Click += (_, _) => SaoLuuNgay();

        // Chỉ để ngoài việc bấm hằng tuần là sao lưu. Khôi phục thì mấy năm mới dùng một
        // lần mà lại là việc nguy hiểm nhất — nằm trong menu, đỏ, khó bấm nhầm hơn.
        var viecKhac = Theme.NutBaCham("Việc khác với bản sao lưu", 52)
            .Viec("Xuất toàn bộ ra Excel", XuatExcel)
            .Viec("Mở thư mục sao lưu", MoThuMuc)
            .Ngan()
            .Viec("Khôi phục bản đã chọn", KhoiPhuc, Theme.Do);

        var btnDong = Theme.NutPhu("Đóng", 120, 52, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        return Theme.ThanhDuoi(null, btnSaoLuu, viecKhac.Nut, btnDong);
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Text = $"Dữ liệu đang dùng: {_kho.DuongDanFile}";
        return Theme.ThanhTrangThai(_lblTrangThai);
    }

    // ---------------- Nạp / lưu cài đặt ----------------

    private void NapCaiDat()
    {
        _dangNap = true;
        _txtThuMuc.Text = _kho.CaiDat.ThuMucSaoLuuThat(_kho.DuongDanFile);
        _numGiuLai.Value = Math.Clamp(_kho.CaiDat.SoBanSaoLuuGiuLai, _numGiuLai.Minimum, _numGiuLai.Maximum);
        _chkTuDong.Checked = _kho.CaiDat.TuDongSaoLuu;
        _chkKemExcel.Checked = _kho.CaiDat.SaoLuuKemExcel;
        _dangNap = false;
    }

    private void LuuCaiDat()
    {
        if (_dangNap)
        {
            return;
        }

        _kho.CaiDat.ThuMucSaoLuu = _txtThuMuc.Text.Trim();
        _kho.CaiDat.SoBanSaoLuuGiuLai = (int)_numGiuLai.Value;
        _kho.CaiDat.TuDongSaoLuu = _chkTuDong.Checked;
        _kho.CaiDat.SaoLuuKemExcel = _chkKemExcel.Checked;
        _kho.LuuCaiDat();
    }

    private void NapDanhSach()
    {
        var thuMuc = _kho.CaiDat.ThuMucSaoLuuThat(_kho.DuongDanFile);

        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        foreach (var ban in SaoLuu.DanhSach(thuMuc))
        {
            _nguon.Add(new DongLuoi
            {
                Ban = ban,
                Luc = ban.Luc,
                SoKhach = DemKhach(ban.DuongDanJson),
                KichThuoc = DungLuong(ban.KichThuoc),
                CoExcel = ban.CoExcel ? "Có" : "—",
                DuongDan = ban.DuongDanJson,
            });
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();
    }

    // ---------------- Thao tác ----------------

    private void ChonThuMuc()
    {
        using var chon = new FolderBrowserDialog
        {
            Description = "Chọn thư mục để cất các bản sao lưu (nên là USB hoặc thư mục đồng bộ lên mạng)",
            UseDescriptionForTitle = true,
            SelectedPath = _txtThuMuc.Text,
        };

        if (chon.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _txtThuMuc.Text = chon.SelectedPath;
        LuuCaiDat();
        NapDanhSach();
        _lblTrangThai.Text = $"Thư mục sao lưu: {chon.SelectedPath}";
    }

    private void SaoLuuNgay()
    {
        LuuCaiDat();

        try
        {
            var ban = SaoLuu.Tao(_kho, _kho.CaiDat);
            NapDanhSach();
            _lblTrangThai.Text = $"Đã sao lưu lúc {ban.Luc:HH:mm dd/MM/yyyy} → {ban.DuongDanJson}";
            _kho.NhatKy.Ghi("Sao lưu dữ liệu", ban.DuongDanJson);
            HopThoai.Bao(this, $"Đã sao lưu xong:\n{ban.DuongDanJson}" + (ban.CoExcel ? $"\n{ban.DuongDanExcel}" : string.Empty));
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không sao lưu được:\n" + ex.Message);
        }
    }

    private void XuatExcel()
    {
        using var hopThoai = new SaveFileDialog
        {
            Title = "Xuất toàn bộ dữ liệu ra Excel",
            Filter = "File Excel (*.xlsx)|*.xlsx",
            FileName = $"Toan bo du lieu {DateTime.Today:dd-MM-yyyy}.xlsx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (hopThoai.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            XuatToanBo.Xuat(_kho.DuLieu, hopThoai.FileName, DateTime.Today);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không xuất được file:\n" + ex.Message);
            return;
        }

        _lblTrangThai.Text = $"Đã xuất: {hopThoai.FileName}";
        if (HopThoai.Hoi(this, $"Đã xuất xong:\n{hopThoai.FileName}\n\nMở file lên xem luôn không?"))
        {
            Mo(hopThoai.FileName);
        }
    }

    private void KhoiPhuc()
    {
        if (DangChon is not { } ban)
        {
            HopThoai.CanhBao(this, "Hãy chọn bản sao lưu muốn khôi phục.");
            return;
        }

        // Khôi phục là chép đè thẳng lên file dữ liệu, máy khác đang mở thì tuyệt đối không được.
        if (_kho.ChiXem)
        {
            HopThoai.CanhBao(
                this,
                $"Đang mở ở chế độ CHỈ XEM nên không khôi phục được.\n\n{_kho.LyDoChiXem}.");
            return;
        }

        if (!HopThoai.Hoi(
                this,
                $"Khôi phục dữ liệu từ bản lúc {ban.TenHienThi}?\n\n" +
                "Toàn bộ dữ liệu hiện tại sẽ bị thay bằng bản này.\n" +
                "Bản hiện tại được cất lại thành file \"truoc-khi-khoi-phuc-…\" trong thư mục sao lưu.\n\n" +
                "Lưu ý: Ctrl+Z KHÔNG lấy lại được sau bước này."))
        {
            return;
        }

        try
        {
            SaoLuu.KhoiPhuc(_kho, _kho.CaiDat, ban);
            _kho.NhatKy.Ghi("Khôi phục từ bản sao lưu", ban.DuongDanJson);
            NapDanhSach();
            _lblTrangThai.Text = $"Đã khôi phục từ bản lúc {ban.TenHienThi}.";
            HopThoai.Bao(this, $"Đã khôi phục xong từ bản lúc {ban.TenHienThi}.");
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không khôi phục được:\n" + ex.Message);
        }
    }

    private void MoThuMuc()
    {
        var thuMuc = _kho.CaiDat.ThuMucSaoLuuThat(_kho.DuongDanFile);
        Directory.CreateDirectory(thuMuc);
        Mo(thuMuc);
    }

    private void Mo(string duongDan)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(duongDan)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            HopThoai.CanhBao(this, "Không mở được:\n" + ex.Message);
        }
    }

    private static string DungLuong(long soByte) => soByte switch
    {
        < 1024 => $"{soByte} B",
        < 1024 * 1024 => $"{soByte / 1024d:0.#} KB",
        _ => $"{soByte / (1024d * 1024d):0.#} MB",
    };

    /// <summary>Đếm nhanh số khách trong một bản sao lưu để nhìn là biết bản nào đầy đủ.</summary>
    private static int DemKhach(string duongDanJson)
    {
        try
        {
            var kho = new KhoDuLieu(duongDanJson);
            kho.Nap();
            return kho.DuLieu.KhachHangs.Count;
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            return 0;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Một bản sao lưu trên lưới.</summary>
    private sealed class DongLuoi
    {
        public BanSaoLuu Ban { get; set; } = null!;

        public DateTime Luc { get; set; }

        public int SoKhach { get; set; }

        public string KichThuoc { get; set; } = string.Empty;

        public string CoExcel { get; set; } = string.Empty;

        public string DuongDan { get; set; } = string.Empty;
    }
}
