# Cập nhật thông tin chuyên môn

## Cách sử dụng

- Bác sĩ: **Yêu cầu cập nhật chuyên môn** trong sidebar hoặc trang Hồ sơ chuyên môn.
- Admin: **Duyệt cập nhật chuyên môn** trong sidebar. Chọn bác sĩ để sửa trực tiếp; để “Tất cả” để duyệt các yêu cầu. Cũng có liên kết trong chi tiết bác sĩ tại Admin/Doctors.
- Có thể cập nhật học vị/bằng cấp, năm bắt đầu hành nghề, chuyên khoa, chứng chỉ; gửi minh chứng PDF/JPG/PNG tối đa 5 MB. Bác sĩ có thể chọn chứng chỉ cũ làm minh chứng, admin phải tự đối chiếu tính phù hợp của tài liệu.
- Admin có thể bổ sung trực tiếp dữ liệu cũ mà chưa có minh chứng mới, nhưng phải ghi lý do. Đây là quyền có chủ đích, mọi thay đổi đều có lịch sử và thông báo chuông.
- CCCD/chứng chỉ hiện có được giữ nguyên nếu không thay tệp. Không bắt toàn bộ bác sĩ nộp lại hồ sơ.
- Năm bắt đầu chưa biết để trống; tiếp tục hiển thị số năm kinh nghiệm cũ. Khi đã nhập năm bắt đầu, số năm kinh nghiệm được tính theo năm Việt Nam hiện tại.
- Giá/giới thiệu vẫn sửa ngay tại hồ sơ bác sĩ; admin cũng có thể sửa trong màn hình mới.
- Cơ sở công tác tiếp tục dùng luồng chuyển công tác hiện có.

## Quy tắc xử lý

Một bác sĩ chỉ có một yêu cầu chờ duyệt. Trong thời gian chờ, hồ sơ công khai và khả năng nhận lịch không thay đổi. Duyệt áp dụng các trường đã yêu cầu; không ghi đè giá/giới thiệu được sửa độc lập trong lúc chờ.

Từ chối phải có lý do. Lịch sử lưu thông tin trước/đề nghị, thông tin thực tế trước khi áp dụng, người gửi, người xử lý, thời điểm, lý do và đường dẫn minh chứng. Admin sửa trực tiếp cũng dùng cùng lịch sử. Nếu admin sửa chuyên môn sau khi yêu cầu được gửi, yêu cầu đó phải bị từ chối và gửi lại để tránh ghi đè dữ liệu mới.

Chuyển chuyên khoa bị chặn khi còn bất kỳ lịch PENDING hoặc CONFIRMED, kể cả lịch quá giờ chưa xử lý. Phải hoàn thành/hủy/xử lý vắng mặt đúng quy tắc trước khi chuyển. Lịch đã đặt giữ giá, tên chuyên khoa, cơ sở và địa chỉ đã ghi nhận.

Các thay đổi giá/chuyên môn và đặt lịch dùng cùng khóa giao dịch theo bác sĩ. Hai admin xử lý cùng yêu cầu chỉ có một thao tác thành công. Cơ sở có lịch sử đặt khám được bảo vệ khỏi xóa qua kiểm tra snapshot.

## Nâng cấp dữ liệu

Migration: `20261019202555_ProfessionalProfileUpdates`. Ứng dụng hiện tự chạy migration khi khởi động; nên sao lưu cơ sở dữ liệu trước khi triển khai như các lần nâng cấp khác.

**Giới hạn dữ liệu cũ:** hệ thống trước đây không lưu giá/chuyên khoa tại thời điểm đặt. Migration chỉ cố định thông tin hiện biết; cơ sở ưu tiên lấy từ lịch làm việc. Các lịch này được đánh dấu `IsLegacySnapshot=true`, giao diện ghi “giá ghi nhận khi nâng cấp”. Không thể khôi phục một giá/chuyên khoa lịch sử đã bị thay đổi trước đó mà không có nguồn đối chiếu. Doanh thu ước tính dùng giá đã cố định, không phải dữ liệu thanh toán thực thu.

Không tự quy đổi số năm kinh nghiệm cũ thành năm bắt đầu. Không xóa hoặc yêu cầu lại tài liệu cũ. Rollback migration sẽ mất lịch sử cập nhật và snapshot mới; không dùng rollback để hoàn tác một yêu cầu, hãy dùng chỉnh sửa có ghi lịch sử.

## Kiểm thử

Chỉ chạy trên database thử nghiệm riêng:
```sh
AUDIT_TEST_CONNECTION='Host=127.0.0.1;Database=professional_test_unique;Username=postgres' dotnet run --project tests/AuditRegression
AUDIT_BASE_URL=http://127.0.0.1:5019 CHROMIUM_EXECUTABLE=/usr/bin/chromium node tests/AuditRegression/professional-browser.cjs
```

Browser dùng fixture tại /tmp/audit-browser-fixtures.json, app phải trỏ đúng database đó; cần Playwright trong Node module path. Upload trong kiểm thử nghiệp vụ dùng dịch vụ giả, không gửi tài liệu sang Cloudinary. Kiểm thử trình duyệt dùng tài liệu có sẵn để tránh tác động dịch vụ ngoài.
