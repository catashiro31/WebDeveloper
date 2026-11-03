# Gửi lại xác minh email

<a id="resend"></a>

## POST `/api/v1/auth/resend-verification` — Gửi lại email xác minh

### 1. Chức năng và vai trò

Gửi lại email xác minh. **Vai trò sử dụng:** Công khai.

### 2. Đầu vào

| Trường | Kiểu | Vị trí | Điều kiện |
|---|---|---|---|
| `email` | `string` | Body JSON | Email hợp lệ. |

### 3. Luồng xử lý kỹ thuật

Trả message chung khi user không có/không chờ xác minh/cooldown. User hợp lệ được khóa theo tài khoản, kiểm hàng Verification trong phút gần nhất, tạo mã 24 giờ và Outbox cùng commit.

Rate limit: 10 request/phút/IP; resend còn giới hạn một mã mới/phút/tài khoản.

### 4. Đầu ra

200: {message} cùng nội dung cho các nhánh; đầu vào sai validation 400; quá policy có thể 429.

### 5. Mã code

`WebDeveloper/Controllers/Api/VerificationEmailController.cs:16`

```csharp
public async Task<IActionResult> Resend(ResendRequest req)
{
    const string message = "Nếu tài khoản đang chờ xác thực, email sẽ được gửi. Vui lòng kiểm tra hộp thư và thư rác.";
    // Tìm tài khoản theo email.
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email.Trim());
    // Trả HTTP 200 với kết quả cho client.
    if (user == null) return Ok(new { message });
    // Bắt đầu giao dịch để nhóm các thay đổi database.
    await using var tx = await db.Database.BeginTransactionAsync();
    // Khóa theo tài khoản tới khi giao dịch kết thúc.
    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-user.UserId})");
    // Đọc lại trạng thái sau khi lấy khóa.
    await db.Entry(user).ReloadAsync();
    // Trả HTTP 200 với kết quả cho client.
    if (!AccountState.AwaitingEmailVerification(user)) return Ok(new { message });
    var cutoff = DateTime.UtcNow.AddMinutes(-1);
    if (await db.EmailOutbox.AnyAsync(x => x.UserId == user.UserId && x.Kind == "Verification" && x.CreatedAt > cutoff))
        // Trả HTTP 200 với kết quả cho client.
        return Ok(new { message });
    user.VerificationCode = Guid.NewGuid().ToString(); user.CodeExpiry = DateTime.UtcNow.AddHours(24);
    // Lưu bền ý định gửi email trong cùng giao dịch.
    db.EmailOutbox.Add(new EmailOutbox { UserId = user.UserId, Code = user.VerificationCode });
    // Lưu các thay đổi đang được theo dõi.
    await db.SaveChangesAsync(); await tx.CommitAsync();
    // Trả HTTP 200 với kết quả cho client.
    return Ok(new { message });
}

```
