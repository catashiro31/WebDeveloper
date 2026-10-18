# Regression cho các lỗi đặt lịch và quản trị

Cần .NET 10, Redis và **database PostgreSQL mới, cô lập**. Runner áp dụng migration,
tạo fixture và giữ lại dữ liệu để kiểm tra UI. Không dùng database production.
Email được thay bằng fake sender; không gọi Cloudinary. Hangfire chỉ lưu job,
không khởi động worker trong runner.

```sh
AUDIT_TEST_CONNECTION='Host=127.0.0.1;Database=audit_regression;Username=postgres' \
AUDIT_TEST_REDIS='127.0.0.1:6379' dotnet run --project tests/AuditRegression
```

Kiểm tra bác sĩ chờ duyệt/bị khóa, đặt trùng cùng giờ và cùng bác sĩ/ngày (cả request
đồng thời), cấm báo vắng/hoàn thành/nhập kết quả trước giờ, cờ có kết quả, sweep
lịch hết hạn và tính lặp lại, xóa mềm chuyên khoa, danh sách admin không lọc trạng
thái, thống kê theo kỳ và khóa tài khoản. `/tmp/audit-browser-fixtures.json` chứa
định danh fixture dùng cho kiểm tra trình duyệt; tài khoản chỉ dành cho database test.

## Kiểm tra Chromium

Sau runner backend, khởi động ứng dụng Development trên cổng 5010 với **cùng**
`AUDIT_TEST_CONNECTION` làm `ConnectionStrings__DefaultConnection` và
`EmailOutbox__Enabled=false`. Cần Playwright trong Node module resolution và
Chromium (có thể đặt `CHROMIUM_EXECUTABLE` nếu dùng browser hệ thống):

```sh
AUDIT_BASE_URL=http://127.0.0.1:5010 node tests/AuditRegression/browser.cjs
```

Kiểm tra chuyển tab admin All/Cancelled/All, số liệu kỳ có/không dữ liệu, vô hiệu
hóa xóa chuyên khoa đang dùng, nút kết quả và mở kết quả thực tế. Chỉ trỏ tới ứng
dụng local sử dụng database fixture, không trỏ tới website production.

## AJAX, responsive và tương tác

Runner backend hiện có 37 assertion, thêm giới hạn đặt lịch ngày +30/+31 và fixture
cho lịch tháng. Khởi động ứng dụng với database đó trên cổng 5011 rồi chạy:

```sh
AUDIT_BASE_URL=http://127.0.0.1:5011 node tests/AuditRegression/ux-browser.cjs
AUDIT_BASE_URL=http://127.0.0.1:5011 node tests/AuditRegression/interaction-browser.cjs
```

`CHROMIUM_EXECUTABLE` chỉ định Chromium hệ thống nếu không dùng bản do Playwright
cài đặt. `AUDIT_WIDTHS=360,768,1024` đổi các kích thước kiểm tra responsive. Bộ tương
tác kiểm tra đọc thông báo trên fixture, nên dùng database thử mới có thông báo
chưa đọc khi chạy lại. Cần module `playwright` trong môi trường kiểm thử Node.
