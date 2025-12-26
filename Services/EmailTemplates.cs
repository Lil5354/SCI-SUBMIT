namespace SciSubmit.Services
{
    public static class EmailTemplates
    {
        public static string GetReviewInvitationEmail(
            string reviewerName,
            string paperTitle,
            string paperAbstract,
            DateTime deadline,
            string acceptUrl,
            string rejectUrl,
            string baseUrl)
        {
            var deadlineFormatted = deadline.ToString("dddd, MMMM dd, yyyy 'at' HH:mm");
            
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Review Invitation</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .email-container {{
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .email-header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff;
            padding: 30px 20px;
            text-align: center;
        }}
        .email-header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .email-body {{
            padding: 30px 20px;
        }}
        .greeting {{
            font-size: 16px;
            margin-bottom: 20px;
            color: #333;
        }}
        .content-section {{
            margin-bottom: 25px;
        }}
        .content-section h2 {{
            color: #667eea;
            font-size: 18px;
            margin-bottom: 10px;
            border-bottom: 2px solid #667eea;
            padding-bottom: 5px;
        }}
        .paper-title {{
            font-size: 18px;
            font-weight: 600;
            color: #2c3e50;
            margin: 15px 0;
            padding: 15px;
            background-color: #f8f9fa;
            border-left: 4px solid #667eea;
            border-radius: 4px;
        }}
        .paper-abstract {{
            font-size: 14px;
            color: #555;
            line-height: 1.8;
            margin: 15px 0;
            padding: 15px;
            background-color: #f8f9fa;
            border-radius: 4px;
            text-align: justify;
        }}
        .deadline-info {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .deadline-info strong {{
            color: #856404;
        }}
        .action-buttons {{
            text-align: center;
            margin: 30px 0;
            padding: 20px 0;
        }}
        .btn {{
            display: inline-block;
            padding: 14px 32px;
            margin: 8px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            font-size: 16px;
            transition: all 0.3s ease;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .btn-accept {{
            background-color: #28a745;
            color: #ffffff;
        }}
        .btn-accept:hover {{
            background-color: #218838;
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        }}
        .btn-reject {{
            background-color: #dc3545;
            color: #ffffff;
        }}
        .btn-reject:hover {{
            background-color: #c82333;
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
            border-top: 1px solid #dee2e6;
        }}
        .footer p {{
            margin: 5px 0;
        }}
        .note {{
            font-size: 13px;
            color: #6c757d;
            font-style: italic;
            margin-top: 20px;
            padding: 10px;
            background-color: #e9ecef;
            border-radius: 4px;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <h1>Review Invitation</h1>
        </div>
        <div class=""email-body"">
            <div class=""greeting"">
                Dear {reviewerName},
            </div>
            
            <div class=""content-section"">
                <p>We hope this message finds you well. We are pleased to invite you to serve as a reviewer for the following paper submitted to our conference.</p>
            </div>
            
            <div class=""content-section"">
                <h2>Paper Information</h2>
                <div class=""paper-title"">
                    {paperTitle}
                </div>
                <div class=""paper-abstract"">
                    <strong>Abstract:</strong><br>
                    {paperAbstract}
                </div>
            </div>
            
            <div class=""deadline-info"">
                <strong>Review Deadline:</strong> {deadlineFormatted}
            </div>
            
            <div class=""content-section"">
                <p>Your expertise in this field would be invaluable in evaluating this submission. We kindly request that you review the paper and provide your assessment by the deadline specified above.</p>
            </div>
            
            <div class=""action-buttons"">
                <a href=""{acceptUrl}"" class=""btn btn-accept"">Accept Invitation</a>
                <a href=""{rejectUrl}"" class=""btn btn-reject"">Decline Invitation</a>
            </div>
            
            <div class=""note"">
                <strong>Note:</strong> Please respond to this invitation at your earliest convenience. If you accept, you will be able to access the full paper and submit your review through the system.
            </div>
            
            <div class=""content-section"">
                <p>Thank you for your time and consideration. We look forward to your response.</p>
                <p>Best regards,<br>
                <strong>Conference Review Committee</strong></p>
            </div>
        </div>
        <div class=""footer"">
            <p>This is an automated message from the SciSubmit Conference Management System.</p>
            <p>Please do not reply to this email. If you have any questions, please contact the conference administration.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

