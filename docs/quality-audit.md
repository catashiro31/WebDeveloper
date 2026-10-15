# Rà soát lỗi giao diện và biểu mẫu

Đợt rà soát này tập trung vào các lỗi đã xuất hiện khi dùng web trên điện thoại:
thông báo API hiện nguyên JSON/HTML, bộ lọc tệp không khớp backend, giới hạn tải
lên, và nội dung tràn ngang. Đã quét các view và service của ba role, kiểm tra
trực tiếp các luồng chính bằng tài khoản thử trong môi trường cục bộ.

| Nhóm | Lỗi tìm thấy | Bản sửa |
| --- | --- | --- |
| Thông báo API | Nhiều trang dùng `res.text()` và hiện nguyên ProblemDetails JSON hoặc HTML từ proxy | Hàm dùng chung chọn `detail`, `message` hoặc lỗi validation; 413 có thông báo dễ hiểu; HTML lỗi không được đưa lên giao diện. |
| Upload | Hồ sơ bác sĩ và đơn thuốc cho chọn định dạng backend không hỗ trợ; trang bác sĩ thiếu kiểm tra 5 MB | Đồng bộ danh sách JPG/PNG/PDF và chặn tệp sai trước khi gửi. Backend từ chối tệp rỗng. |
| Nộp lại hồ sơ bác sĩ | API buộc đính kèm CCCD/chứng chỉ kể cả khi backend giữ được giấy tờ cũ | Chỉ bắt buộc lần đầu; lần nộp lại có thể giữ giấy tờ cũ, vẫn có thể thay thế. |
| Điện thoại | Thanh bên rộng 280 px đẩy nội dung chính ra khỏi màn hình | Ba layout role đưa nội dung chính lên trước và xếp thanh bên bên dưới ở màn hình hẹp; modal cơ sở y tế không tràn ngang. |
| Tải nhiều tệp | Giới hạn proxy 12 MB có thể chặn request ba tệp, mỗi tệp 5 MB | Hướng dẫn đặt `client_max_body_size 20m` trên VPS. |
| Đổi tên hồ sơ | Form lấy tên mới từ DB nhưng thanh bên/header đọc claim cũ trong access token; JavaScript cập nhật nhầm phần tử header | API phát lại cookie access với tên/ảnh mới; trang cập nhật đúng tên ngay và giữ đúng sau reload. |
| Danh sách chuyên khoa/cơ sở | Database đã lưu nhưng API danh sách trả bộ đệm cũ trong 12 giờ; thư viện thông báo không tải được cũng có thể ngăn bước làm mới giao diện | Hai danh sách đọc trực tiếp bằng truy vấn chỉ lấy các cột DTO, không tracking; API và fetch dùng no-store. Thông báo có phương án hiển thị nội bộ khi CDN lỗi. |
| Lỗi tải danh sách | Cơ sở y tế có thể báo mạng lưới trống khi API trả lỗi hoặc phản hồi sai định dạng | Kiểm tra HTTP và mảng dữ liệu, hiển thị trạng thái lỗi cùng nút thử lại cho cả hai trang. |
| Danh mục ở hồ sơ bác sĩ/bộ lọc bệnh nhân | Cùng API bị bộ đệm cũ; giao diện thiếu kiểm tra HTTP và không có cách thử lại. Một danh mục lỗi có thể ngăn trang tải cả danh sách bác sĩ | Dùng hàm tải mảng dữ liệu có kiểm tra phản hồi và no-store. Hai danh mục tải độc lập; hiển thị lỗi/empty đúng và có nút thử lại. Form chỉ cho gửi khi đủ danh mục. |
| Thẻ bác sĩ công khai | Database có cơ sở và tỉnh nhưng DTO danh sách bác sĩ không trả hai trường này | Trả `facilityName` và `province` cùng chuyên khoa để thẻ hiển thị đúng. |
| Lỗi tải hồ sơ bác sĩ | API lỗi bị hiểu thành bác sĩ chưa nộp hồ sơ và mở form đăng ký | Hiển thị lỗi hồ sơ có nút thử lại; danh sách chuyển công tác cũng kiểm tra lỗi và làm mới khi mở. |
| Trạng thái bác sĩ ở admin | API danh sách đã duyệt và chờ duyệt thiếu `verificationStatus`, khiến nhãn hiện `undefined` và nút duyệt không xuất hiện | Trả trạng thái cùng avatar; giao diện đọc cả email dạng phẳng và có nhãn dự phòng khi thiếu trạng thái. |
| Đặt lịch ngay | Nút trên trang chủ sau đăng nhập trỏ `/User/Profile`, không có MVC route tương ứng | Đổi sang `/Portal/Doctors` để chọn bác sĩ đặt lịch. |

## Kiểm tra

- `dotnet build WebDeveloper.slnx --no-restore`: thành công; còn 8 cảnh báo cũ ở các phần không sửa.
- Kiểm tra cú pháp 24 đoạn JavaScript trong các view đã thay đổi: đạt.
- Hàm đọc phản hồi API: kiểm tra 400 ProblemDetails, lỗi validation, 413 HTML,
  500 HTML và lỗi dạng text: đạt.
- Đăng nhập và mở 2 trang admin, 2 trang bệnh nhân, 3 trang bác sĩ trên Chromium:
  không có lỗi JavaScript.
- Cùng các trang trên viewport 375 px: nội dung chính nằm trong màn hình; modal
  thêm cơ sở y tế nằm trong màn hình.
- API hồ sơ bác sĩ mới khi thiếu CCCD: trả 400 với câu giải thích rõ. Tài khoản
  kiểm thử đã được xóa.
- Sửa tên hồ sơ ở ba role: access cookie được phát lại với tên mới; header/thanh
  bên cập nhật ngay và tiếp tục đúng sau reload. Ba tài khoản thử đã được xóa.
- Test hồi quy `tests/CatalogRegression`: trước khi sửa, cả hai bản ghi vừa tạo
  không xuất hiện sau lần đọc danh sách đầu tiên; sau khi sửa, thêm/sửa/xóa và
  xác minh cơ sở đều cập nhật ngay. File upload được thay bằng dịch vụ test,
  không gửi lên Cloudinary. Các bản ghi thử được dọn trong `finally`.
- Chromium: thêm/sửa/xóa chuyên khoa qua giao diện; cơ sở đã thêm sau lần đọc
  đầu xuất hiện ngay, sửa/xác minh/xóa qua giao diện và reload đều đúng. API 503
  hiển thị lỗi thay vì dữ liệu trống, nút thử lại hoạt động. CDN thông báo không
  tải được trong môi trường test, giao diện vẫn lưu và làm mới danh sách đúng.
- Test hồi quy bổ sung: nộp hồ sơ bác sĩ lưu đúng ID chuyên khoa/cơ sở; đọc lại
  trả đúng tên từ PostgreSQL. Hồ sơ PENDING không công khai; khi APPROVED thì
  bộ lọc cùng ID tìm được bác sĩ và thẻ trả đủ tên chuyên khoa/cơ sở.
- Chromium ở role bác sĩ và bệnh nhân: dropdown và bộ lọc có tên danh mục mới;
  API danh mục 503 hoặc trả sai dạng hiện lỗi, có thể thử lại, danh mục còn tốt
  vẫn dùng được. Lỗi bộ lọc không chặn danh sách bác sĩ. API danh sách bác sĩ
  lỗi hiện trạng thái lỗi thay vì kết quả rỗng. Lỗi API hồ sơ không mở form mới.
  Danh sách cơ sở chuyển công tác cập nhật và không chứa cơ sở hiện tại.
- PostgreSQL và Chromium: danh sách admin trả/hiện PENDING và APPROVED đúng;
  nút duyệt hiện cho hồ sơ chờ duyệt, email hiện đúng, trạng thái đã duyệt giữ
  đúng sau reload. Test hồi quy kiểm tra trường trạng thái ở cả hai API.
- Chromium với tài khoản bệnh nhân: từ trang chủ bấm "Đặt lịch ngay" mở
  `/Portal/Doctors` thành công. Khách chưa đăng nhập vẫn có link đăng ký.

Kiểm tra cục bộ không thay thế việc thử upload thật trên VPS: cấu hình Nginx nằm
ngoài repository, và việc lưu tệp còn phụ thuộc dịch vụ Cloudinary của môi trường
triển khai. Sau khi triển khai mã, thử lại một ảnh hợp lệ và một tệp vượt 5 MB cho
từng biểu mẫu tải tệp.

### Khóa và mở khóa người dùng (2026-10-13)

- Nguyên nhân: trang Admin/Users gửi PUT nhưng hai endpoint block/unblock chỉ nhận PATCH, nên yêu cầu không chạy vào service.
- Đổi cả hai thao tác sang PATCH với cookie cùng nguồn; dùng showApiToast có fallback khi CDN Toastify lỗi. Chặn gửi trùng trong lúc xử lý, giữ lý do khi API thất bại, thêm tải lại danh sách khi lỗi.
- API danh sách trả thêm ReasonBanned để hiển thị lý do sau khi khóa. Tên người dùng được truyền qua data attribute đã escape, tránh lỗi JavaScript với tên chứa dấu nháy.
- Kiểm thử Chromium + PostgreSQL local với người dùng thử nghiệm PATIENT và DOCTOR: khóa/mở khóa qua nút giao diện trả 200; trạng thái database đúng; tải lại vẫn giữ trạng thái; tài khoản bị khóa không đăng nhập được, mở khóa đăng nhập lại được. Kiểm thử thêm lý do rỗng, tên O'Brien, CDN Toastify bị chặn và API trả 503 vẫn hiện lỗi, giữ lý do để thử lại. Không phát sinh pageerror.
- dotnet build --no-restore thành công (6 cảnh báo đã có). Chưa triển khai hoặc xác nhận trên VPS; email SMTP thực tế không nằm trong kiểm thử local.

### Phiếu trả kết quả khám (2026-10-14)

- Phiếu hiển thị mã ca khám, bệnh nhân thực tế trong hồ sơ đặt lịch, ngày/giờ và chuyên khoa. Đưa modal ra ngoài vùng layout có transform để tiêu đề/nút đóng không bị che; nội dung dài cuộn trong màn hình.
- Hỗ trợ JPG/PNG/PDF/DOC/DOCX tối đa 5 MB hoặc một link HTTP/HTTPS. Giữ tệp/link cũ khi sửa mà không chọn thay thế; từ chối link sai giao thức hoặc đồng thời gửi tệp và link. Dùng thông báo có fallback khi Toastify không tải được.
- Thêm FollowUpDate nullable, migration AddMedicalResultFollowUpDate. API bác sĩ và bệnh nhân trả ngày tái khám; lịch sử bệnh nhân hiển thị ngày đề nghị và nhắc đặt lịch riêng (không tự đặt lịch).
- Sửa bất nhất: trước đây nút trả kết quả xuất hiện ở CONFIRMED nhưng service chỉ nhận COMPLETED. Nay lưu kết quả và chuyển CONFIRMED sang COMPLETED trong cùng SaveChanges; kiểm tra chẩn đoán, ngày tái khám, quyền bác sĩ và tệp/link trước khi đổi trạng thái.
- Kiểm thử PostgreSQL: migration áp dụng được; kiểm tra dữ liệu không hợp lệ không đổi trạng thái, lưu link/ngày tái khám, cập nhật DOC/DOCX qua fake storage, xóa trường tùy chọn khi sửa. Chromium kiểm tra form thực, lưu và mở lại kết quả, API lịch sử bệnh nhân; không có pageerror. Cloudinary thực tế và VPS chưa được kiểm thử/triển khai.

### Tìm kiếm bác sĩ, phông chữ và đặt lại mật khẩu (2026-10-14)

- Tìm kiếm cũ dùng Contains phân biệt hoa/thường và chỉ đọc họ tên. Nay PostgreSQL unaccent + ILIKE tìm từng từ không phân biệt hoa/thường/dấu trong tên, chuyên khoa, cơ sở và bio. Escape %, _ và dấu gạch chéo ngược để tìm đúng ký tự nhập. Giữ các bộ lọc/phân trang; bỏ qua phản hồi tìm kiếm cũ tới muộn. Placeholder mô tả đúng các trường hỗ trợ.
- Migration EnableUnaccentSearch cài extension unaccent (PostgreSQL cần hỗ trợ extension và tài khoản chạy migration có quyền tạo). Đã áp dụng trên PostgreSQL local.
- Thống nhất Arial, Helvetica, sans-serif cho các trang và form controls; bỏ các font Google/Playfair/Nunito trộn lẫn.
- Thiết kế lại ResetPassword bằng CSS riêng vì trước đây dùng class Bootstrap không có stylesheet tương ứng: thẻ form, nhãn rõ ràng, kích thước mobile, trạng thái đang xử lý/thành công/lỗi, liên kết yêu cầu email mới. Đọc thông báo API qua readApiMessage.
- Build đạt; regression thực trên PostgreSQL đạt cho tên có/không dấu, hoa/thường, nhiều từ, bio/chuyên khoa/cơ sở và ký tự wildcard. Chromium đạt trên desktop/mobile, mật khẩu không khớp, đổi mật khẩu thật với tài khoản thử nghiệm, đăng nhập bằng mật khẩu mới, link đã dùng báo lỗi, tìm kiếm gửi đúng keyword, không có pageerror. Không kiểm thử gửi email thật hay triển khai VPS.

### Bộ lọc, giấy tờ, lịch và chuyển công tác (2026-10-14)

- Chuyên khoa có ô tìm không dấu. Chuyên khoa/cơ sở mặc định hiện 4 mục với Xem thêm/Thu gọn; xóa bộ lọc đưa về trạng thái thu gọn.
- Thêm API doctors/price-range lấy giá lớn nhất của bác sĩ đã duyệt, không giới hạn cứng 2 triệu; bộ lọc tối thiểu 0, không giới hạn trên khi chọn toàn khoảng. Giá đăng ký/cập nhật bác sĩ phải lớn hơn 0; form cũng kiểm tra giá.
- Chứng chỉ hành nghề trong modal admin dùng liên kết tệp thay vì nhúng PDF vào img gây ảnh vỡ.
- Lịch hẹn admin dùng projection trực tiếp thay cho tải toàn bộ entity graph. Frontend kiểm tra HTTP, bỏ phản hồi cũ, timeout 15 giây và có thử lại; chưa đo độ trễ dữ liệu thật trên VPS.
- Giấy phép cơ sở đã hỗ trợ JPG/PNG/PDF tối đa 5 MB trong API và frontend. Kiểm thử lại cả ba loại trong trình duyệt; không mở cho mọi loại tệp. Cloudinary thực tế chưa được kiểm thử ở môi trường local.
- Tạo lịch trùng báo lỗi thay vì thành công 0 ca; kiểm tra tất cả ca trước khi thêm, dùng PostgreSQL advisory transaction lock theo bác sĩ để tránh hai yêu cầu tạo trùng đồng thời. Thêm chú thích đỏ/đã đặt và xanh/còn trống.
- API xử lý CANCELLED cho lịch PENDING, giải phóng slot; NO_SHOW chỉ cho CONFIRMED đã tới giờ khám. Giữ kiểm tra quyền sở hữu lịch.
- Layout bác sĩ hiển thị thông báo kết quả chuyển công tác gần nhất (duyệt/từ chối) cùng ghi chú khi mở/tải lại trang; không gửi email hoặc thông báo đẩy.
- Build/regression PostgreSQL đạt: ca đã đặt/còn trống không tạo trùng, từ chối giải phóng slot, giá trên 2 triệu vào khoảng giá. Chromium đạt: tìm/thu gọn/mở rộng danh mục, giá tối đa động, thông báo cả duyệt/từ chối, chú thích lịch, bảng lịch admin và phục hồi khi API 503, chọn giấy phép PNG/JPG/PDF; không có pageerror. Chưa triển khai VPS.

### Phông tiếng Việt thống nhất (2026-10-14)

Thay các font hệ thống và cấu hình Inter còn sót bằng Noto Sans variable tự lưu trong wwwroot/fonts/noto-sans, kèm giấy phép OFL. typography.css định nghĩa chung --font-app; tất cả layout, trang auth độc lập, nội dung, tiêu đề, form và cấu hình widget dùng cùng font. WOFF2 giữ nguyên glyph, kiểm tra đầy đủ vùng ký tự tiếng Việt U+1EA0–U+1EF9. Build thành công; Chromium kiểm tra sáu trang công khai/auth, font tải từ ứng dụng ngay cả khi chặn toàn bộ yêu cầu HTTPS bên ngoài. Chưa triển khai VPS.

### Giữ sao khi ẩn nội dung đánh giá (2026-10-15)

Đánh giá bị admin ẩn vì nội dung tiêu cực vẫn được trả về trong danh sách công khai và danh sách bác sĩ, giữ nguyên số sao và được tính vào ReviewCount/RatingAverage. Chỉ trường Comment được thay bằng “Nội dung tiêu cực đã được ẩn” ở giao diện. Dashboard admin cũng đếm toàn bộ đánh giá. Không xóa dữ liệu và vẫn cho admin hiện lại bài.

### Naive Bayes hỗ trợ kiểm duyệt (2026-10-15)

Thay điểm cố định/dò từ khóa frontend bằng multinomial Naive Bayes ở server. Từ điển tiếng Việt Blue Eyes (MIT) được lưu kèm giấy phép, commit nguồn và SHA256 trong Moderation/README.md. Mẫu spam/quảng cáo/thông thường do dự án biên soạn, không phải corpus đã kiểm định. Điểm mô hình chỉ dùng gợi ý; không tự ẩn bài và không đổi sao. Thêm ba hàng đợi thô tục/spam/quảng cáo, lọc và tìm trên toàn danh sách trước phân trang. Không gửi nội dung đánh giá ra dịch vụ ngoài.

Build đạt; ModerationRegression đạt 8 ca phân loại và kiểm thử PostgreSQL lọc trước phân trang, không sửa trạng thái/sao (dữ liệu thử rollback). Chromium chạy JavaScript view thật với API giả lập đạt chuyển tab spam/quảng cáo, không pageerror. CatalogRegression hiện thất bại ở kỳ vọng tìm bio `dau dau`, do PublicService hiện chủ động không tìm Bio/FacilityName; không thay đổi hành vi tìm kiếm trong task này. Restore báo cảnh báo dependency PKCS 10.0.0 đã có, không nâng dependency ngoài phạm vi.
