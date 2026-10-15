# Hộp thông báo

Thông báo trong ứng dụng được lưu ở bảng `notifications`, tách theo tài khoản và
có trạng thái đã đọc. Migration `AddNotifications` chạy theo cơ chế migrate khi
khởi động hiện có. Không hồi tạo thông báo cho lịch sử trước khi triển khai.

| Sự kiện | Người nhận |
| --- | --- |
| Đặt lịch mới | Bệnh nhân đặt lịch và bác sĩ phụ trách |
| Xác nhận, hủy/từ chối, hoàn thành, vắng mặt | Bệnh nhân và bác sĩ |
| Tạo/sửa kết quả khám | Tài khoản bệnh nhân sở hữu hồ sơ |
| Nộp/nộp lại hồ sơ bác sĩ | Các admin đang hoạt động |
| Duyệt/từ chối hồ sơ | Bác sĩ |
| Gửi yêu cầu chuyển công tác | Bác sĩ và các admin đang hoạt động |
| Duyệt/từ chối chuyển công tác | Bác sĩ, kèm ghi chú admin |
| Tạo/sửa đánh giá bị mô hình gợi ý xem xét | Các admin đang hoạt động |

Nút chuông ở layout công khai và các layout role. API lấy 20 thông báo mỗi lần,
có Xem thêm dùng cursor ID; badge đếm toàn bộ thông báo chưa đọc. Trình duyệt hỏi
lại mỗi 30 giây khi tab đang hiển thị, không dùng SignalR và không phải browser push.
Nhấn thông báo: PATCH đọc thành công trước khi chuyển tới trang chức năng liên quan.
Không đưa chẩn đoán hoặc nội dung đánh giá vào thông báo. Nội dung được render bằng
textContent, không thực thi HTML.

`NotificationEvents` chuẩn bị thông báo từ thay đổi tracked entity trước
`ApplicationDbContext.SaveChangesAsync`, rồi EF ghi cùng transaction với nghiệp vụ.
No-op save không sinh lại sự kiện; lỗi ghi nghiệp vụ rollback cả thông báo. Các
luồng nghiệp vụ hiện tại dùng SaveChangesAsync. Nếu bổ sung SQL trực tiếp,
ExecuteUpdate hoặc SaveChanges đồng bộ cho các sự kiện trên, cần tích hợp thông báo
tương ứng; các thao tác bulk không đi qua bộ thu thập sự kiện này.

GET /api/v1/notifications; PATCH /api/v1/notifications/{id}/read;
PATCH /api/v1/notifications/read-all. Tất cả yêu cầu đăng nhập, lọc UserId từ claims,
không nhận ID người dùng từ client. Read lại là idempotent, read-all chỉ tác động
tài khoản hiện tại. Hai index hỗ trợ tải theo người nhận/ID và đếm chưa đọc.

Kiểm thử: `tests/NotificationRegression` trên PostgreSQL với rollback fixtures đạt
các sự kiện, người nhận, quyền đánh dấu đọc và rollback khi ghi lỗi. Chromium local
kiểm tra API HTTP, guest 401, truy cập chéo 404, cả ba role, phân trang 20+5, badge,
nhấn đọc/chuyển trang, đọc tất cả, chuông ở trang công khai và panel mobile. Chưa
triển khai hoặc đo tải thực trên VPS.
