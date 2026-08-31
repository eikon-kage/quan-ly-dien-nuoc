using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>
/// Cửa sổ hiện một đoạn văn bản đã soạn sẵn (tin nhắc nợ...) để đọc lại, sửa vài chữ
/// rồi chép vào bộ nhớ máy mà dán sang Zalo / tin nhắn.
/// </summary>
public sealed class VanBanForm : Form
{
    private readonly TextBox _o = new();

    public VanBanForm(string tieuDe, string phuDe, string noiDung)
    {
        Text = tieuDe;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(620, 480);
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        var khung = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Nen,
        };
        // Dòng có chữ thì tự cao theo chữ: xem "Chữ bị cắt" trong docs/giao-dien-may-tinh.md.
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _o.Multiline = true;
        _o.ScrollBars = ScrollBars.Vertical;
        _o.Font = Theme.FontNhap;
        _o.BorderStyle = BorderStyle.FixedSingle;
        _o.Dock = DockStyle.Fill;
        _o.Text = noiDung;
        _o.Select(0, 0);

        var vien = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 10, 20, 6), BackColor = Theme.Nen };
        vien.Controls.Add(Theme.Khung(_o));

        var btnChep = Theme.Nut("CHÉP VÀO BỘ NHỚ", Theme.Chinh, 260, 48, noTheoChu: true);
        btnChep.Click += (_, _) => Chep();

        var btnDong = Theme.NutPhu("Đóng", 120, 48, noTheoChu: true);
        btnDong.Click += (_, _) => Close();

        khung.Controls.Add(Theme.ThanhTieuDe(tieuDe.ToUpperInvariant(), phuDe, tuCao: true), 0, 0);
        khung.Controls.Add(vien, 0, 1);
        khung.Controls.Add(Theme.ThanhDuoi(null, btnChep, btnDong), 0, 2);
        Controls.Add(khung);
    }

    private void Chep()
    {
        try
        {
            Clipboard.SetText(_o.Text);
            HopThoai.Bao(this, "Đã chép xong. Mở Zalo (hoặc tin nhắn) rồi bấm dán là ra.");
        }
        catch (Exception ex)
        {
            HopThoai.Loi(this, "Không chép được vào bộ nhớ máy:\n" + ex.Message);
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
}
