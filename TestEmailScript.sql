-- Script kiểm tra email của user trong database
USE SciSubmit;

-- Kiểm tra user thaodtt22@uef.edu.vn
SELECT 
    Id,
    FullName,
    Email,
    Role,
    IsActive,
    CreatedAt
FROM Users 
WHERE Email = 'thaodtt22@uef.edu.vn';

-- Kiểm tra tất cả submissions của user này
SELECT 
    s.Id,
    s.Title,
    s.Status,
    s.AuthorId,
    u.Email AS AuthorEmail,
    s.CreatedAt
FROM Submissions s
INNER JOIN Users u ON s.AuthorId = u.Id
WHERE u.Email = 'thaodtt22@uef.edu.vn'
ORDER BY s.CreatedAt DESC;

-- Kiểm tra email notifications
SELECT TOP 10
    Id,
    ToEmail,
    Subject,
    Status,
    CreatedAt,
    SentAt,
    RelatedSubmissionId
FROM EmailNotifications
ORDER BY CreatedAt DESC;

