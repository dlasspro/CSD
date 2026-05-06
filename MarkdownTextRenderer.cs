using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSD
{
    /// <summary>
    /// Markdown 文本渲染器
    /// 支持的语法：标题、粗体、斜体、删除线、代码、链接、列表、引用、代码块
    /// </summary>
    internal static class MarkdownTextRenderer
    {
        #region 文本规范化

        /// <summary>
        /// 规范化存储文本，统一使用 \n 换行
        /// </summary>
        public static string NormalizeStorageText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }

        /// <summary>
        /// 转换为编辑器文本，使用系统换行符
        /// </summary>
        public static string ToEditorText(string? text)
        {
            return NormalizeStorageText(text).Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// 获取纯文本（移除 Markdown 格式标记）
        /// </summary>
        public static string GetPlainText(string? text)
        {
            var normalized = NormalizeStorageText(text);
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            var result = new StringBuilder(normalized.Length);

            int i = 0;
            while (i < normalized.Length)
            {
                // 跳过代码块标记
                if (TrySkip(ref i, normalized, "```"))
                {
                    // 跳过整个代码块内容直到结束标记
                    while (i < normalized.Length && !TrySkip(ref i, normalized, "```"))
                    {
                        result.Append(normalized[i]);
                        i++;
                    }
                    continue;
                }

                // 跳过格式标记
                if (TrySkip(ref i, normalized, "**") ||
                    TrySkip(ref i, normalized, "__") ||
                    TrySkip(ref i, normalized, "~~") ||
                    TrySkip(ref i, normalized, "*") ||
                    TrySkip(ref i, normalized, "_") ||
                    TrySkip(ref i, normalized, "`"))
                {
                    continue;
                }

                // 跳过链接语法，保留文本
                if (normalized[i] == '[')
                {
                    int textEnd = normalized.IndexOf(']', i + 1);
                    if (textEnd > i + 1)
                    {
                        // 提取链接文本
                        result.Append(normalized.AsSpan(i + 1, textEnd - i - 1));
                        i = textEnd + 1;

                        // 跳过 URL 部分 (url)
                        if (i < normalized.Length && normalized[i] == '(')
                        {
                            int urlEnd = normalized.IndexOf(')', i + 1);
                            if (urlEnd > i)
                                i = urlEnd + 1;
                        }
                        continue;
                    }
                }

                // 跳过行首标记
                if (i == 0 || normalized[i - 1] == '\n')
                {
                    // 跳过标题标记
                    int headerLevel = 0;
                    while (i + headerLevel < normalized.Length && 
                           headerLevel < 6 && 
                           normalized[i + headerLevel] == '#')
                    {
                        headerLevel++;
                    }
                    if (headerLevel > 0 && i + headerLevel < normalized.Length && 
                        normalized[i + headerLevel] == ' ')
                    {
                        i += headerLevel + 1;
                        continue;
                    }

                    // 跳过引用标记
                    if (normalized[i] == '>')
                    {
                        i++;
                        if (i < normalized.Length && normalized[i] == ' ')
                            i++;
                        continue;
                    }

                    // 跳过列表标记
                    if (i + 1 < normalized.Length && 
                        (normalized[i] == '-' || normalized[i] == '*') && 
                        normalized[i + 1] == ' ')
                    {
                        i += 2;
                        continue;
                    }

                    // 跳过有序列表标记
                    if (char.IsDigit(normalized[i]))
                    {
                        int j = i;
                        while (j < normalized.Length && char.IsDigit(normalized[j]))
                            j++;
                        if (j < normalized.Length && normalized[j] == '.' && 
                            j + 1 < normalized.Length && normalized[j + 1] == ' ')
                        {
                            i = j + 2;
                            continue;
                        }
                    }
                }

                result.Append(normalized[i]);
                i++;
            }

            return result.ToString();
        }

        private static bool TrySkip(ref int index, string text, string marker)
        {
            if (index + marker.Length <= text.Length &&
                text.AsSpan(index, marker.Length).SequenceEqual(marker))
            {
                index += marker.Length;
                return true;
            }
            return false;
        }

        #endregion

        #region RichTextBlock 创建

        /// <summary>
        /// 创建 RichTextBlock 并渲染 Markdown 内容
        /// </summary>
        public static RichTextBlock CreateRichTextBlock(string? text, double fontSize, Brush? foreground = null)
        {
            var richTextBlock = new RichTextBlock
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = false,
                FontSize = fontSize
            };

            if (foreground is not null)
            {
                richTextBlock.Foreground = foreground;
            }

            var normalized = NormalizeStorageText(text);
            foreach (var paragraph in BuildParagraphs(normalized, fontSize))
            {
                richTextBlock.Blocks.Add(paragraph);
            }

            if (richTextBlock.Blocks.Count == 0)
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = string.Empty });
                richTextBlock.Blocks.Add(paragraph);
            }

            return richTextBlock;
        }

        #endregion

        #region 段落构建

        private static IEnumerable<Paragraph> BuildParagraphs(string text, double fontSize)
        {
            var lines = text.Split('\n');
            var inCodeBlock = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine ?? string.Empty;

                // 检测代码块开始/结束
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }

                if (inCodeBlock)
                {
                    yield return BuildCodeParagraph(line, fontSize);
                    continue;
                }

                yield return BuildParagraph(line, fontSize);
            }
        }

        private static Paragraph BuildParagraph(string line, double fontSize)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0) };

            if (string.IsNullOrEmpty(line))
            {
                paragraph.Inlines.Add(new Run { Text = " " });
                return paragraph;
            }

            // 标题
            int headerLevel = 0;
            int contentStart = 0;
            while (contentStart < line.Length && line[contentStart] == '#' && headerLevel < 6)
            {
                headerLevel++;
                contentStart++;
            }
            if (headerLevel > 0 && contentStart < line.Length && line[contentStart] == ' ')
            {
                contentStart++;
                paragraph.FontWeight = FontWeights.Bold;
                paragraph.FontSize = Math.Max(fontSize + (7 - headerLevel) * 2, fontSize + 2);
                AppendInlineContent(paragraph.Inlines, line.AsSpan(contentStart));
                return paragraph;
            }

            // 引用
            if (line.StartsWith(">", StringComparison.Ordinal))
            {
                paragraph.Margin = new Thickness(16, 0, 0, 0);
                paragraph.FontStyle = Windows.UI.Text.FontStyle.Italic;
                var quoteContent = line.AsSpan(1).TrimStart(' ');
                AppendInlineContent(paragraph.Inlines, quoteContent);
                return paragraph;
            }

            // 有序列表
            int digitEnd = 0;
            while (digitEnd < line.Length && char.IsDigit(line[digitEnd]))
                digitEnd++;
            if (digitEnd > 0 && digitEnd + 1 < line.Length && 
                line[digitEnd] == '.' && line[digitEnd + 1] == ' ')
            {
                var numberText = line.AsSpan(0, digitEnd + 2);
                paragraph.Inlines.Add(new Run { Text = numberText.ToString() });
                AppendInlineContent(paragraph.Inlines, line.AsSpan(digitEnd + 2));
                return paragraph;
            }

            // 无序列表
            if (line.Length >= 2 && (line[0] == '-' || line[0] == '*') && line[1] == ' ')
            {
                paragraph.Inlines.Add(new Run { Text = "• " });
                AppendInlineContent(paragraph.Inlines, line.AsSpan(2));
                return paragraph;
            }

            // 普通文本
            AppendInlineContent(paragraph.Inlines, line.AsSpan());
            return paragraph;
        }

        private static Paragraph BuildCodeParagraph(string line, double fontSize)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(12, 0, 0, 0),
                FontFamily = new FontFamily("Cascadia Mono"),
                FontSize = Math.Max(12, fontSize - 1)
            };

            paragraph.Inlines.Add(new Run { Text = string.IsNullOrEmpty(line) ? " " : line });
            return paragraph;
        }

        #endregion

        #region 内联内容解析

        private static void AppendInlineContent(InlineCollection inlines, ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                inlines.Add(new Run { Text = string.Empty });
                return;
            }

            var sb = new StringBuilder();
            int i = 0;

            while (i < text.Length)
            {
                // 尝试解析链接 [text](url)
                if (text[i] == '[')
                {
                    // 先输出累积的普通文本
                    if (sb.Length > 0)
                    {
                        inlines.Add(new Run { Text = sb.ToString() });
                        sb.Clear();
                    }

                    if (TryParseLink(text, ref i, out string? linkText, out string? linkUrl))
                    {
                        if (Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri))
                        {
                            var hyperlink = new Hyperlink { NavigateUri = uri };
                            hyperlink.Inlines.Add(new Run { Text = linkText });
                            inlines.Add(hyperlink);
                        }
                        else
                        {
                            inlines.Add(new Run { Text = linkText });
                        }
                        continue;
                    }
                    // 不是有效的链接，输出 [ 字符并继续
                    sb.Append('[');
                    i++;
                    continue;
                }

                // 尝试解析粗体 **text** 或 __text__
                if (TryParseDelimited(text, ref i, ref sb, inlines, "**", static s => s.FontWeight = FontWeights.Bold))
                    continue;
                if (TryParseDelimited(text, ref i, ref sb, inlines, "__", static s => s.FontWeight = FontWeights.Bold))
                    continue;

                // 尝试解析删除线 ~~text~~
                if (TryParseDelimited(text, ref i, ref sb, inlines, "~~", static s => s.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough))
                    continue;

                // 尝试解析斜体 *text* 或 _text_
                if (TryParseDelimited(text, ref i, ref sb, inlines, "*", static s => s.FontStyle = Windows.UI.Text.FontStyle.Italic))
                    continue;
                if (TryParseDelimited(text, ref i, ref sb, inlines, "_", static s => s.FontStyle = Windows.UI.Text.FontStyle.Italic))
                    continue;

                // 尝试解析行内代码 `code`
                if (text[i] == '`')
                {
                    if (sb.Length > 0)
                    {
                        inlines.Add(new Run { Text = sb.ToString() });
                        sb.Clear();
                    }

                    if (TryParseCode(text, ref i, out string? codeContent))
                    {
                        var span = new Span { FontFamily = new FontFamily("Cascadia Mono") };
                        span.Inlines.Add(new Run { Text = codeContent });
                        inlines.Add(span);
                        continue;
                    }
                    // 不是有效的代码，输出 ` 字符并继续
                    sb.Append('`');
                    i++;
                    continue;
                }

                // 尝试解析自动链接 URL
                if (i + 7 < text.Length && 
                    (text.Slice(i, 7).SequenceEqual("http://".AsSpan()) ||
                     text.Slice(i, 8).SequenceEqual("https://".AsSpan())))
                {
                    if (sb.Length > 0)
                    {
                        inlines.Add(new Run { Text = sb.ToString() });
                        sb.Clear();
                    }

                    if (TryParseUrl(text, ref i, out string? url))
                    {
                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            var hyperlink = new Hyperlink { NavigateUri = uri };
                            hyperlink.Inlines.Add(new Run { Text = url });
                            inlines.Add(hyperlink);
                        }
                        else
                        {
                            inlines.Add(new Run { Text = url });
                        }
                        continue;
                    }
                }

                // 普通字符
                sb.Append(text[i]);
                i++;
            }

            // 输出剩余的普通文本
            if (sb.Length > 0)
            {
                inlines.Add(new Run { Text = sb.ToString() });
            }
        }

        private static bool TryParseLink(ReadOnlySpan<char> text, ref int index, out string? linkText, out string? linkUrl)
        {
            linkText = null;
            linkUrl = null;

            if (index >= text.Length || text[index] != '[')
                return false;

            // 查找 ] 结束文本部分
            int textEnd = text.Slice(index + 1).IndexOf(']');
            if (textEnd < 0)
                return false;
            textEnd += index + 1;

            // 检查后面是否有 (url)
            int urlStart = textEnd + 1;
            if (urlStart >= text.Length || text[urlStart] != '(')
                return false;

            // 查找 ) 结束 URL 部分
            int urlEnd = text.Slice(urlStart + 1).IndexOf(')');
            if (urlEnd < 0)
                return false;
            urlEnd += urlStart + 1;

            // 提取文本和 URL
            linkText = text.Slice(index + 1, textEnd - index - 1).ToString();
            linkUrl = text.Slice(urlStart + 1, urlEnd - urlStart - 1).ToString();

            index = urlEnd + 1;
            return true;
        }

        private static bool TryParseDelimited(
            ReadOnlySpan<char> text,
            ref int index,
            ref StringBuilder buffer,
            InlineCollection inlines,
            string delimiter,
            Action<Span> applyStyle)
        {
            if (index + delimiter.Length > text.Length)
                return false;

            if (!text.Slice(index, delimiter.Length).SequenceEqual(delimiter.AsSpan()))
                return false;

            // 查找结束标记
            int contentStart = index + delimiter.Length;
            int end = text.Slice(contentStart).IndexOf(delimiter.AsSpan());
            if (end < 0)
                return false;
            end += contentStart;

            // 内容不能为空
            if (end == contentStart)
                return false;

            // 输出缓冲区内容
            if (buffer.Length > 0)
            {
                inlines.Add(new Run { Text = buffer.ToString() });
                buffer.Clear();
            }

            // 创建样式 span
            var span = new Span();
            applyStyle(span);
            span.Inlines.Add(new Run { Text = text.Slice(contentStart, end - contentStart).ToString() });
            inlines.Add(span);

            index = end + delimiter.Length;
            return true;
        }

        private static bool TryParseCode(ReadOnlySpan<char> text, ref int index, out string? codeContent)
        {
            codeContent = null;

            if (index >= text.Length || text[index] != '`')
                return false;

            // 查找结束 `
            int end = text.Slice(index + 1).IndexOf('`');
            if (end < 0)
                return false;
            end += index + 1;

            codeContent = text.Slice(index + 1, end - index - 1).ToString();
            index = end + 1;
            return true;
        }

        private static bool TryParseUrl(ReadOnlySpan<char> text, ref int index, out string? url)
        {
            url = null;

            int start = index;
            
            // 确认是 http:// 或 https:// 开头
            if (text.Slice(index).StartsWith("http://".AsSpan()))
                index += 7;
            else if (text.Slice(index).StartsWith("https://".AsSpan()))
                index += 8;
            else
                return false;

            // 读取 URL 直到遇到空白或结束
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            url = text.Slice(start, index - start).ToString();
            return true;
        }

        #endregion
    }
}
