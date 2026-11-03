# API quản trị

<a id="getstats"></a>

## GET `/api/v1/admin/stats` — Dashboard quản trị

### 1. Chức năng và vai trò

Dashboard quản trị. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `startDate` | `DateOnly?` | Query | Ngày YYYY-MM-DD; ý nghĩa theo endpoint. |
| `endDate` | `DateOnly?` | Query | Ngày YYYY-MM-DD; ý nghĩa theo endpoint. |

### 3. Luồng xử lý kỹ thuật

GetStats mặc định 30 ngày Việt Nam, kiểm start<=end; lịch tính theo DateWorking, user mới theo CreatedAt đổi biên sang UTC; tính tỷ lệ/trạng thái và nhóm bác sĩ.

EstimatedRevenue là giá trị khám ước tính theo giá hiện tại, chưa phải tiền đã thu.

### 4. Đầu ra

200: StatResponse.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:24`

```csharp
public async Task<IActionResult> GetStats(
    [FromQuery] DateOnly? startDate,
    [FromQuery] DateOnly? endDate)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _adminService.GetStats(startDate, endDate));
}

```

<a id="getpendingdoctors"></a>

## GET `/api/v1/admin/doctor-pending` — Bác sĩ chờ duyệt

### 1. Chức năng và vai trò

Bác sĩ chờ duyệt. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `search` | `string?` | Query | Mặc định null. |
| `specialtyId` | `int?` | Query | Mặc định null. |
| `facilityId` | `int?` | Query | Mặc định null. |

### 3. Luồng xử lý kỹ thuật

Lọc DoctorDetail PENDING với search/chuyên khoa/cơ sở, trả dữ liệu tóm tắt để admin xét.

### 4. Đầu ra

200: List<object>, các trường anonymous object trong AdminService.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:32`

```csharp
public async Task<IActionResult> GetPendingDoctors(
    [FromQuery] string? search = null,
    [FromQuery] int? specialtyId = null,
    [FromQuery] int? facilityId = null)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _adminService.GetPendingDoctors(search, specialtyId, facilityId));
}

```

<a id="getalldoctors"></a>

## GET `/api/v1/admin/doctor-all` — Danh sách bác sĩ quản trị

### 1. Chức năng và vai trò

Danh sách bác sĩ quản trị. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |
| `search` | `string?` | Query | Mặc định null. |
| `specialtyId` | `int?` | Query | Mặc định null. |
| `facilityId` | `int?` | Query | Mặc định null. |

### 3. Luồng xử lý kỹ thuật

Bắt đầu từ ApprovedDoctors(), lọc search/chuyên khoa/cơ sở; controller clamp page>=0 và size 1..100 trước khi service phân trang.

Service bắt đầu từ ApprovedDoctors(), không lấy mọi trạng thái hồ sơ.

### 4. Đầu ra

200: PagedResult<object>; content gồm doctorId, fullName, email, avatarUrl, verificationStatus, specialtyName, facilityName, ratingAverage, reviewCount.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:41`

```csharp
public async Task<IActionResult> GetAllDoctors(
    [FromQuery] int page = 0,
    [FromQuery] int size = 10,
    [FromQuery] string? search = null,
    [FromQuery] int? specialtyId = null,
    [FromQuery] int? facilityId = null)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _adminService.GetAllDoctors(Math.Max(0, page), Math.Clamp(size, 1, 100), search, specialtyId, facilityId));
}

```

<a id="getdoctordetail"></a>

## GET `/api/v1/admin/doctor/{id}` — Xem hồ sơ bác sĩ để xét duyệt

### 1. Chức năng và vai trò

Xem hồ sơ bác sĩ để xét duyệt. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

AdminService lấy hồ sơ/chuyên khoa/cơ sở/user, trả cả CCCD, CertificateUrl và DiplomaUrl theo projection.

### 4. Đầu ra

200: object chi tiết.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:52`

```csharp
public async Task<IActionResult> GetDoctorDetail(int id)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _adminService.GetDoctorDetail(id));
}

```

<a id="approvedoctor"></a>

## PUT `/api/v1/admin/{id}/approve` — Duyệt hồ sơ bác sĩ ban đầu

### 1. Chức năng và vai trò

Duyệt hồ sơ bác sĩ ban đầu. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Chỉ PENDING; kiểm học vị, đổi APPROVED và cập nhật xác minh cơ sở theo nhánh; lưu, notification; email còn đường gửi trực tiếp.

### 4. Đầu ra

200: object {message}. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:58`

```csharp
public async Task<IActionResult> ApproveDoctor(int id)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        return Ok(await _adminService.ApproveDoctor(id));
    }
    catch (Exception e)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(e.Message);
    }
}

```

<a id="rejectdoctor"></a>

## PUT `/api/v1/admin/{id}/reject` — Từ chối hồ sơ bác sĩ ban đầu

### 1. Chức năng và vai trò

Từ chối hồ sơ bác sĩ ban đầu. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `reason` | `string` | Query | Theo hợp đồng action. |

### 3. Luồng xử lý kỹ thuật

Chỉ PENDING; lưu REJECTED và ReasonReject, thông báo quyết định, gửi email theo đường trực tiếp.

### 4. Đầu ra

200: object {message}. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:71`

```csharp
public async Task<IActionResult> RejectDoctor(int id, [FromQuery] string reason)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        return Ok(await _adminService.RejectDoctor(id, reason));
    }
    catch (Exception e)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(e.Message);
    }
}

```

<a id="addspecialty"></a>

## POST `/api/v1/admin/specialty` — Thêm chuyên khoa

### 1. Chức năng và vai trò

Thêm chuyên khoa. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `specialtyName` | `string` | Body JSON | Bắt buộc. |
| `description` | `string?` | Body JSON | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |

### 3. Luồng xử lý kỹ thuật

Kiểm trùng tên chuẩn hóa trên bản active; thêm Specialty active và SaveChanges.

### 4. Đầu ra

200: chuỗi "Đã thêm chuyên khoa".

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:85`

```csharp
public async Task<IActionResult> AddSpecialty([FromBody] SpecialtyRequest req)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.AddSpecialty(req));

```

<a id="updatespecialty"></a>

## PUT `/api/v1/admin/specialty/{id}` — Sửa chuyên khoa

### 1. Chức năng và vai trò

Sửa chuyên khoa. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `specialtyName` | `string` | Body JSON | Bắt buộc. |
| `description` | `string?` | Body JSON | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |

### 3. Luồng xử lý kỹ thuật

Tìm id, chặn inactive; kiểm tên active trùng đối tượng khác, trim và lưu.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:89`

```csharp
public async Task<IActionResult> UpdateSpecialty(int id, [FromBody] SpecialtyRequest req)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.UpdateSpecialty(id, req));

```

<a id="deletespecialty"></a>

## DELETE `/api/v1/admin/specialty/{id}` — Xóa mềm chuyên khoa

### 1. Chức năng và vai trò

Xóa mềm chuyên khoa. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Chặn khi DoctorDetail còn tham chiếu; đổi IsActive=false.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:93`

```csharp
public async Task<IActionResult> DeleteSpecialty(int id)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.DeleteSpecialty(id));

```

<a id="addfacility"></a>

## POST `/api/v1/admin/facility` — Thêm cơ sở y tế

### 1. Chức năng và vai trò

Thêm cơ sở y tế. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `Address` | `string` | Form multipart | Bắt buộc. |
| `Description` | `string?` | Form multipart | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |
| `FacilityName` | `string` | Form multipart | Bắt buộc. |
| `MapUrl` | `string?` | Form multipart | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |
| `Province` | `string?` | Form multipart | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |
| `File` | `IFormFile?` | Form multipart | Tệp theo MIME của nghiệp vụ, tối đa 5 MB. |
| `LicenseFile` | `IFormFile?` | Form multipart | Bắt buộc khi thêm; không bắt buộc tải lại khi sửa. |

### 3. Luồng xử lý kỹ thuật

Kiểm tên active; cần LicenseFile PDF/JPG/PNG, tùy chọn File ảnh; upload, lưu active và mặc định verified vì admin tạo.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:98`

```csharp
public async Task<IActionResult> AddFacility([FromForm] FacilityRequest req)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.AddFacility(req));

```

<a id="updatefacility"></a>

## PUT `/api/v1/admin/facility/{id}` — Sửa cơ sở y tế

### 1. Chức năng và vai trò

Sửa cơ sở y tế. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `Address` | `string` | Form multipart | Bắt buộc. |
| `Description` | `string?` | Form multipart | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |
| `FacilityName` | `string` | Form multipart | Bắt buộc. |
| `MapUrl` | `string?` | Form multipart | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |
| `Province` | `string?` | Form multipart | Không có [Required]; điều kiện theo nhánh nghiệp vụ. |
| `File` | `IFormFile?` | Form multipart | Tệp theo MIME của nghiệp vụ, tối đa 5 MB. |
| `LicenseFile` | `IFormFile?` | Form multipart | Bắt buộc khi thêm; không bắt buộc tải lại khi sửa. |

### 3. Luồng xử lý kỹ thuật

Tìm active, kiểm tên trùng; đổi tên/địa chỉ/mô tả/tỉnh/bản đồ và upload tệp thay thế nếu có.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:102`

```csharp
public async Task<IActionResult> UpdateFacility(int id, [FromForm] FacilityRequest req)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.UpdateFacility(id, req));

```

<a id="verifyfacility"></a>

## PATCH `/api/v1/admin/facility/{id}/verify` — Xác minh cơ sở

### 1. Chức năng và vai trò

Xác minh cơ sở. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Tìm cơ sở đang active và đặt IsVerified=true, SaveChanges.

### 4. Đầu ra

200: chuỗi thông báo. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:106`

```csharp
public async Task<IActionResult> VerifyFacility(int id)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        return Ok(await _adminService.VerifyFacility(id));
    }
    catch (Exception e)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(e.Message);
    }
}

```

<a id="deletefacility"></a>

## DELETE `/api/v1/admin/facility/{id}` — Xóa mềm cơ sở

### 1. Chức năng và vai trò

Xóa mềm cơ sở. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Chặn nếu còn bác sĩ tham chiếu; đặt IsActive=false và lưu.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:119`

```csharp
public async Task<IActionResult> DeleteFacility(int id)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.DeleteFacility(id));

```

<a id="getallusers"></a>

## GET `/api/v1/admin/users` — Tìm và lọc tài khoản

### 1. Chức năng và vai trò

Tìm và lọc tài khoản. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |
| `search` | `string?` | Query | Mặc định null. |
| `role` | `RoleStatus?` | Query | Mặc định null. |
| `isActive` | `bool?` | Query | Mặc định null. |

### 3. Luồng xử lý kỹ thuật

Tìm tên/email/điện thoại; tên dùng unaccent/ILIKE, từ khóa tách AND; lọc role/active, sắp CreatedAt và phân trang, trả DoctorProfileStatus riêng.

### 4. Đầu ra

200: PagedResult<object>.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:124`

```csharp
public async Task<IActionResult> GetAllUsers(
[FromQuery] int page = 0,
[FromQuery] int size = 10,
[FromQuery] string? search = null,
[FromQuery] RoleStatus? role = null,
[FromQuery] bool? isActive = null)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.GetAllUsers(page, size, search, role, isActive));

```

<a id="blockuser"></a>

## PATCH `/api/v1/admin/users/{id}/block` — Khóa tài khoản

### 1. Chức năng và vai trò

Khóa tài khoản. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `reason` | `string` | Query | Theo hợp đồng action. |

### 3. Luồng xử lý kỹ thuật

Kiểm lý do 3–1000 và không ADMIN; khóa theo user, xóa mã/refresh, hủy lịch active và điều chỉnh ca, ghi notification; email trực tiếp sau commit.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:133`

```csharp
public async Task<IActionResult> BlockUser(int id, [FromQuery] string reason)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.BlockUser(id, reason));

```

<a id="unblockuser"></a>

## PATCH `/api/v1/admin/users/{id}/unblock` — Mở khóa trực tiếp

### 1. Chức năng và vai trò

Mở khóa trực tiếp. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `reason` | `string` | Query | Theo hợp đồng action. |

### 3. Luồng xử lý kỹ thuật

Kiểm lý do, không mở user thật sự chưa xác minh; xóa mã/ban reason, bật active, xử lý phiếu pending theo service.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:137`

```csharp
public async Task<IActionResult> UnblockUser(int id, [FromQuery] string reason)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.UnblockUser(id, reason));

```

<a id="getallappointments"></a>

## GET `/api/v1/admin/appointments` — Theo dõi lịch toàn hệ thống

### 1. Chức năng và vai trò

Theo dõi lịch toàn hệ thống. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `dateFrom` | `DateOnly?` | Query | Lọc ngày tạo lịch, không phải ngày khám. |
| `dateTo` | `DateOnly?` | Query | Lọc ngày tạo lịch, không phải ngày khám. |
| `status` | `BookingStatus?` | Query | Trạng thái enum/chuỗi theo luồng xử lý. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |
| `doctorId` | `int?` | Query | Mặc định null. |
| `facilityId` | `int?` | Query | Mặc định null. |
| `appointmentDate` | `DateOnly?` | Query | Mặc định null. Ngày YYYY-MM-DD; ý nghĩa theo endpoint. |
| `specialtyId` | `int?` | Query | Mặc định null. |
| `workDateFrom` | `DateOnly?` | Query | Mặc định null. Ngày YYYY-MM-DD; ý nghĩa theo endpoint. |
| `workDateTo` | `DateOnly?` | Query | Mặc định null. Ngày YYYY-MM-DD; ý nghĩa theo endpoint. |

### 3. Luồng xử lý kỹ thuật

Ghép filter: dateFrom/dateTo theo CreatedAt; appointmentDate/workDateFrom/workDateTo theo ngày khám; status/bác sĩ/cơ sở/chuyên khoa; phân trang.

### 4. Đầu ra

200: PagedResult<AppointmentAdminResponse>.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:142`

```csharp
public async Task<IActionResult> GetAllAppointments(
    [FromQuery] DateOnly? dateFrom,
    [FromQuery] DateOnly? dateTo,
    [FromQuery] BookingStatus? status,
    [FromQuery] int page = 0,
    [FromQuery] int size = 10,
    [FromQuery] int? doctorId = null,
    [FromQuery] int? facilityId = null,
    [FromQuery] DateOnly? appointmentDate = null,
    [FromQuery] int? specialtyId = null,
    [FromQuery] DateOnly? workDateFrom = null,
    [FromQuery] DateOnly? workDateTo = null)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _adminService.GetAllAppointments(dateFrom, dateTo, status, page, size, doctorId, facilityId, appointmentDate, specialtyId, workDateFrom, workDateTo));
}

```

<a id="getallreviews"></a>

## GET `/api/v1/admin/reviews` — Hàng đợi đánh giá và gợi ý

### 1. Chức năng và vai trò

Hàng đợi đánh giá và gợi ý. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |
| `category` | `string?` | Query | Mặc định null. visible, hidden, profane, spam, advertising. |
| `search` | `string?` | Query | Mặc định null. |

### 3. Luồng xử lý kỹ thuật

Nạp review, chạy classifier vào AiLabels, lọc category/search rồi phân trang; clamp page/size trong service.

### 4. Đầu ra

200: PagedResult<ReviewAdminResponse>.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:160`

```csharp
public async Task<IActionResult> GetAllReviews(
    [FromQuery] int page = 0,
    [FromQuery] int size = 10, [FromQuery] string? category = null, [FromQuery] string? search = null)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _adminService.GetAllReviews(page, size, category, search));
}

```

<a id="rejectreview"></a>

## PATCH `/api/v1/admin/reviews/{id}/hide` — Đổi trạng thái ẩn/hiện đánh giá

### 1. Chức năng và vai trò

Đổi trạng thái ẩn/hiện đánh giá. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Đọc review; toggle IsVisible (false→true, còn lại→false), lưu rồi tính lại tổng sao từ mọi review bác sĩ.

Đây là toggle: gọi lại có thể hiện lại nội dung; sao vẫn được tính.

### 4. Đầu ra

200: chuỗi cho biết đã ẩn hoặc hiện lại.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:168`

```csharp
public async Task<IActionResult> RejectReview(int id)
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
=> Ok(await _adminService.RejectReview(id));

```

<a id="analyzecomment"></a>

## POST `/api/v1/admin/moderation/analyze` — Phân tích một bình luận

### 1. Chức năng và vai trò

Phân tích một bình luận. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `comment` | `string` | Body JSON | Bắt buộc; bình luận không trắng. |

### 3. Luồng xử lý kỹ thuật

Nhận dictionary body, bắt buộc khóa comment không trắng; gọi Naive Bayes cục bộ để trả điểm bốn nhãn.

### 4. Đầu ra

200: {comment, labels}; comment thiếu/trắng trả 400 {error}.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:173`

```csharp
public IActionResult AnalyzeComment([FromBody] Dictionary<string, string> request)
{
    if (!request.TryGetValue("comment", out var commentText) || string.IsNullOrWhiteSpace(commentText))
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(new { error = "Bình luận không hợp lệ" });

    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    var results = _adminService.AnalyzeComment(commentText);
    // Trả HTTP 200 với kết quả cho client.
    return Ok(new { comment = commentText, labels = results });
}

```

<a id="getcredentials"></a>

## GET `/api/v1/admin/credentials` — Danh sách yêu cầu đổi giấy tờ

### 1. Chức năng và vai trò

Danh sách yêu cầu đổi giấy tờ. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `status` | `string` | Query | Mặc định "PENDING". Trạng thái enum/chuỗi theo luồng xử lý. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |

### 3. Luồng xử lý kỹ thuật

Parse status theo TransferStatus, có Enum.IsDefined; service lọc, clamp page/size và trả cũ/mới, tài liệu, thời gian/note.

### 4. Đầu ra

200: PagedResult<object>; status sai trả 400 chuỗi.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:183`

```csharp
public async Task<IActionResult> GetCredentials([FromQuery] string status = "PENDING", [FromQuery] int page = 0, [FromQuery] int size = 10)
{
    // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
    if (!Enum.TryParse<TransferStatus>(status, out var parsed) || !Enum.IsDefined(parsed)) return BadRequest("Trạng thái không hợp lệ");
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _doctorService.GetCredentialRequests(parsed, page, size));
}

```

<a id="approvecredential"></a>

## PUT `/api/v1/admin/credentials/{id}/approve` — Duyệt đổi học vị/giấy tờ

### 1. Chức năng và vai trò

Duyệt đổi học vị/giấy tờ. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `adminNote` | `string?` | Body JSON | Bắt buộc khi từ chối đổi giấy tờ; nhánh khác theo service. |

### 3. Luồng xử lý kỹ thuật

Chỉ PENDING; kiểm bác sĩ APPROVED và dữ liệu hiện tại khớp OldDegree/OldDocumentUrl; cập nhật trường phù hợp, trạng thái/thời điểm và notification cùng save.

### 4. Đầu ra

200: chuỗi "Đã duyệt thay đổi".

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:190`

```csharp
public async Task<IActionResult> ApproveCredential(int id, [FromBody] ProcessTransferRequest req)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    await _doctorService.ProcessCredentialRequest(id, true, req.AdminNote);
    // Trả HTTP 200 với kết quả cho client.
    return Ok("Đã duyệt thay đổi");
}

```

<a id="rejectcredential"></a>

## PUT `/api/v1/admin/credentials/{id}/reject` — Từ chối đổi học vị/giấy tờ

### 1. Chức năng và vai trò

Từ chối đổi học vị/giấy tờ. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `adminNote` | `string?` | Body JSON | Bắt buộc khi từ chối đổi giấy tờ; nhánh khác theo service. |

### 3. Luồng xử lý kỹ thuật

Yêu cầu AdminNote không trắng; chỉ PENDING; ghi REJECTED/ProcessedAt/note, notification.

### 4. Đầu ra

200: chuỗi "Đã từ chối thay đổi".

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:197`

```csharp
public async Task<IActionResult> RejectCredential(int id, [FromBody] ProcessTransferRequest req)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    await _doctorService.ProcessCredentialRequest(id, false, req.AdminNote);
    // Trả HTTP 200 với kết quả cho client.
    return Ok("Đã từ chối thay đổi");
}

```

<a id="gettransferrequests"></a>

## GET `/api/v1/admin/transfers` — Danh sách đề nghị chuyển công tác

### 1. Chức năng và vai trò

Danh sách đề nghị chuyển công tác. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `status` | `string` | Query | Mặc định "PENDING". Trạng thái enum/chuỗi theo luồng xử lý. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |

### 3. Luồng xử lý kỹ thuật

Parse status không phân biệt hoa thường rồi gọi DoctorService lọc/phân trang.

### 4. Đầu ra

200: PagedResult<object>; parse thất bại trả 400 chuỗi.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:204`

```csharp
public async Task<IActionResult> GetTransferRequests(
    [FromQuery] string status = "PENDING",
    [FromQuery] int page = 0,
    [FromQuery] int size = 10)
{
    if (Enum.TryParse<TransferStatus>(status, true, out var transferStatus))
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        return Ok(await _doctorService.GetTransferRequests(transferStatus, page, size));
    }
    // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
    return BadRequest("Trạng thái không hợp lệ");
}

```

<a id="approvetransfer"></a>

## PUT `/api/v1/admin/transfers/{id}/approve` — Duyệt chuyển cơ sở

### 1. Chức năng và vai trò

Duyệt chuyển cơ sở. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `adminNote` | `string?` | Body JSON | Bắt buộc khi từ chối đổi giấy tờ; nhánh khác theo service. |

### 3. Luồng xử lý kỹ thuật

Chỉ PENDING; đổi cơ sở bác sĩ, ghi APPROVED/note/ProcessedAt và đóng ca AVAILABLE tương lai theo code; BOOKED không tự chuyển.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:218`

```csharp
public async Task<IActionResult> ApproveTransfer(int id, [FromBody] ProcessTransferRequest req)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    await _doctorService.ApproveTransfer(id, req.AdminNote);
    // Trả HTTP 200 với kết quả cho client.
    return Ok("Đã duyệt chuyển công tác thành công!");
}

```

<a id="rejecttransfer"></a>

## PUT `/api/v1/admin/transfers/{id}/reject` — Từ chối chuyển cơ sở

### 1. Chức năng và vai trò

Từ chối chuyển cơ sở. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `adminNote` | `string?` | Body JSON | Bắt buộc khi từ chối đổi giấy tờ; nhánh khác theo service. |

### 3. Luồng xử lý kỹ thuật

Chỉ PENDING; ghi REJECTED, note và thời điểm, không đổi FacilityId của bác sĩ; notification theo collector.

### 4. Đầu ra

200: chuỗi thông báo.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminApiController.cs:225`

```csharp
public async Task<IActionResult> RejectTransfer(int id, [FromBody] ProcessTransferRequest req)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    await _doctorService.RejectTransfer(id, req.AdminNote);
    // Trả HTTP 200 với kết quả cho client.
    return Ok("Đã từ chối chuyển công tác!");
}

```
