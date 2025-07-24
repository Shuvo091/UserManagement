namespace CohesionX.UserManagement.Application.Interfaces;

public interface IFileStorageService
{
	Task<string> StoreFileAsync(IFormFile file);
}