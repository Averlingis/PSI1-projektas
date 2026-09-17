using Microsoft.AspNetCore.Mvc;
using PSI1.Api.Models;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetLanguages()
    {
    var languages = Enum.GetValues<Language>()
        .Select(language => language.ToString())
        .ToList();

    return Ok(languages);
    }
}