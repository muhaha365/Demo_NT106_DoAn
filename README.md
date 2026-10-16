# UIT Chat — Đồ án NT106

Ứng dụng desktop C# WinForms, nhiều client kết nối một server trung tâm bằng **TCP Socket + TLS**. Project sử dụng .NET 10 đã có trên máy hiện tại, không cần SQL Server, dịch vụ cloud hay gói NuGet ngoài.

## Chạy nhanh

1. Mở `WinFormsApp1.slnx` bằng Visual Studio hỗ trợ .NET 10, chọn `WinFormsApp1` làm Startup Project, nhấn F5. Hoặc chạy `dotnet run --project WinFormsApp1` từ thư mục này.
2. Chọn **Quản lý server → Khởi động**. Giữ cửa sổ server mở.
3. Chọn **Mở client**. Giữ IP `127.0.0.1`, cổng `5050`; bấm **Lấy mã server trên máy này** nếu mã chứng chỉ chưa được điền.
4. Chọn **Đăng ký**, tạo hai tài khoản khác nhau. Username 3–24 ký tự `a-z`, `0-9`, `_`; mật khẩu 8–128 ký tự. Không có tài khoản/mật khẩu mặc định.
5. Mở thêm một client, đăng nhập tài khoản thứ hai. Trong mục **Mọi người**, chọn người cần chat. Mỗi cửa sổ là một kết nối TCP riêng.
6. Nhấn Enter gửi tin; Shift+Enter xuống dòng. Dùng **Kết bạn**, **Lời mời kết bạn**, **Tạo nhóm mới**, **Thành viên**, **Đính kèm** để thử các chức năng.

Sau khi đóng gói, chạy `artifacts/publish/UITChat/WinFormsApp1.exe`. Giữ nguyên cả thư mục bên cạnh `.exe`. Bản đóng gói phụ thuộc **.NET 10 Desktop Runtime x64** trên máy đích.

## Các tính năng đã hiện thực

| Nhóm | Chức năng |
|---|---|
| Xác thực | Đăng ký username/password, đăng nhập, đăng xuất; username không phân biệt hoa thường |
| Chat 1–1 | Realtime, lịch sử phân trang, phân biệt tin mình/người khác, thời gian gửi theo giờ máy client |
| Trạng thái | Online/offline, danh sách online, heartbeat và phát hiện ngắt kết nối |
| Bạn bè | Tìm username, gửi/chấp nhận/từ chối lời mời, xóa bạn hai chiều |
| Chat nâng cao | Typing có thời hạn, đã xem, emoji Unicode, tìm toàn bộ lịch sử cuộc chat |
| Nhóm | Tạo nhóm, chat nhóm; trưởng nhóm thêm/xóa thành viên; tối đa 50 người |
| Thông báo | Popup WinForms khi có tin ngoài cuộc chat đang xem hoặc lời mời kết bạn; số tin chưa đọc |
| Tệp | Gửi/lưu file, ảnh, video tối đa 5 MB; xem trước ảnh; video tải về để mở ngoài app |
| Bảo mật | PBKDF2-HMAC-SHA256 với salt riêng, TLS 1.2/1.3 tùy hệ điều hành, kiểm tra dấu vân tay chứng chỉ; phân quyền tại server |

**Quy ước:** Không bắt buộc kết bạn để chat. Xóa bạn vẫn giữ lịch sử và khả năng chat. Thành viên mới được xem lịch sử nhóm; thành viên bị xóa không được tải lịch sử/tệp hay gửi tin mới, nhưng những gì đã tải trước đó không thể thu hồi. “Đã gửi” nghĩa là server đã lưu tin, “đã xem” nghĩa là client người nhận đã mở cuộc chat, cửa sổ có focus và đang ở cuối danh sách. Trong nhóm, hiển thị số người khác đã xem.

**Chưa triển khai:** tự động đăng nhập bằng session lưu trên đĩa (mục optional); email/SĐT/Google; phát video trong app; gọi thoại/video; push khi app đã tắt; mã hóa đầu cuối (E2EE). App chỉ nhớ IP, cổng, mã chứng chỉ và username, không nhớ mật khẩu. Khi mất kết nối, đăng nhập lại để đồng bộ lịch sử.

## Demo qua LAN

1. Máy server: nhập `0.0.0.0` vào **IP lắng nghe**, khởi động server; dùng `ipconfig` xác định IPv4 LAN, ví dụ `192.168.1.20`.
2. Nếu Windows Firewall hỏi, cho phép ứng dụng trên mạng Private của nhóm. Nếu bị chặn, người quản lý máy tạo rule inbound TCP cho cổng đang dùng. Script không tự sửa firewall.
3. Máy client: nhập IPv4 LAN của máy server, cổng `5050`, dán SHA-256 lấy từ cửa sổ server qua kênh nhóm tin cậy. Không nhập `0.0.0.0` làm địa chỉ đích.
4. Các máy cùng LAN; mạng khách/AP isolation có thể ngăn hai máy kết nối. Kiểm tra `Test-NetConnection 192.168.1.20 -Port 5050`.
5. Không cần Internet khi chạy. Không mở cổng router ra Internet để demo lớp.

Server console độc lập (tham số: thư mục dữ liệu, cổng, IP):

```powershell
dotnet run --project Chat.Host -- "$env:LOCALAPPDATA\UITChat\server" 5050 0.0.0.0
```

Không chạy server GUI và console cùng thư mục dữ liệu cùng lúc: khóa tệp ngăn hai tiến trình ghi đè dữ liệu.

## Build, kiểm thử và đóng gói

```powershell
dotnet build WinFormsApp1.slnx
dotnet run --project Chat.Tests
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
```

Kiểm thử giao diện thật trên Windows (server/dữ liệu test tách biệt, ảnh trong `artifacts/ui`):

```powershell
dotnet run --project WinFormsApp1 -- --ui-smoke
```

`Chat.Tests` là chương trình integration test tự chạy, mã thoát 0 khi thành công; không dùng `dotnet test`. Kiểm thử tạo dữ liệu ở `%TEMP%\UITChat-tests-*`; không thay dữ liệu thật.

## Dữ liệu và cấu trúc code

- `Chat.Core`: DTO, khung tin, TCP/TLS client dùng chung.
- `Chat.Server`: listener, xử lý yêu cầu, xác thực/phân quyền, lưu dữ liệu.
- `Chat.Host`: console server chạy riêng.
- `WinFormsApp1`: giao diện client + giao diện quản lý server.
- `Chat.Tests`: kiểm thử với nhiều kết nối TCP thật.
- `docs`: thiết kế, báo cáo, timeline, kịch bản demo, câu hỏi vấn đáp.

Server lưu tại `%LOCALAPPDATA%\UITChat\server`: `chat.json`, bản sao trước lần ghi gần nhất `chat.json.bak`, chứng chỉ riêng `server.pfx`, dấu vân tay `fingerprint.txt`, tệp đính kèm `files/*.bin`. Client chỉ truy cập dữ liệu qua server, không đọc JSON trực tiếp.

TLS bảo vệ dữ liệu **khi truyền**. Tin nhắn và tệp trên đĩa server hiện là bản rõ; PFX chứa khóa riêng. Không chia sẻ thư mục server khi nộp source; backup vào vị trí riêng do nhóm kiểm soát. Mỗi lần lưu ghi tệp tạm + flush + thay thế tệp chính; nếu `chat.json` hỏng, server báo lỗi và giữ nguyên tệp để nhóm phục hồi, không tự xóa dữ liệu.

Đây là kiến trúc cho đồ án quy mô nhỏ: dữ liệu JSON được nạp trong RAM, sao chép/lưu lại khi ghi, tìm kiếm tuyến tính. Giới hạn kết nối là 100 và tài khoản là 1.000, **không phải số liệu benchmark**. Muốn mở rộng nên chuyển sang SQLite/PostgreSQL, chỉ mục tìm kiếm, phân trang danh sách người dùng và event cập nhật từng phần.

## Tài liệu báo cáo

- [Đối chiếu môn học](docs/01-mon-hoc.md)
- [Kiến trúc và giao thức](docs/02-kien-truc.md)
- [Tiến độ và nhật ký](docs/03-tien-do.md)
- [Demo và kiểm thử thủ công](docs/04-demo.md)
- [Câu hỏi vấn đáp](docs/05-van-dap.md)
- [Báo cáo Word](docs/Bao-cao-UIT-Chat.docx) và [slide PowerPoint](docs/Thuyet-trinh-UIT-Chat.pptx)

Word/PPT là bản khởi đầu theo phần mềm đã có; cần điền thành viên, MSSV, lịch thực tế, số giờ thực làm và kết quả demo LAN của nhóm. Video demo cần nhóm quay sau khi chốt bài thuyết trình, theo kịch bản trong tài liệu.
