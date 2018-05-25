using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coveo.Framework.CNL;
using Coveo.Framework.Log;
using Coveo.Framework.Utils;
using Coveo.SearchProvider.Processors.FetchPageContent.PostProcessing;
using Coveo.SearchProvider.Utils;
using Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Utils;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Processors.FetchPageContent.PostProcessing {
    public class CleanHtmlTags : IFetchPageContentHtmlPostProcessingProcessor
    {
        private static bool s_HasLoggedErrorOnce;

        private static readonly ILogger s_Logger = CoveoLogManager.GetLogger(MethodBase.GetCurrentMethod()
                                                                                       .DeclaringType);

        private readonly IHtmlCleaner m_HtmlCleaner;

        /// <summary>
        /// Gets or sets the text inside the HTML start tag where to start removing markup.
        /// </summary>
        public string StartTagContent { get; set; }

        /// <summary>
        /// Gets or sets the text inside the HTML end tag where to end removing markup.
        /// </summary>
        public string EndTagContent { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="CleanHtmlTags"/>.
        /// </summary>
        public CleanHtmlTags()
            : this(new HtmlTagsCleaner())
        { }

        /// <summary>
        /// Creates a new instance of <see cref="CleanHtmlTags"/>.
        /// </summary>
        /// <param name="p_HtmlCleaner">The <see cref="IHtmlCleaner"/> used to clean the HTML.</param>
        public CleanHtmlTags(IHtmlCleaner p_HtmlCleaner)
        {
            Precondition.NotNull(p_HtmlCleaner, () => () => p_HtmlCleaner);

            m_HtmlCleaner = p_HtmlCleaner;
        }

        /// <inheritDoc />
        public void Process(FetchPageContentHtmlPostProcessingArgs p_Args)
        {
            Precondition.NotNull(p_Args, () => () => p_Args);

            if (ValidateParameters()) {
                p_Args.HtmlContent = m_HtmlCleaner.CleanHtmlContent(p_Args.HtmlContent, StartTagContent, EndTagContent);
            }
        }

        private bool ValidateParameters()
        {
            List<string> invalidParameters = new List<string>();

            if (String.IsNullOrWhiteSpace(StartTagContent)) {
                invalidParameters.Add("StartTagContent");
            }

            if (String.IsNullOrWhiteSpace(EndTagContent)) {
                invalidParameters.Add("EndTagContent");
            }

            if (StartTagContent.EqualsIgnoreCase(EndTagContent)) {
                invalidParameters.Add("StartTagContent");
                invalidParameters.Add("EndTagContent");
                s_Logger.Warn("The StartTagContent and EndTagContent values are the same. Please use different values.");
            }

            if (invalidParameters.Any()) {
                string errorMessage = String.Format("The \"{0}\" processor will not be executed because it is missing the following parameters: {1}.",
                                                    typeof(CleanHtmlTags).FullName,
                                                    String.Join(", ", invalidParameters));
                LogWarningOnTheFirstOccurrence(errorMessage);
            }

            return !invalidParameters.Any();
        }

        private static void LogWarningOnTheFirstOccurrence(string p_Message)
        {
            if (!s_HasLoggedErrorOnce) {
                s_HasLoggedErrorOnce = true;
                s_Logger.Warn(p_Message);
            } else {
                s_Logger.Debug(p_Message);
            }
        }
    }
}