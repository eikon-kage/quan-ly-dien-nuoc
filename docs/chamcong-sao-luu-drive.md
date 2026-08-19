# App chấm công — sao lưu lên Google Drive

Trước khi có tính năng này, dữ liệu chấm công **chỉ nằm trong một chỗ duy nhất**: bộ nhớ
của cái điện thoại đang cầm. Mất máy, rơi máy, gỡ app, đổi sang máy mới — mất sạch, không
có đường lấy lại. File Excel xuất ra không cứu được: nó cắt sẵn theo kỳ, làm tròn, bỏ id,
nạp ngược lại không ra dữ liệu cũ.

Tính năng này nối app với tài khoản Google của chủ cửa hàng. Nối một lần, từ đó cứ chấm
công là ít phút sau bản mới tự nằm trên Drive.

## 1. Cách chạy

| Việc | Khi nào chạy |
|---|---|
| Đẩy bản mới lên Drive | 20 giây sau lần đổi dữ liệu cuối cùng |
| Đẩy ngay | Bấm nút *Sao lưu ngay* trong màn hình Thợ → Sao lưu Google Drive |
| Khôi phục | Chọn một bản trong danh sách, xác nhận |
| Dọn bản cũ | Sau mỗi lần sao lưu, giữ 30 bản gần nhất |

**Mỗi ngày một file**, tên `Cham-cong-2026-08-05.json`. Trong ngày sao lưu bao nhiêu lần
cũng chỉ ghi đè lên file của ngày hôm ấy.

Điểm này quan trọng, không phải để tiết kiệm chỗ: nếu chỉ giữ **một** file duy nhất thì
hôm nay lỡ tay xoá nhầm mấy chục buổi công, bản sao lưu tự động sẽ chép luôn cái sai ấy đè
lên bản đúng — sao lưu mà vẫn mất dữ liệu. Giữ theo ngày thì lúc nào cũng lùi về được
hôm qua, hoặc tháng trước.

## 2. Những chỗ cố ý làm như vậy

> Trong cùng thư mục Drive ấy còn có **hộp thư đối chiếu** — các file `Cham-cong-so-chu-*`
> và `Cham-cong-so-tho-*` của tính năng hai máy đối chiếu sổ, xem
> [chamcong-doi-chieu.md](chamcong-doi-chieu.md). Tên chúng không khớp khuôn tên bản sao lưu
> nên hàm dọn bản cũ không đụng tới, và **máy thợ thì không sao lưu** — hai máy cùng tài
> khoản mà cùng sao lưu là ghi đè lên nhau.

**Quyền xin của Google chỉ là `drive.file`.** App **chỉ thấy được những file do chính nó
tạo ra**, không đọc được bất cứ thứ gì khác trong Drive của người dùng. Đừng đổi sang
`drive` hay `drive.readonly` — hai quyền ấy đọc được cả kho Drive, Google xếp vào loại hạn
chế, muốn phát hành phải qua kiểm định bảo mật tốn kém.

**Refresh token nằm trong SecureStore** (Keychain của iOS, Keystore của Android) chứ không
nằm trong AsyncStorage. AsyncStorage là file thường, máy đã root/jailbreak là đọc được, mà
cầm refresh token thì vào được Drive của người dùng cho tới khi họ thu hồi.

**Không có client secret ở đâu cả.** Vì vậy phải tạo OAuth client kiểu **iOS** và
**Android** — hai kiểu này không phát secret. Tuyệt đối không dùng kiểu *Web application*:
kiểu ấy bắt buộc có secret, mà secret nhét vào app cài trên máy người dùng thì ai cũng
moi ra được.

**Client ID thì ngược lại, không phải bí mật.** Nó nằm sẵn trong mọi app cài trên máy.
Google chặn kẻ giả mạo bằng cách khác: iOS phải khớp bundle ID, Android phải khớp cả tên
gói lẫn vân tay chữ ký. Nên để trong `EXPO_PUBLIC_*` là đúng, commit file `.env` cũng được
— mà cần thế thật thì máy dựng app mới đọc ra.

**Khôi phục luôn hỏi lại kèm số liệu** ("bản này có 1 thợ, 12 buổi công, 3 kỳ đã chốt").
Khôi phục là ghi đè, không lùi lại được; nhìn con số mới biết mình sắp nhận đúng bản hay
nhầm bản.

## 3. Chuẩn bị — việc phải làm bằng tay

Chưa làm mấy bước này thì phần Drive tự ẩn đi, app vẫn chạy bình thường như trước.

### 3.1. Tạo project trên Google Cloud

1. Vào <https://console.cloud.google.com/> → tạo project mới, đặt tên gì cũng được
   (ví dụ *Cham cong*).
2. Vào **APIs & Services → Library**, tìm **Google Drive API**, bấm **Enable**.

### 3.2. Khai màn hình đồng ý

Vào **APIs & Services → OAuth consent screen**:

1. Chọn **External**.
2. Điền tên app, email hỗ trợ, email liên hệ.
3. Phần **Scopes**: thêm `.../auth/drive.file`. Không thêm quyền Drive nào khác.
4. **Đổi trạng thái sang *In production*.**

> Bước 4 dễ bỏ sót và hậu quả rất khó chịu: để nguyên trạng thái *Testing* thì refresh
> token **hết hạn sau 7 ngày**, cứ mỗi tuần người dùng lại phải nối Drive lại một lần mà
> không hiểu vì sao. Ba quyền app này xin (`openid`, `email`, `drive.file`) đều thuộc loại
> không nhạy cảm, nên chuyển sang *In production* **không cần** Google kiểm định gì.

### 3.3. Tạo hai OAuth client

Vào **APIs & Services → Credentials → Create Credentials → OAuth client ID**:

**Client cho iOS**
- Application type: **iOS**
- Bundle ID: `com.quanlydiennuoc.chamcong`

**Client cho Android**
- Application type: **Android**
- Package name: `com.quanlydiennuoc.chamcong`
- SHA-1 certificate fingerprint: lấy bằng lệnh

  ```sh
  cd mobile
  npx eas credentials
  ```

  chọn Android → chọn profile đang dùng → chép dòng **SHA1 Fingerprint**.

  > Vân tay này gắn với **khoá ký app**. Bản dựng để chạy thử (`development`) và bản phát
  > hành ký bằng hai khoá khác nhau thì phải khai cả hai vân tay, nếu không thì bản kia
  > đăng nhập sẽ báo lỗi `redirect_uri_mismatch` hoặc `invalid_client`.

### 3.4. Điền client ID vào app

```sh
cd mobile
cp .env.example .env
```

Mở `.env`, điền hai mã vừa tạo:

```
EXPO_PUBLIC_GOOGLE_CLIENT_ID_IOS=123456-abcdef.apps.googleusercontent.com
EXPO_PUBLIC_GOOGLE_CLIENT_ID_ANDROID=123456-ghijkl.apps.googleusercontent.com
```

`app.config.js` tự đọc mã iOS ra để khai URL scheme cho Google gọi ngược về app.

### 3.5. Dựng development build

**Sao lưu Drive không chạy trong Expo Go.** Google chỉ chấp nhận địa chỉ trả về gắn với
bundle ID (iOS) hoặc tên gói (Android), mà Expo Go thì mang địa chỉ `exp://` của riêng nó.

```sh
cd mobile
npx eas build:configure       # lần đầu, để sinh eas.json
npx eas build --profile development --platform android
```

Cài file vừa dựng vào máy, rồi chạy `npx expo start --dev-client`.

Trong Expo Go, dòng *Sao lưu Google Drive* vẫn hiện nhưng ghi "Cần bản app cài thẳng vào
máy" — cố tình như vậy để khỏi ai bấm vào rồi nhận lỗi khó hiểu.

## 4. Chỗ nào trong code

| File | Việc |
|---|---|
| `mobile/src/nghiepvu/cauHinhGoogle.ts` | Client ID, quyền xin, địa chỉ trả về |
| `mobile/src/nghiepvu/dangNhapGoogle.ts` | Đăng nhập, giữ và làm mới token |
| `mobile/src/nghiepvu/goiDrive.ts` | Gọi Drive API bằng `fetch` |
| `mobile/src/nghiepvu/goiSaoLuu.ts` | Đóng gói / mở gói file sao lưu |
| `mobile/src/nghiepvu/saoLuuDrive.ts` | Điều phối: sao lưu, liệt kê, khôi phục, dọn bản cũ |
| `mobile/src/giaodien/dungSaoLuu.ts` | Trạng thái sao lưu và hẹn giờ tự đẩy |
| `mobile/src/giaodien/HopSaoLuu.tsx` | Màn hình Sao lưu |
| `mobile/app.config.js` | Khai URL scheme cho Google gọi về |

## 5. Hỏng thì nhìn đâu

| Hiện tượng | Nguyên nhân thường gặp |
|---|---|
| Bấm Nối, trình duyệt mở rồi báo `redirect_uri_mismatch` | SHA-1 hoặc bundle ID khai sai ở bước 3.3 |
| Bấm Đồng ý xong màn hình đứng im, không quay về app | Chưa dựng lại app sau khi điền `.env` — scheme sinh ra từ client ID |
| Tuần nào cũng phải nối lại | Màn hình đồng ý còn ở trạng thái *Testing* (bước 3.2) |
| "Kết nối Google Drive đã hết hạn" | Người dùng đã thu hồi quyền hoặc đổi mật khẩu Google |
| "Chưa đẩy lên Drive được. Sẽ tự thử lại sau." | Mất mạng. Lần đổi dữ liệu sau sẽ tự thử lại |
