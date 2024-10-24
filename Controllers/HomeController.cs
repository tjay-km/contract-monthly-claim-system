using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CMCS.Models;

namespace CMCS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogInformation($"User authenticated: {User.Identity.IsAuthenticated}");
        ViewData["Title"] = "Home Page";
        return View();
    }

    public IActionResult Features()
    {
        ViewData["Title"] = "Features";
        return View();
    }

    public IActionResult Help()
    {
        ViewData["Title"] = "Help";
        return View();
    }

    public IActionResult Faqs()
    {
        ViewData["Title"] = "Faqs";
        return View();
    }

    public IActionResult ToS()
    {
        ViewData["Title"] = "Terms of Service";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error404()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
