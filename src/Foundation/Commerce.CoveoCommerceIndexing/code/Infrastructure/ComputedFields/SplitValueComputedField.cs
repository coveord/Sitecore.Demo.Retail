using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Coveo.Framework.CNL;
using Coveo.Framework.Databases;
using Coveo.Framework.Items;
using Coveo.Framework.Log;
using Coveo.SearchProvider.ComputedFields;
using Sitecore.ContentSearch;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Infrastructure.ComputedFields {
    public class SplitValueComputedField : ConfigurableComputedField
    {
        private static bool s_HasLoggedErrorOnce;

        private static readonly ILogger s_Logger = CoveoLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string SourceField => GetAttributeValue("sourceField");

        public string Separator => GetAttributeValue("separator");

        public SplitValueComputedField()
        {
        }

        protected SplitValueComputedField(XmlNode p_Configuration) : base(p_Configuration)
        {
        }

        protected SplitValueComputedField(ISitecoreFactory p_SitecoreFactory) : base(p_SitecoreFactory)
        {
        }

        public override object ComputeFieldValue(IIndexable p_Indexable)
        {
            s_Logger.TraceEntering();
            Precondition.NotNull(p_Indexable, () => () => p_Indexable);

            object values = null;
            if (AreFieldParametersValid()) {
                IItem item = new ItemWrapper(new IndexableWrapper(p_Indexable));
                values = GetSplitValues(item);
            }

            s_Logger.TraceExiting();
            return values;
        }

        private bool AreFieldParametersValid()
        {
            List<string> invalidParameters = new List<string>();

            if (String.IsNullOrWhiteSpace(SourceField)) {
                invalidParameters.Add("SourceField");
            }

            if (String.IsNullOrWhiteSpace(Separator)) {
                invalidParameters.Add("Separator");
            }

            if (invalidParameters.Any()) {
                string errorMessage = String.Format("The \"{0}\" processor will not be executed because it is missing the following parameters: {1}.",
                                                    typeof(SplitValueComputedField).FullName,
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

        private object GetSplitValues(IItem p_Item)
        {
            Precondition.NotNull(p_Item, () => () => p_Item);

            string[] splitValues = null;
            string fieldValue = p_Item.GetFieldValue(SourceField);
            if (!String.IsNullOrWhiteSpace(fieldValue)) {
                splitValues = fieldValue.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            }

            return splitValues;
        }
    }
}