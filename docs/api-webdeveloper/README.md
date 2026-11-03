# API WebDeveloper
| Nhóm API | Số endpoint |
|---|---|
| [Xác thực và quản lý phiên](01-auth.md) | 7 |
| [Tra cứu công khai](02-public.md) | 8 |
| [Tài khoản hiện tại](03-user.md) | 4 |
| [API bệnh nhân](04-patient.md) | 13 |
| [API bác sĩ](05-doctor.md) | 14 |
| [API quản trị](06-admin.md) | 26 |
| [Danh mục quản trị](07-admin-catalog.md) | 3 |
| [Kháng nghị mở khóa](08-account-appeals.md) | 5 |
| [Thông báo](09-notifications.md) | 5 |
| [Gửi lại xác minh email](10-verification-email.md) | 1 |

# Một số luồng quan trọng
## Một HTTP request cần database

```mermaid
flowchart TB
    A["Trình duyệt gửi request"] --> B["Middleware"]
    B --> C["Controller nhận yêu cầu"]
    C --> D["Service xử lý nghiệp vụ"]
    D --> E["ApplicationDbContext / EF Core"]
    E --> F[("PostgreSQL")]
    F --> G["Trả kết quả về Service và Controller"]
    G --> H{"Nội dung phản hồi"}
    H -->|HTML| I["Razor tạo trang HTML"]
    H -->|JSON| J["JavaScript nhận dữ liệu"]
    I --> K["Trình duyệt hiển thị trang"]
    J --> L["Cập nhật giao diện hiện có"]

    B -.- N1["Kiểm tra xác thực, quyền truy cập<br/>và xử lý lỗi theo pipeline"]
    D -.- N2["Kiểm tra quy tắc nghiệp vụ<br/>Ví dụ: còn ca trống, đúng chủ hồ sơ"]
    E -.- N3["Chuyển truy vấn C# thành SQL<br/>và lưu thay đổi vào database"]
    H -.- N4["HTML phục vụ hiển thị trang<br/>JSON cung cấp dữ liệu cho JavaScript"]

    classDef note fill:#fff9e6,stroke:#c5a352,stroke-dasharray:4 4,color:#333;
    class N1,N2,N3,N4 note;
```

Đây là luồng phổ biến, không bắt buộc mọi request qua service/database. Controller appeal, notification và catalog có query trực tiếp.

## Đặt lịch và commit

```mermaid
sequenceDiagram
    participant C as Bệnh nhân
    participant A as API / PatientService
    participant R as Redis
    participant D as PostgreSQL
    participant H as Hangfire

    C->>A: Gửi hồ sơ, ca và lý do khám
    A->>R: Xin khóa tạm theo ca
    Note over A,R: Hạn chế nhiều request cùng tranh một ca<br/>Khóa có thời hạn 30 giây

    alt Không lấy được khóa
        A-->>C: Báo ca đang có người thao tác
    else Lấy được khóa
        A->>D: Bắt đầu giao dịch và khóa tài khoản
        Note over A,D: Khóa tài khoản giúp kiểm hạn mức chung<br/>khi cùng người dùng đặt nhiều ca đồng thời

        A->>D: Kiểm quyền, hạn mức, ca và thời gian

        alt Không hợp lệ
            A->>D: Rollback
            Note over A,D: Hủy các thay đổi trong giao dịch
            A-->>C: Thông báo lý do không thể đặt
        else Hợp lệ
            A->>D: Lưu BOOKED, PENDING và thông báo
            A->>D: Commit
            Note over A,D: Chấp nhận thay đổi trong database<br/>Ca đã được giữ, lịch chờ bác sĩ xác nhận

            A->>H: Lên việc nhắc 25 phút, hủy 30 phút
            Note over A,H: Bước này nằm sau commit<br/>Lỗi lên job không tự hủy lịch đã lưu

            A-->>C: Thông báo đặt lịch thành công
        end

        A->>R: Giải phóng khóa nếu vẫn đúng chủ
        Note over A,R: GUID nhận diện chủ khóa<br/>Tránh xóa nhầm khóa của request khác
    end
```

TTL Redis không thay thế database. Hai ca khác nhau vẫn cần khóa theo tài khoản để bảo vệ quota chung. Cleanup finally cũng có thể lỗi sau commit.

## Trạng thái lịch hẹn

```mermaid
stateDiagram-v2
    state "PENDING: Chờ xác nhận" as PENDING
    state "CONFIRMED: Đã xác nhận" as CONFIRMED
    state "CANCELLED: Đã hủy" as CANCELLED
    state "COMPLETED: Đã hoàn thành" as COMPLETED
    state "NO_SHOW: Vắng khám" as NO_SHOW

    [*] --> PENDING: Đặt lịch hợp lệ
    PENDING --> CONFIRMED: Bác sĩ chấp nhận còn hạn
    PENDING --> CANCELLED: Hủy hợp lệ hoặc quá hạn
    CONFIRMED --> CANCELLED: Hủy hợp lệ hoặc khóa tài khoản liên quan
    CONFIRMED --> COMPLETED: Đã tới giờ và hoàn thành khám
    CONFIRMED --> NO_SHOW: Đã tới giờ và bác sĩ ghi nhận vắng

    note right of PENDING
        Phải xác nhận trước mốc sớm hơn:
        lúc đặt cộng 30 phút hoặc giờ ca
    end note

    note right of COMPLETED
        Có thể hoàn thành trước khi nhập kết quả
        Vì vậy cần kiểm thêm HasResult
    end note

    note left of CANCELLED
        Giữ bản ghi để lưu lịch sử
        Ca được mở lại hoặc đóng theo từng trường hợp
    end note
```

Hạn xác nhận là mốc sớm hơn giữa đặt+30 phút và giờ ca. Complete chưa chứng minh đã có kết quả. SlotStatus là máy trạng thái khác với BookingStatus.

## Đăng nhập và Đăng xuất

```mermaid
flowchart TB
    A["Người dùng đăng nhập"] --> B["Kiểm email, mật khẩu<br/>và trạng thái tài khoản"]
    B --> C{"Hợp lệ?"}
    C -->|Không| D["Thông báo lỗi"]
    C -->|Có| E["Cấp token qua cookie"]

    E --> F["Người dùng gửi yêu cầu"]
    F --> G{"Access token dùng được?"}
    G -->|Có| H["Kiểm thu hồi, tài khoản<br/>và quyền truy cập"]
    G -->|Cần cấp lại| I{"Refresh token<br/>còn hợp lệ?"}
    I -->|Có| J["Cấp access token mới"]
    J --> H
    I -->|Không| A

    H --> K["Xử lý nếu đủ điều kiện<br/>hoặc từ chối nếu không"]
    K --> L["Trả kết quả"]

    E --> M["Người dùng đăng xuất"]
    M --> N["Thu hồi credential phù hợp<br/>và xóa cookie"]
    N --> O(["Kết thúc phiên"])

    E -.- P["Access token dùng cho request<br/>Refresh token dùng để xin access mới"]
    I -.- Q["Kiểm token khớp database,<br/>còn hạn và tài khoản active"]
    N -.- R["Chặn access còn hợp lệ qua blacklist<br/>và xóa refresh khớp trong database"]

    classDef note fill:#fff9e6,stroke:#c5a352,stroke-dasharray:4 4,color:#333;
    class P,Q,R note;
```