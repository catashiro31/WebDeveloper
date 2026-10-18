# Rà soát frontend 19/10/2026

Dựa trên master `a775503`, giữ các cập nhật giao diện mới có sẵn trên nhánh.

## Các lỗi xác nhận và sửa

- Menu tablet/điện thoại có hai listener cùng toggle, nên mở rồi đóng ngay.
  Chỉ để workspace-navigation quản lý menu, nút đóng và Escape.
- CSS mobile ép mọi modal `display:flex !important`, làm modal chuyên khoa tự
  hiện và chặn thao tác ngay khi vào trang. Bỏ ép display, giữ bố cục bottom sheet.
- Các trang mới gọi `escapeHtml` chưa khai báo; lỗi nằm trong promise catch nên
  dễ bị hiểu nhầm là lỗi tải dữ liệu. Bổ sung helper dùng chung.
- Tên/bình luận chứa dấu nháy làm hỏng JavaScript inline ở phê duyệt bác sĩ,
  ẩn đánh giá, khóa tài khoản, xóa hồ sơ và cơ sở y tế. Dùng data attributes cho
  tham số văn bản/JSON, mã hóa văn bản trước khi dựng HTML.
- Handler phân trang mới gán trực tiếp vào window bị giữ lại sau khi đổi trang
  AJAX. Đăng ký qua lifecycle của trang để dọn khi rời trang.
- Một số request bỏ qua HTTP status và hiển thị trạng thái rỗng khi API lỗi.
  Kiểm tra status trước khi đọc dữ liệu. Danh sách bác sĩ dừng loading, hiện lỗi
  và nút thử lại. Badge quá hạn xử lý lỗi mạng và ẩn khi số lượng về 0.
- Đổi bộ lọc nhanh có thể để phản hồi cũ ghi đè kết quả lịch sử mới; thêm phiên
  bản request. Áp dụng cùng điều kiện cho lỗi request cũ ở danh sách cơ sở y tế.
- Fallback ảnh bác sĩ có JavaScript escape sai. Dùng phần tử chữ viết tắt sẵn có,
  chỉ chuyển trạng thái ảnh/fallback khi lỗi tải.
- Toastify phụ thuộc CDN nên lỗi mạng ngoài có thể làm gián đoạn thao tác lưu.
  Phục vụ bản 1.12.0 nội bộ, kèm giấy phép; dùng cả ở đăng nhập và đăng ký.
- Form đăng nhập/quên mật khẩu thiếu bố cục một cột cho màn hình nhỏ. Thêm CSS
  dùng chung cho các trang xác thực độc lập; sửa nội dung quên mật khẩu đúng với
  chức năng gửi liên kết khôi phục.

## Kiểm tra

Các runner và hướng dẫn tại `tests/AuditRegression/README.md`. Bộ quét trang
bao gồm 23 trang theo vai trò và 13 trang công khai/xác thực, với desktop/tablet/
điện thoại. Regression riêng kiểm tra tương tác và phản hồi API giả lập, không
chỉ kiểm tra khả năng tải trang. Backend runner dùng PostgreSQL và Redis local.

Phạm vi: mã nguồn và ứng dụng local với dữ liệu cô lập. Không triển khai production,
không thay đổi HTTP/HTTPS. Kết quả kiểm thử không khẳng định mọi tổ hợp dữ liệu,
trình duyệt và môi trường production đều đã được bao phủ.
