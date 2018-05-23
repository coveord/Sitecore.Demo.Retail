using System;
using System.Reflection;
using Coveo.Framework.CNL;
using Coveo.Framework.Items;
using Coveo.Framework.Log;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Infrastructure.ComputedFields {
    public class DecimalPriceComputedField : IComputedIndexField
    {
        private static readonly ILogger s_Logger = CoveoLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string PRICE_FIELD_NAME = "ListPrice";

        public string FieldName { get; set; }

        public string ReturnType
        {
            get
            {
                return "Number";
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
            object value = GetDecimalPrice(item);

            s_Logger.TraceExiting();
            return value;
        }

        private object GetDecimalPrice(IItem p_Item)
        {
            Precondition.NotNull(p_Item, () => () => p_Item);

            string itemPrice = p_Item.GetFieldValue(PRICE_FIELD_NAME);
            object decimalPrice = ConvertToDecimal(itemPrice);

            return decimalPrice;
        }

        private decimal ConvertToDecimal(string p_Price) {
            decimal decimalPrice;
            decimal.TryParse(p_Price, out decimalPrice);
            decimalPrice = Decimal.Round(decimalPrice, 2);

            s_Logger.Debug("Decimal price: " + decimalPrice);
            return decimalPrice;
        }
    }
}