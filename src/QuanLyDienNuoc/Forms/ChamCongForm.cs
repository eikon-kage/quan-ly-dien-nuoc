using System.ComponentModel;
using ChamCong.SoDiDong;
using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Xem sổ chấm công của thợ trên máy tính. Sổ thật nằm trong app điện thoại; máy tính đọc bản
/// sao lưu mà app ấy đẩy lên tài khoản Supabase của chủ.
///
/// <para>
/// **Chỉ đọc, không ghi gì.** Sửa chấm công thì sửa trên điện thoại. Máy tính ghi vào đấy nữa
/// là hai bên đè sổ lên nhau mà không ai biết — app điện thoại mới là chỗ có đủ luồng hỏi lại
/// trước khi ghi đè.
/// </para>
///
/// <para>
/// Mỗi ngày một bản, giữ 30 ngày gần nhất. Nên chọn được bản của hôm qua là chuyện thường
/// dùng: hôm nay lỡ tay xoá mấy chục buổi công thì mở bản hôm trước ra đối chiếu.
/// </para>
/// </summary>
public sealed class ChamCongForm : Form
{
    private readonly KhoDuLieu _kho = KhoDuLieu.Instance;

    private readonly TextBox _txtDiaChi = Theme.O(320);
    private readonly TextBox _txtKhoa = Theme.O(260);
    private readonly TextBox _txtEmail = Theme.O(220);
    private readonly TextBox _txtMatKhau = Theme.O(160);
    private readonly Button _btnDangNhap = Theme.Nut("ĐĂNG NHẬP VÀ TẢI SỔ", Theme.Chinh, 240, 40);

    private readonly ComboBox _cboBan = new();
    private readonly Button _btnTaiLai = Theme.NutPhu("Tải lại", 120, 40);

    private readonly DataGridView _luoi = new();
    private readonly Label _lblTong = new();
    private readonly Label _lblTrangThai = new();

    private readonly List<(Button Nut, Bang Bang)> _nutBang = new();
    private readonly ThanhPhanTrang _phanTrang = new();

    /// <summary>Cách vẽ lại lưới cho trang đang xem. Đổi bảng là đổi luôn cách vẽ này.</summary>
    private Action _veLaiTrang = () => { };

    private readonly CauHinhChamCong _cauHinh;

    private NguonSupabase? _nguon;
    private SoChamCong _so = new();
    private string _taoLuc = string.Empty;
    private Bang _dangXem = Bang.KyDangMo;
    private bool _dangNap;

    public ChamCongForm()
    {
        Text = "Chấm công của thợ";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 700);
        Size = new Size(1400, 860);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        // Địa chỉ và khoá lấy từ bản dựng nếu có sẵn — người dùng chỉ gõ email với mật khẩu.
        _cauHinh = CauHinhChamCong.MacDinh(_kho.CaiDat.ChamCongDiaChi, _kho.CaiDat.ChamCongKhoaCongKhai);

        TaoGiaoDien();

        _txtDiaChi.Text = _cauHinh.DiaChi;
        _txtKhoa.Text = _cauHinh.KhoaCongKhai;
        _txtEmail.Text = _kho.CaiDat.ChamCongEmail;
        DatTrangThaiChoTai(false);

        _lblTrangThai.Text = _cauHinh.DaCoSan
            ? $"Gõ email và mật khẩu tài khoản chủ rồi bấm ĐĂNG NHẬP VÀ TẢI SỔ. (Khoá Supabase lấy từ {_cauHinh.Nguon}.)"
            : "Bản dựng này chưa có khoá Supabase. Dán địa chỉ project và khoá công khai (anon key) vào rồi đăng nhập.";

        FormClosed += (_, _) => _nguon?.Dispose();
    }

    /// <summary>Năm cách xem cùng một sổ.</summary>
    private enum Bang
    {
        KyDangMo,
        BuoiCong,
        UngTien,
        GhiChu,
        KyDaChot,
    }

    // ---------------- Giao diện ----------------

    private void TaoGiaoDien()
    {
        var goc = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Nen,
        };
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        goc.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        goc.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        goc.Controls.Add(
            Theme.ThanhTieuDe(
                "CHẤM CÔNG CỦA THỢ",
                "Sổ đọc từ app điện thoại qua Supabase. Máy tính chỉ xem — muốn sửa thì sửa trên điện thoại."),
            0,
            0);
        goc.Controls.Add(TaoThanhNoi(), 0, 1);
        goc.Controls.Add(TaoThanhChonBang(), 0, 2);
        goc.Controls.Add(Theme.Khung(TaoLuoi()), 0, 3);
        goc.Controls.Add(TaoThanhTong(), 0, 4);
        goc.Controls.Add(TaoThanhTrangThai(), 0, 5);

        Controls.Add(goc);
    }

    /// <summary>Hàng nối với Supabase: địa chỉ, khoá, email, mật khẩu.</summary>
    private Control TaoThanhNoi()
    {
        var nen = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ChinhNhat,
            Margin = new Padding(20, 8, 20, 0),
            Padding = new Padding(14, 8, 14, 8),
        };

        _txtMatKhau.UseSystemPasswordChar = true;

        var mach = new ToolTip { InitialDelay = 250, AutoPopDelay = 12000 };
        mach.SetToolTip(_txtDiaChi, "Supabase → Project Settings → Data API → Project URL");
        mach.SetToolTip(
            _txtKhoa,
            "Khoá công khai (anon / publishable key). Đây không phải bí mật — nó nằm trong mọi bản app "
            + "điện thoại. Đừng điền service_role key: khoá ấy bỏ qua mọi lớp chặn của database.");
        mach.SetToolTip(_txtMatKhau, "Không được lưu lại. Mở phần mềm lần sau phải gõ lại.");

        _btnDangNhap.Click += async (_, _) => await DangNhapVaTai();
        foreach (var o in new[] { _txtDiaChi, _txtKhoa, _txtEmail, _txtMatKhau })
        {
            o.KeyDown += async (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await DangNhapVaTai();
                }
            };
        }

        const int CaoO = 38;
        const int Le = 12;
        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Margin = new Padding(0),
        };

        // Khoá đã nằm trong bản dựng thì **không hiện hai ô ấy ra nữa**: đưa địa chỉ với khoá
        // công khai ra trước mặt chủ cửa hàng chỉ làm họ hoang mang, mà cũng chẳng có gì để họ
        // sửa. Bản dựng chưa có khoá thì vẫn phải hiện, không thì kẹt hẳn.
        if (!_cauHinh.DaCoSan)
        {
            hang.Controls.Add(Theme.Truong("ĐỊA CHỈ SUPABASE", _txtDiaChi, 320, CaoO, Le));
            hang.Controls.Add(Theme.Truong("KHOÁ CÔNG KHAI", _txtKhoa, 260, CaoO, Le));
        }

        hang.Controls.Add(Theme.Truong("EMAIL CHỦ", _txtEmail, 260, CaoO, Le));
        hang.Controls.Add(Theme.Truong("MẬT KHẨU", _txtMatKhau, 200, CaoO, Le));
        hang.Controls.Add(Theme.Truong(" ", _btnDangNhap, 240, 40, Le));

        nen.Controls.Add(hang);
        return nen;
    }

    /// <summary>Chọn bản theo ngày, và chọn xem bảng nào.</summary>
    private Control TaoThanhChonBang()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 10, 20, 6) };

        var lbl = Theme.Nhan("BẢN NGÀY:", Theme.FontNhan, Theme.Xam);
        lbl.Margin = new Padding(0, 14, 10, 0);

        _cboBan.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboBan.Font = Theme.FontNhap;
        _cboBan.Width = 300;
        _cboBan.Margin = new Padding(0, 8, 10, 0);
        _cboBan.SelectedIndexChanged += async (_, _) =>
        {
            if (!_dangNap)
            {
                await TaiBanDangChon();
            }
        };

        _btnTaiLai.Margin = new Padding(0, 8, 24, 0);
        _btnTaiLai.Click += async (_, _) => await TaiDanhSachBan();

        var trai = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false };
        trai.Controls.Add(lbl);
        trai.Controls.Add(_cboBan);
        trai.Controls.Add(_btnTaiLai);

        _phanTrang.Dock = DockStyle.Right;
        _phanTrang.Padding = new Padding(0, 8, 0, 0);
        _phanTrang.DoiTrang += (_, _) => _veLaiTrang();

        // Năm nút chuyển bảng, nút đang xem tô đặc — cùng cách sổ công nợ làm với hai nút lọc.
        foreach (var (bang, chu, rong) in new[]
                 {
                     (Bang.KyDangMo, "Kỳ đang mở", 175),
                     (Bang.BuoiCong, "Buổi công", 160),
                     (Bang.UngTien, "Ứng tiền", 150),
                     (Bang.GhiChu, "Ghi chú ngày", 180),
                     (Bang.KyDaChot, "Kỳ đã chốt", 170),
                 })
        {
            var nut = Theme.NutPhu(chu, rong, 40);
            nut.Margin = new Padding(0, 8, 10, 0);
            nut.Click += (_, _) => DoiBang(bang);
            trai.Controls.Add(nut);
            _nutBang.Add((nut, bang));
        }

        nen.Controls.Add(trai);
        nen.Controls.Add(_phanTrang);
        SonNutBang();
        return nen;
    }

    private DataGridView TaoLuoi()
    {
        Theme.ApDungLuoi(_luoi);
        _luoi.ReadOnly = true;
        _luoi.Dock = DockStyle.Fill;
        return _luoi;
    }

    private Control TaoThanhTong()
    {
        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Nen, Padding = new Padding(20, 6, 20, 6) };

        _lblTong.Dock = DockStyle.Fill;
        _lblTong.Font = Theme.FontSo;
        _lblTong.ForeColor = Theme.ChuDam;
        _lblTong.TextAlign = ContentAlignment.MiddleRight;

        nen.Controls.Add(_lblTong);
        return nen;
    }

    private Control TaoThanhTrangThai()
    {
        _lblTrangThai.Dock = DockStyle.Fill;
        _lblTrangThai.Font = Theme.FontPhu;
        _lblTrangThai.ForeColor = Theme.Xam;
        _lblTrangThai.TextAlign = ContentAlignment.MiddleLeft;
        _lblTrangThai.Padding = new Padding(22, 0, 0, 0);

        var nen = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(232, 236, 242) };
        nen.Controls.Add(_lblTrangThai);
        return nen;
    }

    // ---------------- Nối và tải ----------------

    private async Task DangNhapVaTai()
    {
        var diaChi = _txtDiaChi.Text.Trim();
        var khoa = _txtKhoa.Text.Trim();

        if (diaChi.Length == 0 || khoa.Length == 0)
        {
            HopThoai.CanhBao(this, "Bản dựng này chưa có khoá Supabase. Hãy dán địa chỉ project và khoá công khai vào.");
            return;
        }

        if (_txtEmail.Text.Trim().Length == 0 || _txtMatKhau.Text.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy điền email và mật khẩu của tài khoản chủ.");
            return;
        }

        // Email nhớ lại cho khỏi gõ mỗi lần; mật khẩu thì không. Địa chỉ với khoá chỉ lưu khi
        // người dùng tự dán vào — khoá đã có trong bản dựng thì chép lại vào file cài đặt chỉ
        // tổ thêm một bản nữa phải đi sửa mỗi lần đổi project.
        _kho.CaiDat.ChamCongEmail = _txtEmail.Text.Trim();
        if (!_cauHinh.DaCoSan)
        {
            _kho.CaiDat.ChamCongDiaChi = diaChi;
            _kho.CaiDat.ChamCongKhoaCongKhai = khoa;
        }

        _kho.LuuCaiDat();

        _nguon?.Dispose();
        _nguon = new NguonSupabase(diaChi, khoa);

        await Chay("Đang đăng nhập…", async () =>
        {
            await _nguon.DangNhap(_txtEmail.Text, _txtMatKhau.Text);
            _txtMatKhau.Clear();
            await TaiDanhSachBanBenTrong();
        });
    }

    private async Task TaiDanhSachBan()
    {
        if (_nguon is null || !_nguon.DaDangNhap)
        {
            HopThoai.CanhBao(this, "Hãy đăng nhập trước.");
            return;
        }

        await Chay("Đang lấy danh sách bản…", TaiDanhSachBanBenTrong);
    }

    private async Task TaiDanhSachBanBenTrong()
    {
        var ds = await _nguon!.DanhSachBan();

        _dangNap = true;
        _cboBan.Items.Clear();
        foreach (var ban in ds)
        {
            _cboBan.Items.Add(new DongBan(ban));
        }

        _dangNap = false;

        if (ds.Count == 0)
        {
            DatTrangThaiChoTai(false);
            _so = new SoChamCong();
            HienBang();
            _lblTrangThai.Text =
                "Tài khoản này chưa có bản sao lưu nào. Trên điện thoại vào Sao lưu → đẩy sổ lên tài khoản.";
            return;
        }

        DatTrangThaiChoTai(true);

        // Bản mới nhất đứng đầu, mở luôn nó — đây là cái người ta muốn xem chín trên mười lần.
        _cboBan.SelectedIndex = 0;
    }

    private async Task TaiBanDangChon()
    {
        if (_nguon is null || _cboBan.SelectedItem is not DongBan chon)
        {
            return;
        }

        await Chay($"Đang tải bản ngày {NgayViet(chon.Ban.Ngay)}…", async () =>
        {
            var goi = await _nguon.DocBan(chon.Ban.Ngay);
            _so = goi.DuLieu;
            _taoLuc = goi.TaoLuc;
            HienBang();

            var dem = ChamCong.SoDiDong.Goi.Dem(_so);
            _lblTrangThai.Text =
                $"Bản ngày {NgayViet(chon.Ban.Ngay)} · {dem.SoTho} thợ · {dem.SoBuoiCong} buổi công · "
                + $"{dem.SoUngTien} lần ứng tiền · {dem.SoKy} kỳ đã chốt"
                + (_taoLuc.Length > 0 ? $" · sổ chụp lúc {GioViet(_taoLuc)}" : string.Empty);
        });
    }

    /// <summary>
    /// Bọc mọi lượt gọi mạng: khoá nút lại, hiện câu đang làm gì, và **dịch lỗi thành câu đọc
    /// được** thay vì để nguyên câu tiếng Anh của Supabase.
    /// </summary>
    private async Task Chay(string dangLam, Func<Task> viec)
    {
        _btnDangNhap.Enabled = false;
        _btnTaiLai.Enabled = false;
        Cursor = Cursors.WaitCursor;
        _lblTrangThai.Text = dangLam;

        try
        {
            await viec();
        }
        catch (LoiSupabase loi)
        {
            _lblTrangThai.Text = loi.Message;
            HopThoai.CanhBao(this, loi.Message);
        }
        catch (GoiHong loi)
        {
            _lblTrangThai.Text = loi.Message;
            HopThoai.CanhBao(this, loi.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
            _btnDangNhap.Enabled = true;
            _btnTaiLai.Enabled = _nguon?.DaDangNhap == true;
        }
    }

    private void DatTrangThaiChoTai(bool coBan)
    {
        _cboBan.Enabled = coBan;
        _btnTaiLai.Enabled = _nguon?.DaDangNhap == true;
        foreach (var (nut, _) in _nutBang)
        {
            nut.Enabled = coBan;
        }
    }

    // ---------------- Bốn cách xem ----------------

    private void DoiBang(Bang bang)
    {
        _dangXem = bang;
        SonNutBang();
        HienBang();
    }

    /// <summary>Nút của bảng đang xem tô đặc màu chính, các nút còn lại để trắng.</summary>
    private void SonNutBang()
    {
        foreach (var (nut, bang) in _nutBang)
        {
            var dangXem = bang == _dangXem;
            nut.BackColor = dangXem ? Theme.Chinh : Theme.Trang;
            nut.ForeColor = dangXem ? Color.White : Theme.Chu;
        }
    }

    private void HienBang()
    {
        // Đổi bảng thì về trang đầu — số trang của bảng cũ không nói gì về bảng mới.
        _phanTrang.VeTrangDau();
        _luoi.DataSource = null;
        _luoi.Columns.Clear();

        switch (_dangXem)
        {
            case Bang.KyDangMo:
                HienKyDangMo();
                break;
            case Bang.BuoiCong:
                HienBuoiCong();
                break;
            case Bang.UngTien:
                HienUngTien();
                break;
            case Bang.GhiChu:
                HienGhiChu();
                break;
            case Bang.KyDaChot:
                HienKyDaChot();
                break;
        }
    }

    /// <summary>
    /// Gắn cả danh sách vào lưới qua thanh phân trang: lưới chỉ nhận 30 dòng một lúc, còn câu
    /// tổng ở chân màn hình vẫn cộng trên **cả** danh sách.
    /// </summary>
    private void Gan<T>(List<T> tatCa)
    {
        void Ve() => _luoi.DataSource = new BindingList<T>(_phanTrang.Cat(tatCa));

        _veLaiTrang = Ve;
        _phanTrang.DatTong(tatCa.Count);
        Ve();
    }

    private void HienKyDangMo()
    {
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongKyMo.TenTho), "THỢ", 200),
            Theme.Cot(nameof(DongKyMo.CongSang), "CÔNG SÁNG", 100, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongKyMo.CongChieu), "CÔNG CHIỀU", 100, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongKyMo.TongCong), "TỔNG CÔNG", 100, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongKyMo.TienCong), "TIỀN CÔNG", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKyMo.DaUng), "ĐÃ ỨNG", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKyMo.NoKyTruoc), "NỢ KỲ TRƯỚC", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKyMo.ConLai), "CÒN PHẢI TRẢ", 140, "#,##0", canPhai: true));

        var ky = BangLuongSo.KyHienTai(_so);
        Gan(ky.Dongs
            .Select(d => new DongKyMo
            {
                TenTho = d.Tho.Ten,
                CongSang = d.CongSang,
                CongChieu = d.CongChieu,
                TongCong = d.TongCong,
                TienCong = d.TienCong,
                DaUng = d.DaUng,
                NoKyTruoc = d.NoKyTruoc,
                ConLai = d.ConLai,
            })
            .ToList());

        var khoang = ky.TuNgay.Length > 0
            ? $"{NgayViet(ky.TuNgay)} – {NgayViet(ky.DenNgay)}   ·   "
            : string.Empty;
        _lblTong.Text =
            $"{khoang}{So.Luong(ky.TongCong)} công   ·   tiền công {So.Tien(ky.TongTienCong)}   ·   "
            + $"đã ứng {So.Tien(ky.TongDaUng)}   ·   CÒN PHẢI TRẢ {So.Tien(ky.TongPhaiTra)}";
    }

    private void HienBuoiCong()
    {
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongBuoi.Ngay), "NGÀY", 110),
            Theme.Cot(nameof(DongBuoi.TenTho), "THỢ", 200),
            Theme.Cot(nameof(DongBuoi.Buoi), "BUỔI", 90),
            Theme.Cot(nameof(DongBuoi.SoCong), "SỐ CÔNG", 90, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongBuoi.TienMotCong), "TIỀN MỘT CÔNG", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongBuoi.ThanhTien), "THÀNH TIỀN", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongBuoi.DaTra), "ĐÃ TRẢ", 100),
            Theme.Cot(nameof(DongBuoi.GhiChu), "GHI CHÚ", 200));

        var tenTho = _so.Thos.ToDictionary(t => t.Id, t => t.Ten, StringComparer.Ordinal);
        var daChot = _so.KyLuongs.SelectMany(k => k.BuoiCongIds).ToHashSet(StringComparer.Ordinal);

        var dongs = _so.BuoiCongs
            .OrderByDescending(b => b.Ngay, StringComparer.Ordinal)
            .ThenBy(b => tenTho.GetValueOrDefault(b.ThoId, string.Empty), StringComparer.CurrentCultureIgnoreCase)
            .Select(b =>
            {
                var tho = _so.Thos.FirstOrDefault(t => t.Id == b.ThoId);
                var gia = b.TienMotCong ?? tho?.TienMotCongNgay(b.Ngay) ?? 0m;
                return new DongBuoi
                {
                    Ngay = NgayViet(b.Ngay),
                    TenTho = tho?.Ten ?? "(thợ đã bị xoá)",
                    Buoi = b.BuoiTiengViet,
                    SoCong = b.SoCong,
                    TienMotCong = gia,
                    ThanhTien = Math.Round(b.SoCong * gia, 0, MidpointRounding.AwayFromZero),
                    DaTra = daChot.Contains(b.Id) ? "đã trả" : string.Empty,
                    GhiChu = b.GhiChu,
                };
            })
            .ToList();

        Gan(dongs);
        _lblTong.Text =
            $"{dongs.Count} buổi công   ·   {So.Luong(dongs.Sum(d => d.SoCong))} công   ·   "
            + $"thành tiền {So.Tien(dongs.Sum(d => d.ThanhTien))}";
    }

    private void HienUngTien()
    {
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongUng.Ngay), "NGÀY", 110),
            Theme.Cot(nameof(DongUng.TenTho), "THỢ", 220),
            Theme.Cot(nameof(DongUng.SoTien), "SỐ TIỀN", 140, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongUng.DaTra), "ĐÃ TRỪ VÀO KỲ", 130),
            Theme.Cot(nameof(DongUng.GhiChu), "GHI CHÚ", 300));

        var daChot = _so.KyLuongs.SelectMany(k => k.UngTienIds).ToHashSet(StringComparer.Ordinal);
        var dongs = _so.UngTiens
            .OrderByDescending(u => u.Ngay, StringComparer.Ordinal)
            .Select(u => new DongUng
            {
                Ngay = NgayViet(u.Ngay),
                TenTho = _so.Thos.FirstOrDefault(t => t.Id == u.ThoId)?.Ten ?? "(thợ đã bị xoá)",
                SoTien = u.SoTien,
                DaTra = daChot.Contains(u.Id) ? "đã trừ" : string.Empty,
                GhiChu = u.GhiChu,
            })
            .ToList();

        Gan(dongs);
        _lblTong.Text = $"{dongs.Count} lần ứng   ·   tổng {So.Tien(dongs.Sum(d => d.SoTien))}";
    }

    /// <summary>
    /// Ghi chú chủ gõ trên điện thoại lúc chấm công — "nghỉ đám cưới", "về sớm đi khám".
    ///
    /// <para>
    /// Để riêng một bảng chứ không ghép vào bảng Buổi công: ghi chú nói về **cả ngày**, mà ngày
    /// đáng ghi chú nhất lại thường là ngày thợ nghỉ hẳn — ngày ấy không có dòng buổi công nào
    /// để mà ghép vào.
    /// </para>
    /// </summary>
    private void HienGhiChu()
    {
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongGhiChu.Ngay), "NGÀY", 130),
            Theme.Cot(nameof(DongGhiChu.TenTho), "THỢ", 220),
            Theme.Cot(nameof(DongGhiChu.NoiDung), "GHI CHÚ", 560));

        var dongs = _so.GhiChuNgays
            .OrderByDescending(g => g.Ngay, StringComparer.Ordinal)
            .ThenBy(
                g => _so.Thos.FirstOrDefault(t => t.Id == g.ThoId)?.Ten ?? string.Empty,
                StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new DongGhiChu
            {
                Ngay = NgayViet(g.Ngay),
                TenTho = _so.Thos.FirstOrDefault(t => t.Id == g.ThoId)?.Ten ?? "(thợ đã bị xoá)",
                NoiDung = g.NoiDung,
            })
            .ToList();

        Gan(dongs);
        _lblTong.Text = dongs.Count == 0
            ? "Chưa có ghi chú nào. Ghi chú gõ trên điện thoại, ở màn hình chấm công."
            : $"{dongs.Count} ghi chú";
    }

    private void HienKyDaChot()
    {
        _luoi.Columns.AddRange(
            Theme.Cot(nameof(DongKy.Khoang), "KỲ", 200),
            Theme.Cot(nameof(DongKy.ChotLuc), "CHỐT LÚC", 150),
            Theme.Cot(nameof(DongKy.SoTho), "SỐ THỢ", 90, canPhai: true),
            Theme.Cot(nameof(DongKy.TongCong), "TỔNG CÔNG", 100, "#,##0.##", canPhai: true),
            Theme.Cot(nameof(DongKy.TienCong), "TIỀN CÔNG", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKy.DaUng), "ĐÃ ỨNG", 120, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKy.DaTra), "ĐÃ TRẢ", 130, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKy.ChuyenKySau), "CHUYỂN KỲ SAU", 140, "#,##0", canPhai: true),
            Theme.Cot(nameof(DongKy.GhiChu), "GHI CHÚ", 200));

        // Kỳ mới nhất lên đầu — đúng thứ tự người ta muốn đọc.
        var dongs = Enumerable.Reverse(_so.KyLuongs)
            .Select(k => new DongKy
            {
                Khoang = $"{NgayViet(k.TuNgay)} – {NgayViet(k.DenNgay)}",
                ChotLuc = GioViet(k.ChotLuc),
                SoTho = k.Dongs.Count,
                TongCong = k.Dongs.Sum(d => d.TongCong),
                TienCong = k.Dongs.Sum(d => d.TienCong),
                DaUng = k.Dongs.Sum(d => d.DaUng),
                DaTra = k.Dongs.Sum(d => d.DaTra),
                ChuyenKySau = k.Dongs.Sum(d => d.ChuyenKySau),
                GhiChu = k.GhiChu,
            })
            .ToList();

        Gan(dongs);
        _lblTong.Text = dongs.Count == 0
            ? "Chưa chốt kỳ nào."
            : $"{dongs.Count} kỳ đã chốt   ·   đã trả tất cả {So.Tien(dongs.Sum(d => d.DaTra))}";
    }

    // ---------------- Đổi chữ cho dễ đọc ----------------

    /// <summary>"2026-08-20" -> "20/08/2026". Chuỗi lạ thì trả nguyên, đừng đoán.</summary>
    private static string NgayViet(string ngay) =>
        DateTime.TryParseExact(ngay, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d)
            ? d.ToString("dd/MM/yyyy")
            : ngay;

    /// <summary>Mốc ISO -> "20/08/2026 10:05" theo giờ máy đang chạy.</summary>
    private static string GioViet(string iso) =>
        DateTime.TryParse(
            iso,
            null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var d)
            ? d.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : iso;

    // ---------------- Dòng hiện trên lưới ----------------

    private sealed class DongBan
    {
        public DongBan(BanTaiKhoan ban) => Ban = ban;

        public BanTaiKhoan Ban { get; }

        public override string ToString() =>
            Ban.SuaLuc.Length > 0
                ? $"{NgayViet(Ban.Ngay)}   ·   ghi lúc {GioViet(Ban.SuaLuc)}"
                : NgayViet(Ban.Ngay);
    }

    private sealed class DongKyMo
    {
        public string TenTho { get; init; } = string.Empty;

        public decimal CongSang { get; init; }

        public decimal CongChieu { get; init; }

        public decimal TongCong { get; init; }

        public decimal TienCong { get; init; }

        public decimal DaUng { get; init; }

        public decimal NoKyTruoc { get; init; }

        public decimal ConLai { get; init; }
    }

    private sealed class DongBuoi
    {
        public string Ngay { get; init; } = string.Empty;

        public string TenTho { get; init; } = string.Empty;

        public string Buoi { get; init; } = string.Empty;

        public decimal SoCong { get; init; }

        public decimal TienMotCong { get; init; }

        public decimal ThanhTien { get; init; }

        public string DaTra { get; init; } = string.Empty;

        public string GhiChu { get; init; } = string.Empty;
    }

    private sealed class DongUng
    {
        public string Ngay { get; init; } = string.Empty;

        public string TenTho { get; init; } = string.Empty;

        public decimal SoTien { get; init; }

        public string DaTra { get; init; } = string.Empty;

        public string GhiChu { get; init; } = string.Empty;
    }

    private sealed class DongGhiChu
    {
        public string Ngay { get; init; } = string.Empty;

        public string TenTho { get; init; } = string.Empty;

        public string NoiDung { get; init; } = string.Empty;
    }

    private sealed class DongKy
    {
        public string Khoang { get; init; } = string.Empty;

        public string ChotLuc { get; init; } = string.Empty;

        public int SoTho { get; init; }

        public decimal TongCong { get; init; }

        public decimal TienCong { get; init; }

        public decimal DaUng { get; init; }

        public decimal DaTra { get; init; }

        public decimal ChuyenKySau { get; init; }

        public string GhiChu { get; init; } = string.Empty;
    }
}
