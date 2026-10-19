# Phân trang và bố cục danh sách

- Doctors, Reviews, Specialties, Facilities dùng chung bộ nút phân trang theo
  Admin/Users: Trang trước, số trang, Trang sau; trang hiện tại màu tím, nút biên
  bị vô hiệu hóa. Giữ bộ lọc và callback theo lifecycle AJAX. Reviews quay về
  trang còn hợp lệ khi thao tác làm giảm tổng số trang.
- “Đang hiển thị”/“Đã ẩn” là chữ màu trên một dòng, không nền hay viền.
- Trang chủ lấy tối đa 6 bác sĩ có đánh giá; xếp sao giảm dần, số đánh giá rồi ID
  để ổn định thứ tự. Bác sĩ chưa có đánh giá không đứng trước bác sĩ đã có sao.
  Carousel hiển thị 3 thẻ desktop, 2 thẻ tablet, 1 thẻ điện thoại; có nút lướt,
  hỗ trợ cuộn chạm và reduced motion. Không lồng button bên trong link thẻ.
- Portal/Doctors giãn đủ chiều ngang, hai cột ở tablet 641–1024px, giữ thẻ gọn
  trên điện thoại. Cố định 6 bác sĩ/trang và bỏ bộ chọn số lượng. Tổng số kết quả
  trên mobile lấy tổng API thay vì số thẻ của trang hiện tại. Bộ lọc đóng được
  ẩn khỏi cả hình ảnh và điều hướng bàn phím.
- Header tô màu và đánh dấu aria-current cho mục hiện tại. Drawer dùng tới
  1024px; phần tài khoản dropdown không lặp lại avatar/tên/email trong drawer.

Validation: dotnet build, npm run build:assets và regression Playwright trong
`tests/AuditRegression/pagination-browser.cjs`; dữ liệu test cô lập. Không thay
đổi cấu hình HTTP/HTTPS hoặc triển khai production.
