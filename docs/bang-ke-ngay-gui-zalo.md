# Bảng kê hàng trong ngày — gửi khách qua Zalo

Cuối buổi, khách đã lấy mấy món trong ngày thì gửi cho khách một tấm ảnh bảng kê để đối
chiếu. Khách xem ngay trong khung chat Zalo, không phải tải file về rồi tìm phần mềm mở.

## Làm thế nào

1. Mở **đơn hàng của khách** (bấm đúp vào tên khách ở màn hình chính).
2. Dưới bảng hàng, bấm **BẢNG KÊ TRONG NGÀY**.
3. Chọn ngày: **HÔM NAY**, **HÔM QUA**, hoặc gõ / bấm lịch ở ô *Ngày khác*.
4. Nhìn tấm ảnh xem trước cho đúng khách, đúng ngày, đủ mấy dòng hàng.
5. Bấm **CHÉP ẢNH ĐỂ DÁN VÀO ZALO** (hoặc `Ctrl+C`).
6. Mở Zalo, chọn khách, bấm `Ctrl+V` — ảnh vào khung chat, bấm gửi.

Máy không dán được (Zalo bản cũ, hoặc muốn gửi qua mail) thì bấm **Lưu ảnh ra file...**
rồi kéo file `.png` vào khung chat cũng vậy.

## Tấm ảnh có gì

- Tên cửa hàng, địa chỉ, điện thoại — đọc từ chính file mẫu hoá đơn `MauHoaDon\trang-1.xls`,
  sửa mẫu bằng Excel là ảnh đổi theo.
- Tên khách và ngày của bảng kê.
- Bảng hàng: số thứ tự, tên hàng, đơn vị, số lượng, đơn giá, thành tiền. Ghi chú của dòng
  (nếu có) đi ngay dưới tên hàng.
- **Tổng tiền hàng trong ngày** kèm số tiền **bằng chữ** — để khỏi cãi nhau vì một số 0.
- Tiền khách trả trong chính ngày ấy (nếu có).
- **Còn nợ tính đến hôm nay**, gồm mọi hoá đơn của khách.

## Mấy điều phần mềm tự lo

**Khách lấy ở hai tờ hoá đơn cùng một ngày.** Ví dụ một tờ cho công trình nhà, một tờ cho
quán. Bảng kê gom cả hai tờ và ghi rõ dòng nào thuộc tờ nào; gửi thiếu một tờ là khách đối
chiếu ra ngay. Chỉ có một tờ thì không ghi mã tờ cho đỡ rối.

**Hàng khách trả lại.** Dòng số lượng âm ghi thẳng vào tờ đang mở, hay dòng của một tờ hoàn
hàng, đều hiện **màu đỏ** kèm chữ *(khách trả lại)* và trừ vào tổng tiền hàng trong ngày.

**Số còn nợ tính đến hôm nay, không phải đến ngày của bảng kê.** Sáng nay mới gửi bảng kê
của hôm qua thì khách vẫn muốn biết *ngay lúc này* mình còn nợ bao nhiêu. Ngày tính nợ ghi
rõ trong ảnh (*"Còn nợ tính đến ngày 31/08/2026"*) để khỏi hiểu nhầm.

**Ngày trống.** Hôm ấy khách không lấy hàng mà cũng không trả tiền thì màn hình báo luôn,
không dựng ảnh — khỏi lỡ tay gửi cho khách một tờ giấy trắng.

**Tên hàng dài** không bị cắt cụt: nó tự xuống dòng, tấm ảnh cao thêm một chút.

## Bản lưu

Mỗi lần bấm *Chép ảnh*, phần mềm cất luôn một bản `.png` ở:

```
%APPDATA%\QuanLyDienNuoc\BangKeNgay\Bang ke <tên khách> <dd-MM-yyyy>.png
```

Đường dẫn hiện ở dải dưới cùng màn hình. Gửi lại lần nữa (khách xoá mất tin) thì mở đúng
file ấy, khỏi phải dựng lại. Cùng khách cùng ngày thì đè lên file cũ, không sinh ra một
đống file trùng.

## Chỗ sửa trong mã nguồn

| Việc | File |
|---|---|
| Gom dòng hàng trong ngày, tính tổng và còn nợ | [TongHopNgay.cs](../src/QuanLyDienNuoc.Core/BaoCao/TongHopNgay.cs) |
| Vẽ tấm ảnh (bố cục, cỡ chữ, màu) | [AnhBangKeNgay.cs](../src/QuanLyDienNuoc/Ui/AnhBangKeNgay.cs) |
| Màn hình chọn ngày, chép ảnh, lưu file | [TongHopNgayForm.cs](../src/QuanLyDienNuoc/Forms/TongHopNgayForm.cs) |
| Kiểm thử phần gom dòng | [TongHopNgayTests.cs](../tests/QuanLyDienNuoc.Tests/TongHopNgayTests.cs) |

Ảnh bề ngang cố định 1000 px cho vừa màn hình điện thoại, chiều cao co theo số dòng hàng.
Muốn ảnh to nhỏ khác đi thì sửa hằng số `AnhBangKeNgay.RongAnh`, còn bề ngang từng cột nằm
ở mảng `TyLeCot` (tính theo phần trăm nên đổi bề ngang ảnh là cột tự co theo).
