# Xóa và tạo lại dữ liệu

Chỉ chạy với PostgreSQL thử nghiệm riêng, không dùng database thật. Runner tự áp dụng migration và tạo dữ liệu có tiền tố ngẫu nhiên; dữ liệu được giữ lại để kiểm tra. Xóa database/container thử nghiệm sau khi chạy nếu không cần giữ fixture.

```sh
DELETE_RECREATE_TEST_CONNECTION='Host=127.0.0.1;Database=delete_recreate_test;Username=postgres;Password=...' \
  dotnet run --project tests/DeleteRecreateRegression
```

Kiểm tra service/controller thật trên PostgreSQL:

- Xóa cơ sở/chuyên khoa rồi tạo lại cùng tên: bản ghi mới xuất hiện, bản ghi cũ vẫn bị ẩn.
- Tên đang hoạt động vẫn bị chặn trùng, kể cả khác chữ hoa/thường hoặc khoảng trắng đầu cuối.
- Đổi tên sang tên đã xóa; chặn sửa qua form cũ sau khi xóa.
- Bác sĩ tạo cơ sở mới trùng tên cơ sở đã xóa: vẫn cần duyệt; không cho chọn chuyên khoa đã xóa.
- Chặn xóa cơ sở/chuyên khoa đang có bác sĩ.
- Đóng rồi tạo lại ca khám: mở lại đúng ca, không nhân đôi; chặn xóa ca đã đặt.
- Xóa rồi tạo lại người thân; chặn trùng hồ sơ đang hoạt động, sửa hồ sơ đã xóa, xóa hồ sơ bản thân hoặc hồ sơ còn lịch hẹn.
- Xóa thông báo đã đọc và xóa hàng loạt: giữ thông báo chưa đọc và thông báo của tài khoản khác.

Tệp tải lên dùng fake storage, không gửi email hay gọi Cloudinary. Không kiểm thử giao diện trình duyệt hoặc HTTP authentication trong runner này.
