using Microsoft.AspNetCore.Components;

namespace MicroChat.Client;

public partial class ChatHeaderIcons
{
    [Parameter]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public int Width { get; set; } = 20;

    [Parameter]
    public int Height { get; set; } = 20;
}
