using QuanLyDienNuoc.Data;
using QuanLyDienNuoc.Models;
using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>
/// Kiểm tra danh mục vật tư dựng sẵn: bản thân danh mục phải sạch (không trùng tên, không
/// trùng mã tắt, có đủ đơn vị và giá) và điền vào sổ đang dùng thì không được đè lên hàng cũ.
/// </summary>
public sealed class DanhMucMauTests
{
    [Fact]
    public void DanhMuc_KhongTrungTenHang()
    {
        var du = new DuLieuApp();
        DanhMucMau.BoSung(du);

        var trung = du.VatTus
            .GroupBy(v => ChuViet.BoDau(v.Ten))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(trung);
        Assert.Equal(DanhMucMau.SoMatHang, du.VatTus.Count);
    }

    [Fact]
    public void DanhMuc_KhongTrungMaTat()
    {
        var du = new DuLieuApp();
        DanhMucMau.BoSung(du);

        // Mã tắt trùng nhau thì gõ tắt lúc nhập hàng ra hai thứ khác nhau — phải không có cái nào.
        var trung = du.VatTus
            .Where(v => v.MaTat.Length > 0)
            .GroupBy(v => ChuViet.BoDau(v.MaTat))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(trung);
        Assert.All(du.VatTus, v => Assert.NotEqual(string.Empty, v.MaTat));
    }

    [Fact]
    public void DanhMuc_MoiHangCoNhomDonViVaGia()
    {
        var du = new DuLieuApp();
        DanhMucMau.BoSung(du);

        Assert.All(du.VatTus, v =>
        {
            Assert.NotEqual(string.Empty, v.Ten.Trim());
            Assert.NotEqual(string.Empty, v.DonVi.Trim());
            Assert.True(v.DonGiaMacDinh > 0, $"\"{v.Ten}\" chưa có giá tham khảo.");
            Assert.NotNull(v.NhomId);
            Assert.Contains(du.NhomHangs, n => n.Id == v.NhomId);
        });

        Assert.Equal(DanhMucMau.TenCacNhom.Count, du.NhomHangs.Count);
        Assert.All(DanhMucMau.TenCacNhom, ten => Assert.Contains(du.NhomHangs, n => n.Ten == ten));
    }

    [Fact]
    public void DanhMuc_MoiNhomCoItNhatMotHang()
    {
        var du = new DuLieuApp();
        DanhMucMau.BoSung(du);

        Assert.All(du.NhomHangs, n => Assert.Contains(du.VatTus, v => v.NhomId == n.Id));
    }

    [Fact]
    public void BoSung_GiuNguyenGiaVaNhomCuaHangDaCo()
    {
        var du = new DuLieuApp();
        var nhomRieng = new NhomHang { Ten = "Hàng nhà tự để" };
        du.NhomHangs.Add(nhomRieng);
        du.VatTus.Add(new VatTu
        {
            Ten = "ống nhựa pvc d27",
            DonVi = "Ống",
            MaTat = "ong27",
            DonGiaMacDinh = 99_000,
            NhomId = nhomRieng.Id,
        });

        var ketQua = DanhMucMau.BoSung(du);

        var cu = Assert.Single(du.VatTus, v => ChuViet.BoDau(v.Ten) == "ong nhua pvc d27");
        Assert.Equal(99_000, cu.DonGiaMacDinh);
        Assert.Equal("Ống", cu.DonVi);
        Assert.Equal(nhomRieng.Id, cu.NhomId);

        Assert.Equal(1, ketQua.SoHangDaCo);
        Assert.Equal(DanhMucMau.SoMatHang - 1, ketQua.SoHangThem);
    }

    [Fact]
    public void BoSung_DungLaiNhomCungTenChuKhongTaoNhomGanGiong()
    {
        var du = new DuLieuApp();
        var nhomCu = new NhomHang { Ten = "ống nước" };
        du.NhomHangs.Add(nhomCu);

        DanhMucMau.BoSung(du);

        Assert.Single(du.NhomHangs, n => ChuViet.BoDau(n.Ten) == "ong nuoc");
        Assert.Contains(du.VatTus, v => v.Ten == "Ống nhựa PVC D21" && v.NhomId == nhomCu.Id);
    }

    [Fact]
    public void BoSung_KhongLayMaTatDaCoCuaCuaHang()
    {
        var du = new DuLieuApp();
        du.VatTus.Add(new VatTu { Ten = "Ống kẽm D21 của nhà", MaTat = "o21" });

        DanhMucMau.BoSung(du);

        var moi = Assert.Single(du.VatTus, v => v.Ten == "Ống nhựa PVC D21");
        Assert.Equal(string.Empty, moi.MaTat);
        Assert.Single(du.VatTus, v => ChuViet.BoDau(v.MaTat) == "o21");
    }

    [Fact]
    public void BoSung_LamHaiLanKhongSinhHangTrung()
    {
        var du = new DuLieuApp();
        DanhMucMau.BoSung(du);
        var lanHai = DanhMucMau.BoSung(du);

        Assert.Equal(0, lanHai.SoHangThem);
        Assert.Equal(0, lanHai.SoNhomThem);
        Assert.False(lanHai.CoThemGi);
        Assert.Equal(DanhMucMau.SoMatHang, du.VatTus.Count);
    }

    [Fact]
    public void DanhMuc_GoTatRaDungMatHang()
    {
        var du = new DuLieuApp();
        DanhMucMau.BoSung(du);

        // Gõ mã tắt lúc nhập hàng phải cho đúng một mặt hàng đứng đầu, không lẫn sang hàng khác.
        foreach (var vatTu in du.VatTus.Where(v => v.MaTat.Length > 0))
        {
            var diemCaoNhat = du.VatTus.Max(v => TimHang.Diem(v.Ten, v.MaTat, vatTu.MaTat));
            Assert.Equal(TimHang.Diem(vatTu.Ten, vatTu.MaTat, vatTu.MaTat), diemCaoNhat);
        }
    }
}
