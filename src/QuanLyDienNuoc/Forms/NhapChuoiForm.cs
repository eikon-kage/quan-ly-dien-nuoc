using QuanLyDienNuoc.Ui;

namespace QuanLyDienNuoc.Forms;

/// <summary>Hộp thoại nhỏ hỏi một dòng chữ (đặt tên bộ hàng, đặt mã tắt...).</summary>
public sealed class NhapChuoiForm : Form
{
    private readonly TextBox _o = Theme.O(420);

    public NhapChuoiForm(string tieuDe, string nhan, string macDinh = "")
    {
        Text = tieuDe;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 210);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Nen;
        Font = Theme.FontThuong;
        AutoScaleMode = AutoScaleMode.Dpi;

        _o.Text = macDinh;
        _o.SelectAll();

        var truong = Theme.Truong(nhan, _o, 460);
        truong.Location = new Point(30, 26);

        var btnOk = Theme.Nut("Đồng ý", Theme.Chinh, 160, 48);
        btnOk.Location = new Point(30, 120);
        btnOk.Click += (_, _) => Xong();

        var btnHuy = Theme.NutPhu("Huỷ", 140, 48);
        btnHuy.Location = new Point(206, 120);
        btnHuy.Click += (_, _) => Close();

        Controls.Add(truong);
        Controls.Add(btnOk);
        Controls.Add(btnHuy);
        AcceptButton = btnOk;
    }

    /// <summary>Chuỗi người dùng đã nhập, chỉ có giá trị khi bấm Đồng ý.</summary>
    public string KetQua { get; private set; } = string.Empty;

    /// <summary>Hỏi một dòng chữ; trả về null nếu người dùng bỏ qua hoặc để trống.</summary>
    public static string? Hoi(IWin32Window? chu, string tieuDe, string nhan, string macDinh = "")
    {
        using var form = new NhapChuoiForm(tieuDe, nhan, macDinh);
        return form.ShowDialog(chu) == DialogResult.OK ? form.KetQua : null;
    }

    private void Xong()
    {
        var chu = _o.Text.Trim();
        if (chu.Length == 0)
        {
            HopThoai.CanhBao(this, "Hãy nhập nội dung.");
            _o.Focus();
            return;
        }

        KetQua = chu;
        DialogResult = DialogResult.OK;
        Close();
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
