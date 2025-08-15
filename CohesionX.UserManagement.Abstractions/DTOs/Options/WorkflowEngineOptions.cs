// <copyright file="WorkflowEngineOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Abstractions.DTOs.Options;

/// <summary>
/// Represents options for configuring the workflow engine.
/// </summary>
public class WorkflowEngineOptions
{
    /// <summary>
    /// Gets or sets the base URI for the workflow engine API, which is used to construct API endpoints for workflow operations.
    /// </summary>
    public string BaseUrl { get; set; } = default!;
}
