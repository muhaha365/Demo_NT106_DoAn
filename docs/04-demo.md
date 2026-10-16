# Kịch bản demo và kiểm thử thủ công

## Chuẩn bị

Một máy server, 2–3 cửa sổ client (ưu tiên 2–3 máy LAN để minh họa mạng thật). Tạo tài khoản của nhóm, ví dụ `minh_anh`, `hoang_nam`, `bao_chi`; tự đặt mật khẩu. Chuẩn bị ảnh PNG nhỏ, tệp PDF nhỏ và tệp lớn hơn 5 MB. Chỉ dùng dữ liệu demo.

Chạy build/test trước. Kiểm tra IP LAN, cổng, fingerprint, .NET Desktop Runtime, firewall. Giữ bản backup thư mục server trước buổi trình bày. Không thay certificate giữa chừng vì client sẽ từ chối fingerprint cũ.

## Demo live khoảng 8–10 phút

| Thời lượng dự kiến | Thao tác | Kết quả cần chỉ ra |
|---|---|---|
| 0:00–1:00 | Mở server, giải thích IP/cổng; mở hai client | Một server, nhiều TCP connection; mã chứng chỉ |
| 1:00–2:00 | Đăng ký, thử sai mật khẩu, đăng nhập đúng | Validate đầu vào; PBKDF2; không lưu password thô |
| 2:00–3:00 | Tìm bạn, gửi lời mời, nhận popup, chấp nhận | Quan hệ hai chiều, danh sách bạn bè |
| 3:00–4:30 | Gửi text/emoji/xuống dòng; gõ chậm rồi dừng | Tin realtime, timestamp, typing tự hết hạn |
| 4:30–5:15 | Chuyển focus sang cửa sổ người nhận và mở đúng chat | Đã xem; badge chưa đọc giảm; popup khi chat khác |
| 5:15–6:15 | Tạo nhóm ba người, thêm/xóa thành viên | Chỉ trưởng nhóm quản lý; người bị xóa mất quyền |
| 6:15–7:15 | Gửi PNG/PDF, xem ảnh, lưu tệp; thử tệp >5 MB | Giới hạn client/server, byte round trip |
| 7:15–8:00 | Tìm một từ khóa trong lịch sử; bỏ lọc | Tìm ở server, không chỉ tin đang hiển thị |
| 8:00–9:00 | Đăng xuất/mở lại; dừng rồi khởi động server | Offline, kết nối lỗi rõ ràng, lịch sử và tệp còn |
| 9:00–10:00 | Chỉ ra code framing/phân quyền; nêu giới hạn | Liên hệ TCP, async/thread, TLS và lưu trữ |

## Video dự phòng

Quay đúng luồng trên sau khi nhóm diễn tập; nên dùng công cụ quay màn hình sẵn có. File đề xuất `Demo-UIT-Chat.mp4`, độ dài 5–8 phút. Hiện chưa có video được ghi tự động thay nhóm.

Đầu video hiện tên đề tài, tên nhóm và ngày quay; màn hình thấy server cùng các client. Chèn lời giải thích ngắn về TCP/TLS, dữ liệu, quyền nhóm. Cuối video có restart server để chứng minh persistence. Phát thử file video trên máy trình bày, giữ một bản offline và một bản USB.

## Checklist thủ công trước buổi nộp

| Ca thử | Mong đợi | Kết quả thực tế / người thử |
|---|---|---|
| Hai máy LAN dùng đúng IP/cổng/pin | Chat qua hai máy thật | Chưa xác nhận |
| IP/cổng sai hoặc firewall chặn | Báo lỗi/timeout; không treo UI | Chưa xác nhận |
| Pin sai | TLS từ chối | Tự động đã thử; LAN cần thử |
| Tắt Wi-Fi/rút dây mạng | Báo mất kết nối sau timeout; đăng nhập lại lấy lịch sử | Chưa xác nhận trên LAN |
| App thu nhỏ/không focus | Có popup; không đánh dấu đã xem | Cần xác nhận trực tiếp |
| Typing rồi dừng | Indicator hết sau vài giây | Server event đã thử; cần quan sát UI |
| DPI 100%, 125%, 150% | Chữ và nút không che nhau | Mới chụp ở môi trường hiện tại |
| Tệp sát 5 MB | Gửi được; quá giới hạn bị chặn | Chưa benchmark tốc độ LAN |
| Video nhỏ | Gửi/lưu được, mở ngoài app | Chưa có player tích hợp |
| Server hết quyền ghi/đĩa đầy | Không ACK thành công khi chưa lưu | Lỗi ghi đã giả lập tự động |
| Mở hai client cùng username | Đóng một vẫn online nếu còn client kia | Tự động đã thử |
| Người bị xóa khỏi nhóm tải lại file | Server từ chối | Cần thử thêm thao tác UI |

## Xử lý lỗi thường gặp

- “Only one usage…”: cổng đã được server khác dùng; tắt server cũ hoặc đổi cả cổng server/client.
- Tệp `server.lock` bị khóa: đang có server khác dùng cùng dữ liệu; không xóa dữ liệu để giải quyết, hãy dừng tiến trình server đúng cách.
- Không có fingerprint: khởi động server trước; dùng nút lấy mã local, hoặc dán mã từ server LAN.
- Thấy người kia offline: kiểm tra họ đã đăng nhập thành công, cùng server và cùng cổng; đăng ký thôi chưa phải online.
- Lịch sử mất trên máy khác: server mới đang dùng thư mục dữ liệu khác. Client không giữ lịch sử riêng.
- Bản JSON lỗi: dừng server, sao chép file lỗi để giữ minh chứng, phục hồi bản backup đã kiểm tra. Bản `.bak` chỉ chứa trạng thái trước lần ghi gần nhất, không thay backup dài hạn.
