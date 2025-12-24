using Microsoft.EntityFrameworkCore;
using SciSubmit.Models.Enums;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Conference;
using SciSubmit.Models.Content;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Review;
using System.Security.Cryptography;
using System.Text;

namespace SciSubmit.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Check if data already exists
            if (await context.Users.AnyAsync())
            {
                return; // Data already seeded
            }

            // 1. Create Users
            // Simple password hashing (for development only)
            string HashPassword(string password)
            {
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                    return Convert.ToBase64String(hashedBytes);
                }
            }

            var admin = new User
            {
                Email = "admin@scisubmit.com",
                PasswordHash = HashPassword("Admin@123"),
                FullName = "Admin User",
                Affiliation = "SciSubmit Organization",
                Role = UserRole.Admin,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var reviewer1 = new User
            {
                Email = "reviewer1@scisubmit.com",
                PasswordHash = HashPassword("Reviewer@123"),
                FullName = "GS. Nguyễn Văn X",
                Affiliation = "Đại học ABC",
                Role = UserRole.Reviewer,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var reviewer2 = new User
            {
                Email = "reviewer2@scisubmit.com",
                PasswordHash = HashPassword("Reviewer@123"),
                FullName = "PGS. Trần Thị Y",
                Affiliation = "Đại học XYZ",
                Role = UserRole.Reviewer,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var author1 = new User
            {
                Email = "author1@scisubmit.com",
                PasswordHash = HashPassword("Author@123"),
                FullName = "TS. Lê Văn A",
                Affiliation = "Đại học DEF",
                Role = UserRole.Author,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var author2 = new User
            {
                Email = "author2@scisubmit.com",
                PasswordHash = HashPassword("Author@123"),
                FullName = "ThS. Phạm Thị B",
                Affiliation = "Đại học GHI",
                Role = UserRole.Author,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(admin, reviewer1, reviewer2, author1, author2);
            await context.SaveChangesAsync();

            // 2. Create Conference
            var conference = new Conference
            {
                Name = "Hội thảo Khoa học Quốc tế về Kinh tế và Quản trị Kinh doanh 2025",
                Description = "Hội thảo khoa học quốc tế về các lĩnh vực kinh tế, tài chính, quản trị kinh doanh và marketing",
                Location = "TP. Hồ Chí Minh, Việt Nam",
                StartDate = new DateTime(2025, 4, 18, 8, 0, 0),
                EndDate = new DateTime(2025, 4, 20, 17, 0, 0),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Conferences.Add(conference);
            await context.SaveChangesAsync();

            // 3. Create ConferencePlan
            var plan = new ConferencePlan
            {
                ConferenceId = conference.Id,
                AbstractSubmissionOpenDate = new DateTime(2025, 1, 1, 0, 0, 0),
                AbstractSubmissionDeadline = new DateTime(2025, 1, 30, 23, 59, 0),
                FullPaperSubmissionOpenDate = new DateTime(2025, 2, 1, 0, 0, 0),
                FullPaperSubmissionDeadline = new DateTime(2025, 2, 15, 23, 59, 0),
                ReviewDeadline = new DateTime(2025, 2, 28, 23, 59, 0),
                ResultAnnouncementDate = new DateTime(2025, 3, 1, 0, 0, 0),
                ConferenceDate = new DateTime(2025, 4, 18, 8, 0, 0),
                CreatedAt = DateTime.UtcNow
            };

            context.ConferencePlans.Add(plan);
            await context.SaveChangesAsync();

            // 4. Create Topics
            var topics = new List<Topic>
            {
                new Topic { ConferenceId = conference.Id, Name = "Kinh tế", Description = "Các nghiên cứu về kinh tế học", IsActive = true, OrderIndex = 1, CreatedAt = DateTime.UtcNow },
                new Topic { ConferenceId = conference.Id, Name = "Tài chính & Kế toán", Description = "Nghiên cứu về tài chính và kế toán", IsActive = true, OrderIndex = 2, CreatedAt = DateTime.UtcNow },
                new Topic { ConferenceId = conference.Id, Name = "Thương mại", Description = "Nghiên cứu về thương mại", IsActive = true, OrderIndex = 3, CreatedAt = DateTime.UtcNow },
                new Topic { ConferenceId = conference.Id, Name = "Quản trị kinh doanh", Description = "Nghiên cứu về quản trị", IsActive = true, OrderIndex = 4, CreatedAt = DateTime.UtcNow },
                new Topic { ConferenceId = conference.Id, Name = "Marketing", Description = "Nghiên cứu về marketing", IsActive = true, OrderIndex = 5, CreatedAt = DateTime.UtcNow }
            };

            context.Topics.AddRange(topics);
            await context.SaveChangesAsync();

            // 5. Create Keywords
            var keywords = new List<Keyword>
            {
                new Keyword { ConferenceId = conference.Id, Name = "AI", Status = KeywordStatus.Approved, CreatedAt = DateTime.UtcNow, CreatedBy = admin.Id, ApprovedBy = admin.Id, ApprovedAt = DateTime.UtcNow },
                new Keyword { ConferenceId = conference.Id, Name = "Machine Learning", Status = KeywordStatus.Approved, CreatedAt = DateTime.UtcNow, CreatedBy = admin.Id, ApprovedBy = admin.Id, ApprovedAt = DateTime.UtcNow },
                new Keyword { ConferenceId = conference.Id, Name = "Tài chính", Status = KeywordStatus.Approved, CreatedAt = DateTime.UtcNow, CreatedBy = admin.Id, ApprovedBy = admin.Id, ApprovedAt = DateTime.UtcNow },
                new Keyword { ConferenceId = conference.Id, Name = "E-commerce", Status = KeywordStatus.Approved, CreatedAt = DateTime.UtcNow, CreatedBy = admin.Id, ApprovedBy = admin.Id, ApprovedAt = DateTime.UtcNow },
                new Keyword { ConferenceId = conference.Id, Name = "Thương mại điện tử", Status = KeywordStatus.Approved, CreatedAt = DateTime.UtcNow, CreatedBy = admin.Id, ApprovedBy = admin.Id, ApprovedAt = DateTime.UtcNow },
                new Keyword { ConferenceId = conference.Id, Name = "Blockchain", Status = KeywordStatus.Pending, CreatedAt = DateTime.UtcNow, CreatedBy = author1.Id },
                new Keyword { ConferenceId = conference.Id, Name = "Fintech", Status = KeywordStatus.Approved, CreatedAt = DateTime.UtcNow, CreatedBy = admin.Id, ApprovedBy = admin.Id, ApprovedAt = DateTime.UtcNow }
            };

            context.Keywords.AddRange(keywords);
            await context.SaveChangesAsync();

            // 6. Assign Keywords to Reviewers
            var aiKeyword = keywords.First(k => k.Name == "AI");
            var mlKeyword = keywords.First(k => k.Name == "Machine Learning");
            var financeKeyword = keywords.First(k => k.Name == "Tài chính");
            var fintechKeyword = keywords.First(k => k.Name == "Fintech");

            var userKeywords = new List<UserKeyword>
            {
                new UserKeyword { UserId = reviewer1.Id, KeywordId = aiKeyword.Id, CreatedAt = DateTime.UtcNow },
                new UserKeyword { UserId = reviewer1.Id, KeywordId = mlKeyword.Id, CreatedAt = DateTime.UtcNow },
                new UserKeyword { UserId = reviewer1.Id, KeywordId = financeKeyword.Id, CreatedAt = DateTime.UtcNow },
                new UserKeyword { UserId = reviewer2.Id, KeywordId = aiKeyword.Id, CreatedAt = DateTime.UtcNow },
                new UserKeyword { UserId = reviewer2.Id, KeywordId = financeKeyword.Id, CreatedAt = DateTime.UtcNow },
                new UserKeyword { UserId = reviewer2.Id, KeywordId = fintechKeyword.Id, CreatedAt = DateTime.UtcNow }
            };

            context.UserKeywords.AddRange(userKeywords);
            await context.SaveChangesAsync();

            // 7. Create Submissions
            var financeTopic = topics.First(t => t.Name == "Tài chính & Kế toán");
            var commerceTopic = topics.First(t => t.Name == "Thương mại");

            var submission1 = new Submission
            {
                ConferenceId = conference.Id,
                AuthorId = author1.Id,
                Title = "Nghiên cứu về ứng dụng AI trong quản lý tài chính",
                Abstract = "Bài báo nghiên cứu về việc ứng dụng trí tuệ nhân tạo (AI) trong quản lý tài chính doanh nghiệp. Nghiên cứu phân tích các mô hình machine learning để dự đoán rủi ro tài chính và tối ưu hóa quyết định đầu tư. Kết quả cho thấy AI có thể cải thiện đáng kể hiệu quả quản lý tài chính.",
                Status = SubmissionStatus.PendingAbstractReview,
                AbstractSubmittedAt = new DateTime(2025, 1, 15, 10, 30, 0),
                CreatedAt = new DateTime(2025, 1, 10, 8, 0, 0)
            };

            var submission2 = new Submission
            {
                ConferenceId = conference.Id,
                AuthorId = author2.Id,
                Title = "Phân tích xu hướng thương mại điện tử tại Việt Nam",
                Abstract = "Nghiên cứu phân tích các xu hướng phát triển của thương mại điện tử tại Việt Nam trong giai đoạn 2020-2025. Bài báo tập trung vào các yếu tố ảnh hưởng đến sự phát triển của e-commerce và đề xuất các giải pháp phát triển bền vững.",
                Status = SubmissionStatus.AbstractApproved,
                AbstractSubmittedAt = new DateTime(2025, 1, 10, 9, 0, 0),
                AbstractReviewedAt = new DateTime(2025, 1, 20, 14, 0, 0),
                CreatedAt = new DateTime(2025, 1, 5, 8, 0, 0)
            };

            var submission3 = new Submission
            {
                ConferenceId = conference.Id,
                AuthorId = author1.Id,
                Title = "Ứng dụng Blockchain trong thanh toán điện tử",
                Abstract = "Nghiên cứu về việc ứng dụng công nghệ blockchain trong hệ thống thanh toán điện tử. Bài báo phân tích các ưu điểm và thách thức của blockchain trong lĩnh vực fintech.",
                Status = SubmissionStatus.UnderReview,
                AbstractSubmittedAt = new DateTime(2025, 1, 12, 11, 0, 0),
                AbstractReviewedAt = new DateTime(2025, 1, 18, 15, 0, 0),
                FullPaperSubmittedAt = new DateTime(2025, 2, 5, 10, 0, 0),
                CreatedAt = new DateTime(2025, 1, 8, 8, 0, 0)
            };

            context.Submissions.AddRange(submission1, submission2, submission3);
            await context.SaveChangesAsync();

            // 8. Add Submission Authors
            var submissionAuthors1 = new List<SubmissionAuthor>
            {
                new SubmissionAuthor { SubmissionId = submission1.Id, FullName = author1.FullName, Email = author1.Email, Affiliation = author1.Affiliation, IsCorrespondingAuthor = true, OrderIndex = 1, CreatedAt = DateTime.UtcNow },
                new SubmissionAuthor { SubmissionId = submission1.Id, FullName = "Nguyễn Văn C", Email = "nguyenvanc@example.com", Affiliation = "Đại học JKL", IsCorrespondingAuthor = false, OrderIndex = 2, CreatedAt = DateTime.UtcNow }
            };

            var submissionAuthors2 = new List<SubmissionAuthor>
            {
                new SubmissionAuthor { SubmissionId = submission2.Id, FullName = author2.FullName, Email = author2.Email, Affiliation = author2.Affiliation, IsCorrespondingAuthor = true, OrderIndex = 1, CreatedAt = DateTime.UtcNow }
            };

            context.SubmissionAuthors.AddRange(submissionAuthors1);
            context.SubmissionAuthors.AddRange(submissionAuthors2);
            await context.SaveChangesAsync();

            // 9. Add Submission Topics
            var submissionTopics = new List<SubmissionTopic>
            {
                new SubmissionTopic { SubmissionId = submission1.Id, TopicId = financeTopic.Id, CreatedAt = DateTime.UtcNow },
                new SubmissionTopic { SubmissionId = submission2.Id, TopicId = commerceTopic.Id, CreatedAt = DateTime.UtcNow },
                new SubmissionTopic { SubmissionId = submission3.Id, TopicId = financeTopic.Id, CreatedAt = DateTime.UtcNow }
            };

            context.SubmissionTopics.AddRange(submissionTopics);
            await context.SaveChangesAsync();

            // 10. Add Submission Keywords
            var submissionKeywords = new List<SubmissionKeyword>
            {
                new SubmissionKeyword { SubmissionId = submission1.Id, KeywordId = aiKeyword.Id, CreatedAt = DateTime.UtcNow },
                new SubmissionKeyword { SubmissionId = submission1.Id, KeywordId = mlKeyword.Id, CreatedAt = DateTime.UtcNow },
                new SubmissionKeyword { SubmissionId = submission1.Id, KeywordId = financeKeyword.Id, CreatedAt = DateTime.UtcNow },
                new SubmissionKeyword { SubmissionId = submission2.Id, KeywordId = keywords.First(k => k.Name == "E-commerce").Id, CreatedAt = DateTime.UtcNow },
                new SubmissionKeyword { SubmissionId = submission2.Id, KeywordId = keywords.First(k => k.Name == "Thương mại điện tử").Id, CreatedAt = DateTime.UtcNow },
                new SubmissionKeyword { SubmissionId = submission3.Id, KeywordId = keywords.First(k => k.Name == "Blockchain").Id, CreatedAt = DateTime.UtcNow },
                new SubmissionKeyword { SubmissionId = submission3.Id, KeywordId = fintechKeyword.Id, CreatedAt = DateTime.UtcNow }
            };

            context.SubmissionKeywords.AddRange(submissionKeywords);
            await context.SaveChangesAsync();

            // 11. Create Review Assignments
            var reviewAssignment1 = new ReviewAssignment
            {
                SubmissionId = submission3.Id,
                ReviewerId = reviewer1.Id,
                Status = ReviewAssignmentStatus.Accepted,
                InvitedAt = new DateTime(2025, 2, 6, 10, 0, 0),
                InvitedBy = admin.Id,
                AcceptedAt = new DateTime(2025, 2, 7, 9, 0, 0),
                Deadline = new DateTime(2025, 2, 25, 23, 59, 0),
                CreatedAt = DateTime.UtcNow
            };

            var reviewAssignment2 = new ReviewAssignment
            {
                SubmissionId = submission3.Id,
                ReviewerId = reviewer2.Id,
                Status = ReviewAssignmentStatus.Pending,
                InvitedAt = new DateTime(2025, 2, 6, 10, 0, 0),
                InvitedBy = admin.Id,
                Deadline = new DateTime(2025, 2, 25, 23, 59, 0),
                CreatedAt = DateTime.UtcNow
            };

            context.ReviewAssignments.AddRange(reviewAssignment1, reviewAssignment2);
            await context.SaveChangesAsync();

            // 12. Create Review Criteria
            var reviewCriterias = new List<ReviewCriteria>
            {
                new ReviewCriteria { ConferenceId = conference.Id, Name = "Tính mới", Description = "Tính mới và sáng tạo của nghiên cứu", MaxScore = 5, OrderIndex = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ReviewCriteria { ConferenceId = conference.Id, Name = "Độ sâu nghiên cứu", Description = "Độ sâu và chi tiết của nghiên cứu", MaxScore = 5, OrderIndex = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ReviewCriteria { ConferenceId = conference.Id, Name = "Phương pháp nghiên cứu", Description = "Tính hợp lý và khoa học của phương pháp", MaxScore = 5, OrderIndex = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ReviewCriteria { ConferenceId = conference.Id, Name = "Trình bày", Description = "Cách trình bày và cấu trúc bài báo", MaxScore = 5, OrderIndex = 4, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            context.ReviewCriterias.AddRange(reviewCriterias);
            await context.SaveChangesAsync();

            // 13. Create a Review (for submission3)
            var review = new Review
            {
                ReviewAssignmentId = reviewAssignment1.Id,
                SubmissionId = submission3.Id,
                ReviewerId = reviewer1.Id,
                AverageScore = 4.2m,
                Recommendation = "Accept", // Accept, MinorRevision, MajorRevision, Reject
                CommentsForAuthor = "Bài báo có nội dung tốt, phương pháp nghiên cứu hợp lý. Tuy nhiên cần bổ sung thêm một số phân tích về rủi ro.",
                CommentsForAdmin = "Nghiên cứu có giá trị, đề xuất chấp nhận với điều kiện chỉnh sửa nhỏ.",
                SubmittedAt = new DateTime(2025, 2, 20, 14, 30, 0),
                CreatedAt = DateTime.UtcNow
            };

            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            // 14. Create Review Scores
            var reviewScores = new List<ReviewScore>
            {
                new ReviewScore { ReviewId = review.Id, CriteriaName = "Tính mới", Score = 4, CreatedAt = DateTime.UtcNow },
                new ReviewScore { ReviewId = review.Id, CriteriaName = "Độ sâu nghiên cứu", Score = 4, CreatedAt = DateTime.UtcNow },
                new ReviewScore { ReviewId = review.Id, CriteriaName = "Phương pháp nghiên cứu", Score = 5, CreatedAt = DateTime.UtcNow },
                new ReviewScore { ReviewId = review.Id, CriteriaName = "Trình bày", Score = 4, CreatedAt = DateTime.UtcNow }
            };

            context.ReviewScores.AddRange(reviewScores);
            await context.SaveChangesAsync();

            // Update review assignment to completed
            reviewAssignment1.CompletedAt = new DateTime(2025, 2, 20, 14, 30, 0);
            reviewAssignment1.Status = ReviewAssignmentStatus.Completed;
            await context.SaveChangesAsync();

            // 15. Create a Final Decision (for submission2 - already approved)
            var finalDecision = new FinalDecision
            {
                SubmissionId = submission2.Id,
                Decision = FinalDecisionType.Accepted,
                DecisionBy = admin.Id,
                DecisionReason = "Bài báo đáp ứng đầy đủ các tiêu chí của hội thảo. Nội dung nghiên cứu có giá trị và phù hợp với chủ đề.",
                AverageScore = 4.5m,
                DecidedAt = new DateTime(2025, 2, 22, 10, 0, 0),
                CreatedAt = DateTime.UtcNow
            };

            context.FinalDecisions.Add(finalDecision);
            submission2.Status = SubmissionStatus.Accepted;
            await context.SaveChangesAsync();

            // 16. Add more submissions for better testing
            var submission4 = new Submission
            {
                ConferenceId = conference.Id,
                AuthorId = author2.Id,
                Title = "Nghiên cứu về tác động của chuyển đổi số đến doanh nghiệp vừa và nhỏ",
                Abstract = "Nghiên cứu phân tích tác động của quá trình chuyển đổi số đến các doanh nghiệp vừa và nhỏ tại Việt Nam. Bài báo tập trung vào các thách thức và cơ hội mà chuyển đổi số mang lại.",
                Status = SubmissionStatus.AbstractRejected,
                AbstractSubmittedAt = new DateTime(2025, 1, 8, 11, 0, 0),
                AbstractReviewedAt = new DateTime(2025, 1, 15, 14, 0, 0),
                AbstractRejectionReason = "Nội dung chưa đủ tính mới và sáng tạo. Phương pháp nghiên cứu cần được làm rõ hơn.",
                CreatedAt = new DateTime(2025, 1, 5, 8, 0, 0)
            };

            var submission5 = new Submission
            {
                ConferenceId = conference.Id,
                AuthorId = author1.Id,
                Title = "Ứng dụng Big Data trong phân tích hành vi người tiêu dùng",
                Abstract = "Nghiên cứu ứng dụng công nghệ Big Data để phân tích hành vi người tiêu dùng trong lĩnh vực thương mại điện tử. Bài báo đề xuất một mô hình phân tích dựa trên machine learning.",
                Status = SubmissionStatus.FullPaperSubmitted,
                AbstractSubmittedAt = new DateTime(2025, 1, 12, 9, 0, 0),
                AbstractReviewedAt = new DateTime(2025, 1, 19, 15, 0, 0),
                FullPaperSubmittedAt = new DateTime(2025, 2, 10, 14, 0, 0),
                CreatedAt = new DateTime(2025, 1, 8, 8, 0, 0)
            };

            context.Submissions.AddRange(submission4, submission5);
            await context.SaveChangesAsync();

            // Add topics and keywords for new submissions
            var marketingTopic = topics.First(t => t.Name == "Marketing");
            var bigDataKeyword = new Keyword 
            { 
                ConferenceId = conference.Id, 
                Name = "Big Data", 
                Status = KeywordStatus.Approved, 
                CreatedAt = DateTime.UtcNow, 
                CreatedBy = admin.Id, 
                ApprovedBy = admin.Id, 
                ApprovedAt = DateTime.UtcNow 
            };
            context.Keywords.Add(bigDataKeyword);
            await context.SaveChangesAsync();

            context.SubmissionTopics.Add(new SubmissionTopic 
            { 
                SubmissionId = submission4.Id, 
                TopicId = commerceTopic.Id, 
                CreatedAt = DateTime.UtcNow 
            });
            context.SubmissionTopics.Add(new SubmissionTopic 
            { 
                SubmissionId = submission5.Id, 
                TopicId = marketingTopic.Id, 
                CreatedAt = DateTime.UtcNow 
            });

            context.SubmissionKeywords.Add(new SubmissionKeyword 
            { 
                SubmissionId = submission5.Id, 
                KeywordId = bigDataKeyword.Id, 
                CreatedAt = DateTime.UtcNow 
            });
            context.SubmissionKeywords.Add(new SubmissionKeyword 
            { 
                SubmissionId = submission5.Id, 
                KeywordId = mlKeyword.Id, 
                CreatedAt = DateTime.UtcNow 
            });

            await context.SaveChangesAsync();
        }
    }
}







