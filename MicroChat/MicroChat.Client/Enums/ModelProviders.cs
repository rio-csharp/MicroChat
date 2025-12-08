namespace MicroChat.Client.Enums;

// 聊天模型品牌枚举（用于图标显示）
public enum ModelProviders
{
    Unknown,

    // 国际主流
    OpenAI,          // ChatGPT, GPT-4
    Claude,          // Anthropic Claude
    Gemini,          // Google Gemini
    Meta,            // Llama
    Mistral,         // Mistral AI
    Cohere,          // Cohere Command
    Grok,            // Groq
    Perplexity,      // Perplexity

    // 中国主流
    DeepSeek,        // DeepSeek
    Qwen,            // 阿里通义千问
    Ernie,           // 百度文心一言
    ChatGLM,         // 智谱 ChatGLM
    Kimi,            // 月之暗面 Kimi/Moonshot
    Yi,              // 零一万物 Yi
    Doubao,          // 字节豆包
    Hunyuan,         // 腾讯混元
    Spark,           // 讯飞星火

    // 云平台
    Azure,           // Azure OpenAI
    AWS,             // AWS Bedrock

    // 其他
    Other
}
