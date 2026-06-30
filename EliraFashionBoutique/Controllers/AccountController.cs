using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EliraFashionBoutique.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public AccountController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Inventory");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Retrieve the user by email
        var user = await _userRepository.GetByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("Email", "Email address not found.");
            return View(model);
        }

        // 2. Prioritize checking if the email has been verified
        if (user.IsEmailVerified != true)
        {
            ModelState.AddModelError("Email", "Your email address is not verified.");
            return View(model);
        }

        // 3. Verify credentials
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("Password", "Invalid password.");
            return View(model);
        }

        // 4. Update LastLogin
        user.LastLogin = DateTime.Now;
        await _userRepository.UpdateAsync(user);

        // 5. Establish Cookie Authentication Session
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.RoleName)
        };

        // Add Customer/Supplier details to claims if available
        if (user.Customer != null)
        {
            claims.Add(new Claim("FullName", user.Customer.FullName));
        }
        else if (user.Supplier != null)
        {
            claims.Add(new Claim("FullName", user.Supplier.SupplierName));
        }
        else
        {
            claims.Add(new Claim("FullName", "Administrator"));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        // 6. Post-Login Redirection
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Inventory");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
