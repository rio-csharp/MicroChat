using MicroChat.Client.Enums;

namespace MicroChat.Client.Models;

public class AIModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ModelProvider? Provider => ModelProvider.GetProvider(GetModelProvider());

    // 聊天模型前缀映射到品牌/图标
    private readonly static Dictionary<string, ModelProviders> _models = new()
    {
        // OpenAI 系列（只保留聊天模型）
        { "gpt-", ModelProviders.OpenAI },
        { "chatgpt-", ModelProviders.OpenAI },
        { "o1-", ModelProviders.OpenAI },
        { "o3-", ModelProviders.OpenAI },
        { "o4-", ModelProviders.OpenAI },
        
        // Claude 系列
        { "claude-", ModelProviders.Claude },
        
        // Gemini 系列
        { "gemini-", ModelProviders.Gemini },
        { "gemma-", ModelProviders.Gemini },
        
        // Meta Llama 系列
        { "llama-", ModelProviders.Meta },
        { "llama2-", ModelProviders.Meta },
        { "llama3-", ModelProviders.Meta },
        { "llama-3", ModelProviders.Meta },
        
        // Mistral AI
        { "mistral-", ModelProviders.Mistral },
        { "mixtral-", ModelProviders.Mistral },
        { "pixtral-", ModelProviders.Mistral },
        
        // Cohere 系列（只保留对话模型）
        { "command-", ModelProviders.Cohere },
        
        // Grok
        { "grok-", ModelProviders.Grok },
        
        // Perplexity
        { "pplx-", ModelProviders.Perplexity },
        { "sonar-", ModelProviders.Perplexity },
        
        // DeepSeek 系列
        { "deepseek-", ModelProviders.DeepSeek },
        { "deepseek-chat", ModelProviders.DeepSeek },
        { "deepseek-coder", ModelProviders.DeepSeek },
        
        // 通义千问
        { "qwen-", ModelProviders.Qwen },
        { "qwen2", ModelProviders.Qwen },
        
        // 文心一言
        { "ernie-", ModelProviders.Ernie },
        
        // ChatGLM
        { "glm-", ModelProviders.ChatGLM },
        { "chatglm-", ModelProviders.ChatGLM },
        
        // Kimi (月之暗面)
        { "moonshot-", ModelProviders.Kimi },
        { "kimi-", ModelProviders.Kimi },
        
        // Yi (零一万物)
        { "yi-", ModelProviders.Yi },
        
        // 豆包 (字节跳动)
        { "doubao-", ModelProviders.Doubao },
        { "skylark-", ModelProviders.Doubao },
        
        // 混元 (腾讯)
        { "hunyuan-", ModelProviders.Hunyuan },
        
        // 星火 (讯飞)
        { "spark-", ModelProviders.Spark },
        
        // Azure OpenAI
        { "azure-", ModelProviders.Azure },
        
        // AWS Bedrock (托管多种模型)
        { "bedrock/", ModelProviders.AWS },
        { "amazon.", ModelProviders.AWS },
        
        // 其他开源聊天模型
        { "vicuna-", ModelProviders.Other },
        { "wizard-", ModelProviders.Other },
        { "falcon-", ModelProviders.Other },
        { "mpt-", ModelProviders.Other },
    };


    private ModelProviders GetModelProvider()
    {
        ModelProviders provider;
        if (_models.TryGetValue(Id, out provider))
        {
            return provider;
        }

        foreach (var kvp in _models)
        {
            if (Id.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return ModelProviders.Unknown;
    }
}
