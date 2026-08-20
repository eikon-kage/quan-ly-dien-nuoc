-- Tạo hoặc mở khoá tài khoản chủ bằng tay, không cần thư xác nhận.
--
-- Dùng khi đường thư tắc: link xác nhận hết hạn (`otp_expired`), thư không tới, hoặc SMTP mặc
-- định của Supabase đã chặn theo giờ. Chạy trong **SQL Editor**, nơi có quyền quản trị.
--
-- Mọi lệnh ở đây đụng vào lược đồ `auth` — **của Supabase, không phải của mình.** Supabase có
-- thể đổi cột trong `auth.users` giữa các bản, nên nếu một lệnh báo thiếu cột hay sai kiểu thì
-- đừng chữa bằng cách đoán: dùng Cách 1, hoặc tạo tài khoản qua dashboard.
--
-- Đường chính thức, không phụ thuộc lược đồ, là Admin API (`POST /auth/v1/admin/users` với
-- `email_confirm: true`) — nhưng nó cần `service_role` key, mà khoá ấy bỏ qua RLS và đọc xoá
-- được cả database. Đừng dán khoá ấy vào đâu ngoài máy của mình.

-- ===========================================================================
-- XEM TRƯỚC: đang có những tài khoản nào
-- ===========================================================================
-- Chạy riêng khối này trước. `email_confirmed_at` null nghĩa là chưa xác nhận — đó chính là
-- lý do đăng nhập hụt dù tài khoản đã tồn tại.

select id,
       email,
       is_anonymous,
       email_confirmed_at,
       created_at
  from auth.users
 order by created_at desc
 limit 20;

-- ===========================================================================
-- CÁCH 1 — tài khoản đã có, chỉ thiếu xác nhận  (khuyên dùng)
-- ===========================================================================
-- Ít đụng vào lược đồ nhất: chỉ ghi một cột. Sau lệnh này bấm *Đăng nhập* trong app bằng
-- đúng mật khẩu đã gõ lúc tạo tài khoản.
--
-- Bỏ dấu `--` ở dòng dưới rồi sửa email cho đúng.

-- update auth.users
--    set email_confirmed_at = coalesce(email_confirmed_at, now()),
--        updated_at = now()
--  where email = 'chu@cuahang.vn';

-- ===========================================================================
-- CÁCH 2 — quên mật khẩu, đặt lại mật khẩu cho tài khoản đã có
-- ===========================================================================
-- `crypt` với `gen_salt('bf')` là đúng cách GoTrue băm mật khẩu. Nó nằm trong extension
-- pgcrypto, Supabase để ở lược đồ `extensions`.
--
-- Đặt mật khẩu từ **6 ký tự** trở lên, kẻo app báo "Mật khẩu quá ngắn".

-- update auth.users
--    set encrypted_password = extensions.crypt('matkhaumoi123', extensions.gen_salt('bf')),
--        email_confirmed_at = coalesce(email_confirmed_at, now()),
--        updated_at = now()
--  where email = 'chu@cuahang.vn';

-- ===========================================================================
-- CÁCH 3 — tạo hẳn một tài khoản chủ mới, xác nhận sẵn
-- ===========================================================================
-- Tạo cả hàng `auth.users` và hàng `auth.identities` trong một lần. Phải có identity: thiếu
-- nó thì đăng nhập bằng mật khẩu có bản GoTrue chạy được, có bản không, mà lúc hỏng thì
-- triệu chứng chỉ là "email hoặc mật khẩu không đúng" — không lần ra được.
--
-- Sửa hai giá trị trong dòng `select` đầu rồi bỏ dấu `--` cả khối.

-- do $$
-- declare
--   v_email    text := 'chu@cuahang.vn';
--   v_mat_khau text := 'matkhau123';
--   v_id       uuid := gen_random_uuid();
-- begin
--   if exists (select 1 from auth.users where email = v_email) then
--     raise exception 'Email % đã có tài khoản. Dùng Cách 1 hoặc Cách 2.', v_email;
--   end if;
--
--   insert into auth.users (
--     instance_id, id, aud, role, email, encrypted_password,
--     email_confirmed_at, created_at, updated_at,
--     raw_app_meta_data, raw_user_meta_data, is_anonymous
--   ) values (
--     '00000000-0000-0000-0000-000000000000',
--     v_id, 'authenticated', 'authenticated', v_email,
--     extensions.crypt(v_mat_khau, extensions.gen_salt('bf')),
--     now(), now(), now(),
--     '{"provider":"email","providers":["email"]}'::jsonb,
--     '{}'::jsonb,
--     false
--   );
--
--   insert into auth.identities (
--     id, user_id, identity_data, provider, provider_id,
--     last_sign_in_at, created_at, updated_at
--   ) values (
--     gen_random_uuid(), v_id,
--     jsonb_build_object('sub', v_id::text, 'email', v_email, 'email_verified', true),
--     'email', v_email,
--     now(), now(), now()
--   );
--
--   raise notice 'Đã tạo % (id %). Vào app bấm Đăng nhập.', v_email, v_id;
-- end $$;

-- ===========================================================================
-- SAU KHI ĐĂNG NHẬP ĐƯỢC
-- ===========================================================================
-- App tự gọi `tao_nhom()` lúc đăng nhập xong, nên **không phải chạy gì thêm ở đây** để lập
-- nhóm. Kiểm lại bằng câu này: phải thấy đúng một hàng vai 'chu'.

select tv.user_id, u.email, tv.vai, tv.nhom_id, tv.tao_luc
  from thanh_vien tv
  join auth.users u on u.id = tv.user_id
 order by tv.tao_luc desc;

-- ===========================================================================
-- ĐỂ LẦN SAU KHÔNG TẮC NỮA
-- ===========================================================================
-- Hai chỗ trong dashboard, không phải SQL:
--
--   1. Authentication → Sign In / Providers → Email → **tắt Confirm email**. Nhóm sáu người
--      thì việc xác nhận thư chỉ đem lại một chỗ để tắc; mà SMTP mặc định giới hạn vài thư
--      mỗi giờ nên thử vài lần là chặn.
--
--   2. Authentication → URL Configuration → **Site URL**. Mặc định là http://localhost:3000,
--      nên link trong thư trả về một trang trắng. App này không có trang web nào, nên nếu còn
--      giữ Confirm email thì đặt Site URL thành một địa chỉ thật mà mình mở được.
--
-- Và **Allow anonymous sign-ins phải bật** — máy thợ vào nhóm bằng tài khoản ẩn danh. Chỗ đó
-- không liên quan gì tới tài khoản chủ.
