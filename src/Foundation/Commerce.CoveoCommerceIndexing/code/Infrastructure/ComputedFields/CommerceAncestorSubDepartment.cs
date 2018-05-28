using System.Collections.Generic;
using Sitecore.Commerce.Connect.CommerceServer;
using Sitecore.Commerce.Connect.CommerceServer.Search;
using Sitecore.Commerce.Connect.CommerceServer.Search.ComputedFields;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Infrastructure.ComputedFields
{
    /// <remarks>This code is almost an exact copy of Sitecore.Commerce.Connect.CommerceServer.Search.ComputedFields.CommerceAncestorNames</remarks>
    public class CommerceAncestorSubDepartment : BaseAncestorField
    {
        public override object ComputeValue(IIndexable p_Indexable)
        {
            Assert.ArgumentNotNull((object) p_Indexable, "p_Indexable");
            List<string> stringList = new List<string>();
            Item validatedItem = this.GetValidatedItem(p_Indexable);
            if (validatedItem == null)
                return (object) stringList;
            List<ID> idList = new List<ID>();
            List<ID> ancestorsForItem = this.GetAncestorsForItem(validatedItem);
            idList.AddRange((IEnumerable<ID>) ancestorsForItem);
            List<ID> virtualAncestors = CommerceTypeLoader.CreateInstance<ICommerceSearchManager>().GetVirtualAncestors(validatedItem);
            idList.AddRange((IEnumerable<ID>) virtualAncestors);
            Database database = validatedItem.Database;
            if (idList.Count >= 2) {
                ID itemId = idList[1];
                Item obj = database.GetItem(itemId);
                if (obj != null) {
                    return obj.Name;
                }
            }
            return null;
        }
    }
}