using System;
using System.Reflection;
using Coveo.Framework.CNL;
using Coveo.Framework.Items;
using Coveo.Framework.Log;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Infrastructure.ComputedFields {
    public class IntegerRatingComputedField : IComputedIndexField
    {
        private static readonly ILogger s_Logger = CoveoLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string FieldName { get; set; }

        public string ReturnType
        {
            get
            {
                return "Integer";
            }
            set
            {
            }
        }

        public object ComputeFieldValue(IIndexable p_Indexable)
        {
            s_Logger.TraceEntering();
            Precondition.NotNull(p_Indexable, () => () => p_Indexable);

            IItem item = new ItemWrapper(new IndexableWrapper(p_Indexable));
            object value = GetIntegerRating(item);

            s_Logger.TraceExiting();
            return value;
        }

        private object GetIntegerRating(IItem p_Item)
        {
            Precondition.NotNull(p_Item, () => () => p_Item);

            string itemRating = p_Item.GetFieldValue(Templates.Commerce.Product.FieldNames.Rating);
            object integerRating = ConvertToInteger(itemRating);

            return integerRating;
        }

        private int ConvertToInteger(string p_Rating) {
            int intRating;
            Int32.TryParse(p_Rating, out intRating);

            s_Logger.Debug("Integer rating: " + intRating);
            return intRating;
        }
    }
}