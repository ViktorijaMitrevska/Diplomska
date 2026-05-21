using Diplomska.Models;
using Diplomska.Services;
using Diplomska.ViewModels;
using Diplomska.ViewModels.Auth;
using DocumentFormat.OpenXml.Spreadsheet;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public class AuthController : Controller
{
    private readonly FirestoreService _firestore;
    private readonly FirebaseAuthService _auth;
    private readonly FirestoreUserService _userService;

    public AuthController(FirestoreService firestore, FirebaseAuthService auth, FirestoreUserService userService)
    {
        _firestore = firestore;
        _auth = auth;
        _userService = userService;
    }

    public IActionResult Login() => View();

    [HttpGet]
    public async Task<IActionResult> RegisterOwner()
    {
        if (await _userService.OwnerExistsAsync())
            return RedirectToAction("Login");

        return View();
    }
    [HttpPost]
    public async Task<IActionResult> RegisterOwner(RegisterViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            // 1️⃣ Firebase Auth register
            var uid = await _auth.Register(vm.Email, vm.Password);

            // 2️⃣ Firestore user
            var user = new AppUser
            {
                UserId = uid,
                Name = vm.Name,
                Email = vm.Email,
                Role = "Owner",
                OwnerId = null,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userService.AddUserAsync(user);

            return RedirectToAction("Login");
        }
        catch
        {
            ModelState.AddModelError("", "Грешка при регистрација");
            return View(vm);
        }
    }
    [Authorize(Roles = "Owner")]
    [HttpGet]
    public IActionResult RegisterEmployee()
    {
        if (!User.IsInRole("Owner"))
            return Unauthorized();

        return View();
    }
    [Authorize(Roles = "Owner")]
    [HttpPost]
    //public async Task<IActionResult> RegisterEmployee(RegisterViewModel vm)
    //{
    //    if (!User.IsInRole("Owner"))
    //        return Unauthorized();

    //    if (!ModelState.IsValid)
    //        return View(vm);

    //    try
    //    {
    //        var uid = await _auth.Register(vm.Email, vm.Password);

    //        var ownerId = User.FindFirst("UserId")?.Value;

    //        var employee = new AppUser
    //        {
    //            UserId = uid,
    //            Name = vm.Name,
    //            Email = vm.Email,
    //            Role = "Employee",
    //            OwnerId = ownerId,
    //            CreatedAt = DateTime.UtcNow,
    //            IsActive = true
    //        };

    //        await _userService.AddUserAsync(employee);

    //        return RedirectToAction("Profile");
    //    }
    //    catch (Exception ex)
    //    {
    //        ModelState.AddModelError("", ex.Message);
    //        return View(vm);
    //    }
    //}

public async Task<IActionResult> RegisterEmployee(RegisterViewModel vm)
{
    if (!User.IsInRole("Owner"))
        return Unauthorized();

    if (!ModelState.IsValid)
        return View(vm);

    try
    {
        var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(
            new UserRecordArgs
            {
                Email = vm.Email,
                Password = vm.Password,
                DisplayName = vm.Name
            });

        var ownerId = User.FindFirst("UserId")?.Value;

        var employee = new AppUser
        {
            UserId = userRecord.Uid,
            Name = vm.Name,
            Email = vm.Email,
            Role = "Employee",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _userService.AddUserAsync(employee);

        return RedirectToAction("Profile");
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", ex.Message);
        return View(vm);
    }
}


[HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            await _auth.Login(vm.Email, vm.Password);

            var user = await _userService.GetUserByEmailAsync(vm.Email);
            if (user == null || !user.IsActive)
                throw new Exception();

            var claims = new List<Claim>
            {
                new Claim("UserId", user.UserId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Firma");
        }
        catch
        {
            ModelState.AddModelError("", "Погрешен email или лозинка");
            return View(vm);
        }
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            await _auth.SendPasswordResetEmail(vm.Email);
            ViewBag.Message = "Проверете го вашиот email за линк за промена на лозинка.";
        }
        catch
        {
            ModelState.AddModelError("", "Грешка при испраќање email.");
        }

        return View(vm);
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            // повторен login → добиваш idToken
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var idToken = await _auth.Login(userEmail, vm.CurrentPassword);

            await _auth.ChangePassword(idToken, vm.NewPassword);

            ViewBag.Message = "Лозинката е успешно променета.";
        }
        catch
        {
            ModelState.AddModelError("", "Погрешна тековна лозинка.");
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirst("UserId")?.Value;
        if (userId == null)
            return RedirectToAction("Login");

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound();

        var vm = new UserProfileViewModel
        {
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? reason = null)
    {
        var ownerExists = await _userService.OwnerExistsAsync();
        ViewBag.OwnerExists = ownerExists;
        ViewBag.Reason = reason;
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login", new { reason = "logout" });
    }
}
