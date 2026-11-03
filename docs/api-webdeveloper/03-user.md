# Tài khoản hiện tại

<a id="getprofile"></a>

## GET `/api/v1/user/profile` — Xem tài khoản hiện tại

### 1. Chức năng và vai trò

Xem tài khoản hiện tại. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |

### 3. Luồng xử lý kỹ thuật

SecurityHelper lấy người dùng từ danh tính request; UserService trả thông tin tài khoản qua DTO.

### 4. Đầu ra

200: UserProfileResponse.

### 5. Mã code

`WebDeveloper/Controllers/Api/UserApiController.cs:32`

```csharp
public async Task<IActionResult> GetProfile()
{
    // Lấy tài khoản hiện tại từ danh tính đã xác thực.
    var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    return Ok(await _userService.GetProfile(user));
}

```

<a id="updateprofile"></a>

## PUT `/api/v1/user/profile` — Cập nhật tài khoản và avatar

### 1. Chức năng và vai trò

Cập nhật tài khoản và avatar. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `FullName` | `string` | Form multipart | Bắt buộc; 2–60 ký tự chữ và khoảng trắng. |
| `PhoneNumber` | `string?` | Form multipart | Nếu có: điện thoại di động Việt Nam 10 chữ số hợp lệ. |
| `File` | `IFormFile?` | Form multipart | Tệp theo MIME của nghiệp vụ, tối đa 5 MB. |

### 3. Luồng xử lý kỹ thuật

Nhận multipart, kiểm tên/điện thoại và avatar JPG/PNG ≤5 MB; upload nếu có, lưu User. Controller cấp lại access cookie vì tên/avatar nằm trong JWT.

### 4. Đầu ra

200: UserProfileResponse; kèm Set-Cookie accessToken.

### 5. Mã code

`WebDeveloper/Controllers/Api/UserApiController.cs:39`

```csharp
public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest req)
{
    // Lấy tài khoản hiện tại từ danh tính đã xác thực.
    var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    var profile = await _userService.UpdateProfile(user, req);
    // Gửi cookie phiên về trình duyệt.
    Response.Cookies.Append("accessToken", _tokens.CreateToken(user), new CookieOptions
    {
        HttpOnly = true,
        Secure = _cookieSettings.Secure,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddMinutes(15)
    });
    // Trả HTTP 200 với kết quả cho client.
    return Ok(profile);
}

```

<a id="changepassword"></a>

## PUT `/api/v1/user/password` — Đổi mật khẩu với mật khẩu cũ

### 1. Chức năng và vai trò

Đổi mật khẩu với mật khẩu cũ. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |
| `oldPassword` | `string` | Body JSON | Bắt buộc. |
| `newPassword` | `string` | Body JSON | Bắt buộc; ≥6 ký tự, có chữ hoa, chữ thường và số. |

### 3. Luồng xử lý kỹ thuật

Kiểm OldPassword bằng BCrypt, NewPassword hợp lệ và khác cũ; lưu hash mới.

### 4. Đầu ra

200: {message: "Đổi mật khẩu thành công!"}.

### 5. Mã code

`WebDeveloper/Controllers/Api/UserApiController.cs:56`

```csharp
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
{
    // Lấy tài khoản hiện tại từ danh tính đã xác thực.
    var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
    // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
    await _userService.ChangePassword(user, req);
    // Trả HTTP 200 với kết quả cho client.
    return Ok(new { Message = "Đổi mật khẩu thành công!" });
}

```

<a id="getuploadsignature"></a>

## GET `/api/v1/user/upload-signature` — Lấy chữ ký upload

### 1. Chức năng và vai trò

Lấy chữ ký upload. **Vai trò sử dụng:** Đã đăng nhập.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Phiên hợp lệ; đúng vai trò của API. |

### 3. Luồng xử lý kỹ thuật

IFileUploadService tạo chữ ký từ tham số upload và cấu hình Cloudinary. Tham số FromServices do DI cung cấp, không do client gửi.

### 4. Đầu ra

200: CloudinarySignatureResponse.

### 5. Mã code

`WebDeveloper/Controllers/Api/UserApiController.cs:64`

```csharp
public IActionResult GetUploadSignature([FromServices] IFileUploadService fileUploadService)
{
    // Ký tham số upload, không trả API secret.
    var signature = fileUploadService.GenerateUploadSignature();
    // Trả HTTP 200 với kết quả cho client.
    return Ok(signature);
}

```
