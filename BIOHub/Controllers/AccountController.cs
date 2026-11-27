using Microsoft.AspNetCore.Mvc;
using BIOHub.ViewModels;
using BIOHub.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BIOHub.Data;
using System.Security.Claims;


namespace BIOHub.Controllers
{
    public class AccountController : Controller
    {
        private BIOHubDbContext _context;
        private IPasswordHasher<Users> _passwordHasher;

        public AccountController(BIOHubDbContext context, IPasswordHasher<Users> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

       
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
           if (!ModelState.IsValid)
            {
                return View(model);
            }




            bool usernameExists = await _context.Users.AnyAsync(u => u.UserName == model.Username);
            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.Username), "此帳號已被使用");

                return View(model);
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "此 Email 已被使用");
                return View(model);
            }

            var user = new Users
            {
                UserName = model.Username.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                Phone = model.Phone.Trim(),
                IsActive = true,
                CreateAt = DateTime.Now

            };
            user.PasswordHash = _passwordHasher.HashPassword(
                user, model.Password);
            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty, "無法完成註冊，帳號或Email已被使用");

                //var errorMessage = ex.GetBaseException().Message;
                //ModelState.AddModelError(
                //    string.Empty,
                //    $"資料庫錯誤:{errorMessage}"
                //    );

                return View(model);
            }

            TempData["SuccessMessage"] = "註冊成功，請登入";
            return RedirectToAction(nameof(Login));
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }



        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) 
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.Username);

            if(user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");

                return View(model);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
                );

            if(result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");

                return View(model);
            }

            user.LastAt = DateTime.Now;

            if(result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            }
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier, user.UsersID.ToString()),

                new Claim (ClaimTypes.Name, user.UserName)
            };

            // 建立登入身分
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            // 建立登入主體
            var principal = new ClaimsPrincipal(identity);
            // 正式登入，建立 Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme ,principal);

            return RedirectToAction("Index", "Dashboard");
        }


        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
