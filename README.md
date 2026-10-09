# Hệ Thống Backend Đặt Lịch Khám Bệnh

Hệ thống backend cung cấp các API cho ứng dụng đặt lịch khám bệnh trực tuyến. Dự án được xây dựng bằng **ASP.NET Core 10 Web API**, kết nối với cơ sở dữ liệu **PostgreSQL** qua **Entity Framework Core**.

## Kiến trúc hệ thống
Hệ thống được thiết kế theo mô hình **MVC (Model-View-Controller) kết hợp Web API**, đồng thời tổ chức code theo cấu trúc **N-Tier (N-Layer)** để tách biệt các thành phần:

- **Tầng Presentation (MVC & API)**:
  - **Views & Controllers**: Sử dụng mô hình MVC truyền thống cho các trang cơ bản (có thư mục `Views` và `HomeController`).
  - **Api Controllers**: Nằm trong `Controllers/Api/`, đóng vai trò là các RESTful Web API (`[ApiController]`) tiếp nhận request HTTP, trả về dữ liệu JSON, xử lý xác thực/phân quyền (JWT) cho Frontend (React/Vue/Mobile) gọi tới.
- **Tầng Services (Business Logic Layer)**: Chứa toàn bộ logic nghiệp vụ cốt lõi (AuthService, PatientService, DoctorService, AdminService,...). Tầng này giúp controller mỏng lại (Fat Model/Service - Thin Controller).
- **Tầng Data (Data Access Layer)**: Chứa `ApplicationDbContext` kế thừa từ `DbContext`, định nghĩa các `DbSet` giao tiếp trực tiếp với PostgreSQL.
- **Models**:
  - **Entities**: Lớp ánh xạ trực tiếp với bảng trong cơ sở dữ liệu.
  - **DTOs**: Các object chuyển giao dữ liệu giữa client và server (Request/Response).
  - **Enums**: Định nghĩa các trạng thái chuẩn hóa (như `BookingStatus`, `RoleStatus`, `SlotStatus`,...).
- **Middleware**: Xử lý logic chung cho mọi request như `GlobalExceptionMiddleware` (bắt lỗi tập trung) và `JwtMiddleware` (xử lý token).

## Công nghệ sử dụng
- **Framework**: ASP.NET Core 10
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core 10 (Npgsql)
- **Authentication**: JWT (JSON Web Tokens)
- **Mã hóa mật khẩu**: BCrypt.Net-Next
- **Lưu trữ file**: Cloudinary (CloudinaryDotNet)
- **Gửi Email**: MailKit / SMTP

## Tính năng chính
1. **Xác thực & Phân quyền**: Đăng nhập, đăng ký, quên mật khẩu (gửi mật khẩu mới qua email), xác minh email qua mã OTP/link. Hỗ trợ 3 role: `ADMIN`, `DOCTOR`, `PATIENT`.
2. **Quản lý người dùng (Patient)**: Bệnh nhân có thể tạo và quản lý hồ sơ người thân, đặt lịch khám theo ca làm việc của bác sĩ, xem lịch sử khám và đánh giá bác sĩ.
3. **Quản lý bác sĩ (Doctor)**: Bác sĩ có thể đăng ký tài khoản (cần duyệt), cập nhật hồ sơ, tạo lịch làm việc (ca khám), quản lý lịch hẹn (xác nhận, hoàn thành, cập nhật kết quả bệnh án/đơn thuốc), và yêu cầu chuyển cơ sở công tác.
4. **Quản trị viên (Admin)**: Admin có thể xem thống kê, quản lý/duyệt hồ sơ bác sĩ, quản lý chuyên khoa (Specialty) và cơ sở y tế (Facility), xử lý yêu cầu chuyển công tác, khóa/mở khóa người dùng và ẩn các đánh giá vi phạm.
5. **Background Services**: Hệ thống chạy ngầm dịch vụ dọn dẹp các lịch hẹn hết hạn hoặc tự động nhắc nhở (đang có class `AppointmentCleanupService`).

## Cấu trúc thư mục
- `/Controllers`: Các endpoint API và view (nếu có).
- `/Services`: Logic nghiệp vụ chính của ứng dụng.
- `/Data`: File `ApplicationDbContext.cs` thiết lập DbContext và Fluent API.
- `/Models`: Entities, DTOs, Enums.
- `/Middleware`: `GlobalExceptionMiddleware` để chuẩn hóa định dạng trả về lỗi (400, 401, 409, 500).
- `/Security`: Các class liên quan đến JWT (như `JwtTokenProvider`).
- `/BackgroundServices`: Chứa các job chạy ngầm (IHostedService).

## Thiết kế Cơ sở dữ liệu (Mô hình ERD)
Mô hình cơ sở dữ liệu được thiết kế theo mô hình quan hệ (Relational Database) và chuẩn hóa. Các thực thể chính và liên kết:

- **Mô hình người dùng (User & Profiles)**
  - **User** 1-N **PatientProfiles**: (1 tài khoản đăng nhập có thể tạo nhiều hồ sơ khám bệnh cho người thân).
- **Mô hình lịch khám (Scheduling)**
  - **Facility / Specialty** 1-N **DoctorDetails**: (Nhiều bác sĩ có thể làm việc tại 1 cơ sở và thuộc 1 chuyên khoa).
  - **DoctorDetail** 1-N **DoctorSchedules**: (1 bác sĩ được tạo nhiều ca khám - slot làm việc theo ngày).
- **Mô hình đặt hẹn & bệnh án (Booking & Medical Records)**
  - **PatientProfile** 1-N **Appointments**: (1 hồ sơ bệnh nhân có nhiều lịch hẹn).
  - **DoctorSchedule** 1-N **Appointments**: (1 ca khám cho phép đặt lịch, có ràng buộc check trạng thái BookingStatus).
  - **Appointment** 1-1 **MedicalResult**: (Mỗi lịch khám khi hoàn thành sẽ có 1 kết quả khám và đơn thuốc duy nhất).
  - **Appointment** 1-1 **Review**: (Mỗi lịch khám cho phép bệnh nhân để lại 1 đánh giá duy nhất).

*Lưu ý: Các Indexes (chỉ mục) được tối ưu hóa bằng Fluent API cho việc tra cứu nhanh theo User, Specialty, Facility và Appointment.*

## Các Bài Toán Hệ Thống Đặt Ra & Cách Code Hiện Tại Giải Quyết

Trong hệ thống đặt lịch y tế trực tuyến, có nhiều bài toán phức tạp phát sinh. Dưới đây là các vấn đề chính và cách mã nguồn hiện tại đang giải quyết:

### 1. Tránh Đặt Trùng Lịch (Double Booking / Concurrency)
- **Vấn đề**: Nhiều bệnh nhân cùng lúc click đặt chung 1 ca làm việc của 1 bác sĩ. Nếu không cẩn thận, 2 bệnh nhân sẽ cùng đặt thành công 1 lịch.
- **Cách xử lý hiện tại**: Entity `DoctorSchedule` dùng Annotation `[ConcurrencyCheck]`. Nếu có 2 luồng cùng update, EF Core sẽ văng ra lỗi `DbUpdateConcurrencyException` (Khóa lạc quan - Optimistic Locking).
- **Giải pháp tối ưu / Hiện đại**: Ở các hệ thống lớn (Concurrency cao), việc để request chạm tới DB rồi mới văng lỗi sẽ gây tốn tài nguyên DB. Thay vào đó, hệ thống sẽ sử dụng **Distributed Lock (Khóa phân tán)** trên **Redis** (ví dụ: Redlock). Khi user ấn đặt, slot đó bị lock ngay trên RAM (Redis). Request thứ 2 đến sẽ bị từ chối lập tức mà không cần Query xuống Database.

### 2. Bảo Mật Luồng Dữ Liệu & Phân Quyền
- **Vấn đề**: Ngăn chặn bệnh nhân gọi API của bác sĩ, hoặc ngăn chặn truy cập dữ liệu nhạy cảm của người khác.
- **Cách xử lý hiện tại**: Sử dụng 1 **JWT (JSON Web Token)** duy nhất gắn Role. Dùng code thủ công `if (profile.UserId != user.UserId)` để chặn quyền.
- **Giải pháp tối ưu / Hiện đại**: 
  - Thay vì dùng 1 JWT sống dài, kiến trúc chuẩn sẽ dùng cặp **Access Token (sống ngắn 10-15p)** và **Refresh Token (sống dài 7-30 ngày)** lưu trong HTTP-Only Cookie để chống XSS.
  - Quản lý user/role sẽ được tách ra 1 Server độc lập gọi là **Identity Provider** (chuẩn OAuth2/OpenID Connect) thông qua các công cụ như Keycloak hoặc Duende IdentityServer thay vì tự code.

### 3. Xử Lý Lưu Trữ File (Ảnh CCCD, Chứng Chỉ, Kết quả)
- **Vấn đề**: File lưu trực tiếp vào server có thể làm quá tải ổ cứng server và nghẽn băng thông.
- **Cách xử lý hiện tại**: Backend nhận file qua API rồi tự đẩy lên **Cloudinary** (Đồng bộ).
- **Giải pháp tối ưu / Hiện đại**: Việc để file đi qua luồng của Backend sẽ làm chậm Server. Các hệ thống hiện đại áp dụng mô hình **Pre-signed URL (vd: AWS S3)**. Backend chỉ sinh ra một "đường link tạm thời có chữ ký". Client (Web/App) sẽ dùng link này để upload file **TRỰC TIẾP** từ máy khách lên S3/Cloudinary, giảm tải 100% băng thông tải file cho Backend.

### 4. Đồng Bộ Trạng Thái, Thu Dọn Dữ Liệu Hết Hạn
- **Vấn đề**: Lịch chờ duyệt bị bỏ quên sẽ làm chết (khóa) slot của bác sĩ, cần thu hồi lại.
- **Cách xử lý hiện tại**: Viết 1 Background Task (`IHostedService`) liên tục thức dậy quét toàn bộ Database để tìm lịch quá hạn.
- **Giải pháp tối ưu / Hiện đại**: 
  - Quét DB liên tục (Polling) là mô hình tốn kém. Hệ thống lớn áp dụng mô hình Event-Driven với **Message Queue (RabbitMQ / Kafka)** dùng cơ chế **Delayed Message**. Ví dụ: Vừa tạo lịch xong, bắn 1 message hẹn đúng 30p sau mới kích hoạt. Sau 30p, hệ thống nhận tin nhắn và đi hủy lịch, không cần query DB lặp đi lặp lại.
  - Nếu dùng Job, sẽ sử dụng framework **Hangfire** hoặc **Quartz.NET** để quản lý tiến trình chạy ẩn có Dashboard giám sát và cơ chế tự động thử lại (Retry) khi fail.

### 5. Chuẩn Hóa Lỗi & Giấu Mã Lỗi Hệ Thống
- **Vấn đề**: Lỗi văng ra mang theo Stack Trace chi tiết làm lộ cấu trúc Database cho Hacker.
- **Cách xử lý hiện tại**: Bắt Exception bằng `GlobalExceptionMiddleware` và trả về mã lỗi 400 nếu bắt gặp `InvalidOperationException`.
- **Giải pháp tối ưu / Hiện đại**: 
  - Thay vì trả Json tự chế, các API chuẩn sẽ dùng quy chuẩn quốc tế **RFC 7807 (Problem Details)** có sẵn của .NET để báo lỗi.
  - Không ném chung lỗi vào `InvalidOperationException` vì sẽ lẫn lộn với lỗi sập hệ thống (System Error). Code xịn sẽ định nghĩa các **Domain Exception** riêng biệt (ví dụ: `DoctorFullyBookedException`, `UserNotFoundException`) để phân loại rõ ràng.

### 6. Chặn Hành Vi Phá Hoại (Spam Đặt Lịch)
- **Vấn đề**: Hacker viết Tool tự động gọi API ngàn lần/giây để spam full lịch khám ảo.
- **Cách xử lý hiện tại**: Query DB kiểm tra số lịch PENDING >= 3 thì chặn bằng If-Else.
- **Giải pháp tối ưu / Hiện đại**: Khi bị spam liên tục, thao tác Query Count trong DB sẽ làm treo luôn Database. Các hệ thống thực tế giải quyết bằng **Rate Limiting** chặn từ cổng **API Gateway** (Nginx, YARP) hoặc Redis. Khách bị cấm gọi quá 5 request/giây. Đồng thời yêu cầu tích hợp thêm **CAPTCHA (Google reCAPTCHA / Cloudflare Turnstile)** để xác minh con người.

---

## 10 Rủi Ro Nghiêm Trọng Nhất Hệ Thống Có Thể Đối Mặt & Giải Pháp Tiêu Chuẩn Ngành

Nếu đưa hệ thống này lên môi trường thực tế (Production) với quy mô hàng trăm ngàn người dùng, đây là 10 lỗ hổng và rủi ro lớn nhất hệ thống sẽ đối mặt, hậu quả và cách ngành công nghiệp phần mềm đang giải quyết:

### 1. Gửi Mật Khẩu Trần Qua Email (Plaintext Password in Email) [Done]
- **Hậu quả**: Chức năng Quên Mật Khẩu hiện tại đang tự sinh mật khẩu mới (8 ký tự) và gửi thẳng văn bản đó vào Email người dùng. Bất kỳ ai chặn bắt được Email (hoặc nhân viên IT đọc được log mail server) đều có thể chiếm đoạt tài khoản. Vi phạm nghiêm trọng chuẩn bảo mật quốc tế.
- **Giải pháp hiện nay**: Hệ thống tuyệt đối không được sinh mật khẩu hộ người dùng. Chuẩn hiện tại là sinh ra một **Password Reset Token** (mã hóa), gửi 1 đường link chứa token đó qua Email. Người dùng click vào link và tự điền mật khẩu mới.

### 2. "Cartesian Explosion" (Bùng nổ dữ liệu kết bảng) trong Entity Framework [Done]
- **Hậu quả**: Trong các API lấy danh sách Lịch khám (`GetAppointments`), mã nguồn đang dùng `Include().ThenInclude()` liên tục 4-5 cấp (Lấy Lịch -> lấy Ca Khám -> lấy Bác sĩ -> lấy Khoa -> lấy Bệnh nhân). EF Core sẽ sinh ra câu lệnh SQL `JOIN` khổng lồ, trả về hàng triệu dòng dữ liệu trùng lặp trên bộ nhớ RAM, làm sập Server (Out Of Memory) ngay lập tức khi lượng dữ liệu lớn.
- **Giải pháp hiện nay**: Sử dụng **`.AsSplitQuery()`** trong EF Core để tách thành nhiều câu Query nhỏ. Hoặc với các chức năng chỉ cần đọc dữ liệu nhanh (Read-heavy), sử dụng thư viện **Dapper** (Micro-ORM) viết SQL thuần để tối ưu hiệu suất.

### 3. Cập nhật dữ liệu hàng loạt ở HTTP GET (Vi phạm RESTful) [Done]
- **Hậu quả**: API `/transfers` (Lấy danh sách chuyển công tác) đang gọi lén hàm `UpdateHistoricalSchedules()` thực thi `UPDATE` toàn bộ bảng `DoctorSchedules`. Nếu Admin F5 trang này 10 lần, Database sẽ bị khóa (Table Lock) 10 lần, làm treo mọi giao dịch đặt lịch của người bệnh.
- **Giải pháp hiện nay**: Bóc tách logic UPDATE ra khỏi HTTP GET. Đưa logic đồng bộ lịch sử vào một quy trình Event (VD: Khi bác sĩ đổi chỗ làm xong thì kích hoạt Event cập nhật ca khám cũ) hoặc chuyển sang một API `HTTP POST` độc lập để chạy bất đồng bộ.

### 4. Bất đồng bộ thời gian do Hardcode Timezone [Done]
- **Hậu quả**: Việc sử dụng `DateTime.UtcNow.AddHours(7)` cố định trong code sẽ khiến hệ thống không thể hoạt động nếu được deploy ở máy chủ quốc tế, hoặc khi mở rộng cho bệnh nhân quốc tế (ví dụ việt kiều). Khách đặt lịch 8h sáng nhưng hệ thống lại ghi nhận lệch múi giờ, dẫn tới bác sĩ nhầm lịch.
- **Giải pháp hiện nay**: Toàn bộ Backend/Database phải lưu theo chuẩn **UTC Time** (`DateTimeOffset`). Việc cộng trừ múi giờ (+7) là việc của Frontend (Browser/Mobile) tự tính toán dựa trên cấu hình máy tính của người dùng cuối.

### 5. Nút thắt cổ chai ở Background Job quét DB liên tục [Chưa cần phải sử lý vì hệ thống nhỏ]
- **Hậu quả**: Hàm `AppointmentCleanupService` định kỳ thức dậy và `SELECT/UPDATE` toàn bộ Database để tìm lịch hẹn quá hạn. Khi hệ thống có 10 triệu lịch hẹn, Job này quét sẽ ngốn 100% CPU của Database, làm chậm mọi thao tác khác.
- **Giải pháp hiện nay**: Áp dụng mô hình **Event-Driven Architecture** (RabbitMQ / Kafka). Thay vì rà quét toàn bộ DB, khi một user vừa đặt lịch xong, hệ thống ném 1 viên nén (Message) vào Queue, cấu hình nó ngủ đúng 30 phút. Hết 30 phút, Queue nhả viên nén ra kích hoạt lệnh hủy đúng 1 lịch khám duy nhất đó.

### 6. Quá tải do tính toán thống kê (Rating) Real-Time
- **Hậu quả**: Khi một bệnh nhân đánh giá (Review), code hiện tại đang lấy toàn bộ đánh giá của bác sĩ đó để cộng lại chia trung bình (`Math.Round(Average)`). Khi bác sĩ có 50.000 đánh giá, thao tác tính toán này sẽ mất vài giây, làm hệ thống treo cứng chờ đợi.
- **Giải pháp hiện nay**: Áp dụng mô hình CQRS hoặc sử dụng **Materialized View**. Rating trung bình sẽ không tính realtime khi người dùng gửi đánh giá, mà được tính toán định kỳ (hàng đêm) bằng batch processing, hoặc cộng dồn (Incremental Update) thay vì query toàn bộ bảng.

### 7. Nguy cơ DDoS (Tấn công từ chối dịch vụ) ở tầng Application
- **Hậu quả**: Ứng dụng chưa có bất kỳ cơ chế giới hạn nào. Kẻ tấn công chỉ cần viết 1 đoạn script gọi API Đăng nhập hoặc Xem danh sách Bác sĩ 10.000 lần / giây. Server sẽ cạn kiệt Connection Pool tới Database và sập ngay lập tức.
- **Giải pháp hiện nay**: Cài đặt **API Gateway** (như Kong, Ocelot) hoặc sử dụng middleware **Rate Limiting** của .NET kết hợp Redis. Cấm mọi IP thực hiện quá `x` requests / giây. Tích hợp Web Application Firewall (WAF) như Cloudflare.

### 8. Lộ lọt bảo mật Token do cách lưu trữ chưa chuẩn [Done]
- **Hậu quả**: Hiện tại Token JWT tuy có đọc từ Cookie, nhưng mặc định các hệ thống SPA (React/Angular) lại lưu ở `LocalStorage`. Bất kỳ script độc hại nào (XSS - Cross Site Scripting) nhúng vào web đều có thể lấy trộm JWT và mạo danh bác sĩ/admin vĩnh viễn.
- **Giải pháp hiện nay**: Backend phải thiết lập Token trả về dưới dạng **`HttpOnly; Secure; SameSite` Cookies**. Khi đó Javascript ở trình duyệt không thể đọc được Token, chặn đứng 100% rủi ro bị đánh cắp qua XSS. 

### 9. Không Caching Database (Khủng hoảng lượng truy cập) [Done]
- **Hậu quả**: Dữ liệu như "Danh sách Chuyên Khoa", "Danh sách Cơ sở Y Tế" gần như không bao giờ đổi. Nếu 10,000 người vào trang chủ, hệ thống sẽ chọc xuống PostgreSQL 10,000 lần cùng một câu Query y hệt nhau, gây lãng phí tài nguyên khủng khiếp.
- **Giải pháp hiện nay**: Bắt buộc phải áp dụng **Distributed Caching (Redis/Memcached)**. Khi có người request danh sách Chuyên khoa lần đầu, kết quả sẽ lưu vào RAM (Redis). 9,999 người vào sau sẽ lấy thẳng từ RAM với tốc độ 1ms mà DB không hề hay biết.

### 10. Lỗ hổng Race-Condition (Cạnh tranh luồng) khi đăng ký Tài Khoản
- **Hậu quả**: Trong hàm `SignUp`, ứng dụng kiểm tra `if (!Any(Email))` rồi mới `Add(User)`. Trong khe hở 1 mili-giây giữa dòng If và dòng Add, một tool tự động bắn 2 request đăng ký cùng 1 email. Chữ If đều pass và Database sẽ bị chèn 2 User có chung email, gây sụp đổ logic đăng nhập.
- **Giải pháp hiện nay**: Mọi xử lý liên quan tới tính Duy Nhất tuyệt đối không thể chỉ tin tưởng vào code `If` ở Backend. Phải đẩy ràng buộc xuống thẳng Database bằng cách thiết lập **Unique Index** cho cột Email. Khi bị chèn đúp, Database sẽ báo lỗi trực tiếp và Backend chỉ việc Catch lỗi đó.