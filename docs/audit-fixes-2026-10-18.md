# Đối chiếu lỗi đặt lịch và quản trị

Phạm vi: mã nguồn hiện tại và ứng dụng chạy trên PostgreSQL/Redis cô lập.
Chưa triển khai các thay đổi lên `163.61.182.126`. Theo yêu cầu, giữ cấu hình HTTP
hiện tại và hoãn HTTPS; không thay đổi cookie hay cấu hình TLS ở đợt này.

| Mục | Kết quả kiểm tra và xử lý |
| --- | --- |
| Site HTTP | Hoãn theo yêu cầu. Cấu hình triển khai không đổi. |
| Khóa tài khoản và lịch hẹn | Đã có hủy lịch active nhưng thiếu cập nhật khung giờ BOOKED của bác sĩ và thông tin người/lý do hủy. Đã sửa: đóng khung giờ liên quan khi khóa bác sĩ; bệnh nhân bị khóa chỉ trả khung giờ tương lai, đóng giờ đã qua; giữ lịch sử đã hoàn thành; thông báo qua cơ chế lưu dữ liệu hiện có. Đặt lịch kiểm tra lại tài khoản bác sĩ và phối hợp khóa database với thao tác block. |
| Trùng lịch | Đã chặn cùng hồ sơ/cùng khung giờ nhưng có race giữa hai khung giờ/bác sĩ và chưa chặn cùng bác sĩ/ngày. Đã thêm transaction/advisory lock theo tài khoản, kiểm tra cùng bác sĩ/ngày và khóa Redis có token sở hữu. Lịch đã hủy không chặn đặt lại. |
| Nút Kết quả | Lỗi còn tồn tại ở lịch sử bệnh nhân. Đã chỉ hiện khi `hasResult=true`; nếu chưa có thì hiển thị “Chưa có kết quả”. |
| Báo Không đến trước giờ | Backend đã chặn theo giờ Việt Nam. Kiểm thử xác nhận từ chối lịch tương lai. Hoàn thành và lưu kết quả trước giờ cũng đã bị chặn. |
| Lịch quá hạn không tự hủy | Đã có delayed job 30 phút nhưng thiếu sweep phục hồi lịch cũ/lỗi enqueue. Thêm recurring job mỗi phút, kiểm tra lại trạng thái dưới row lock; hủy lịch PENDING sau 30 phút hoặc khi đến giờ khám. Trả khung giờ chưa đến, đóng khung giờ đã qua; không hủy lịch CONFIRMED. Xác nhận lịch hết hạn bị từ chối để không đua với cleanup. |
| Hồ sơ bác sĩ chờ duyệt nhận lịch | Backend đặt lịch đã chặn. Bổ sung chặn xác nhận lịch có sẵn khi bác sĩ chưa được duyệt. |
| Tab admin Tất cả trống | Không tái hiện ở bản hiện tại: API không truyền status trả đủ mọi trạng thái; Chromium chuyển All → Cancelled → All hiển thị đúng. Không sửa logic đang đúng. Nếu production vẫn lỗi cần đối chiếu bản triển khai/dữ liệu/bộ lọc đang dùng. |
| Xóa chuyên khoa đang dùng | Backend đã từ chối khi có bác sĩ tham chiếu và chỉ soft delete mục chưa dùng. Bổ sung `canDelete` để UI vô hiệu hóa nút Xóa cho mục đang dùng; backend vẫn kiểm tra độc lập. |
| Tổng quan thiếu chỉ số | Đã thêm lọc kỳ theo ngày khám, doanh thu ước tính, tỉ lệ hủy/vắng và bảng hiệu suất từng bác sĩ. Các tỉ lệ chia cho toàn bộ lịch có ngày khám trong kỳ; kỳ rỗng trả 0. |

## Giới hạn doanh thu

Chưa có bảng thanh toán hoặc giá chụp tại thời điểm đặt lịch. Chỉ số hiển thị rõ
“Doanh thu ước tính”: tổng giá khám **hiện tại** của bác sĩ cho các ca COMPLETED
trong kỳ. Không coi là tiền đã thu; thay đổi giá bác sĩ có thể thay đổi số ước tính
của kỳ cũ. Hiệu suất = số hoàn thành / tổng lịch trong kỳ, kèm số hủy và vắng.

## Xác minh

- Build .NET thành công.
- `AuditRegression`: 33 assertion đạt với PostgreSQL và Redis thật, bao gồm request
  đặt lịch đồng thời, thời hạn xác nhận, sweep chạy lại, khóa tài khoản, soft delete,
  lọc trạng thái, cờ kết quả và tính số liệu theo kỳ.
- Chromium: admin All/Cancelled/All, lọc ngày dashboard (có dữ liệu/kỳ rỗng), bảng
  hiệu suất, nút xóa chuyên khoa và nút/xem kết quả đều đạt; không có lỗi JavaScript.
- `NotificationRegression`, `ModerationRegression` (cả PostgreSQL), và
  `AccountRecoveryRegression` đạt sau thay đổi.
- Ba suite cũ Backend/Workflow/Catalog đã có kỳ vọng lệch trước đợt này (phiếu trùng,
  mật khẩu mẫu yếu, tìm trong bio); không sửa các nghiệp vụ ngoài phạm vi để làm
  chúng đạt. Bộ mới kiểm tra trực tiếp các mục trong báo cáo này.
- Build vẫn có cảnh báo dependency `System.Security.Cryptography.Pkcs` 10.0.0
  và các cảnh báo C# có sẵn; không đổi package trong đợt sửa nghiệp vụ này.

## Áp dụng

Triển khai lại ứng dụng từ mã đã sửa theo quy trình hiện tại. Không cần migration
schema mới. Hangfire tự đăng ký `cancel-overdue-appointments` khi khởi động, chạy
mỗi phút và xử lý cả các lịch PENDING đã quá hạn từ trước. Kiểm tra worker Hangfire
đang chạy và kết nối được PostgreSQL. Không cần bật dịch vụ cleanup cũ vốn đang
bị vô hiệu hóa trong `Program.cs`.
