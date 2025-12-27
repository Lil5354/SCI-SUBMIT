using OfficeOpenXml;
using OfficeOpenXml.Style;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Enums;
using System.Drawing;

namespace SciSubmit.Services
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // EPPlus 7+ requires license context
        }

        public async Task<byte[]> ExportSubmissionsToExcelAsync(List<Submission> submissions)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Submissions");

                // Header row
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Tiêu đề";
                worksheet.Cells[1, 3].Value = "Tác giả";
                worksheet.Cells[1, 4].Value = "Email";
                worksheet.Cells[1, 5].Value = "Trạng thái";
                worksheet.Cells[1, 6].Value = "Ngày nộp";
                worksheet.Cells[1, 7].Value = "Ngày duyệt";
                worksheet.Cells[1, 8].Value = "Từ khóa";
                worksheet.Cells[1, 9].Value = "Chủ đề";

                // Style header
                using (var range = worksheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Data rows
                int row = 2;
                foreach (var submission in submissions)
                {
                    worksheet.Cells[row, 1].Value = submission.Id;
                    worksheet.Cells[row, 2].Value = submission.Title;
                    worksheet.Cells[row, 3].Value = submission.Author?.FullName ?? "";
                    worksheet.Cells[row, 4].Value = submission.Author?.Email ?? "";
                    worksheet.Cells[row, 5].Value = GetStatusDisplay(submission.Status);
                    worksheet.Cells[row, 6].Value = submission.AbstractSubmittedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                    worksheet.Cells[row, 7].Value = submission.AbstractReviewedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                    
                    // Keywords
                    var keywords = submission.SubmissionKeywords?
                        .Select(sk => sk.Keyword?.Name ?? "")
                        .Where(k => !string.IsNullOrEmpty(k))
                        .ToList() ?? new List<string>();
                    worksheet.Cells[row, 8].Value = string.Join(", ", keywords);

                    // Topics
                    var topics = submission.SubmissionTopics?
                        .Select(st => st.Topic?.Name ?? "")
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList() ?? new List<string>();
                    worksheet.Cells[row, 9].Value = string.Join(", ", topics);

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting submissions to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportReportsToExcelAsync(object reportData)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Reports");

                // This is a placeholder - implement based on actual report data structure
                worksheet.Cells[1, 1].Value = "Báo cáo";
                worksheet.Cells[2, 1].Value = "Ngày xuất";
                worksheet.Cells[2, 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reports to Excel");
                throw;
            }
        }

        private string GetStatusDisplay(SubmissionStatus status)
        {
            return status switch
            {
                SubmissionStatus.Draft => "Nháp",
                SubmissionStatus.PendingAbstractReview => "Chờ duyệt tóm tắt",
                SubmissionStatus.AbstractRejected => "Tóm tắt bị từ chối",
                SubmissionStatus.AbstractApproved => "Tóm tắt đã duyệt",
                SubmissionStatus.FullPaperSubmitted => "Đã nộp bài đầy đủ",
                SubmissionStatus.UnderReview => "Đang phản biện",
                SubmissionStatus.RevisionRequired => "Cần chỉnh sửa",
                SubmissionStatus.Accepted => "Đã chấp nhận",
                SubmissionStatus.Rejected => "Bị từ chối",
                SubmissionStatus.Withdrawn => "Đã rút",
                _ => status.ToString()
            };
        }
    }
}













