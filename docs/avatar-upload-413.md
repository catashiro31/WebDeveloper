# Sửa lỗi 413 khi cập nhật ảnh đại diện

Ảnh trên trang hồ sơ bệnh nhân hiển thị trang HTML `413 Request Entity Too Large`
của `nginx/1.24.0`. Đây là phản hồi của Nginx trước khi request tới ASP.NET Core.
Nginx thường giới hạn request body ở 1 MB; giao diện cho phép ảnh JPG/PNG đến
5 MB. Form `multipart/form-data` còn có thêm một ít dữ liệu ngoài ảnh.

Trên VPS, tìm file cấu hình Nginx đang phục vụ `163.61.182.126` (thường ở
`/etc/nginx/sites-enabled/` hoặc `/etc/nginx/conf.d/`). Thêm dòng sau **bên trong
khối `server { ... }` phục vụ website**:

```nginx
client_max_body_size 6m;
```

Kiểm tra và nạp lại Nginx:

```sh
sudo nginx -t
sudo systemctl reload nginx
```

Nếu Nginx chạy bằng container, sửa cấu hình đang mount vào container rồi kiểm tra
và reload Nginx **trong container tương ứng**. Giới hạn của proxy/CDN phía trước
Nginx, nếu có, cũng cần lớn hơn 5 MB.

Mã ứng dụng mới từ nhánh `master` từ chối ảnh vượt 5 MB hoặc không phải JPG/PNG
ngay trên trang và kiểm tra lại tại backend. Khi proxy trả 413, trang hiển thị
thông báo ngắn thay vì in nguyên HTML lỗi. Sau khi lấy mã mới, build lại dịch vụ
ứng dụng. Cần cả thay đổi Nginx lẫn triển khai ứng dụng để ảnh từ 1–5 MB tải lên
được và thông báo lỗi dễ hiểu.

Kiểm tra bằng một ảnh JPG/PNG dưới 1 MB, một ảnh trong khoảng 1–5 MB, rồi một ảnh
lớn hơn 5 MB. Hai ảnh đầu phải đi qua Nginx; ảnh cuối phải bị trang từ chối trước
khi tải lên. Không cần thay đổi cơ sở dữ liệu.
