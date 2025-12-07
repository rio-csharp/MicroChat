using MicroChat.Client.Enums;

namespace MicroChat.Client.Models;

public class ModelProvider
{
    public string Name { get; set; }
    public string IconSlug { get; set; }

    public static ModelProvider GetProvider(ModelProviders provider)
    {
        return _providers[provider];
    }

    private readonly static Dictionary<ModelProviders, ModelProvider> _providers = new()
    {
        { ModelProviders.Unknown, new ModelProvider { Name = "Unknown", IconSlug = string.Empty }  },
        
        // 国际主流
        { ModelProviders.OpenAI, new ModelProvider { Name = "OpenAI", IconSlug = "openai" } },
        { ModelProviders.Claude, new ModelProvider { Name = "Claude", IconSlug = "claude-color" } },
        { ModelProviders.Gemini, new ModelProvider { Name = "Gemini", IconSlug = "gemini-color" } },
        { ModelProviders.Meta, new ModelProvider { Name = "Meta", IconSlug = "meta-color" } },
        { ModelProviders.Mistral, new ModelProvider { Name = "Mistral", IconSlug = "mistral-color" } },
        { ModelProviders.Cohere, new ModelProvider { Name = "Cohere", IconSlug = "cohere-color" } },
        { ModelProviders.Groq, new ModelProvider { Name = "Groq", IconSlug = "groq" } },
        { ModelProviders.Perplexity, new ModelProvider { Name = "Perplexity", IconSlug = "perplexity-color" } },
        
        // 中国主流
        { ModelProviders.DeepSeek, new ModelProvider { Name = "DeepSeek", IconSlug = "deepseek-color" } },
        { ModelProviders.Qwen, new ModelProvider { Name = "通义千问", IconSlug = "qwen-color" } },
        { ModelProviders.Ernie, new ModelProvider { Name = "文心一言", IconSlug = "wenxin-color" } },
        { ModelProviders.ChatGLM, new ModelProvider { Name = "ChatGLM", IconSlug = "chatglm-color" } },
        { ModelProviders.Kimi, new ModelProvider { Name = "Kimi", IconSlug = "kimi-color" } },
        { ModelProviders.Yi, new ModelProvider { Name = "Yi", IconSlug = "yi-color" } },
        { ModelProviders.Doubao, new ModelProvider { Name = "豆包", IconSlug = "doubao-color" } },
        { ModelProviders.Hunyuan, new ModelProvider { Name = "混元", IconSlug = "hunyuan-color" } },
        { ModelProviders.Spark, new ModelProvider { Name = "星火", IconSlug = "spark-color" } },
        
        // 云平台
        { ModelProviders.Azure, new ModelProvider { Name = "Azure", IconSlug = "azure-color" } },
        { ModelProviders.AWS, new ModelProvider { Name = "AWS", IconSlug = "aws-color" } },
        
        // 其他
        { ModelProviders.Other, new ModelProvider { Name = "Other", IconSlug = string.Empty } },
    };
}
