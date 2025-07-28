using System.Text.Json;
using CohesionX.UserManagement.Application.Interfaces;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Provides operations for notifying the workflow engine about Elo updates.
/// </summary>
public class WorkflowEngineClient : IWorkflowEngineClient
{
	private readonly HttpClient _httpClient;
	private readonly string _eloUpdateNotifyUri;

	/// <summary>
	/// Initializes a new instance of the <see cref="WorkflowEngineClient"/> class.
	/// </summary>
	/// <param name="httpClient">The HTTP client for making requests.</param>
	/// <param name="configuration">The configuration containing endpoint URIs.</param>
	public WorkflowEngineClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_eloUpdateNotifyUri = configuration["WORKFLOW_ENGINE_ELO_NOTIFY_URI"] !;
	}

	/// <summary>
	/// Notifies the workflow engine that an Elo update has occurred.
	/// </summary>
	/// <param name="request">The Elo update notification request details.</param>
	/// <returns>
	/// The response from the workflow engine acknowledging the update,
	/// or <c>null</c> if no response was received.
	/// </returns>
	public async Task<EloUpdateNotificationResponse?> NotifyEloUpdatedAsync(EloUpdateNotificationRequest request)
	{
		try
		{
			var response = await _httpClient.PostAsJsonAsync(_eloUpdateNotifyUri, request);

			if (!response.IsSuccessStatusCode)
			{
				// Optionally log or inspect the status code and content
				var errorContent = await response.Content.ReadAsStringAsync();

				// Log errorContent if needed
				return null;
			}

			return await response.Content.ReadFromJsonAsync<EloUpdateNotificationResponse>();
		}
		catch (HttpRequestException)
		{
			// Handle network-level errors
			// Log exception ex if needed
			return null;
		}
		catch (NotSupportedException)
		{
			// Handle unsupported content-type
			return null;
		}
		catch (JsonException)
		{
			// Handle JSON deserialization errors
			return null;
		}
	}
}
