# Ôn vấn đáp gắn với source

1. **Vì sao dùng TCP cho chat?** Cần truyền có thứ tự, tin cậy trên kết nối. UDP không tự cung cấp các thuộc tính đó. TCP không đảm bảo người dùng đã đọc tin; seen là giao thức ứng dụng.

2. **Vì sao không `Read()` một lần rồi parse JSON?** TCP truyền byte stream, một lần đọc có thể thiếu hoặc gộp nhiều thông điệp. `FrameCodec` dùng 4 byte big-endian cho chiều dài, `ReadExactlyAsync` đọc đủ payload.

3. **IP và port có vai trò gì?** IP xác định giao diện/host, port xác định đầu cuối dịch vụ transport. Server listen trên 5050; client dùng cổng nguồn tạm thời. Loopback chỉ trong máy; LAN dùng IP của máy server.

4. **Ứng dụng nằm ở tầng nào?** Giao thức chat/JSON ở tầng ứng dụng; TLS bảo vệ kênh; TCP ở transport; IP ở network. Không gọi gói JSON là một IP packet hay khẳng định một tin chat = một TCP segment.

5. **Nhiều client được phục vụ thế nào?** Accept loop tạo tác vụ xử lý cho từng peer; read/write dùng async. `SemaphoreSlim` tuần tự hóa thay đổi trạng thái, mỗi peer có một writer để tránh byte của hai frame đan xen.

6. **Task có phải một thread riêng không?** Không. Task biểu diễn công việc; async I/O thường trả thread khi chờ mạng. PBKDF2 là CPU work được chuyển sang ThreadPool. WinForms vẫn có UI thread riêng.

7. **Vì sao dùng BeginInvoke?** Control WinForms phải được cập nhật trên thread đã tạo nó. Receive loop không giả định đang chạy trên UI thread; `Theme.Ui` đưa thao tác về UI.

8. **Hash và mã hóa khác nhau?** Hash không có khóa giải mã để khôi phục mật khẩu. Mã hóa dùng khóa để biến bản rõ/bản mã. Bản này dùng PBKDF2 cho password, TLS cho đường truyền.

9. **Tại sao không SHA256(password) một lần?** Quá nhanh khi thử hàng loạt mật khẩu. PBKDF2 lặp HMAC-SHA256 600.000 vòng, salt riêng mỗi account; cùng password không cho cùng hash. SHA-256 dài 256 bit, tức 32 byte.

10. **Chứng chỉ tự ký có an toàn không?** Cần có cách tin đúng server. Bản này xác minh SHA-256 của certificate bằng mã lấy từ server qua kênh tin cậy. Không chấp nhận mọi certificate; chưa triển khai PKI/CA/thu hồi chứng chỉ.

11. **Mã hóa này có phải E2EE không?** Không. TLS bảo vệ client–server; server giải mã để xử lý và lưu bản rõ trên đĩa. E2EE đòi hỏi quản lý khóa giữa người dùng/thiết bị, không để server đọc nội dung.

12. **Đã gửi khác đã xem thế nào?** Đã gửi sau khi server commit; đã xem do client người nhận gửi read cursor khi cuộc chat có focus và đã cuộn tới cuối. TCP ACK không phải read receipt.

13. **Client có thể giả username người gửi không?** Server lấy username từ peer đã xác thực, không lấy sender do payload tự khai. Membership cũng được kiểm tra cho history, send, typing, read và download.

14. **Nếu hai người gửi cùng lúc?** Semaphore và server sequence đảm bảo ID riêng, trạng thái lưu nhất quán. Thứ tự ID là thứ tự server commit, không nhất thiết thứ tự nhấn nút giữa hai đồng hồ máy khác nhau.

15. **Nếu bị ngắt mạng ngay sau gửi?** Có thể server đã lưu nhưng response chưa tới client. Kiểm tra lịch sử trước khi gửi lại. `clientId` hỗ trợ chống trùng nếu retry cùng ID; UI chưa tự retry qua phiên đăng nhập.

16. **Online/offline xác định ra sao?** Có ít nhất một peer đã đăng nhập. Socket đóng được nhận biết nhanh; heartbeat/timeout xử lý mạng chết im lặng. Nhiều cửa sổ cùng user chỉ offline khi không còn peer hoạt động.

17. **Một client chậm ảnh hưởng người khác không?** Server enqueue vào hàng đợi riêng, không giữ semaphore khi gửi socket. Queue đầy thì ngắt peer chậm. Lưu JSON toàn bộ vẫn là nút thắt chung khi dữ liệu lớn.

18. **Vì sao giới hạn file và frame khác nhau?** byte[] thành Base64 trong JSON làm dữ liệu tăng khoảng một phần ba, cộng metadata. File tối đa 5 MiB nằm trong frame tối đa 8 MiB. Giới hạn kiểm tra cả client lẫn server.

19. **Filename có thể ghi ra đường dẫn tùy ý không?** Server bỏ đường dẫn của tên client gửi và lưu bytes theo ID số do server tạo, không lấy filename làm đường dẫn đích. Download kiểm tra membership trước đọc tệp.

20. **Cơ sở dữ liệu ở đâu, sao không SQL?** Bản đầu dùng JSON để minh họa Stream/serialization và giảm bước cài demo. Bất lợi là copy/serialize toàn bộ, quét tuyến tính. Có thể thay Storage bằng SQLite transaction và index.

21. **Làm sao biết không chỉ là mock UI?** Chạy `Chat.Tests` với socket thật, `--ui-smoke` gửi qua nút WinForms và đọc lại ở client khác; tốt nhất demo thêm hai máy LAN.

22. **Ping được mà không chat được?** Ping dùng ICMP, không chứng minh dịch vụ TCP 5050 đang mở. Kiểm tra listener, IP/port, firewall và fingerprint. Dùng Test-NetConnection cho TCP port.

23. **Tại sao app không dùng HTTP/SMTP/FTP dù slide có?** Đề tài dùng giao thức chat riêng trên TCP; các chương khác giúp hiểu mô hình request/response, chuyển tiếp và truyền tệp. Gửi file trong app không phải triển khai FTP.

24. **Nếu phát triển tiếp, ưu tiên gì?** Kiểm thử LAN/DPI đầy đủ, SQLite, token phiên bảo vệ bằng DPAPI, reconnect/backoff và hàng đợi gửi có ID bền vững; sau đó upload chunk, phát media và E2EE nếu đủ thời gian.
