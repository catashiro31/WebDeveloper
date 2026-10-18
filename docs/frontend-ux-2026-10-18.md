# Điều hướng AJAX, responsive, lịch đặt khám và thông báo

## Hành vi mới

- Các liên kết cùng khu vực Admin/Doctor/Patient tải vùng nội dung bằng AJAX, giữ
  nguyên header, sidebar và trung tâm thông báo. URL vẫn cập nhật để hỗ trợ chia sẻ
  liên kết, tải lại, Back/Forward. Chuyển khu vực hoặc trang công khai vẫn dùng điều
  hướng thông thường.
- Hiển thị spinner khi lấy trang; lỗi mạng giữ nội dung hiện tại và có nút thử lại
  hoặc mở trang đầy đủ. Phiên đăng nhập hết hạn đưa về trang đăng nhập.
- Script từng trang chạy trong phạm vi riêng. Khi rời trang, hủy request, timer và
  modal đã đưa ra body; phản hồi cũ không được ghi lên trang mới. Layout trả fragment
  khi nhận `X-Workspace-Navigation: 1`; các controller vẫn giữ kiểm tra quyền như cũ.
- Tablet/điện thoại dùng menu bên có nút mở/đóng và Escape. Bộ lọc, form, card và
  modal tự xuống dòng; bảng rộng cuộn ngang trong vùng riêng.
- Lịch bệnh nhân cho chọn từ hôm nay tới ngày +30 theo giờ Việt Nam, đồng bộ giới
  hạn tạo lịch của bác sĩ. Hiển thị 5 nhóm tuần để không mất các ngày 29–30. Backend
  cũng kiểm tra giới hạn, không chỉ ẩn ngày trên giao diện.
- Bấm một lần thông báo chỉ đánh dấu đã đọc và cập nhật số chưa đọc. Bấm đúp (hoặc
  chạm hai lần nhanh) mở trang liên quan. Alt+Enter hỗ trợ mở bằng bàn phím. Chỉ mở
  URL nội bộ cùng origin; thao tác đọc có thể gọi lại mà không tạo thay đổi trùng.

## CSS cục bộ

Tailwind được build thành `WebDeveloper/wwwroot/css/utilities.css`, không còn tải
runtime Tailwind từ CDN. Tệp CSS đã được lưu cùng mã nên chạy/publish .NET không cần
Node. Khi thêm hoặc sửa utility class trong Razor/JavaScript, chạy từ thư mục gốc:

```sh
npm ci
npm run build:css
dotnet build WebDeveloper.slnx
```

Commit cả nguồn, `package-lock.json` và CSS đã sinh. Responsive dùng thêm
`responsive.css`, `role-layout.css` và script bọc bảng `responsive.js`.

Với trang role mới, đặt script trong `DocBookingPage.mount(page => { ... })`, dùng
`page.fetch`, timer của `page`, `page.expose`/`page.globals` cho handler HTML, và
`page.own` cho modal đưa ra body. Những thành phần dùng chung được giữ ở layout.

## Kiểm thử

- Build .NET thành công; build CSS cục bộ thành công.
- 37 assertion backend đạt với PostgreSQL/Redis thật, bao gồm xem/đặt ngày +30 và
  từ chối ngày +31, cùng hồi quy đặt trùng, quá hạn, khóa tài khoản và thống kê.
- 23 trang role và 8 trang công khai được kiểm tra ở 1440/820/390 px, tiếp tục kiểm
  tra ở 360/768/1024 px; các lỗi tràn ngang phát hiện đã được sửa và kiểm tra lại.
- Chuyển AJAX lặp lại không tải lại layout và không có lỗi JavaScript.
- Kiểm tra spinner, Back/Forward, lỗi 503/thử lại, request API chậm sau khi rời trang,
  phiên hết hạn, menu mobile/tablet, modal mobile và thêm/lưu/xóa chuyên khoa qua
  form sau chuyển trang AJAX đều đạt.
- Kiểm tra click/double-click và tap/double-tap thông báo đạt; lịch theo giờ Việt Nam
  vẫn đúng khi browser chạy ở múi giờ America/Los_Angeles.

Các runner: `tests/AuditRegression/ux-browser.cjs` và `interaction-browser.cjs`.
Cần chạy backend fixture trước trên database thử mới, rồi chạy app Development với
cùng database. Không trỏ các runner tới website production.

Đây là thay đổi trong kho mã và kiểm thử cục bộ. Chưa triển khai lên
`163.61.182.126`; cấu hình HTTP được giữ nguyên theo yêu cầu.
