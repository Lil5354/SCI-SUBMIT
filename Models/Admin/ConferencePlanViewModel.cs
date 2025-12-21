namespace SciSubmit.Models.Admin
{
    public class ConferencePlanViewModel
    {
        public int? Id { get; set; }
        public int ConferenceId { get; set; }
        public DateTime AbstractSubmissionOpenDate { get; set; }
        public DateTime AbstractSubmissionDeadline { get; set; }
        public DateTime? FullPaperSubmissionOpenDate { get; set; }
        public DateTime? FullPaperSubmissionDeadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
        public DateTime? ResultAnnouncementDate { get; set; }
        public DateTime? ConferenceDate { get; set; }
    }
}
