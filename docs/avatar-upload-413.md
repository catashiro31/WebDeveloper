# Sửa lỗi 413 khi cập nhật ảnh đại diện

Ảnh trên trang hồ sơ bệnh nhân hiển thị trang HTML `413 Request Entity Too Large`
của `nginx/1.24.0`. Đây là phản hồi của Nginx trước khi request tới ASP.NET Core.
Nginx thường giới hạn request body ở 1 MB; giao diện cho phép ảnh JPG/PNG đến
5 MB. Form thêm cơ sở y tế còn gửi cả ảnh đại diện và giấy phép (JPG/PNG/PDF),
mỗi tệp tối đa 5 MB. Form `multipart/form-data` có thêm một ít dữ liệu ngoài tệp.

Trên VPS, tìm file cấu hình Nginx đang phục vụ `163.61.182.126` (thường ở
`/etc/nginx/sites-enabled/` hoặc `/etc/nginx/conf.d/`). Thêm dòng sau **bên trong
khối `server { ... }` phục vụ website**:

```nginx
client_max_body_size 12m;
```

Kiểm tra và nạp lại Nginx:

```sh
sudo nginx -t
sudo systemctl reload nginx
```

Nếu Nginx chạy bằng container, sửa cấu hình đang mount vào container rồi kiểm tra
và reload Nginx **trong container tương ứng**. Giới hạn của proxy/CDN phía trước
Nginx, nếu có, cũng cần lớn hơn 5 MB.

Mã ứng dụng mới từ nhánh `master` từ chối ảnh đại diện vượt 5 MB hoặc không phải
JPG/PNG ngay trên trang và kiểm tra lại tại backend. Giấy phép cơ sở y tế hỗ trợ
JPG/PNG/PDF, tối đa 5 MB. Khi proxy trả 413, trang hiển thị thông báo ngắn thay vì
in nguyên HTML lỗi. Sau khi lấy mã mới, build lại dịch vụ ứng dụng. Cần cả thay đổi
Nginx lẫn triển khai ứng dụng để hai tệp hợp lệ tải lên được.

Kiểm tra bằng một ảnh JPG/PNG dưới 1 MB, một ảnh trong khoảng 1–5 MB, rồi một ảnh
lớn hơn 5 MB. Hai ảnh đầu phải đi qua Nginx; ảnh cuối phải bị trang từ chối trước
khi tải lên. Không cần thay đổi cơ sở dữ liệu.

Nếu form thêm cơ sở y tế báo 400 với giấy phép PNG/JPG, bản cũ của backend chỉ
cho phép PDF dù form ghi “Ảnh/PDF”. Bản sửa đồng bộ backend với form: giấy phép
JPG, PNG và PDF đều được chấp nhận khi mỗi tệp không quá 5 MB. Sau khi triển khai,
thử tạo cơ sở với ảnh đại diện JPG/PNG và giấy phép PNG như trường hợp đã báo lỗi.
