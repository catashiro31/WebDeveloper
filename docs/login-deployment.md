# Đăng nhập trên HTTP và HTTPS

## Triệu chứng và nguyên nhân

Trong Production, cookie `accessToken` và `refreshToken` mặc định có cờ `Secure`.
Trình duyệt không lưu các cookie này khi truy cập HTTP qua IP/tên miền, kể cả khi
API đăng nhập trả 200. Vì vậy cả ADMIN, PATIENT và DOCTOR đều mất phiên ở request
tiếp theo. Localhost có ngoại lệ của trình duyệt nên kiểm thử localhost có thể không
phát hiện lỗi này.

ADMIN/DOCTOR mở trang được bảo vệ sẽ bị từ chối. PATIENT được chuyển về `/` theo
thiết kế, nhưng phải thấy thông tin tài khoản khi có phiên hợp lệ. Lỗi này không
phải do frontend gọi sai URL: frontend và API hiện dùng cùng origin.

## Cách triển khai

**Khuyến nghị:** cấu hình HTTPS tại reverse proxy, truy cập web bằng HTTPS và giữ
`Authentication__AllowInsecureCookies=false` (mặc định). Redirect HTTP sang HTTPS
tại proxy. Cookie vẫn giữ `Secure` kể cả khi kết nối nội bộ proxy → ứng dụng là HTTP.

Nếu cần chạy tạm HTTP qua IP, bản sửa hỗ trợ lựa chọn tường minh. Với Docker Compose,
thêm vào `.env` trên máy triển khai:

```dotenv
AUTH_ALLOW_INSECURE_COOKIES=true
```

Sau khi lấy mã mới, build và tạo lại riêng container web:

```sh
docker compose up -d --build --no-deps webapi
```

Nếu chạy bằng dịch vụ/dotnet, đặt biến môi trường
`Authentication__AllowInsecureCookies=true` trong dịch vụ rồi khởi động lại.
Lựa chọn này áp dụng đồng nhất cho đăng nhập, refresh chủ động và refresh tự động.
HTTP truyền mật khẩu và cookie không mã hóa, nên chỉ dùng tạm; khi bật HTTPS,
đặt lại thành `false`, tạo lại container/dịch vụ và đăng nhập lại.

## Kiểm tra trên trình duyệt

1. Mở DevTools → Network, đăng nhập; `POST /api/v1/auth/signin` phải trả 200.
2. `GET /api/v1/user/profile` ngay sau đó phải trả 200. Nếu cookie bị chặn, frontend
   giữ nguyên trang đăng nhập và hiển thị lỗi thay vì báo thành công rồi chuyển trang.
3. ADMIN vào `/Admin/Dashboard`; PATIENT về `/` vẫn hiện tài khoản; DOCTOR đã duyệt
   vào `/Doctor/Appointments`, trạng thái còn lại vào `/Doctor/Profile`.
4. Reload trang vẫn giữ phiên. Request trang MVC chưa đăng nhập chuyển tới trang
   đăng nhập; API chưa đăng nhập vẫn trả 401 JSON.

Các thay đổi mã nguồn/cấu hình chỉ có hiệu lực trên VPS sau khi triển khai lại.

## Kết quả kiểm thử bản sửa

Đã build thành công và kiểm thử Chromium với hostname ánh xạ về môi trường thử
(không dùng ngoại lệ cookie của localhost), chạy ASP.NET Core Production:

- HTTP mặc định: cả 6 trường hợp giữ ở form với thông báo cookie không được gửi.
- HTTP bật lựa chọn tạm thời: cả 6 trường hợp đăng nhập, chuyển trang đúng, reload,
  refresh tự động khi mất access cookie và refresh chủ động đều thành công.
- HTTPS mặc định: cùng 6 trường hợp thành công, cookie vẫn có `Secure` và `HttpOnly`.
  HTTPS thử nghiệm dùng chứng chỉ tự ký cục bộ.
- Sáu trường hợp gồm ADMIN, PATIENT và DOCTOR chưa có hồ sơ/chờ duyệt/bị từ chối/đã duyệt.
- API chưa đăng nhập trả 401; trang quản trị chưa đăng nhập chuyển về form (302).

Đây là kiểm thử trong môi trường phát triển cô lập, chưa phải xác nhận bản sửa đã
được triển khai trên VPS. Tài khoản và chứng chỉ thử đã được dọn sau kiểm thử.
