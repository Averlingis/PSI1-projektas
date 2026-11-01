using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PSI1.Api.Data;
using PSI1.Api.DTOs;
using PSI1.Api.Extensions;
using PSI1.Api.Models;

namespace PSI1.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // Returns all available categories defined in the Category enum.
    // Left open to anonymous users.
    [HttpGet]
    public IActionResult GetCategories()
    {
        var categories = Enum.GetValues<Category>()
            .Select(category => category.ToString())
            .ToList();

        return Ok(categories);
    }

    [Authorize]
    [HttpPut("select")]
    public async Task<IActionResult> SelectCategory(CategorySelectionRequest request)
    {
        var user = await _db.GetCurrentUserAsync(User);

        user.SelectedCategory = request.Category;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Learning category changed to: {request.Category}.",
            category = request.Category.ToString()
        });
    }

    [Authorize]
    [HttpGet("selected")]
    public async Task<IActionResult> GetSelectedCategory()
    {
        var user = await _db.GetCurrentUserAsync(User);

        return Ok(new { category = user.SelectedCategory?.ToString() });
    }
}