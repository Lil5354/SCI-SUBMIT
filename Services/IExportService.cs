using SciSubmit.Models.Submission;

namespace SciSubmit.Services
{
    public interface IExportService
    {
        /// <summary>
        /// Export submissions ra Excel file
        /// </summary>
        Task<byte[]> ExportSubmissionsToExcelAsync(List<Submission> submissions);

        /// <summary>
        /// Export reports/statistics ra Excel file
        /// </summary>
        Task<byte[]> ExportReportsToExcelAsync(object reportData);
    }
}











