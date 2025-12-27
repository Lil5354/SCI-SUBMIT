using System.ComponentModel.DataAnnotations;

namespace SciSubmit.Models.Review
{
    public class ReviewDetailsViewModel
    {
        public int AssignmentId { get; set; }
        public int SubmissionId { get; set; }
        
        // Blind Review - không hiển thị tên tác giả
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        
        public string? AbstractFileUrl { get; set; }
        public string? FullPaperFileUrl { get; set; }
        
        public DateTime Deadline { get; set; }
        public DateTime InvitedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        
        public string Status { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public bool IsUrgent { get; set; }
        
        // Review Criteria (do Admin cấu hình)
        public List<ReviewCriteriaViewModel> Criteria { get; set; } = new();
        
        // Existing review (nếu đã có)
        public bool HasExistingReview { get; set; }
        public int? ReviewId { get; set; }
        public Dictionary<string, int> ExistingScores { get; set; } = new();
        public string? ExistingCommentsForAuthor { get; set; }
        public string? ExistingCommentsForAdmin { get; set; }
        public string? ExistingRecommendation { get; set; }
    }
    
    public class ReviewCriteriaViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxScore { get; set; } = 5;
        public int OrderIndex { get; set; }
    }
    
    public class SubmitReviewViewModel
    {
        [Required(ErrorMessage = "Assignment ID là bắt buộc.")]
        public int AssignmentId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng đánh giá tất cả các tiêu chí.")]
        public Dictionary<string, int> Scores { get; set; } = new();
        
        [Required(ErrorMessage = "Vui lòng nhập bình luận cho tác giả.")]
        [MinLength(10, ErrorMessage = "Bình luận cho tác giả phải có ít nhất 10 ký tự.")]
        public string CommentsForAuthor { get; set; } = string.Empty;
        
        public string? CommentsForAdmin { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn khuyến nghị.")]
        public string Recommendation { get; set; } = string.Empty; // Accept, MinorRevision, MajorRevision, Reject
    }
}



