-- Xoá dữ liệu hộp thư chấm công trên Supabase.
--
-- File chia làm bốn mức, mỗi mức xoá nhiều hơn mức trước. Chạy đúng khối mình cần và bỏ qua
-- phần còn lại.
--
-- Mọi lệnh ở đây chạy với quyền quản trị của SQL Editor, tức là **bỏ qua RLS**. Những policy
-- trong thiet-lap.sql không chặn gì ở đây — chúng chỉ chặn app. Xoá là xoá thật, không có
-- thùng rác, không có hoàn tác.
--
-- **Các lệnh `delete` ở đây chạy được ngay, không bị chú thích.** Vì vậy: đừng dán cả file
-- vào SQL Editor rồi bấm Run — nó chạy toàn bộ nội dung đang mở, tức là xoá thẳng qua mức 4.
-- Bôi đen riêng khối mình cần rồi Run.
--
-- Trước mỗi khối xoá có một câu `select` để xem trước. Chạy câu ấy trước, nhìn con số, rồi
-- mới chạy `delete`. Chạy `delete` trước rồi mới thắc mắc mình vừa xoá gì là muộn.
--
-- Hai chỗ vẫn để dạng chú thích, và không phải vì an toàn: **4b** cần thay `tho_id` thật vào
-- mới chạy được, còn **4c** xoá cả tài khoản email của chủ nên nó không thuộc việc "dọn dữ
-- liệu" nữa. Bỏ dấu `--` khi thật sự cần tới hai việc ấy.
--
-- Sổ thật của mỗi máy **không nằm ở đây** — nó nằm trong máy. Xoá sạch cả bốn mức thì máy chủ
-- và máy thợ vẫn còn nguyên sổ của mình, chỉ mất phần đã đặt lên hộp thư và mất chỗ trong
-- nhóm. Muốn dọn cả trong máy thì xem phần cuối file.
--
-- **Một ngoại lệ, và phải nhớ:** bảng `sao_luu` giữ *cả sổ* của chủ, mỗi ngày một bản. Đó là
-- bản cứu khi chủ mất máy, nên nó không nằm trong bốn mức trên — có khối riêng ở gần cuối
-- file, và khối ấy để dạng chú thích. Riêng **mức 4c xoá cả tài khoản thì bản sao lưu mất
-- theo** (khoá ngoài `on delete cascade`); đọc kỹ chỗ ấy.

-- ===========================================================================
-- XEM TRƯỚC: hiện đang có gì
-- ===========================================================================
-- Chạy riêng khối này trước tiên, cho biết mình đang đứng ở đâu.

select 'thanh_vien' as bang, count(*) as so_hang from thanh_vien
union all select 'so_cong',  count(*) from so_cong
union all select 'ma_moi',   count(*) from ma_moi
union all select 'sao_luu',  count(*) from sao_luu
union all select 'auth.users', count(*) from auth.users;

-- Bản sao lưu đang giữ tới hôm nào, của tài khoản nào, mỗi bản nặng bao nhiêu.
select u.email, sl.ngay, sl.sua_luc, pg_size_pretty(length(sl.goi::text)::bigint) as co_goi
  from sao_luu sl join auth.users u on u.id = sl.user_id
 order by sl.ngay desc
 limit 20;

-- Chi tiết từng nhóm: nhóm nào có mấy người, mấy sổ.
select tv.nhom_id,
       count(*) filter (where tv.vai = 'chu') as so_chu,
       count(*) filter (where tv.vai = 'tho') as so_tho,
       (select count(*) from so_cong sc where sc.nhom_id = tv.nhom_id) as so_so_cong
  from thanh_vien tv
 group by tv.nhom_id
 order by so_so_cong desc;

-- ===========================================================================
-- MỨC 1 — Xoá sổ đã gửi, giữ nguyên nhóm và thành viên
-- ===========================================================================
--
-- Dùng khi muốn làm sạch hộp thư để đối chiếu lại từ đầu. Không ai phải đăng nhập lại,
-- không ai phải dán mã mời lại. Lần đồng bộ tới, hai máy tự đặt sổ lên lại.
--
-- Đây là mức nên dùng trong hầu hết trường hợp.

-- xem trước
select nhom_id, tho_id, nguon, tu_ngay, den_ngay,
       jsonb_array_length(dongs) as so_dong, tao_luc
  from so_cong
 order by nhom_id, tho_id, nguon;

-- xoá cả bảng
delete from so_cong;

-- hoặc chỉ một nhóm — thay uuid vào
-- delete from so_cong where nhom_id = '00000000-0000-0000-0000-000000000000';

-- hoặc chỉ sổ của một thợ, cả hai chiều
-- delete from so_cong where tho_id = 'CC-mf3k2a-9xq1';

-- ===========================================================================
-- MỨC 2 — Dọn mã mời
-- ===========================================================================
--
-- Mã mời là mật khẩu thật của hệ thống này: ai đọc được một mã còn hạn là vào được nhóm với
-- đúng tho_id ghi trong mã. Nên dọn mã cũ là việc vệ sinh, không phải việc dọn dẹp cho gọn.
--
-- Chạy được cả lúc đang dùng bình thường: mã đã dùng và mã hết hạn thì không ai đổi được nữa.

-- xem trước
select ma, nhom_id, tho_id, het_han, da_dung,
       het_han < now() as het_han_roi
  from ma_moi
 order by het_han desc;

-- chỉ dọn mã chết — an toàn, chạy định kỳ được
delete from ma_moi where da_dung or het_han < now();

-- huỷ sạch mọi mã, kể cả mã vừa phát mà thợ chưa dán
-- delete from ma_moi;

-- ===========================================================================
-- MỨC 3 — Giải tán nhóm: xoá sổ, mã mời, và thành viên
-- ===========================================================================
--
-- Sau khi chạy: mọi máy vẫn đăng nhập được (tài khoản còn), nhưng không máy nào ở trong nhóm
-- nào nữa. Màn hình sẽ hiện "Đã đăng nhập, chưa vào nhóm".
--
-- Máy chủ bấm nối lại là có nhóm mới — **nhom_id mới**, không phải nhóm cũ. Nghĩa là mọi thợ
-- phải dán mã mời mới. Đừng chạy mức này chỉ để dọn sổ; mức 1 làm việc đó rồi.
--
-- Thứ tự xoá không quan trọng vì ba bảng không có khoá ngoài nào ràng buộc nhau — nhưng vẫn
-- xoá sổ trước cho khỏi để lại hàng mồ côi giữa hai lệnh.

-- xem trước: sẽ mất bao nhiêu dòng thành viên
select vai, count(*) from thanh_vien group by vai;

delete from so_cong;
delete from ma_moi;
delete from thanh_vien;

-- hoặc chỉ giải tán một nhóm
-- delete from so_cong    where nhom_id = '00000000-0000-0000-0000-000000000000';
-- delete from ma_moi     where nhom_id = '00000000-0000-0000-0000-000000000000';
-- delete from thanh_vien where nhom_id = '00000000-0000-0000-0000-000000000000';

-- ===========================================================================
-- MỨC 4 — Xoá cả tài khoản
-- ===========================================================================
--
-- `thanh_vien.user_id` có khoá ngoài `on delete cascade`, nên xoá tài khoản là dòng thành
-- viên của họ mất theo. Nhưng **hàng so_cong thì không mất** — không có khoá ngoài nào trỏ
-- vào đó. Vì vậy chạy mức 1 trước, rồi mới chạy mức này, nếu không sẽ để lại sổ mồ côi mà
-- không còn ai nhận.
--
-- Máy nào bị xoá tài khoản sẽ thấy app đòi đăng nhập lại. Máy thợ đăng nhập ẩn danh thì bấm
-- nối là có tài khoản mới ngay, nhưng là **người mới** — phải dán mã mời lại.

-- 4a. Dọn rác: tài khoản ẩn danh tạo lâu rồi mà chưa hề vào nhóm nào.
--
-- Đây là việc nên chạy định kỳ, không phải việc xoá dữ liệu. Ai có khoá công khai trong app
-- cũng gọi được signInAnonymously, nên có thể bơm hàng nghìn tài khoản rỗng vào project của
-- mình. Thợ thật thì nối rồi dán mã trong vài phút, nên tài khoản ẩn danh sống quá 7 ngày mà
-- chưa vào nhóm nào gần như chắc chắn là rác.

-- xem trước
select count(*) as se_xoa
  from auth.users u
 where u.is_anonymous
   and u.created_at < now() - interval '7 days'
   and not exists (select 1 from thanh_vien t where t.user_id = u.id);

delete from auth.users u
 where u.is_anonymous
   and u.created_at < now() - interval '7 days'
   and not exists (select 1 from thanh_vien t where t.user_id = u.id);

-- 4b. Loại một thợ cụ thể ra khỏi nhóm.
--
-- Hiện app **không có** đường nào làm việc này — thiet-lap.sql không có policy xoá nào trên
-- thanh_vien và không có hàm thu hồi. Nên thợ nghỉ việc, hay thợ mất điện thoại, thì phải xoá
-- tay ở đây. Thay tho_id thật vào.

-- xem trước: tài khoản nào đang mang tho_id ấy
-- select user_id, nhom_id, vai, tho_id, tao_luc
--   from thanh_vien where tho_id = 'CC-mf3k2a-9xq1';

-- xoá tài khoản → dòng thanh_vien mất theo (cascade)
-- delete from auth.users
--  where id in (select user_id from thanh_vien where tho_id = 'CC-mf3k2a-9xq1');

-- rồi dọn sổ của thợ ấy, cả hai chiều
-- delete from so_cong where tho_id = 'CC-mf3k2a-9xq1';

-- 4c. Xoá sạch mọi tài khoản — về đúng trạng thái project mới dựng.
--
-- Kể cả tài khoản email của chủ. Chủ sẽ phải tạo lại tài khoản, và nhóm mới sẽ mang nhom_id
-- mới. Chỉ chạy khi đang dựng thử, tuyệt đối không chạy trên project đang dùng thật.
--
-- **Và nó xoá luôn mọi bản sao lưu sổ của chủ**, vì `sao_luu.user_id` có `on delete cascade`.
-- Đó là bản cứu khi mất máy. Máy của chủ còn trong tay thì không mất gì thật, nhưng nếu đang
-- chạy khối này *vì* máy kia đã mất thì đây chính là chỗ mất sổ. Lấy một bản ra trước:
--
--   select goi from sao_luu order by ngay desc limit 1;
--
-- rồi bấm nút chép trong SQL Editor, dán vào một file `.json`. Đúng khuôn file sao lưu của
-- app, nên *Khôi phục từ file* trong app đọc được.

-- delete from so_cong;
-- delete from ma_moi;
-- delete from auth.users;   -- cascade xoá sạch thanh_vien

-- ===========================================================================
-- BẢN SAO LƯU SỔ CỦA CHỦ — cả khối để dạng chú thích, và không phải vì lịch sự
-- ===========================================================================
--
-- Mỗi hàng ở đây là **cả một sổ chấm công**, có mốc lương, ứng tiền, kỳ đã chốt. Ba bảng trên
-- kia xoá đi thì hai máy tự đặt sổ lên lại ở lần đồng bộ tới; hàng ở đây xoá đi thì không có
-- gì đặt lại, trừ khi cái máy đã ghi nó vẫn còn trong tay chủ.
--
-- App tự dọn cho chỉ còn 30 ngày gần nhất nên bảng này không phình ra. Chỉ có hai lý do thật
-- để chạy tay ở đây: dọn bản của một tài khoản dựng thử, hoặc chủ muốn xoá hẳn sổ khỏi
-- Supabase. Bỏ dấu `--` khi thật sự cần.

-- xem trước, và **nhìn cột email**: xoá của đúng tài khoản mình định xoá
-- select u.email, sl.ngay, sl.sua_luc from sao_luu sl join auth.users u on u.id = sl.user_id
--  order by u.email, sl.ngay desc;

-- chỉ dọn bản cũ hơn 30 ngày (app vẫn tự làm việc này — đây là để dọn sau khi sửa tay)
-- delete from sao_luu where ngay < current_date - 30;

-- xoá mọi bản của một tài khoản — thay email vào
-- delete from sao_luu
--  where user_id in (select id from auth.users where email = 'chu@cuahang.vn');

-- ===========================================================================
-- KIỂM LẠI SAU KHI XOÁ
-- ===========================================================================
--
-- Bốn bảng, các policy và bốn hàm vẫn còn nguyên sau mọi mức trên — `delete` chỉ xoá dòng,
-- không xoá cấu trúc. Câu này để chắc chắn RLS vẫn bật: xoá dữ liệu xong mà lỡ tay tắt RLS thì
-- bảng rỗng ấy sẽ công khai với cả internet ngay khi có dòng đầu tiên.

select relname as bang, relrowsecurity as rls_bat
  from pg_class
 where relname in ('thanh_vien', 'so_cong', 'ma_moi', 'sao_luu')
 order by relname;

-- Cả bốn phải là `true`. Nếu có `false` thì chạy lại thiet-lap.sql. Riêng `sao_luu` mà `false`
-- thì nghĩa là sổ có tiền của chủ đang công khai với cả internet.

-- ===========================================================================
-- DỌN TRONG MÁY (không phải SQL)
-- ===========================================================================
--
-- Xoá trên Supabase không chạm được vào máy: phiên đăng nhập nằm trong Keychain/Keystore, sổ
-- thật và bản chụp bên kia nằm trong bộ nhớ app. Máy vẫn tưởng mình đang ở trong nhóm cho tới
-- lần đồng bộ kế tiếp.
--
--   Chỉ rời nhóm, giữ sổ  — bấm **Ngắt** trong app. Sổ đã chấm vẫn còn trong máy.
--   Xoá sạch mọi thứ      — xoá app rồi cài lại. Mất cả sổ thật, nên sao lưu trước.
--
-- Máy cũ của chủ chuyền tay cho thợ thì lúc nhận mã mời có nút **xoá sổ của người khác** —
-- dùng nút ấy, đừng chỉ ngắt: cái gì không có trên máy thì không ai xem lén được.
