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
| Danh sách chuyên khoa/cơ sở | Database đã lưu nhưng API danh sách trả bộ đệm cũ trong 12 giờ; thư viện thông báo không tải được cũng có thể ngăn bước làm mới giao diện | Hai danh sách đọc trực tiếp bằng truy vấn chỉ lấy các cột DTO, không tracking; API và fetch dùng no-store. Thông báo có phương án hiển thị nội bộ khi CDN lỗi. |
| Lỗi tải danh sách | Cơ sở y tế có thể báo mạng lưới trống khi API trả lỗi hoặc phản hồi sai định dạng | Kiểm tra HTTP và mảng dữ liệu, hiển thị trạng thái lỗi cùng nút thử lại cho cả hai trang. |
| Danh mục ở hồ sơ bác sĩ/bộ lọc bệnh nhân | Cùng API bị bộ đệm cũ; giao diện thiếu kiểm tra HTTP và không có cách thử lại. Một danh mục lỗi có thể ngăn trang tải cả danh sách bác sĩ | Dùng hàm tải mảng dữ liệu có kiểm tra phản hồi và no-store. Hai danh mục tải độc lập; hiển thị lỗi/empty đúng và có nút thử lại. Form chỉ cho gửi khi đủ danh mục. |
| Thẻ bác sĩ công khai | Database có cơ sở và tỉnh nhưng DTO danh sách bác sĩ không trả hai trường này | Trả `facilityName` và `province` cùng chuyên khoa để thẻ hiển thị đúng. |
| Lỗi tải hồ sơ bác sĩ | API lỗi bị hiểu thành bác sĩ chưa nộp hồ sơ và mở form đăng ký | Hiển thị lỗi hồ sơ có nút thử lại; danh sách chuyển công tác cũng kiểm tra lỗi và làm mới khi mở. |

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
- Test hồi quy `tests/CatalogRegression`: trước khi sửa, cả hai bản ghi vừa tạo
  không xuất hiện sau lần đọc danh sách đầu tiên; sau khi sửa, thêm/sửa/xóa và
  xác minh cơ sở đều cập nhật ngay. File upload được thay bằng dịch vụ test,
  không gửi lên Cloudinary. Các bản ghi thử được dọn trong `finally`.
- Chromium: thêm/sửa/xóa chuyên khoa qua giao diện; cơ sở đã thêm sau lần đọc
  đầu xuất hiện ngay, sửa/xác minh/xóa qua giao diện và reload đều đúng. API 503
  hiển thị lỗi thay vì dữ liệu trống, nút thử lại hoạt động. CDN thông báo không
  tải được trong môi trường test, giao diện vẫn lưu và làm mới danh sách đúng.
- Test hồi quy bổ sung: nộp hồ sơ bác sĩ lưu đúng ID chuyên khoa/cơ sở; đọc lại
  trả đúng tên từ PostgreSQL. Hồ sơ PENDING không công khai; khi APPROVED thì
  bộ lọc cùng ID tìm được bác sĩ và thẻ trả đủ tên chuyên khoa/cơ sở.
- Chromium ở role bác sĩ và bệnh nhân: dropdown và bộ lọc có tên danh mục mới;
  API danh mục 503 hoặc trả sai dạng hiện lỗi, có thể thử lại, danh mục còn tốt
  vẫn dùng được. Lỗi bộ lọc không chặn danh sách bác sĩ. API danh sách bác sĩ
  lỗi hiện trạng thái lỗi thay vì kết quả rỗng. Lỗi API hồ sơ không mở form mới.
  Danh sách cơ sở chuyển công tác cập nhật và không chứa cơ sở hiện tại.

Kiểm tra cục bộ không thay thế việc thử upload thật trên VPS: cấu hình Nginx nằm
ngoài repository, và việc lưu tệp còn phụ thuộc dịch vụ Cloudinary của môi trường
triển khai. Sau khi triển khai mã, thử lại một ảnh hợp lệ và một tệp vượt 5 MB cho
từng biểu mẫu tải tệp.
