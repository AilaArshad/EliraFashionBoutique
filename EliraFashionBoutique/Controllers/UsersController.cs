using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize(Policy = "AdminAccess")]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var list = users.Select(u => new
            {
                userId = u.UserId,
                roleName = u.RoleName,
                email = u.Email,
                isEmailVerified = u.IsEmailVerified ?? true,
                createdAt = u.CreatedAt ?? DateTime.Now,

                // Customer details
                fullName = u.Customer?.FullName,
                phoneNo = u.Customer?.PhoneNo,
                gender = u.Customer?.Gender,
                dateOfBirth = u.Customer?.DateOfBirth?.ToString("yyyy-MM-dd"),
                addressLine = u.Customer?.Address,
                city = u.Customer?.City,
                postalCode = u.Customer?.PostalCode,
                country = u.Customer?.Country,

                // Supplier details
                supplierName = u.Supplier?.SupplierName,
                contactPerson = u.Supplier?.ContactPerson,
                supplierPhone = u.Supplier?.Phone,
                supplierAddress = u.Supplier?.Address
            });

            return Json(list);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] User user)
    {
        if (user == null)
        {
            return Json(new { success = false, message = "Invalid user data." });
        }

        var errors = new Dictionary<string, string>();

        // 1. Email validation
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            errors["email"] = "Email is required.";
        }
        else if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            errors["email"] = "Invalid email format.";
        }

        // 2. Uniqueness check
        if (!errors.ContainsKey("email"))
        {
            var existingWithEmail = await _userRepository.GetByEmailAsync(user.Email);
            if (existingWithEmail != null && existingWithEmail.UserId != user.UserId)
            {
                errors["email"] = "Email is already taken.";
            }
        }

        // 3. Password validation
        if (user.UserId == 0) // Creation
        {
            if (string.IsNullOrWhiteSpace(user.Password))
            {
                errors["password"] = "Password is required.";
            }
            else if (user.Password.Length < 6)
            {
                errors["password"] = "Password must be at least 6 characters.";
            }
        }
        else // Update
        {
            if (!string.IsNullOrEmpty(user.Password) && user.Password.Length < 6)
            {
                errors["password"] = "Password must be at least 6 characters.";
            }
        }

        // 4. Role validation & role-specific details validation
        if (string.IsNullOrWhiteSpace(user.RoleName))
        {
            errors["roleName"] = "Role is required.";
        }
        else if (user.RoleName == "Customer")
        {
            if (user.Customer == null)
            {
                errors["fullName"] = "Customer details are required.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.Customer.FullName))
                {
                    errors["fullName"] = "Full Name is required.";
                }
                if (string.IsNullOrWhiteSpace(user.Customer.Address))
                {
                    errors["address"] = "Address is required."; // Used for showing error on address input
                }
            }
        }
        else if (user.RoleName == "Supplier")
        {
            if (user.Supplier == null)
            {
                errors["supplierName"] = "Supplier details are required.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.Supplier.SupplierName))
                {
                    errors["supplierName"] = "Supplier Name is required.";
                }
            }
        }

        if (errors.Any())
        {
            return Json(new { success = false, errors });
        }

        try
        {
            if (user.UserId == 0)
            {
                // Create
                user.CreatedAt = DateTime.Now;
                user.Password = _passwordHasher.HashPassword(user, user.Password);

                if (user.RoleName == "Customer")
                {
                    user.Supplier = null; // Ensure only customer profile is created
                }
                else if (user.RoleName == "Supplier")
                {
                    user.Customer = null; // Ensure only supplier profile is created
                    if (user.Supplier != null)
                    {
                        user.Supplier.CreatedAt = DateTime.Now;
                    }
                }
                else // Admin
                {
                    user.Customer = null;
                    user.Supplier = null;
                }

                await _userRepository.AddAsync(user);
            }
            else
            {
                // Update
                var existing = await _userRepository.GetByIdAsync(user.UserId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                existing.Email = user.Email;
                existing.IsEmailVerified = user.IsEmailVerified;

                if (!string.IsNullOrEmpty(user.Password))
                {
                    existing.Password = _passwordHasher.HashPassword(existing, user.Password);
                }

                if (existing.RoleName == "Customer")
                {
                    if (existing.Customer == null)
                    {
                        existing.Customer = new Customer { UserId = existing.UserId };
                    }
                    existing.Customer.FullName = user.Customer?.FullName ?? string.Empty;
                    existing.Customer.PhoneNo = user.Customer?.PhoneNo;
                    existing.Customer.Address = user.Customer?.Address ?? string.Empty;
                    existing.Customer.Gender = user.Customer?.Gender;
                    existing.Customer.DateOfBirth = user.Customer?.DateOfBirth;
                    existing.Customer.City = user.Customer?.City;
                    existing.Customer.PostalCode = user.Customer?.PostalCode;
                    existing.Customer.Country = user.Customer?.Country;
                }
                else if (existing.RoleName == "Supplier")
                {
                    if (existing.Supplier == null)
                    {
                        existing.Supplier = new Supplier { UserId = existing.UserId };
                    }
                    existing.Supplier.SupplierName = user.Supplier?.SupplierName ?? string.Empty;
                    existing.Supplier.ContactPerson = user.Supplier?.ContactPerson;
                    existing.Supplier.Phone = user.Supplier?.Phone;
                    existing.Supplier.Address = user.Supplier?.Address;
                }

                await _userRepository.UpdateAsync(existing);
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
