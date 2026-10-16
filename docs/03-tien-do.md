# Timeline và nhật ký đồ án

## Thông tin nhóm cần điền

Tên nhóm: [điền]. Lớp: [điền]. Giảng viên: [điền]. Thành viên/MSSV: [điền]. Ngày báo cáo giữa kỳ/cuối kỳ theo lớp: [điền]. Không có số giờ thực làm được tự điền thay cho nhóm.

## Kế hoạch đề xuất theo mốc môn học

| Mốc tương đối | Kết quả cần có | Ước lượng công sức cả nhóm | Người phụ trách |
|---|---|---|---|
| Trước buổi 4 | Chốt use case, mockup, phân công, quy tắc Git | 6–10 giờ | [điền] |
| Buổi 4–5 | TCP framing, client/server nhiều kết nối, TLS | 12–18 giờ | [điền] |
| Buổi 5–6 | Đăng ký/đăng nhập, lưu dữ liệu, chat 1–1 | 12–18 giờ | [điền] |
| Buổi 6–7 | Lịch sử, presence, bạn bè, UI cơ bản | 10–16 giờ | [điền] |
| Buổi 8–9 | Demo giữa kỳ, giải thích giao thức, nhận phản hồi | 6–10 giờ | Cả nhóm |
| Buổi 9–11 | Nhóm, typing, seen, emoji, tìm kiếm, tệp | 14–22 giờ | [điền] |
| Buổi 11–13 | Test LAN, lỗi mạng, quyền nhóm, chỉnh UI | 12–18 giờ | [điền] |
| Trước buổi 14–15 | Chốt báo cáo/PPT, quay video, diễn tập vấn đáp | 10–14 giờ | Cả nhóm |

Đây là ước lượng để nhóm lập kế hoạch học và hoàn thiện sản phẩm; không phải lịch sử thời gian đã thực hiện. Bản source được hỗ trợ tạo ban đầu đã có nhiều chức năng; nhóm vẫn cần hiểu, thử, điều chỉnh và ghi nhận đóng góp thật.

## Nhật ký khởi tạo — 09/10/2026

| Công việc đã thực hiện trong phiên xây dựng | Minh chứng |
|---|---|
| Đọc đề cương 7 trang và trích xuất chữ trong 11 slide PDF | `01-mon-hoc.md`, `course-notes` |
| Tách giao thức, server, host, GUI và test | 5 project trong solution |
| Triển khai TCP framing + TLS + xác thực + lưu JSON | `Chat.Core`, `Chat.Server` |
| Triển khai chat, bạn bè, nhóm, tệp và thông báo | `ChatForm.cs`, `Dialogs.cs` |
| Viết và chạy integration test, chụp các cửa sổ GUI qua test TCP thật | `Chat.Tests`, `UiSmoke.cs`, `artifacts/tests`, `artifacts/ui` |
| Soạn tài liệu kiến trúc, báo cáo, slide và kịch bản demo | `docs` |

Kết quả đợt kiểm thử đầu: 51 kiểm tra đạt. Các kiểm tra bổ sung về lỗi ghi đĩa và nhiều thiết bị được lưu cùng kết quả mới nhất trong `artifacts/tests/integration.txt`. Xem `06-kiem-thu.md` để biết phạm vi kiểm thử chính xác.

## Khó khăn và cách xử lý có thể trình bày

| Vấn đề | Cách xử lý trong code | Điều nhóm nên tự thử |
|---|---|---|
| TCP có thể chia một JSON qua nhiều lần nhận | Header 4 byte và ReadExactlyAsync | Xem test FragmentStream |
| Nhiều người gửi cùng lúc làm xung đột ID/lưu dữ liệu | Semaphore cho giao dịch; sequence server; writer riêng | Ba client gửi đồng thời |
| Gửi mạng trên UI khiến cửa sổ đứng | async I/O, BeginInvoke khi nhận event | Kéo cửa sổ khi gửi tệp |
| Lịch sử tải về chậm đè cuộc chat đang chọn | Bộ đếm phiên tải và merge theo ID | Chuyển liên tục giữa hai chat |
| Thành viên cũ vẫn gọi lệnh sau khi bị xóa | Server kiểm tra membership trên từng lệnh | Thử đọc/gửi/tải file sau khi bị xóa |
| Đứt mạng không biết tin đã lưu chưa | ACK sau commit; ID chống trùng ở server; kiểm tra lịch sử trước gửi lại | Dừng server giữa lúc gửi |
| Client không tin chứng chỉ tự ký | Pin SHA-256 lấy từ server, giữ chứng chỉ qua restart | Dán sai fingerprint |
| Giao diện bị cắt chữ/nút khi sắp xếp control | Chụp các form, sửa kích thước và layout | Thử DPI 125%/150% trên máy nhóm |

## Mẫu nhật ký cập nhật hằng tuần

| Ngày | Thành viên | Công việc / mã issue | Giờ thực tế | Kết quả / commit / ảnh | Khó khăn / cách xử lý | Việc tiếp theo |
|---|---|---|---:|---|---|---|
| [điền] | [điền] | [điền] | [điền] | [điền] | [điền] | [điền] |

## Checklist giữa kỳ

- Chạy được server + ít nhất 2 client, đăng ký/đăng nhập và chat 1–1.
- Chỉ ra frame TCP, đường đi tin nhắn và dữ liệu lưu ở đâu.
- Bảng phân công, thời gian thực tế, tính năng đã/chưa kiểm chứng.
- Demo mất kết nối, chụp minh chứng; phản hồi giảng viên được ghi lại.

## Checklist cuối kỳ

- Mỗi người giải thích phần phụ trách và ít nhất một luồng xuyên client–server.
- Có kết quả LAN trên các máy thật, slide, Word/PDF, source và bản đóng gói.
- Có video demo dự phòng do nhóm quay, kiểm tra phát được trước buổi báo cáo.
- Tách tính năng đã chạy khỏi hướng phát triển; không trình bày test tự động như kết quả đo hiệu năng.
