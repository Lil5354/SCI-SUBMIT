# 📋 CHECKLIST HOÀN THIỆN FRONTEND - HỆ THỐNG HỘI THẢO KHOA HỌC

## 🎯 MỤC TIÊU
Hoàn thiện toàn bộ Frontend (FE) cho hệ thống quản lý hội thảo khoa học, đảm bảo đầy đủ chức năng cho tất cả user roles.

---

## 👥 USER ROLES CẦN HỖ TRỢ (Theo PDF)

**PDF xác định 4 nhóm người dùng:**
1. **Guest (Khách)** - Chưa đăng nhập
2. **Author (Tác giả)** - Người nộp bài
3. **Reviewer (Người phản biện)** - Người đánh giá bài
4. **Admin (Ban tổ chức)** - Quản lý hệ thống

### ✅ 1. Guest (Khách)
- [ ] Trang chủ công khai với thông tin hội nghị
- [ ] Hiển thị thông tin: Tên hội nghị, thời gian, địa điểm, Call for Papers
- [ ] Diễn giả chính (Keynote speakers)
- [ ] Đồng hồ đếm ngược (Countdown timer) đến các deadline
- [ ] Nút "Nộp bài ngay" / "Đăng ký tham dự"
- [ ] Đăng ký tài khoản (Email + Mật khẩu hoặc Google OAuth)
- [ ] Xác minh email (OTP hoặc link)
- [ ] Đăng nhập (Email/SĐT)
- [ ] Quên mật khẩu

### ✅ 2. Author (Tác giả)
**Dashboard Tác giả:**
- [x] Giao diện chính sau khi đăng nhập
- [ ] Thanh tiến độ 4 bước: Tóm tắt -> Bài đầy đủ -> Phản biện -> Thanh toán
- [x] Nút "Tạo bài nộp mới"
- [x] Danh sách bài đã nộp với trạng thái rõ ràng

**Bước 1: Nộp Tóm tắt (Abstract Submission)**
- [ ] Form nhập liệu:
  - [ ] Tiêu đề (giới hạn 200 ký tự)
  - [ ] Tóm tắt (Abstract) - giới hạn 300 từ/ký tự, có bộ đếm
  - [ ] Từ khóa (Keywords) - Tag input, gợi ý từ CSDL, giới hạn 5-6 từ
  - [ ] Chủ đề (Topic) - Dropdown do Admin quy định
  - [ ] Thông tin Tác giả - Thêm/xóa động nhiều tác giả (Họ tên, Email, Đơn vị)
  - [ ] Tác giả chính (corresponding author)
  - [ ] File hỗ trợ (PDF/DOCX, < 10MB) - Tùy chọn
- [ ] Lưu nháp (tự động mỗi 30s hoặc nút "Lưu nháp")
- [x] Nộp chính thức với validation
- [ ] Email xác nhận sau khi nộp
- [ ] Trạng thái: "Chờ duyệt tóm tắt"

**Bước 2: Nộp Bài đầy đủ (Full Paper Submission)**
- [ ] Chỉ kích hoạt khi tóm tắt được Admin "Chấp nhận"
- [ ] Upload file (PDF, DOCX)
- [ ] Quản lý phiên bản (re-upload nhiều lần trước deadline)
- [ ] Lịch sử phiên bản
- [ ] Trạng thái: "Đã nộp bài đầy đủ"

**Bước 3: Nhận Kết quả Phản biện & Sửa chữa**
- [ ] Nhận email khi có kết quả (Accepted, Minor/Major revision, Rejected)
- [ ] Xem bình luận ẩn danh của Reviewer
- [ ] Xem deadline nộp bản sửa
- [ ] Upload file "Bản cuối đã nộp" (Final version)

**Bước 4: Thanh toán**
- [ ] Kích hoạt sau khi bài được "Chấp nhận"
- [ ] Hiển thị phí tham dự (cho tác giả, sinh viên...)
- [ ] Tích hợp cổng thanh toán (Momo, VNPAY, Thẻ ngân hàng)
- [ ] Trạng thái: "Đã thanh toán"
- [ ] Gửi biên lai/hóa đơn qua email

### ✅ 3. Reviewer (Người phản biện)
- [x] Xem danh sách bài được phân công
- [ ] Nhận email mời phản biện (kèm tiêu đề, tóm tắt, deadline)
- [ ] Nút "Chấp nhận" / "Từ chối" lời mời
- [ ] Dashboard Reviewer:
  - [ ] Danh sách bài chờ phản biện (hiển thị deadline)
  - [ ] Danh sách bài đã hoàn thành
- [ ] Xem chi tiết bài cần phản biện
- [ ] Giao diện Blind Review
- [ ] Form đánh giá do Admin cấu hình
- [ ] Tải file bài báo
- [ ] Upload file phản biện
- [ ] Cập nhật Keywords chuyên môn trong hồ sơ
- [ ] Đồng hồ đếm ngược hạn phản biện

### ❓ 4. Admin (Ban tổ chức)
- [ ] Dashboard tổng quan
- [ ] Quản lý người dùng
- [ ] Phân công phản biện
- [ ] Quản lý bài nộp (xem tất cả)
- [ ] Quản lý trạng thái bài
- [ ] Quản lý hội thảo (thông tin, deadline)
- [ ] Báo cáo và thống kê
- [ ] Quản lý lĩnh vực nghiên cứu
- [ ] Cấu hình hệ thống

### ❓ 4. Editor (Biên tập viên)
- [ ] Xem tất cả bài nộp
- [ ] Phê duyệt/từ chối bài
- [ ] Phân công phản biện
- [ ] Quản lý quy trình duyệt
- [ ] Gửi thông báo cho tác giả
- [ ] Quản lý deadline

### ❓ 5. Track Chair (Chủ tịch track)
- [ ] Xem bài trong track được phân công
- [ ] Quản lý phản biện trong track
- [ ] Phê duyệt bài trong track
- [ ] Báo cáo track

---

## 📄 PAGES/VIEWS CẦN TẠO

### ✅ Đã Có
- [x] Home/Index - Trang chủ
- [x] Submission/Create - Nộp bài mới
- [x] Submission/Index - Danh sách bài đã nộp
- [x] Review/Index - Danh sách bài cần phản biện

### ❌ Còn Thiếu - Author
- [ ] Submission/Details/{id} - Chi tiết bài đã nộp
- [ ] Submission/Edit/{id} - Chỉnh sửa bài
- [ ] Submission/Withdraw/{id} - Rút bài
- [ ] Submission/Feedback/{id} - Xem phản hồi

### ❌ Còn Thiếu - Reviewer
- [ ] Review/Details/{id} - Chi tiết bài cần phản biện
- [ ] Review/Create/{id} - Tạo phản biện
- [ ] Review/Edit/{id} - Chỉnh sửa phản biện
- [ ] Review/History - Lịch sử phản biện

### ❌ Còn Thiếu - Admin
- [ ] Admin/Dashboard - Dashboard quản trị
- [ ] Admin/Users - Quản lý người dùng
- [ ] Admin/Users/Create - Tạo người dùng
- [ ] Admin/Users/Edit/{id} - Chỉnh sửa người dùng
- [ ] Admin/Submissions - Quản lý tất cả bài nộp
- [ ] Admin/Submissions/Details/{id} - Chi tiết bài (admin view)
- [ ] Admin/Assignments - Phân công phản biện
- [ ] Admin/Assignments/Create - Tạo phân công
- [ ] Admin/Conference - Quản lý thông tin hội thảo
- [ ] Admin/Conference/Edit - Chỉnh sửa thông tin hội thảo
- [ ] Admin/Reports - Báo cáo và thống kê
- [ ] Admin/Fields - Quản lý lĩnh vực nghiên cứu
- [ ] Admin/Settings - Cấu hình hệ thống

### ❌ Còn Thiếu - Editor
- [ ] Editor/Dashboard - Dashboard biên tập
- [ ] Editor/Submissions - Tất cả bài nộp
- [ ] Editor/Submissions/Review/{id} - Xem và phê duyệt bài
- [ ] Editor/Assignments - Quản lý phân công
- [ ] Editor/Notifications - Gửi thông báo

### ❌ Còn Thiếu - Track Chair
- [ ] TrackChair/Dashboard - Dashboard track chair
- [ ] TrackChair/Submissions - Bài trong track
- [ ] TrackChair/Reviewers - Quản lý phản biện
- [ ] TrackChair/Reports - Báo cáo track

### ❌ Còn Thiếu - Authentication & Common
- [ ] Account/Login - Đăng nhập
- [ ] Account/Register - Đăng ký
- [ ] Account/ForgotPassword - Quên mật khẩu
- [ ] Account/ResetPassword - Đặt lại mật khẩu
- [ ] Account/Profile - Hồ sơ cá nhân
- [ ] Account/Profile/Edit - Chỉnh sửa hồ sơ
- [ ] Account/ChangePassword - Đổi mật khẩu
- [ ] Notifications/Index - Thông báo
- [ ] Help/Documentation - Tài liệu hướng dẫn
- [ ] Help/FAQ - Câu hỏi thường gặp

---

## 🎨 COMPONENTS CẦN TẠO/ĐỔI MỚI

### Forms
- [ ] Form phản biện (Review form)
- [ ] Form quản lý người dùng
- [ ] Form phân công phản biện
- [ ] Form cấu hình hội thảo
- [ ] Form upload file (cải thiện)
- [ ] Form tìm kiếm nâng cao

### Modals/Dialogs
- [ ] Modal xác nhận xóa
- [ ] Modal xác nhận rút bài
- [ ] Modal xem file preview
- [ ] Modal gửi thông báo
- [ ] Modal phân công phản biện

### Tables/Data Display
- [ ] Table quản lý người dùng
- [ ] Table tất cả bài nộp (admin)
- [ ] Table phân công phản biện
- [ ] Table thống kê
- [ ] Calendar view cho deadline

### Charts/Visualizations
- [ ] Dashboard charts (submissions, reviews, users)
- [ ] Statistics cards
- [ ] Progress indicators
- [ ] Timeline view

---

## 🔧 CONTROLLERS CẦN TẠO

### ✅ Đã Có
- [x] HomeController
- [x] SubmissionController (Index, Create)
- [x] ReviewController (Index)

### ❌ Cần Tạo/Bổ Sung
- [ ] SubmissionController
  - [ ] Details(int id)
  - [ ] Edit(int id)
  - [ ] Update(int id, model)
  - [ ] Withdraw(int id)
  - [ ] Download(int id)

- [ ] ReviewController
  - [ ] Details(int id)
  - [ ] Create(int submissionId)
  - [ ] Edit(int id)
  - [ ] Update(int id, model)
  - [ ] History()

- [ ] AdminController
  - [ ] Dashboard()
  - [ ] Users()
  - [ ] UserDetails(int id)
  - [ ] CreateUser()
  - [ ] EditUser(int id)
  - [ ] DeleteUser(int id)
  - [ ] Submissions()
  - [ ] SubmissionDetails(int id)
  - [ ] AssignReviewer()
  - [ ] Conference()
  - [ ] EditConference()
  - [ ] Reports()
  - [ ] Fields()
  - [ ] Settings()

- [ ] EditorController
  - [ ] Dashboard()
  - [ ] Submissions()
  - [ ] ReviewSubmission(int id)
  - [ ] ApproveSubmission(int id)
  - [ ] RejectSubmission(int id)
  - [ ] Assignments()
  - [ ] Notifications()

- [ ] TrackChairController
  - [ ] Dashboard()
  - [ ] Submissions()
  - [ ] Reviewers()
  - [ ] Reports()

- [ ] AccountController
  - [ ] Login()
  - [ ] Register()
  - [ ] Logout()
  - [ ] ForgotPassword()
  - [ ] ResetPassword()
  - [ ] Profile()
  - [ ] EditProfile()
  - [ ] ChangePassword()

- [ ] NotificationController
  - [ ] Index()
  - [ ] MarkAsRead(int id)
  - [ ] MarkAllAsRead()

- [ ] HelpController
  - [ ] Documentation()
  - [ ] FAQ()

---

## 📊 MODELS CẦN TẠO

- [ ] User (nếu chưa có)
- [ ] Submission (nếu chưa có)
- [ ] Review
- [ ] Assignment (Phân công phản biện)
- [ ] Conference
- [ ] Field (Lĩnh vực)
- [ ] Notification
- [ ] FileAttachment
- [ ] ReviewComment

---

## 🎯 PRIORITY IMPLEMENTATION ORDER

### 🔴 Phase 1: Hoàn Thiện Author & Reviewer (HIGH)
1. Submission/Details - Chi tiết bài
2. Submission/Edit - Chỉnh sửa bài
3. Review/Details - Chi tiết bài cần phản biện
4. Review/Create - Tạo phản biện
5. Account/Profile - Hồ sơ cá nhân

### 🟡 Phase 2: Authentication & Common (HIGH)
1. Account/Login
2. Account/Register
3. Account/ForgotPassword
4. Notifications/Index

### 🟡 Phase 3: Admin Basic (MEDIUM)
1. Admin/Dashboard
2. Admin/Users
3. Admin/Submissions
4. Admin/Assignments

### 🟢 Phase 4: Editor & Advanced (MEDIUM)
1. Editor/Dashboard
2. Editor/Submissions
3. Editor/Assignments

### 🟢 Phase 5: Track Chair & Reports (LOW)
1. TrackChair/Dashboard
2. Admin/Reports
3. Help/Documentation

---

## 📝 NOTES

### Design Guidelines
- Tuân thủ design system hiện tại
- Responsive trên mọi thiết bị
- Accessibility (WCAG 2.1)
- Performance optimization

### Technical Requirements
- ASP.NET Core MVC
- Bootstrap 5
- Font Awesome icons
- jQuery (nếu cần)

### Testing
- Test trên Chrome, Firefox, Edge, Safari
- Test responsive trên mobile, tablet, desktop
- Test với các role khác nhau
- Test form validation

---

## ✅ TRẠNG THÁI TỔNG QUAN

- **Đã hoàn thành:** ~30%
- **Đang thực hiện:** Sửa lỗi giao diện
- **Còn lại:** ~70%

---

## 📌 LƯU Ý QUAN TRỌNG

**CẦN FILE PDF ĐỂ:**
- Xác định chính xác các chức năng
- Xác định các user roles
- Xác định workflow chi tiết
- Xác định các trường dữ liệu cần thiết

**Vui lòng cung cấp file PDF "Mo ta so bo he thong HOI THAO.pdf" để hoàn thiện checklist này.**

