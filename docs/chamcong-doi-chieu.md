# Chấm công: hai máy đối chiếu sổ với nhau

Một máy là **máy chủ** — chủ chấm công cho cả nhóm, tính lương, chốt kỳ. Các máy khác là
**máy thợ** — mỗi thợ tự chấm cho mình. Hai bên giữ **hai sổ riêng**, đọc được sổ của nhau,
và có màn hình đối chiếu chỉ ra chỗ hai bên ghi khác nhau.

## Điều quan trọng nhất: không có gì tự trộn

Sổ bên kia là **bản chụp để đọc**, lưu ở một khoá riêng trong máy
([soBenKia.ts](../mobile/src/nghiepvu/soBenKia.ts)), không bao giờ nhập vào
`DuLieuChamCong`. Bảng lương và quyết toán chỉ tính từ sổ của chính máy đó.

Nếu để dữ liệu bên kia tự chảy vào sổ mình thì thợ tự thêm công cho mình được, và bảng lương
của chủ đổi số mà chủ không hề biết. Muốn sửa thì bấm **từng dòng lệch**, không có nút lấy
tất cả — chỗ này là chỗ tiền ra tiền vào.

Nhờ vậy cũng không cần tới những thứ mà đồng bộ tự động bắt buộc phải có: không cần đánh dấu
bản ghi đã xoá, không sợ buổi đã bỏ chấm sống lại, không sợ hai máy cùng chốt kỳ.

## Sổ công: chỉ có công, không có một đồng nào

Mẩu dữ liệu hai bên trao nhau là [`SoCong`](../mobile/src/nghiepvu/soCong.ts) — ngày, buổi,
số công của **đúng một thợ**. Không mốc lương, không ứng tiền, không kỳ đã chốt.

Cắt tiền ra ngay từ lúc đóng gói, không phải ở giao diện: gói đã gửi đi là nằm trong tay
người ta, mở file ra đọc được hết. Bài kiểm thử soát cả chuỗi JSON xem có số tiền nào lọt ra.

Cùng một kiểu dùng cho **cả hai chiều** — chủ gửi xuống và thợ gửi lên — nên hàm đối chiếu
chỉ có một bản, chạy đúng ở cả hai máy.

## Hai mốc ngày, và vì sao thiếu nó là vô dụng

Mỗi sổ khai `tuNgay`/`denNgay`: khoảng mà nó nói là **đầy đủ**. Đối chiếu chỉ so trong phần
giao của hai khoảng.

Không có hai mốc ấy thì máy thợ mới cài hôm qua, đối chiếu với sổ chủ có ba tháng trước đó,
sẽ ra một trăm dòng "thợ thiếu công" toàn là ngày thợ chưa có app. Người dùng nhìn một màn
hình đỏ rực không sửa được gì rồi thôi, không mở lại nữa.

- Máy thợ khai từ ngày nhận mã mời (`batDauTu`).
- Máy chủ khai 90 ngày gần nhất (`CUA_SO_NGAY`) — đối chiếu là việc của kỳ đang làm.

## Buổi đã quyết toán thì khoá

Sổ chủ mang thêm cờ `daChot` cho những buổi đã nằm trong kỳ đã trả tiền. Dòng lệch ấy vẫn
hiện lên cho hai bên biết, nhưng không có nút sửa: tiền đã trả rồi, sửa số công bây giờ là
bảng lương cũ nói khác tờ quyết toán đã đưa cho thợ.

## Mã mời

Máy chủ đọc cho thợ một mã dạng `CC-mf3k2a-9xq1`, chính là id của thợ trong sổ chủ. Máy thợ
dán mã vào rồi tạo bản ghi thợ mang **đúng id ấy** — hai máy đặt id khác nhau thì lúc đối
chiếu không ghép được ai với ai.

Mã không nhồi tên thợ vào: tên có dấu, gõ lại qua Zalo là sai. Máy thợ lấy tên từ chính sổ
chủ gửi xuống.

Máy cũ của chủ chuyền tay cho thợ thì lúc nhận mã có thêm nút **xoá sổ của người khác** —
bỏ hết bản ghi của người khác và xoá sạch tiền, kể cả mốc lương của chính thợ ấy. Cái gì
không có trên máy thì không ai xem lén được; ẩn bằng giao diện thì vẫn còn nằm đó.

## Hộp thư: hiện tại là Drive dùng chung một tài khoản

[hopThu.ts](../mobile/src/nghiepvu/hopThu.ts) là **một lớp mỏng có thể thay ruột**. Giao diện
của nó chỉ nói bằng lời của việc chấm công — gửi sổ, đọc sổ — không hé một chữ nào về Drive,
file hay token.

Ruột hiện tại là Google Drive. Mỗi (bên gửi, thợ) đúng một file, ghi đè mãi lên nó:

```
Cham-cong-so-chu-<thoId>.json    chủ gửi xuống cho thợ đó
Cham-cong-so-tho-<thoId>.json    thợ đó gửi lên
```

Cả nhóm **đăng nhập cùng một tài khoản Google**. Bắt buộc phải thế: quyền app xin là
`drive.file`, app chỉ thấy được file do chính nó tạo trên tài khoản đó — hai tài khoản khác
nhau thì máy chủ không đọc được file thợ tạo, dù thợ đã chia sẻ.

**Phải nói rõ giới hạn:** cách này *không chặn được gì về mặt quyền*. Máy nào cũng đọc và
xoá được file của máy khác, và ai mở drive.google.com bằng tài khoản ấy là thấy hết. Vì vậy
tài khoản dùng làm hộp thư nên là một Gmail **tạo riêng cho việc này**, không dùng cho gì
khác. Rủi ro mất dữ liệu thì nhẹ: sổ thật của chủ nằm trên máy chủ, Drive chỉ là hộp thư.

Muốn chặn thật thì thay ruột `hopThu.ts` bằng một máy chủ có phân quyền (Firebase Firestore
chẳng hạn: thợ chỉ đọc được phần chủ gửi cho mình, chỉ ghi được sổ của mình). Màn hình đối
chiếu và toàn bộ phần tính toán không phải sửa gì.

### Hộp thư nằm chung chỗ với bản sao lưu

Hai loại file sống cùng một thư mục Drive nên có hai chỗ phải giữ, và cả hai đều có kiểm thử
canh:

1. Tên file sổ **không khớp** khuôn tên bản sao lưu (`Cham-cong-2026-08-19.json`), nên hàm
   dọn bản cũ — chỉ giữ 30 bản gần nhất — không xoá mất hộp thư.
2. **Máy thợ không sao lưu.** Tên bản sao lưu chỉ theo ngày, hai máy cùng tài khoản mà cùng
   sao lưu là ghi đè lên nhau, bản còn lại là của máy bấm sau. Sổ máy thợ vốn đã nằm trong
   hộp thư nên không mất gì.

## Khi nào đồng bộ

Một lần lúc mở app (nếu đã nối Google), và mỗi lần bấm mũi tên đồng bộ. Không chạy ngầm sau
từng ô chấm như sao lưu: đối chiếu là việc cuối ngày hay cuối kỳ, đẩy đi liên tục chỉ tốn 3G
của cả nhóm cho những con số chưa ai xem.

Sổ nhận về ghi xuống máy ngay, nên mất mạng vẫn xem đối chiếu được — chỉ là số liệu tính đến
lần đồng bộ gần nhất, và màn hình ghi rõ giờ của lần ấy.

## Chưa làm

- **Ứng tiền chưa đối chiếu.** Ứng tiền là tiền, mà máy thợ được quy định chỉ thấy số công.
  Muốn thợ soát được "tôi ứng 500, sổ ghi 300" thì phải mở cho máy thợ thấy tiền ứng của
  chính mình — một quyết định về nghiệp vụ, không phải về code.
- **Chưa có QR.** Mã mời đọc bằng miệng hoặc dán qua Zalo. Một tháng chấm công của một thợ
  chỉ vài chục byte nếu mã hoá gọn, lọt thừa vào một QR — sau này thêm được đường trao sổ
  không cần mạng.
