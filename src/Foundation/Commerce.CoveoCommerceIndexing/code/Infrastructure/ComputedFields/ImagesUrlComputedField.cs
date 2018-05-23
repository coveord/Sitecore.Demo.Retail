using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coveo.Framework.CNL;
using Coveo.Framework.Databases;
using Coveo.Framework.Items;
using Coveo.Framework.Log;
using Coveo.Framework.Utils;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Globalization;

namespace Sitecore.Foundation.Commerce.CoveoCommerceIndexing.Infrastructure.ComputedFields {
    public class ImagesUrlComputedField : IComputedIndexField
    {
        private static readonly ILogger s_Logger = CoveoLogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string[] s_ImagesSeperator = { IMAGES_FIELD_VALUE_SEPARATOR };
        private readonly IUrlUtilities m_UrlUtilities = UrlUtilitiesProvider.GetInstance();

        [ThreadStatic]
        private static IDatabaseWrapper s_Database;

        private const string IMAGES_FIELD_NAME = "Images";
        private const string IMAGES_FIELD_VALUE_SEPARATOR = "|";

        public string FieldName { get; set; }

        public string ReturnType
        {
            get
            {
                return "string";
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
            object value = GetItemImagesUrl(item);

            s_Logger.TraceExiting();
            return value;
        }

        private object GetItemImagesUrl(IItem p_Item)
        {
            Precondition.NotNull(p_Item, () => () => p_Item);

            s_Database = p_Item.Database;

            object imagesUrl = null;
            IEnumerable<string> imagesItemsIds = GetImagesFieldItemsId(p_Item);
            if (imagesItemsIds.Any()) {
                imagesUrl = GetImagesUrl(imagesItemsIds, p_Item.Language);
            }

            return imagesUrl;
        }

        private IEnumerable<string> GetImagesFieldItemsId(IItem p_Item)
        {
            IEnumerable<string> imageItemsId = new List<string>();
            string imagesFieldValue = p_Item.GetFieldValue(IMAGES_FIELD_NAME);

            if (!String.IsNullOrEmpty(imagesFieldValue)) {
                imageItemsId = imagesFieldValue.Split(s_ImagesSeperator, StringSplitOptions.RemoveEmptyEntries);
            }

            return imageItemsId;
        }

        private List<string> GetImagesUrl(IEnumerable<string> p_ImageItemsId, Language p_SourceItemLanguage)
        {
            List<string> imagesUrl = new List<string>();
            foreach (string imageItemId in p_ImageItemsId) {
                string imageUrl = GetImageUrl(imageItemId, p_SourceItemLanguage);

                if (!String.IsNullOrEmpty(imageUrl)) {
                    imagesUrl.Add(imageUrl);
                }
            }

            s_Logger.Info(imagesUrl.ToString());
            return imagesUrl.Any() ? imagesUrl : null;
        }

        private string GetImageUrl(string p_ImageItemId, Language p_SourceItemLanguage)
        {
            string imageUrl = null;

            IItem imageItem = ResolveReferencedItem(p_ImageItemId, p_SourceItemLanguage);
            if (imageItem != null) {
                imageUrl = m_UrlUtilities.GetMediaUrl(imageItem);
                imageUrl = imageUrl.Replace("/sitecore/shell", "");
            }

            return imageUrl;
        }

        private IItem ResolveReferencedItem(string p_Id, Language p_SourceItemLanguage)
        {
            IItem item = s_Database.GetItem(p_Id, p_SourceItemLanguage);
            if (item == null || !item.Versions.HasVersion()) {
                item = s_Database.GetItem(p_Id);
            }

            return item;
        }
    }
}