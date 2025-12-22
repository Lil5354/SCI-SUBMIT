-- ============================================
-- SCRIPT TẠO DATA MẪU ĐỂ TEST CHỨC NĂNG PHẢN BIỆN
-- ============================================
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio
-- Để test các chức năng: Phân công phản biện, Xem assignments, Ra quyết định cuối cùng

USE [SciSubmit] -- Thay đổi tên database nếu khác
GO

-- ============================================
-- 1. ĐẢM BẢO CÓ CONFERENCE VÀ TOPICS
-- ============================================
DECLARE @ConferenceId INT;
SELECT @ConferenceId = Id FROM Conferences WHERE IsActive = 1;

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
END

-- Tạo Topics nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Marketing')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Marketing', N'Nghiên cứu về marketing', 1, 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Tài chính')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Tài chính', N'Nghiên cứu về tài chính', 1, 2, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Công nghệ thông tin')
BEGIN
    INSERT INTO Topics (ConferenceId, Name, Description, IsActive, OrderIndex, CreatedAt)
    VALUES (@ConferenceId, N'Công nghệ thông tin', N'Nghiên cứu về công nghệ thông tin', 1, 3, GETUTCDATE());
END

-- ============================================
-- 2. ĐẢM BẢO CÓ USERS (AUTHORS VÀ REVIEWERS)
-- ============================================
DECLARE @AdminId INT;
DECLARE @Author1Id INT;
DECLARE @Author2Id INT;
DECLARE @Reviewer1Id INT;
DECLARE @Reviewer2Id INT;

-- Lấy Admin ID
SELECT @AdminId = Id FROM Users WHERE Role = 3 AND Email = 'admin@scisubmit.com';
IF @AdminId IS NULL
BEGIN
    SELECT TOP 1 @AdminId = Id FROM Users WHERE Role = 3; -- Admin role
END

-- Lấy hoặc tạo Author 1
SELECT @Author1Id = Id FROM Users WHERE Email = 'author1@scisubmit.com';
IF @Author1Id IS NULL
BEGIN
    SELECT TOP 1 @Author1Id = Id FROM Users WHERE Role = 1; -- Author role
END

-- Lấy hoặc tạo Author 2
SELECT @Author2Id = Id FROM Users WHERE Email = 'author2@scisubmit.com';
IF @Author2Id IS NULL
BEGIN
    SELECT TOP 1 @Author2Id = Id FROM Users WHERE Role = 1 AND Id != @Author1Id;
END

-- Lấy hoặc tạo Reviewer 1 (GS. Nguyễn Văn X)
SELECT @Reviewer1Id = Id FROM Users WHERE Email = 'reviewer1@scisubmit.com';
IF @Reviewer1Id IS NULL
BEGIN
    SELECT TOP 1 @Reviewer1Id = Id FROM Users WHERE Role = 2; -- Reviewer role
END

-- Lấy hoặc tạo Reviewer 2 (PGS. Trần Thị Y)
SELECT @Reviewer2Id = Id FROM Users WHERE Email = 'reviewer2@scisubmit.com';
IF @Reviewer2Id IS NULL
BEGIN
    SELECT TOP 1 @Reviewer2Id = Id FROM Users WHERE Role = 2 AND Id != @Reviewer1Id;
END

-- ============================================
-- 3. TẠO KEYWORDS
-- ============================================
DECLARE @KeywordBigDataId INT;
DECLARE @KeywordMachineLearningId INT;
DECLARE @KeywordBlockchainId INT;
DECLARE @KeywordFintechId INT;
DECLARE @KeywordAIId INT;

-- Big Data
IF NOT EXISTS (SELECT 1 FROM Keywords WHERE Name = N'Big Data' AND ConferenceId = @ConferenceId)
BEGIN
    INSERT INTO Keywords (ConferenceId, Name, Status, CreatedAt)
    VALUES (@ConferenceId, N'Big Data', 1, GETUTCDATE()); -- Approved
    SET @KeywordBigDataId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @KeywordBigDataId = Id FROM Keywords WHERE Name = N'Big Data' AND ConferenceId = @ConferenceId;
END

-- Machine Learning
IF NOT EXISTS (SELECT 1 FROM Keywords WHERE Name = N'Machine Learning' AND ConferenceId = @ConferenceId)
BEGIN
    INSERT INTO Keywords (ConferenceId, Name, Status, CreatedAt)
    VALUES (@ConferenceId, N'Machine Learning', 1, GETUTCDATE());
    SET @KeywordMachineLearningId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @KeywordMachineLearningId = Id FROM Keywords WHERE Name = N'Machine Learning' AND ConferenceId = @ConferenceId;
END

-- Blockchain
IF NOT EXISTS (SELECT 1 FROM Keywords WHERE Name = N'Blockchain' AND ConferenceId = @ConferenceId)
BEGIN
    INSERT INTO Keywords (ConferenceId, Name, Status, CreatedAt)
    VALUES (@ConferenceId, N'Blockchain', 1, GETUTCDATE());
    SET @KeywordBlockchainId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @KeywordBlockchainId = Id FROM Keywords WHERE Name = N'Blockchain' AND ConferenceId = @ConferenceId;
END

-- Fintech
IF NOT EXISTS (SELECT 1 FROM Keywords WHERE Name = N'Fintech' AND ConferenceId = @ConferenceId)
BEGIN
    INSERT INTO Keywords (ConferenceId, Name, Status, CreatedAt)
    VALUES (@ConferenceId, N'Fintech', 1, GETUTCDATE());
    SET @KeywordFintechId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @KeywordFintechId = Id FROM Keywords WHERE Name = N'Fintech' AND ConferenceId = @ConferenceId;
END

-- AI
IF NOT EXISTS (SELECT 1 FROM Keywords WHERE Name = N'AI' AND ConferenceId = @ConferenceId)
BEGIN
    INSERT INTO Keywords (ConferenceId, Name, Status, CreatedAt)
    VALUES (@ConferenceId, N'AI', 1, GETUTCDATE());
    SET @KeywordAIId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SELECT @KeywordAIId = Id FROM Keywords WHERE Name = N'AI' AND ConferenceId = @ConferenceId;
END

-- ============================================
-- 4. GÁN KEYWORDS CHO REVIEWERS
-- ============================================
-- Reviewer 1: Big Data, Machine Learning, AI
IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId = @Reviewer1Id AND KeywordId = @KeywordBigDataId)
BEGIN
    INSERT INTO UserKeywords (UserId, KeywordId, CreatedAt)
    VALUES (@Reviewer1Id, @KeywordBigDataId, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId = @Reviewer1Id AND KeywordId = @KeywordMachineLearningId)
BEGIN
    INSERT INTO UserKeywords (UserId, KeywordId, CreatedAt)
    VALUES (@Reviewer1Id, @KeywordMachineLearningId, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId = @Reviewer1Id AND KeywordId = @KeywordAIId)
BEGIN
    INSERT INTO UserKeywords (UserId, KeywordId, CreatedAt)
    VALUES (@Reviewer1Id, @KeywordAIId, GETUTCDATE());
END

-- Reviewer 2: Blockchain, Fintech, AI
IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId = @Reviewer2Id AND KeywordId = @KeywordBlockchainId)
BEGIN
    INSERT INTO UserKeywords (UserId, KeywordId, CreatedAt)
    VALUES (@Reviewer2Id, @KeywordBlockchainId, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId = @Reviewer2Id AND KeywordId = @KeywordFintechId)
BEGIN
    INSERT INTO UserKeywords (UserId, KeywordId, CreatedAt)
    VALUES (@Reviewer2Id, @KeywordFintechId, GETUTCDATE());
END

IF NOT EXISTS (SELECT 1 FROM UserKeywords WHERE UserId = @Reviewer2Id AND KeywordId = @KeywordAIId)
BEGIN
    INSERT INTO UserKeywords (UserId, KeywordId, CreatedAt)
    VALUES (@Reviewer2Id, @KeywordAIId, GETUTCDATE());
END

-- ============================================
-- 5. TẠO SUBMISSIONS VỚI STATUS PHÙ HỢP
-- ============================================
DECLARE @Submission1Id INT; -- Big Data submission
DECLARE @Submission2Id INT; -- Blockchain submission
DECLARE @MarketingTopicId INT;

SELECT @MarketingTopicId = Id FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Marketing';

-- Submission 1: Ứng dụng Big Data trong phân tích hành vi người tiêu dùng
IF NOT EXISTS (SELECT 1 FROM Submissions WHERE Title LIKE N'%Big Data%')
BEGIN
    INSERT INTO Submissions (
        ConferenceId, AuthorId, Title, Abstract, Status,
        AbstractSubmittedAt, AbstractReviewedAt, FullPaperSubmittedAt,
        CreatedAt
    )
    VALUES (
        @ConferenceId, @Author1Id,
        N'Ứng dụng Big Data trong phân tích hành vi người tiêu dùng',
        N'Nghiên cứu này trình bày việc ứng dụng công nghệ Big Data để phân tích hành vi người tiêu dùng trong lĩnh vực thương mại điện tử. Chúng tôi đề xuất một mô hình phân tích dựa trên machine learning để dự đoán xu hướng mua sắm của khách hàng.',
        4, -- FullPaperSubmitted
        '2025-01-20 10:00:00',
        '2025-01-21 15:00:00',
        '2025-01-25 14:00:00',
        '2025-01-20 10:00:00'
    );
    SET @Submission1Id = SCOPE_IDENTITY();
    
    -- Gán topic
    IF @MarketingTopicId IS NOT NULL
    BEGIN
        INSERT INTO SubmissionTopics (SubmissionId, TopicId, CreatedAt)
        VALUES (@Submission1Id, @MarketingTopicId, GETUTCDATE());
    END
    
    -- Gán keywords
    INSERT INTO SubmissionKeywords (SubmissionId, KeywordId, CreatedAt)
    VALUES (@Submission1Id, @KeywordBigDataId, GETUTCDATE());
    
    INSERT INTO SubmissionKeywords (SubmissionId, KeywordId, CreatedAt)
    VALUES (@Submission1Id, @KeywordMachineLearningId, GETUTCDATE());
END
ELSE
BEGIN
    SELECT @Submission1Id = Id FROM Submissions WHERE Title LIKE N'%Big Data%';
    -- Update status nếu cần
    UPDATE Submissions 
    SET Status = 4, -- FullPaperSubmitted
        FullPaperSubmittedAt = '2025-01-25 14:00:00'
    WHERE Id = @Submission1Id;
END

-- Submission 2: Ứng dụng Blockchain trong thanh toán điện tử
IF NOT EXISTS (SELECT 1 FROM Submissions WHERE Title LIKE N'%Blockchain%')
BEGIN
    INSERT INTO Submissions (
        ConferenceId, AuthorId, Title, Abstract, Status,
        AbstractSubmittedAt, AbstractReviewedAt, FullPaperSubmittedAt,
        CreatedAt
    )
    VALUES (
        @ConferenceId, @Author2Id,
        N'Ứng dụng Blockchain trong thanh toán điện tử',
        N'Nghiên cứu này khám phá việc ứng dụng công nghệ Blockchain trong lĩnh vực thanh toán điện tử và fintech. Chúng tôi phân tích các ưu điểm và thách thức của việc sử dụng blockchain trong các giao dịch tài chính.',
        5, -- UnderReview
        '2025-01-20 11:00:00',
        '2025-01-21 16:00:00',
        '2025-01-26 15:00:00',
        '2025-01-20 11:00:00'
    );
    SET @Submission2Id = SCOPE_IDENTITY();
    
    -- Gán topic
    DECLARE @FinanceTopicId INT;
    SELECT @FinanceTopicId = Id FROM Topics WHERE ConferenceId = @ConferenceId AND Name = N'Tài chính';
    IF @FinanceTopicId IS NOT NULL
    BEGIN
        INSERT INTO SubmissionTopics (SubmissionId, TopicId, CreatedAt)
        VALUES (@Submission2Id, @FinanceTopicId, GETUTCDATE());
    END
    
    -- Gán keywords
    INSERT INTO SubmissionKeywords (SubmissionId, KeywordId, CreatedAt)
    VALUES (@Submission2Id, @KeywordBlockchainId, GETUTCDATE());
    
    INSERT INTO SubmissionKeywords (SubmissionId, KeywordId, CreatedAt)
    VALUES (@Submission2Id, @KeywordFintechId, GETUTCDATE());
END
ELSE
BEGIN
    SELECT @Submission2Id = Id FROM Submissions WHERE Title LIKE N'%Blockchain%';
    -- Update status nếu cần
    UPDATE Submissions 
    SET Status = 5, -- UnderReview
        FullPaperSubmittedAt = '2025-01-26 15:00:00'
    WHERE Id = @Submission2Id;
END

-- ============================================
-- 6. TẠO REVIEW ASSIGNMENTS
-- ============================================
-- Assignment 1: Reviewer 1 cho Submission 2 (Blockchain) - COMPLETED
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @Submission2Id AND ReviewerId = @Reviewer1Id)
BEGIN
    INSERT INTO ReviewAssignments (
        SubmissionId, ReviewerId, Status, InvitedAt, InvitedBy, Deadline,
        AcceptedAt, CompletedAt, CreatedAt
    )
    VALUES (
        @Submission2Id, @Reviewer1Id, 3, -- Completed
        '2025-02-06 10:00:00',
        @AdminId,
        '2025-02-25 23:59:59',
        '2025-02-07 09:00:00',
        '2025-02-20 16:00:00',
        '2025-02-06 10:00:00'
    );
END
ELSE
BEGIN
    -- Update assignment để có status Completed
    UPDATE ReviewAssignments
    SET Status = 3, -- Completed
        AcceptedAt = '2025-02-07 09:00:00',
        CompletedAt = '2025-02-20 16:00:00'
    WHERE SubmissionId = @Submission2Id AND ReviewerId = @Reviewer1Id;
END

-- Assignment 2: Reviewer 2 cho Submission 2 (Blockchain) - PENDING
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @Submission2Id AND ReviewerId = @Reviewer2Id)
BEGIN
    INSERT INTO ReviewAssignments (
        SubmissionId, ReviewerId, Status, InvitedAt, InvitedBy, Deadline,
        CreatedAt
    )
    VALUES (
        @Submission2Id, @Reviewer2Id, 0, -- Pending
        '2025-02-06 10:00:00',
        @AdminId,
        '2025-02-25 23:59:59', -- Deadline trong tương lai hoặc quá hạn để test
        '2025-02-06 10:00:00'
    );
END

-- Assignment 3: Reviewer 1 cho Submission 1 (Big Data) - ACCEPTED (chưa complete)
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @Submission1Id AND ReviewerId = @Reviewer1Id)
BEGIN
    INSERT INTO ReviewAssignments (
        SubmissionId, ReviewerId, Status, InvitedAt, InvitedBy, Deadline,
        AcceptedAt, CreatedAt
    )
    VALUES (
        @Submission1Id, @Reviewer1Id, 1, -- Accepted
        '2025-01-28 10:00:00',
        @AdminId,
        '2025-02-28 23:59:59',
        '2025-01-29 09:00:00',
        '2025-01-28 10:00:00'
    );
END

-- ============================================
-- 7. TẠO REVIEWS CHO ASSIGNMENTS ĐÃ COMPLETED
-- ============================================
DECLARE @Assignment1Id INT;
SELECT @Assignment1Id = Id FROM ReviewAssignments 
WHERE SubmissionId = @Submission2Id AND ReviewerId = @Reviewer1Id AND Status = 3;

IF @Assignment1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Reviews WHERE ReviewAssignmentId = @Assignment1Id)
BEGIN
    INSERT INTO Reviews (
        ReviewAssignmentId, SubmissionId, ReviewerId,
        AverageScore, Recommendation, CommentsForAuthor, CommentsForAdmin,
        SubmittedAt, CreatedAt
    )
    VALUES (
        @Assignment1Id, @Submission2Id, @Reviewer1Id,
        8.5, -- Average score
        N'Accept',
        N'Bài báo có chất lượng tốt, nghiên cứu rõ ràng và có giá trị thực tiễn. Tác giả đã trình bày tốt về ứng dụng blockchain trong thanh toán điện tử.',
        N'Bài báo đáp ứng đầy đủ các tiêu chí đánh giá. Nên chấp nhận.',
        '2025-02-20 16:00:00',
        '2025-02-20 16:00:00'
    );
END

-- ============================================
-- 8. KIỂM TRA DATA ĐÃ TẠO
-- ============================================
PRINT '=== KIỂM TRA SUBMISSIONS ===';
SELECT 
    s.Id,
    s.Title,
    s.Status,
    CASE s.Status
        WHEN 0 THEN 'Draft'
        WHEN 1 THEN 'PendingAbstractReview'
        WHEN 2 THEN 'AbstractRejected'
        WHEN 3 THEN 'AbstractApproved'
        WHEN 4 THEN 'FullPaperSubmitted'
        WHEN 5 THEN 'UnderReview'
        WHEN 6 THEN 'RevisionRequired'
        WHEN 7 THEN 'Accepted'
        WHEN 8 THEN 'Rejected'
        WHEN 9 THEN 'Withdrawn'
    END AS StatusName,
    s.FullPaperSubmittedAt,
    u.FullName AS AuthorName
FROM Submissions s
INNER JOIN Users u ON s.AuthorId = u.Id
WHERE s.Id IN (@Submission1Id, @Submission2Id);

PRINT '=== KIỂM TRA REVIEW ASSIGNMENTS ===';
SELECT 
    ra.Id,
    s.Title AS SubmissionTitle,
    u.FullName AS ReviewerName,
    u.Email AS ReviewerEmail,
    CASE ra.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Accepted'
        WHEN 2 THEN 'Rejected'
        WHEN 3 THEN 'Completed'
    END AS StatusName,
    ra.InvitedAt,
    ra.Deadline,
    ra.AcceptedAt,
    ra.CompletedAt
FROM ReviewAssignments ra
INNER JOIN Submissions s ON ra.SubmissionId = s.Id
INNER JOIN Users u ON ra.ReviewerId = u.Id
WHERE s.Id IN (@Submission1Id, @Submission2Id)
ORDER BY ra.InvitedAt DESC;

PRINT '=== KIỂM TRA REVIEWS ===';
SELECT 
    r.Id,
    s.Title AS SubmissionTitle,
    u.FullName AS ReviewerName,
    r.AverageScore,
    r.Recommendation,
    r.SubmittedAt
FROM Reviews r
INNER JOIN ReviewAssignments ra ON r.ReviewAssignmentId = ra.Id
INNER JOIN Submissions s ON r.SubmissionId = s.Id
INNER JOIN Users u ON r.ReviewerId = u.Id
WHERE s.Id IN (@Submission1Id, @Submission2Id);

PRINT '=== KIỂM TRA KEYWORDS CỦA REVIEWERS ===';
SELECT 
    u.FullName AS ReviewerName,
    k.Name AS KeywordName
FROM UserKeywords uk
INNER JOIN Users u ON uk.UserId = u.Id
INNER JOIN Keywords k ON uk.KeywordId = k.Id
WHERE u.Id IN (@Reviewer1Id, @Reviewer2Id)
ORDER BY u.FullName, k.Name;

PRINT '=== KIỂM TRA KEYWORDS CỦA SUBMISSIONS ===';
SELECT 
    s.Title AS SubmissionTitle,
    k.Name AS KeywordName
FROM SubmissionKeywords sk
INNER JOIN Submissions s ON sk.SubmissionId = s.Id
INNER JOIN Keywords k ON sk.KeywordId = k.Id
WHERE s.Id IN (@Submission1Id, @Submission2Id)
ORDER BY s.Title, k.Name;

PRINT '';
PRINT 'Hoàn thành! Bây giờ bạn có thể test:';
PRINT '1. Phân công phản biện: Vào /Admin/Submissions, chọn bài báo, click "Phân công phản biện"';
PRINT '2. Xem assignments: Vào /Admin/Assignments để xem danh sách phân công';
PRINT '3. Ra quyết định cuối cùng: Vào /Admin/FinalDecision/{id} cho bài báo đã có reviews';
PRINT '';
PRINT 'Dữ liệu test:';
PRINT '- Submission 1 (Big Data): Status = FullPaperSubmitted, có 1 assignment Accepted';
PRINT '- Submission 2 (Blockchain): Status = UnderReview, có 2 assignments (1 Completed, 1 Pending)';
PRINT '- Reviewer 1: Keywords = Big Data, Machine Learning, AI';
PRINT '- Reviewer 2: Keywords = Blockchain, Fintech, AI';

GO





