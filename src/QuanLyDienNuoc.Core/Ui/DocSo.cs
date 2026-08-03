using System.Text;

namespace QuanLyDienNuoc.Ui;

/// <summary>Đọc số tiền thành chữ để điền dòng "Thành tiền (bằng chữ)" trên hoá đơn.</summary>
public static class DocSo
{
    private static readonly string[] ChuSo =
    {
        "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín",
    };

    private static readonly string[] HauTo =
    {
        "", " nghìn", " triệu", " tỷ", " nghìn tỷ", " triệu tỷ",
    };

    /// <summary>1500000 -> "Một triệu năm trăm nghìn đồng".</summary>
    public static string DocTien(decimal soTien)
    {
        var so = (long)Math.Round(soTien, 0, MidpointRounding.AwayFromZero);
        if (so == 0)
        {
            return "Không đồng";
        }

        var am = so < 0;
        so = Math.Abs(so);

        // Tách thành các nhóm 3 chữ số: nhóm[0] là hàng đơn vị, nhóm[1] là nghìn...
        var nhom = new List<int>();
        while (so > 0)
        {
            nhom.Add((int)(so % 1000));
            so /= 1000;
        }

        if (nhom.Count > HauTo.Length)
        {
            return soTien.ToString("#,##0");
        }

        var phan = new List<string>();
        for (var i = nhom.Count - 1; i >= 0; i--)
        {
            if (nhom[i] == 0)
            {
                continue;
            }

            // Nhóm cao nhất đọc gọn ("hai triệu"), các nhóm sau đọc đủ ("không trăm linh năm").
            var dayDu = i < nhom.Count - 1;
            phan.Add(DocNhomBaChuSo(nhom[i], dayDu) + HauTo[i]);
        }

        var chu = string.Join(" ", phan);
        if (am)
        {
            chu = "âm " + chu;
        }

        return char.ToUpper(chu[0]) + chu[1..] + " đồng";
    }

    private static string DocNhomBaChuSo(int so, bool dayDu)
    {
        var tram = so / 100;
        var chuc = so / 10 % 10;
        var donVi = so % 10;
        var sb = new StringBuilder();

        if (tram > 0 || dayDu)
        {
            sb.Append(ChuSo[tram]).Append(" trăm");
            if (chuc == 0 && donVi > 0)
            {
                sb.Append(" linh");
            }
        }

        if (chuc == 1)
        {
            sb.Append(" mười");
        }
        else if (chuc > 1)
        {
            sb.Append(' ').Append(ChuSo[chuc]).Append(" mươi");
        }

        if (donVi > 0)
        {
            // "hai mươi mốt", "mười lăm", "hai mươi tư"
            if (chuc >= 2 && donVi == 1)
            {
                sb.Append(" mốt");
            }
            else if (chuc >= 1 && donVi == 5)
            {
                sb.Append(" lăm");
            }
            else if (chuc >= 2 && donVi == 4)
            {
                sb.Append(" tư");
            }
            else
            {
                sb.Append(' ').Append(ChuSo[donVi]);
            }
        }

        return sb.ToString().Trim();
    }
}
