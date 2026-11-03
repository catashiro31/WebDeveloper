# Tra cứu công khai

<a id="getpricerange"></a>

## GET `/api/v1/portal/doctors/price-range` — Khoảng giá cho bộ lọc

### 1. Chức năng và vai trò

Khoảng giá cho bộ lọc. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

Không có dữ liệu đầu vào từ client.

### 3. Luồng xử lý kỹ thuật

PublicService lấy giá lớn nhất từ bác sĩ APPROVED; min cố định bằng 0.

### 4. Đầu ra

200: {min: 0, max: số}; không có giá phù hợp thì max=0.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:19`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetPriceRange() => Ok(new { min = 0, max = await _publicService.GetMaxDoctorPrice() });

```

<a id="getstats"></a>

## GET `/api/v1/portal/stats` — Thống kê cổng thông tin

### 1. Chức năng và vai trò

Thống kê cổng thông tin. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

Không có dữ liệu đầu vào từ client.

### 3. Luồng xử lý kỹ thuật

Đếm bác sĩ APPROVED, tổng Appointment và điểm trung bình; MemoryCache trong tiến trình giữ 30 phút.

### 4. Đầu ra

200: PortalStatsResponse.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:22`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetStats() => Ok(await _publicService.GetPortalStats());

```

<a id="getdoctors"></a>

## GET `/api/v1/portal/doctors` — Tìm và phân trang bác sĩ

### 1. Chức năng và vai trò

Tìm và phân trang bác sĩ. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `keyword` | `string?` | Query | Có thể bỏ. |
| `specId` | `int?` | Query | Có thể bỏ. |
| `facilityId` | `int?` | Query | Có thể bỏ. |
| `province` | `string?` | Query | Có thể bỏ. |
| `minPrice` | `double?` | Query | Có thể bỏ. |
| `maxPrice` | `double?` | Query | Có thể bỏ. |
| `sortBy` | `string?` | Query | rating, priceAsc, priceDesc, experience. |
| `page` | `int` | Query | Mặc định 0. Trang bắt đầu từ 0. |
| `size` | `int` | Query | Mặc định 10. Số mục/trang; giới hạn theo từng endpoint. |

### 3. Luồng xử lý kỹ thuật

Bắt đầu từ APPROVED, ghép filter. Tách từng từ khóa AND trên tên bác sĩ và chuyên khoa, unaccent/ILIKE và escape wildcard; Count/Skip/Take; trả card DTO.

### 4. Đầu ra

200: PagedResult<DoctorCardResponse>.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:26`

```csharp
public async Task<IActionResult> GetDoctors(
    [FromQuery] string? keyword,
    [FromQuery] int? specId,
    [FromQuery] int? facilityId,
    [FromQuery] string? province,
    [FromQuery] double? minPrice,
    [FromQuery] double? maxPrice,
    [FromQuery] string? sortBy,
    [FromQuery] int page = 0,
    [FromQuery] int size = 10)
{
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _publicService.GetDoctors(keyword, specId, facilityId, province, minPrice, maxPrice, sortBy, page, size));
}

```

<a id="getdoctordetail"></a>

## GET `/api/v1/portal/doctors/{id}` — Chi tiết bác sĩ công khai

### 1. Chức năng và vai trò

Chi tiết bác sĩ công khai. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Tra DoctorDetail theo id và điều kiện công khai trong PublicService, ghép user/chuyên khoa/cơ sở thành DTO.

### 4. Đầu ra

200: DoctorDetailPublicResponse.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:41`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetDoctorDetail(int id) => Ok(await _publicService.GetDoctorById(id));

```

<a id="getdoctorreviews"></a>

## GET `/api/v1/portal/doctors/{id}/reviews` — Đánh giá công khai của bác sĩ

### 1. Chức năng và vai trò

Đánh giá công khai của bác sĩ. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `id` | `int` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Truy review theo bác sĩ, sắp mới trước và ánh xạ DoctorReviewResponse.

Code hiện vẫn trả Comment khi IsVisible=false; chưa che nội dung tại API.

### 4. Đầu ra

200: List<DoctorReviewResponse>, không bọc PagedResult.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:44`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetDoctorReviews(int id) => Ok(await _publicService.GetReviewsByDoctorId(id));

```

<a id="getavailableslots"></a>

## GET `/api/v1/portal/doctors/{id}/slots` — Ca trống theo bác sĩ và ngày

### 1. Chức năng và vai trò

Ca trống theo bác sĩ và ngày. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `id` | `int` | Path | Định danh đối tượng trong route. |
| `date` | `DateOnly` | Query | Ngày YYYY-MM-DD; ý nghĩa theo endpoint. |

### 3. Luồng xử lý kỹ thuật

Kiểm trong hôm nay..+30 ngày Việt Nam và bác sĩ APPROVED; lấy ca AVAILABLE, loại ca hôm nay đã bắt đầu.

### 4. Đầu ra

200: List<DoctorSlotResponse>; ngoài cửa sổ hoặc chưa duyệt có thể trả [].

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:47`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetAvailableSlots(int id, [FromQuery] DateOnly date) => Ok(await _publicService.GetAvailableSlots(id, date));

```

<a id="getallfacilities"></a>

## GET `/api/v1/portal/facilities` — Danh sách cơ sở hoạt động

### 1. Chức năng và vai trò

Danh sách cơ sở hoạt động. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

Không có dữ liệu đầu vào từ client.

### 3. Luồng xử lý kỹ thuật

CatalogQueries lọc IsActive, sắp theo id và chiếu sang FacilityResponse, đọc lại database mỗi request.

### 4. Đầu ra

200: List<FacilityResponse>.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:51`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetAllFacilities() => Ok(await _publicService.GetAllFacilities());

```

<a id="getallspecialties"></a>

## GET `/api/v1/portal/specialties` — Danh sách chuyên khoa hoạt động

### 1. Chức năng và vai trò

Danh sách chuyên khoa hoạt động. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

Không có dữ liệu đầu vào từ client.

### 3. Luồng xử lý kỹ thuật

CatalogQueries lọc IsActive==true, sắp id và chọn dữ liệu cần hiển thị.

### 4. Đầu ra

200: List<SpecialtyResponse>.

### 5. Mã code

`WebDeveloper/Controllers/Api/PublicApiController.cs:55`

```csharp
// Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
public async Task<IActionResult> GetAllSpecialties() => Ok(await _publicService.GetAllSpecialties());

```
