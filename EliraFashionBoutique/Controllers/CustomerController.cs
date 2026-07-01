using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize(Policy = "AdminAccess")]
public class CustomerController : Controller
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerController(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Customer customer)
    {
        if (customer == null)
        {
            return Json(new { success = false, message = "Invalid customer data." });
        }

        if (string.IsNullOrWhiteSpace(customer.FullName))
        {
            return Json(new { success = false, message = "Full Name is required." });
        }

        try
        {
            if (customer.CustomerId == 0)
            {
                await _customerRepository.AddAsync(customer);
            }
            else
            {
                var existing = await _customerRepository.GetByIdAsync(customer.CustomerId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Customer not found." });
                }

                existing.FullName = customer.FullName;
                existing.PhoneNo = customer.PhoneNo;
                existing.Address = customer.Address;
                existing.Gender = customer.Gender;
                existing.DateOfBirth = customer.DateOfBirth;
                existing.City = customer.City;
                existing.PostalCode = customer.PostalCode;
                existing.Country = customer.Country;

                await _customerRepository.UpdateAsync(existing);
            }
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _customerRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
