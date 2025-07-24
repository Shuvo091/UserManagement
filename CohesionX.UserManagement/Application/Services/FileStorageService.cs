using CohesionX.UserManagement.Application.Interfaces;

namespace CohesionX.UserManagement.Application.Services;

public class FileStorageService : IFileStorageService
{
	private readonly IWebHostEnvironment _env;
	private readonly IConfiguration _config;

	public FileStorageService(IWebHostEnvironment env, IConfiguration config)
	{
		_env = env;
		_config = config;
	}

	public async Task<string> StoreFileAsync(IFormFile file)
	{
		var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
		if (!Directory.Exists(uploadsPath))
		{
			Directory.CreateDirectory(uploadsPath);
		}

		var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
		var filePath = Path.Combine(uploadsPath, fileName);

		using (var stream = new FileStream(filePath, FileMode.Create))
		{
			await file.CopyToAsync(stream);
		}

		return $"/uploads/{fileName}";
	}
}