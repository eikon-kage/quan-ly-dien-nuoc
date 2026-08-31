# Bảng kê hàng trong ngày — gửi khách qua Zalo

Cuối buổi, khách đã lấy mấy món trong ngày thì gửi cho khách ảnh bảng kê để đối chiếu.
Khách xem ngay trong khung chat Zalo, không phải tải file về rồi tìm phần mềm mở.

Bảng kê này **chỉ kê hàng và số lượng** — không đơn giá, không thành tiền, không tổng tiền,
không còn nợ. Nó để khách soát lại đúng số hàng đã nhận; chuyện tiền nong nói riêng với
khách, chứ gửi vào một khung chat mà cả nhà khách đọc được thì không tiện. Muốn gửi cả tiền
thì dùng **bản in hoá đơn** hoặc **tin nhắc nợ**.

## Làm thế nào

1. Mở **đơn hàng của khách** (bấm đúp vào tên khách ở màn hình chính).
2. Dưới bảng hàng, bấm **BẢNG KÊ TRONG NGÀY**.
3. Chọn ngày: **HÔM NAY**, **HÔM QUA**, hoặc gõ / bấm lịch ở ô *Ngày khác*.
4. Nhìn ảnh xem trước cho đúng khách, đúng ngày, đủ mấy dòng hàng. Bảng kê dài bị cắt ra
   nhiều tấm thì có thanh **‹ ẢNH TRƯỚC / ẢNH SAU ›** (hoặc phím `PageUp` `PageDown`) để xem
   lần lượt.
5. Bấm **CHÉP ẢNH ĐỂ DÁN VÀO ZALO** (hoặc `Ctrl+C`).
6. Mở Zalo, chọn khách, bấm `Ctrl+V` — ảnh vào khung chat, bấm gửi.

Máy không dán được (Zalo bản cũ, hoặc muốn gửi qua mail) thì bấm **Lưu ảnh ra file...**
rồi kéo file `.png` vào khung chat cũng vậy.

## Tấm ảnh có gì

- Tên cửa hàng, địa chỉ, điện thoại — đọc từ chính file mẫu hoá đơn `MauHoaDon\trang-1.xls`,
  sửa mẫu bằng Excel là ảnh đổi theo.
- Tên khách và ngày của bảng kê.
- Bảng hàng bốn cột: **số thứ tự, tên hàng, đơn vị, số lượng**. Ghi chú của dòng (nếu có) đi
  ngay dưới tên hàng.
- Cắt ra nhiều tấm thì mỗi tấm ghi rõ **"Ảnh 2 trong 3"** ngay dưới ngày.

Không có đồng tiền nào trong ảnh — cố ý, xem đầu trang này.

## Mấy điều phần mềm tự lo

**Khách lấy ở hai tờ hoá đơn cùng một ngày.** Ví dụ một tờ cho công trình nhà, một tờ cho
quán. Bảng kê gom cả hai tờ và ghi rõ dòng nào thuộc tờ nào; gửi thiếu một tờ là khách đối
chiếu ra ngay. Chỉ có một tờ thì không ghi mã tờ cho đỡ rối.

**Hàng khách trả lại.** Dòng số lượng âm ghi thẳng vào tờ đang mở, hay dòng của một tờ hoàn
hàng, đều hiện **màu đỏ** kèm chữ *(khách trả lại)*, số lượng để dấu trừ như trong sổ.

**Bảng kê dài thì cắt ra nhiều ảnh.** Một tấm cao quá 2000 px là Zalo thu thành một vệt nhỏ
trong khung chat, mở ra thì chữ đã nén nhoè. Nên quá chỗ ấy là phần mềm sang tấm mới, và
tấm nào cũng có **đủ đầu ảnh** (tên cửa hàng, tên khách, ngày, số hiệu ảnh) — mỗi tấm là một
tin nhắn riêng, khách mở đúng tấm thứ ba mà không thấy tên mình thì không biết của ai. Chân
mấy tấm đầu ghi *"Hàng trong ngày còn nữa — xem tiếp ở ảnh sau."*

**Cắt giữa một tờ hoá đơn** thì đầu trang sau ghi lại mã tờ kèm chữ *(tiếp)*; dòng mã tờ
không bao giờ đứng trơ một mình ở cuối ảnh.

**Ngày trống.** Hôm ấy khách không lấy hàng thì màn hình báo luôn, không dựng ảnh — khỏi lỡ
tay gửi cho khách một tờ giấy trắng. Ngày chỉ có thu tiền mà không có hàng cũng vậy: bảng kê
không ghi tiền nên chẳng có gì để kê.

**Tên hàng dài** không bị cắt cụt: nó tự xuống dòng, tấm ảnh cao thêm một chút.

## Bản lưu

Mỗi lần bấm *Chép ảnh*, phần mềm cất luôn một bản `.png` ở:

```
%APPDATA%\QuanLyDienNuoc\BangKeNgay\Bang ke <tên khách> <dd-MM-yyyy>.png
```

Cắt ra nhiều tấm thì tên file đánh số luôn — `... (trang 2 trong 3).png` — để lúc kéo tay
vào Zalo biết tấm nào trước tấm nào sau.

Đường dẫn hiện ở dải dưới cùng màn hình. Gửi lại lần nữa (khách xoá mất tin) thì mở đúng
file ấy, khỏi phải dựng lại. Cùng khách cùng ngày thì đè lên file cũ, không sinh ra một
đống file trùng.

Bấm *Chép ảnh* khi bảng kê có nhiều tấm thì bộ nhớ máy nhận **cả bộ file** (Ctrl+V một lần
ra đủ mấy tấm), còn ảnh dán thẳng dạng ảnh là tấm đang xem — Zalo bản cũ chỉ nhận một tấm
thì kéo lần lượt từng file trong thư mục trên.

## Chỗ sửa trong mã nguồn

| Việc | File |
|---|---|
| Gom dòng hàng trong ngày (và tính còn nợ cho chỗ khác dùng) | [TongHopNgay.cs](../src/QuanLyDienNuoc.Core/BaoCao/TongHopNgay.cs) |
| Vẽ ảnh (bố cục, cỡ chữ, màu, cắt trang) | [AnhBangKeNgay.cs](../src/QuanLyDienNuoc/Ui/AnhBangKeNgay.cs) |
| Phép chia trang (hàm thuần, có kiểm thử) | [ChiaTrangAnh.cs](../src/QuanLyDienNuoc.Core/Ui/ChiaTrangAnh.cs) |
| Màn hình chọn ngày, lật ảnh, chép, lưu file | [TongHopNgayForm.cs](../src/QuanLyDienNuoc/Forms/TongHopNgayForm.cs) |
| Kiểm thử phần gom dòng | [TongHopNgayTests.cs](../tests/QuanLyDienNuoc.Tests/TongHopNgayTests.cs) |
| Kiểm thử phần cắt ảnh | [ChiaTrangAnhTests.cs](../tests/QuanLyDienNuoc.Tests/ChiaTrangAnhTests.cs) |

Ảnh bề ngang cố định 1000 px cho vừa màn hình điện thoại, chiều cao co theo số dòng hàng và
tối đa `AnhBangKeNgay.CaoToiDa` (2000 px) một tấm. Muốn ảnh to nhỏ khác đi thì sửa hai hằng
số ấy, còn bề ngang từng cột nằm ở mảng `TyLeCot` (tính theo phần trăm nên đổi bề ngang ảnh
là cột tự co theo).

Chỗ trống cho dòng hàng trên mỗi tấm **không cộng tay**: phần mềm đo thử một trang có đủ đầu
đủ chân nhưng không dòng nào, rồi lấy `CaoToiDa` trừ đi. Sửa bố cục đầu ảnh về sau không phải
đi sửa lại con số nào.
