using System.Text.Json;
using CohesionX.UserManagement.Abstractions.DTOs;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using CohesionX.UserManagement.Database.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using SharedLibrary.Common.Security;

namespace CohesionX.UserManagement.Application.Services;

/// <summary>
/// Service for managing admin configuration in the QA system.
/// </summary>
public class AdminAndQaUserSeederService
{
    private readonly IUserRepository userRepository;
    private readonly ILogger<AdminAndQaUserSeederService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminAndQaUserSeederService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="logger">Logger.</param>
    public AdminAndQaUserSeederService(
        IUserRepository userRepository,
        ILogger<AdminAndQaUserSeederService> logger)
    {
        this.userRepository = userRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Populates the database with admin configuration from a file.
    /// </summary>
    /// <param name="configFilePath">The path to the configuration file.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task PopulateDatabaseAsync(string configFilePath)
    {
        if (string.IsNullOrEmpty(configFilePath))
        {
            this.logger.LogError("Admin and QA user seeder file path cannot be null or empty.");
            throw new ArgumentException("Admin and QA user seeder file path cannot be null or empty.", nameof(configFilePath));
        }

        var configJson = await File.ReadAllTextAsync(configFilePath);
        var users = JsonSerializer.Deserialize<List<UserModel>>(configJson);

        if (users == null)
        {
            this.logger.LogError("Admin and QA user seeder file path cannot parsed.");
            throw new ArgumentException("Admin and QA user seeder file path cannot be parsed.", nameof(configFilePath));
        }

        var dtNow = DateTime.UtcNow;
        foreach (var user in users)
        {
            var userDb = await this.userRepository.GetUserByIdAsync(user.Id, true, false, u => u.Statistics!);
            if (userDb == null)
            {
                var userToBeSeeded = new User
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    UserName = user.Email,
                    PasswordHash = PasswordHasher.Hash(user.Password),
                    Phone = user.Phone,
                    IdNumber = user.IdNumber,
                    Status = user.Status,
                    Role = user.Role,
                    IsProfessional = user.IsProfessional,
                    CreatedAt = dtNow,
                    UpdatedAt = dtNow,
                    Statistics = new UserStatistics
                    {
                        TotalJobs = 0,
                        CurrentElo = 0,
                        PeakElo = 0,
                        GamesPlayed = 0,
                        LastCalculated = dtNow,
                        CreatedAt = dtNow,
                        UpdatedAt = dtNow,
                    },
                };
                await this.userRepository.AddAsync(userToBeSeeded);
            }
            else
            {
                userDb.FirstName = user.FirstName;
                userDb.LastName = user.LastName;
                userDb.Email = user.Email;
                userDb.UserName = user.Email;
                userDb.PasswordHash = PasswordHasher.Hash(user.Password);
                userDb.Phone = user.Phone;
                userDb.IdNumber = user.IdNumber;
                userDb.Status = user.Status;
                userDb.Role = user.Role;
                userDb.IsProfessional = user.IsProfessional;
                userDb.UpdatedAt = dtNow;
            }
        }

        await this.userRepository.SaveChangesAsync();
    }
}