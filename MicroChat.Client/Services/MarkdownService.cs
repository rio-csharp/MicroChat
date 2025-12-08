using Markdig;
using Markdown.ColorCode;
using System.Text;
using System.Text.RegularExpressions;

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

        // 提取代码块的语言信息（在转换前）
        var languageMap = ExtractCodeBlockLanguages(markdown);

        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);

        // 添加行号到代码块
        return AddLineNumbersToCodeBlocks(html, languageMap);
    }

    private Dictionary<int, string> ExtractCodeBlockLanguages(string markdown)
    {
        var languages = new Dictionary<int, string>();
        var pattern = @"```(\w*)\s*\n";
        var matches = Regex.Matches(markdown, pattern);

        int index = 0;
        foreach (Match match in matches)
        {
            var lang = match.Groups[1].Value;
            if (string.IsNullOrEmpty(lang))
            {
                lang = "text";
            }
            languages[index++] = lang.ToLower();
        }

        return languages;
    }

    private string AddLineNumbersToCodeBlocks(string html, Dictionary<int, string> languageMap)
    {
        // ColorCode 生成两种格式：
        // 1. <div style="..."><pre>代码</pre></div>  (ColorCode 格式)
        // 2. <pre><code>代码</code></pre>  (标准 Markdown 格式)

        // 先处理 ColorCode 格式
        var result = ProcessColorCodeBlocks(html, languageMap);
        html = result.html;
        var codeBlockIndex = result.nextIndex;

        // 再处理标准格式
        var pattern = @"<pre><code(?:\s+class=""([^""]*)"")?\s*>(.*?)</code></pre>";
        var currentIndex = codeBlockIndex;

        return Regex.Replace(html, pattern, match =>
        {
            var classes = match.Groups[1].Success ? match.Groups[1].Value : "";
            var codeContent = match.Groups[2].Value;

            // 如果内容已经包含 code-line 结构，说明已经处理过了
            if (codeContent.Contains("code-line"))
            {
                return match.Value;
            }

            var sb = new StringBuilder();
            var classAttr = string.IsNullOrEmpty(classes) ? "" : $" class=\"{classes}\"";

            // 获取语言信息 - 优先使用从原始 markdown 提取的语言
            var language = "text";
            if (languageMap.TryGetValue(currentIndex, out var lang))
            {
                language = lang;
            }
            else if (!string.IsNullOrEmpty(classes))
            {
                var match2 = Regex.Match(classes, @"language-(\w+)");
                if (match2.Success)
                {
                    language = match2.Groups[1].Value;
                }
            }

            currentIndex++;

            sb.Append($"<pre data-language=\"{language}\"><code{classAttr}>");

            // 对于纯文本代码块，解码后处理
            var decodedContent = System.Net.WebUtility.HtmlDecode(codeContent);
            var lines = decodedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // 移除首尾空白行
            lines = TrimEmptyLines(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                var encodedLine = System.Net.WebUtility.HtmlEncode(lines[i]);
                sb.Append($"<div class=\"code-line\">");
                sb.Append($"<span class=\"line-number\">{i + 1}</span>");
                sb.Append($"<span class=\"code-content\">{encodedLine}</span>");
                sb.Append("</div>");
            }

            sb.Append("</code></pre>");
            return sb.ToString();
        }, RegexOptions.Singleline);
    }

    private (string html, int nextIndex) ProcessColorCodeBlocks(string html, Dictionary<int, string> languageMap)
    {
        // 匹配 ColorCode 生成的格式：<div style="..."><pre>...</pre></div>
        var pattern = @"<div(?:\s+class=""([^""]*)"")?\s+style=""([^""]*)""><pre>(.*?)</pre></div>";
        var codeBlockIndex = 0;

        var result = Regex.Replace(html, pattern, match =>
        {
            var classes = match.Groups[1].Success ? match.Groups[1].Value : "";
            var style = match.Groups[2].Value;
            var codeContent = match.Groups[3].Value;

            // 如果已经处理过，跳过
            if (codeContent.Contains("code-line"))
            {
                return match.Value;
            }

            // 从原始 markdown 提取的语言映射中获取语言
            var language = "text";
            if (languageMap.TryGetValue(codeBlockIndex, out var lang))
            {
                language = lang;
            }

            codeBlockIndex++;

            // 替换 ColorCode 的深色样式为浅色样式
            var lightStyle = ConvertToLightTheme(style);

            var sb = new StringBuilder();
            sb.Append($"<div class=\"colorCode-wrapper\"><pre data-language=\"{language}\" style=\"{lightStyle}\">");

            // ColorCode 生成的代码包含 <span> 标签，需要按行分割并保持标签
            // 同时需要将深色主题的颜色替换为浅色主题
            codeContent = ConvertCodeToLightTheme(codeContent);
            var lines = SplitHtmlByLines(codeContent);

            // 移除首尾空白行
            lines = TrimEmptyHtmlLines(lines);

            for (int i = 0; i < lines.Count; i++)
            {
                sb.Append($"<div class=\"code-line\">");
                sb.Append($"<span class=\"line-number\">{i + 1}</span>");
                sb.Append($"<span class=\"code-content\">{lines[i]}</span>");
                sb.Append("</div>");
            }

            sb.Append("</pre></div>");
            return sb.ToString();
        }, RegexOptions.Singleline);

        return (result, codeBlockIndex);
    }

    private string ConvertToLightTheme(string style)
    {
        // 将深色背景替换为浅色背景
        return style
            .Replace("color:#DADADA", "color:#24292e")
            .Replace("color:#dadada", "color:#24292e")
            .Replace("background-color:#1E1E1E", "background-color:#f6f8fa")
            .Replace("background-color:#1e1e1e", "background-color:#f6f8fa")
            .Replace("background-color:#282c34", "background-color:#f6f8fa");
    }

    private string ConvertCodeToLightTheme(string codeHtml)
    {
        // ColorCode 生成的语法高亮颜色映射（深色 -> 浅色）
        var colorMap = new Dictionary<string, string>
        {
            // 关键字 (紫色 -> 深紫色)
            { "#569CD6", "#0550ae" },
            { "#569cd6", "#0550ae" },
            { "#C586C0", "#8250df" },
            { "#c586c0", "#8250df" },
            
            // 字符串 (绿色 -> 深绿色)
            { "#CE9178", "#0a3069" },
            { "#ce9178", "#0a3069" },
            { "#D69D85", "#0a3069" },
            { "#d69d85", "#0a3069" },
            
            // 注释 (灰色 -> 深灰色)
            { "#57A64A", "#1a7f37" },
            { "#57a64a", "#1a7f37" },
            { "#6A9955", "#1a7f37" },
            { "#6a9955", "#1a7f37" },
            
            // 数字 (橙色 -> 深橙色)
            { "#B5CEA8", "#0550ae" },
            { "#b5cea8", "#0550ae" },
            
            // 类型/函数
            { "#4EC9B0", "#0550ae" },
            { "#4ec9b0", "#0550ae" },
            { "#DCDCAA", "#953800" },
            { "#dcdcaa", "#953800" }
        };

        var result = codeHtml;
        foreach (var kvp in colorMap)
        {
            result = result.Replace($"color:{kvp.Key}", $"color:{kvp.Value}");
        }

        return result;
    }

    private string[] TrimEmptyLines(string[] lines)
    {
        if (lines.Length == 0) return lines;

        int start = 0;
        int end = lines.Length - 1;

        // 找到第一个非空行
        while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        // 找到最后一个非空行
        while (end >= 0 && string.IsNullOrWhiteSpace(lines[end]))
        {
            end--;
        }

        // 如果没有非空行，返回空数组
        if (start > end)
        {
            return Array.Empty<string>();
        }

        // 提取中间的行
        var result = new string[end - start + 1];
        Array.Copy(lines, start, result, 0, end - start + 1);
        return result;
    }

    private List<string> TrimEmptyHtmlLines(List<string> lines)
    {
        if (lines.Count == 0) return lines;

        int start = 0;
        int end = lines.Count - 1;

        // 找到第一个非空行（忽略只有HTML标签的行）
        while (start < lines.Count && IsEmptyHtmlLine(lines[start]))
        {
            start++;
        }

        // 找到最后一个非空行
        while (end >= 0 && IsEmptyHtmlLine(lines[end]))
        {
            end--;
        }

        // 如果没有非空行，返回空列表
        if (start > end)
        {
            return new List<string>();
        }

        // 提取中间的行
        return lines.GetRange(start, end - start + 1);
    }

    private bool IsEmptyHtmlLine(string line)
    {
        // 移除所有HTML标签后检查是否为空
        var withoutTags = Regex.Replace(line, @"<[^>]*>", "");
        return string.IsNullOrWhiteSpace(withoutTags);
    }

    private List<string> SplitHtmlByLines(string html)
    {
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var i = 0;

        while (i < html.Length)
        {
            // 检查换行符
            if (i < html.Length - 1 && html[i] == '\r' && html[i + 1] == '\n')
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                i += 2;
            }
            else if (html[i] == '\n')
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                i++;
            }
            else if (html[i] == '<')
            {
                // 遇到 HTML 标签，完整复制到下一个 >
                var tagEnd = html.IndexOf('>', i);
                if (tagEnd != -1)
                {
                    currentLine.Append(html.Substring(i, tagEnd - i + 1));
                    i = tagEnd + 1;
                }
                else
                {
                    currentLine.Append(html[i]);
                    i++;
                }
            }
            else
            {
                currentLine.Append(html[i]);
                i++;
            }
        }

        // 添加最后一行
        if (currentLine.Length > 0 || lines.Count > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }
}
