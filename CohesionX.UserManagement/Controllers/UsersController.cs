using CohesionX.UserManagement.Modules.Users.Application.DTOs;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CohesionX.UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [IgnoreAntiforgeryToken]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;

        public UsersController(IUserService userService, IFileStorageService fileStorageService)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] UserRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string? idPhotoPath = null;
            if (dto.IdPhoto != null)
            {
                idPhotoPath = await _fileStorageService.StoreFileAsync(dto.IdPhoto);
            }

            var result = await _userService.RegisterUserAsync(dto, idPhotoPath);

            return Created($"/api/v1/users/{result.UserId}/profile", new
            {
                userId = result.UserId,
                eloRating = 1200,
                status = result.Status,
                profileUri = $"/api/v1/users/{result.UserId}/profile",
                verificationRequired = result.VerificationRequired
            });
        }
    }
} 