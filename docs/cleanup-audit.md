# Rà soát dead code và code lặp — 19/10/2026

Đối chiếu trên `42522f3`; giữ các thay đổi phân trang và giao diện của commit này.

| Nhận định | Kết quả và xử lý |
| --- | --- |
| AppointmentCleanupService không chạy | Đúng. Xóa class và dòng đăng ký đã comment; giữ Hangfire AppointmentJobService và lịch recurring. |
| Privacy view, scoped CSS template, validation partial | Không có action/tham chiếu runtime. Xóa. |
| Bootstrap, jQuery và plugin validation | Chỉ validation partial mồ côi tham chiếu; xóa các asset này, giữ Toastify và giấy phép. |
| site.js, site.css | JS chỉ là comment. CSS vẫn được AdminLayout/ForgotPassword nạp, nhưng chứa scaffold không còn cần thiết; bỏ tham chiếu rồi xóa, loại margin-bottom 60px. |
| WorkspaceControls mồ côi | Đúng. Dùng lại partial cho overlay/loading/error ở ba layout; tách WorkspaceMenu để hamburger vẫn nằm trong header. |
| Portal/Booking thiếu view | Đúng. Xóa action không được gọi; URL cũ trả 404, luồng đặt lịch vẫn ở DoctorDetail. |
| Styles/utilities.css thừa | Không đúng: đây là đầu vào `npm run build:css`. Giữ và build lại CSS đầu ra. Không sửa tay CSS sinh tự động. |
| Ba ChangePassword và ba Profile/Personal lặp | Đúng. Dùng chung markup và script partial; giữ DocBookingPage.mount để chạy và dọn handler khi chuyển trang AJAX. |
| Personal thiếu userStr | Đang phụ thuộc biến toàn cục của DoctorLayout, chưa chắc phát sinh ReferenceError ngay. Form dùng chung khai báo token/userStr cục bộ; lưu hồ sơ giữ verificationStatus và cập nhật tên/avatar đúng vai trò. |
| Modal CSS lặp | Đúng. Gom vào modals.css; giữ kích thước riêng, hỗ trợ cả class active và legacy display toggle; responsive tiếp tục ở responsive.css. |
| Toastify gọi lặp | Đúng. Các view gọi showApiToast, dùng fallback khi thư viện không tải được. |
| Login/Register import Toastify hai lần | Không đúng: Layout=null, cần asset riêng. Giữ một bộ import trên mỗi trang và dùng helper chung. |
| GetCurrentUser luôn gây hai truy vấn user | Không đúng: FindAsync dùng entity đã được JwtMiddleware nạp trong DbContext scoped. Regression đếm lệnh SQL xác nhận middleware/helper/service/layout dùng một truy vấn. Truy vấn DoctorDetails là dữ liệu khác và vẫn cần cho nghiệp vụ. Giữ kiểm tra tài khoản hiện hành. |
| _AuthState đọc user thừa | Đúng: AsNoTracking bỏ qua entity middleware đã nạp. Chuyển sang FindAsync dùng lại entity; chỉ select trạng thái duyệt bác sĩ thay vì cả bản ghi. Vẫn đọc trạng thái mới trên mỗi request. |
| EmailService đăng ký hai lần | Hai service type trước đây có thể tạo hai instance. Dùng một concrete registration và alias IEmailService tới cùng instance, giữ DeliverOutbox của worker. |
| AddControllers lặp | Gom AddJsonOptions vào AddControllersWithViews, giữ enum converter và IgnoreCycles. |
| AppointmentJobService inject email không dùng | Xóa field/constructor argument; cập nhật test. Không thay đổi hành vi gửi mail hiện có của job. |
| API danh mục lặp | Có trùng lọc active/projection, nhưng admin có tìm kiếm, sắp xếp và canDelete riêng. Gom CatalogQueries dùng chung; giữ shape, phân quyền và thứ tự từng API. Không thay public API bằng endpoint admin. |

Sửa thêm dấu `>` thiếu trên logo AdminLayout khi gom các partial.
Không thay cấu hình HTTPS hoặc triển khai lên VPS.

## Kiểm tra

- `npm run build:assets`, `dotnet build WebDeveloper.slnx --no-restore`.
- AuditRegression dùng PostgreSQL/Redis cô lập; thêm kiểm tra đếm SQL (39 assertion).
- `ux-browser.cjs`: role/public/auth routes, AJAX và các viewport 1440/820/390.
- `frontend-browser.cjs`: popup, ký tự đặc biệt, menu, lỗi API, handler và avatar.
- `interaction-browser.cjs`: lịch đặt tháng, thông báo click/tap, form sau AJAX, retry, Back/Forward và phiên hết hạn.
- `cleanup-browser.cjs`: lưu hồ sơ và đổi mật khẩu thực cho ba vai trò, không submit lặp sau AJAX, giữ trạng thái duyệt, reload dữ liệu mới; catalog/filter và Booking trả 404.

Các cảnh báo build NuGet Pkcs, API Hangfire cũ và nullable đã có từ trước, không được che hoặc tắt trong thay đổi này.
