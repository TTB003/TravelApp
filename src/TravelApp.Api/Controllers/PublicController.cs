using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TravelApp.Api.Controllers;

[ApiController]
[Route("poi")]
[AllowAnonymous]
public class PublicController : Controller
{
    [HttpGet("detail/{id:int}")]
    public IActionResult Detail(int id)
    {
        // Redirect to Admin.Web public page
        return Redirect($"/public/poi/detail/{id}");
    }
}
