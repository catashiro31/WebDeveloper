# Xác thực và quản lý phiên

<a id="signin"></a>

## POST `/api/v1/auth/signin` — Đăng nhập

### 1. Chức năng và vai trò

Đăng nhập. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `email` | `string` | Body JSON | Email hợp lệ. |
| `password` | `string` | Body JSON | Bắt buộc. |

### 3. Luồng xử lý kỹ thuật

AuthService kiểm email, BCrypt.Verify và trạng thái tài khoản; tạo JWT/refresh, lưu refresh. Controller ghi cookie access 15 phút và refresh 7 ngày, HttpOnly/SameSite Strict; Secure lấy từ AuthCookieSettings.

Cookie access 15 phút; thời hạn JWT lấy từ cấu hình riêng.

### 4. Đầu ra

200: SignInResponse gồm token="SECURE_HTTPONLY_COOKIE", refreshToken="", message và user. Token thật chỉ nằm trong cookie. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:23`

```csharp
public async Task<IActionResult> SignIn([FromBody] SignInRequest req)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        var res = await _authService.SignIn(req);

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15) 
        };
        // Gửi cookie phiên về trình duyệt.
        Response.Cookies.Append("accessToken", res.Token, accessCookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        // Gửi cookie phiên về trình duyệt.
        Response.Cookies.Append("refreshToken", res.RefreshToken, refreshCookieOptions);

        res.Token = "SECURE_HTTPONLY_COOKIE";
        res.RefreshToken = "";

        // Trả HTTP 200 với kết quả cho client.
        return Ok(res);
    }
    catch (Exception ex)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(new { Message = ex.Message });
    }
}

```

<a id="refreshtoken"></a>

## POST `/api/v1/auth/refresh-token` — Gia hạn phiên chủ động

### 1. Chức năng và vai trò

Gia hạn phiên chủ động. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `refreshToken` | `string` | Cookie | Bắt buộc; còn hạn, khớp token lưu và user active. |

### 3. Luồng xử lý kỹ thuật

Đọc refreshToken từ cookie; AuthService kiểm active/hạn và xoay refresh bằng cập nhật có điều kiện. Ghi lại hai cookie.

### 4. Đầu ra

200: {message: "Đã gia hạn phiên đăng nhập thành công."}; kèm Set-Cookie. Nhánh lỗi trong action: 401.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:60`

```csharp
public async Task<IActionResult> RefreshToken()
{
    try
    {
        // Đọc credential từ cookie của request.
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            // Credential không hợp lệ: HTTP 401.
            return Unauthorized(new { Message = "Tokens are missing." });
        }

        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        var res = await _authService.RefreshToken(refreshToken);

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };
        // Gửi cookie phiên về trình duyệt.
        Response.Cookies.Append("accessToken", res.Token, accessCookieOptions);

        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.Secure,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        // Gửi cookie phiên về trình duyệt.
        Response.Cookies.Append("refreshToken", res.RefreshToken, refreshCookieOptions);

        // Trả HTTP 200 với kết quả cho client.
        return Ok(new { Message = "Đã gia hạn phiên đăng nhập thành công." });
    }
    catch (Exception ex)
    {
        // Credential không hợp lệ: HTTP 401.
        return Unauthorized(new { Message = ex.Message });
    }
}

```

<a id="signup"></a>

## POST `/api/v1/auth/signup` — Đăng ký tài khoản

### 1. Chức năng và vai trò

Đăng ký tài khoản. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `email` | `string` | Body JSON | Email hợp lệ. |
| `password` | `string` | Body JSON | Bắt buộc; ≥6 ký tự, có chữ hoa, chữ thường và số. |
| `fullName` | `string` | Body JSON | Bắt buộc; 2–60 ký tự chữ và khoảng trắng. |
| `phoneNumber` | `string` | Body JSON | Bắt buộc; điện thoại di động Việt Nam 10 chữ số hợp lệ. |
| `role` | `Enums.RoleStatus` | Body JSON | PATIENT hoặc DOCTOR; không cho tự đăng ký ADMIN. |

### 3. Luồng xử lý kỹ thuật

Kiểm dữ liệu, không cho tự đăng ký ADMIN; băm BCrypt, tạo User inactive và mã xác minh hạn 24 giờ. User và hàng EmailOutbox cùng lần lưu.

### 4. Đầu ra

200: object {message}; chưa cấp phiên đăng nhập. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:100`

```csharp
public async Task<IActionResult> SignUp([FromBody] SignUpRequest req)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        var msg = await _authService.SignUp(req);
        // Trả HTTP 200 với kết quả cho client.
        return Ok(new { Message = msg });
    }
    catch (Exception ex)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(new { Message = ex.Message });
    }
}

```

<a id="signoutapp"></a>

## POST `/api/v1/auth/signout` — Đăng xuất

### 1. Chức năng và vai trò

Đăng xuất. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `accessToken / Authorization` | `JWT` | Cookie / Bearer header | Token phiên cần thu hồi nếu có. |
| `refreshToken` | `string` | Cookie | Xóa refresh khớp nếu có. |

### 3. Luồng xử lý kỹ thuật

Lấy access từ cookie trước rồi Bearer header; AuthService ghi hash token hợp lệ vào blacklist và xóa refresh khớp. Sau lưu, controller yêu cầu xóa cookie accessToken, refreshToken, user_info.

### 4. Đầu ra

200: {message: "Đăng xuất thành công"}.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:114`

```csharp
public async Task<IActionResult> SignOutApp()
{
    // Đọc credential từ cookie của request.
    var token = Request.Cookies["accessToken"];

    if (string.IsNullOrEmpty(token))
    {
        // Đọc Bearer header nếu không có cookie.
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            token = authHeader["Bearer ".Length..];
        }
    }

    // Đọc credential từ cookie của request.
    await _authService.SignOut(token, Request.Cookies["refreshToken"]);
    // Yêu cầu trình duyệt xóa cookie phiên.
    Response.Cookies.Delete("accessToken");
    Response.Cookies.Delete("refreshToken");
    Response.Cookies.Delete("user_info");
    // Trả HTTP 200 với kết quả cho client.
    return Ok(new { Message = "Đăng xuất thành công" });
}

```

<a id="verifyaccount"></a>

## GET `/api/v1/auth/verify` — Xác minh email

### 1. Chức năng và vai trò

Xác minh email. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `email` | `string` | Query | Theo hợp đồng action. |
| `code` | `string` | Query | Theo hợp đồng action. |

### 3. Luồng xử lý kỹ thuật

AuthService kiểm email/code/hạn, trạng thái active và bị ban; thành công bật active, xóa mã; PATIENT được tạo hồ sơ SELF.

### 4. Đầu ra

200: object {message}. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:136`

```csharp
public async Task<IActionResult> VerifyAccount([FromQuery] string email, [FromQuery] string code)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        var result = await _authService.VerifyAccount(email, code);
        // Trả HTTP 200 với kết quả cho client.
        return Ok(new { Message = result });
    }
    catch (Exception ex)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(new { Message = ex.Message });
    }
}

```

<a id="forgotpassword"></a>

## POST `/api/v1/auth/forgot-password` — Yêu cầu khôi phục mật khẩu

### 1. Chức năng và vai trò

Yêu cầu khôi phục mật khẩu. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `email` | `string` | Query | Theo hợp đồng action. |

### 3. Luồng xử lý kỹ thuật

Nhận email qua query; AuthService chỉ cho user hoạt động, tạo mã hạn 15 phút và gửi thư trực tiếp SMTP.

### 4. Đầu ra

200: object {message}. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:150`

```csharp
public async Task<IActionResult> ForgotPassword([FromQuery] string email)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        var result = await _authService.ForgotPassword(email);
        // Trả HTTP 200 với kết quả cho client.
        return Ok(new { Message = result });
    }
    catch (Exception ex)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(new { Message = ex.Message });
    }
}

```

<a id="resetpassword"></a>

## POST `/api/v1/auth/reset-password` — Đặt lại mật khẩu

### 1. Chức năng và vai trò

Đặt lại mật khẩu. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `email` | `string` | Body JSON | Email hợp lệ. |
| `code` | `string` | Body JSON | Bắt buộc. |
| `newPassword` | `string` | Body JSON | Bắt buộc; ≥6 ký tự, có chữ hoa, chữ thường và số. |

### 3. Luồng xử lý kỹ thuật

Kiểm email/code/hạn và mật khẩu mới; ghi BCrypt hash rồi xóa mã một lần.

### 4. Đầu ra

200: object {message}. Nhánh lỗi trong action: 400.

### 5. Mã code

`WebDeveloper/Controllers/Api/AuthApiController.cs:164`

```csharp
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
{
    try
    {
        // Gọi service thực hiện nghiệp vụ và kiểm tra điều kiện.
        var result = await _authService.ResetPassword(req);
        // Trả HTTP 200 với kết quả cho client.
        return Ok(new { Message = result });
    }
    catch (Exception ex)
    {
        // Từ chối dữ liệu hoặc trạng thái không hợp lệ: HTTP 400.
        return BadRequest(new { Message = ex.Message });
    }
}

```
