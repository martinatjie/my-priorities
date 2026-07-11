namespace PrioritizationApp.Web.Auth;

public class EmailAllowlist
{
    private readonly HashSet<string> _allowed;

    public EmailAllowlist(IEnumerable<string> allowedEmails)
    {
        _allowed = allowedEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim().ToLowerInvariant())
            .ToHashSet();
    }

    public bool IsEmpty => _allowed.Count == 0;

    public bool IsAllowed(string? email) =>
        !string.IsNullOrWhiteSpace(email) &&
        _allowed.Contains(email.Trim().ToLowerInvariant());
}
