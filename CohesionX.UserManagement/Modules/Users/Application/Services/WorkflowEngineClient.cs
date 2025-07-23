using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using System.Text.Json;

namespace CohesionX.UserManagement.Modules.Users.Application.Services;

public class WorkflowEngineClient : IWorkflowEngineClient
{
	private readonly HttpClient _httpClient;
	private readonly string _eloUpdateNotifyUri;

	public WorkflowEngineClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_eloUpdateNotifyUri = configuration["WORKFLOW_ENGINE_ELO_NOTIFY_URI"]!;
	}

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
		catch (HttpRequestException ex)
		{
			// Handle network-level errors
			// Log exception ex if needed
			return null;
		}
		catch (NotSupportedException ex)
		{
			// Handle unsupported content-type
			return null;
		}
		catch (JsonException ex)
		{
			// Handle JSON deserialization errors
			return null;
		}
	}

}
