# Phạm vi kiểm thử

## Tự động

Kết quả trên môi trường Windows hiện tại ngày 09/10/2026: **54/54 kiểm tra tích hợp đạt**; build Debug và Release đều **0 lỗi, 0 cảnh báo**; UI smoke đạt ở cả bản Debug và bản đã publish. Không suy rộng kết quả localhost này thành kết quả chạy LAN.

Chạy `scripts/test.ps1`. Kết quả mỗi check trong `artifacts/tests/integration.txt`; ảnh UI và kết quả trong `artifacts/ui`. Các test dùng dữ liệu tạm mới cho từng lần, không có tài khoản thử được thêm vào server thật.

Nhóm ca kiểm thử: frame phân mảnh/gộp, Unicode, frame sai; hash/salt; TLS pin sai; xác thực; lời mời bạn bè; chat realtime/dedupe; typing; seen; text/file limits; quyền download; group owner/membership; gửi đồng thời; tìm kiếm/phân trang; lỗi lưu trữ và rollback trạng thái RAM; logout; nhiều thiết bị; ngắt kết nối; restart giữ chứng chỉ, lịch sử, file và read receipt.

UI smoke mở form thật ở ngoài vùng hiển thị, chụp bằng `DrawToBitmap`, gửi một tin qua nút **Gửi** và xác nhận từ client TCP khác. Ảnh minh họa chứa tài khoản/dữ liệu được tạo riêng bởi kịch bản test. Đây không phải ảnh một buổi demo LAN của nhóm.

## Cần kiểm chứng thủ công

Chưa thử trên hai máy vật lý; chưa đo hiệu năng/tải cực đại; chưa thử mọi tổ hợp DPI/độ phân giải; popup và focus/seen cần quan sát trên desktop của nhóm. Không có kết quả được tự ghi “đạt” cho các mục này. Theo checklist `04-demo.md` để bổ sung.

## Tiêu chí đạt trước khi nộp

Build không lỗi/cảnh báo; integration test mã thoát 0; UI smoke mã thoát 0; nhóm demo LAN đầy đủ với nhật ký; báo cáo phản ánh đúng tính năng hiện có. Test đúng chưa thay thế việc đọc và hiểu source để trả lời vấn đáp.
