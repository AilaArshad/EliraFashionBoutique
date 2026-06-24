using Microsoft.AspNetCore.Mvc;

namespace EliraFashionBoutique.Controllers;

public class ReportsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}