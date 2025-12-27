-- ============================================
-- SCRIPT TẠO DATA CHO REVIEWER: thaodtt22@uef.edu.vn
-- ============================================
-- Chạy script này để tạo data test cho reviewer cụ thể

USE [SciSubmit]
GO

-- 1. Tìm hoặc tạo Reviewer
DECLARE @ReviewerId INT;
SELECT @ReviewerId = Id FROM Users WHERE Email = 'thaodtt22@uef.edu.vn';

IF @ReviewerId IS NULL
BEGIN
    -- Hash password: Reviewer@123 (sử dụng cùng method như DbInitializer)
    -- Password hash sẽ được tạo tự động khi user đăng nhập lần đầu
    -- Hoặc bạn có thể tạo user qua Admin panel
    
    PRINT 'Reviewer thaodtt22@uef.edu.vn chưa tồn tại.';
    PRINT 'Vui lòng tạo user qua Admin panel hoặc đảm bảo user đã được tạo.';
    PRINT 'Email: thaodtt22@uef.edu.vn';
    PRINT 'Role: Reviewer (2)';
    RETURN;
END

-- Đảm bảo reviewer có role đúng và active
UPDATE Users 
SET Role = 2, -- Reviewer
    IsActive = 1,
    EmailConfirmed = 1
WHERE Id = @ReviewerId;

PRINT 'Reviewer ID: ' + CAST(@ReviewerId AS VARCHAR);
PRINT 'Reviewer Email: thaodtt22@uef.edu.vn';

-- 2. Tìm Conference
DECLARE @ConferenceId INT;
SELECT TOP 1 @ConferenceId = Id FROM Conferences WHERE IsActive = 1 ORDER BY Id DESC;

IF @ConferenceId IS NULL
BEGIN
    PRINT 'Không tìm thấy Conference. Vui lòng chạy seed data cơ bản trước.';
    RETURN;
END

-- 3. Đảm bảo có Review Criteria
IF NOT EXISTS (SELECT 1 FROM ReviewCriterias WHERE ConferenceId = @ConferenceId AND IsActive = 1)
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

-- 4. Tìm Submission có Full Paper (status = UnderReview)
DECLARE @SubmissionId INT;
SELECT TOP 1 @SubmissionId = s.Id 
FROM Submissions s
INNER JOIN FullPaperVersions fp ON s.Id = fp.SubmissionId
WHERE s.Status = 5 -- UnderReview
AND fp.IsCurrentVersion = 1
ORDER BY s.Id DESC;

IF @SubmissionId IS NULL
BEGIN
    -- Tìm bất kỳ submission nào
    SELECT TOP 1 @SubmissionId = Id FROM Submissions ORDER BY Id DESC;
    
    IF @SubmissionId IS NOT NULL
    BEGIN
        -- Update status và tạo Full Paper
        UPDATE Submissions 
        SET Status = 5, -- UnderReview
            FullPaperSubmittedAt = GETUTCDATE()
        WHERE Id = @SubmissionId;
        
        -- Đảm bảo có FullPaperVersion
        IF NOT EXISTS (SELECT 1 FROM FullPaperVersions WHERE SubmissionId = @SubmissionId AND IsCurrentVersion = 1)
        BEGIN
            DECLARE @AuthorId INT;
            SELECT @AuthorId = AuthorId FROM Submissions WHERE Id = @SubmissionId;
            
            INSERT INTO FullPaperVersions (SubmissionId, FileUrl, FileName, FileSize, VersionNumber, IsCurrentVersion, UploadedAt, UploadedBy)
            VALUES (@SubmissionId, 'https://example.com/fullpaper.pdf', 'fullpaper.pdf', 1024000, 1, 1, GETUTCDATE(), @AuthorId);
        END
        
        PRINT 'Updated Submission ID: ' + CAST(@SubmissionId AS VARCHAR);
    END
END

IF @SubmissionId IS NULL
BEGIN
    PRINT 'Không tìm thấy Submission. Vui lòng chạy seed data cơ bản trước.';
    RETURN;
END

PRINT 'Submission ID: ' + CAST(@SubmissionId AS VARCHAR);

-- 5. Tìm Admin
DECLARE @AdminId INT;
SELECT TOP 1 @AdminId = Id FROM Users WHERE Role = 1; -- Admin

-- 6. Tạo hoặc Update Review Assignment (Accepted, chưa completed)
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
        1, -- Accepted
        DATEADD(day, -7, GETUTCDATE()),
        @AdminId,
        DATEADD(day, 14, GETUTCDATE()),
        DATEADD(day, -5, GETUTCDATE()),
        DATEADD(day, -7, GETUTCDATE())
    );
    SET @AssignmentId = SCOPE_IDENTITY();
    PRINT 'Created Review Assignment ID: ' + CAST(@AssignmentId AS VARCHAR);
END
ELSE
BEGIN
    -- Update để đảm bảo status = Accepted và chưa completed
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

-- 7. Tạo thêm 1-2 assignments khác để có nhiều data test
-- Assignment 2: Pending
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @SubmissionId AND ReviewerId = @ReviewerId AND Status = 0)
BEGIN
    INSERT INTO ReviewAssignments (
        SubmissionId, ReviewerId, Status, InvitedAt, InvitedBy, Deadline, CreatedAt
    )
    VALUES (
        @SubmissionId,
        @ReviewerId,
        0, -- Pending
        DATEADD(day, -2, GETUTCDATE()),
        @AdminId,
        DATEADD(day, 14, GETUTCDATE()),
        DATEADD(day, -2, GETUTCDATE())
    );
    PRINT 'Created Pending Assignment';
END

-- 8. Kiểm tra kết quả
PRINT '';
PRINT '=== KẾT QUẢ ===';
PRINT 'Reviewer ID: ' + CAST(@ReviewerId AS VARCHAR);
PRINT 'Reviewer Email: thaodtt22@uef.edu.vn';
PRINT 'Submission ID: ' + CAST(@SubmissionId AS VARCHAR);
PRINT 'Assignment ID (Accepted): ' + CAST(@AssignmentId AS VARCHAR);
PRINT '';
PRINT 'Đăng nhập với: thaodtt22@uef.edu.vn';
PRINT 'Truy cập: http://localhost:5234/Review';

-- 9. Hiển thị thống kê
SELECT 
    'Tổng số assignments' AS Metric,
    COUNT(*) AS Value
FROM ReviewAssignments
WHERE ReviewerId = @ReviewerId

UNION ALL

SELECT 
    'Pending assignments' AS Metric,
    COUNT(*) AS Value
FROM ReviewAssignments
WHERE ReviewerId = @ReviewerId AND Status = 0

UNION ALL

SELECT 
    'Accepted assignments (chưa completed)' AS Metric,
    COUNT(*) AS Value
FROM ReviewAssignments
WHERE ReviewerId = @ReviewerId AND Status = 1 AND CompletedAt IS NULL

UNION ALL

SELECT 
    'Completed assignments' AS Metric,
    COUNT(*) AS Value
FROM ReviewAssignments
WHERE ReviewerId = @ReviewerId AND Status = 3;

GO



