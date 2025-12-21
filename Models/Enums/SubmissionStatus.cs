namespace SciSubmit.Models.Enums
{
    public enum SubmissionStatus
    {
        Draft = 0,
        PendingAbstractReview = 1,
        AbstractRejected = 2,
        AbstractApproved = 3,
        FullPaperSubmitted = 4,
        UnderReview = 5,
        RevisionRequired = 6,
        Accepted = 7,
        Rejected = 8,
        Withdrawn = 9
    }
}
