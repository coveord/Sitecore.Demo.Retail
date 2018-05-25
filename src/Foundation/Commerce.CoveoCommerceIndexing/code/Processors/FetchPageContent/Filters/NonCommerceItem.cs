using System.Reflection;
using Coveo.AbstractLayer.FieldManagement;
using Coveo.AbstractLayer.RepositoryItem;
using Coveo.Framework.CNL;
using Coveo.Framework.Log;
using Coveo.SearchProvider.Processors.FetchPageContent.Filters;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Processors.FetchPageContent.Filters
{
    public class NonCommerceItem : IFetchPageContentInboundFilterProcessor
    {
        private static readonly ILogger s_Logger = CoveoLogManager.GetLogger(MethodBase.GetCurrentMethod()
                                                                                       .DeclaringType);

        /// <inheritDoc />
        public void Process(FetchPageContentRequestInboundFilterArgs p_Args)
        {
            Precondition.NotNull(p_Args, () => () => p_Args);
            Precondition.NotNull(p_Args.Item, () => () => p_Args.Item);

            if (p_Args.ShouldProcessItem) {
                p_Args.ShouldProcessItem = !IsCommerceItem(p_Args.Item);
            }
        }

        private bool IsCommerceItem(IReadOnlyCoveoIndexableItem p_Item)
        {
            Precondition.NotNull(p_Item, () => () => p_Item);
            Precondition.NotNull(p_Item.Metadata, () => () => p_Item.Metadata);

            object itemFullPath;
            return p_Item.Metadata.TryGetValue(MetadataNames.s_FullPath, out itemFullPath) && ((string) itemFullPath).ToLower().StartsWith("/sitecore/commerce");
        }
    }
}