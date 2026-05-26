namespace Inviter.Api.Infrastructure.Email;

public class EmailOptions
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@inviter.nu";
    public string FromName { get; set; } = "Inviter";
    public bool UseStartTls { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);
}

public class AppOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5173";
    public string DashboardSlug { get; set; } = "";
    public string DataPath { get; set; } = "";
}
