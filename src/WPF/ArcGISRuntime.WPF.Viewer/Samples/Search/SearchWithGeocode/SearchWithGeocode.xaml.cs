﻿// Copyright 2022 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the Licenase.

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Offline;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using ArcGISRuntime.Samples.Managers;
using System.Threading;
using System.Reflection;

namespace ArcGISRuntime.WPF.Samples.SearchWithGeocode
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Search with geocode",
        category: "Search",
        description: "Find the location for an address, or places of interest near a location or within a specific area.",
        instructions: "Choose an address from the suggestions or submit your own address to show its location on the map in a callout. Tap on a result pin to display its address. If you pan away from the result area, a \"Repeat Search Here\" button will appear. Tap it to query again for the currently viewed area on the map.",
        tags: new[] { "POI", "address", "businesses", "geocode", "locations", "locator", "places of interest", "point of interest", "search", "suggestions", "toolkit" })]
    [ArcGISRuntime.Samples.Shared.Attributes.OfflineData("3424d442ebe54f3cbf34462382d3aebe")]
    public partial class SearchWithGeocode
    {
        private LocatorTask _sanDiegoStreetsTask;

        public SearchWithGeocode()
        {
            InitializeComponent();
            _ = Initialize();
        }

        private async Task Initialize()
        {
            MyMapView.Map = new Map(BasemapStyle.ArcGISImagery);

            await MyMapView.SetViewpointAsync(new Viewpoint(34.058, -117.195, 5e4));

            var searchResultsOverlay = new GraphicsOverlay();

            MyMapView.GraphicsOverlays.Add(searchResultsOverlay);

            // Edit default source.
            LocatorSearchSource defaultSearchSource = MySearchView.SearchViewModel.Sources.First() as LocatorSearchSource;
            defaultSearchSource.MaximumResults = 10;

            // Setting this value to 5, lower than the default value of 6, breaks the search extent functionality.
            //defaultSearchSource.MaximumSuggestions = 5;

            // Create custom implementation of default source.
            CustomSearchSource customSource = await CustomSearchSource.CreateDefaultSourceAsync();
            customSource.MaximumResults = 4;
            customSource.MaximumSuggestions = 3;
            customSource.DisplayName = "Custom World Geocoder";

            // Get the path to the offline data.
            string sanDiegoStreetsPath = DataManager.GetDataFolder("3424d442ebe54f3cbf34462382d3aebe", "SanDiego_StreetAddress.loc");

            // Load the geocoder.
            _sanDiegoStreetsTask = await LocatorTask.CreateAsync(new Uri(sanDiegoStreetsPath));
            LocatorSearchSource offlineDataSource = new LocatorSearchSource(_sanDiegoStreetsTask);
            offlineDataSource.DisplayName = "Offline Data";
            offlineDataSource.MaximumResults = 10;
            offlineDataSource.MaximumSuggestions = 5;

            // Add the custom sources to the search component view model.
            MySearchView.SearchViewModel.Sources.Add(offlineDataSource);
            MySearchView.SearchViewModel.Sources.Add(customSource);
        }

        private void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            _ = GeoViewTappedTask(e);
        }

        private async Task GeoViewTappedTask(GeoViewInputEventArgs e)
        {
            try
            {
                // Search for the graphics underneath the user's tap.
                IReadOnlyList<IdentifyGraphicsOverlayResult> results = await MyMapView.IdentifyGraphicsOverlaysAsync(e.Position, 12, false);

                // Return gracefully and dismiss any existing callout if there was no result.
                if (results.Count < 1 || results.First().Graphics.Count < 1)
                {
                    MyMapView.DismissCallout();

                    return;
                }

                // Get the first tapped graphic from the graphics overlay result.
                Graphic pinGraphic = results.First().Graphics.First();

                // The SearchView toolkit component default search adds attributes to the graphics that are generated by a search result. If these are present the callout place name and address can be taken obtained here.
                if (pinGraphic.Attributes.Count > 0)
                {
                    // Use the address of the user tapped location graphic.
                    // To get the fully geocoded address use "Place_addr".
                    string calloutAddress = pinGraphic.Attributes["Place_addr"].ToString() != string.Empty ? pinGraphic.Attributes["Place_addr"].ToString() : "Unknown Address";
                    string calloutPlaceName = pinGraphic.Attributes["PlaceName"].ToString() != string.Empty ? pinGraphic.Attributes["PlaceName"].ToString() : "Unknown Place Name";

                    // Define the callout title and detail.
                    CalloutDefinition calloutBody = new CalloutDefinition(calloutPlaceName, calloutAddress);

                    // Show the callout on the map at the tapped location
                    MyMapView.ShowCalloutForGeoElement(pinGraphic, e.Position, calloutBody);
                }
                else
                {
                    // Reverse geocode to get addresses.
                    ReverseGeocodeParameters parameters = new ReverseGeocodeParameters();
                    parameters.ResultAttributeNames.Add("*");
                    parameters.MaxResults = 1;
                    IReadOnlyList<GeocodeResult> addresses = await _sanDiegoStreetsTask.ReverseGeocodeAsync(e.Location, parameters);

                    // Skip if there are no results.
                    if (!addresses.Any())
                    {
                        MessageBox.Show("No results found.", "No results");
                        return;
                    }

                    // Get the first result.
                    GeocodeResult address = addresses.First();

                    // Use the address as the callout title.
                    string calloutTitle = address.Attributes["StAddr"].ToString();
                    string calloutDetail = address.Attributes["City"].ToString() + ", " + address.Attributes["RegionAbbr"] + " " + address.Attributes["Postal"];

                    // Define the callout.
                    CalloutDefinition calloutBody = new CalloutDefinition(calloutTitle, calloutDetail);

                    // Show the callout on the map at the tapped location.
                    MyMapView.ShowCalloutForGeoElement(pinGraphic, e.Position, calloutBody);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }
    }

    // This class is a clone of LocatorSearchSource used for local debugging.
    public class CustomSearchSource : ISearchSource
    {
        internal const string WorldGeocoderUriString = "https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer";

        private static LocatorTask _worldGeocoderTask;

        /// <summary>
        /// Creates a <see cref="LocatorSearchSource"/> configured for use with the Esri World Geocoder service.
        /// </summary>
        /// <param name="cancellationToken">Token used for cancellation.</param>
        /// <remarks>This method will re-use a static LocatorTask instance to improve performance.</remarks>
        public static async Task<CustomSearchSource> CreateDefaultSourceAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_worldGeocoderTask == null)
            {
                _worldGeocoderTask = new LocatorTask(new Uri(WorldGeocoderUriString));
                await _worldGeocoderTask.LoadAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new CustomWorldGeocoderSearchSource(_worldGeocoderTask, null);
        }

        private readonly Task _loadTask;

        /// <summary>
        /// Gets the task used to perform initial locator setup.
        /// </summary>
        protected Task LoadTask => _loadTask;

        /// <summary>
        /// Gets or sets the name of the locator. Defaults to the locator's name, or "locator" if not set.
        /// </summary>
        public string DisplayName { get; set; } = "Locator";

        /// <summary>
        /// Gets or sets the maximum number of results to return for a search. Default is 6.
        /// </summary>
        public int MaximumResults { get; set; } = 6;

        /// <summary>
        /// Gets or sets the maximum number of suggestions to return. Default is 6.
        /// </summary>
        public int MaximumSuggestions { get; set; } = 6;

        /// <summary>
        /// Gets the geocode parameters, which can be used to configure search behavior.
        /// </summary>
        /// <remarks>
        /// <see cref="GeocodeParameters.MaxResults"/>, <see cref="GeocodeParameters.PreferredSearchLocation"/>,
        /// and <see cref="GeocodeParameters.SearchArea"/> are set by <see cref="LocatorSearchSource"/> automatically on search.
        /// <see cref="GeocodeParameters.Categories"/> is set to <c>"*"</c> when the <see cref="Locator"/> is loaded for the first time.
        /// </remarks>
        public GeocodeParameters GeocodeParameters { get; } = new GeocodeParameters();

        /// <summary>
        /// Gets the suggestion parameters, which can be used to configure suggestion behavior.
        /// </summary>
        /// <remarks>
        /// <see cref="SuggestParameters.MaxResults"/> and <see cref="SuggestParameters.PreferredSearchLocation"/> are
        /// set automatically on search.
        /// </remarks>
        public SuggestParameters SuggestParameters { get; } = new SuggestParameters();

        /// <summary>
        /// Gets the underlying locator.
        /// </summary>
        public LocatorTask Locator { get; }

        /// <summary>
        /// Gets or sets the placeholder to show when this search source is selected for use.
        /// </summary>
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the default callout definition to use with results.
        /// </summary>
        public CalloutDefinition DefaultCalloutDefinition { get; set; }

        /// <summary>
        /// Gets or sets the default symbol to use when displaying results.
        /// </summary>
        public Symbol DefaultSymbol { get; set; } = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 2);

        /// <inheritdoc />
        public double DefaultZoomScale { get; set; } = 100_000;

        /// <inheritdoc />
        public Esri.ArcGISRuntime.Geometry.Geometry SearchArea { get; set; }

        /// <inheritdoc />
        public MapPoint PreferredSearchLocation { get; set; }

        /// <summary>
        /// Gets or sets the attribute key to use as the subtitle when returning results. Key must be included in <see cref="GeocodeParameters.ResultAttributeNames"/>.
        /// </summary>
        public string SubtitleAttributeKey { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocatorSearchSource"/> class.
        /// <seealso cref="CreateDefaultSourceAsync(CancellationToken)"/> to create a source configured for use with the Esri World Geocoder.
        /// </summary>
        /// <param name="locator">Locator to be used.</param>
        public CustomSearchSource(LocatorTask locator)
        {
            Locator = locator;

            _loadTask = EnsureLoaded();
        }

        private async Task EnsureLoaded()
        {
            await Locator.LoadAsync();

            Stream resourceStream = Assembly.GetAssembly(typeof(LocatorSearchSource))?.GetManifestResourceStream(
                "Esri.ArcGISRuntime.Toolkit.EmbeddedResources.pin_red.png");

            if (resourceStream != null)
            {
                PictureMarkerSymbol pinSymbol = await PictureMarkerSymbol.CreateAsync(resourceStream);
                pinSymbol.Width = 33;
                pinSymbol.Height = 33;
                pinSymbol.LeaderOffsetX = 16.5;
                pinSymbol.OffsetY = 16.5;
                DefaultSymbol = pinSymbol;
            }

            if (DisplayName != Locator?.LocatorInfo?.Name && !string.IsNullOrWhiteSpace(Locator?.LocatorInfo?.Name))
            {
                DisplayName = Locator?.LocatorInfo?.Name ?? "Locator";
            }

            GeocodeParameters.ResultAttributeNames.Add("*");
        }

        /// <summary>
        /// This search source does not track selection state.
        /// </summary>
        public virtual void NotifySelected(SearchResult result)
        {
            // This space intentionally left blank.
        }

        /// <summary>
        /// This search source does not track selection state.
        /// </summary>
        public virtual void NotifyDeselected(SearchResult result)
        {
            // This space intentionally left blank.
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await _loadTask;

            cancellationToken.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = PreferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;

            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return SuggestionToSearchSuggestion(results);
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken = default)
        {
            await _loadTask;

            cancellationToken.ThrowIfCancellationRequested();

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return ResultToSearchResult(results);
        }

        /// <inheritdoc/>
        public virtual async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await _loadTask;

            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return ResultToSearchResult(results);
        }

        /// <inheritdoc />
        public virtual async Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Envelope queryExtent, CancellationToken cancellationToken = default)
        {
            await _loadTask;

            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.SearchArea = queryExtent;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return ResultToSearchResult(results);
        }

        /// <summary>
        /// Converts geocode result list into list of results, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchResult> ResultToSearchResult(IReadOnlyList<GeocodeResult> input)
        {
            var results = input.Select(i => GeocodeResultToSearchResult(i));

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// Converts suggest result list into list of suggestions, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchSuggestion> SuggestionToSearchSuggestion(IReadOnlyList<SuggestResult> input)
        {
            var results = input.Select(i => SuggestResultToSearchSuggestion(i));

            return results.Take(MaximumResults).ToList();
        }

        /// <summary>
        /// Creates a basic search result for the given geocode result.
        /// </summary>
        private SearchResult GeocodeResultToSearchResult(GeocodeResult r)
        {
            string subtitle = null;
            if (SubtitleAttributeKey != null && r.Attributes.ContainsKey(SubtitleAttributeKey))
            {
                subtitle = r.Attributes[SubtitleAttributeKey]?.ToString();
            }

            Esri.ArcGISRuntime.Mapping.Viewpoint selectionViewpoint = r.Extent == null ? null : new Esri.ArcGISRuntime.Mapping.Viewpoint(r.Extent);
            return new SearchResult(r.Label, subtitle, this, new Graphic(r.DisplayLocation, r.Attributes, DefaultSymbol), selectionViewpoint) { CalloutDefinition = DefaultCalloutDefinition };
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection, UnderlyingObject = r };
        }
    }

    // This class is a clone of WorldGeocoderSearchSource used for local debugging.
    internal class CustomWorldGeocoderSearchSource : CustomSearchSource
    {
        private const string AddressAttributeKey = "Place_addr";

        // Attribute used to identify the type of result coming from the locaotr.
        private const string LocatorIconAttributeKey = "Type";

        private readonly Task _additionalLoadTask;

        /// <summary>
        /// Gets or sets the minimum number of results to attempt to return.
        /// If there are too few results, the search is repeated with loosened parameters until enough results are accumulated.
        /// </summary>
        /// <remarks>
        /// If no search is successful, it is still possible to have a total number of results less than this threshold.
        /// Does not apply to repeated search with area constraint.
        /// Set to zero to disable search repeat behavior. Defaults to 1.
        /// </remarks>
        public int RepeatSearchResultThreshold { get; set; } = 0;

        /// <summary>
        /// Gets or sets the minimum number of suggestions to attempt to return.
        /// If there are too few suggestions, request is repeated with loosened constraints until enough suggestions are accumulated.
        /// </summary>
        /// <remarks>
        /// If no search is successful, it is still possible to have a total number of results less than this threshold.
        /// Does not apply to repeated search with area constraint.
        /// Set to zero to disable search repeat behavior. Defaults to 6.
        /// </remarks>
        public int RepeatSuggestResultThreshold { get; set; } = 6;

        /// <summary>
        /// Gets or sets the web style used to find symbols for results.
        /// When set, symbols are found for results based on the result's `Type` field, if available.
        /// </summary>
        /// <remarks>
        /// Defaults to the style identified by the name "Esri2DPointSymbolsStyle".
        /// The default Esri 2D point symbol has good results for many of the types returned by the world geocode service.
        /// You can use this property to customize result icons by publishing a web style, taking care to ensure that symbol keys match the `Type` attribute returned by the locator.
        /// </remarks>
        public SymbolStyle ResultSymbolStyle { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldGeocoderSearchSource"/> class.
        /// </summary>
        /// <param name="locator">Locator to use.</param>
        /// <param name="style">Symbol style to use to find results.</param>
        public CustomWorldGeocoderSearchSource(LocatorTask locator, SymbolStyle style)
            : base(locator)
        {
            SubtitleAttributeKey = AddressAttributeKey;
            if (style != null)
            {
                ResultSymbolStyle = style;
            }

            _additionalLoadTask = EnsureLoaded();
        }

        private async Task EnsureLoaded()
        {
            await LoadTask;

            if (Locator.LocatorInfo is LocatorInfo info)
            {
                // Locators from online services have descriptions but not names.
                if (!string.IsNullOrWhiteSpace(info.Name) && info.Name != Locator.Uri?.ToString())
                {
                    DisplayName = info.Name;
                }
                else if (!string.IsNullOrWhiteSpace(info.Description))
                {
                    DisplayName = info.Description;
                }
            }

            // Add attributes expected from the World Geocoder Service if present, otherwise default to all attributes.
            if (Locator.Uri?.ToString() == WorldGeocoderUriString &&
                (Locator.LocatorInfo?.ResultAttributes?.Any() ?? false))
            {
                var desiredAttributes = new[] { AddressAttributeKey, LocatorIconAttributeKey };
                foreach (var attr in desiredAttributes.OfType<string>())
                {
                    if (Locator.LocatorInfo.ResultAttributes.Where(at => at.Name == attr).Any())
                    {
                        GeocodeParameters.ResultAttributeNames.Add(attr);
                    }
                }
            }
            else
            {
                GeocodeParameters.ResultAttributeNames.Add("*");
            }
        }

        /// <summary>
        /// Converts suggest result list into list of suggestions, applying result limits and calling necessary callbacks.
        /// </summary>
        private IList<SearchSuggestion> SuggestionToSearchSuggestion(IReadOnlyList<SuggestResult> input)
        {
            var results = input.Select(i => SuggestResultToSearchSuggestion(i));

            return results.Take(MaximumResults).ToList();
        }

        /// <inheritdoc />
        public override async Task<IList<SearchResult>> SearchAsync(SearchSuggestion suggestion, CancellationToken cancellationToken = default)
        {
            await _additionalLoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            var tempParams = new GeocodeParameters();
            foreach (var attribute in GeocodeParameters.ResultAttributeNames)
            {
                tempParams.ResultAttributeNames.Add(attribute);
            }

            var results = await Locator.GeocodeAsync(suggestion.UnderlyingObject as SuggestResult, tempParams, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return await ResultToSearchResult(results);
        }

        /// <summary>
        /// Creates a basic search suggestion for the given suggest result.
        /// </summary>
        private SearchSuggestion SuggestResultToSearchSuggestion(SuggestResult r)
        {
            return new SearchSuggestion(r.Label, this) { IsCollection = r.IsCollection, UnderlyingObject = r };
        }

        /// <inheritdoc/>
        public override async Task<IList<SearchSuggestion>> SuggestAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await _additionalLoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            SuggestParameters.PreferredSearchLocation = PreferredSearchLocation;
            SuggestParameters.MaxResults = MaximumSuggestions;
            SuggestParameters.SearchArea = SearchArea;

            var results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (RepeatSuggestResultThreshold > 0 && results.Count < RepeatSuggestResultThreshold)
            {
                SuggestParameters.SearchArea = null;
                results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (RepeatSuggestResultThreshold > 0 && results.Count < RepeatSuggestResultThreshold)
            {
                SuggestParameters.PreferredSearchLocation = null;
                results = await Locator.SuggestAsync(queryString, SuggestParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return SuggestionToSearchSuggestion(results);
        }

        /// <inheritdoc/>
        public override async Task<IList<SearchResult>> SearchAsync(string queryString, CancellationToken cancellationToken = default)
        {
            await _additionalLoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.PreferredSearchLocation = PreferredSearchLocation;
            GeocodeParameters.SearchArea = SearchArea;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (RepeatSearchResultThreshold > 0 && results.Count < RepeatSearchResultThreshold)
            {
                GeocodeParameters.SearchArea = null;
                results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (RepeatSearchResultThreshold > 0 && results.Count < RepeatSearchResultThreshold)
            {
                GeocodeParameters.PreferredSearchLocation = null;
                results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await ResultToSearchResult(results);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public override async Task<IList<SearchResult>> RepeatSearchAsync(string queryString, Esri.ArcGISRuntime.Geometry.Envelope queryArea, CancellationToken cancellationToken = default)
        {
            await _additionalLoadTask;
            cancellationToken.ThrowIfCancellationRequested();

            // Reset spatial parameters
            GeocodeParameters.SearchArea = queryArea;
            GeocodeParameters.MaxResults = MaximumResults;

            var results = await Locator.GeocodeAsync(queryString, GeocodeParameters, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return await ResultToSearchResult(results);
        }

        private async Task<SearchResult> GeocodeResultToSearchResult(GeocodeResult r)
        {
            var symbol = await SymbolForResult(r);
            string subtitle = null;
            if (SubtitleAttributeKey != null && r.Attributes.ContainsKey(SubtitleAttributeKey) && r.Attributes[SubtitleAttributeKey] is string subtitleString)
            {
                subtitle = subtitleString;
            }

            var viewpoint = r.Extent == null ? null : new Esri.ArcGISRuntime.Mapping.Viewpoint(r.Extent);

            var graphic = new Graphic(r.DisplayLocation, r.Attributes, symbol);

            CalloutDefinition callout = new CalloutDefinition(graphic) { Text = r.Label, DetailText = subtitle };

            return new SearchResult(r.Label, subtitle, this, graphic, viewpoint) { CalloutDefinition = callout };
        }

        private async Task<Symbol> SymbolForResult(GeocodeResult r)
        {
            if (ResultSymbolStyle != null && r.Attributes.ContainsKey(LocatorIconAttributeKey) && r.Attributes[LocatorIconAttributeKey] is string typeAttrs)
            {
                if (Locator.Uri?.ToString() == WorldGeocoderUriString && ResultSymbolStyle.StyleName == "Esri2DPointSymbolsStyle")
                {
                    var firstResult = await ResultSymbolStyle.GetSymbolAsync(new[] { typeAttrs.ToString().Replace(' ', '-').ToLower() });
                    if (firstResult != null)
                    {
                        return firstResult;
                    }
                }

                var symbParams = new SymbolStyleSearchParameters();
                symbParams.Names.Add(typeAttrs.ToString());
                symbParams.NamesStrictlyMatch = false;
                var symbolResult = await ResultSymbolStyle.SearchSymbolsAsync(symbParams);

                if (symbolResult.Any())
                {
                    return await symbolResult.First().GetSymbolAsync();
                }
            }

            return DefaultSymbol;
        }

        /// <summary>
        /// Converts geocode result list into list of results, applying result limits and calling necessary callbacks.
        /// </summary>
        private async Task<IList<SearchResult>> ResultToSearchResult(IReadOnlyList<GeocodeResult> input)
        {
            IEnumerable<SearchResult> results = await Task.WhenAll(input.Select(i => GeocodeResultToSearchResult(i)));

            return results.Take(MaximumResults).ToList();
        }
    }
}