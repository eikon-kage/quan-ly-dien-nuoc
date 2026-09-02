/**
 * Phép đo bàn phím ảo của bản web — phần **quyết định**.
 *
 * Phải có bài cho chỗ này vì con số ra sai thì hỏng theo kiểu khó thấy nhất: hộp nhảy lên
 * quá đầu bàn phím, hoặc nhấp nhổm theo mỗi cú cuộn trang, mà cả hai đều chỉ hiện ra trên
 * điện thoại thật chứ không hiện trong Chrome trên máy tính. Ba con số đi vào phép tính này
 * (`innerHeight`, `visualViewport.height`, `visualViewport.offsetTop`) thì mỗi trình duyệt
 * lại đổi một kiểu, nên chốt luôn từng trường hợp ở đây.
 *
 * **Không có bài dựng hộp ra để xem nó lên đúng chỗ chưa** — cùng lý do như bài
 * [hopThoai.test.tsx](./hopThoai.test.tsx): `Modal` bên web dùng portal của react-dom mà
 * react-test-renderer không dựng nổi. Phần vẽ soi bằng cách mở bản dựng thật trên máy.
 */

import { caoBanPhimCua } from '../../dungCaoBanPhim.web';

test('trình duyệt không có visualViewport thì không đẩy gì', () => {
  expect(caoBanPhimCua({ innerHeight: 800, visualViewport: null })).toBe(0);
});

test('bàn phím mở: phần bị che là chỗ phải chừa ở đáy', () => {
  expect(
    caoBanPhimCua({ innerHeight: 800, visualViewport: { height: 460, offsetTop: 0 } }),
  ).toBe(340);
});

/*
  Safari trên iOS không co khung trang mà đẩy trôi phần nhìn thấy lên. Bỏ `offsetTop` ra
  khỏi phép tính là hộp bị đẩy quá tay đúng bằng khoảng trôi ấy.
*/
test('Safari trôi trang lên thì trừ cả khoảng trôi', () => {
  expect(
    caoBanPhimCua({ innerHeight: 800, visualViewport: { height: 400, offsetTop: 60 } }),
  ).toBe(340);
});

test('thanh địa chỉ thu gọn khi cuộn thì coi như bàn phím chưa mở', () => {
  expect(caoBanPhimCua({ innerHeight: 800, visualViewport: { height: 770, offsetTop: 0 } })).toBe(
    0,
  );
});

/*
  Android/Chrome co hẳn khung trang khi bàn phím mở nên hiệu ra 0 — trình duyệt đã đẩy hộ,
  app không đẩy thêm lần nữa.
*/
test('trình duyệt tự co khung trang thì app không đẩy thêm', () => {
  expect(caoBanPhimCua({ innerHeight: 460, visualViewport: { height: 460, offsetTop: 0 } })).toBe(
    0,
  );
});

test('phần nhìn thấy cao hơn khung trang thì cũng không đẩy, không ra số âm', () => {
  expect(caoBanPhimCua({ innerHeight: 700, visualViewport: { height: 740, offsetTop: 0 } })).toBe(
    0,
  );
});

test('số lẻ được làm tròn — điểm ảnh lẻ làm hộp rung', () => {
  expect(
    caoBanPhimCua({ innerHeight: 800.4, visualViewport: { height: 459.8, offsetTop: 0 } }),
  ).toBe(341);
});
