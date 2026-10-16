# Đối chiếu đề cương và slide NT106

## Tài liệu đã đọc

Đề cương `de_cuong_NT106.pdf`: 7 trang, ngày biên soạn 14/10/2022. Thư mục slide có 11 PDF, tổng 555 trang. Đã trích xuất toàn bộ phần chữ bằng PyMuPDF; các trang chỉ có sơ đồ/ảnh hoặc source dạng ảnh không được chuyển thành mã để dùng trực tiếp. Bản trích xuất dùng đối chiếu nằm ở `docs/course-notes` (không đưa vào source control).

| Tệp slide | Số trang | Liên hệ đồ án |
|---|---:|---|
| Chương 1 – Giới thiệu | 69 | IP/port, network stack, .NET, C# và cấu trúc lớp |
| Chương 2 – Input và Ouput | 62 | NetworkStream/FileStream, UTF-8, serialization và lưu dữ liệu |
| Chương 3 – Sockets | 51 | TcpClient/TcpListener, IPAddress, IPEndPoint, TCP/UDP; trọng tâm transport |
| Chương 3B – Thread | 21 | Đồng thời, tránh treo UI, quản lý vòng đời tác vụ |
| Chương 4 – Web server | 48 | HTTP request/response; tham khảo cách phân biệt request/response, không dùng HTTP cho chat |
| Chương 5 – Email Server | 54 | SMTP/POP3/IMAP; so sánh cơ chế chuyển tiếp qua server; không triển khai email |
| Chương 6 – File Server | 64 | Truyền tệp và quyền truy cập; đồ án dùng giao thức TCP riêng, không gọi đó là FTP |
| Chương 7 – Security | 45 | Hạ tầng mạng, proxy/router, firewall/tunneling; lưu ý khi demo LAN |
| Chương 8 – ICMP | 28 | Ping/traceroute để chẩn đoán đường mạng; ping không chứng minh cổng chat mở |
| Chương 9 – IP Packet | 68 | Bắt/phân tích gói; có thể demo Wireshark TCP handshake và bản mã TLS |
| Chương 10 – Bảo vệ dữ liệu | 45 | Hash, mã hóa đối xứng/bất đối xứng, PKI, quản lý khóa |

## Mốc và đánh giá trong đề cương

- G2.2: hiểu cách ứng dụng mạng vận hành; G3.1: phát triển ứng dụng mạng; G3.2: hợp tác nhóm.
- Buổi 3–4: nội dung đa luồng, socket và chọn/phân tích đề tài.
- Buổi 8–9: sinh viên báo cáo quá trình, giảng viên hỗ trợ nhóm.
- Buổi 14–15: seminar và báo cáo đồ án.
- A1 quá trình 30%; A2 thực hành 20%; A3 vấn đáp cuối kỳ 50% (đề cương trang 6).

Đây là mốc theo tài liệu được cung cấp, không suy ra ngày deadline thực tế của lớp năm học 2026–2027.

## Quyết định thiết kế để sát môn học

Giữ C# WinForms và TCP Socket tự xây giao thức. Dùng `async/await` cho I/O thay vì tạo một thread bị block cho mỗi client; `SemaphoreSlim` bảo vệ trạng thái server, `Channel` tách gửi dữ liệu ra khỏi xử lý nghiệp vụ. Cập nhật UI qua `BeginInvoke`. Lưu JSON dùng FileStream và System.Text.Json để dễ đọc và trình bày, không phụ thuộc server cơ sở dữ liệu trong buổi demo.

Lớp ứng dụng tự xử lý request ID, xác thực, quyền thành viên, thông báo, giới hạn kích thước và khung tin. Các nội dung này có thể chỉ ra trực tiếp trong code khi vấn đáp.

## Những điểm cần nói chính xác khi dùng slide cũ

- SHA-256 có đầu ra **256 bit = 32 byte**; slide Chương 10, trang PDF 40 ghi đơn vị byte ở các mục SHA là không đúng. Hash mật khẩu bằng PBKDF2-HMAC-SHA256, không phải một lần SHA-256.
- TCP là Transmission Control Protocol. TCP cung cấp byte stream có thứ tự; không đảm bảo một lần gửi tương ứng một lần nhận của ứng dụng.
- `127.0.0.1` là loopback. Máy vẫn có thể kết nối LAN/Internet qua giao diện khác khi ứng dụng dùng loopback.
- Xung đột bind phụ thuộc IP, port, transport và tùy chọn socket; không phát biểu tuyệt đối rằng hai ứng dụng không bao giờ có cùng số port.
- Ví dụ `Thread.Abort` trong tài liệu cũ không dùng trong bản này; dừng bằng cancellation và đóng socket.
- TLS client–server không đồng nghĩa mã hóa đầu cuối giữa hai người dùng.

## Nguồn kỹ thuật bổ sung

- Microsoft: [SslStream](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=net-10.0) — TLS trên stream TCP.
- OWASP: [Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html) — PBKDF2-HMAC-SHA256, salt và chi phí băm. Bản này dùng 600.000 vòng.

Các nguồn bổ sung giải thích lựa chọn hiện thực; không thay đổi tiêu chí chấm đồ án do giảng viên đưa ra.
