﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Search.Facets;

namespace Orckestra.Composer.Search.Providers.SelectedFacet
{
    public class RangeSelectedFacetProvider : ISelectedFacetProvider
    {
        public RangeSelectedFacetProvider(IFacetLocalizationProvider facetLocalizationProvider)
        {
            if (facetLocalizationProvider == null)
            {
                throw new ArgumentNullException("facetLocalizationProvider");
            }

            FacetLocalizationProvider = facetLocalizationProvider;
        }

        protected IFacetLocalizationProvider FacetLocalizationProvider { get; private set; }

        /// <summary>
        ///     Gets the type of facet this provider is building.
        /// </summary>
        protected FacetType FacetType
        {
            get { return FacetType.Range; }
        }

        /// <summary>
        ///     Creates a new list of a <see cref="Facets.SelectedFacet" /> from a <see cref="filter" /> object.
        /// </summary>
        /// <param name="filter">Facet to create the facet predicate from.</param>
        /// <param name="setting">Settings of the facet</param>
        /// <param name="cultureInfo">Culture in which the display names will be returned in</param>
        public IEnumerable<Facets.SelectedFacet> CreateSelectedFacetList(SearchFilter filter, FacetSetting setting,
            CultureInfo cultureInfo)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }
            if (setting == null)
            {
                throw new ArgumentNullException("setting");
            }
            if (!setting.FieldName.Equals(filter.Name, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    string.Format(
                        "The specified setting is for the facet '{0}', whereas the filter is for the facet '{1}'",
                        setting.FieldName, filter.Name), "setting");
            }
            if (setting.FacetType != FacetType)
            {
                throw new ArgumentException(
                    string.Format("The facetResult is defined as '{0}' which does not match '{1}'", setting.FacetType,
                        FacetType), "setting");
            }

            var rangeValues = filter.Value.Split(SearchConfiguration.FacetRangeValueSplitter);
            var minValue = rangeValues[0];
            var maxValue = rangeValues.Length > 1 ? rangeValues[1] : null;

            return new List<Facets.SelectedFacet>
            {
                new Facets.SelectedFacet
                {
                    FieldName = filter.Name,
                    DisplayName = FacetLocalizationProvider.GetFormattedRangeFacetValues(filter.Name, minValue, maxValue, setting.FacetValueType, cultureInfo),
                    FacetType = FacetType,
                    IsRemovable = !filter.IsSystem,
                    MinimumValue = minValue,
                    MaximumValue = maxValue
                }
            };
        }
    }
}