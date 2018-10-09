﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Composite.Core.Routing;
using Composite.Data;
using Composite.Data.Types;
using Orckestra.Composer.CompositeC1.Pages;

namespace Orckestra.Composer.CompositeC1.Services
{
    public class PageService : IPageService
    {
        /// <summary>
        /// Returns localized URL
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public IPage GetPage(Guid pageId, CultureInfo cultureInfo = null)
        {
            using (DataConnection connection = new DataConnection(cultureInfo))
            {
                return connection.Get<IPage>().FirstOrDefault(p => p.Id == pageId);
            }
        }

        public Guid GetParentPageId(IPage page)
        {
            return page.GetParentId();
        }

        /// <summary>
        /// Returns localized URL. It was changed from original implementation to fix 6520: Previewing unpublished category pages is C1 console shows error in the preview, and on frontend once the page is published 
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public string GetRendererPageUrl(Guid pageId, CultureInfo cultureInfo = null)
        {
            if (cultureInfo == null)
            {
                cultureInfo = DataLocalizationFacade.DefaultLocalizationCulture;
            } 

            var pageUrlData = new PageUrlData(pageId, PublicationScope.Published, cultureInfo);

            return PageUrls.BuildUrl(pageUrlData, UrlKind.Renderer);
        }

        public string GetPageUrl(Guid pageId, CultureInfo cultureInfo = null)
        {
            var page = GetPage(pageId, cultureInfo);
            if (page == null)
            {
                return null;
            }

            return PageUrls.BuildUrl(page);
            
        }

        public string GetPageUrl(IPage page)
        {
            var url = PageUrls.BuildUrl(page);
            return url;
        }

        public List<CheckoutStepInfoPage> GetCheckoutStepPages(CultureInfo cultureInfo = null)
        {
            using (DataConnection connection = new DataConnection(cultureInfo))
            {
                return connection.Get<CheckoutStepInfoPage>().ToList();
            }
        }

        public CheckoutStepInfoPage GetCheckoutStepPage(Guid pageId, CultureInfo cultureInfo = null)
        {
            using (DataConnection connection = new DataConnection(cultureInfo))
            {
                return connection.Get<CheckoutStepInfoPage>().FirstOrDefault(p => p.PageId == pageId);
            }
        }
    }
}