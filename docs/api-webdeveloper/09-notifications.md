# Thông báo

<a id="list"></a>

## GET `/api/v1/notifications` — Tải hộp thông báo

### 1. Chức năng và vai trò

Tải hộp thông báo. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `before` | `long?` | Query | Cursor: chỉ lấy thông báo có Id nhỏ hơn giá trị này. |

### 3. Luồng xử lý kỹ thuật

Lấy UserId từ claim; đếm chưa đọc, lọc Id<before nếu có, sắp Id giảm, lấy 21 để trả tối đa 20 và hasMore.

### 4. Đầu ra

200: {unreadCount, items, hasMore}; mỗi item id/title/message/url/createdAt/readAt.

### 5. Mã code

`WebDeveloper/Controllers/Api/NotificationsController.cs:17`

```csharp
public async Task<IActionResult> List([FromQuery] long? before, CancellationToken ct)
{
    var own = db.Notifications.AsNoTracking().Where(n => n.UserId == UserId);
    // Đếm thông báo chưa đọc của tài khoản hiện tại.
    var unreadCount = await own.CountAsync(n => n.ReadAt == null, ct);
    var items = await own.Where(n => !before.HasValue || n.Id < before.Value).OrderByDescending(n => n.Id)
        .Select(n => new { n.Id, n.Title, n.Message, Url = n.Url == "/Admin/Doctors" && n.Title == "Có hồ sơ bác sĩ cần duyệt" ? "/Admin/Doctors?status=PENDING" : n.Url, n.CreatedAt, n.ReadAt }).Take(21).ToListAsync(ct);
    // Trả HTTP 200 với kết quả cho client.
    return Ok(new { unreadCount, items = items.Take(20), hasMore = items.Count > 20 });
}

```

<a id="read"></a>

## PATCH `/api/v1/notifications/{id:long}/read` — Đánh dấu một thông báo đã đọc

### 1. Chức năng và vai trò

Đánh dấu một thông báo đã đọc. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `long` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Lọc đồng thời UserId/Id; không tìm thấy trả 404; chỉ hàng ReadAt null được ExecuteUpdate thành UTC hiện tại.

### 4. Đầu ra

204 No Content; không có body JSON. Nhánh lỗi trong action: 404.

### 5. Mã code

`WebDeveloper/Controllers/Api/NotificationsController.cs:27`

```csharp
public async Task<IActionResult> Read(long id, CancellationToken ct)
{
    var own = db.Notifications.Where(n => n.UserId == UserId && n.Id == id);
    // Không có bản ghi phù hợp: HTTP 404.
    if (!await own.AnyAsync(ct)) return NotFound();
    // Cập nhật trực tiếp các bản ghi thỏa điều kiện.
    await own.Where(n => n.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    // Trả HTTP 204, không có body.
    return NoContent();
}

```

<a id="deleteread"></a>

## DELETE `/api/v1/notifications/read` — Xóa toàn bộ thông báo đã đọc

### 1. Chức năng và vai trò

Xóa toàn bộ thông báo đã đọc. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |

### 3. Luồng xử lý kỹ thuật

ExecuteDelete với UserId hiện tại và ReadAt!=null; không xóa các thông báo chưa đọc.

### 4. Đầu ra

200: {deleted: số hàng đã xóa}.

### 5. Mã code

`WebDeveloper/Controllers/Api/NotificationsController.cs:36`

```csharp
public async Task<IActionResult> DeleteRead(CancellationToken ct)
{
    // Xóa trực tiếp các bản ghi thỏa điều kiện.
    var deleted = await db.Notifications.Where(n => n.UserId == UserId && n.ReadAt != null).ExecuteDeleteAsync(ct);
    // Trả HTTP 200 với kết quả cho client.
    return Ok(new { deleted });
}

```

<a id="delete"></a>

## DELETE `/api/v1/notifications/{id:long}` — Xóa một thông báo đã đọc

### 1. Chức năng và vai trò

Xóa một thông báo đã đọc. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `id` | `long` | Path | Định danh đối tượng trong route. |

### 3. Luồng xử lý kỹ thuật

Chỉ ExecuteDelete khi UserId/Id khớp và đã đọc; 0 hàng thì NotFound.

### 4. Đầu ra

204 nếu xóa được; 404 nếu không có/khác chủ/chưa đọc.

### 5. Mã code

`WebDeveloper/Controllers/Api/NotificationsController.cs:43`

```csharp
public async Task<IActionResult> Delete(long id, CancellationToken ct)
{
    // Xóa trực tiếp các bản ghi thỏa điều kiện.
    var deleted = await db.Notifications.Where(n => n.UserId == UserId && n.Id == id && n.ReadAt != null).ExecuteDeleteAsync(ct);
    return deleted == 0 ? NotFound() : NoContent();
}

```

<a id="readall"></a>

## PATCH `/api/v1/notifications/read-all` — Đánh dấu tất cả đã đọc

### 1. Chức năng và vai trò

Đánh dấu tất cả đã đọc. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |

### 3. Luồng xử lý kỹ thuật

ExecuteUpdate các hàng của user còn ReadAt null thành DateTime.UtcNow.

### 4. Đầu ra

204 No Content.

### 5. Mã code

`WebDeveloper/Controllers/Api/NotificationsController.cs:50`

```csharp
public async Task<IActionResult> ReadAll(CancellationToken ct)
{
    await db.Notifications.Where(n => n.UserId == UserId && n.ReadAt == null)
        // Cập nhật trực tiếp các bản ghi thỏa điều kiện.
        .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    // Trả HTTP 204, không có body.
    return NoContent();
}

```
