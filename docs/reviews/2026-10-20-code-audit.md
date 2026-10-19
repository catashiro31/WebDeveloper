# Rà soát trước tính năng cập nhật chuyên môn

Mốc rà soát: commit 86486dd. Kiểm tra mã nguồn toàn bộ các tầng chính và thử các tình huống trên PostgreSQL/Redis riêng; không thử tấn công website thật, không gửi email hoặc kiểm tra khóa dịch vụ bên ngoài. Danh sách này không phải cam kết đã phát hiện mọi lỗi.

## Đã tái hiện trên môi trường thử

| Mức | Vấn đề | Vị trí và bằng chứng |
|---|---|---|
| Cao | Stored XSS ở danh sách cơ sở | Views/Portal/Facilities.cshtml đưa description/map URL vào HTML chưa escape. Mô tả thử nghiệm đã thực thi cờ JS vô hại khi mở trang. Cần escape nội dung và kiểm tra giao thức URL. |
| Cao | Bác sĩ sửa dữ liệu cơ sở dùng chung và tự xác minh | DoctorService.ChangeProfile từng nhận FacilityDescription/FacilityVerified. API cho phép ghi và dữ liệu thay đổi. **Đã chặn các trường này trong tính năng cập nhật chuyên môn hiện tại**; điểm render XSS trên vẫn cần sửa riêng. |
| Cao | Đổi mật khẩu không thu hồi phiên cũ | Sau khi đổi mật khẩu, phiên đã sao chép vẫn lấy được profile và refresh token. UserService/AuthService cần cơ chế thu hồi phiên/token. |
| Trung bình | Đánh giá đã ẩn vẫn lộ nội dung qua API công khai | PublicService.GetReviewsByDoctorId trả Comment của review IsVisible=false. Đã ẩn bằng admin, gọi ẩn danh vẫn nhận nội dung. |
| Cao | Tạo trùng ca trong cùng một request | DoctorService.CreateSchedule nhận hai mục ngày/giờ giống nhau và tạo hai ID khác nhau. Cần loại trùng trong request và ràng buộc dữ liệu phù hợp. |
| Trung bình | Enum khung giờ dạng số ngoài danh sách được chấp nhận | slotIds=["999"] được tạo, giờ fallback thành 00:00. Cần Enum.IsDefined/whitelist. |
| Trung bình | Phân trang thiếu kiểm tra | API doctors với page=-1 trả 500; size=0 tạo totalPages vô lý; size rất lớn được nhận. Cần giới hạn thống nhất. API cập nhật chuyên môn mới đã có giới hạn riêng. |
| Trung bình | Lịch cũ đổi cơ sở theo hồ sơ hiện tại | Sau chuyển công tác, API lịch đã hoàn thành trả tên cơ sở mới. **Tính năng hiện tại bổ sung snapshot cho lịch mới và backfill thông tin hiện biết cho lịch cũ**. |
| Trung bình | Chuyển đến cơ sở ngừng hoạt động | CreateTransferRequest và ApproveTransfer không kiểm tra IsActive của nơi đến; đã gửi và duyệt thành công trong fixture. |
| Trung bình | Bác sĩ bị khóa vẫn xuất hiện công khai | PublicService lọc APPROVED nhưng không lọc User.IsActive; danh sách/chi tiết vẫn hiện, UI ghi “Đang hoạt động”. Backend đặt lịch có chặn. |
| Trung bình | Đăng nhập thiếu rate limit | 12 lần sai liên tục đều trả 400, không 429; policy hiện chỉ gắn ở một số endpoint khác. Cần giới hạn và theo dõi đăng nhập thất bại. |

## Xác nhận qua mã nguồn, giới hạn kiểm chứng

- **Cao:** appsettings.json được theo dõi trong Git có giá trị không rỗng cho JWT secret, Cloudinary secret và mật khẩu SMTP. Không đưa giá trị vào báo cáo, không thử chúng với dịch vụ thật. Cần chuyển sang secret store và thay khóa đã lộ; chưa xác nhận VPS dùng các giá trị đó.
- **Trung bình:** PatientService.CancelAppointment so thời gian hẹn Việt Nam với DateTime.UtcNow.AddHours(window). Ví dụ UTC 03:00/VN 10:00, hẹn 17:00: còn 7 giờ nhưng cửa sổ 12 giờ vẫn có thể cho hủy. Chưa kiểm thử trực tiếp cửa sổ này trên dữ liệu thời điểm rà soát.
- **Trung bình:** AppointmentJobService có thông báo “gửi email” chỉ bằng Console.WriteLine. Không coi log này là email đã gửi; thông báo chuông hủy lịch là cơ chế khác.
- **Trung bình:** EmailService nuốt lỗi SMTP ở chế độ mặc định; luồng quên mật khẩu có thể trả thành công dù email thất bại.
- Docker Compose công bố cổng PostgreSQL/Redis; Redis không cấu hình mật khẩu trong cấu hình này. Mức phơi bày thực tế phụ thuộc firewall/cấu hình triển khai. Chưa quét cổng VPS.
- Build còn cảnh báo NU1902/NU1903 cho System.Security.Cryptography.Pkcs 10.0.0 cùng cảnh báo nullable/obsolete có sẵn. Chưa khẳng định khả năng khai thác trong ứng dụng.
- Dashboard admin hiện chỉ render số đếm; API có chỉ số doanh thu ước tính/tỉ lệ nhưng giao diện chưa thể hiện đầy đủ.

Các lỗi ngoài phạm vi tính năng chuyên môn chưa được sửa trong commit này. Giữ HTTP theo yêu cầu hiện tại. Kiểm thử thành công không có nghĩa các vấn đề trên đã được giải quyết.
