# App chấm công — sao lưu

Sổ chấm công thật nằm trong bộ nhớ của cái điện thoại đang cầm (`chamcong.dulieu.v1` trong
AsyncStorage, xem [luuTru.ts](../mobile/src/nghiepvu/luuTru.ts)). Hộp thư đối chiếu trên
Supabase **không** giữ sổ: trong đó chỉ có số công của từng thợ, không có một đồng tiền nào —
xem [chamcong-doi-chieu.md](chamcong-doi-chieu.md).

Nghĩa là nếu không sao lưu thì dữ liệu chỉ có đúng một chỗ. Gỡ app, đổi máy mới, lỡ tay xoá
mấy chục buổi công — mất sạch. File Excel xuất ra không cứu được: nó cắt sẵn theo kỳ, làm
tròn, bỏ id, nạp ngược lại không ra dữ liệu cũ.

Có ba đường sao lưu, và **chúng chống ba chuyện khác nhau**. Đây là điều quan trọng nhất của
tài liệu này.

| Đường | Chống được | Không chống được |
|---|---|---|
| Bản trong máy (tự chạy) | Hỏng dữ liệu, lỡ tay xoá, bản cập nhật app làm hỏng sổ | Xoá app, mất máy, rơi máy |
| Bản trên tài khoản (tự chạy, máy chủ đã đăng nhập email) | Cả những chuyện trên, kể cả mất máy | Không có mạng, không có tài khoản, chủ quên mật khẩu |
| Gửi bản ra ngoài (bấm tay) | Cả những chuyện trên | — |

Vì vậy màn hình Sao lưu luôn có câu nhắc gửi một bản ra ngoài, kể cả lúc vừa sao lưu xong và
kể cả khi bản trên tài khoản đang chạy. Bỏ câu ấy đi là màn hình đang nói dối người dùng: nó
ghi "đã sao lưu lúc 16:12" trong khi mất máy là mất cả bản sao lưu ấy. Có một bài kiểm thử
canh đúng câu này.

## 1. Cách chạy

| Việc | Khi nào chạy |
|---|---|
| Ghi bản mới vào máy | 20 giây sau lần đổi dữ liệu cuối cùng |
| Đẩy cả sổ lên tài khoản | 2 phút sau lần đổi cuối, và lúc mở app nếu hôm nay chưa có bản |
| Ghi ngay | Bấm *Sao lưu ngay* trong màn hình Thợ → Sao lưu |
| Đẩy lên tài khoản ngay | Bấm *Sao lưu lên tài khoản ngay* trong cùng màn hình |
| Gửi một bản ra ngoài | Bấm *Gửi bản ra ngoài* → chọn Zalo, mail, Files… |
| Khôi phục một bản trong máy | Chọn một bản trong danh sách, xác nhận |
| Lấy một bản trên tài khoản về | Bấm *Lấy về* ở bản ấy, xác nhận |
| Khôi phục từ file tự chọn | Bấm *Khôi phục từ file*, chọn file `.json` đã gửi ra ngoài |
| Dọn bản cũ | Sau mỗi lần sao lưu, giữ 30 bản gần nhất — cả hai đường |

Không có nút Lưu — sao lưu chạy ngầm giống hệt cách app ghi xuống bộ nhớ. Bản trong máy không
phải nối tài khoản nào cả; bản trên tài khoản thì đúng như tên nó, cần chủ đã đăng nhập.

## 2. Bản trong máy

**Mỗi ngày một file**, tên `Cham-cong-2026-08-05.json`, nằm trong thư mục `SaoLuu` thuộc phần
riêng của app. Trong ngày sao lưu bao nhiêu lần cũng chỉ ghi đè lên file của ngày hôm ấy.

Điểm này quan trọng, không phải để tiết kiệm chỗ: nếu chỉ giữ **một** file duy nhất thì hôm
nay lỡ tay xoá nhầm mấy chục buổi công, bản tự động sẽ chép luôn cái sai ấy đè lên bản đúng —
sao lưu mà vẫn mất dữ liệu. Giữ theo ngày thì lúc nào cũng lùi về được hôm qua, hoặc tháng
trước.

**Chạy trên cả hai vai.** Bản Drive trước đây phải tắt trên máy thợ vì cả nhóm nối chung một
tài khoản Google mà tên file chỉ theo ngày — hai máy cùng sao lưu là ghi đè lên nhau, bản còn
lại là của máy bấm sau. Sao lưu vào máy thì mỗi máy một thư mục riêng, không còn chuyện ấy.

## 3. Bản trên tài khoản: đổi máy thì sổ theo tài khoản mà sang

Chuyện có thật, và là lý do có phần này: chủ đổi điện thoại, đăng nhập đúng tài khoản cũ, vào
lại đúng nhóm cũ — mà mở app ra thì sổ trắng trơn. Tài khoản trước đây chỉ mang theo *chỗ
trong nhóm* (`thanh_vien` gắn với `user_id`), không mang theo sổ. Hộp thư đối chiếu không đỡ
được chỗ ấy và cũng không nên đỡ: trong đó chỉ có số công của từng thợ, không có mốc lương,
không có ứng tiền, không có kỳ đã chốt — dựng sổ lại từ đó là dựng ra một sổ khác.

Tệ hơn cả việc sổ trống: chủ ngồi nhập lại danh sách thợ trên máy mới thì `thoId` sinh mới,
không khớp `tho_id` trong `thanh_vien` và `so_cong` cũ. Đối chiếu không ghép được ai với ai,
thợ phải nhận mã mời lại, và sổ cũ trong máy thợ cũng rơi ra ngoài id mới. Làm lại bằng tay
không cứu được, càng làm càng lệch.

Nên có bảng `sao_luu`: **mỗi tài khoản mỗi ngày một bản, mang cả sổ, kể cả tiền.**

### Ai đọc được

Đây là bảng duy nhất trên Supabase có tiền trong đó, nên phần này là phần đáng soát nhất:

- Khoá là `user_id`, **không phải `nhom_id`**. Policy `sao_luu_cua_minh` chỉ cho đúng tài khoản
  đã ghi được đọc và ghi. Cả nhóm không ai thấy, kể cả thợ cùng nhóm. Đổi sang khoá theo nhóm
  là mở tiền công của cả cửa hàng cho mọi máy thợ.
- Tài khoản **ẩn danh không ghi được** (`is_anonymous` trong policy). Máy thợ đăng nhập ẩn
  danh, mà tài khoản ẩn danh chỉ sống trong đúng cái điện thoại ấy: sao lưu vào đó là sao lưu
  vào cái máy sắp mất, đổi lấy một chỗ nữa có tiền. Thợ mất máy thì dán mã mời mới, sổ công của
  họ vẫn nằm trong sổ chủ.
- Cả hai điều trên có bài kiểm tra chạy trên Postgres thật, cạnh các bài của hộp thư:
  [kiem-tra-rls.sql](../mobile/supabase/kiem-tra-rls.sql). RLS là ổ khoá duy nhất — khoá công
  khai nằm sẵn trong app, ai gỡ app ra cũng đọc được.

### Điều quan trọng nhất: không đẩy sổ trống lên đè bản thật

Đây là cái bẫy của việc cho sổ theo tài khoản, và nó nằm đúng ở chỗ người dùng không bấm gì
cả. Chủ đăng nhập trên máy mới: sổ trong máy trống, mà lượt đẩy thì chạy ngầm sau hai phút.
Không có luật chặn thì đúng bản sổ họ đang đi tìm bị một sổ trống ghi đè.

Nên lượt đẩy đợi **biết chắc** một trong ba điều, và đây là toàn bộ luật
([dungSaoLuuTaiKhoan.ts](../mobile/src/giaodien/dungSaoLuuTaiKhoan.ts)):

1. Máy này lúc mở app đã có sổ sẵn — sổ của chính nó, bản trên tài khoản chỉ là bản cũ.
2. Trên tài khoản chưa có bản nào — không có gì để mất.
3. Người dùng đã trả lời câu *lấy sổ trên tài khoản về?* — lấy về, hay tự nói là không.

Ba điều kèm theo, mỗi điều chặn một cách sai:

- **Chưa đọc được danh sách bản thì không đẩy.** Mất mạng là *không biết* trên tài khoản có gì,
  khác hẳn *biết là chưa có* — cùng một lẽ với `traHut` bên
  [dungSupabase.ts](../mobile/src/giaodien/dungSupabase.ts).
- **Cờ "máy này lúc mở app đã có sổ chưa" chốt một lần,** ở nhịp đầu tiên đọc xong dữ liệu.
  Đọc lại `soTrong` mỗi lượt thì người dùng gõ một dòng vào sổ trống là máy mới lại được phép
  đẩy cái sổ một dòng ấy lên.
- **Sổ trống thì không đẩy, kể cả khi bấm thẳng vào nút.** Nút *Sao lưu lên tài khoản ngay* nằm
  ngay trong màn hình Sao lưu, mà máy mới đăng nhập xong thì đó là chỗ người ta mở ra đầu tiên
  để tìm sổ cũ. Bấm một cái là xoá đúng thứ mình đang đi tìm.

Bài kiểm thử canh cả ba điều: [dungSaoLuuTaiKhoan.test.tsx](../mobile/src/giaodien/__tests__/dungSaoLuuTaiKhoan.test.tsx).
Nhìn code không thấy được loại lỗi này, nhìn màn hình cũng không — nó chỉ hiện ra mấy hôm sau,
lúc người dùng vào tìm bản cũ.

Và vì **mỗi ngày một bản, giữ 30 ngày**, cái sai của hôm nay không xoá được bản của hôm qua.

### Màn hình chắn ngang lúc mở app

[ManHinhLaySo.tsx](../mobile/src/giaodien/ManHinhLaySo.tsx) hiện trước cả thanh tab khi **máy
này chưa có sổ mà tài khoản thì có**, giống cách [ManHinhMoDau](../mobile/src/giaodien/ManHinhMoDau.tsx)
hiện khi máy chưa vào nhóm. Chắn ngang chứ không phải một dòng chữ trong mục Sao lưu, và lý do
không phải để long trọng: chấm vài ô vào sổ trống là máy này thành máy có sổ riêng, nên câu hỏi
phải được trả lời **trước khi người dùng gõ dòng đầu tiên**.

- **Lấy về vẫn đi qua hộp xác nhận kèm số liệu** — "4 thợ, 312 buổi công, 3 kỳ đã chốt". Máy
  đang trống nên nghe như thừa, nhưng đó là chỗ người dùng nhìn ra mình sắp nhận bản nào.
- **Chỉ chắn khi sổ trong máy *đang* trống,** không chỉ "trống lúc mở app". Thiếu điều kiện
  này là một lỗi bắt được lúc chạy trên máy thật: người dùng thêm một thợ vào sổ trống, lượt
  đẩy ngầm gửi sổ ấy lên (được phép, vì trên tài khoản chưa có bản nào), rồi app quay lại mời
  chính nó lấy về **cái nó vừa ghi** — kèm câu "máy này chưa có buổi công nào" trong lúc trên
  máy đã có thợ. Đã gõ rồi thì thôi chắn ngang: tới đó máy này có sổ riêng của nó.
- **Chỉ mời bản mới nhất.** Muốn bản ngày khác thì vào Thợ → Sao lưu, ở đó có cả danh sách. Bày
  30 ngày ra đây là bắt người vừa mất máy chọn một câu họ chưa có cơ sở để chọn.
- **Hai đường đi tiếp, khác nhau ở đúng một điểm.** *Máy này chấm sổ mới* là một câu trả lời:
  từ đó lượt đẩy ngầm được chạy, nên màn hình nói thẳng cái giá — bản của hôm nay trên tài
  khoản sẽ bị sổ máy này thay, các bản ngày trước vẫn còn. Còn *Để sau* thì **không** trả lời
  gì cả, nên nó không mở đường cho lượt đẩy; cả hai chỉ nhớ trong lượt mở app ấy, sổ vẫn trống
  thì lần mở sau hỏi lại là đúng.

## 4. Những chỗ cố ý làm như vậy

**Khôi phục luôn hỏi lại kèm số liệu** ("bản này có 1 thợ, 12 buổi công, 3 kỳ đã chốt"). Khôi
phục là ghi đè, không lùi lại được; nhìn con số mới biết mình sắp nhận đúng bản hay nhầm bản.
**Cả bốn đường** — bản trong máy, file tự chọn, bản trên tài khoản, và màn hình chắn ngang lúc
mở app — đều đi qua đúng một hàm hỏi ấy: [hoiGhiDe.ts](../mobile/src/giaodien/hoiGhiDe.ts).
Viết lại câu hỏi ở từng màn hình thì sớm muộn có một đường nuốt lặng, mà đường ấy chính là
đường mất sổ.

**Không có gì tự trộn.** Bản trên tài khoản không bao giờ tự nhập vào sổ đang có — cùng một
nguyên tắc với sổ bên kia trong đối chiếu. Tự trộn thì hai máy chủ mở cùng lúc là hai sổ đè
nhau mà không ai biết.

**Mọi gói đọc vào đều qua một bộ kiểm.** Chọn nhầm một file JSON nào đó thì bị từ chối chứ
không nuốt vào rồi xoá sạch sổ; gói của bản app mới hơn cũng bị từ chối, vì cấu trúc có thể đã
khác. Hàng lấy từ database đi qua đúng bộ kiểm ấy (`docGoi`) chứ không được tin sẵn chỉ vì nó
đến từ Postgres: cùng một tài khoản có thể vừa chạy bản app cũ vừa chạy bản mới, và hàng ấy sửa
tay được trong SQL Editor. Soát đuôi `.json` ở
[chonFileSaoLuu.ts](../mobile/src/nghiepvu/chonFileSaoLuu.ts) chỉ để đỡ cho người dùng một lần
đọc file vô ích, **không** phải để tin file.

**Gửi ra ngoài thì đóng gói lại từ dữ liệu đang có**, không gửi lại file bản cũ trong máy:
người bấm nút ấy muốn cầm đi bản mới nhất.

**Gửi qua bảng chia sẻ của hệ điều hành**, không tự lưu vào một thư mục nào. Người dùng chọn
luôn chỗ để — Zalo gửi cho chính mình, hộp thư, thư mục Files — và tự biết file đang nằm đâu
mà mở lại. App cũng không phải xin quyền gì cả.

**Dọn bản cũ không đụng tới file lạ.** Hàm chọn bản để xoá
([`banCanXoa`](../mobile/src/nghiepvu/goiSaoLuu.ts)) bỏ qua mọi tên không đúng khuôn, và nó là
hàm thuần có kiểm thử riêng: chọn sai ở đây là xoá mất bản sao lưu, mà lỗi ấy chỉ hiện ra
đúng lúc người dùng cần quay về. Bản trên tài khoản dùng **đúng phép chọn ấy** trên ngày thay
vì trên tên file (`ngayCanXoa`), không phải một đoạn `sort` rồi `slice` viết lần thứ hai.

**Danh sách bản trên tài khoản không kéo cột `goi` về.** Mỗi bản là cả một sổ; lấy hết là mỗi
lần mở màn hình Sao lưu tốn mấy megabyte 3G để hiện mấy dòng ngày.

**Chờ 2 phút, không phải 20 giây như bản trong máy.** Mỗi lượt đẩy là cả sổ chứ không phải sổ
của một thợ, nên chờ dài hơn để cả một lượt chấm gói vào một lần gọi mạng — và bỏ qua nếu sổ y
hệt lần đẩy trước. Đừng nới thêm nữa: người chấm xong rồi tắt app đi luôn là chuyện thường.

## 5. Vì sao bỏ Google Drive

Bản trước đẩy file JSON lên Drive của chủ, dùng OAuth với quyền `drive.file`. Đã bỏ hẳn, cùng
lúc bỏ hộp thư Drive. Lý do:

- Hộp thư đã chuyển sang Supabase, nơi có phân quyền thật (RLS). Giữ Drive lại chỉ để sao lưu
  thì cả nhóm vẫn phải đăng nhập Google cho một việc duy nhất.
- Cách ấy bắt buộc phải dựng project trên Google Cloud, khai màn hình đồng ý, tạo hai OAuth
  client, lấy vân tay SHA-1, và **phải chuyển sang *In production*** kẻo tuần nào cũng bị hỏi
  đăng nhập lại. Nhiều bước làm bằng tay cho một app dùng trong một nhóm sáu người.
- Không chạy được trong Expo Go, nên máy nào chưa dựng bản cài thẳng là không có sao lưu.

Bản trên tài khoản làm đúng việc mà Drive từng làm — chống mất máy — mà không thêm một tài
khoản nào: nó dùng lại đúng tài khoản chủ đã có để vào nhóm. Không cấu hình gì thêm ngoài việc
chạy lại `thiet-lap.sql`.

## 6. Chỗ nào trong code

| File | Việc |
|---|---|
| `mobile/src/nghiepvu/goiSaoLuu.ts` | Đóng gói / mở gói, tên file, chọn bản cần xoá — thuần, kiểm thử được |
| `mobile/src/nghiepvu/saoLuuMay.ts` | Ghi, liệt kê, đọc, dọn bản cũ trong thư mục của app |
| `mobile/src/nghiepvu/saoLuuTaiKhoan.ts` | Bản trên tài khoản: giao diện, và vì sao chỉ máy chủ dùng |
| `mobile/src/nghiepvu/saoLuuTaiKhoanSupabase.ts` | Ruột Supabase của nó: bảng `sao_luu` |
| `mobile/src/nghiepvu/chiaSeSaoLuu.ts` | Gửi một bản ra khỏi app |
| `mobile/src/nghiepvu/chonFileSaoLuu.ts` | Chọn file `.json` để khôi phục |
| `mobile/src/nghiepvu/chiaSeFile.ts` | Ghi file tạm rồi mở bảng chia sẻ (dùng chung với Excel) |
| `mobile/src/nghiepvu/chonFile.ts` | Mở bảng chọn file (dùng chung với nhập Excel) |
| `mobile/src/giaodien/dungSaoLuu.ts` | Trạng thái bản trong máy và hẹn giờ tự ghi |
| `mobile/src/giaodien/dungSaoLuuTaiKhoan.ts` | Trạng thái bản trên tài khoản, và **luật không đẩy sổ trống** |
| `mobile/src/giaodien/hoiGhiDe.ts` | Hộp hỏi kèm số liệu — mọi đường khôi phục đi qua đây |
| `mobile/src/giaodien/HopSaoLuu.tsx` | Màn hình Sao lưu |
| `mobile/src/giaodien/ManHinhLaySo.tsx` | Màn hình mời lấy sổ về, hiện lúc mở app |
| `mobile/supabase/thiet-lap.sql` | Bảng `sao_luu` và policy của nó |

## 7. Hỏng thì nhìn đâu

| Hiện tượng | Nguyên nhân thường gặp |
|---|---|
| "Chưa ghi được bản sao lưu. Máy có thể đã hết chỗ trống." | Đúng như câu ấy nói: máy hết chỗ |
| "Máy này chưa sao lưu được" | Đang chạy bản web, không có thư mục nào để ghi |
| "Chưa xem được danh sách các bản trong máy." | Không đọc được thư mục `SaoLuu` |
| "File này không phải bản sao lưu chấm công." | Chọn nhầm một file JSON khác |
| "Bản sao lưu này của phiên bản app mới hơn." | File làm từ bản app mới hơn máy đang chạy |
| "Máy này không gửi file đi được." | Bảng chia sẻ của hệ điều hành không dùng được (thường là bản web) |
| "Cần máy chủ đăng nhập bằng email." | Máy thợ, hoặc chủ chưa đăng nhập — bản trên tài khoản không chạy |
| "Chỗ sao lưu trên tài khoản chưa được dựng." | Project còn chạy `thiet-lap.sql` bản trước, chưa có bảng `sao_luu` |
| "Tài khoản này không sao lưu được." | Đang đăng nhập ẩn danh (tài khoản của máy thợ) |
| "Sổ trên máy này đang trống, chưa đẩy lên…" | Đúng luật ở mục 3: máy trống thì không được ghi đè bản trên tài khoản |
| Sổ trống mà không thấy màn hình mời lấy sổ về | Chưa đọc được danh sách bản (mất mạng), hoặc đã bấm *Để sau* trong lượt này |
