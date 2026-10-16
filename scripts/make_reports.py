"""Rebuild Word/PPT from the source notes. Requires python-docx and python-pptx.
Run after --ui-smoke to include actual screenshots from the isolated test scenario.
"""
from pathlib import Path
import re
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from pptx import Presentation
from pptx.util import Inches, Pt as PptPt
from pptx.dml.color import RGBColor as PptColor
from pptx.enum.shapes import MSO_SHAPE, MSO_CONNECTOR

ROOT = Path(__file__).resolve().parent.parent
DOCS = ROOT / 'docs'
TEAL = '0F766E'
INK = '182D30'
MUTED = '6B7D80'
PALE = 'E5F3ED'
BG = 'F6F8F7'

def clean(s):
    s = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'\1 (\2)', s)
    return s.replace('**', '').replace('`', '')

doc = Document()
section = doc.sections[0]
section.page_width, section.page_height = Cm(21), Cm(29.7)
section.top_margin = section.bottom_margin = Cm(2)
section.left_margin = section.right_margin = Cm(2.2)
normal = doc.styles['Normal']
normal.font.name, normal.font.size = 'Calibri', Pt(11)
normal.font.color.rgb = RGBColor.from_string(INK)
normal.paragraph_format.space_after = Pt(6)
normal.paragraph_format.line_spacing = 1.12
for style_name in ('Title', 'Heading 1', 'Heading 2', 'Heading 3'):
    style = doc.styles[style_name]
    style.font.name = 'Calibri'
    style.font.color.rgb = RGBColor.from_string(TEAL)
header = section.header.paragraphs[0]
header.text = 'UIT / CHAT                                               NT106 · LẬP TRÌNH MẠNG CĂN BẢN'
header.style = doc.styles['Caption']
footer = section.footer.paragraphs[0]
footer.text = 'Bản khởi đầu 09/10/2026 · Nhóm bổ sung thông tin thực tế                       '
field = OxmlElement('w:fldSimple'); field.set(qn('w:instr'), 'PAGE'); footer._p.append(field)
footer.style = doc.styles['Caption']
doc.add_paragraph('TRƯỜNG ĐẠI HỌC CÔNG NGHỆ THÔNG TIN\nĐẠI HỌC QUỐC GIA TP. HỒ CHÍ MINH')
doc.add_paragraph('\nĐỒ ÁN MÔN HỌC', 'Subtitle')
doc.add_paragraph('UIT CHAT', 'Title')
doc.add_paragraph('Ứng dụng nhắn tin desktop\nKiến trúc client–server trên TCP Socket', 'Subtitle')
doc.add_paragraph('Môn học: NT106 – Lập trình mạng căn bản\nNăm học: 2026–2027\nNhóm / lớp: [điền]\nGiảng viên: [điền]\nThành viên và MSSV: [điền]')
doc.add_paragraph('Bản tài liệu khởi đầu dựa trên phần mềm đã hiện thực. Các trường thông tin nhóm, thời gian thực làm và kết quả LAN cần được bổ sung trước khi nộp.')
doc.add_paragraph('09 tháng 10 năm 2026')
doc.add_page_break()
doc.add_heading('Tổng quan đề tài', 1)
doc.add_paragraph('UIT Chat là ứng dụng desktop phục vụ trao đổi trong nhóm học tập nhỏ. Người dùng đăng ký tài khoản, trò chuyện riêng hoặc theo nhóm, chia sẻ tệp và tra cứu lịch sử. Server trung tâm xác thực người dùng, kiểm tra quyền truy cập và lưu dữ liệu. Các client giao tiếp trực tiếp qua TCP được bảo vệ bởi TLS.')
doc.add_heading('Phạm vi sản phẩm', 2)
doc.add_paragraph('Đã hiện thực: username/password, đăng nhập/đăng xuất, chat 1–1/nhóm, bạn bè, presence, typing, seen, emoji, lịch sử/tìm kiếm, popup khi app đang chạy, truyền tệp tối đa 5 MB và xem trước ảnh. Tính năng optional lưu session đăng nhập chưa triển khai; client chỉ nhớ cấu hình và username.')
doc.add_heading('Tính thực tiễn và độ khó', 2)
doc.add_paragraph('Ứng dụng phù hợp demo phòng thực hành, trao đổi trong LAN hoặc nhóm học tập. Phần khó tập trung ở ranh giới thông điệp trên TCP, nhiều client gửi đồng thời, tính nhất quán khi lưu, phân quyền nhóm, cập nhật UI từ tác vụ nền và xác minh chứng chỉ tự ký. Đây chưa phải hệ thống nhắn tin vận hành quy mô lớn.')
doc.add_heading('Cấu trúc báo cáo', 2)
for text in ['Đối chiếu đề cương và nội dung slide', 'Kiến trúc, giao thức, dữ liệu và bảo mật', 'Timeline, nhật ký, khó khăn và giải pháp', 'Kịch bản demo và checklist thử nghiệm', 'Câu hỏi lý thuyết gắn với source', 'Phạm vi kiểm thử và ảnh giao diện', 'Đúc kết và hướng phát triển']:
    doc.add_paragraph(text, 'List Bullet')

def table(rows):
    rows = [row for row in rows if not all(re.fullmatch(r':?-+:?', cell.strip()) for cell in row)]
    if not rows: return
    t = doc.add_table(rows=0, cols=len(rows[0])); t.style = 'Light Shading Accent 1'
    for n, row in enumerate(rows):
        cells = t.add_row().cells
        for cell, text in zip(cells, row):
            cell.text = clean(text.strip())
            for paragraph in cell.paragraphs:
                for run in paragraph.runs: run.font.size = Pt(9)
            if n == 0:
                shade = OxmlElement('w:shd'); shade.set(qn('w:fill'), TEAL); cell._tc.get_or_add_tcPr().append(shade)
                for run in cell.paragraphs[0].runs: run.font.bold = True; run.font.color.rgb = RGBColor(255,255,255)
        if n == 0:
            repeat = OxmlElement('w:tblHeader'); t.rows[0]._tr.get_or_add_trPr().append(repeat)
    doc.add_paragraph()

def markdown_file(path):
    doc.add_page_break()
    lines = path.read_text(encoding='utf-8').splitlines(); i=0; code=False; lang=''
    while i < len(lines):
        line=lines[i]
        if line.startswith('```'):
            if not code: lang=line[3:]
            code=not code; i+=1; continue
        if code:
            if lang != 'mermaid':
                p=doc.add_paragraph(line); p.paragraph_format.space_after=Pt(0)
                for run in p.runs: run.font.name='Consolas'; run.font.size=Pt(8)
        elif line.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].startswith('|'):
                rows.append(lines[i].strip('|').split('|')); i+=1
            table(rows); continue
        elif line.startswith('#'):
            match=re.match(r'^(#+)\s+(.*)',line)
            if match: doc.add_heading(clean(match.group(2)), min(len(match.group(1)),3))
        elif line.startswith('- '): doc.add_paragraph(clean(line[2:]), 'List Bullet')
        elif line.strip(): doc.add_paragraph(clean(line))
        i+=1

for name in ['01-mon-hoc.md','02-kien-truc.md','03-tien-do.md','04-demo.md','05-van-dap.md','06-kiem-thu.md']:
    markdown_file(DOCS/name)
doc.add_page_break(); doc.add_heading('Ảnh giao diện và minh chứng',1)
for filename, caption in [('login.png','Màn hình đăng nhập và cấu hình kết nối'),('chat.png','Chat nhóm, thời gian gửi và trạng thái đã xem'),('server.png','Cửa sổ quản lý server'),('group.png','Chọn thành viên tạo nhóm')]:
    path=ROOT/'artifacts'/'ui'/filename
    if path.exists():
        doc.add_picture(str(path), width=Cm(16))
        doc.add_paragraph(caption+' — ảnh từ kịch bản UI smoke với dữ liệu test riêng.', 'Caption')
doc.add_heading('Đúc kết',1)
doc.add_paragraph('Bản triển khai minh họa một ứng dụng mạng hoàn chỉnh từ giao diện đến socket, xác thực, lưu trữ và phân quyền. Lựa chọn async I/O, khung tin có độ dài, ACK sau commit và kiểm tra quyền ở server xử lý các vấn đề quan trọng của ứng dụng chat. Các giới hạn còn lại là lưu JSON toàn bộ, chưa tự phục hồi phiên, chưa E2EE và chưa đo trên mạng nhiều máy. Giá trị học tập nằm ở việc nhóm hiểu và giải thích được các lựa chọn này, tự kiểm chứng trong LAN và ghi nhật ký đóng góp thực tế.')
doc.save(DOCS/'Bao-cao-UIT-Chat.docx')

prs=Presentation(); prs.slide_width=Inches(13.333); prs.slide_height=Inches(7.5)
def rgb(value): return PptColor.from_string(value)
def box(slide,x,y,w,h,color=PALE,radius=True):
    s=slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE if radius else MSO_SHAPE.RECTANGLE,Inches(x),Inches(y),Inches(w),Inches(h))
    s.fill.solid(); s.fill.fore_color.rgb=rgb(color); s.line.fill.background(); return s
def text(slide,value,x,y,w,h=1,size=22,color=INK,bold=False):
    shape=slide.shapes.add_textbox(Inches(x),Inches(y),Inches(w),Inches(h)); tf=shape.text_frame; tf.word_wrap=True
    for i,line in enumerate(value.split('\n')):
        p=tf.paragraphs[0] if i==0 else tf.add_paragraph(); p.text=line
        p.font.name='Calibri'; p.font.size=PptPt(size); p.font.color.rgb=rgb(color); p.font.bold=bold; p.space_after=PptPt(12)
    return shape
def slide(title,kicker='ĐỒ ÁN NT106'):
    s=prs.slides.add_slide(prs.slide_layouts[6]); s.background.fill.solid(); s.background.fill.fore_color.rgb=rgb(BG)
    box(s,0,0,.16,7.5,TEAL,False); text(s,kicker,.6,.25,11,.4,11,TEAL,True); text(s,title,.6,.85,12,1.1,32,INK,True)
    text(s,'UIT / CHAT · Bản khởi đầu — bổ sung thông tin nhóm và kết quả LAN',.65,7.05,11,.3,10,MUTED)
    text(s,f'{len(prs.slides):02}',12.1,7.03,.6,.3,11,TEAL,True); return s
def bullets(s,items,x=.7,y=2,w=11.9,size=23):
    text(s,'\n'.join('• '+item for item in items),x,y,w,min(4.7,6.65-y),size)
def cards(s,left_title,left,right_title,right):
    box(s,.65,2,5.85,4.6); box(s,6.75,2,5.85,4.6,'FFFFFF')
    text(s,left_title,.95,2.25,5.2,.6,24,TEAL,True); text(s,right_title,7.05,2.25,5.2,.6,24,TEAL,True)
    text(s,'\n'.join('• '+v for v in left),.95,3,5.1,3.4,21); text(s,'\n'.join('• '+v for v in right),7.05,3,5.1,3.4,21)

s=slide('UIT Chat','C# WINFORMS / TCP SOCKET / CLIENT–SERVER')
text(s,'Kết nối bạn bè.\nChia sẻ mỗi ngày.',.8,2,8,2.2,40,TEAL,True)
text(s,'Nhóm: [điền]    ·    Thành viên / MSSV: [điền]\nGiảng viên / lớp: [điền]\nNăm học 2026–2027',.85,4.8,11,1.5,20,MUTED)
s=slide('Bài toán và tính thực tiễn')
cards(s,'Nhu cầu',['Trao đổi trong nhóm học tập','Chat riêng và phối hợp theo nhóm','Tìm lại nội dung, chia sẻ tài liệu'],'Phạm vi bản hiện tại',['Desktop Windows, mạng LAN','Server trung tâm, dữ liệu lưu bền','Sản phẩm học tập quy mô nhỏ'])
s=slide('Bám sát nội dung môn học')
bullets(s,['Chương 2: Stream, UTF-8, serialization và lưu tệp','Chương 3: TcpClient / TcpListener, IP và port','Chương 3B: đồng thời, tác vụ nền và UI thread','Chương 10: hash, mã hóa và chứng chỉ','Buổi 8–9 báo cáo quá trình; buổi 14–15 báo cáo đồ án'],size=23)
s=slide('Timeline và phân công dự kiến')
cards(s,'Đến giữa kỳ',['Buổi 4–5: giao thức TCP và TLS','Buổi 5–7: xác thực, chat, lịch sử','Buổi 8–9: demo và nhận phản hồi'],'Đến cuối kỳ',['Buổi 9–11: nhóm, seen, tệp','Buổi 11–13: test LAN, chỉnh UI','Buổi 14–15: báo cáo và vấn đáp'])
s.notes_slide.notes_text_frame.text='Ước lượng công sức và bảng nhật ký trong docs/03-tien-do.md. Điền người phụ trách, giờ thực tế và deadline lớp. Không trình bày lịch đề xuất như công việc nhóm đã thực hiện.'
s=slide('Kiến trúc một server, nhiều client')
for i,label in enumerate(['Client A','Client B','Client C']):
    box(s,.85,2.15+i*1.35,2.5,.85); text(s,label,1.15,2.32+i*1.35,2,.5,22,TEAL,True)
    connector=s.shapes.add_connector(MSO_CONNECTOR.STRAIGHT,Inches(3.35),Inches(2.6+i*1.35),Inches(5.3),Inches(3.95)); connector.line.color.rgb=rgb(TEAL)
box(s,5.3,3.05,2.65,1.8,TEAL); text(s,'TCP + TLS\nChat Server',5.55,3.3,2.2,1.2,23,'FFFFFF',True)
box(s,9.65,2.6,2.6,1.1); text(s,'chat.json',9.9,2.85,2.2,.5,23,TEAL,True)
box(s,9.65,4.4,2.6,1.1); text(s,'files/*.bin',9.9,4.65,2.2,.5,23,TEAL,True)
for y in [3.15,4.95]:
    line=s.shapes.add_connector(MSO_CONNECTOR.STRAIGHT,Inches(7.95),Inches(3.95),Inches(9.65),Inches(y)); line.line.color.rgb=rgb(TEAL)
s=slide('Công nghệ và cấu trúc source')
cards(s,'Công nghệ',['C# / .NET 10 / WinForms','TcpListener, TcpClient, SslStream','System.Text.Json + FileStream','async/await, SemaphoreSlim, Channel'],'Các project',['Chat.Core: giao thức + client','Chat.Server / Chat.Host: server','WinFormsApp1: giao diện','Chat.Tests: kiểm thử TCP thật'])
s=slide('Giao thức ứng dụng trên TCP')
box(s,.85,2.05,3.2,1.0,TEAL); text(s,'4 byte độ dài N',1.05,2.27,2.9,.5,24,'FFFFFF',True)
box(s,4.2,2.05,8.1,1.0); text(s,'N byte JSON UTF-8',4.5,2.27,7.5,.5,24,TEAL,True)
bullets(s,['ReadExactlyAsync xử lý phân mảnh và gộp dữ liệu','Request ID ghép phản hồi; clientId chống tin trùng','Server gán sender, sequence ID và UTC timestamp','Giới hạn frame 8 MiB; tệp tối đa 5 MiB'],y=3.5,size=23)
s=slide('Tính năng đã hiện thực')
cards(s,'Tài khoản và trò chuyện',['Đăng ký, đăng nhập, đăng xuất','Chat 1–1 và nhóm realtime','Lịch sử, tìm kiếm, timestamp','Online/offline và danh sách online'],'Tương tác',['Bạn bè và lời mời kết bạn','Typing, seen, emoji Unicode','Popup và số tin chưa đọc','Gửi tệp, xem trước ảnh'])
s=slide('Giao diện chat nhóm')
chat_image=ROOT/'artifacts'/'ui'/'chat.png'
if chat_image.exists(): s.shapes.add_picture(str(chat_image),Inches(2.5),Inches(1.9),height=Inches(4.95))
s.notes_slide.notes_text_frame.text='Ảnh từ UI smoke chạy server và client thật với dữ liệu test riêng. Chỉ ra danh sách hội thoại, group owner, bong bóng, timestamp, seen, đính kèm và tìm kiếm.'
s=slide('Bảo mật và quyền truy cập')
cards(s,'Đã triển khai',['PBKDF2-HMAC-SHA256 + salt','TLS + pin SHA-256 certificate','Sender gắn với phiên xác thực','Membership kiểm tra ở server'],'Giới hạn rõ ràng',['Không phải mã hóa đầu cuối','Tin nhắn trên đĩa là bản rõ','Chưa lưu token đăng nhập','Chưa có Google / email / SĐT'])
s=slide('Đồng thời và tính nhất quán')
bullets(s,['Mỗi client có vòng nhận và hàng đợi gửi riêng','Semaphore bảo vệ giao dịch và ID tin nhắn','Ghi tệp tạm → flush → thay thế tệp chính → ACK','Lỗi ghi: giữ trạng thái cũ, không báo gửi thành công','UI nhận event qua BeginInvoke; hợp nhất tin theo ID'],size=23)
s=slide('Kiểm thử và bằng chứng')
cards(s,'Tự động trên máy hiện tại',['54 kiểm tra integration đạt','TCP/TLS thật, nhiều client gửi','Phân quyền, lỗi lưu, restart','UI smoke gửi qua nút WinForms'],'Nhóm cần bổ sung',['Hai máy vật lý trên LAN','Mất Wi-Fi và firewall','DPI 125% / 150%, popup/focus','Đo tải nếu muốn nêu hiệu năng'])
s=slide('Những khó khăn và cách giải quyết')
bullets(s,['JSON bị cắt/gộp → khung độ dài + đọc đủ byte','Nhiều client ghi cùng lúc → transaction + sequence','UI bị treo/cross-thread → async I/O + BeginInvoke','Mất mạng sau gửi → ACK sau commit, kiểm tra lịch sử','Thành viên cũ gọi API → kiểm tra quyền ở server'],size=23)
s=slide('Kịch bản demo live')
bullets(s,['1. Server + hai client; đăng ký và đăng nhập','2. Kết bạn → chat → typing → seen → popup','3. Tạo nhóm; thêm/xóa thành viên','4. Gửi ảnh/tệp; tìm lịch sử; thử giới hạn 5 MB','5. Restart server và kiểm tra dữ liệu còn nguyên'],size=23)
s.notes_slide.notes_text_frame.text='Chuẩn bị video MP4 dự phòng 5–8 phút, quay sau khi nhóm diễn tập. Chưa có video trong bản bàn giao. Xem docs/04-demo.md.'
s=slide('Đúc kết và hướng phát triển')
cards(s,'Bài học',['TCP không có ranh giới tin','Đồng thời cần bảo vệ trạng thái','UI và mạng phải tách luồng','Bảo mật cần cả xác thực và quyền'],'Bước tiếp theo',['SQLite + index thay JSON toàn bộ','Token phiên + DPAPI','Reconnect + hàng đợi bền vững','Upload chunk; media; E2EE'])
s=slide('Câu hỏi lý thuyết để thảo luận')
text(s,'Vì sao TCP đã tin cậy mà\nứng dụng vẫn cần ACK và “đã xem”?',.9,2.05,11.5,1.6,34,TEAL,True)
text(s,'TCP xác nhận truyền byte.\nACK ứng dụng xác nhận server đã lưu.\nRead receipt xác nhận thao tác đọc của client.',.95,4.2,11.1,1.7,25)
prs.save(DOCS/'Thuyet-trinh-UIT-Chat.pptx')
print(f'Created Word report and {len(prs.slides)} slides in {DOCS}')
