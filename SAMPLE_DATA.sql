-- ============================================
-- SCRIPT TẠO DATA MẪU ĐỂ TEST
-- ============================================
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio
-- Để test 2 chức năng: Timeline Hội thảo và CRUD Lĩnh vực

USE [SciSubmit] -- Thay đổi tên database nếu khác
GO

-- ============================================
-- 1. TẠO/UPDATE CONFERENCE PLAN VỚI TIMELINE
-- ============================================
-- Lưu ý: DateTime trong SQL Server được lưu dưới dạng UTC
-- Script này tạo timeline cho năm 2025

DECLARE @ConferenceId INT;
SELECT @ConferenceId = Id FROM Conferences WHERE IsActive = 1;

IF @ConferenceId IS NULL
BEGIN
    -- Tạo conference nếu chưa có
    INSERT INTO Conferences (Name, Description, Location, StartDate, EndDate, IsActive, CreatedAt)
    VALUES (
        N'Hội thảo Khoa học Quốc tế về Kinh tế và Quản trị Kinh doanh 2025',
        N'Hội thảo khoa học quốc tế về các lĩnh vực kinh tế, tài chính, quản trị kinh doanh và marketing',
        N'TP. Hồ Chí Minh, Việt Nam',
        '2025-04-18 08:00:00',
        '2025-04-20 17:00:00',
        1,
        GETUTCDATE()
    );
    SET @ConferenceId = SCOPE_IDENTITY();
END

-- Xóa plan cũ nếu có (để test lại)
DELETE FROM ConferencePlans WHERE ConferenceId = @ConferenceId;

-- Tạo ConferencePlan mới với timeline đầy đủ
-- Lưu ý: Tất cả datetime phải là UTC
-- Ví dụ: 2025-01-20 12:00:00 (local) = 2025-01-20 05:00:00 (UTC) nếu timezone là UTC+7
-- Để đơn giản, script này dùng UTC trực tiếp

INSERT INTO ConferencePlans (
    ConferenceId,
    AbstractSubmissionOpenDate,
    AbstractSubmissionDeadline,
    FullPaperSubmissionOpenDate,
    FullPaperSubmissionDeadline,
    ReviewDeadline,
    ResultAnnouncementDate,
    ConferenceDate,
    CreatedAt
)
VALUES (
    @ConferenceId,
    '2025-01-20 00:00:00',  -- Ngày mở nộp tóm tắt: 20/01/2025 00:00 (UTC)
    '2025-01-22 00:00:00',  -- Deadline nộp tóm tắt: 22/01/2025 00:00 (UTC)
    '2025-01-28 00:00:00',  -- Ngày mở nộp Full-text: 28/01/2025 00:00 (UTC)
    '2025-02-02 00:00:00',  -- Deadline nộp Full-text: 02/02/2025 00:00 (UTC)
    '2025-02-22 00:00:00',  -- Deadline phản biện mặc định: 22/02/2025 00:00 (UTC)
    '2025-02-10 12:00:00',  -- Ngày công bố kết quả: 10/02/2025 12:00 (UTC)
    '2025-02-20 12:00:00',  -- Ngày hội nghị: 20/02/2025 12:00 (UTC)
    GETUTCDATE()
);

PRINT 'Đã tạo ConferencePlan với timeline đầy đủ';
GO

-- ============================================
-- 2. TẠO DATA MẪU CHO TOPICS (LĨNH VỰC)
-- ============================================
-- Xóa topics cũ để test lại (nếu cần)
-- DELETE FROM Topics WHERE ConferenceId = (SELECT Id FROM Conferences WHERE IsActive = 1);

DECLARE @ConferenceId INT;
SELECT @ConferenceId = Id FROM Conferences WHERE IsActive = 1;

-- Tạo các Topics mẫu
IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Kinh tế')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Kinh tế', N'Các nghiên cứu về kinh tế học', 1, 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Tài chính & Kế toán')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Tài chính & Kế toán', N'Nghiên cứu về tài chính và kế toán', 1, 2, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Thương mại')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Thương mại', N'Nghiên cứu về thương mại', 1, 3, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Quản trị kinh doanh')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Quản trị kinh doanh', N'Nghiên cứu về quản trị', 1, 4, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Marketing')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Marketing', N'Nghiên cứu về marketing', 1, 5, GETUTCDATE());
END

-- Thêm một số topics mẫu khác để test CRUD
IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Công nghệ thông tin')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Công nghệ thông tin', N'Nghiên cứu về công nghệ thông tin và ứng dụng', 1, 6, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Quản lý dự án')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Quản lý dự án', N'Nghiên cứu về phương pháp và công cụ quản lý dự án', 1, 7, GETUTCDATE());
END

PRINT 'Đã tạo Topics mẫu';
GO

-- ============================================
-- 3. KIỂM TRA DATA ĐÃ TẠO
-- ============================================
PRINT '=== KIỂM TRA CONFERENCE PLAN ===';
SELECT 
    cp.Id,
    c.Name AS ConferenceName,
    cp.AbstractSubmissionOpenDate,
    cp.AbstractSubmissionDeadline,
    cp.FullPaperSubmissionOpenDate,
    cp.FullPaperSubmissionDeadline,
    cp.ReviewDeadline,
    cp.ResultAnnouncementDate,
    cp.ConferenceDate
FROM ConferencePlans cp
INNER JOIN Conferences c ON cp.ConferenceId = c.Id
WHERE c.IsActive = 1;

PRINT '=== KIỂM TRA TOPICS ===';
SELECT 
    t.Id,
    t.Name,
    t.Description,
    t.IsActive,
    t.OrderIndex,
    c.Name AS ConferenceName
FROM Topics t
INNER JOIN Conferences c ON t.ConferenceId = c.Id
WHERE c.IsActive = 1
ORDER BY t.OrderIndex;

PRINT 'Hoàn thành! Bây giờ bạn có thể test:';
PRINT '1. Timeline Hội thảo: Vào /Admin/Conference và kiểm tra timeline';
PRINT '2. CRUD Lĩnh vực: Vào /Admin/Fields và thử Create, Update, Delete';






