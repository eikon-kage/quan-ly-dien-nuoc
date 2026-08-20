# App chấm công — sao lưu

Sổ chấm công thật nằm trong bộ nhớ của cái điện thoại đang cầm (`chamcong.dulieu.v1` trong
AsyncStorage, xem [luuTru.ts](../mobile/src/nghiepvu/luuTru.ts)). Supabase **không** giữ sổ:
ở đó chỉ có hộp thư đối chiếu, và trong hộp thư không có một đồng tiền nào — xem
[chamcong-doi-chieu.md](chamcong-doi-chieu.md).

Nghĩa là nếu không sao lưu thì dữ liệu chỉ có đúng một chỗ. Gỡ app, đổi máy mới, lỡ tay xoá
mấy chục buổi công — mất sạch. File Excel xuất ra không cứu được: nó cắt sẵn theo kỳ, làm
tròn, bỏ id, nạp ngược lại không ra dữ liệu cũ.

Có hai đường sao lưu, và **chúng chống hai chuyện khác nhau**. Đây là điều quan trọng nhất
của tài liệu này.

| Đường | Chống được | Không chống được |
|---|---|---|
| Bản trong máy (tự chạy) | Hỏng dữ liệu, lỡ tay xoá, bản cập nhật app làm hỏng sổ | Xoá app, mất máy, rơi máy |
| Gửi bản ra ngoài (bấm tay) | Cả những chuyện trên | — |

Vì vậy màn hình Sao lưu luôn có câu nhắc gửi một bản ra ngoài, kể cả lúc vừa sao lưu xong.
Bỏ câu ấy đi là màn hình đang nói dối người dùng: nó ghi "đã sao lưu lúc 16:12" trong khi
mất máy là mất cả bản sao lưu ấy. Có một bài kiểm thử canh đúng câu này.

## 1. Cách chạy

| Việc | Khi nào chạy |
|---|---|
| Ghi bản mới vào máy | 20 giây sau lần đổi dữ liệu cuối cùng |
| Ghi ngay | Bấm *Sao lưu ngay* trong màn hình Thợ → Sao lưu |
| Gửi một bản ra ngoài | Bấm *Gửi bản ra ngoài* → chọn Zalo, mail, Files… |
| Khôi phục một bản trong máy | Chọn một bản trong danh sách, xác nhận |
| Khôi phục từ file tự chọn | Bấm *Khôi phục từ file*, chọn file `.json` đã gửi ra ngoài |
| Dọn bản cũ | Sau mỗi lần sao lưu, giữ 30 bản gần nhất |

Không có nút Lưu, và không phải nối tài khoản nào cả — sao lưu vào máy chạy ngầm giống hệt
cách app ghi xuống bộ nhớ.

**Mỗi ngày một file**, tên `Cham-cong-2026-08-05.json`, nằm trong thư mục `SaoLuu` thuộc phần
riêng của app. Trong ngày sao lưu bao nhiêu lần cũng chỉ ghi đè lên file của ngày hôm ấy.

Điểm này quan trọng, không phải để tiết kiệm chỗ: nếu chỉ giữ **một** file duy nhất thì hôm
nay lỡ tay xoá nhầm mấy chục buổi công, bản tự động sẽ chép luôn cái sai ấy đè lên bản đúng —
sao lưu mà vẫn mất dữ liệu. Giữ theo ngày thì lúc nào cũng lùi về được hôm qua, hoặc tháng
trước.

**Chạy trên cả hai vai.** Bản Drive trước đây phải tắt trên máy thợ vì cả nhóm nối chung một
tài khoản Google mà tên file chỉ theo ngày — hai máy cùng sao lưu là ghi đè lên nhau, bản còn
lại là của máy bấm sau. Sao lưu vào máy thì mỗi máy một thư mục riêng, không còn chuyện ấy.

## 2. Những chỗ cố ý làm như vậy

**Khôi phục luôn hỏi lại kèm số liệu** ("bản này có 1 thợ, 12 buổi công, 3 kỳ đã chốt"). Khôi
phục là ghi đè, không lùi lại được; nhìn con số mới biết mình sắp nhận đúng bản hay nhầm bản.
Cả hai đường khôi phục — bản trong máy và file tự chọn — đều đi qua đúng một hàm hỏi ấy.

**Mọi file đọc vào đều qua `moGoi`.** Chọn nhầm một file JSON nào đó thì bị từ chối chứ không
nuốt vào rồi xoá sạch sổ; gói của bản app mới hơn cũng bị từ chối, vì cấu trúc có thể đã khác.
Soát đuôi `.json` ở [chonFileSaoLuu.ts](../mobile/src/nghiepvu/chonFileSaoLuu.ts) chỉ để đỡ
cho người dùng một lần đọc file vô ích, **không** phải để tin file.

**Gửi ra ngoài thì đóng gói lại từ dữ liệu đang có**, không gửi lại file bản cũ trong máy:
người bấm nút ấy muốn cầm đi bản mới nhất.

**Gửi qua bảng chia sẻ của hệ điều hành**, không tự lưu vào một thư mục nào. Người dùng chọn
luôn chỗ để — Zalo gửi cho chính mình, hộp thư, thư mục Files — và tự biết file đang nằm đâu
mà mở lại. App cũng không phải xin quyền gì cả.

**Dọn bản cũ không đụng tới file lạ.** Hàm chọn bản để xoá
([`banCanXoa`](../mobile/src/nghiepvu/goiSaoLuu.ts)) bỏ qua mọi tên không đúng khuôn, và nó là
hàm thuần có kiểm thử riêng: chọn sai ở đây là xoá mất bản sao lưu, mà lỗi ấy chỉ hiện ra
đúng lúc người dùng cần quay về.

## 3. Vì sao bỏ Google Drive

Bản trước đẩy file JSON lên Drive của chủ, dùng OAuth với quyền `drive.file`. Đã bỏ hẳn, cùng
lúc bỏ hộp thư Drive. Lý do:

- Hộp thư đã chuyển sang Supabase, nơi có phân quyền thật (RLS). Giữ Drive lại chỉ để sao lưu
  thì cả nhóm vẫn phải đăng nhập Google cho một việc duy nhất.
- Cách ấy bắt buộc phải dựng project trên Google Cloud, khai màn hình đồng ý, tạo hai OAuth
  client, lấy vân tay SHA-1, và **phải chuyển sang *In production*** kẻo tuần nào cũng bị hỏi
  đăng nhập lại. Nhiều bước làm bằng tay cho một app dùng trong một nhóm sáu người.
- Không chạy được trong Expo Go, nên máy nào chưa dựng bản cài thẳng là không có sao lưu.

Sao lưu vào máy không cần cấu hình gì, chạy trên mọi máy, và đường gửi ra ngoài phủ đúng
chỗ mà Drive từng phủ — chỉ khác là người dùng tự chọn cất vào đâu.

## 4. Chỗ nào trong code

| File | Việc |
|---|---|
| `mobile/src/nghiepvu/goiSaoLuu.ts` | Đóng gói / mở gói, tên file, chọn bản cần xoá — thuần, kiểm thử được |
| `mobile/src/nghiepvu/saoLuuMay.ts` | Ghi, liệt kê, đọc, dọn bản cũ trong thư mục của app |
| `mobile/src/nghiepvu/chiaSeSaoLuu.ts` | Gửi một bản ra khỏi app |
| `mobile/src/nghiepvu/chonFileSaoLuu.ts` | Chọn file `.json` để khôi phục |
| `mobile/src/nghiepvu/chiaSeFile.ts` | Ghi file tạm rồi mở bảng chia sẻ (dùng chung với Excel) |
| `mobile/src/nghiepvu/chonFile.ts` | Mở bảng chọn file (dùng chung với nhập Excel) |
| `mobile/src/giaodien/dungSaoLuu.ts` | Trạng thái sao lưu và hẹn giờ tự ghi |
| `mobile/src/giaodien/HopSaoLuu.tsx` | Màn hình Sao lưu |

## 5. Hỏng thì nhìn đâu

| Hiện tượng | Nguyên nhân thường gặp |
|---|---|
| "Chưa ghi được bản sao lưu. Máy có thể đã hết chỗ trống." | Đúng như câu ấy nói: máy hết chỗ |
| "Máy này chưa sao lưu được" | Đang chạy bản web, không có thư mục nào để ghi |
| "Chưa xem được danh sách các bản trong máy." | Không đọc được thư mục `SaoLuu` |
| "File này không phải bản sao lưu chấm công." | Chọn nhầm một file JSON khác |
| "Bản sao lưu này của phiên bản app mới hơn." | File làm từ bản app mới hơn máy đang chạy |
| "Máy này không gửi file đi được." | Bảng chia sẻ của hệ điều hành không dùng được (thường là bản web) |
