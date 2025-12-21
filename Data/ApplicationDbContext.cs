using Microsoft.EntityFrameworkCore;
using SciSubmit.Models.Identity;
using SciSubmit.Models.Conference;
using SciSubmit.Models.Content;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Review;
using SciSubmit.Models.Payment;
using SciSubmit.Models.Notification;

namespace SciSubmit.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Conference> Conferences { get; set; }
        public DbSet<ConferencePlan> ConferencePlans { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Keyword> Keywords { get; set; }
        public DbSet<UserKeyword> UserKeywords { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionAuthor> SubmissionAuthors { get; set; }
        public DbSet<SubmissionKeyword> SubmissionKeywords { get; set; }
        public DbSet<SubmissionTopic> SubmissionTopics { get; set; }
        public DbSet<FullPaperVersion> FullPaperVersions { get; set; }
        public DbSet<ReviewCriteria> ReviewCriterias { get; set; }
        public DbSet<ReviewAssignment> ReviewAssignments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewScore> ReviewScores { get; set; }
        public DbSet<FinalDecision> FinalDecisions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentConfiguration> PaymentConfigurations { get; set; }
        public DbSet<EmailNotification> EmailNotifications { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<int>();
                
                entity.HasMany(e => e.Submissions)
                    .WithOne(e => e.Author)
                    .HasForeignKey(e => e.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.ReviewAssignments)
                    .WithOne(e => e.Reviewer)
                    .HasForeignKey(e => e.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.UserKeywords)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.CreatedKeywords)
                    .WithOne(e => e.Creator)
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.ApprovedKeywords)
                    .WithOne(e => e.Approver)
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.FinalDecisions)
                    .WithOne(e => e.DecisionMaker)
                    .HasForeignKey(e => e.DecisionBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.Payments)
                    .WithOne(e => e.User)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Conference Configuration
            modelBuilder.Entity<Conference>(entity =>
            {
                entity.HasMany(e => e.Plans)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.Topics)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.Keywords)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.Submissions)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.ReviewCriterias)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.PaymentConfigurations)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.EmailTemplates)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.SystemSettings)
                    .WithOne(e => e.Conference)
                    .HasForeignKey(e => e.ConferenceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Keyword Configuration
            modelBuilder.Entity<Keyword>(entity =>
            {
                entity.HasIndex(e => new { e.ConferenceId, e.Name }).IsUnique();
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.Creator)
                    .WithMany(e => e.CreatedKeywords)
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Approver)
                    .WithMany(e => e.ApprovedKeywords)
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.SubmissionKeywords)
                    .WithOne(e => e.Keyword)
                    .HasForeignKey(e => e.KeywordId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.UserKeywords)
                    .WithOne(e => e.Keyword)
                    .HasForeignKey(e => e.KeywordId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserKeyword Configuration - Unique constraint
            modelBuilder.Entity<UserKeyword>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.KeywordId }).IsUnique();
            });

            // Submission Configuration
            modelBuilder.Entity<Submission>(entity =>
            {
                entity.HasIndex(e => e.AuthorId);
                entity.HasIndex(e => e.ConferenceId);
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.PresentationType).HasConversion<int>();
                
                entity.HasOne(e => e.FinalDecision)
                    .WithOne(e => e.Submission)
                    .HasForeignKey<FinalDecision>(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.SubmissionAuthors)
                    .WithOne(e => e.Submission)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.SubmissionKeywords)
                    .WithOne(e => e.Submission)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.SubmissionTopics)
                    .WithOne(e => e.Submission)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.FullPaperVersions)
                    .WithOne(e => e.Submission)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.ReviewAssignments)
                    .WithOne(e => e.Submission)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.Payments)
                    .WithOne(e => e.Submission)
                    .HasForeignKey(e => e.SubmissionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SubmissionKeyword Configuration - Unique constraint
            modelBuilder.Entity<SubmissionKeyword>(entity =>
            {
                entity.HasIndex(e => new { e.SubmissionId, e.KeywordId }).IsUnique();
            });

            // SubmissionTopic Configuration - Unique constraint
            modelBuilder.Entity<SubmissionTopic>(entity =>
            {
                entity.HasIndex(e => new { e.SubmissionId, e.TopicId }).IsUnique();
            });
            
            // Topic Configuration
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasMany(e => e.SubmissionTopics)
                    .WithOne(e => e.Topic)
                    .HasForeignKey(e => e.TopicId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FullPaperVersion Configuration
            modelBuilder.Entity<FullPaperVersion>(entity =>
            {
                entity.HasIndex(e => e.SubmissionId);
                
                entity.HasOne(e => e.Uploader)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ReviewAssignment Configuration
            modelBuilder.Entity<ReviewAssignment>(entity =>
            {
                entity.HasIndex(e => e.ReviewerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Deadline);
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.Review)
                    .WithOne(e => e.ReviewAssignment)
                    .HasForeignKey<Review>(e => e.ReviewAssignmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Review Configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasIndex(e => e.ReviewAssignmentId).IsUnique();
                
                entity.HasOne(e => e.Reviewer)
                    .WithMany()
                    .HasForeignKey(e => e.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasMany(e => e.ReviewScores)
                    .WithOne(e => e.Review)
                    .HasForeignKey(e => e.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ReviewScore Configuration
            modelBuilder.Entity<ReviewScore>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_ReviewScore_Score", "[Score] >= 1 AND [Score] <= 5"));
            });
            
            // ReviewCriteria Configuration - No direct relationship with ReviewScore
            // ReviewScore links to Review, and ReviewCriteria is referenced by CriteriaName

            // FinalDecision Configuration
            modelBuilder.Entity<FinalDecision>(entity =>
            {
                entity.HasIndex(e => e.SubmissionId).IsUnique();
                entity.Property(e => e.Decision).HasConversion<int>();
            });
            
            // EmailNotification Configuration
            modelBuilder.Entity<EmailNotification>(entity =>
            {
                entity.HasOne(e => e.RelatedSubmission)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedSubmissionId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.RelatedUser)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Payment Configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasIndex(e => e.SubmissionId);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.PaymentMethod).HasConversion<int>();
            });

            // EmailNotification Configuration
            modelBuilder.Entity<EmailNotification>(entity =>
            {
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.Property(e => e.Status).HasConversion<int>();
            });

            // EmailTemplate Configuration
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasIndex(e => new { e.ConferenceId, e.Type }).IsUnique();
            });

            // SystemSetting Configuration
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasIndex(e => new { e.ConferenceId, e.Key }).IsUnique();
            });
        }
    }
}
