using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Admin
{
    public class KeywordViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public KeywordStatus Status { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class KeywordFilterViewModel
    {
        public KeywordStatus? Status { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}












