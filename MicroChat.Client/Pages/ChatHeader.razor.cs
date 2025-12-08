using MicroChat.Client.Models;
using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components;

namespace MicroChat.Client.Pages;

public partial class ChatHeader : IDisposable
{
    [Parameter]
    public bool ShowBackButton { get; set; } = false;

    [Parameter]
    public EventCallback OnBackClicked { get; set; }

    [Parameter]
    public bool IsContainerMode { get; set; } = false;

    [Parameter]
    public EventCallback OnToggleContainerMode { get; set; }

    [Inject]
    private ConversationService ConversationService { get; set; } = default!;

    [Inject]
    private ModelService ModelService { get; set; } = default!;

    private List<string> Models { get; set; } = new();
    private string SelectedModelId { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var models = await ModelService.GetModelsAsync();
        if (models != null && models.Count > 0)
        {
            Models = models.ToList();
            var currentModelId = ConversationService.SelectedConversation?.AIModel?.Id;

            if (!string.IsNullOrEmpty(currentModelId) && Models.Contains(currentModelId))
            {
                SelectedModelId = currentModelId;
            }
            else
            {
                SelectedModelId = Models.First();
            }
        }
        ConversationService.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        ConversationService.OnChange -= StateHasChanged;
    }

    private async Task OnModelChangedAsync()
    {
        ConversationService.SelectedConversation!.AIModel = new AIModel()
        {
            Id = SelectedModelId,
            Name = SelectedModelId
        };
        await ConversationService.UpdateConversationAsync(ConversationService.SelectedConversation);
    }

    private void HandleBackClick()
    {
        OnBackClicked.InvokeAsync();
    }

    private void HandleToggleContainerMode()
    {
        OnToggleContainerMode.InvokeAsync();
    }
}
