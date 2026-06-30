using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EliraFashionBoutique.Controllers;

[Authorize]
public class ReportsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}