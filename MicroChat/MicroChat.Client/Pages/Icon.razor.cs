using Microsoft.AspNetCore.Components;

namespace MicroChat.Client.Pages;

public partial class Icon
{
    [Parameter] 
    public string? IconSlug { get; set; }
    
    [Parameter] 
    public string Format { get; set; } = "svg"; // svg, png, webp
    
    [Parameter] 
    public int Height { get; set; } = 32;
}
