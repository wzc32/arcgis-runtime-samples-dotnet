// Copyright 2017 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using Esri.ArcGISRuntime.Data;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace ArcGISRuntime.WinUI.Samples.StatsQueryGroupAndSort
{
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        name: "Statistical query group and sort",
        category: "Data",
        description: "Query a feature table for statistics, grouping and sorting by different fields.",
        instructions: "The sample will start with some default options selected. You can immediately click the \"Get Statistics\" button to see the results for these options. There are several ways to customize your queries:",
        tags: new[] { "correlation", "data", "fields", "filter", "group", "sort", "statistics", "table" })]
    public partial class StatsQueryGroupAndSort
    {
        // URI for the US states map service
        private Uri _usStatesServiceUri = new Uri("https://services.arcgis.com/jIL9msH9OI208GCb/arcgis/rest/services/Counties_Obesity_Inactivity_Diabetes_2013/FeatureServer/0");

        // US states feature table
        private FeatureTable _usStatesTable;

        // Collection of (user-defined) statistics to use in the query
        private ObservableCollection<StatisticDefinition> _statDefinitions = new ObservableCollection<StatisticDefinition>();

        // Selected fields for grouping results
        private List<string> _groupByFields = new List<string>();

        // Collection to hold fields to order results by
        private ObservableCollection<OrderFieldOption> _orderByFields = new ObservableCollection<OrderFieldOption>();

        public StatsQueryGroupAndSort()
        {
            InitializeComponent();

            // Initialize the US states feature table and populate UI controls
            _ = Initialize();
        }

        private async Task Initialize()
        {
            // Create the US states feature table
            _usStatesTable = new ServiceFeatureTable(_usStatesServiceUri);

            try
            {
                // Load the table
                await _usStatesTable.LoadAsync();

                // Fill the fields combo and "group by" list with fields from the table
                FieldsComboBox.ItemsSource = _usStatesTable.Fields;
                GroupFieldsListBox.ItemsSource = _usStatesTable.Fields;

                // Set the (initially empty) collection of fields as the "order by" fields list data source
                OrderByFieldsListBox.ItemsSource = _orderByFields;

                // Fill the statistics type combo with values from the StatisticType enum
                StatTypeComboBox.ItemsSource = Enum.GetValues(typeof(StatisticType)).OfType<StatisticType>().Select(e => e.ToString());

                // Set the (initially empty) collection of statistic definitions as the statistics list box data source
                StatFieldsListBox.ItemsSource = _statDefinitions;
            }
            catch (Exception e)
            {
                await new MessageDialog2(e.ToString(), "Error").ShowAsync();
            }
        }

        // Execute a statistical query using the parameters defined by the user and display the results
        private async void OnExecuteStatisticsQueryClicked(object sender, RoutedEventArgs e)
        {
            // Verify that there is at least one statistic definition
            if (_statDefinitions.Count == 0)
            {
                ShowMessage("Please define at least one statistic for the query.", "Statistical Query");
                return;
            }

            // Create the statistics query parameters, pass in the list of statistic definitions
            StatisticsQueryParameters statQueryParams = new StatisticsQueryParameters(_statDefinitions);

            // Specify the group fields (if any)
            foreach (string groupField in _groupByFields)
            {
                statQueryParams.GroupByFieldNames.Add(groupField);
            }

            // Specify the fields to order by (if any)
            foreach (OrderFieldOption orderBy in _orderByFields)
            {
                statQueryParams.OrderByFields.Add(orderBy.OrderInfo);
            }

            // Ignore counties with missing data
            statQueryParams.WhereClause = "\"State\" IS NOT NULL";

            try
            {
                // Execute the statistical query with these parameters and await the results
                StatisticsQueryResult statQueryResult = await _usStatesTable.QueryStatisticsAsync(statQueryParams);

                // Format the output for display of grouped results in the list view
                IEnumerable<IGrouping<string,IReadOnlyDictionary<string,object>>> groupedResults = statQueryResult.GroupBy(r => string.Join(", ", r.Group.Values), r => r.Statistics);

                // Apply the results to the list view data source
                GroupedResultData.Source = groupedResults;
            }
            catch (Exception ex)
            {
                await new MessageDialog2(ex.Message, "Error").ShowAsync();
            }
        }
        
        // Helper function to show a message
        private void ShowMessage(string message, string title)
        {
            _ = new MessageDialog2(message, title).ShowAsync();
        }

        // Handle when the check box for a "group by" field is checked on or off by adding or removing the field from the collection
        private void GroupFieldCheckChanged(object sender, RoutedEventArgs e)
        {
            // Get the check box that raised the event (group field)
            CheckBox groupFieldCheckBox = (CheckBox)sender;

            // Get the field name
            string fieldName = groupFieldCheckBox.Content.ToString();

            // See if the field is being added or removed from the "group by" list
            bool fieldAdded = groupFieldCheckBox.IsChecked == true;

            // See if the field already exists in the "group by" list
            bool fieldIsInList = _groupByFields.Contains(fieldName);

            // If the field is being added, and is NOT in the list, add it ...
            if (fieldAdded && !fieldIsInList)
            {
                _groupByFields.Add(fieldName);

                // Also add it to the "order by" list
                OrderBy orderBy = new OrderBy(fieldName, SortOrder.Ascending);
                OrderFieldOption orderOption = new OrderFieldOption(false, orderBy);
                _orderByFields.Add(orderOption);
            }
            // If the field is being removed and it IS in the list, remove it ...
            else if (!fieldAdded && fieldIsInList)
            {
                _groupByFields.Remove(fieldName);

                // Also check for this field in the "order by" list and remove if necessary (only group fields can be used to order results)
                OrderFieldOption orderBy = _orderByFields.FirstOrDefault(field => field.OrderInfo.FieldName == fieldName);
                if (orderBy != null)
                {
                    // Remove the field from the "order by" list
                    _orderByFields.Remove(orderBy);
                }
            }
        }

        // Create a statistic definition and add it to the collection based on the user selection in the combo boxes
        private void AddStatisticClicked(object sender, RoutedEventArgs e)
        {
            // Verify that a field name and statistic type has been selected
            if (FieldsComboBox.SelectedValue == null || StatTypeComboBox.SelectedValue == null) { return; }

            // Get the chosen field name and statistic type from the combo boxes
            string fieldName = FieldsComboBox.SelectedValue.ToString();
            StatisticType statType = Enum.Parse<StatisticType>(StatTypeComboBox.SelectedValue as string);

            // Check if this statistic definition has already be created (same field name and statistic type)
            StatisticDefinition existingStatDefinition = _statDefinitions.FirstOrDefault(def => def.OnFieldName == fieldName && def.StatisticType == statType);

            // If it doesn't exist, create it and add it to the collection (use the field name and statistic type to build the output alias)
            if (existingStatDefinition == null)
            {
                StatisticDefinition statDefinition = new StatisticDefinition(fieldName, statType, fieldName + "_" + statType.ToString());
                _statDefinitions.Add(statDefinition);
            }
        }

        // Toggle the sort order (ascending/descending) for the field selected in the sort fields list
        private void ChangeFieldSortOrder(object sender, RoutedEventArgs e)
        {
            // Verify that there is a selected sort field in the list
            OrderFieldOption selectedSortField = OrderByFieldsListBox.SelectedItem as OrderFieldOption;
            if (selectedSortField == null) { return; }

            // Create a new order field info to define the sort for the selected field
            OrderBy newOrderBy = new OrderBy(selectedSortField.OrderInfo.FieldName, selectedSortField.OrderInfo.SortOrder);
            OrderFieldOption newSortDefinition = new OrderFieldOption(true, newOrderBy);

            // Toggle the sort order from the current value
            if (newSortDefinition.OrderInfo.SortOrder == SortOrder.Ascending)
            {
                newSortDefinition.OrderInfo.SortOrder = SortOrder.Descending;
            }
            else
            {
                newSortDefinition.OrderInfo.SortOrder = SortOrder.Ascending;
            }

            // Add the new OrderBy at the same location in the collection and remove the old one
            _orderByFields.Insert(_orderByFields.IndexOf(selectedSortField), newSortDefinition);
            _orderByFields.Remove(selectedSortField);
        }

        // Remove the selected statistic definition from the list
        private void RemoveStatisticClicked(object sender, RoutedEventArgs e)
        {
            // Verify that there is a selected statistic definition
            if (StatFieldsListBox.SelectedItem == null) { return; }

            // Get the selected statistic definition and remove it from the collection
            StatisticDefinition selectedStat = StatFieldsListBox.SelectedItem as StatisticDefinition;
            _statDefinitions.Remove(selectedStat);
        }

        // Add the selected field in the "group by" list to the "order by" list
        private void AddSortFieldClicked(object sender, RoutedEventArgs e)
        {
            // Verify that there is a selected field in the "group by" list
            if (GroupFieldsListBox.SelectedItem == null) { return; }

            // Get the name of the selected field and ensure that it's in the list of selected group fields (checked on in the list, e.g.)
            string selectedFieldName = GroupFieldsListBox.SelectedItem.ToString();
            if (!_groupByFields.Contains(selectedFieldName))
            {
                ShowMessage("Only fields used for grouping can be used to order results.", "Order fields");
                return;
            }

            // Verify that the field isn't already in the "order by" list
            OrderFieldOption existingOrderBy = _orderByFields.FirstOrDefault(f => f.OrderInfo.FieldName == selectedFieldName);
            if (existingOrderBy == null)
            {
                // Create a new OrderBy for this field and add it to the collection (default to ascending sort order)
                OrderBy newOrderBy = new OrderBy(selectedFieldName, SortOrder.Ascending);
                OrderFieldOption orderField = new OrderFieldOption(false, newOrderBy);
                _orderByFields.Add(orderField);
            }
        }

        // Remove the selected field from the list of "order by" fields
        private void RemoveSortFieldClicked(object sender, RoutedEventArgs e)
        {
            // Verify that there is a selected item in the "order by" list
            if (OrderByFieldsListBox.SelectedItem == null) { return; }

            // Get the selected OrderFieldOption object and remove it from the collection
            OrderFieldOption selectedOrderBy = OrderByFieldsListBox.SelectedItem as OrderFieldOption;
            _orderByFields.Remove(selectedOrderBy);
        }        
    }

    // Simple class to describe an "order by" option
    public class OrderFieldOption
    {
        // Whether or not to use this field to order results
        public bool OrderWith { get; set; }

        // The order by info: field name and sort order
        public OrderBy OrderInfo { get; set; }

        public OrderFieldOption(bool orderWith, OrderBy orderInfo)
        {
            OrderWith = orderWith;
            OrderInfo = orderInfo;
        }
    }

    // Value converter to return strings in a specified format
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Make sure there's a value
            if (value == null)
                return null;

            // Make sure there's a converter parameter (format string, "{0:0,0}", for example)
            if (parameter == null)
                return value;

            // Return the formatted string
            return string.Format((string)parameter, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}