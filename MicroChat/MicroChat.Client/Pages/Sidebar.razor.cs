using MicroChat.Client.Models;
using MicroChat.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace MicroChat.Client.Pages;

public partial class Sidebar : IDisposable
{
    [Parameter]
    public EventCallback OnChatSelected { get; set; }

    [Inject]
    private ConversationService ConversationService { get; set; } = default!;

    [Inject]
    private ModelService ModelService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ConversationService.OnChange += StateHasChanged;
        await ConversationService.LoadConversationsAsync();
        if (ConversationService.Conversations.Count == 0)
        {
            await CreateNewChatAsync();
        }
    }

    public void Dispose()
    {
        ConversationService.OnChange -= StateHasChanged;
    }

    private async Task SelectChatAsync(Guid conversationId)
    {
        ConversationService.SelectedConversationId = conversationId;
        await OnChatSelected.InvokeAsync();
    }

    private async Task CreateNewChatAsync()
    {
        var models = await ModelService.GetModelsAsync();
        var firstModel = models?.FirstOrDefault();

        var aiModel = firstModel != null ? new AIModel { Id = firstModel, Name = firstModel } : null;
        await ConversationService.CreateNewConversationAsync(aiModel);
    }

    private async Task DeleteChatAsync(MouseEventArgs e, Guid conversationId)
    {
        await ConversationService.DeleteConversationAsync(conversationId);

        // If no conversations left, create a new one
        if (ConversationService.Conversations.Count == 0)
        {
            await CreateNewChatAsync();
        }
    }
}
