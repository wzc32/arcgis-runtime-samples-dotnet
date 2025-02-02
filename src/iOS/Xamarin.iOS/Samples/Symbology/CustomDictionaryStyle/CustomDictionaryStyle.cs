﻿// Copyright 2019 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGISRuntime;
using ArcGISRuntime.Samples.Managers;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using System;
using UIKit;

namespace ArcGISRuntimeXamarin.Samples.CustomDictionaryStyle
{
    [Register("CustomDictionaryStyle")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Custom dictionary style",
        category: "Symbology",
        description: "Use a custom dictionary style (.stylx) to symbolize features using a variety of attribute values.",
        instructions: "Pan and zoom the map to see the symbology from the custom dictionary style.",
        tags: new[] { "dictionary", "military", "renderer", "style", "stylx", "unique value", "visualization" })]
    [ArcGISRuntime.Samples.Shared.Attributes.OfflineData("751138a2e0844e06853522d54103222a")]
    public class CustomDictionaryStyle : UIViewController
    {
        // Hold references to UI controls.
        private MapView _myMapView;

        // Path for the restaurants style file.
        private readonly string _stylxPath = DataManager.GetDataFolder("751138a2e0844e06853522d54103222a", "Restaurant.stylx");

        // Uri for the restaurants feature service.
        private readonly Uri _restaurantUri = new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/arcgis/rest/services/Redlands_Restaurants/FeatureServer/0");

        public CustomDictionaryStyle()
        {
            Title = "Custom dictionary style";
        }

        private async void Initialize()
        {
            try
            {
                // Create a new map with a streets basemap.
                Map map = new Map(BasemapStyle.ArcGISStreets);

                // Create the restaurants layer and add it to the map.
                FeatureLayer restaurantLayer = new FeatureLayer(_restaurantUri);
                map.OperationalLayers.Add(restaurantLayer);

                // Load the feature table for the restaurants layer.
                await restaurantLayer.FeatureTable.LoadAsync();

                // Open the custom style file.
                DictionarySymbolStyle restaurantStyle = await DictionarySymbolStyle.CreateFromFileAsync(_stylxPath);

                // Create the dictionary renderer with the style file and the field overrides.
                DictionaryRenderer dictRenderer = new DictionaryRenderer(restaurantStyle);

                // Set the restaurant layer renderer to the dictionary renderer.
                restaurantLayer.Renderer = dictRenderer;

                // Set the map's initial extent to that of the restaurants.
                map.InitialViewpoint = new Viewpoint(restaurantLayer.FullExtent);

                // Set the map to the map view.
                _myMapView.Map = map;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override void LoadView()
        {
            // Create the views.
            View = new UIView() { BackgroundColor = ApplicationTheme.BackgroundColor };

            _myMapView = new MapView();
            _myMapView.TranslatesAutoresizingMaskIntoConstraints = false;

            // Add the views.
            View.AddSubviews(_myMapView);

            // Lay out the views.
            NSLayoutConstraint.ActivateConstraints(new[]{
                _myMapView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _myMapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                _myMapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _myMapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor)
            });
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            Initialize();
        }
    }
}