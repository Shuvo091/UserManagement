// <copyright file="IdentityServerOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Represents options for configuring IdentityServer authentication.
/// </summary>
public class IdentityServerOptions
{
    /// <summary>
    /// Gets or sets the authority URL for IdentityServer.
    /// </summary>
    public string Authority { get; set; } = default!;

    /// <summary>
    /// Gets or sets the API name for which the JWT tokens are issued.
    /// </summary>
    public string ApiName { get; set; } = default!;
}
