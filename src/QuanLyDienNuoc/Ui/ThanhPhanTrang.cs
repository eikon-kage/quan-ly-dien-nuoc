namespace QuanLyDienNuoc.Ui;

/// <summary>
/// Thanh phân trang dùng chung cho các bảng dài: hai nút lùi / tiến và câu "Trang 2/7".
///
/// <para>
/// Chữ trên nút để thẳng "Trang trước" / "Trang sau" chứ không dùng mũi tên ◀ ▶: phông Segoe UI
/// thiếu nhiều ký tự ký hiệu, thiếu là Windows in ra ô vuông rỗng — mà đây là nút bấm hằng ngày,
/// không phải chỗ để mạo hiểm.
/// </para>
///
/// <para>
/// Chỉ giữ **số trang đang xem**, không giữ dữ liệu: bảng nào dùng thì tự cắt danh sách của mình
/// bằng <see cref="PhanTrang.Cat{T}"/>. Nhờ vậy một thanh dùng được cho mọi bảng, kể cả bảng đổi
/// nguồn dữ liệu liên tục như màn chấm công.
/// </para>
/// </summary>
public sealed class ThanhPhanTrang : Panel
{
    private readonly Button _nutTruoc = Theme.NutPhu("Trang trước", 150, 38);
    private readonly Button _nutSau = Theme.NutPhu("Trang sau", 140, 38);
    private readonly Label _nhan = new();

    private int _tongDong;

    public ThanhPhanTrang()
    {
        AutoSize = true;
        Margin = new Padding(0);

        _nhan.AutoSize = false;
        _nhan.Width = 300;
        _nhan.Height = 38;
        _nhan.Font = Theme.FontThuong;
        _nhan.ForeColor = Theme.Xam;
        _nhan.TextAlign = ContentAlignment.MiddleRight;
        _nhan.Margin = new Padding(0, 0, 12, 0);

        _nutTruoc.Margin = new Padding(0, 0, 8, 0);
        _nutSau.Margin = new Padding(0);
        _nutTruoc.Click += (_, _) => VeTrang(Trang - 1);
        _nutSau.Click += (_, _) => VeTrang(Trang + 1);

        var hang = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0),
        };
        hang.Controls.Add(_nhan);
        hang.Controls.Add(_nutTruoc);
        hang.Controls.Add(_nutSau);
        Controls.Add(hang);

        CapNhat();
    }

    /// <summary>Người dùng vừa đổi trang — bảng nghe tin này để cắt lại danh sách.</summary>
    public event EventHandler? DoiTrang;

    /// <summary>Trang đang xem, đếm từ 0.</summary>
    public int Trang { get; private set; }

    /// <summary>
    /// Cho biết cả bảng có bao nhiêu dòng. Trang đang xem được giữ nguyên nếu còn hợp lệ — lọc
    /// hay nạp lại mà cứ quăng về trang 1 thì đang dò dở giữa sổ là mất chỗ.
    /// </summary>
    public void DatTong(int tongDong)
    {
        _tongDong = Math.Max(0, tongDong);
        Trang = PhanTrang.TrangHopLe(Trang, _tongDong);
        CapNhat();
    }

    /// <summary>Nhảy tới một trang. Vượt quá hai đầu thì kẹp lại, không báo lỗi.</summary>
    public void VeTrang(int trang)
    {
        var moi = PhanTrang.TrangHopLe(trang, _tongDong);
        if (moi == Trang)
        {
            return;
        }

        Trang = moi;
        CapNhat();
        DoiTrang?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Về trang đầu **không phát tin đổi trang** — dùng khi bảng vừa đổi sang nguồn dữ liệu khác,
    /// lúc ấy phát tin ra là bảng cũ vẽ lại một lần vô ích.
    /// </summary>
    public void VeTrangDau()
    {
        Trang = 0;
        CapNhat();
    }

    /// <summary>Cắt ra đúng trang đang xem.</summary>
    public List<T> Cat<T>(IReadOnlyList<T> tatCa) => PhanTrang.Cat(tatCa, Trang);

    private void CapNhat()
    {
        _nhan.Text = PhanTrang.MoTa(Trang, _tongDong);

        var soTrang = PhanTrang.SoTrang(_tongDong);
        _nutTruoc.Enabled = Trang > 0;
        _nutSau.Enabled = Trang < soTrang - 1;

        // Vừa một trang thì hai nút không có việc gì làm — ẩn hẳn cho hàng nút đỡ rối.
        var nhieuTrang = soTrang > 1;
        _nutTruoc.Visible = nhieuTrang;
        _nutSau.Visible = nhieuTrang;
    }
}
