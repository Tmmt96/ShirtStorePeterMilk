using ShirtStore.Domain.Interfaces;

namespace ShirtStore.Infrastructure.Email;

/// <summary>
/// Stub — substituir por implementação real (Brevo / Resend / SMTP) na fase de email.
/// </summary>
public class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        // TODO: integrar fornecedor de email transacional
        Console.WriteLine($"[EMAIL stub] To={toEmail} Subject={subject}");
        return Task.CompletedTask;
    }
}
