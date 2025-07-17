namespace CohesionX.UserManagement.Modules.Users.Application.Interfaces;

public interface IFileStorageService
{
	Task<string> StoreFileAsync(IFormFile file);
}