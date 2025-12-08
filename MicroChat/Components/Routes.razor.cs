using Microsoft.AspNetCore.Components;

namespace MicroChat.Components;

public partial class Routes
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }
}
