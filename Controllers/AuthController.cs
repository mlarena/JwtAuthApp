using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JwtAuthApp.Data;
using JwtAuthApp.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace JwtAuthApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AuthController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            
            if (user == null || !_authService.VerifyPassword(model.Password, user.PasswordHash, user.Salt))
            {
                ModelState.AddModelError("", "Invalid credentials");
                return View(model);
            }

            var token = _authService.GenerateJwtToken(user);
            HttpContext.Session.SetString("JWToken", token);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("", "Username already exists");
                return View(model);
            }

            var (hash, salt) = _authService.HashPassword(model.Password);
            var user = new Models.User
            {
                Username = model.Username,
                PasswordHash = hash,
                Salt = salt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            var token = _authService.GenerateJwtToken(user);
            HttpContext.Session.SetString("JWToken", token);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWToken");
            return RedirectToAction("Login");
        }
    }

    public class LoginModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}