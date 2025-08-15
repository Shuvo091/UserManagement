using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// DelegatingHandler that attaches the current user's JWT token to outgoing requests.
/// </summary>
public class WorkflowEngineAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEngineAuthHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
    public WorkflowEngineAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = this.httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token.Substring("Bearer ".Length));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
