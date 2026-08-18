using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecoeex_Academy_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserController(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.tb_Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Mobile,
                    u.AuthProvider,
                    u.RegistrationType,
                    u.UserType,
                    u.EmailVerified,
                    u.MobileVerified,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.tb_Users
                .AsNoTracking()
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Mobile,
                    u.AuthProvider,
                    u.RegistrationType,
                    u.UserType,
                    u.EmailVerified,
                    u.MobileVerified,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(user);
        }



        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.tb_Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            _context.tb_Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateUserRequest
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Mobile { get; set; }
        public string? Password { get; set; }
        public string? AuthProvider { get; set; }
        public string? RegistrationType { get; set; }
        public string? UserType { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Mobile { get; set; }
        public string? NewPassword { get; set; }
        public string? RegistrationType { get; set; }
        public string? UserType { get; set; }
    }
}
