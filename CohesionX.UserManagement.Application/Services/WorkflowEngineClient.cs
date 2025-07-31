// <copyright file="WorkflowEngineClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Handles communication with the Workflow Engine regarding Elo updates.
/// </summary>
public class WorkflowEngineClient : IWorkflowEngineClient
{
    private readonly HttpClient httpClient;
    private readonly string eloUpdateNotifyUri;
    private readonly ILogger<WorkflowEngineClient> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEngineClient"/> class.
    /// </summary>
    /// <param name="httpClient"> http client. </param>
    /// <param name="configOptions"> options. </param>
    /// <param name="logger"> logger. </param>
    public WorkflowEngineClient(HttpClient httpClient, IOptions<WorkflowEngineOptions> configOptions, ILogger<WorkflowEngineClient> logger)
    {
        this.httpClient = httpClient;
        this.eloUpdateNotifyUri = configOptions.Value.EloNotifyUri
                              ?? throw new ArgumentNullException(nameof(configOptions), "EloUpdateNotifyUri must be set.");
        this.logger = logger;
    }

    /// <summary>
    /// Notifies the workflow engine that an Elo update has occurred.
    /// </summary>
    /// <param name="request"> request. </param>
    /// <returns>Task of ELoUpdateNotification.</returns>
    public async Task<EloUpdateNotificationResponse?> NotifyEloUpdatedAsync(EloUpdateNotificationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            using var response = await this.httpClient.PostAsJsonAsync(this.eloUpdateNotifyUri, request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EloUpdateNotificationResponse>();
        }
        catch (Exception ex) when (ex is HttpRequestException or NotSupportedException or JsonException)
        {
            return null;
        }
    }
}
