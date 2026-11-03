# Danh mục quản trị

<a id="specialties"></a>

## GET `/api/v1/admin/specialties` — Danh mục chuyên khoa cho admin

### 1. Chức năng và vai trò

Danh mục chuyên khoa cho admin. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `search` | `string?` | Query | Có thể bỏ. |

### 3. Luồng xử lý kỹ thuật

Đọc active qua CatalogQueries, tìm tên không dấu/hoa thường, sắp tên; canDelete dựa việc có bác sĩ tham chiếu.

### 4. Đầu ra

200: array {id, name, description, canDelete}.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminCatalogController.cs:15`

```csharp
public async Task<IActionResult> Specialties(string? search)
{
    // Trả HTTP 200 với kết quả cho client.
    return Ok(await db.ActiveSpecialties(search)
        .OrderBy(s => s.SpecialtyName).Select(s => new { id = s.SpecialtyId, name = s.SpecialtyName, s.Description, canDelete = !db.DoctorDetails.Any(d => d.SpecialtyId == s.SpecialtyId) }).ToListAsync());
}

```

<a id="facilities"></a>

## GET `/api/v1/admin/facilities` — Danh mục cơ sở cho admin

### 1. Chức năng và vai trò

Danh mục cơ sở cho admin. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `search` | `string?` | Query | Mặc định null. |
| `province` | `string?` | Query | Mặc định null. |

### 3. Luồng xử lý kỹ thuật

CatalogQueries lọc active, tên tìm kiếm và tỉnh; sắp tên, dùng projection FacilityResponse chung.

### 4. Đầu ra

200: List<FacilityResponse>.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminCatalogController.cs:22`

```csharp
public async Task<IActionResult> Facilities([FromQuery] string? search = null, [FromQuery] string? province = null)
{
    // Trả HTTP 200 với kết quả cho client.
    return Ok(await db.ActiveFacilities(search, province)
        .OrderBy(f => f.FacilityName).ToFacilityResponses().ToListAsync());
}

```

<a id="doctorscatalog"></a>

## GET `/api/v1/admin/doctors/catalog` — Danh mục bác sĩ dùng cho bộ lọc

### 1. Chức năng và vai trò

Danh mục bác sĩ dùng cho bộ lọc. **Vai trò sử dụng:** ADMIN.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |

### 3. Luồng xử lý kỹ thuật

Query trực tiếp DbContext, lấy hồ sơ APPROVED, sắp theo tên user, chọn id/tên/chuyên khoa/cơ sở.

### 4. Đầu ra

200: array {id, name, specialty, facility}.

### 5. Mã code

`WebDeveloper/Controllers/Api/AdminCatalogController.cs:29`

```csharp
public async Task<IActionResult> DoctorsCatalog()
{
    // Trả HTTP 200 với kết quả cho client.
    return Ok(await db.DoctorDetails.AsNoTracking()
        .Where(d => d.VerificationStatus == VerificationStatus.APPROVED)
        .OrderBy(d => d.User.FullName)
        .Select(d => new { id = d.DoctorId, name = d.User.FullName, specialty = d.Specialty.SpecialtyName, facility = d.Facility.FacilityName })
        .ToListAsync());
}

```
