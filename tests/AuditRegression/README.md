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

## Rà soát frontend ngày 19/10/2026

```sh
npm ci
npm run build:assets
AUDIT_BASE_URL=http://127.0.0.1:5012 node tests/AuditRegression/frontend-browser.cjs
AUDIT_BASE_URL=http://127.0.0.1:5012 AUDIT_WIDTHS=360,390,768,820,1024,1440 node tests/AuditRegression/ux-browser.cjs
```

`frontend-browser.cjs` kiểm tra menu cả ba vai trò, modal ẩn trên điện thoại,
dọn handler sau AJAX, thông báo không phụ thuộc CDN, ký tự nháy/xuống dòng trong
modal, phản hồi API lỗi, badge quá hạn về 0, phản hồi lọc đến sai thứ tự, ảnh hỏng
và độ rộng form xác thực. Các phản hồi bất thường được mock trong browser context;
không sửa dữ liệu thật. Dùng các biến Chromium/Playwright như phần trên.

`build:assets` biên dịch utility CSS và sao chép Toastify cùng giấy phép từ phiên
bản npm cố định. Asset đầu ra được commit, nên máy chạy .NET không cần Node.

## Phân trang, danh sách chuyên gia và header

Sau khi tạo database AuditRegression cô lập, nạp `featured-fixtures.sql` vào
**database test đó** (không dùng production). Script thêm 8 bác sĩ mẫu, gồm một
bác sĩ chưa được đánh giá để kiểm tra thứ tự xếp hạng. Sau đó khởi động ứng dụng
với database này và chạy:

```sh
AUDIT_BASE_URL=http://127.0.0.1:5013 node tests/AuditRegression/pagination-browser.cjs
```

Kiểm tra chuyển trang đầu/cuối/quay lại ở bốn trang Admin sau điều hướng AJAX,
trạng thái đánh giá không đóng khung, hai cột bác sĩ ở 768/820/1024px, bỏ bộ chọn
số bác sĩ/trang, menu cả ba vai trò không lặp thông tin, header có mục đang chọn,
bộ lọc đóng/mở, tối đa 6 chuyên gia có đánh giá và carousel hiện 3/2/1 thẻ.
