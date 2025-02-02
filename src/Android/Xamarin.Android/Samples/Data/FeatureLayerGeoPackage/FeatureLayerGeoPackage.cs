// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using ArcGISRuntime.Samples.Managers;
using Esri.ArcGISRuntime.Data;

namespace ArcGISRuntime.Samples.FeatureLayerGeoPackage
{
    [Activity (ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	[ArcGISRuntime.Samples.Shared.Attributes.OfflineData("68ec42517cdd439e81b036210483e8e7")]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Feature layer (GeoPackage)",
        category: "Data",
        description: "Display features from a local GeoPackage.",
        instructions: "Pan and zoom around the map. View the data loaded from the geopackage.",
        tags: new[] { "OGC", "feature table", "geopackage", "gpkg", "package", "standards" })]
    public class FeatureLayerGeoPackage : Activity
    {
        private MapView _myMapView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Title = "Feature layer (GeoPackage)";

            // Create the UI, setup the control references and execute initialization
            CreateLayout();
            Initialize();
        }
        
        private async void Initialize()
        {
            // Create a new map centered on Aurora Colorado
            _myMapView.Map = new Map(BasemapStyle.ArcGISLightGray);
            _myMapView.Map.InitialViewpoint = new Viewpoint(39.7294, -104.8319, 9);

            // Get the full path
            string geoPackagePath = GetGeoPackagePath();

            try
            {
                // Open the GeoPackage
                GeoPackage myGeoPackage = await GeoPackage.OpenAsync(geoPackagePath);

                // Read the feature tables and get the first one
                FeatureTable geoPackageTable = myGeoPackage.GeoPackageFeatureTables.FirstOrDefault();

                // Make sure a feature table was found in the package
                if (geoPackageTable == null) { return; }

                // Create a layer to show the feature table
                FeatureLayer newLayer = new FeatureLayer(geoPackageTable);
                await newLayer.LoadAsync();

                // Add the feature table as a layer to the map (with default symbology)
                _myMapView.Map.OperationalLayers.Add(newLayer);
            }
            catch (Exception e)
            {
                new AlertDialog.Builder(this).SetMessage(e.ToString()).SetTitle("Error").Show();
            }
        }

        private static string GetGeoPackagePath()
        {
            return DataManager.GetDataFolder("68ec42517cdd439e81b036210483e8e7", "AuroraCO.gpkg");
        }

        private void CreateLayout()
        {
            // Create a new vertical layout for the app
            LinearLayout layout = new LinearLayout(this) { Orientation = Orientation.Vertical };

            // Add a map view to the layout
            _myMapView = new MapView(this);
            layout.AddView(_myMapView);

            // Show the layout in the app
            SetContentView(layout);
        }
    }
}