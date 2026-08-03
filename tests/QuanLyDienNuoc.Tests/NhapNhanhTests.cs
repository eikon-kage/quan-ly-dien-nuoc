using QuanLyDienNuoc.Ui;
using Xunit;

namespace QuanLyDienNuoc.Tests;

/// <summary>Kiểm tra các tiện ích nhập nhanh: tính ngay trong ô, gõ tắt tên hàng, gõ một dòng nhiều món.</summary>
public class NhapNhanhTests
{
    // ---------- Máy tính ngay trong ô ----------

    [Theory]
    [InlineData("3+2*4", 11)]
    [InlineData("=5+5", 10)]
    [InlineData("2x3", 6)]                  // thợ hay viết dấu nhân là chữ x
    [InlineData("2 x 3", 6)]
    [InlineData("(1+2)*3", 9)]
    [InlineData("10-4", 6)]
    [InlineData("1,2+0,8", 2)]              // phẩy là thập phân
    [InlineData("1.500+500", 2000)]         // chấm vẫn là phân cách nghìn
    [InlineData("15.000*2", 30000)]
    [InlineData("-3+5", 2)]
    [InlineData("10/4", 2.5)]
    [InlineData("2,5+1,5+3", 7)]
    public void TryTinh_TinhDungPhepTinhGoTrongO(string nhap, decimal mongDoi)
    {
        Assert.True(So.TryTinh(nhap, out var giaTri));
        Assert.Equal(mongDoi, giaTri);
    }

    [Theory]
    [InlineData("15.000", 15000)]           // số thuần vẫn đọc theo kiểu Việt Nam
    [InlineData("2,5", 2.5)]
    [InlineData("1.500.000", 1500000)]
    public void TryTinh_SoThuanVanDocNhuCu(string nhap, decimal mongDoi)
    {
        Assert.True(So.TryTinh(nhap, out var giaTri));
        Assert.Equal(mongDoi, giaTri);
    }

    [Theory]
    [InlineData("3*")]
    [InlineData("(1+2")]
    [InlineData("ống 27")]
    [InlineData("5/0")]
    [InlineData("")]
    [InlineData(null)]
    public void TryTinh_TraVeFalseKhiKhongTinhDuoc(string? nhap)
    {
        Assert.False(So.TryTinh(nhap, out var giaTri));
        Assert.Equal(0m, giaTri);
    }

    // ---------- Gõ tắt tên hàng ----------

    [Theory]
    [InlineData("ong 27")]      // không dấu
    [InlineData("Ống 27")]
    [InlineData("27")]          // chỉ nhớ cỡ ống
    [InlineData("27 ong")]      // gõ ngược thứ tự
    [InlineData("ongnhua")]     // dính liền
    [InlineData("")]
    public void TimHang_KhopDuocCacKieuGoTat(string tuKhoa)
    {
        Assert.True(TimHang.Khop("Ống nhựa PVC D27", maTat: "", tuKhoa));
    }

    [Fact]
    public void TimHang_MaTatKhopHanDuocDiemCaoNhat()
    {
        var diemMaTat = TimHang.Diem("Ống nhựa PVC D27", "o27", "o27");
        var diemTrongTen = TimHang.Diem("Ống nhựa PVC D27", maTat: "", "ong");

        Assert.Equal(100, diemMaTat);
        Assert.True(diemMaTat > diemTrongTen);
    }

    [Fact]
    public void TimHang_KhongKhopThiTraVeKhong()
    {
        Assert.Equal(0, TimHang.Diem("Ống nhựa PVC D27", "o27", "aptomat"));
        Assert.False(TimHang.Khop("Ống nhựa PVC D27", "o27", "day dien"));
    }

    [Fact]
    public void TimHang_TenBatDauBangTuKhoaDuocXepTrenTenChiChuaTuKhoa()
    {
        var batDau = TimHang.Diem("Keo dán ống 100g", maTat: "", "keo");
        var chuaGiua = TimHang.Diem("Chất tẩy keo", maTat: "", "keo");

        Assert.True(batDau > chuaGiua);
    }

    // ---------- Gõ một dòng nhiều món ----------

    [Fact]
    public void Tach_TachDungNhieuMonTrenMotDong()
    {
        var muc = DongNhapNhanh.Tach("ống 27 x10, co 90 x5, keo dán ống x1");

        Assert.Equal(3, muc.Count);
        Assert.Equal("ống 27", muc[0].Ten);
        Assert.Equal(10m, muc[0].SoLuong);
        Assert.Equal("co 90", muc[1].Ten);
        Assert.Equal(5m, muc[1].SoLuong);
        Assert.Equal("keo dán ống", muc[2].Ten);
        Assert.Equal(1m, muc[2].SoLuong);
    }

    [Fact]
    public void Tach_TenCoSanSoKhongBiHieuNhamLaSoLuong()
    {
        var muc = DongNhapNhanh.Tach("ống 27");

        Assert.Single(muc);
        Assert.Equal("ống 27", muc[0].Ten);
        Assert.Equal(1m, muc[0].SoLuong);
    }

    [Fact]
    public void Tach_DocDuocGiaGhiSauDauA()
    {
        var muc = DongNhapNhanh.Tach("ống 27 x10 @45.000");

        Assert.Single(muc);
        Assert.Equal("ống 27", muc[0].Ten);
        Assert.Equal(10m, muc[0].SoLuong);
        Assert.Equal(45000m, muc[0].DonGia);
    }

    [Theory]
    [InlineData("ống 27 x10; co 90 x5")]
    [InlineData("ống 27 x10\nco 90 x5")]
    public void Tach_ChapNhanDauChamPhayVaXuongDong(string dong)
    {
        var muc = DongNhapNhanh.Tach(dong);

        Assert.Equal(2, muc.Count);
        Assert.Equal(10m, muc[0].SoLuong);
        Assert.Equal(5m, muc[1].SoLuong);
    }

    [Fact]
    public void Tach_SoLuongThapPhanVaDauSaoDeuHieu()
    {
        var muc = DongNhapNhanh.Tach("ống 21 * 5,7");

        Assert.Single(muc);
        Assert.Equal("ống 21", muc[0].Ten);
        Assert.Equal(5.7m, muc[0].SoLuong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Tach_DongRongThiKhongRaMonNao(string? dong)
    {
        Assert.Empty(DongNhapNhanh.Tach(dong));
    }
}
