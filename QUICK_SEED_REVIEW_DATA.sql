-- ============================================
-- QUICK SCRIPT: TẠO DATA TEST CHO REVIEW FLOW
-- ============================================
-- Chạy script này trong SQL Server Management Studio
-- Hoặc dùng: sqlcmd -S localhost -d SciSubmit -i QUICK_SEED_REVIEW_DATA.sql

USE [SciSubmit]
GO

-- 1. Đảm bảo có Reviewer
DECLARE @ReviewerId INT;
SELECT @ReviewerId = Id FROM Users WHERE Email = 'reviewer1@scisubmit.com' AND Role = 2;

IF @ReviewerId IS NULL
BEGIN
    PRINT 'Reviewer chưa tồn tại. Vui lòng chạy DbInitializer.SeedAsync trước.';
    RETURN;
END

PRINT 'Reviewer ID: ' + CAST(@ReviewerId AS VARCHAR);

-- 2. Tìm một Submission có Full Paper
DECLARE @SubmissionId INT;
SELECT TOP 1 @SubmissionId = s.Id 
FROM Submissions s
INNER JOIN FullPaperVersions fp ON s.Id = fp.SubmissionId
WHERE s.Status = 5 -- UnderReview
AND fp.IsCurrentVersion = 1
ORDER BY s.Id DESC;

IF @SubmissionId IS NULL
BEGIN
    -- Tìm bất kỳ submission nào và update
    SELECT TOP 1 @SubmissionId = Id FROM Submissions ORDER BY Id DESC;
    
    IF @SubmissionId IS NOT NULL
    BEGIN
        -- Update status
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

-- 3. Tìm Admin
DECLARE @AdminId INT;
SELECT TOP 1 @AdminId = Id FROM Users WHERE Role = 1;

-- 4. Tạo hoặc Update Review Assignment
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
    
    -- Xóa review cũ nếu có
    DELETE FROM ReviewScores WHERE ReviewId IN (SELECT Id FROM Reviews WHERE ReviewAssignmentId = @AssignmentId);
    DELETE FROM Reviews WHERE ReviewAssignmentId = @AssignmentId;
    
    PRINT 'Updated Review Assignment ID: ' + CAST(@AssignmentId AS VARCHAR);
END

-- 5. Kiểm tra kết quả
PRINT '';
PRINT '=== KẾT QUẢ ===';
PRINT 'Reviewer ID: ' + CAST(@ReviewerId AS VARCHAR);
PRINT 'Submission ID: ' + CAST(@SubmissionId AS VARCHAR);
PRINT 'Assignment ID: ' + CAST(@AssignmentId AS VARCHAR);
PRINT '';
PRINT 'Đăng nhập với: reviewer1@scisubmit.com / Reviewer@123';
PRINT 'Truy cập: http://localhost:5234/Review';

GO



