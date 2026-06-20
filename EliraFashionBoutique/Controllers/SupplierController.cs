using EliraFashionBoutique.Models;
using EliraFashionBoutique.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EliraFashionBoutique.Controllers;

public class SupplierController : Controller
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierController(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] Supplier supplier)
    {
        if (supplier == null)
        {
            return Json(new { success = false, message = "Invalid supplier data." });
        }

        if (string.IsNullOrWhiteSpace(supplier.SupplierName))
        {
            return Json(new { success = false, message = "Supplier Name is required." });
        }

        try
        {
            if (supplier.SupplierId == 0)
            {
                await _supplierRepository.AddAsync(supplier);
            }
            else
            {
                var existing = await _supplierRepository.GetByIdAsync(supplier.SupplierId);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Supplier not found." });
                }

                existing.SupplierName = supplier.SupplierName;
                existing.ContactPerson = supplier.ContactPerson;
                existing.Phone = supplier.Phone;
                existing.Address = supplier.Address;

                await _supplierRepository.UpdateAsync(existing);
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
            await _supplierRepository.DeleteAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
