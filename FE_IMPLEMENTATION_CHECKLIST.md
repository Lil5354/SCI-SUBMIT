# 📋 CHECKLIST TRIỂN KHAI FRONTEND - HỆ THỐNG HỘI THẢO KHOA HỌC

## 🎯 MỤC TIÊU
Hoàn thiện toàn bộ Frontend (FE) bằng **ASP.NET Core MVC** theo đúng mô tả trong PDF, đảm bảo:
- ✅ Giao diện đẹp, thuận mắt, màu hài hòa
- ✅ Màu chủ đạo: **Xanh dương đậm tối** (#1e40af, #1e3a8a)
- ✅ Tối ưu, tiện lợi, responsive
- ✅ Tuân thủ nghiêm ngặt .NET Core

---

## 📖 PHÂN TÍCH PDF

### 4 Nhóm Người Dùng:
1. **Guest (Khách)** - Chưa đăng nhập
2. **Author (Tác giả)** - Người nộp bài
3. **Reviewer (Người phản biện)** - Người đánh giá
4. **Admin (Ban tổ chức)** - Quản lý hệ thống

### 4 Module Chính:
- **Module 1:** Trang công khai & Quản lý tài khoản
- **Module 2:** Luồng tác giả (Author Workflow) - 4 bước
- **Module 3:** Luồng ban tổ chức (Admin Workflow)
- **Module 4:** Luồng người phản biện (Reviewer Workflow)

---

## 🔴 PHẦN 1: SỬA LỖI GIAO DIỆN HIỆN TẠI

### ✅ 1.1. Sửa CSS Button Overflow
- [x] Thêm `white-space: nowrap !important`
- [x] Thêm `overflow: visible !important`
- [x] Thêm `min-width: fit-content !important`
- [x] Thêm `box-sizing: border-box !important`
- [ ] Kiểm tra trên tất cả button sizes
- [ ] Test responsive

### ✅ 1.2. Sửa Table Alignment
- [x] Đặt `table-layout: fixed !important`
- [x] Fix `text-align` với `!important`
- [x] Fix `vertical-align: middle !important`
- [x] Fix action buttons alignment
- [ ] Kiểm tra trên tất cả tables
- [ ] Test responsive

### ✅ 1.3. Cập Nhật Màu Chủ Đạo
- [x] Đổi primary color sang #1e40af (xanh dương đậm tối)
- [x] Đổi primary-dark sang #1e3a8a
- [ ] Cập nhật tất cả gradients
- [ ] Cập nhật navbar
- [ ] Cập nhật buttons
- [ ] Cập nhật cards
- [ ] Kiểm tra contrast

---

## 📄 PHẦN 2: MODULE 1 - TRANG CÔNG KHAI & QUẢN LÝ TÀI KHOẢN

### 🔵 2.1. Trang Chủ Công Khai (Guest)
- [ ] **Home/Index (Công khai)**
  - [ ] Hiển thị thông tin hội nghị: Tên, thời gian, địa điểm
  - [ ] Call for Papers section
  - [ ] Diễn giả chính (Keynote speakers) section
  - [ ] Đồng hồ đếm ngược (Countdown timer) đến các deadline
  - [ ] Nút "Nộp bài ngay" / "Đăng ký tham dự"
  - [ ] Responsive design

### 🔵 2.2. Authentication
- [ ] **Account/Login**
  - [ ] Form đăng nhập (Email/SĐT + Mật khẩu)
  - [ ] Link "Quên mật khẩu"
  - [ ] Link "Đăng ký"
  - [ ] Google OAuth button (nếu có)
  - [ ] Validation và error messages
  - [ ] Remember me checkbox

- [ ] **Account/Register**
  - [ ] Form đăng ký (Email, Mật khẩu, Xác nhận mật khẩu)
  - [ ] Yêu cầu nhập SĐT (theo PDF)
  - [ ] Google OAuth option
  - [ ] Terms & Conditions checkbox
  - [ ] Validation
  - [ ] Success message

- [ ] **Account/ForgotPassword**
  - [ ] Form nhập email
  - [ ] Gửi link reset password
  - [ ] Success message

- [ ] **Account/ResetPassword**
  - [ ] Form nhập mật khẩu mới
  - [ ] Xác nhận mật khẩu
  - [ ] Validation
  - [ ] Success message

---

## 📄 PHẦN 3: MODULE 2 - LUỒNG TÁC GIẢ (AUTHOR WORKFLOW)

### 🔵 3.1. Dashboard Tác Giả
- [ ] **Home/Index (Sau đăng nhập - Author)**
  - [x] Giao diện chính sau khi đăng nhập
  - [ ] **Thanh tiến độ 4 bước:**
    - [ ] Bước 1: Tóm tắt (Abstract)
    - [ ] Bước 2: Bài đầy đủ (Full Paper)
    - [ ] Bước 3: Phản biện (Review)
    - [ ] Bước 4: Thanh toán (Payment)
  - [ ] Visual progress bar với trạng thái từng bước
  - [x] Nút "Tạo bài nộp mới"
  - [x] Danh sách bài đã nộp với trạng thái

### 🔵 3.2. Bước 1: Nộp Tóm Tắt (Abstract Submission)
- [ ] **Submission/Create (Cải thiện)**
  - [ ] **Form nhập liệu:**
    - [ ] Tiêu đề (giới hạn 200 ký tự, có counter)
    - [ ] Tóm tắt (Abstract) - giới hạn 300 từ/ký tự, có bộ đếm real-time
    - [ ] Từ khóa (Keywords) - Tag input (gõ + Enter), gợi ý từ CSDL, giới hạn 5-6 từ
    - [ ] Chủ đề (Topic) - Dropdown do Admin quy định
    - [ ] Thông tin Tác giả - Thêm/xóa động nhiều tác giả
      - [ ] Họ tên
      - [ ] Email
      - [ ] Đơn vị
      - [ ] Checkbox "Tác giả chính" (corresponding author)
    - [ ] File hỗ trợ (PDF/DOCX, < 10MB) - Tùy chọn
  - [ ] **Lưu nháp:**
    - [ ] Tự động lưu mỗi 30s
    - [ ] Nút "Lưu nháp" manual
    - [ ] Thông báo "Đã lưu nháp"
  - [ ] **Nộp chính thức:**
    - [x] Validation tất cả trường bắt buộc
    - [ ] Disable edit sau khi nộp (chỉ Admin mới có thể trả lại quyền sửa)
  - [ ] **Phản hồi:**
    - [ ] Email xác nhận sau khi nộp
    - [ ] Trạng thái: "Chờ duyệt tóm tắt"

### 🔵 3.3. Bước 2: Nộp Bài Đầy Đủ (Full Paper Submission)
- [ ] **Submission/FullPaper/{id}**
  - [ ] Chỉ kích hoạt khi tóm tắt được Admin "Chấp nhận"
  - [ ] Upload file (PDF, DOCX)
  - [ ] File size validation (< 10MB)
  - [ ] Quản lý phiên bản:
    - [ ] Cho phép re-upload nhiều lần trước deadline
    - [ ] Lịch sử phiên bản (timeline)
    - [ ] Hiển thị phiên bản cuối cùng
  - [ ] Trạng thái: "Đã nộp bài đầy đủ"
  - [ ] Deadline countdown

### 🔵 3.4. Bước 3: Nhận Kết Quả Phản Biện & Sửa Chữa
- [ ] **Submission/Feedback/{id}**
  - [ ] Hiển thị kết quả: Accepted, Minor/Major revision, Rejected
  - [ ] Xem bình luận ẩn danh của Reviewer
  - [ ] Xem deadline nộp bản sửa
  - [ ] Upload file "Bản cuối đã nộp" (Final version)
  - [ ] Lịch sử chỉnh sửa

### 🔵 3.5. Bước 4: Thanh Toán
- [ ] **Payment/Index/{id}**
  - [ ] Chỉ kích hoạt sau khi bài được "Chấp nhận"
  - [ ] Hiển thị phí tham dự:
    - [ ] Phí cho tác giả
    - [ ] Phí cho sinh viên
    - [ ] Phí cho người tham dự
  - [ ] Tích hợp cổng thanh toán:
    - [ ] Momo
    - [ ] VNPAY
    - [ ] Thẻ ngân hàng
  - [ ] Trạng thái: "Đã thanh toán"
  - [ ] Gửi biên lai/hóa đơn qua email

### 🔵 3.6. Quản Lý Bài Nộp (Author)
- [x] **Submission/Index** - Danh sách bài đã nộp
- [ ] **Submission/Details/{id}** - Chi tiết bài đã nộp
  - [ ] Thông tin bài
  - [ ] Trạng thái và timeline
  - [ ] File đã upload
  - [ ] Phản hồi từ Reviewer (nếu có)
  - [ ] Actions: Edit (nếu chưa nộp), Withdraw, Download

- [ ] **Submission/Edit/{id}** - Chỉnh sửa bài (chỉ khi chưa nộp hoặc Admin cho phép)
- [ ] **Submission/Withdraw/{id}** - Rút bài với confirmation modal

---

## 📄 PHẦN 4: MODULE 3 - LUỒNG BAN TỔ CHỨC (ADMIN WORKFLOW)

### 🔵 4.1. Dashboard Admin
- [ ] **Admin/Dashboard**
  - [ ] Thống kê tổng quan:
    - [ ] Số bài đã nộp
    - [ ] Số bài đã duyệt
    - [ ] Số bài đang phản biện
    - [ ] Số lượng đăng ký
  - [ ] Các deadline sắp tới
  - [ ] Charts/Visualizations
  - [ ] Quick actions

### 🔵 4.2. Cấu Hình Kế Hoạch Hội Nghị
- [ ] **Admin/Conference/Edit**
  - [ ] Thiết lập Timeline:
    - [ ] Ngày mở nộp
    - [ ] Deadline nộp tóm tắt
    - [ ] Deadline nộp Full-text
    - [ ] Ngày công bố kết quả
    - [ ] Ngày hội nghị
  - [ ] Tự động đóng/mở form nộp
  - [ ] Tự động gửi thông báo nhắc nhở

### 🔵 4.3. Quản Lý Bài Nộp
- [ ] **Admin/Submissions**
  - [ ] Table danh sách tất cả bài nộp
  - [ ] Bộ lọc mạnh mẽ:
    - [ ] Lọc theo trạng thái
    - [ ] Lọc theo chủ đề
    - [ ] Lọc theo từ khóa
  - [ ] Tìm kiếm (theo tên bài, tác giả)
  - [ ] Pagination
  - [ ] Export to Excel

- [ ] **Admin/Submissions/Details/{id}**
  - [ ] Xem chi tiết bài
  - [ ] Actions: Approve, Reject, Assign Reviewer

### 🔵 4.4. Giai Đoạn 1: Duyệt Tóm Tắt
- [ ] **Admin/Submissions/Review/{id}**
  - [ ] Đọc Tóm tắt
  - [ ] Ra quyết định:
    - [ ] Chấp nhận -> Mở luồng nộp bài đầy đủ
    - [ ] Từ chối -> Nhập lý do từ chối (bắt buộc)
  - [ ] Gửi email tự động cho tác giả

### 🔵 4.5. Giai Đoạn 2: Quản Lý Phản Biện
- [ ] **Admin/Assignments**
  - [ ] Danh sách phân công phản biện
  - [ ] Gán Reviewer:
    - [ ] Chọn Reviewer từ danh sách
    - [ ] AI gợi ý Reviewer dựa trên Keywords
    - [ ] Admin duyệt/chuẩn hóa danh sách Keywords
  - [ ] Quy định thời gian (Deadline) cho Reviewer
  - [ ] Theo dõi tiến độ:
    - [ ] Ai đã nhận lời
    - [ ] Ai đã nộp đánh giá
    - [ ] Ai trễ hạn
  - [ ] Tự động gửi email nhắc nhở (trước 3 ngày)

### 🔵 4.6. Giai Đoạn 3: Tổng Hợp Kết Quả & Ra Quyết Định
- [ ] **Admin/Submissions/FinalDecision/{id}**
  - [ ] Xem tất cả phản biện
  - [ ] Tổng hợp điểm số
  - [ ] Ra quyết định cuối cùng:
    - [ ] Accepted
    - [ ] Minor/Major revision
    - [ ] Rejected
  - [ ] Gửi email thông báo cho tác giả

### 🔵 4.7. Quản Lý Khác
- [ ] **Admin/Users** - Quản lý người dùng
- [ ] **Admin/Fields** - Quản lý lĩnh vực nghiên cứu
- [ ] **Admin/Keywords** - Quản lý từ khóa (duyệt từ khóa mới)
- [ ] **Admin/Settings** - Cấu hình hệ thống
- [ ] **Admin/Reports** - Báo cáo và thống kê

---

## 📄 PHẦN 5: MODULE 4 - LUỒNG NGƯỜI PHẢN BIỆN (REVIEWER WORKFLOW)

### 🔵 5.1. Nhận Lời Mời
- [ ] Email mời phản biện (kèm tiêu đề, tóm tắt, deadline)
- [ ] **Review/Invitation/{id}**
  - [ ] Hiển thị thông tin bài
  - [ ] Nút "Chấp nhận" / "Từ chối"
  - [ ] Deadline hiển thị rõ ràng

### 🔵 5.2. Dashboard Reviewer
- [x] **Review/Index** - Danh sách bài được phân công
- [ ] Cải thiện:
  - [ ] Danh sách bài chờ phản biện (hiển thị deadline rõ ràng)
  - [ ] Danh sách bài đã hoàn thành
  - [ ] Đồng hồ đếm ngược hạn phản biện
  - [ ] Filter theo trạng thái

### 🔵 5.3. Thực Hiện Phản Biện
- [ ] **Review/Details/{id}**
  - [ ] Xem chi tiết bài cần phản biện
  - [ ] Giao diện Blind Review (ẩn danh tác giả)
  - [ ] Tải file bài báo
  - [ ] Form đánh giá do Admin cấu hình:
    - [ ] Điểm số (nếu có)
    - [ ] Bình luận
    - [ ] Khuyến nghị (Accept, Minor revision, Major revision, Reject)
  - [ ] Upload file phản biện (nếu cần)
  - [ ] Deadline countdown
  - [ ] Lưu nháp
  - [ ] Submit review

### 🔵 5.4. Quản Lý Hồ Sơ Reviewer
- [ ] **Account/Profile/Edit** (cho Reviewer)
  - [ ] Cập nhật Keywords chuyên môn
  - [ ] Chọn từ danh sách chuẩn do Admin ban hành
  - [ ] Thông tin cá nhân

### 🔵 5.5. Lịch Sử Phản Biện
- [ ] **Review/History**
  - [ ] Danh sách bài đã phản biện
  - [ ] Xem lại đánh giá đã nộp

---

## 🎨 PHẦN 6: CẢI THIỆN GIAO DIỆN & DESIGN SYSTEM

### 🔵 6.1. Cập Nhật Màu Sắc
- [x] Primary: #1e40af (xanh dương đậm tối)
- [x] Primary-dark: #1e3a8a
- [ ] Cập nhật tất cả gradients
- [ ] Cập nhật navbar
- [ ] Cập nhật buttons
- [ ] Cập nhật cards
- [ ] Cập nhật badges
- [ ] Đảm bảo contrast đủ

### 🔵 6.2. Components Cần Tạo
- [ ] **Progress Bar 4 Bước** (Tóm tắt -> Bài đầy đủ -> Phản biện -> Thanh toán)
- [ ] **Countdown Timer** Component
- [ ] **Tag Input** cho Keywords
- [ ] **Auto-save Indicator**
- [ ] **File Upload với Preview**
- [ ] **Timeline Component** cho lịch sử
- [ ] **Review Form** (có thể cấu hình)
- [ ] **Payment Gateway Integration UI**

### 🔵 6.3. Modals
- [ ] Modal xác nhận xóa
- [ ] Modal xác nhận rút bài
- [ ] Modal xem file preview
- [ ] Modal gửi thông báo
- [ ] Modal phân công phản biện
- [ ] Modal thanh toán

---

## 🔧 PHẦN 7: CONTROLLERS & ACTIONS CẦN TẠO

### ✅ Đã Có
- [x] HomeController
- [x] SubmissionController (Index, Create, Details)
- [x] ReviewController (Index)

### ❌ Cần Tạo/Bổ Sung

#### SubmissionController
- [ ] Edit(int id)
- [ ] Update(int id, model)
- [ ] Withdraw(int id)
- [ ] Download(int id)
- [ ] FullPaper(int id) - Nộp bài đầy đủ
- [ ] UploadFullPaper(int id, file)
- [ ] Feedback(int id) - Xem phản hồi
- [ ] UploadFinalVersion(int id, file)

#### ReviewController
- [ ] Details(int id)
- [ ] Invitation(int id) - Xem và chấp nhận/từ chối lời mời
- [ ] AcceptInvitation(int id)
- [ ] RejectInvitation(int id)
- [ ] Create(int submissionId) - Tạo phản biện
- [ ] Edit(int id)
- [ ] Update(int id, model)
- [ ] History() - Lịch sử phản biện
- [ ] Download(int id) - Tải file bài báo

#### AdminController
- [ ] Dashboard()
- [ ] Users() - Quản lý người dùng
- [ ] UserDetails(int id)
- [ ] CreateUser()
- [ ] EditUser(int id)
- [ ] DeleteUser(int id)
- [ ] Submissions() - Tất cả bài nộp
- [ ] SubmissionDetails(int id)
- [ ] ReviewSubmission(int id) - Duyệt tóm tắt
- [ ] ApproveAbstract(int id)
- [ ] RejectAbstract(int id, reason)
- [ ] Assignments() - Phân công phản biện
- [ ] AssignReviewer(int submissionId, int reviewerId, DateTime deadline)
- [ ] FinalDecision(int id) - Ra quyết định cuối
- [ ] Conference() - Quản lý thông tin hội thảo
- [ ] EditConference()
- [ ] Reports() - Báo cáo
- [ ] Fields() - Quản lý lĩnh vực
- [ ] Keywords() - Quản lý từ khóa
- [ ] Settings() - Cấu hình

#### AccountController
- [ ] Login()
- [ ] Register()
- [ ] Logout()
- [ ] ForgotPassword()
- [ ] ResetPassword()
- [ ] Profile()
- [ ] EditProfile()
- [ ] ChangePassword()

#### PaymentController
- [ ] Index(int submissionId)
- [ ] ProcessPayment(int submissionId, string gateway)
- [ ] Callback(string gateway)
- [ ] Invoice(int id)

#### NotificationController
- [ ] Index()
- [ ] MarkAsRead(int id)
- [ ] MarkAllAsRead()

---

## 📊 PHẦN 8: MODELS CẦN TẠO

- [ ] User
- [ ] Submission
- [ ] AbstractSubmission
- [ ] FullPaperSubmission
- [ ] Review
- [ ] ReviewAssignment
- [ ] ReviewComment
- [ ] Conference
- [ ] ConferenceTimeline
- [ ] Field (Lĩnh vực)
- [ ] Keyword
- [ ] Notification
- [ ] FileAttachment
- [ ] Payment
- [ ] PaymentTransaction

---

## 🎯 PRIORITY IMPLEMENTATION ORDER

### 🔴 Phase 1: Sửa Lỗi & Cải Thiện Hiện Tại (URGENT)
1. ✅ Sửa CSS button overflow với !important
2. ✅ Sửa table alignment với !important
3. ✅ Cập nhật màu chủ đạo
4. [ ] Test và verify tất cả fixes
5. [ ] Hard refresh browser để clear cache

### 🔴 Phase 2: Authentication (HIGH) - ✅ 100%
1. ✅ Account/Login
2. ✅ Account/Register (với SĐT)
3. ✅ Account/ForgotPassword
4. ✅ Account/ResetPassword
5. ✅ Account/Profile

### 🔴 Phase 3: Hoàn Thiện Author Workflow (HIGH) - ✅ 90%
1. ✅ Cải thiện Submission/Create (Abstract form với đầy đủ fields)
2. ✅ Submission/FullPaper/{id} - Nộp bài đầy đủ
3. ✅ Submission/Details/{id} - Chi tiết bài
4. ✅ Submission/Feedback/{id} - Xem phản hồi
5. ✅ Progress bar 4 bước trên Dashboard
6. ✅ Auto-save functionality (localStorage, cần backend)

### 🟡 Phase 4: Reviewer Workflow (MEDIUM)
1. Review/Details/{id} - Chi tiết bài cần phản biện
2. Review/Create/{id} - Form phản biện
3. Review/Invitation/{id} - Chấp nhận/từ chối
4. Review/History - Lịch sử

### 🟡 Phase 5: Admin Basic (MEDIUM) - ✅ 70%
1. ✅ Admin/Dashboard
2. ✅ Admin/Submissions
3. ✅ Admin/ReviewSubmission/{id} - Duyệt tóm tắt
4. ✅ Admin/Assignments - Phân công phản biện
5. ⏳ Admin/Conference/Edit - Cấu hình timeline
6. ⏳ Admin/Users - Quản lý người dùng
7. ⏳ Admin/Fields - Quản lý lĩnh vực
8. ⏳ Admin/Keywords - Quản lý từ khóa
9. ⏳ Admin/Reports - Báo cáo

### 🟢 Phase 6: Advanced Features (LOW)
1. Payment integration
2. Admin/Reports
3. Email notifications
4. Auto-save với backend
5. File management

---

## 📝 DESIGN GUIDELINES

### Màu Sắc
- **Primary:** #1e40af (Xanh dương đậm tối)
- **Primary Dark:** #1e3a8a
- **Primary Light:** #3b82f6
- **Secondary:** #10b981 (Xanh lá)
- **Accent:** #f59e0b (Cam)
- **Neutral:** Gray scale từ #f9fafb đến #111827

### Typography
- Font: System fonts (Segoe UI, Roboto, Helvetica Neue)
- Sizes: Responsive với clamp()
- Weights: 400 (normal), 500 (medium), 600 (semibold), 700 (bold)

### Spacing
- Consistent spacing scale (0.25rem đến 4rem)
- Padding và margin đều đặn

### Components
- Rounded corners (0.5rem đến 1.5rem)
- Shadows (4 levels)
- Hover effects với transform
- Smooth transitions

---

## ✅ TRẠNG THÁI

- **Đã hoàn thành:** ~65%
- **Đang thực hiện:** Hoàn thiện Admin views và Reviewer views còn thiếu
- **Còn lại:** ~35%

### Chi tiết:
- ✅ Authentication: 100%
- ✅ Author Workflow: 90%
- ✅ Payment: 100% (UI)
- ✅ Admin Basic: 70%
- ✅ Reviewer Workflow: 80%
- ⏳ Advanced Features: 20%

---

## 🚀 BẮT ĐẦU THỰC HIỆN

Bắt đầu từ Phase 1: Sửa lỗi và cải thiện hiện tại.

