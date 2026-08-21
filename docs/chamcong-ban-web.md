# App chấm công — bản web cài lên màn hình chính

Bản này để **cài app lên iPhone mà không mất tiền và không phải dựng lại sau 7 ngày**. Bản
Android vẫn là bản React Native đóng thành APK như cũ, không đổi gì.

Vì sao phải có đường này: Apple chỉ cho chứng chỉ 7 ngày với Apple ID miễn phí, và Expo Go
trên App Store thì đã mắc kẹt ở SDK cũ (bản cho SDK 55 còn chờ Apple duyệt từ 4/5/2026, dự án
này đang SDK 57). `eas go` thì bắt buộc tài khoản Apple Developer trả phí. Nên trên iPhone,
đường duy nhất vừa miễn phí vừa không hết hạn là **chạy như trang web rồi Thêm vào Màn hình
chính**: có icon riêng, mở ra không thấy thanh địa chỉ, và không có ngày hết hạn nào.

## 1. Dựng và đẩy lên

```bash
cd mobile
npm run build:web                                   # phát từ gốc tên miền (chạy thử ở nhà)
GOC_WEB=/quan-ly-dien-nuoc npm run build:web        # phát từ địa chỉ con (GitHub Pages)
```

`npm run build:web` gọi [dung-web.mjs](../mobile/scripts/dung-web.mjs): nó chạy
`expo export -p web` rồi **điền vào `dist/sw.js`** tên file mã của đúng bản vừa dựng. Đừng gọi
`expo export` trực tiếp — service worker sẽ nạp sẵn tên file của bản cũ, và lỗi ấy chỉ hiện ra
đúng lúc người dùng mất mạng.

Đẩy lên thì đã có workflow [ban-web-cham-cong.yml](../.github/workflows/ban-web-cham-cong.yml):
mỗi lần đổi gì trong `mobile/` là nó soát kiểu, chạy kiểm thử, dựng, rồi đẩy lên Pages. Lần đầu
phải bật tay một lần: **Settings → Pages → Source: GitHub Actions**. Xong thì địa chỉ là
`https://vinhnqhe161630.github.io/quan-ly-dien-nuoc/`.

Đổi sang Cloudflare Pages, Netlify hay tên miền riêng — chỗ phát từ gốc — thì bỏ biến `GOC_WEB`
đi là xong, xem [app.config.js](../mobile/app.config.js).

### Khoá Supabase phải điền vào chỗ dựng

Địa chỉ và khoá Supabase đi vào bản app **lúc dựng**, qua hai biến
`EXPO_PUBLIC_SUPABASE_URL` và `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` (bảng điều khiển Supabase
bản cũ gọi khoá ấy là `ANON_KEY`; [cauHinhSupabase.ts](../mobile/src/nghiepvu/cauHinhSupabase.ts)
nhận cả hai tên). Ở nhà thì chúng nằm trong file biến môi trường của `mobile/`; còn trên GitHub
thì phải điền ở **Settings → Secrets and variables → Actions**, mục *Variables* hay *Secrets* đều
được — workflow nhận cả hai chỗ và cả hai tên khoá.

> **Bẫy đã dính một lần.** GitHub Actions đặt biến chưa điền thành **chuỗi rỗng**, không phải bỏ
> trống. Nên chỗ chọn giữa hai tên khoá phải dùng `||` chứ không phải `??`: `'' ?? khoáThật` cho
> ra `''`, tức là điền `PUBLISHABLE_KEY` mà workflow có nhắc tới `ANON_KEY` thì khoá thật bị
> chặn lại, và bản dựng ra lặng lẽ không có nhóm, không có đăng nhập, không có sao lưu tài khoản.

Quên điền thì app vẫn dựng, vẫn chấm công, chỉ **tắt hẳn** phần nhóm, đăng nhập và sao lưu lên
tài khoản. Trên bản web thì đó là chuyện lớn: bản web không có sao lưu vào máy, nên sao lưu tài
khoản là lưới an toàn duy nhất còn lại. Workflow có một bước nhắc to nếu thiếu.

Khoá này **không phải bí mật** — nó nằm trong mọi bản app đã phát ra, ai gỡ APK cũng đọc được,
và bản web thì càng dễ đọc hơn. Thứ chặn người này đọc sổ của người kia là **RLS trong
database**. Tuyệt đối không đưa khoá `sb_secret_` hay `service_role` vào đây: chúng bỏ qua RLS.
Soát nhanh một bản dựng:

```bash
grep -o -c "sb_secret_\|service_role" mobile/dist/_expo/static/js/web/*.js
```

Ra `1` cho `sb_secret_` là **bình thường**: đó là mã của thư viện supabase-js soát tiền tố
(`e.startsWith("sb_secret_")`), không phải khoá. Có thêm một chuỗi dài đằng sau tiền tố ấy mới
là chuyện phải chữa ngay.

## 2. Cài lên iPhone

Mở địa chỉ trên **bằng Safari** → nút Chia sẻ → *Thêm vào Màn hình chính*. Chrome trên iOS
không có mục ấy, đây là chỗ hay làm người ta loay hoay nhất.

Mở lần đầu **phải có mạng** (tải mã app về, chừng 2 MB). Từ lần sau thì mất mạng vẫn mở được:
service worker giữ lại mã, còn sổ thì vốn nằm ngay trong máy.

## 3. Bản web khác bản Android những gì

Đây là phần quan trọng nhất của tài liệu này. Ai định phát bản web cho thợ dùng thì phải biết
trước bốn chỗ dưới đây.

| Chỗ | Bản Android (APK) | Bản web |
|---|---|---|
| Sao lưu vào máy, giữ 30 bản | Có | **Không có.** Trình duyệt không cho app một thư mục riêng nào |
| Phiên đăng nhập Supabase | Keychain / Keystore | `localStorage` — mã JavaScript trên trang đọc được |
| Gửi file Excel, gửi bản sao lưu | Bảng chia sẻ của hệ điều hành | Bảng chia sẻ của Safari, không có thì tải về |
| Hộp hỏi lại trước khi ghi đè sổ | Hộp của hệ điều hành | Hộp app tự vẽ, trượt lên từ đáy |

**Mất bản sao lưu trong máy là chỗ đáng lo nhất.** Cả sổ nằm trong `localStorage` của Safari,
mà người dùng bấm "Xoá dữ liệu website" là mất. Trên bản web thì lưới an toàn chỉ còn hai
đường, và cả hai đều chạy tốt: **sao lưu lên tài khoản** và **gửi bản sao lưu ra ngoài**. Xem
[chamcong-sao-luu.md](chamcong-sao-luu.md) — bảng "chống được / không chống được" ở đầu tài
liệu ấy vẫn đúng, chỉ là dòng đầu (bản trong máy) không có trên web.

Màn hình sao lưu đã lường sẵn cảnh này: `dungSaoLuu` trả `hoTro: false` trên web nên nó vẽ đúng
câu "máy này không ghi được bản sao lưu" thay vì một danh sách trống hay một cái nút bấm không
ăn.

Về phiên đăng nhập: `localStorage` là bước lùi thật so với Keychain, và trên web thì không có
gì thay được. Bù lại mặt tấn công hẹp — trang này không nhúng quảng cáo, không nạp script từ
đâu khác. **Giữ nguyên nguyên tắc "không thêm script ngoài vào trang"** là giữ được cửa ấy;
chi tiết trong [khoAnToan.web.ts](../mobile/src/nghiepvu/khoAnToan.web.ts).

## 4. Mã nguồn chia thế nào

Bản Android không kéo theo một dòng mã web nào, và ngược lại: Metro chọn file theo nền tảng
lúc đóng gói. Năm chỗ chạm vào máy có hai bản:

| File | Bản web làm gì khác |
|---|---|
| [chonFile](../mobile/src/nghiepvu/chonFile.web.ts) | Đọc từ đối tượng `File` của trình duyệt, không qua expo-file-system |
| [chiaSeFile](../mobile/src/nghiepvu/chiaSeFile.web.ts) | `navigator.share`, không được thì tải về. Không dùng expo-sharing |
| [khoAnToan](../mobile/src/nghiepvu/khoAnToan.web.ts) | `localStorage`, và giữ trong RAM nếu trình duyệt chặn |
| [saoLuuMay](../mobile/src/nghiepvu/saoLuuMay.web.ts) | Tắt hẳn — không có thư mục nào để ghi |
| [hopThoai](../mobile/src/giaodien/hopThoai.web.tsx) | Tự vẽ hộp hỏi lại bằng `Modal` |

Kiểu dùng chung của mấy cặp file ấy để ở [kieuFile.ts](../mobile/src/nghiepvu/kieuFile.ts) — hai
bản không nhìn thấy nhau, khai lặp thì `instanceof` ở chỗ bắt lỗi sẽ trượt.

### Ba cái bẫy đã dính, đừng dính lại

1. **`Alert.alert` của react-native-web là một hàm rỗng.** Không hiện gì, không báo lỗi. Nên
   mọi câu hỏi lại phải đi qua `hoi()` của [hopThoai](../mobile/src/giaodien/hopThoai.tsx) chứ
   đừng gọi `Alert` trực tiếp — cửa duy nhất của mọi đường khôi phục sổ đi qua đó.

2. **Hai bản của một file phải cùng đuôi.** Metro thử theo đuôi trước rồi mới xét nền tảng:
   hết `.web.ts` → `.ts` xong mới sang `.web.tsx` → `.tsx`. Để `hopThoai.ts` cạnh
   `hopThoai.web.tsx` là bản máy thắng **cả trên web** — đã dính đúng lỗi này, và vì `Alert`
   của web im lặng nên không có dấu hiệu gì.

3. **`dist/sw.js` là bản do script điền**, `public/sw.js` mới là bản gốc. Sửa trong `dist` thì
   lần dựng sau mất sạch.

4. **Metro không dựng lại khi chỉ có biến môi trường đổi.** Đổi khoá Supabase rồi dựng lại mà ra
   đúng file cũ tới từng mã băm — đã dính. Vì vậy `dung-web.mjs` luôn dựng kèm `--clear`: chậm
   thêm gần một phút, đổi lấy việc không bao giờ đẩy lên một bản dựng từ khoá cũ.

## 5. Chạy thử

```bash
cd mobile
npm test                    # ba bộ: nghiepvu, giaodien, và web (jsdom)
npm run web                 # mở bản web bằng máy dựng, sửa mã là tự nạp lại
```

Bài kiểm thử của bản web nằm trong `src/**/__tests__/web/`, chạy bằng `jest-expo/web`. Có một
chỗ **không** kiểm thử được: phần *vẽ* hộp hỏi lại. `Modal` của react-native-web gắn hộp vào
`document.body` bằng portal của react-dom, mà react-test-renderer thì không dựng nổi portal ấy.
Nên phần quyết định (thứ tự nút, chạm ra ngoài coi như bấm nút nào) tách ra hàm riêng để thử,
còn phần vẽ thì soi bằng Chrome thật:

```bash
cd mobile && npm run build:web
cd dist && python3 -m http.server 8899 &
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \
  --headless=new --window-size=500,900 --virtual-time-budget=10000 \
  --screenshot=/tmp/anh.png http://127.0.0.1:8899/
```

Muốn bấm thử từng bước, hay muốn thử cảnh mất mạng, thì mở Chrome kèm
`--remote-debugging-port=9222` rồi lái bằng CDP: `Emulation.setDeviceMetricsOverride` để đúng
khổ điện thoại, `Network.emulateNetworkConditions` với `offline: true` để ngắt mạng. Cách này
đã dùng để soi ba việc mà kiểm thử không nói được: hộp hỏi lại có nằm trên hộp đang mở không,
font tiếng Việt và icon có ra không, và ngắt mạng rồi tải lại app có mở được không.
