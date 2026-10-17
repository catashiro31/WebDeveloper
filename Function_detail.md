# Chi tiết chức năng và kỹ thuật xử lý DocBooking

Tài liệu mô tả code hiện tại, gồm backend ASP.NET Core 10, Entity Framework Core/PostgreSQL và giao diện Razor/JavaScript. Các đường dẫn bên dưới tính từ thư mục gốc repository. Cập nhật tài liệu khi thay đổi API, trạng thái hoặc quy tắc nghiệp vụ.

## 1. Cách đọc và cấu trúc code

```text
WebDeveloper/
  Program.cs                 Khởi tạo dependency injection và pipeline HTTP
  Controllers/               Trả trang Razor theo từng vai trò
  Controllers/Api/           API JSON: nhận DTO, xác thực và gọi nghiệp vụ
  Services/                  Nghiệp vụ theo chức năng/vai trò
  Services/Interfaces/       Mỗi interface nằm trong một file cùng tên
  Models/Entities/           Ánh xạ bảng và quan hệ PostgreSQL
  Models/DTOs/               Request/response, validation đầu vào
  Models/Enums/              Trạng thái chuẩn của tài khoản, lịch, hồ sơ
  Data/                      DbContext, index, sự kiện thông báo khi lưu
  Security/                  JWT, cookie, blacklist, kiểm tra tài khoản
  Helpers/                   Giờ Việt Nam, phân trang, phân biệt trạng thái tài khoản
  Moderation/                Naive Bayes, từ điển, dữ liệu huấn luyện và giấy phép
  BackgroundServices/       Mã tác vụ nền định kỳ; xem trạng thái bật/tắt ở mục 13
  Middleware/                Chuẩn hóa lỗi HTTP
  Views/                     Màn hình, layout và partial dùng chung
  wwwroot/                   CSS, JavaScript và tài nguyên tĩnh
  Migrations/                Lịch sử thay đổi schema EF
 tests/                      Các chương trình kiểm thử hồi quy
 docs/                       Hướng dẫn triển khai và phân tích từng lỗi
```

Luồng thông thường: **Razor/JavaScript → API controller → service → DbContext → PostgreSQL → DTO → giao diện**. `AccountAppealsController`, `NotificationsController`, `AdminCatalogController` hiện truy vấn DbContext trực tiếp; không phải mọi nghiệp vụ đều đã tách sang service.

`UserService` được tách khỏi `AuthService.cs`. Các hợp đồng trước đây trong `ICoreServices.cs` và `IDomainServices.cs` được tách thành `IAuthService.cs`, `IUserService.cs`, `IDoctorService.cs`… Namespace, chữ ký hàm và đăng ký DI giữ nguyên.

Các comment `// === ... ===` trong service đánh dấu cụm chức năng. Comment gần đoạn xử lý phức tạp giải thích lý do/điều kiện bắt buộc; tài liệu này giải thích luồng tổng thể để tránh lặp lại mô tả dài trong từng dòng code.

## 2. Bản đồ chức năng

| Chức năng | Điểm vào API | Nơi xử lý chính | Màn hình |
|---|---|---|---|
| Đăng ký, đăng nhập, đăng xuất, reset mật khẩu | `/api/v1/auth/*` | `Services/AuthService.cs`, `Security/*` | `Views/Auth/*` |
| Hồ sơ cá nhân, ảnh, đổi mật khẩu | `/api/v1/user/*` | `Services/UserService.cs` | Profile/Personal/ChangePassword theo role |
| Khóa/mở khóa tài khoản | `/api/v1/admin/users/{id}/block`, `/unblock` | `Services/AdminService.cs` | `Views/Admin/Users.cshtml` |
| Phiếu xin mở khóa | `/api/v1/auth/appeals`, `/appeals/status`, `/api/v1/admin/appeals` | `Controllers/Api/AccountAppealsController.cs` | `Views/Auth/Appeal.cshtml`, `Views/Admin/Appeals.cshtml` |
| Hồ sơ chuyên môn, lịch bác sĩ | `/api/v1/doctor/profile`, `/schedules` | `Services/DoctorService.cs` | `Views/Doctor/Profile.cshtml`, `Schedules.cshtml` |
| Hồ sơ bệnh nhân/người thân | `/api/v1/patient/relatives*` | `Services/PatientService.cs` | `Views/Patient/Relatives.cshtml` |
| Đặt/hủy/theo dõi lịch khám | `/api/v1/patient/appointments*`, `/api/v1/doctor/appointment*` | PatientService, DoctorService | Appointments theo role |
| Kết quả khám, tái khám, lịch sử | `/api/v1/doctor/appointment/{id}/result`, `/api/v1/patient/history*` | DoctorService, PatientApiController | Lịch hẹn bác sĩ, lịch sử bệnh nhân |
| Đánh giá và kiểm duyệt | `/api/v1/patient/appointments/{id}/review`, `/api/v1/admin/reviews*` | PatientService, AdminService, ReviewClassifier | Reviews theo role |
| Chuyển công tác | `/api/v1/doctor/transfer`, `/api/v1/admin/transfers*` | DoctorService | Doctor/Profile, Admin/Transfers |
| Tìm kiếm công khai | `/api/v1/portal/*` | `Services/PublicService.cs` | `Views/Portal/*`, Home |
| Danh mục và thống kê quản trị | `/api/v1/admin/*` | AdminService, AdminCatalogController | `Views/Admin/*` |
| Hộp thông báo | `/api/v1/notifications*` | NotificationsController, NotificationEvents | `_Notifications.cshtml`, `notifications.js` |

## 3. Dữ liệu và phân quyền

- `User`: tài khoản, email, mật khẩu băm, role, trạng thái hoạt động và lý do khóa.
- `PatientProfile`: người được khám, có thể là bản thân hoặc người thân của một `User`.
- `DoctorDetail`: hồ sơ chuyên môn liên kết tài khoản, chuyên khoa và cơ sở y tế.
- `DoctorSchedule`: ngày/ca làm việc, cơ sở tại ca đó, trạng thái và trường `Version` kiểm soát cập nhật đồng thời.
- `Appointment`: liên kết **PatientProfile** với **DoctorSchedule**, trạng thái, lý do khám/hủy và người hủy.
- `MedicalResult`, `Review`: gắn với lịch khám.
- `DoctorTransferRequest`: đích chuyển công tác, tên cơ sở gốc chụp tại lúc gửi, trạng thái và ngày xử lý.
- `AccountAppeal`, `Notification`, `TokenBlacklist`: phiếu mở khóa, hộp thông báo, token đã thu hồi.

**PatientId không phải UserId.** Kiểm tra quyền của một lịch bệnh nhân phải đi qua `Appointment.Patient.UserId`. Thông báo gửi cho tài khoản sở hữu hồ sơ; cảnh báo vắng khám cộng các hồ sơ thuộc cùng tài khoản.

`[Authorize(Roles = "ADMIN"/"DOCTOR"/"PATIENT")]` giới hạn nhóm API. Service tiếp tục kiểm tra quyền sở hữu dữ liệu, vì đúng role chưa đủ để sửa lịch của người khác. ID người dùng hiện tại lấy từ claim, không tin ID do frontend tự gửi.

### Các trạng thái độc lập

| Đối tượng | Trạng thái | Ý nghĩa |
|---|---|---|
| Tài khoản | `IsActive`, `ReasonBanned` | Quyền truy cập; không thay thế trạng thái hồ sơ |
| Hồ sơ bác sĩ | `PENDING`, `APPROVED`, `REJECTED` | Được phép nhận lịch sau khi duyệt |
| Ca làm việc | `AVAILABLE`, `BOOKED`, `CLOSED` | Trống, đã đặt, đóng |
| Lịch khám | `PENDING`, `CONFIRMED`, `COMPLETED`, `CANCELLED`, `NO_SHOW` | Chờ, xác nhận, hoàn thành, hủy, vắng |
| Chuyển công tác/phiếu mở khóa | `PENDING`, `APPROVED`, `REJECTED` | Vòng đời yêu cầu |

## 4. Đăng ký, phiên đăng nhập và mật khẩu

**Mã nguồn:** `AuthService`, `AuthApiController`, `AccessTokenProvider`, `AuthCookieSettings`, `JwtMiddleware`, `AccountState`, `wwwroot/js/auth-logout.js`.

1. Đăng ký băm mật khẩu bằng BCrypt, tạo tài khoản chưa kích hoạt và ghi email xác thực vào EmailOutbox cùng transaction. Worker gửi SMTP sau, request đăng ký không chờ SMTP.
2. Xác thực kiểm tra mã và hạn sử dụng; kích hoạt tài khoản, xóa mã, tạo hồ sơ bản thân cho bệnh nhân. Tài khoản có lý do bị khóa không được tự mở khóa bằng link xác thực.
3. Đăng nhập kiểm tra mật khẩu trước trạng thái tài khoản, cấp JWT access token và refresh token. JWT chứa ID, role và thông tin định danh.
4. Controller ghi cookie HttpOnly, SameSite Strict. Access cookie sống 15 phút, refresh cookie 7 ngày. `AuthCookieSettings` bật Secure theo môi trường/cấu hình; HTTP triển khai cần cấu hình rõ ràng, xem `docs/login-deployment.md`.
5. Khi access cookie hết hạn, `Program.cs` có luồng phục hồi phiên từ refresh cookie. Endpoint refresh tường minh xoay refresh token; cập nhật có điều kiện để hạn chế dùng lại token cũ.
6. Đăng xuất thu hồi token phía server và xóa cookie. Blacklist lưu khóa băm của token; vẫn nhận diện định dạng token cũ để tương thích.
7. `JwtMiddleware` chặn phiên bị thu hồi/tài khoản bị khóa. Với HTML, xóa cookie và cho về khách hoặc trang đăng nhập; với API, trả lỗi xác thực/phân quyền.

Mật khẩu đăng ký/đổi/đặt lại yêu cầu tối thiểu 6 ký tự. Chữ hoa, số và ký tự đặc biệt chỉ là khuyến nghị độ mạnh.

**Điểm dễ nhầm:** `VerificationCode` hiện dùng chung cho xác thực email và reset mật khẩu. Có mã chưa đồng nghĩa chưa xác thực. `AccountState.AwaitingEmailVerification` xét cả `IsActive` và `ReasonBanned`; khi khóa/mở khóa, mã một lần cũ được xóa. Đây là cách xử lý tương thích dữ liệu hiện tại, chưa phải hai kho token tách biệt.

`UserService.ChangePassword` kiểm tra mật khẩu cũ và cấm mật khẩu mới trùng mật khẩu cũ. `ForgotPassword`/`ResetPassword` dùng mã có thời hạn; mã được xóa sau khi reset.

## 5. Hồ sơ cá nhân, người thân và upload

**Hồ sơ cá nhân:** `UserService.GetProfile/UpdateProfile` đọc nguồn chính từ database, cập nhật tên, số điện thoại và ảnh. DTO trả lại dữ liệu đã lưu để frontend đồng bộ phần tên/ảnh. Email không được đổi qua màn hình này.

**Người thân:** PatientService kiểm tra hồ sơ thuộc tài khoản hiện tại trước khi xem/sửa/xóa; xóa theo trạng thái hoạt động để giữ quan hệ lịch sử. Không lấy ID hồ sơ của người khác để đặt lịch.

**Upload:** `FileUploadService.ValidateFile` kiểm tra tệp rỗng, tối đa 5 MiB mỗi tệp và danh sách MIME do từng chức năng truyền vào. `UploadFileAsync` gửi stream lên Cloudinary, dùng GUID cho public ID và trả URL. API chữ ký trả timestamp/chữ ký/API key công khai, không trả API secret.

- Ảnh đại diện: JPG/PNG.
- Giấy phép/chứng chỉ: xem whitelist tại caller; không mặc định tất cả chức năng nhận mọi định dạng.
- Kết quả khám: JPG/PNG/PDF/DOC/DOCX hoặc link HTTP(S).
- MIME hiện lấy từ metadata upload; chưa xác minh chữ ký byte nội dung và chưa có quét malware.
- Nginx có thể chặn request bằng 413 trước khi ASP.NET xử lý. Tổng multipart có thể lớn hơn một tệp; cấu hình giới hạn request cần phù hợp, xem `docs/avatar-upload-413.md`.

## 6. Hồ sơ chuyên môn và lịch bác sĩ

**Hồ sơ:** DoctorService đăng ký thông tin chuyên môn, cơ sở, chuyên khoa và tài liệu. Admin duyệt/từ chối riêng với role tài khoản. Tài khoản có role DOCTOR nhưng chưa có hồ sơ vẫn có thể xuất hiện ở trang Người dùng.

**Lịch:** `GetSchedules`, `CreateSchedule`, `DeleteSchedule`, `Helpers/VietnamTime.cs`, `TimeSlotHelper.cs`.

- Dùng `DateOnly` cho ngày và enum `TimeSlot` cho các giờ cố định. So thời điểm bằng giờ `Asia/Ho_Chi_Minh`.
- API lịch làm việc không trả ca `CLOSED` hoặc ca đã đến giờ/quá giờ, kể cả trong hôm nay. Lịch sử khám không bị xóa.
- Tạo lịch yêu cầu bác sĩ đã duyệt, ngày bắt đầu từ hôm nay + 7 ngày theo giờ Việt Nam, khoảng liên tiếp tối đa 14 ngày và đầy đủ 7 TimeSlot. Cuối tuần/ngày nghỉ vẫn được tạo. Giao diện hỗ trợ 1/7/14 ngày; không sửa lịch đã có.
- Transaction và PostgreSQL `pg_advisory_xact_lock(doctorId)` tuần tự hóa thao tác trên lịch bác sĩ. Kiểm tra thời gian lần nữa sau khi lấy khóa.
- Đã có ca AVAILABLE/BOOKED cùng ngày/giờ: giữ nguyên và tạo các ca còn thiếu; gửi lại cùng request không sinh bản ghi trùng.
- Ca CLOSED nằm trong khoảng hợp lệ từ hôm nay + 7 ngày và không có lịch khám chưa hủy: mở lại bản ghi cũ, cập nhật cơ sở/Version. Không tạo một ca trùng mới.
- Hủy ca: kiểm tra quyền, thời gian và lịch khám; chuyển CLOSED, không xóa dữ liệu.
- `Version` là concurrency token của EF. Các thao tác đặt/đóng/mở lại tăng Version để phát hiện bản ghi đã bị sửa bởi request khác.
- Frontend vô hiệu hóa giờ đã qua và cập nhật danh sách mỗi phút khi tab hiện, modal đóng. Backend vẫn là nơi quyết định cuối cùng.

## 7. Đặt lịch, xác nhận, hủy và vắng mặt

**Đặt lịch:** `PatientService.BookAppointment`.

1. Lấy khóa Redis `lock:schedule:{id}` bằng SET-if-not-exists, TTL 5 giây.
2. Giới hạn tối đa 3 lịch đang PENDING/CONFIRMED cho tài khoản.
3. Kiểm tra hồ sơ thuộc tài khoản, bác sĩ đã duyệt, ca AVAILABLE và còn trong thời gian cho phép.
4. Kiểm tra trùng ngày/giờ trên hồ sơ người khám.
5. Đổi ca thành BOOKED, tăng Version, thêm Appointment PENDING và lưu.
6. Đặt job Hangfire nhắc ở phút 25 và hủy nếu chưa xác nhận ở phút 30; giải phóng khóa Redis trong `finally`.

Khóa Redis có TTL hữu hạn, không thay thế transaction/concurrency token ở database. Việc lưu lịch và enqueue Hangfire là các bước riêng, chưa có transactional outbox đảm bảo hai việc thành công cùng nhau.

**Bác sĩ:** `ConfirmAppointment` chỉ nhận PENDING. `DeclineAppointment` cho hủy/từ chối lịch chờ, hoặc đánh dấu NO_SHOW từ CONFIRMED sau giờ khám. `CompleteAppointment` chỉ hoàn thành lịch đã xác nhận. Mỗi nhánh kiểm tra lịch thuộc bác sĩ.

**Bệnh nhân hủy:** chỉ PENDING/CONFIRMED, kiểm tra quyền sở hữu và `App:CancellationWindow` (mặc định 12 giờ). Lưu `CancelledBy`, `CancellationReason` và trả ca về trống theo điều kiện của service. Nhánh thời hạn hủy hiện vẫn dùng UTC khi so với ngày/giờ lịch địa phương; đây là điểm cần thống nhất múi giờ khi sửa nghiệp vụ, không được coi là đã đồng bộ hoàn toàn với VietnamTime.

**Vắng khám:** khi bác sĩ đánh dấu NO_SHOW, transaction lấy advisory lock theo tài khoản đặt lịch. Đếm NO_SHOW theo ngày khám trong 30 ngày lịch, gồm hôm nay và 29 ngày trước. Khi số cũ là 4 và thêm lần thứ 5 trong khoảng đó, gửi thông báo cho admin đang hoạt động. Không tự khóa; các lần thứ 6 trở lên không lặp cảnh báo nếu số vẫn ở trên ngưỡng.

## 8. Kết quả khám, tái khám và lịch sử

`DoctorService.SaveMedicalResult` kiểm tra quyền sở hữu và trạng thái CONFIRMED/COMPLETED. Chẩn đoán bắt buộc. Cả SaveMedicalResult và CompleteAppointment đều chặn trước thời điểm bắt đầu ca bằng VietnamTime.HasStarted; kiểm tra trước upload. Giao diện vô hiệu hóa thao tác tương ứng. Đây là kiểm tra giờ khám, chưa phải xác nhận bệnh nhân đã có mặt. Ngày tái khám phải sau ngày khám và sau hôm nay tại Việt Nam.

Tệp và link là hai lựa chọn loại trừ nhau. Link chỉ nhận HTTP/HTTPS tuyệt đối; tệp phải qua whitelist. Khi sửa mà không gửi tệp/link mới, giữ đính kèm hiện tại. Upsert MedicalResult theo AppointmentId, chuyển lịch sang COMPLETED và lưu cùng lần SaveChanges.

PatientApiController cung cấp lịch sử/chi tiết kết quả theo tài khoản sở hữu hồ sơ. Hiển thị mã ca, người khám, ngày, chuyên khoa, chẩn đoán, lời dặn, đính kèm và ngày tái khám nếu có.

## 9. Chuyển công tác và quản lý danh mục

`CreateTransferRequest` kiểm tra cơ sở đích khác cơ sở hiện tại và không có yêu cầu PENDING khác. Lưu `OldFacilityName` ngay lúc gửi để thông tin cũ không đổi theo hồ sơ bác sĩ sau này.

Admin chỉ duyệt/từ chối yêu cầu PENDING; lưu ghi chú và `ProcessedAt`. Khi duyệt, cập nhật FacilityId và đóng các ca AVAILABLE tương lai theo điều kiện hiện có; không chuyển lịch đã đặt sang cơ sở mới. API trả cơ sở cũ/đích và ngày xử lý.

Dữ liệu lịch sử đã duyệt trước khi có snapshot có thể không biết cơ sở gốc; giao diện hiện “Chưa ghi nhận”. Không suy ngược cơ sở gốc từ cơ sở hiện tại.

Chuyên khoa/cơ sở có thêm, sửa, vô hiệu hóa và xác thực giấy phép. Các truy vấn danh mục đọc lại database để phản ánh lần chỉnh sửa vừa lưu. AdminCatalogController cung cấp danh mục lọc quản trị, bao gồm danh mục bác sĩ cho bộ lọc lịch hẹn.

## 10. Khóa tài khoản và phiếu xin mở khóa

**Khóa:** AdminService yêu cầu lý do, cấm khóa tài khoản admin, lấy advisory lock theo tài khoản, xóa refresh token/mã cũ, cập nhật IsActive/ReasonBanned, hủy lịch liên quan và đóng ca bác sĩ theo logic hiện tại. Lưu thông báo và gửi email thông qua IEmailService.

**Mở khóa trực tiếp:** yêu cầu lý do; phân biệt tài khoản thật sự chưa xác thực bằng AccountState. Xóa mã cũ, kích hoạt, giải quyết các phiếu PENDING thành APPROVED và thông báo cho tài khoản. Không phục hồi tự động những lịch đã bị hủy khi khóa.

**Phiếu xin mở khóa:**

- POST `/api/v1/auth/appeals`: email + mật khẩu + lý do 10–1000 ký tự. Xác minh mật khẩu nhưng không cấp phiên đăng nhập.
- Chỉ dành cho tài khoản bị khóa phù hợp; không dùng để kích hoạt đăng ký chưa xác thực hay tài khoản admin.
- Advisory lock theo tài khoản chống gửi trùng; tối đa một phiếu đang chờ và không gửi lại trong 24 giờ.
- Policy rate limit `appeals`: 10 request/phút theo IP, dùng cho gửi và tra cứu có mật khẩu.
- POST `/api/v1/auth/appeals/status`: trả trạng thái hoạt động, lý do khóa và phiếu mới nhất để người bị khóa đọc kết quả.
- GET `/api/v1/admin/appeals`: lọc trạng thái, mỗi trang 20 phiếu.
- PUT `/api/v1/admin/appeals/{id}`: admin duyệt không cần lý do, từ chối bắt buộc lý do; lưu người và thời điểm xử lý, khóa để tránh xử lý hai lần.

## 11. Tìm kiếm, phân trang và thống kê

PublicService kết hợp tên/chuyên khoa, cơ sở, tỉnh, khoảng giá và thứ tự sắp xếp. Tách từ khóa; PostgreSQL `unaccent` + `ILIKE` tìm không dấu, không phân biệt hoa thường. Escape ký tự `%`, `_`, `\` khi dựng mẫu LIKE để đầu vào không vô tình thành wildcard.

AdminService/AdminCatalogController hỗ trợ tìm kiếm và lọc danh sách quản trị. Các truy vấn phân trang dùng Count/Skip/Take; `Helpers/PagedResult` trả content, số phần tử và số trang. Giao diện có debounce; một số danh sách có version request để tránh response chậm ghi đè kết quả mới.

“Tài khoản bác sĩ” và “hồ sơ bác sĩ đã duyệt” là hai phép đếm khác nhau. `ApprovedDoctors()` là điều kiện chung cho tổng và danh sách quản trị đã duyệt, kể cả tài khoản bị khóa. Thống kê tăng trưởng theo khoảng ngày có thể dùng điều kiện khác; không so trực tiếp với tổng hồ sơ.

Max của bộ lọc giá lấy từ dữ liệu giá khám, không cố định 2 triệu. Giá hồ sơ bác sĩ được validation lớn hơn 0.

`PublicService.GetAvailableSlots` lọc AVAILABLE và giờ đã qua nếu ngày yêu cầu là hôm nay. Nhánh ngày cũ chưa được lọc tường minh ở API này; API đặt lịch vẫn kiểm tra và từ chối. Không suy rộng bộ lọc ở lịch bác sĩ sang mọi endpoint khác.

## 12. Đánh giá và phân loại nội dung

PatientService chỉ cho chủ lịch COMPLETED đánh giá; mỗi lịch có một review. Sau thêm/sửa, tính lại ReviewCount/RatingAverage. Admin ẩn nội dung bằng IsVisible; số sao vẫn được tính. API công khai thay nội dung bị ẩn bằng thông báo, không bỏ cả review. Không có đánh giá thì giao diện hiện “Chưa có đánh giá”, không mặc định 5 sao.

**ReviewClassifier:** Multinomial Naive Bayes chạy nội bộ, nạp mô hình một lần khi singleton được truy cập.

- Chuẩn hóa Unicode Form C, chữ thường; giới hạn đầu vào 10.000 ký tự và tối đa 300 từ.
- Đặc trưng: unigram, bigram, khớp từ điển, link và lặp từ.
- Học đếm từ từ `training.tsv`; Laplace smoothing + prior đồng đều; tính log-likelihood để tránh tràn số.
- Chuẩn hóa điểm sau khi trừ log-score lớn nhất. Nhãn không thông thường có điểm cao nhất từ 60 được đề xuất kiểm duyệt.
- Nhãn: thông thường, thô tục, spam, quảng cáo theo dữ liệu huấn luyện. Đây là gợi ý, không tự ẩn hoặc thay đổi sao; điểm không phải độ chính xác thực nghiệm.
- Từ điển từ Blue Eyes, giấy phép MIT; nguồn, commit và SHA256 tại [Moderation/README.md](WebDeveloper/Moderation/README.md). Ví dụ huấn luyện là dữ liệu mẫu của dự án, chưa phải tập dữ liệu đã đánh giá diện rộng.
- Admin hiện phân loại review được chiếu dữ liệu trong request trước khi phân trang; dữ liệu lớn cần thiết kế cache/kết quả phân loại có phiên bản.

## 13. Thông báo và tác vụ nền

**Thông báo:** `NotificationEvents.Collect` đọc ChangeTracker trước SaveChangesAsync, xét entity được thêm hoặc trạng thái/nội dung thực sự đổi. Thông báo và nghiệp vụ được lưu cùng lần commit; thất bại thì rollback và gỡ thông báo mới khỏi tracking. Lưu lặp không có thay đổi không tạo sự kiện lặp.

Các nguồn gồm đặt lịch/trạng thái khám, kết quả khám, duyệt hồ sơ, chuyển công tác, review nghi vấn. Phiếu mở khóa, khóa tài khoản và ngưỡng vắng khám có phần tạo thông báo trực tiếp ở luồng xử lý tương ứng.

NotificationsController luôn lọc UserId từ claim. GET trả tối đa 20 mục và cursor `before` dựa trên Id, kèm unreadCount. PATCH từng mục/tất cả để đọc. DELETE từng mục hoặc `/read` chỉ xóa thông báo đã đọc của chính người gọi. Đọc lặp không làm tăng/giảm sai số lượng.

`_Notifications.cshtml`, `notifications.js`, `notifications.css`: chuông, badge, vùng cuộn, tải thêm, đọc/xóa và polling 30 giây khi tab hiện. Nội dung gắn bằng textContent; chỉ điều hướng URL cùng origin. Đây là polling, chưa phải SignalR/WebSocket. Dòng báo chuyển công tác cố định ở đầu trang bác sĩ đã bỏ để tránh lặp hộp thư.

**Hangfire đang bật:** lưu job PostgreSQL; nhắc 25 phút, hủy lịch vẫn PENDING sau 30 phút. Hai hàm job hiện ghi log nội dung email, chưa gọi gửi SMTP thật. Chạy lại job kiểm tra trạng thái mới nhất trước xử lý.

**AppointmentCleanupService chưa bật:** đăng ký AddHostedService trong Program đang comment. File chứa thuật toán đóng ca, dọn blacklist và tính rating mỗi giờ nhưng không được coi là tác vụ đang chạy. Nó vẫn dùng UTC và lọc rating theo IsVisible; cần đồng bộ quy tắc mới trước khi bật lại.

## 14. Lỗi, giao diện dùng chung và khởi động

`GlobalExceptionMiddleware` ánh xạ DomainException, lỗi nghiệp vụ, xung đột concurrency và lỗi hệ thống sang ProblemDetails. `wwwroot/js/api-messages.js` đọc thông báo API; không hiển thị nguyên HTML lỗi proxy. Một số endpoint auth dùng `{ message }`, frontend cần hỗ trợ cả hai dạng.

Layout theo role dùng chung trạng thái xác thực, font và hộp thông báo. JavaScript gọi API, cập nhật lại dữ liệu sau thao tác; thông tin trên cookie/giao diện không thay quyền kiểm tra phía server. Render dữ liệu người dùng nên dùng textContent hoặc escape trước khi đưa vào HTML.

`Program.cs` thiết lập PostgreSQL, Redis, JWT, DI, middleware và Hangfire. Khởi động chạy Migrate; lỗi schema được ghi log và dừng ứng dụng. Migration AddDurableEmailDelivery tạo email_outbox và bổ sung hai cột cancellation_reason/cancelled_by bằng ADD COLUMN IF NOT EXISTS để tương thích database từng bổ sung thủ công.

## 15. Kiểm thử và cách mở rộng

| Bộ kiểm thử | Phạm vi | Yêu cầu |
|---|---|---|
| `tests/AccountRecoveryRegression` | Mã reset cũ, phiếu/mở khóa hai role, không vượt khóa bằng link verify | Database mới riêng; EnsureCreated; email giả |
| `tests/BackendRegression` | Lịch, mở lại ca, chuyển công tác, tìm kiếm, vắng khám, thông báo | PostgreSQL riêng; runner gọi migration |
| `tests/NotificationRegression` | Sự kiện, quyền đọc và tính nguyên tử | PostgreSQL thử nghiệm |
| `tests/CatalogRegression` | Danh mục, dữ liệu vừa sửa, tệp | PostgreSQL thử nghiệm |
| `tests/ModerationRegression` | Đặc trưng, điểm và gợi ý kiểm duyệt | Dữ liệu classifier trong project |

Không chạy các runner ghi fixture vào production. Đọc README của từng bộ kiểm thử trước khi chạy; runner dựa vào migration có thể bị chặn nếu snapshot EF lệch với model hiện tại.

Khi thêm chức năng: xác định role/quyền sở hữu → DTO và validation → service/API → trạng thái/transaction → sự kiện thông báo → giao diện/lỗi → kiểm thử hành vi → cập nhật mục liên quan trong file này. Khi thêm trường entity, bổ sung migration/snapshot đồng bộ; không chỉ thêm thuộc tính C# hoặc SQL thủ công.

Tài liệu liên quan: [README](README.md), [thông báo](docs/notifications.md), [cookie triển khai](docs/login-deployment.md), [upload 413](docs/avatar-upload-413.md), [các sửa backend](docs/backend-fixes-2026-10-15.md).

## 16. Cập nhật luồng nghiệp vụ tháng 10/2026

- **Email nền:** EmailOutbox được lưu cùng tài khoản hoặc quyết định phiếu mở khóa. EmailOutboxProcessor dùng transaction và FOR UPDATE SKIP LOCKED để các instance không xử lý cùng hàng đồng thời. Worker kiểm tra mỗi 5 giây khi rỗng, SMTP có deadline 20 giây; thử tối đa 5 lần với thời gian chờ tăng dần. SentAt đánh dấu thành công, LastError chỉ giữ loại lỗi. Email gửi thành công nhưng tiến trình chết trước commit có thể gửi lặp (at-least-once).
- **Gửi lại xác thực:** POST /api/v1/auth/resend-verification, giao diện /Auth/ResendVerification. Giới hạn một lần/phút/tài khoản và rate limit endpoint; phản hồi chung tránh tiết lộ email. Đổi mã, hết hạn sau 24 giờ. Worker bỏ thư xác thực cũ/hết hạn/tài khoản đã kích hoạt hoặc bị khóa. EmailOutbox:Enabled=false chỉ dùng khi muốn tắt worker trong môi trường kiểm thử. Các luồng email cũ khác chưa chuyển hết sang outbox.
- **Duyệt phiếu mở khóa:** chấp nhận chỉ cần xác nhận, không nhập lý do. Từ chối bắt buộc lý do 3–1000 ký tự. Quyết định và email được lưu cùng transaction; DeliveryKey theo phiếu ngăn tạo lặp email quyết định. Khác với thao tác mở khóa trực tiếp ở quản lý người dùng.
- **Học vị:** Helpers/DoctorDegree cung cấp danh sách dùng chung frontend/backend. Mục Khác cần ít nhất 3 chữ cái, tối đa 100 ký tự cả tiền tố, chỉ nhận bộ ký tự cho phép. Admin không duyệt hồ sơ có học vị không hợp lệ; yêu cầu sửa lại. Đây là kiểm tra định dạng, không xác minh bằng cấp thực tế.
- **Thông báo:** hồ sơ chờ duyệt dùng /Admin/Doctors?status=PENDING và doctorId khi có. GET sửa liên kết thông báo cũ tương ứng; trang quản trị đọc query để chọn tab. DELETE /read vẫn lọc chủ sở hữu và ReadAt khác null. Frontend chờ tải đang chạy trước thao tác, khóa nút khi xử lý, xác nhận rõ đánh dấu đọc/xóa đã đọc và hiển thị số xóa.
- **Kiểm thử mới:** tests/WorkflowRegression kiểm tra outbox/retry/resend, 7 ngày và đầy đủ ca, kết quả sớm, học vị, phiếu mở khóa, liên kết thông báo và bảo toàn tin chưa đọc. Chỉ chạy trên database thử nghiệm riêng; hướng dẫn tại tests/WorkflowRegression/README.md.
