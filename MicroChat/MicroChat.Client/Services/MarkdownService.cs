using Markdig;
using Markdown.ColorCode;

namespace MicroChat.Client.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseColorCode()
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }
}
