using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Model;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Ecoeex_Academy_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        private readonly PasswordHasher<AdminUser> _passwordHasher;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            _passwordHasher = new PasswordHasher<AdminUser>();
        }


        // =========================================================
        // REGISTER
        // =========================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var existingUser = await _context.tb_AdminUsers
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Admin already exists."
                });
            }

            var adminUser = new AdminUser
            {
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            adminUser.PasswordHash =
                _passwordHasher.HashPassword(
                    adminUser,
                    request.Password);

            _context.tb_AdminUsers.Add(adminUser);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Admin registered successfully."
            });
        }


        // =========================================================
        // LOGIN
        // =========================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var adminUser = await _context.tb_AdminUsers
                .FirstOrDefaultAsync(x =>
                    x.Email == request.Email);

            if (adminUser == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    adminUser,
                    adminUser.PasswordHash,
                    request.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }


            // Generate tokens

            var accessToken = GenerateAccessToken(adminUser);

            var refreshToken = GenerateRefreshToken();


            // Save refresh token

            adminUser.RefreshToken = refreshToken;

            adminUser.RefreshTokenExpiry =
                DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();


            return Ok(new
            {
                message = "Login successful.",

                accessToken = accessToken,

                refreshToken = refreshToken
            });
        }


        // =========================================================
        // REFRESH TOKEN
        // =========================================================

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            RefreshTokenRequest request)
        {
            var adminUser =
                await _context.tb_AdminUsers
                .FirstOrDefaultAsync(x =>
                    x.RefreshToken == request.RefreshToken);


            // Refresh token doesn't exist

            if (adminUser == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid refresh token."
                });
            }


            // Refresh token expired

            if (adminUser.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return Unauthorized(new
                {
                    message = "Refresh token expired."
                });
            }


            // Generate new tokens

            var newAccessToken =
                GenerateAccessToken(adminUser);

            var newRefreshToken =
                GenerateRefreshToken();


            // Rotate refresh token

            adminUser.RefreshToken =
                newRefreshToken;

            adminUser.RefreshTokenExpiry =
                DateTime.UtcNow.AddDays(7);


            await _context.SaveChangesAsync();


            return Ok(new
            {
                accessToken = newAccessToken,

                refreshToken = newRefreshToken
            });
        }


        // =========================================================
        // ACCESS TOKEN
        // =========================================================

        private string GenerateAccessToken(
            AdminUser adminUser)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    adminUser.AdminUserId.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    adminUser.Email),

                new Claim(
                    ClaimTypes.Role,
                    "Admin")
            };


            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));


            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);


            var token = new JwtSecurityToken(

                issuer:
                    _configuration["Jwt:Issuer"],

                audience:
                    _configuration["Jwt:Audience"],

                claims: claims,

                // Access token = 15 minutes
                expires:
                    DateTime.UtcNow.AddMinutes(15),

                signingCredentials:
                    credentials
            );


            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }


        // =========================================================
        // REFRESH TOKEN
        // =========================================================

        private string GenerateRefreshToken()
        {
            var randomBytes =
                RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(
                randomBytes);
        }
    }


    // =============================================================
    // REQUEST MODELS
    // =============================================================

    public class RegisterRequest
    {
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }


    public class LoginRequest
    {
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }


    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
    }
}