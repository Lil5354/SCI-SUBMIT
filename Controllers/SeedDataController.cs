using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Enums;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Conference;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Review;
using SciSubmit.Models.Content;

namespace SciSubmit.Controllers
{
    public class SeedDataController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeedDataController> _logger;

        public SeedDataController(ApplicationDbContext context, ILogger<SeedDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SeedTestData()
        {
            try
            {
                // Get or create active conference
                var conference = await _context.Conferences
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (conference == null)
                {
                conference = new Conference
                {
                    Name = "Hội thảo Khoa học Quốc tế 2025",
                    Description = "International Conference on Scientific Computing 2025",
                    StartDate = DateTime.UtcNow.AddMonths(3),
                    EndDate = DateTime.UtcNow.AddMonths(3).AddDays(3),
                    Location = "Hà Nội, Việt Nam",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                    _context.Conferences.Add(conference);
                    await _context.SaveChangesAsync();
                }

                // Create test users (Authors)
                var author1 = await GetOrCreateUser("author1@test.com", "Author One", UserRole.Author);
                var author2 = await GetOrCreateUser("author2@test.com", "Author Two", UserRole.Author);
                var author3 = await GetOrCreateUser("author3@test.com", "Author Three", UserRole.Author);

                // Create test reviewers
                var reviewer1 = await GetOrCreateUser("reviewer1@test.com", "Reviewer One", UserRole.Reviewer);
                var reviewer2 = await GetOrCreateUser("reviewer2@test.com", "Reviewer Two", UserRole.Reviewer);

                // Create topics
                var topic1 = await GetOrCreateTopic(conference.Id, "Tài chính & Kế toán", 1);
                var topic2 = await GetOrCreateTopic(conference.Id, "Thương mại", 2);
                var topic3 = await GetOrCreateTopic(conference.Id, "Quản trị kinh doanh", 3);
                var topic4 = await GetOrCreateTopic(conference.Id, "Marketing", 4);

                // Create keywords
                var keyword1 = await GetOrCreateKeyword(conference.Id, "AI", KeywordStatus.Approved);
                var keyword2 = await GetOrCreateKeyword(conference.Id, "Tài chính", KeywordStatus.Approved);
                var keyword3 = await GetOrCreateKeyword(conference.Id, "Thương mại điện tử", KeywordStatus.Approved);
                var keyword4 = await GetOrCreateKeyword(conference.Id, "Chuyển đổi số", KeywordStatus.Approved);
                var keyword5 = await GetOrCreateKeyword(conference.Id, "Marketing số", KeywordStatus.Approved);

                // Create test submissions
                var submission1 = await CreateSubmission(
                    conference.Id,
                    author1.Id,
                    "Nghiên cứu về ứng dụng AI trong quản lý tài chính",
                    GenerateAbstract(200),
                    topic1.Id,
                    new[] { keyword1.Id, keyword2.Id },
                    new[]
                    {
                        new { Name = "Nguyễn Văn A", Email = "nguyenvana@test.com", Affiliation = "Đại học ABC", IsCorresponding = true },
                        new { Name = "Trần Thị B", Email = "tranthib@test.com", Affiliation = "Đại học XYZ", IsCorresponding = false }
                    },
                    SubmissionStatus.PendingAbstractReview,
                    DateTime.UtcNow.AddDays(-10)
                );

                var submission2 = await CreateSubmission(
                    conference.Id,
                    author2.Id,
                    "Phân tích xu hướng thương mại điện tử tại Việt Nam",
                    GenerateAbstract(180),
                    topic2.Id,
                    new[] { keyword3.Id },
                    new[]
                    {
                        new { Name = "Nguyễn Văn C", Email = "nguyenvanc@test.com", Affiliation = "Đại học DEF", IsCorresponding = true }
                    },
                    SubmissionStatus.Accepted,
                    DateTime.UtcNow.AddDays(-15)
                );

                var submission3 = await CreateSubmission(
                    conference.Id,
                    author3.Id,
                    "Đánh giá tác động của chuyển đổi số lên doanh nghiệp vừa và nhỏ",
                    GenerateAbstract(220),
                    topic3.Id,
                    new[] { keyword4.Id },
                    new[]
                    {
                        new { Name = "Lê Văn D", Email = "levand@test.com", Affiliation = "Đại học GHI", IsCorresponding = true },
                        new { Name = "Phạm Thị E", Email = "phamthie@test.com", Affiliation = "Đại học JKL", IsCorresponding = false }
                    },
                    SubmissionStatus.RevisionRequired,
                    DateTime.UtcNow.AddDays(-20)
                );

                var submission4 = await CreateSubmission(
                    conference.Id,
                    author1.Id,
                    "Chiến lược marketing số cho doanh nghiệp SME",
                    GenerateAbstract(190),
                    topic4.Id,
                    new[] { keyword5.Id },
                    new[]
                    {
                        new { Name = "Hoàng Thị F", Email = "hoangthif@test.com", Affiliation = "Đại học MNO", IsCorresponding = true }
                    },
                    SubmissionStatus.Rejected,
                    DateTime.UtcNow.AddDays(-25)
                );

                // Create review assignments
                await CreateReviewAssignment(submission1.Id, reviewer1.Id, 1, DateTime.UtcNow.AddDays(15), ReviewAssignmentStatus.Pending);
                await CreateReviewAssignment(submission3.Id, reviewer1.Id, 1, DateTime.UtcNow.AddDays(10), ReviewAssignmentStatus.Accepted);
                await CreateReviewAssignment(submission4.Id, reviewer2.Id, 1, DateTime.UtcNow.AddDays(2), ReviewAssignmentStatus.Accepted); // Nearing deadline
                await CreateReviewAssignment(submission2.Id, reviewer1.Id, 1, DateTime.UtcNow.AddDays(-5), ReviewAssignmentStatus.Completed);
                await CreateReviewAssignment(submission3.Id, reviewer2.Id, 1, DateTime.UtcNow.AddDays(12), ReviewAssignmentStatus.Pending);

                // Create a completed review for submission2
                var assignment2 = await _context.ReviewAssignments
                    .FirstOrDefaultAsync(ra => ra.SubmissionId == submission2.Id && ra.ReviewerId == reviewer1.Id);
                
                if (assignment2 != null)
                {
                    var review = new Review
                    {
                        ReviewAssignmentId = assignment2.Id,
                        SubmissionId = submission2.Id,
                        ReviewerId = reviewer1.Id,
                        AverageScore = 5.0m,
                        Recommendation = "Accept",
                        CommentsForAuthor = "Bài báo có chất lượng tốt, nghiên cứu sâu sắc.",
                        CommentsForAdmin = "Nên chấp nhận bài báo này.",
                        SubmittedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    };
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã tạo dữ liệu test thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding test data");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        private async Task<User> GetOrCreateUser(string email, string fullName, UserRole role)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = fullName,
                    PhoneNumber = "0123456789",
                    PasswordHash = HashPassword("123456"),
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            return user;
        }

        private async Task<Topic> GetOrCreateTopic(int conferenceId, string name, int orderIndex)
        {
            var topic = await _context.Topics
                .FirstOrDefaultAsync(t => t.ConferenceId == conferenceId && t.Name == name);
            if (topic == null)
            {
                topic = new Topic
                {
                    ConferenceId = conferenceId,
                    Name = name,
                    OrderIndex = orderIndex,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Topics.Add(topic);
                await _context.SaveChangesAsync();
            }
            return topic;
        }

        private async Task<Keyword> GetOrCreateKeyword(int conferenceId, string name, KeywordStatus status)
        {
            var keyword = await _context.Keywords
                .FirstOrDefaultAsync(k => k.ConferenceId == conferenceId && k.Name == name);
            if (keyword == null)
            {
                keyword = new Keyword
                {
                    ConferenceId = conferenceId,
                    Name = name,
                    Status = status,
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Keywords.Add(keyword);
                await _context.SaveChangesAsync();
            }
            return keyword;
        }

        private async Task<Submission> CreateSubmission(
            int conferenceId,
            int authorId,
            string title,
            string abstractText,
            int topicId,
            int[] keywordIds,
            dynamic[] authors,
            SubmissionStatus status,
            DateTime createdAt)
        {
            var submission = new Submission
            {
                ConferenceId = conferenceId,
                AuthorId = authorId,
                Title = title,
                Abstract = abstractText,
                Status = status,
                AbstractSubmittedAt = createdAt,
                CreatedAt = createdAt
            };
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            // Add topic
            var submissionTopic = new SubmissionTopic
            {
                SubmissionId = submission.Id,
                TopicId = topicId,
                CreatedAt = DateTime.UtcNow
            };
            _context.SubmissionTopics.Add(submissionTopic);

            // Add keywords
            foreach (var keywordId in keywordIds)
            {
                var submissionKeyword = new SubmissionKeyword
                {
                    SubmissionId = submission.Id,
                    KeywordId = keywordId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SubmissionKeywords.Add(submissionKeyword);
            }

            // Add authors
            for (int i = 0; i < authors.Length; i++)
            {
                var author = authors[i];
                var submissionAuthor = new SubmissionAuthor
                {
                    SubmissionId = submission.Id,
                    FullName = author.Name,
                    Email = author.Email,
                    Affiliation = author.Affiliation,
                    IsCorrespondingAuthor = author.IsCorresponding,
                    OrderIndex = i + 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SubmissionAuthors.Add(submissionAuthor);
            }

            await _context.SaveChangesAsync();
            return submission;
        }

        private async Task<ReviewAssignment> CreateReviewAssignment(
            int submissionId,
            int reviewerId,
            int invitedBy,
            DateTime deadline,
            ReviewAssignmentStatus status)
        {
            var assignment = new ReviewAssignment
            {
                SubmissionId = submissionId,
                ReviewerId = reviewerId,
                Status = status,
                InvitedAt = DateTime.UtcNow.AddDays(-10),
                InvitedBy = invitedBy,
                Deadline = deadline,
                CreatedAt = DateTime.UtcNow
            };

            if (status == ReviewAssignmentStatus.Accepted)
            {
                assignment.AcceptedAt = DateTime.UtcNow.AddDays(-5);
            }
            else if (status == ReviewAssignmentStatus.Completed)
            {
                assignment.AcceptedAt = DateTime.UtcNow.AddDays(-10);
                assignment.CompletedAt = DateTime.UtcNow.AddDays(-5);
            }

            _context.ReviewAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            return assignment;
        }

        private string GenerateAbstract(int wordCount)
        {
            var words = new[] { "nghiên cứu", "phân tích", "đánh giá", "ứng dụng", "phương pháp", "kết quả", "thực nghiệm", "dữ liệu", "mô hình", "hệ thống", "giải pháp", "công nghệ", "quy trình", "hiệu quả", "chất lượng" };
            var random = new Random();
            var abstractText = new List<string>();
            
            for (int i = 0; i < wordCount; i++)
            {
                abstractText.Add(words[random.Next(words.Length)]);
            }
            
            return string.Join(" ", abstractText);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

