namespace ShirtStore.Domain.Interfaces;

/// <summary>
/// Abstração de envio de email — permite trocar de fornecedor sem tocar no domínio.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
