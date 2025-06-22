using FSH.Framework.Core.Events;

namespace FSH.Framework.Core.Identity.Tokens.Events;

public class TokenGeneratedEvent : IEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiryTime { get; set; }
    public string EventType => nameof(TokenGeneratedEvent);
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
