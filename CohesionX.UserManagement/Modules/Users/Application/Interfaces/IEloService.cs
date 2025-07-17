using CohesionX.UserManagement.Modules.Users.Application.DTOs;

namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IEloService
{
	Task<EloResultDto[]> ApplyEloUpdatesAsync(EloUpdateRequestDto request);
	Task<EloHistoryDto[]> GetHistoryAsync(Guid userId);
}