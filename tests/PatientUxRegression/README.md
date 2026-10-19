# Hồ sơ, đăng ký và thông báo tiếng Việt

Không cần database, Redis, SMTP hay tài khoản thật.

```sh
dotnet run --project tests/PatientUxRegression
node tests/PatientUxRegression/browser.cjs
```

Cần .NET 10; bài kiểm thử trình duyệt cần Playwright và Chromium (`CHROMIUM_EXECUTABLE`, mặc định `/usr/bin/chromium`).

Bài C# kiểm tra danh sách quan hệ, bảo vệ SELF, giữ nhãn cũ khi sửa hồ sơ và thông báo lỗi API tiếng Việt. Bài Chromium dùng markup/script của trang đăng ký và đặt lịch với API giả lập: kiểm tra email và vai trò gửi đi, lỗi mạng, thông báo nhập liệu khi trình duyệt dùng tiếng Anh, đọc lỗi API, mặc định bản thân, chọn và đặt cho người thân, chặn gửi khi chưa chọn hồ sơ.

Đây là kiểm thử hành vi frontend và helper; không thay thế kiểm thử tích hợp PostgreSQL, gửi email xác thực hay tải hồ sơ bác sĩ lên Cloudinary.
