-- ============================================
-- SCRIPT TẠO DATA ĐỂ TEST FULL LUỒNG PHẢN BIỆN
-- ============================================
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio
-- Để test: Phản biện ẩn danh, Form đánh giá, Submit review

USE [SciSubmit] -- Thay đổi tên database nếu khác
GO

-- ============================================
-- 1. ĐẢM BẢO CÓ CONFERENCE
-- ============================================
DECLARE @ConferenceId INT;
SELECT @ConferenceId = Id FROM Conferences WHERE IsActive = 1 ORDER BY Id DESC;

IF @ConferenceId IS NULL
BEGIN
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
    PRINT 'Created Conference ID: ' + CAST(@ConferenceId AS VARCHAR);
END
ELSE
BEGIN
    PRINT 'Using existing Conference ID: ' + CAST(@ConferenceId AS VARCHAR);
END

-- ============================================
-- 2. ĐẢM BẢO CÓ REVIEW CRITERIA
-- ============================================
IF NOT EXISTS (SELECT 1 FROM ReviewCriterias WHERE ConferenceId = @ConferenceId AND Name = N'Tính mới')
BEGIN
    INSERT INTO ReviewCriterias (ConferenceId, Name, Description, MaxScore, OrderIndex, IsActive, CreatedAt)
    VALUES 
        (@ConferenceId, N'Tính mới', N'Tính mới và sáng tạo của nghiên cứu', 5, 1, 1, GETUTCDATE()),
        (@ConferenceId, N'Độ sâu nghiên cứu', N'Độ sâu và chi tiết của nghiên cứu', 5, 2, 1, GETUTCDATE()),
        (@ConferenceId, N'Phương pháp nghiên cứu', N'Tính hợp lý và khoa học của phương pháp', 5, 3, 1, GETUTCDATE()),
        (@ConferenceId, N'Trình bày', N'Cách trình bày và cấu trúc bài báo', 5, 4, 1, GETUTCDATE()),
        (@ConferenceId, N'Kết quả và đóng góp', N'Giá trị và đóng góp của kết quả nghiên cứu', 5, 5, 1, GETUTCDATE());
    PRINT 'Created Review Criteria';
END
ELSE
BEGIN
    PRINT 'Review Criteria already exists';
END

-- ============================================
-- 3. ĐẢM BẢO CÓ REVIEWER USER
-- ============================================
DECLARE @ReviewerId INT;
SELECT @ReviewerId = Id FROM Users WHERE Email = 'reviewer1@scisubmit.com' AND Role = 2; -- Reviewer role

IF @ReviewerId IS NULL
BEGIN
    -- Hash password: Reviewer@123
    DECLARE @ReviewerPasswordHash NVARCHAR(255) = 'YWRtaW5Ac2Npc3VibWl0U2FsdA=='; -- Placeholder, cần hash đúng
    -- Tạo reviewer user
    INSERT INTO Users (Email, PasswordHash, FullName, Affiliation, Role, EmailConfirmed, IsActive, CreatedAt)
    VALUES (
        'reviewer1@scisubmit.com',
        'YWRtaW5Ac2Npc3VibWl0U2FsdA==', -- Cần hash đúng password
        N'GS. Nguyễn Văn Reviewer',
        N'Đại học Khoa học',
        2, -- Reviewer
        1,
        1,
        GETUTCDATE()
    );
    SET @ReviewerId = SCOPE_IDENTITY();
    PRINT 'Created Reviewer ID: ' + CAST(@ReviewerId AS VARCHAR);
END
ELSE
BEGIN
    -- Đảm bảo reviewer active
    UPDATE Users SET IsActive = 1, Role = 2 WHERE Id = @ReviewerId;
    PRINT 'Using existing Reviewer ID: ' + CAST(@ReviewerId AS VARCHAR);
END

-- ============================================
-- 4. ĐẢM BẢO CÓ AUTHOR USER
-- ============================================
DECLARE @AuthorId INT;
SELECT @AuthorId = Id FROM Users WHERE Email = 'author1@scisubmit.com' AND Role = 3; -- Author role

IF @AuthorId IS NULL
BEGIN
    INSERT INTO Users (Email, PasswordHash, FullName, Affiliation, Role, EmailConfirmed, IsActive, CreatedAt)
    VALUES (
        'author1@scisubmit.com',
        'YWRtaW5Ac2Npc3VibWl0U2FsdA==', -- Cần hash đúng password
        N'TS. Trần Văn Author',
        N'Đại học Công nghệ',
        3, -- Author
        1,
        1,
        GETUTCDATE()
    );
    SET @AuthorId = SCOPE_IDENTITY();
    PRINT 'Created Author ID: ' + CAST(@AuthorId AS VARCHAR);
END
ELSE
BEGIN
    UPDATE Users SET IsActive = 1, Role = 3 WHERE Id = @AuthorId;
    PRINT 'Using existing Author ID: ' + CAST(@AuthorId AS VARCHAR);
END

-- ============================================
-- 5. ĐẢM BẢO CÓ TOPIC
-- ============================================
DECLARE @TopicId INT;
SELECT @TopicId = Id FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Công nghệ thông tin';

IF @TopicId IS NULL
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Công nghệ thông tin', N'Nghiên cứu về công nghệ thông tin', 1, 1, GETUTCDATE());
    SET @TopicId = SCOPE_IDENTITY();
END

-- ============================================
-- 6. TẠO SUBMISSION VỚI FULL PAPER (Status = UnderReview)
-- ============================================
DECLARE @SubmissionId INT;
SELECT @SubmissionId = Id FROM Submissions 
WHERE Title = N'Ứng dụng Machine Learning trong Phân tích Dữ liệu Lớn' 
AND ConferenceId = @ConferenceId;

IF @SubmissionId IS NULL
BEGIN
    INSERT INTO Submissions (
        ConferenceId, AuthorId, Title, Abstract, Status,
        AbstractFileUrl, AbstractSubmittedAt, AbstractReviewedAt,
        FullPaperSubmittedAt, CreatedAt
    )
    VALUES (
        @ConferenceId,
        @AuthorId,
        N'Ứng dụng Machine Learning trong Phân tích Dữ liệu Lớn',
        N'Bài báo này nghiên cứu về việc ứng dụng các kỹ thuật Machine Learning để phân tích dữ liệu lớn trong các lĩnh vực kinh doanh. Nghiên cứu đề xuất một framework mới để xử lý và phân tích dữ liệu real-time với độ chính xác cao.',
        5, -- UnderReview
        'https://example.com/abstract.pdf',
        '2025-01-15 10:00:00',
        '2025-01-20 15:00:00',
        '2025-02-01 14:00:00',
        '2025-01-10 08:00:00'
    );
    SET @SubmissionId = SCOPE_IDENTITY();
    
    -- Tạo FullPaperVersion
    INSERT INTO FullPaperVersions (SubmissionId, FileUrl, VersionNumber, IsCurrentVersion, SubmittedAt, CreatedAt)
    VALUES (
        @SubmissionId,
        'https://example.com/fullpaper.pdf',
        1,
        1,
        '2025-02-01 14:00:00',
        '2025-02-01 14:00:00'
    );
    
    -- Tạo SubmissionTopic
    INSERT INTO SubmissionTopics (SubmissionId, TopicId, CreatedAt)
    VALUES (@SubmissionId, @TopicId, GETUTCDATE());
    
    PRINT 'Created Submission ID: ' + CAST(@SubmissionId AS VARCHAR);
END
ELSE
BEGIN
    -- Update status để đảm bảo là UnderReview và có Full Paper
    UPDATE Submissions 
    SET Status = 5, -- UnderReview
        FullPaperSubmittedAt = '2025-02-01 14:00:00'
    WHERE Id = @SubmissionId;
    
    -- Đảm bảo có FullPaperVersion
    IF NOT EXISTS (SELECT 1 FROM FullPaperVersions WHERE SubmissionId = @SubmissionId AND IsCurrentVersion = 1)
    BEGIN
        INSERT INTO FullPaperVersions (SubmissionId, FileUrl, VersionNumber, IsCurrentVersion, SubmittedAt, CreatedAt)
        VALUES (@SubmissionId, 'https://example.com/fullpaper.pdf', 1, 1, '2025-02-01 14:00:00', GETUTCDATE());
    END
    
    PRINT 'Using existing Submission ID: ' + CAST(@SubmissionId AS VARCHAR);
END

-- ============================================
-- 7. TẠO REVIEW ASSIGNMENT VỚI STATUS ACCEPTED
-- ============================================
DECLARE @AdminId INT;
SELECT @AdminId = Id FROM Users WHERE Role = 1; -- Admin role

DECLARE @AssignmentId INT;
SELECT @AssignmentId = Id FROM ReviewAssignments 
WHERE SubmissionId = @SubmissionId AND ReviewerId = @ReviewerId;

IF @AssignmentId IS NULL
BEGIN
    INSERT INTO ReviewAssignments (
        SubmissionId, ReviewerId, Status, InvitedAt, InvitedBy, Deadline,
        AcceptedAt, CreatedAt
    )
    VALUES (
        @SubmissionId,
        @ReviewerId,
        1, -- Accepted (để có thể review ngay)
        DATEADD(day, -7, GETUTCDATE()), -- Invited 7 days ago
        @AdminId,
        DATEADD(day, 14, GETUTCDATE()), -- Deadline in 14 days
        DATEADD(day, -5, GETUTCDATE()), -- Accepted 5 days ago
        DATEADD(day, -7, GETUTCDATE())
    );
    SET @AssignmentId = SCOPE_IDENTITY();
    PRINT 'Created Review Assignment ID: ' + CAST(@AssignmentId AS VARCHAR);
END
ELSE
BEGIN
    -- Update để đảm bảo status là Accepted và chưa completed
    UPDATE ReviewAssignments
    SET Status = 1, -- Accepted
        AcceptedAt = DATEADD(day, -5, GETUTCDATE()),
        CompletedAt = NULL,
        Deadline = DATEADD(day, 14, GETUTCDATE())
    WHERE Id = @AssignmentId;
    
    -- Xóa review cũ nếu có (để có thể test lại)
    DELETE FROM ReviewScores WHERE ReviewId IN (SELECT Id FROM Reviews WHERE ReviewAssignmentId = @AssignmentId);
    DELETE FROM Reviews WHERE ReviewAssignmentId = @AssignmentId;
    
    PRINT 'Updated Review Assignment ID: ' + CAST(@AssignmentId AS VARCHAR);
END

-- ============================================
-- 8. TẠO THÊM 1 ASSIGNMENT KHÁC (PENDING) ĐỂ TEST
-- ============================================
DECLARE @Reviewer2Id INT;
SELECT @Reviewer2Id = Id FROM Users WHERE Email = 'reviewer2@scisubmit.com' AND Role = 2;

IF @Reviewer2Id IS NULL
BEGIN
    INSERT INTO Users (Email, PasswordHash, FullName, Affiliation, Role, EmailConfirmed, IsActive, CreatedAt)
    VALUES (
        'reviewer2@scisubmit.com',
        'YWRtaW5Ac2Npc3VibWl0U2FsdA==',
        N'PGS. Lê Thị Reviewer',
        N'Đại học Kinh tế',
        2,
        1,
        1,
        GETUTCDATE()
    );
    SET @Reviewer2Id = SCOPE_IDENTITY();
END

-- Assignment 2: Pending status
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @SubmissionId AND ReviewerId = @Reviewer2Id)
BEGIN
    INSERT INTO ReviewAssignments (
        SubmissionId, ReviewerId, Status, InvitedAt, InvitedBy, Deadline, CreatedAt
    )
    VALUES (
        @SubmissionId,
        @Reviewer2Id,
        0, -- Pending
        DATEADD(day, -2, GETUTCDATE()),
        @AdminId,
        DATEADD(day, 14, GETUTCDATE()),
        DATEADD(day, -2, GETUTCDATE())
    );
END

-- ============================================
-- 9. KIỂM TRA DATA ĐÃ TẠO
-- ============================================
PRINT '';
PRINT '=== KIỂM TRA DATA ===';
PRINT '';

PRINT '1. Conference:';
SELECT Id, Name, IsActive FROM Conferences WHERE Id = @ConferenceId;

PRINT '';
PRINT '2. Review Criteria:';
SELECT Id, Name, MaxScore, OrderIndex FROM ReviewCriterias WHERE ConferenceId = @ConferenceId ORDER BY OrderIndex;

PRINT '';
PRINT '3. Reviewer:';
SELECT Id, Email, FullName, Role, IsActive FROM Users WHERE Id = @ReviewerId;

PRINT '';
PRINT '4. Submission:';
SELECT Id, Title, Status, FullPaperSubmittedAt FROM Submissions WHERE Id = @SubmissionId;

PRINT '';
PRINT '5. Review Assignments:';
SELECT 
    ra.Id,
    ra.Status,
    ra.InvitedAt,
    ra.AcceptedAt,
    ra.Deadline,
    ra.CompletedAt,
    u.FullName AS ReviewerName
FROM ReviewAssignments ra
INNER JOIN Users u ON ra.ReviewerId = u.Id
WHERE ra.SubmissionId = @SubmissionId;

PRINT '';
PRINT '=== DATA ĐÃ SẴN SÀNG ĐỂ TEST ===';
PRINT 'Reviewer Login: reviewer1@scisubmit.com / Reviewer@123';
PRINT 'Assignment ID để test: ' + CAST(@AssignmentId AS VARCHAR);
PRINT 'Submission ID: ' + CAST(@SubmissionId AS VARCHAR);

GO

