// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using System;
using ArcGISRuntime.Samples.Managers;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Windows.UI.Popups;

namespace ArcGISRuntime.UWP.Samples.ReadGeoPackage
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Read GeoPackage",
        category: "Data",
        description: "Add rasters and feature tables from a GeoPackage to a map.",
        instructions: "When the sample loads, the feature tables and rasters from the GeoPackage will be shown on the map.",
        tags: new[] { "GeoPackage", "OGC", "container", "layer", "map", "package", "raster", "table" })]
    [ArcGISRuntime.Samples.Shared.Attributes.OfflineData("68ec42517cdd439e81b036210483e8e7")]
    public partial class ReadGeoPackage
    {
        public ReadGeoPackage()
        {
            InitializeComponent();
            Initialize();
        }

        private async void Initialize()
        {
            // Create a new map centered on Aurora Colorado.
            MyMapView.Map = new Map(BasemapStyle.ArcGISStreets);
            MyMapView.Map.InitialViewpoint = new Viewpoint(39.7294, -104.70, 11);

            // Get the full path to the GeoPackage on the device.
            string myGeoPackagePath = DataManager.GetDataFolder("68ec42517cdd439e81b036210483e8e7", "AuroraCO.gpkg");

            try
            {
                // Open the GeoPackage.
                GeoPackage myGeoPackage = await GeoPackage.OpenAsync(myGeoPackagePath);

                // Loop through each GeoPackageRaster.
                foreach (GeoPackageRaster oneGeoPackageRaster in myGeoPackage.GeoPackageRasters)
                {
                    // Create a RasterLayer from the GeoPackageRaster.
                    RasterLayer myRasterLayer = new RasterLayer(oneGeoPackageRaster)
                    {
                        // Set the opacity on the RasterLayer to partially visible.
                        Opacity = 0.55
                    };

                    // Add the raster layer to the map.
                    MyMapView.Map.OperationalLayers.Add(myRasterLayer);
                }

                // Loop through each GeoPackageFeatureTable.
                foreach (GeoPackageFeatureTable oneGeoPackageFeatureTable in myGeoPackage.GeoPackageFeatureTables)
                {
                    // Create a FeatureLayer from the GeoPackageFeatureLayer.
                    FeatureLayer myFeatureLayer = new FeatureLayer(oneGeoPackageFeatureTable);

                    // Add the layer to the map.
                    MyMapView.Map.OperationalLayers.Add(myFeatureLayer);
                }
            }
            catch (Exception e)
            {
                await new MessageDialog(e.ToString(), "Error").ShowAsync();
            }
        }
    }
}