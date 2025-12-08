using Microsoft.AspNetCore.Mvc;

namespace MicroChat.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ModelsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<IEnumerable<string>> Get()
    {
        var raw = _configuration["Models"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Ok(Array.Empty<string>());
        }

        var models = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(models);
    }
}
