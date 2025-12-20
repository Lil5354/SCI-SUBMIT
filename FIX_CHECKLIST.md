# 🔧 CHECKLIST SỬA LỖI GIAO DIỆN

## 📋 TỔNG QUAN
Checklist này giải quyết các vấn đề hiện tại của giao diện và hoàn thiện FE theo yêu cầu.

---

## ✅ PHẦN 1: SỬA LỖI GIAO DIỆN HIỆN TẠI

### 🔴 1.1. Sửa Background và Bố Cục Chữ, Màu Sắc Hero Section (Hình 1)
**Vấn đề:** Background và bố cục chữ, màu chưa đẹp

- [ ] **1.1.1. Điều chỉnh Background Overlay**
  - [ ] Giảm opacity của gradient overlay để background image hiển thị rõ hơn
  - [ ] Điều chỉnh `background-blend-mode` để tối ưu hiệu ứng
  - [ ] Cân bằng màu sắc gradient overlay (giảm độ đậm)

- [ ] **1.1.2. Cải thiện Typography và Text Layout**
  - [ ] Tăng text-shadow cho title để dễ đọc hơn
  - [ ] Điều chỉnh line-height cho subtitle
  - [ ] Cải thiện spacing giữa các elements
  - [ ] Tối ưu font-size responsive

- [ ] **1.1.3. Cải thiện Hero Info Cards**
  - [ ] Điều chỉnh màu sắc và opacity của cards
  - [ ] Cải thiện contrast giữa text và background
  - [ ] Tối ưu spacing và padding

- [ ] **1.1.4. Điều chỉnh Màu Sắc Tổng Thể**
  - [ ] Cân bằng màu gradient (không quá đậm)
  - [ ] Đảm bảo text có đủ contrast
  - [ ] Kiểm tra trên nhiều màn hình

---

### 🔴 1.2. Sửa Chữ Bị Tràn Ra Khỏi Button (Hình 2)
**Vấn đề:** Text trong button bị overflow

- [ ] **1.2.1. Fix Button Text Overflow**
  - [ ] Thêm `white-space: nowrap` hoặc `word-wrap: break-word` phù hợp
  - [ ] Tăng padding horizontal cho buttons
  - [ ] Thêm `min-width` cho buttons để đảm bảo đủ không gian
  - [ ] Kiểm tra responsive trên mobile

- [ ] **1.2.2. Cải thiện Button Sizing**
  - [ ] Điều chỉnh font-size cho buttons
  - [ ] Thêm `box-sizing: border-box`
  - [ ] Đảm bảo icon và text có spacing hợp lý

- [ ] **1.2.3. Responsive Button Behavior**
  - [ ] Kiểm tra trên các breakpoints khác nhau
  - [ ] Điều chỉnh button size trên mobile
  - [ ] Đảm bảo text không bị cắt

---

### 🔴 1.3. Sửa Bố Cục Bảng - Header và Nội Dung Bị Lệch (Hình 3, 4)
**Vấn đề:** Table headers và content không align đúng

- [ ] **1.3.1. Fix Table Column Alignment**
  - [ ] Đảm bảo `text-align` của header và content khớp nhau
  - [ ] Sử dụng `vertical-align: middle` cho tất cả cells
  - [ ] Fix column widths để không bị lệch

- [ ] **1.3.2. Fix "Tiêu đề" Column Layout**
  - [ ] Tách rõ title và author info
  - [ ] Đảm bảo author info không tràn sang cột khác
  - [ ] Cải thiện spacing và line-height

- [ ] **1.3.3. Fix "Lĩnh vực" Column**
  - [ ] Đảm bảo content nằm đúng trong column
  - [ ] Fix alignment với header
  - [ ] Xử lý text overflow nếu cần

- [ ] **1.3.4. Fix Action Buttons Alignment**
  - [ ] Center align action buttons trong cell
  - [ ] Đảm bảo buttons không bị lệch
  - [ ] Consistent spacing giữa các buttons

- [ ] **1.3.5. Fix Table Structure**
  - [ ] Kiểm tra `table-layout` (auto vs fixed)
  - [ ] Đảm bảo `thead` và `tbody` align đúng
  - [ ] Fix responsive table behavior

---

## 📄 PHẦN 2: KIỂM TRA VÀ HOÀN THIỆN FE THEO PDF

### 🔵 2.1. Đọc và Phân Tích File PDF
- [ ] **2.1.1. Tìm File PDF**
  - [ ] Tìm file "Mo ta so bo he thong HOI THAO.pdf" trong workspace
  - [ ] Nếu không có, yêu cầu người dùng cung cấp

- [ ] **2.1.2. Phân Tích Yêu Cầu**
  - [ ] Liệt kê tất cả chức năng (features)
  - [ ] Liệt kê tất cả user roles
  - [ ] Xác định workflow và user flows
  - [ ] Xác định các trang/views cần thiết

---

### 🔵 2.2. Đánh Giá FE Hiện Tại
- [ ] **2.2.1. Kiểm Tra Controllers**
  - [ ] HomeController - ✅ Có
  - [ ] SubmissionController - ✅ Có (Index, Create)
  - [ ] ReviewController - ✅ Có (Index)
  - [ ] Các controllers khác cần thiết?

- [ ] **2.2.2. Kiểm Tra Views**
  - [ ] Home/Index - ✅ Có
  - [ ] Submission/Index - ✅ Có
  - [ ] Submission/Create - ✅ Có
  - [ ] Submission/Details - ❓ Cần kiểm tra
  - [ ] Review/Index - ✅ Có
  - [ ] Review/Details - ❓ Cần kiểm tra
  - [ ] Admin views - ❓ Cần kiểm tra
  - [ ] Authentication views - ❓ Cần kiểm tra

- [ ] **2.2.3. Kiểm Tra User Roles**
  - [ ] Author (Tác giả) - ✅ Có FE
  - [ ] Reviewer (Phản biện) - ✅ Có FE
  - [ ] Admin (Quản trị) - ❓ Cần kiểm tra
  - [ ] Editor (Biên tập) - ❓ Cần kiểm tra
  - [ ] Các role khác?

- [ ] **2.2.4. Kiểm Tra Chức Năng**
  - [ ] Nộp bài - ✅ Có
  - [ ] Xem danh sách bài - ✅ Có
  - [ ] Phản biện - ✅ Có (cơ bản)
  - [ ] Quản lý người dùng - ❓
  - [ ] Quản lý hội thảo - ❓
  - [ ] Báo cáo/Thống kê - ❓
  - [ ] Notification system - ❓
  - [ ] Email notifications - ❓
  - [ ] File management - ❓
  - [ ] Các chức năng khác?

---

### 🔵 2.3. Tạo Checklist Hoàn Thiện FE
- [ ] **2.3.1. Tạo Checklist Chi Tiết**
  - [ ] Liệt kê tất cả views cần tạo
  - [ ] Liệt kê tất cả controllers cần tạo
  - [ ] Liệt kê tất cả models cần tạo
  - [ ] Xác định priority (High/Medium/Low)

- [ ] **2.3.2. Planning Implementation**
  - [ ] Phân chia thành phases
  - [ ] Ước tính effort cho mỗi task
  - [ ] Xác định dependencies

---

## 🚀 PHẦN 3: THỰC HIỆN

### ✅ 3.1. Ưu Tiên Cao (Làm Ngay)
1. Sửa background và bố cục hero section
2. Sửa button text overflow
3. Sửa table alignment issues

### ✅ 3.2. Ưu Tiên Trung Bình
4. Kiểm tra và hoàn thiện FE theo PDF
5. Tạo các views/controllers còn thiếu

### ✅ 3.3. Ưu Tiên Thấp
6. Tối ưu performance
7. Thêm animations nâng cao
8. Cải thiện accessibility

---

## 📝 NOTES
- Tất cả thay đổi phải responsive
- Đảm bảo không break existing functionality
- Test trên nhiều browsers
- Đảm bảo accessibility (WCAG)

---

## ✅ TRẠNG THÁI
- [ ] Phần 1: Sửa lỗi giao diện - **ĐANG THỰC HIỆN**
- [ ] Phần 2: Kiểm tra và hoàn thiện FE - **CHỜ FILE PDF**
- [ ] Phần 3: Thực hiện - **CHỜ**

