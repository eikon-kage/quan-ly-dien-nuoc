using System.ComponentModel;
using QuanLyDienNuoc.Excel;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Nhập nhiều khách hàng một lúc từ file Excel/CSV. Không bắt người dùng nhớ cột nào là cột
/// mấy: có nút tải file mẫu ngay trong màn hình, thứ tự cột ghi rõ ở phụ đề, và bảng xem
/// trước hiện đúng "1 TÊN · 2 ĐIỆN THOẠI · 3 ĐỊA CHỈ · 4 GHI CHÚ" để soát lại trước khi ghi.
/// </summary>
public sealed class NhapKhachForm : Form
{
    private readonly IReadOnlyList<KhachHang> _khachDaCo;

    private readonly DataGridView _luoi = new();
    private readonly BindingList<DongKhachNhap> _nguon = new();
    private readonly TextBox _txtFile = Theme.O(520);
    private readonly Label _lblTomTat = new();
    private readonly Label _lblCanhBao = new();
    private readonly Button _btnNhap = Theme.Nut("NHẬP VÀO SỔ", Theme.Xanh, 260, 52);
    private readonly Button _btnChon = Theme.Nut("Chọn file...", Theme.Chinh, 170, 34);

    private bool _dangNap;

    public NhapKhachForm(IReadOnlyList<KhachHang> khachDaCo, string? duongDanFile = null)
    {
        _khachDaCo = khachDaCo;

        Text = "Nhập khách hàng từ file";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1240, 800);
        MinimumSize = new Size(1080, 700);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        TaoGiaoDien();

        if (!string.IsNullOrWhiteSpace(duongDanFile))
        {
            NapFile(duongDanFile);
        }
        else
        {
            CapNhatTomTat();
        }
    }

    /// <summary>Các khách sẽ thêm vào sổ, chỉ có sau khi bấm Nhập.</summary>
    public List<KhachHang> KetQua { get; } = new();

    private void TaoGiaoDien()
    {
        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Nen,
        };
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));

        khung.Controls.Add(
            Theme.ThanhTieuDe(
                "NHẬP KHÁCH HÀNG TỪ FILE",
                "File theo mẫu, cột xếp đúng thứ tự:  1 tên khách hàng  ·  2 điện thoại  ·  3 địa chỉ  ·  4 ghi chú"),
            0,
            0);
        khung.Controls.Add(TaoThanhChon(), 0, 1);
        khung.Controls.Add(TaoThanNoiDung(), 0, 2);
        khung.Controls.Add(TaoThanhCanhBao(), 0, 3);
        khung.Controls.Add(TaoThanhDuoi(), 0, 4);

        Controls.Add(khung);

        // Không để con trỏ nằm ở ô chỉ đọc: mở màn ra là cả đường dẫn bị bôi xanh, nhìn như
        // vừa gõ gì vào đó. Việc đầu tiên của người dùng là chọn file nên đứng luôn ở nút ấy.
        ActiveControl = _btnChon;
    }

    private Control TaoThanhChon()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.ChinhNhat, Padding = new Padding(14, 8, 14, 8) };

        _txtFile.ReadOnly = true;
        _txtFile.BackColor = Color.White;
        _txtFile.Text = "(chưa chọn file)";
        _txtFile.TabStop = false;

        _btnChon.Click += (_, _) => ChonFile();

        // Nút tải mẫu đặt ngay cạnh nút chọn file: người chưa có danh sách thì tải mẫu về
        // điền, khỏi phải đoán phần mềm chờ file kiểu gì.
        var btnMau = Theme.NutPhu("Tải file mẫu...", 190, 34);
        btnMau.ForeColor = Theme.Chinh;
        btnMau.Click += (_, _) => TaiFileMau();

        var hang = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        hang.Controls.Add(Theme.Truong("FILE ĐANG ĐỌC", _txtFile, 520));
        hang.Controls.Add(Theme.Truong(" ", _btnChon, 170));
        hang.Controls.Add(Theme.Truong(" ", btnMau, 190));

        nen.Controls.Add(hang);
        return nen;
    }

    private Control TaoThanNoiDung()
    {
        var than = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Nen,
            Padding = new Padding(20, 8, 20, 0),
        };
        than.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        than.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblTomTat.Font = Theme.FontDam;
        _lblTomTat.ForeColor = Theme.Xam;
        _lblTomTat.Dock = DockStyle.Fill;
        _lblTomTat.TextAlign = ContentAlignment.MiddleLeft;
        than.Controls.Add(_lblTomTat, 0, 0);

        Theme.ApDungLuoi(_luoi);

        // Gõ là sửa được ngay như mấy bảng khác của phần mềm: sửa tên viết sai trong file
        // ngay tại đây nhanh hơn mở lại Excel rồi nhập lại từ đầu.
        _luoi.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

        var cotChon = new DataGridViewCheckBoxColumn
        {
            Name = "colChon",
            DataPropertyName = nameof(DongKhachNhap.Chon),
            HeaderText = "NHẬP",
            FillWeight = 40,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        _luoi.Columns.Add(cotChon);

        // Tên cột mang luôn số thứ tự cột trong file: mở bảng ra là biết cột 1 đã vào đúng ô
        // tên khách hay chưa, không cần mở lại file để đối chiếu.
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongKhachNhap.SoDong), "DÒNG", 40, "0", canPhai: true),
            Theme.Cot(nameof(DongKhachNhap.Ten), "1 · TÊN KHÁCH HÀNG", 180, chiDoc: false),
            Theme.Cot(nameof(DongKhachNhap.DienThoai), "2 · ĐIỆN THOẠI", 100, chiDoc: false),
            Theme.Cot(nameof(DongKhachNhap.DiaChi), "3 · ĐỊA CHỈ", 160, chiDoc: false),
            Theme.Cot(nameof(DongKhachNhap.GhiChu), "4 · GHI CHÚ", 120, chiDoc: false),
            // Cột tình trạng phải đủ chỗ cho cả câu kèm tên khách bị trùng: cắt mất nửa câu
            // thì người dùng chỉ thấy "không nhập đ..." rồi phải tự đoán vì sao.
            Theme.Cot(nameof(DongKhachNhap.TinhTrangChu), "TÌNH TRẠNG", 230));

        _luoi.DataSource = _nguon;
        _luoi.Dock = DockStyle.Fill;

        // Tích ô vuông xong phải chốt ngay, không thì phải bấm sang dòng khác mới ăn.
        _luoi.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                _luoi.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        _luoi.CellValueChanged += (_, e) =>
        {
            if (_dangNap || e.RowIndex < 0 || e.RowIndex >= _nguon.Count)
            {
                return;
            }

            if (e.ColumnIndex == 0)
            {
                _nguon[e.RowIndex].TuTayChon = true;
            }

            ChamLai();
        };

        _luoi.CellFormatting += Luoi_CellFormatting;

        than.Controls.Add(Theme.Khung(_luoi), 0, 1);
        return than;
    }

    private Control TaoThanhCanhBao()
    {
        _lblCanhBao.Dock = DockStyle.Fill;
        _lblCanhBao.Font = Theme.FontPhu;
        _lblCanhBao.ForeColor = Theme.Cam;
        _lblCanhBao.TextAlign = ContentAlignment.MiddleLeft;
        _lblCanhBao.Padding = new Padding(22, 0, 22, 0);

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen };
        nen.Controls.Add(_lblCanhBao);
        return nen;
    }

    private Control TaoThanhDuoi()
    {
        _btnNhap.Click += (_, _) => Nhap();

        var btnHuy = Theme.NutPhu("Huỷ", 140, 52);
        btnHuy.Click += (_, _) => DialogResult = DialogResult.Cancel;

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(_btnNhap);
        trai.Controls.Add(btnHuy);

        var ghiChu = new Label
        {
            Dock = DockStyle.Right,
            Width = 560,
            Font = Theme.FontPhu,
            ForeColor = Theme.Xam,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "Sửa được ngay trên bảng trước khi nhập · Ctrl+Z hoàn tác cả lô vừa nhập",
        };

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 12, 20, 10) };
        nen.Controls.Add(trai);
        nen.Controls.Add(ghiChu);

        AcceptButton = _btnNhap;
        CancelButton = btnHuy;
        return nen;
    }

    private void Luoi_CellFormatting(object? nguoiGui, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _nguon.Count)
        {
            return;
        }

        if (e.CellStyle is not { } kieu)
        {
            return;
        }

        var dong = _nguon[e.RowIndex];
        var thuocTinh = _luoi.Columns[e.ColumnIndex].DataPropertyName;

        if (thuocTinh == nameof(DongKhachNhap.TinhTrangChu))
        {
            kieu.ForeColor = dong.TinhTrang switch
            {
                TinhTrangDongKhach.ThemMoi => Theme.Xanh,
                TinhTrangDongKhach.ThieuTen => Theme.Do,
                TinhTrangDongKhach.KhongGiongTen => Theme.XamNhat,
                _ => Theme.Cam,
            };
        }
        else if (thuocTinh == nameof(DongKhachNhap.Ten))
        {
            kieu.Font = Theme.FontLuoiDam;
        }
    }

    // ---------------- Việc ----------------

    private void ChonFile()
    {
        using var hopThoai = new OpenFileDialog
        {
            Title = "Chọn file danh sách khách hàng",
            Filter = "File Excel hoặc CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Tất cả các file (*.*)|*.*",
        };

        if (hopThoai.ShowDialog(this) == DialogResult.OK)
        {
            NapFile(hopThoai.FileName);
        }
    }

    private void TaiFileMau()
    {
        using var hopThoai = new SaveFileDialog
        {
            Title = "Lưu file mẫu danh sách khách hàng",
            Filter = "File Excel (*.xlsx)|*.xlsx",
            FileName = "Mau-danh-sach-khach-hang.xlsx",
        };

        if (hopThoai.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            NhapKhachHang.XuatFileMau(hopThoai.FileName);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không lưu được file mẫu:\n" + ex.Message);
            return;
        }

        if (HopThoai.Hoi(
                this,
                $"Đã lưu file mẫu:\n{hopThoai.FileName}\n\n" +
                "Điền mỗi khách một dòng vào sheet \"Khách hàng\", lưu lại rồi bấm \"Chọn file...\".\n\n" +
                "Mở file mẫu lên điền luôn không?"))
        {
            MoFile(hopThoai.FileName);
        }
    }

    private void NapFile(string duongDan)
    {
        KetQuaNhapKhach ketQua;
        try
        {
            ketQua = NhapKhachHang.Doc(duongDan, _khachDaCo);
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, $"Không đọc được file:\n{duongDan}\n\n{ex.Message}");
            return;
        }

        _txtFile.Text = duongDan;

        _dangNap = true;
        _nguon.RaiseListChangedEvents = false;
        _nguon.Clear();
        foreach (var dong in ketQua.Dong)
        {
            _nguon.Add(dong);
        }

        _nguon.RaiseListChangedEvents = true;
        _nguon.ResetBindings();
        _dangNap = false;

        _lblCanhBao.Text = ketQua.CanhBao.Count > 0
            ? "⚠  " + string.Join("  ·  ", ketQua.CanhBao)
            : string.Empty;

        CapNhatTomTat(ketQua);

        // Chọn nhầm một tờ hoá đơn là chuyện thường: hai việc đều mang chữ "nhập từ Excel".
        // Nói rõ file là gì và chỉ sang đúng chỗ, chứ không đọc bừa theo thứ tự cột.
        if (ketQua.LaHoaDon)
        {
            _lblTomTat.Text = "File này là một tờ hoá đơn, không phải danh sách khách hàng.";
            HopThoai.CanhBao(
                this,
                "File này là một tờ hoá đơn (có bảng tên hàng · đvt · số lượng), không phải " +
                "danh sách khách hàng.\n\n" +
                "• Muốn nhập hàng từ hoá đơn Excel: mở đơn hàng của khách rồi bấm " +
                "\"Nhập từ Excel\".\n" +
                "• Muốn nhập danh sách khách: bấm \"Tải file mẫu...\" ở trên, điền mỗi khách " +
                "một dòng rồi chọn lại file.");
            return;
        }

        if (ketQua.Dong.Count == 0)
        {
            HopThoai.CanhBao(
                this,
                "File này không có dòng khách nào đọc được.\n\n" +
                "Kiểm tra lại: mỗi khách một dòng, cột 1 là tên khách hàng, và giữ nguyên " +
                "dòng tiêu đề của file mẫu.");
        }
    }

    private void ChamLai()
    {
        NhapKhachHang.ChamLaiTinhTrang(_nguon, _khachDaCo);

        _dangNap = true;
        _luoi.Refresh();
        _dangNap = false;

        CapNhatTomTat();
    }

    private void CapNhatTomTat(KetQuaNhapKhach? ketQua = null)
    {
        var seNhap = _nguon.Count(d => d.Chon);
        _btnNhap.Text = seNhap > 0 ? $"NHẬP {seNhap} KHÁCH VÀO SỔ" : "NHẬP VÀO SỔ";
        _btnNhap.Enabled = seNhap > 0;

        if (_nguon.Count == 0)
        {
            _lblTomTat.Text = _txtFile.Text.StartsWith('(')
                ? "Chưa chọn file. Chưa có danh sách thì bấm \"Tải file mẫu...\" để lấy file về điền."
                : "File không có dòng khách nào đọc được.";
            return;
        }

        var trung = _nguon.Count(d =>
            d.TinhTrang is TinhTrangDongKhach.TrungKhachCu or TinhTrangDongKhach.TrungTrongFile);
        var thieuTen = _nguon.Count(d => d.TinhTrang == TinhTrangDongKhach.ThieuTen);
        var khongGiong = _nguon.Count(d => d.TinhTrang == TinhTrangDongKhach.KhongGiongTen);

        var cau = $"Đọc được {_nguon.Count} dòng";
        if (ketQua is not null && !string.IsNullOrEmpty(ketQua.TenBang))
        {
            cau += $" ở bảng \"{ketQua.TenBang}\"";
        }

        cau += $" · sẽ nhập {seNhap} khách";
        if (trung > 0)
        {
            cau += $" · {trung} dòng trùng tên đã bỏ tích";
        }

        if (thieuTen > 0)
        {
            cau += $" · {thieuTen} dòng thiếu tên";
        }

        if (khongGiong > 0)
        {
            cau += $" · {khongGiong} dòng không giống tên khách";
        }

        _lblTomTat.Text = cau;
    }

    private void Nhap()
    {
        var chon = _nguon.Where(d => d.Chon && d.Ten.Trim().Length > 0).ToList();
        if (chon.Count == 0)
        {
            HopThoai.CanhBao(this, "Chưa có dòng nào được tích ở cột NHẬP.");
            return;
        }

        var trung = chon.Count(d => d.TinhTrang != TinhTrangDongKhach.ThemMoi);
        if (trung > 0 && !HopThoai.Hoi(
                this,
                $"Trong {chon.Count} dòng sắp nhập có {trung} dòng trùng tên với khách đã có.\n\n" +
                "Thêm nữa là một người thành hai khách, công nợ bị chia đôi. Vẫn nhập?"))
        {
            return;
        }

        var homNay = DateTime.Today;
        KetQua.Clear();
        KetQua.AddRange(chon.Select(d => d.ThanhKhachHang(homNay)));
        DialogResult = DialogResult.OK;
    }

    private void MoFile(string duongDan)
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
            HopThoai.CanhBao(this, "Không mở được file (máy chưa cài Excel hoặc WPS?):\n" + ex.Message);
        }
    }
}
