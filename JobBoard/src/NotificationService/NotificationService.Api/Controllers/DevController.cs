using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MassTransit;
using JobBoard.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace NotificationService.Api.Controllers;

[ApiController]
[Route("api/dev")]
public sealed class DevController(IConfiguration configuration, IBus bus) : ControllerBase
{
    private bool DevEnabled => string.Equals(
        configuration["Auth:DevEnabled"], "true", StringComparison.OrdinalIgnoreCase);

    [HttpPost("token")]
    public IActionResult CreateToken([FromBody] DevTokenRequest request)
    {
        if (!DevEnabled)
        {
            return NotFound();
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Auth:Key"] ?? throw new InvalidOperationException("Auth:Key is not configured")));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, request.Role));
        }

        var token = new JwtSecurityToken(
            issuer: "JobBoard",
            audience: "JobBoard",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    [HttpPost("publish-test-event")]
    public async Task<IActionResult> PublishTestEvent([FromBody] PublishTestEventRequest request)
    {
        if (!DevEnabled)
        {
            return NotFound();
        }

        switch (request.EventType.Trim(), (ApplicationStatus)Math.Max(1, Math.Min(5, request.Status)))
        {
            case ("ApplicationSubmitted", _):
                await bus.Publish(new ApplicationSubmittedEvent(
                    Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
                    EmployerUserId: request.EmployerUserId ?? Guid.NewGuid(),
                    ApplicantUserId: Guid.NewGuid(),
                    ApplicantName: "Dana Developer",
                    JobTitle: "Senior Backend Engineer (.NET)"));
                break;

            case ("JobPosted", _):
                await bus.Publish(new JobPostedEvent(
                    Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(),
                    EmployerUserId: request.EmployerUserId ?? Guid.NewGuid(),
                    CompanyName: "Acme Corp",
                    JobTitle: "Senior Backend Engineer (.NET)",
                    RecipientUserIds:
                    [
                        request.RecipientUserId ?? Guid.NewGuid()
                    ]));
                break;

            case ("ApplicationStatusChanged", var status):
                await bus.Publish(new ApplicationStatusChangedEvent(
                    Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
                    CandidateUserId: request.RecipientUserId ?? Guid.NewGuid(),
                    EmployerUserId: request.EmployerUserId ?? Guid.NewGuid(),
                    JobTitle: "Senior Backend Engineer (.NET)",
                    NewStatus: status));
                break;

            default:
                return BadRequest(new
                {
                    error = "eventType must be one of: ApplicationSubmitted, JobPosted, ApplicationStatusChanged"
                });
        }

        return Ok(new { published = request.EventType });
    }
}

public sealed record DevTokenRequest(Guid UserId, string Role);

public sealed record PublishTestEventRequest(string EventType, int Status = 5, Guid? EmployerUserId = null, Guid? RecipientUserId = null);
