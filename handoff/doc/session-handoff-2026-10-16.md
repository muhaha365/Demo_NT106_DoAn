# Bàn giao session — UIT Chat / NT106

Ngày tổng hợp: **16/10/2026**. Phiên xây dựng ứng dụng và kiểm thử ban đầu: **09/10/2026**.

Tài liệu này dùng để bắt đầu session mới mà không phải đọc lại toàn bộ hội thoại. Các đường dẫn tương đối trong nội dung tính từ thư mục gốc project, trừ những liên kết Markdown đã ghi rõ.

## 1. Trạng thái tại thời điểm bàn giao

- Đã xây dựng ứng dụng **UIT Chat** bằng C# WinForms, một server trung tâm và nhiều client giao tiếp qua TCP Socket được bảo vệ bằng TLS.
- Người dùng đã tự chạy ứng dụng và phản hồi ngày 16/10/2026: **“tôi đã chạy và thấy app tạm ổn”**. Chưa có lỗi cụ thể hay yêu cầu đổi tính năng mới được đưa ra.
- Yêu cầu mới nhất chỉ là tổng hợp session vào `handoff/doc`. Lượt này đọc lại cấu trúc project, tài liệu và log đã có; **không sửa code ứng dụng, không chạy lại build/test, không thay dữ liệu người dùng**.
- Kết quả kiểm thử đã lưu từ phiên triển khai: **54/54 kiểm tra tích hợp đạt**, build Debug/Release **0 lỗi, 0 cảnh báo**; UI smoke đạt ở bản Debug và bản publish.
- Đã có bản executable, README, tài liệu kỹ thuật, báo cáo Word và PowerPoint 16 slide.
- Chưa xác nhận demo trên hai máy LAN vật lý; chưa có video demo được quay.
- Git kiểm tra ngày 16/10/2026, trước khi thêm tài liệu này: branch `main`, commit `bbadac6` — `first commit`, working tree sạch. Tài liệu handoff được tạo sau lần kiểm tra đó; không tự commit/push trong lượt này.

## 2. Bối cảnh và mục tiêu của người dùng

Người dùng là sinh viên năm 2 Trường Đại học Công nghệ Thông tin, ngành Mạng máy tính, đang học **NT106 — Lập trình mạng căn bản**. Nhóm làm đồ án desktop app với đề tài **“Ứng dụng nhắn tin, chat”**.

Thực hành môn học dùng C# WinForms; đồ án được phép chọn ngôn ngữ khác. Đã lựa chọn tiếp tục C# WinForms trên project có sẵn để dễ liên hệ bài thực hành và giải thích khi vấn đáp.

Danh sách tính năng do nhóm tự chọn:

- Đăng ký username/password, đăng nhập/đăng xuất; lưu session là optional.
- Chat 1–1 realtime, lịch sử, phân biệt tin mình/người khác, timestamp.
- Online/offline và danh sách người đang online.
- Một server trung tâm, nhiều client, giao tiếp TCP Socket.
- Tìm user, thêm/xóa bạn bè.
- Typing, seen, emoji cơ bản.
- Tạo nhóm, thêm/xóa thành viên, chat nhóm.
- Thông báo tin mới bằng popup WinForms.
- Băm mật khẩu, mã hóa tin nhắn cơ bản, tìm tin trong đoạn chat.

Giảng viên còn gợi ý email/SĐT/Google và gửi file/ảnh/video. Bản này bổ sung gửi tệp và xem trước ảnh; **không triển khai đăng nhập Google/email/SĐT** vì nhóm đã chọn username/password.

Đồ án cần ghi chú quá trình để báo cáo giữa kỳ và cuối kỳ. Tiêu chí giảng viên: timeline/thời gian dành cho đề tài, báo cáo Word/PDF, PPT, demo live kèm video dự phòng, tính năng, công nghệ, độ khó, tính thực tiễn, giao diện, khó khăn/cách giải quyết, đúc kết và câu hỏi lý thuyết liên quan.

## 3. Workspace và tài liệu đầu vào

Thư mục gốc:

```text
C:\Users\longlong\Documents\DaiHoc\HK1 2026-2027\LTMCB\WinFormsApp1
```

Đề cương do người dùng cung cấp:

```text
C:\Users\longlong\Documents\DaiHoc\HK1 2026-2027\LTMCB\de_cuong_NT106.pdf
```

Thư mục slide:

```text
C:\Users\longlong\Documents\DaiHoc\HK1 2026-2027\LTMCB\drive-download-20261007T115714Z-1-001
```

Đề cương có 7 trang; bộ slide gồm 11 PDF, tổng 555 trang: giới thiệu, I/O, sockets, thread, web server, email server, file server, security, ICMP, IP packet và bảo vệ dữ liệu.

Phiên trước đã đọc đề cương, trích xuất toàn bộ phần chữ của các PDF và đối chiếu nội dung liên quan. `pypdf` làm thiếu chữ ở nhiều slide; chuyển sang **PyMuPDF** lấy được phần chữ đầy đủ hơn. Các trang chỉ chứa ảnh/sơ đồ không được chuyển thành source để dùng trực tiếp. Bản trích xuất nằm trong `docs/course-notes/`, được bỏ qua bởi `.gitignore`; không cần trích xuất lại nếu không có nhu cầu mới.

Điểm chính từ đề cương:

- Trọng tâm: mô hình client–server, TCP/UDP Socket, xử lý đồng thời, Stream/I/O và bảo mật.
- Buổi 8–9 báo cáo quá trình; buổi 14–15 seminar/báo cáo đồ án.
- A1 quá trình 30%, A2 thực hành 20%, A3 vấn đáp cuối kỳ 50%.
- Đề cương được biên soạn năm 2022. Mốc buổi học **không phải ngày deadline thực tế của lớp 2026–2027**.
- Một số cách diễn đạt/ví dụ cũ đã được lưu ý trong `docs/01-mon-hoc.md`: SHA-256 là 256 bit/32 byte; loopback không có nghĩa máy không kết nối mạng; TCP không giữ ranh giới thông điệp; dùng cancellation thay `Thread.Abort`.

## 4. Môi trường và cấu trúc source

Máy Windows, shell PowerShell. Phiên triển khai ghi nhận .NET SDK `10.0.401`, .NET/Desktop Runtime `10.0.12`. Đây là thông tin ghi nhận ngày 09/10, không phải kết quả kiểm tra SDK mới ngày 16/10.

- GUI target `net10.0-windows`; các project thư viện/console target `net10.0`.
- Không dùng SQL Server, cloud hay package NuGet ngoài cho ứng dụng.
- Solution: `WinFormsApp1.slnx`. Startup project GUI: `WinFormsApp1`.
- Project ban đầu là WinForms trống; đã mở rộng thành 5 project. Giữ tên project/executable `WinFormsApp1`, tên sản phẩm hiển thị là UIT Chat.

| Project / file | Trách nhiệm |
|---|---|
| `Chat.Core/Contracts.cs` | DTO request/response/event và các giới hạn |
| `Chat.Core/FrameCodec.cs` | Khung 4 byte độ dài big-endian + JSON UTF-8 |
| `Chat.Core/ChatClient.cs` | TCP/TLS client, request ID, receive loop, heartbeat, ngắt kết nối |
| `Chat.Server/Storage.cs` | Mô hình dữ liệu, lưu JSON, PBKDF2, chứng chỉ server |
| `Chat.Server/ChatServer.cs` | Accept loop, peer, dispatcher, xác thực/phân quyền, phát event |
| `Chat.Host/Program.cs` | Server console độc lập |
| `WinFormsApp1/Program.cs` | Entry point, cờ `--ui-smoke` |
| `WinFormsApp1/Form1.cs` | Launcher: mở server hoặc các client |
| `WinFormsApp1/LoginForm.cs` | Đăng ký/đăng nhập, IP/cổng/pin, nhớ cấu hình |
| `WinFormsApp1/ServerForm.cs` | Start/stop server, fingerprint, log, số người online |
| `WinFormsApp1/ChatForm.cs` | Hội thoại, người dùng, bạn bè, tin nhắn, tìm kiếm, typing/seen |
| `WinFormsApp1/MessageBubble.cs` | Vẽ bong bóng tin nhắn, metadata và trạng thái đã xem |
| `WinFormsApp1/Dialogs.cs` | Tạo nhóm, quản lý thành viên, xem/lưu attachment |
| `WinFormsApp1/Theme.cs` | Style giao diện, avatar, popup và helper marshal về UI thread |
| `WinFormsApp1/UiSmoke.cs` | Kịch bản test UI với server/client TCP thật và dữ liệu riêng |
| `Chat.Tests/Program.cs` | Chương trình integration test tự chạy |
| `scripts/test.ps1` | Build + integration tests + UI smoke, ghi log |
| `scripts/publish.ps1` | Build Release, publish client/server win-x64 |
| `scripts/make_reports.py` | Sinh báo cáo Word và PPT từ nội dung/tài liệu và screenshot |

## 5. Tính năng đã triển khai và quy ước

| Nhóm | Trạng thái / hành vi |
|---|---|
| Xác thực | Register/login/logout; username 3–24 ký tự a–z, 0–9, `_`, chuẩn hóa về chữ thường; password 8–128 ký tự |
| Chat 1–1 | Realtime, nội dung Unicode/xuống dòng, timestamp, bong bóng khác màu |
| Lịch sử | Lưu server, 50 tin/trang; tải tin cũ và tìm trên lịch sử lưu, không chỉ nội dung đang hiển thị |
| Presence | Online/offline, tab Mọi người/Bạn bè/Đang online; heartbeat 15 giây |
| Bạn bè | Tìm username, gửi/chấp nhận/từ chối lời mời, xóa quan hệ hai chiều |
| Typing/seen | Typing hết hạn; read cursor và danh sách người đã xem được lưu |
| Emoji | Menu emoji Unicode cơ bản |
| Nhóm | Tạo nhóm 2–50 người; chỉ trưởng nhóm thêm/xóa thành viên |
| Notification | Popup khi có lời mời hoặc tin ngoài chat đang xem/không focus; badge tin chưa đọc |
| Attachment | File/ảnh/video tối đa 5 MiB; download/lưu; preview ảnh hợp lệ; video mở ngoài app |
| Bảo mật | TLS + certificate pinning, PBKDF2-HMAC-SHA256, kiểm tra quyền tại server |

Quy ước nghiệp vụ cần giữ hoặc thảo luận trước khi thay đổi:

- Không bắt buộc kết bạn mới được chat. Xóa bạn không xóa lịch sử và không chặn nhắn tin.
- Thành viên mới được xem lịch sử nhóm. Thành viên bị xóa mất quyền đọc/gửi/tải tệp mới; dữ liệu đã tải về trước đó không thể thu hồi.
- “Đã gửi” nghĩa là server đã commit dữ liệu. “Đã xem” là read event của client khi mở đúng cuộc chat, có focus, không thu nhỏ và đang ở cuối danh sách; nhóm hiển thị số người khác đã xem.
- Có thể mở nhiều client cùng một tài khoản; chỉ offline khi không còn kết nối đã đăng nhập của tài khoản đó.
- Không có tài khoản/password mặc định. Các tên trong ảnh demo do UI test tạo trong dữ liệu tạm.

## 6. Các quyết định kỹ thuật quan trọng

### TCP, TLS và đồng thời

- TCP là byte stream: `FrameCodec` đọc header và payload bằng `ReadExactlyAsync`, xử lý phân mảnh/gộp; kiểm tra độ dài trước cấp phát. Frame tối đa **8 MiB**.
- JSON dùng UTF-8; `byte[]` được serialize thành Base64, nên file giới hạn **5 MiB**.
- Request có ID riêng; response ghép vào `TaskCompletionSource`. Event có ID rỗng: `state`, `message`, `typing`, `read`, `notice`.
- `clientId` của tin nhắn là khóa chống trùng theo sender khi retry cùng ID. Server gán message ID tăng dần, sender từ phiên xác thực và timestamp UTC.
- Server dùng async accept/read/write; mỗi peer có `Channel` gửi riêng, giới hạn 32 gói. Hàng đợi đầy ngắt peer chậm.
- `SemaphoreSlim` tuần tự hóa truy cập/thay đổi trạng thái. PBKDF2 chạy ngoài semaphore; lúc register kiểm tra trùng username trong khóa.
- UI nhận event qua `BeginInvoke`; phiên tải lịch sử và merge theo message ID tránh response cũ đè chat mới hoặc mất tin realtime.
- Giới hạn cấu hình: 100 kết nối, 1.000 tài khoản, 40 lệnh/giây/kết nối, 20 lần xác thực/IP/phút, 4.000 ký tự/tin, 100 ký tự/từ khóa tìm. Đây **không phải benchmark khả năng tải**.
- Handshake có timeout; heartbeat client 15 giây; server timeout đọc khung của peer đã login là 75 giây. Mất mạng vật lý có thể cần timeout thay vì nhận offline ngay.

### Xác thực và lưu trữ

- Người dùng ban đầu đề xuất SHA256. Bản triển khai dùng **PBKDF2-HMAC-SHA256, 600.000 vòng, salt ngẫu nhiên 16 byte, hash 32 byte**, so sánh constant-time.
- TLS qua `SslStream`, cho phép TLS 1.2/1.3 tùy hệ điều hành. Chứng chỉ tự ký được giữ qua restart, client pin SHA-256 của chứng chỉ lấy từ server; không có callback chấp nhận mọi chứng chỉ.
- TLS bảo vệ đường truyền client–server; **không phải E2EE**. Server đọc và lưu tin nhắn bản rõ; tệp trên đĩa cũng là bản rõ.
- Storage dùng JSON toàn bộ trong RAM. Giao dịch sửa một bản sao, ghi `.tmp`, flush, thay thế tệp chính, rồi cập nhật RAM và enqueue ACK/event. Nếu lưu lỗi thì không công bố bản sao.
- Có khóa tệp ngăn hai tiến trình server cùng dùng một thư mục dữ liệu. Mỗi lần ghi giữ phiên bản trước trong `.bak`.
- Nếu JSON lỗi khi khởi động, báo lỗi để phục hồi, không tự reset/xóa dữ liệu.
- Attachment lưu theo ID server, không dùng đường dẫn client cung cấp. Download kiểm tra quyền trong cuộc chat trước đọc tệp.

## 7. Chạy, kiểm thử và đóng gói

Chạy các lệnh từ thư mục gốc project:

```powershell
dotnet build WinFormsApp1.slnx
dotnet run --project WinFormsApp1
```

Hoặc mở Visual Studio, chọn `WinFormsApp1` làm Startup Project và F5.

Demo local:

1. Launcher → **Quản lý server → Khởi động**. Mặc định listen `127.0.0.1:5050`.
2. **Mở client**, lấy fingerprint local nếu chưa có, đăng ký tài khoản rồi đăng nhập.
3. Mở thêm client thứ hai, tạo/đăng nhập tài khoản khác.
4. Mục **Mọi người** chọn user để chat. Enter gửi; Shift+Enter xuống dòng.

Demo LAN: server listen `0.0.0.0`; client nhập IPv4 LAN thực tế của máy server, cổng tương ứng, dán fingerprint từ server qua kênh tin cậy. `0.0.0.0` không phải địa chỉ đích của client. Kiểm tra firewall/mạng khách/AP isolation. Script không tự đổi firewall.

Server console độc lập:

```powershell
dotnet run --project Chat.Host -- "$env:LOCALAPPDATA\UITChat\server" 5050 0.0.0.0
```

Không chạy GUI server và console server cùng thư mục dữ liệu đồng thời.

Kiểm thử:

```powershell
dotnet run --project Chat.Tests
dotnet run --project WinFormsApp1 -- --ui-smoke
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test.ps1
```

`Chat.Tests` là console test runner, **không phải project chạy bằng `dotnet test`**. Test dùng thư mục `%TEMP%\UITChat-tests-*` / `%TEMP%\UITChat-ui-*`, không sử dụng database thật.

Đóng gói:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\publish.ps1
```

Kết quả:

```text
artifacts/publish/UITChat/WinFormsApp1.exe
artifacts/publish/Server/Chat.Host.exe
```

Publish framework-dependent `win-x64`; máy đích cần .NET 10 Desktop Runtime x64 để chạy GUI. Chép **cả thư mục**, không chỉ một file `.exe`. Sau sửa code, publish lại để cập nhật bản phát cho nhóm.

## 8. Dữ liệu thật và những thứ cần bảo toàn

Server mặc định lưu ngoài workspace:

```text
%LOCALAPPDATA%\UITChat\server\
  chat.json
  chat.json.bak
  server.lock
  server.pfx
  fingerprint.txt
  files\<messageId>.bin
```

Client lưu cấu hình tại `%LOCALAPPDATA%\UITChat\client.json`: IP, port, fingerprint, username. **Không lưu password hay token tự đăng nhập**.

Người dùng đã chạy app nên có thể đã có tài khoản/tin nhắn thật. Không xóa/reset/seed đè dữ liệu này để thử tính năng. Khi thay storage/schema nên có backup và phương án migration. Không đưa PFX/khóa riêng hay dữ liệu chat thật vào báo cáo/source nộp.

Giữ nguyên các tài liệu học tập bên ngoài workspace và `note.ini` của người dùng. `.gitignore` bỏ qua `bin`, `obj`, `.vs`, `artifacts`, `.pfx` và `docs/course-notes`; do đó clone Git mới có thể cần build/tạo lại artifacts.

## 9. Bằng chứng kiểm thử đã có

Đọc trực tiếp ngày 16/10: log vẫn có `ALL 54 CHECKS PASSED.` và UI result là `PASS: real TCP UI scenario, screenshots and composer send.`. Đây là log lần chạy trước, **không phải chạy lại vào ngày tổng hợp**.

- `artifacts/tests/integration.txt`: 54 checks.
- `artifacts/ui/result.txt`: UI smoke.
- `artifacts/ui/chat.png`, `login.png`, `register.png`, `group.png`, `server.png`: ảnh form từ kịch bản TCP thật với dữ liệu test.

Integration test bao phủ frame phân mảnh/gộp/Unicode/độ dài sai, hash/salt, TLS pin sai, register/login, truy cập chưa xác thực, bạn bè, realtime/dedupe, typing, seen, file limit/download authorization, owner/membership nhóm, nhiều client gửi đồng thời, pagination/search, lỗi ghi đĩa không commit RAM, logout, nhiều thiết bị, ngắt kết nối và restart giữ dữ liệu/chứng chỉ.

UI smoke mở form ngoài vùng màn hình, chụp `DrawToBitmap`, thao tác nút **Gửi**, kiểm tra tin tới client khác qua TCP. Không coi đây là kiểm thử thủ công toàn bộ các nút hoặc mọi tình huống focus/DPI.

Đã sửa trong phiên trước: chữ slogan bị cắt, bố trí xác nhận mật khẩu, chiều cao hàng tìm kiếm; bổ sung chặn kết quả chọn user cũ đè lựa chọn mới. Các sửa này đã có trong bản bàn giao.

## 10. Tài liệu báo cáo đã tạo

| Tệp | Nội dung |
|---|---|
| [README.md](../../README.md) | Hướng dẫn chạy, LAN, build/test, tính năng và giới hạn |
| [docs/01-mon-hoc.md](../../docs/01-mon-hoc.md) | Đối chiếu đề cương và slide, các lưu ý lý thuyết |
| [docs/02-kien-truc.md](../../docs/02-kien-truc.md) | Kiến trúc, wire protocol, lệnh, mô hình dữ liệu và bảo mật |
| [docs/03-tien-do.md](../../docs/03-tien-do.md) | Timeline đề xuất, nhật ký khởi tạo, mẫu ghi giờ/đóng góp |
| [docs/04-demo.md](../../docs/04-demo.md) | Kịch bản live, video dự phòng, checklist và xử lý lỗi |
| [docs/05-van-dap.md](../../docs/05-van-dap.md) | 24 câu hỏi lý thuyết gắn với source |
| [docs/06-kiem-thu.md](../../docs/06-kiem-thu.md) | Kết quả/phạm vi đã kiểm chứng và phần cần thử thủ công |
| [docs/Bao-cao-UIT-Chat.docx](../../docs/Bao-cao-UIT-Chat.docx) | Bản báo cáo Word khởi đầu, có hình UI |
| [docs/Thuyet-trinh-UIT-Chat.pptx](../../docs/Thuyet-trinh-UIT-Chat.pptx) | PowerPoint 16 slide |

Script sinh báo cáo: `python -X utf8 scripts/make_reports.py`, cần `python-docx`, `python-pptx`; lấy screenshot từ `artifacts/ui`. Chạy lại script sẽ ghi đè Word/PPT, nên giữ bản sửa tay của nhóm trước khi sinh lại.

Word/PPT đã kiểm tra cấu trúc bằng thư viện Python; đã sửa một textbox vượt canvas ở PPT. Thử xuất PDF bằng Office COM bị treo; đã dừng tiến trình tự khởi tạo. **Không có PDF được bàn giao và không coi việc render Word/PPT qua Office là đã thành công**. Các bản `.docx`/`.pptx` vẫn tồn tại, dùng được như tài liệu nguồn.

Các trường tên nhóm, MSSV, giảng viên, deadline thực tế, giờ làm và kết quả LAN để trống cho nhóm điền. Ước lượng timeline không phải số giờ nhóm đã thực làm. Chưa quay video dự phòng.

Nguồn kỹ thuật bổ sung đã dùng trong phiên trước:

- Microsoft: https://learn.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=net-10.0
- OWASP: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html

## 11. Giới hạn và việc còn lại

Chưa triển khai:

- Lưu session/token tự đăng nhập (optional trong yêu cầu gốc).
- Google/email/SĐT, quên mật khẩu, gọi thoại/video.
- Phát video trong app; video hiện là attachment tải về.
- Push khi ứng dụng đã tắt.
- E2EE, mã hóa dữ liệu lưu trên đĩa, PKI/thu hồi/đổi chứng chỉ tự động.
- Reconnect tự động, hàng đợi tin bền vững qua phiên, upload chunk/resume.

Giới hạn hiện thực:

- JSON nạp/copy/ghi toàn bộ, tìm kiếm tuyến tính; snapshot trạng thái gửi toàn bộ. Chưa tối ưu lịch sử lớn hoặc tải cao.
- Upload file cùng luồng TCP có thể khiến text chờ; lỗi ghi metadata có thể để lại attachment mồ côi, chưa có tác vụ dọn.
- Khi timeout gửi, server có thể đã lưu nhưng response chưa tới. UI không tự retry; cần đăng nhập lại/kiểm tra lịch sử trước gửi lại. Dedupe backend có hỗ trợ nếu retry cùng `clientId`, nhưng UI chưa giữ ID đó qua nhiều phiên.
- Những nội dung đã tải về trước khi bị xóa khỏi nhóm không thể thu hồi.

Cần kiểm chứng thủ công: hai máy LAN, firewall và mất Wi-Fi, popup/seen theo focus, DPI 125%/150%, tệp gần giới hạn, tốc độ/khả năng tải nếu muốn đưa số liệu vào báo cáo. Không diễn giải phản hồi “app tạm ổn” của người dùng thành xác nhận tất cả các mục này.

## 12. Cách tiếp tục ở session mới

1. Đọc tài liệu này, README và `docs/02-kien-truc.md`; kiểm tra `git status`, AGENTS.md nếu có, rồi đọc source liên quan đến yêu cầu mới.
2. Giữ bối cảnh là đồ án năm 2 cần hiểu và vấn đáp được. Giải thích bằng tiếng Việt, ưu tiên code rõ ràng và có thể liên hệ TCP/Stream/thread/bảo mật.
3. Không dựng lại từ đầu, không tự đổi stack/framework/storage chỉ vì có thể. Những ý tưởng dưới đây là đề xuất, **chưa phải yêu cầu triển khai tiếp của người dùng**.
4. Nếu sửa code: chạy kiểm tra phù hợp, cập nhật tài liệu bị ảnh hưởng, publish lại nếu bàn giao executable mới. Dùng dữ liệu test riêng.
5. Không tự điền giờ làm, đóng góp thành viên, kết quả LAN hay giả lập video thành buổi demo thật.

Các hướng có thể trao đổi khi người dùng yêu cầu phát triển tiếp: hoàn thiện UI theo phản hồi thực tế; ghi kết quả LAN; lưu session an toàn; reconnect có kiểm soát; chuyển JSON sang SQLite kèm migration; thêm upload chunk/preview media; bổ sung ảnh/video demo và báo cáo.

### Prompt mẫu để bắt đầu session mới

```text
Hãy đọc file:
C:\Users\longlong\Documents\DaiHoc\HK1 2026-2027\LTMCB\WinFormsApp1\handoff\doc\session-handoff-2026-10-16.md

Đây là tài liệu bàn giao của đồ án UIT Chat, môn NT106.
Ứng dụng C# WinForms + TCP/TLS đã được xây dựng và tôi đã chạy thấy tạm ổn.
Hãy tiếp tục dựa trên source hiện tại, bảo toàn dữ liệu của tôi và phân biệt
kết quả test cũ với những gì bạn kiểm chứng trong session mới.

Yêu cầu tiếp theo của tôi là: [điền công việc muốn thực hiện].
```
