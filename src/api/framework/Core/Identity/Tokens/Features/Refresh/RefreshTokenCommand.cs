using FluentValidation;
using FSH.Framework.Core.Identity.Tokens.Models;
using MediatR;

namespace FSH.Framework.Core.Identity.Tokens.Features.Refresh;
public record RefreshTokenCommand : IRequest<TokenResponse>
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }

    public RefreshTokenCommand(string token, string refreshToken)
    {
        Token = token;
        RefreshToken = refreshToken;
    }
}

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(p => p.Token).Cascade(CascadeMode.Stop).NotEmpty();

        RuleFor(p => p.RefreshToken).Cascade(CascadeMode.Stop).NotEmpty();
    }
}
