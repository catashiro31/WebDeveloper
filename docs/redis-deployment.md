# Cập nhật cấu hình Redis trên VPS

Redis chỉ được truy cập qua mạng Docker nội bộ. Ứng dụng và Redis cùng dùng
`REDIS_PASSWORD` trong file `.env` cạnh `docker-compose.yml`. Compose sẽ báo lỗi
nếu biến này chưa có hoặc rỗng. Không commit `.env` lên Git.

## Triển khai

Trong `/root/WebDeveloper`, cập nhật mã nguồn rồi mở `.env` để thêm hoặc sửa
`REDIS_PASSWORD`. Tạo mật khẩu bằng `openssl rand -hex 32`, lưu kết quả vào `.env`:

```dotenv
REDIS_PASSWORD=<mật khẩu hex vừa tạo>
```

Giữ nguyên các biến khác đang có trong `.env`. Với website hiện vẫn chạy HTTP,
đảm bảo `AUTH_ALLOW_INSECURE_COOKIES=true`; chuyển về `false` khi triển khai HTTPS.

```bash
chmod 600 .env
docker compose config --quiet
docker compose up -d --build --force-recreate redis_cache webapi
```

Lệnh tạo lại hai container gây gián đoạn ngắn. PostgreSQL và các volume được giữ
nguyên. Không dùng `docker compose down -v`.

## Kiểm tra sau cập nhật

```bash
docker compose ps
docker exec docbooking_redis redis-cli ping
docker port docbooking_redis
```

Redis phải `healthy`, lệnh ping trả `PONG`, và lệnh kiểm tra cổng không có đầu ra.
`redis-cli` dùng biến `REDISCLI_AUTH` được cấu hình sẵn trong container. Tiếp theo
kiểm tra lại lịch hẹn, lịch sử khám, hồ sơ người thân và danh sách hồ sơ khi đặt
lịch bằng tài khoản bệnh nhân. Kiểm tra log nếu vẫn có lỗi:

```bash
docker logs --since 5m docbooking_api 2>&1 | grep -i -A 15 -B 3 -E 'fail:|exception|NOAUTH|WRONGPASS'
```

Việc sửa cấu hình này xử lý lỗi xác thực Redis đã thấy trong log; chưa xác định
nguồn thay đổi mật khẩu trước đó. Không công bố lại cổng Redis ra Internet.
