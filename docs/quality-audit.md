# Rà soát lỗi giao diện và biểu mẫu

Đợt rà soát này tập trung vào các lỗi đã xuất hiện khi dùng web trên điện thoại:
thông báo API hiện nguyên JSON/HTML, bộ lọc tệp không khớp backend, giới hạn tải
lên, và nội dung tràn ngang. Đã quét các view và service của ba role, kiểm tra
trực tiếp các luồng chính bằng tài khoản thử trong môi trường cục bộ.

| Nhóm | Lỗi tìm thấy | Bản sửa |
| --- | --- | --- |
| Thông báo API | Nhiều trang dùng `res.text()` và hiện nguyên ProblemDetails JSON hoặc HTML từ proxy | Hàm dùng chung chọn `detail`, `message` hoặc lỗi validation; 413 có thông báo dễ hiểu; HTML lỗi không được đưa lên giao diện. |
| Upload | Hồ sơ bác sĩ và đơn thuốc cho chọn định dạng backend không hỗ trợ; trang bác sĩ thiếu kiểm tra 5 MB | Đồng bộ danh sách JPG/PNG/PDF và chặn tệp sai trước khi gửi. Backend từ chối tệp rỗng. |
| Nộp lại hồ sơ bác sĩ | API buộc đính kèm CCCD/chứng chỉ kể cả khi backend giữ được giấy tờ cũ | Chỉ bắt buộc lần đầu; lần nộp lại có thể giữ giấy tờ cũ, vẫn có thể thay thế. |
| Điện thoại | Thanh bên rộng 280 px đẩy nội dung chính ra khỏi màn hình | Ba layout role đưa nội dung chính lên trước và xếp thanh bên bên dưới ở màn hình hẹp; modal cơ sở y tế không tràn ngang. |
| Tải nhiều tệp | Giới hạn proxy 12 MB có thể chặn request ba tệp, mỗi tệp 5 MB | Hướng dẫn đặt `client_max_body_size 20m` trên VPS. |
| Đổi tên hồ sơ | Form lấy tên mới từ DB nhưng thanh bên/header đọc claim cũ trong access token; JavaScript cập nhật nhầm phần tử header | API phát lại cookie access với tên/ảnh mới; trang cập nhật đúng tên ngay và giữ đúng sau reload. |

## Kiểm tra

- `dotnet build WebDeveloper.slnx --no-restore`: thành công; còn 8 cảnh báo cũ ở các phần không sửa.
- Kiểm tra cú pháp 24 đoạn JavaScript trong các view đã thay đổi: đạt.
- Hàm đọc phản hồi API: kiểm tra 400 ProblemDetails, lỗi validation, 413 HTML,
  500 HTML và lỗi dạng text: đạt.
- Đăng nhập và mở 2 trang admin, 2 trang bệnh nhân, 3 trang bác sĩ trên Chromium:
  không có lỗi JavaScript.
- Cùng các trang trên viewport 375 px: nội dung chính nằm trong màn hình; modal
  thêm cơ sở y tế nằm trong màn hình.
- API hồ sơ bác sĩ mới khi thiếu CCCD: trả 400 với câu giải thích rõ. Tài khoản
  kiểm thử đã được xóa.
- Sửa tên hồ sơ ở ba role: access cookie được phát lại với tên mới; header/thanh
  bên cập nhật ngay và tiếp tục đúng sau reload. Ba tài khoản thử đã được xóa.

Kiểm tra cục bộ không thay thế việc thử upload thật trên VPS: cấu hình Nginx nằm
ngoài repository, và việc lưu tệp còn phụ thuộc dịch vụ Cloudinary của môi trường
triển khai. Sau khi triển khai mã, thử lại một ảnh hợp lệ và một tệp vượt 5 MB cho
từng biểu mẫu tải tệp.
