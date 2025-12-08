using Microsoft.AspNetCore.Components;

namespace MicroChat.Client.Components;

public partial class MessageItemIcons
{
    [Parameter]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public int Width { get; set; } = 16;

    [Parameter]
    public int Height { get; set; } = 16;
}
