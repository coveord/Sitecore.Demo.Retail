using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Coveo.Framework.CNL;
using Coveo.SearchProvider.Utils;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Utils {
    public class HtmlTagsCleaner : IHtmlCleaner
    {
        /// <inheritDoc/>
        public string CleanHtmlContent(string p_HtmlContent,
                                       string p_StartTagContent,
                                       string p_EndTagContent)
        {
            Precondition.NotEmpty(p_HtmlContent, () => () => p_HtmlContent);
            Precondition.NotEmpty(p_StartTagContent, () => () => p_StartTagContent);
            Precondition.NotEmpty(p_EndTagContent, () => () => p_EndTagContent);

            string htmlContent = p_HtmlContent;
            string startTagContent = Regex.Escape(p_StartTagContent);
            string endTagContent = Regex.Escape(p_EndTagContent);
            Regex startTagRegex = new Regex(@"<\s*" + startTagContent + @"\s*>", RegexOptions.Singleline | RegexOptions.Compiled);
            Regex endTagRegex = new Regex(@"<\s*" + endTagContent + @"\s*>", RegexOptions.Singleline | RegexOptions.Compiled);

            Match startTagMatch = startTagRegex.Match(htmlContent);
            Match endTagMatch = endTagRegex.Match(htmlContent);

            while (StartTagIsBeforeEndTag(startTagMatch, endTagMatch)) {
                int firstPartLength = startTagMatch.Index;
                startTagMatch = startTagMatch.NextMatch();
                while (EndTagIsAfterNextStartTag(startTagMatch, endTagMatch)) {
                    startTagMatch = startTagMatch.NextMatch();
                    endTagMatch = endTagMatch.NextMatch();
                }
                int secondPartStartIndex = endTagMatch.Index + endTagMatch.Length;
                int secondPartLength = htmlContent.Length - secondPartStartIndex;
                htmlContent = htmlContent.Substring(0, firstPartLength) + htmlContent.Substring(secondPartStartIndex, secondPartLength);
                startTagMatch = startTagRegex.Match(htmlContent);
                endTagMatch = endTagRegex.Match(htmlContent);
            }

            return htmlContent;
        }

        /// <inheritDoc/>
        public bool ValidateHtmlCleaningDelimiters(string p_StartDelimiterText,
                                                   string p_EndDelimiterText)
        {
            return IsValidDelimiterText(p_StartDelimiterText) &&
                   IsValidDelimiterText(p_EndDelimiterText) &&
                   (p_StartDelimiterText != p_EndDelimiterText);
        }

        private bool IsValidDelimiterText(string p_DelimiterText)
        {
            return !String.IsNullOrWhiteSpace(p_DelimiterText) &&
                   p_DelimiterText.All(IsValidDelimiterCharacter);
        }

        private bool IsValidDelimiterCharacter(char p_CommentCharacter)
        {
            return p_CommentCharacter != '<' &&
                   p_CommentCharacter != '>';
        }

        private bool StartTagIsBeforeEndTag(Match p_StartTagMatch,
                                            Match p_EndTagMatch)
        {
            return p_StartTagMatch.Success &&
                   p_EndTagMatch.Success &&
                   p_StartTagMatch.Index <= p_EndTagMatch.Index;
        }

        private bool EndTagIsAfterNextStartTag(Match p_StartTagMatch,
                                               Match p_EndTagMatch)
        {
            return p_EndTagMatch.Index > p_StartTagMatch.Index &&
                   p_StartTagMatch.Success &&
                   p_EndTagMatch.Success;
        }
    }
}