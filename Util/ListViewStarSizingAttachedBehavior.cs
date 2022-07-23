using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace DocumentDatabase.Util
{
    // Most of this is from Andy Stagg  at https://stackoverflow.com/a/70229891

    public class ColumnAttachedProperties : DependencyObject
    {
        public static readonly DependencyProperty GridLength_WidthProperty = DependencyProperty.RegisterAttached(
            name: "GridLength_Width",
            propertyType: typeof(GridLength),
            ownerType: typeof(ColumnAttachedProperties),
            defaultMetadata: new FrameworkPropertyMetadata(GridLength.Auto));

        public static GridLength GetGridLength_Width(DependencyObject dependencyObject) => (GridLength)dependencyObject.GetValue(GridLength_WidthProperty);

        public static void SetGridLength_Width(DependencyObject dependencyObject, string value) => dependencyObject.SetValue(GridLength_WidthProperty, value);


        public static readonly DependencyProperty GridLength_MinWidthProperty = DependencyProperty.RegisterAttached(
            name: "GridLength_MinWidth",
            propertyType: typeof(double),
            ownerType: typeof(ColumnAttachedProperties),
            defaultMetadata: new FrameworkPropertyMetadata(0.0));

        public static double GetGridLength_MinWidth(DependencyObject dependencyObject) => (double)dependencyObject.GetValue(GridLength_MinWidthProperty);

        public static void SetGridLength_MinWidth(DependencyObject dependencyObject, string value) => dependencyObject.SetValue(GridLength_MinWidthProperty, value);
    }



    public static class ListViewStarSizingAttachedBehavior
    {
        public static readonly DependencyProperty UseGridLength_WidthProperty = DependencyProperty.RegisterAttached(
        name: "UseGridLength_Width",
        propertyType: typeof(bool),
        ownerType: typeof(ListViewStarSizingAttachedBehavior),
        new UIPropertyMetadata(false, RegisterEventHandlers));

        public static bool GetUseGridLength_Width(DependencyObject dependencyObject)
            => (bool)dependencyObject.GetValue(UseGridLength_WidthProperty);
        public static void SetUseGridLength_Width(DependencyObject dependencyObject, bool value)
            => dependencyObject.SetValue(UseGridLength_WidthProperty, value);

        private static void RegisterEventHandlers(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView)
            {
                if (e.NewValue is bool booleanValue && booleanValue == true)
                {
                    listView.SizeChanged += ListView_SizeChanged;
                    listView.Loaded += ListView_Loaded;
                }
                else
                {
                    listView.SizeChanged -= ListView_SizeChanged;
                    listView.Loaded -= ListView_Loaded;
                }
            }
        }

        private static void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            CalculateGridColumnWidths(sender);
        }

        private static void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ListView listView && !listView.IsLoaded) return;

            CalculateGridColumnWidths(sender);
        }

        private static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private static void CalculateGridColumnWidths(object sender)
        {
            if (sender is ListView listView && listView.View is GridView gridView)
            {
                // Auto adjust based on vertical scrollbar being visible

                ScrollViewer sv = FindVisualChild<ScrollViewer>(listView);
                Visibility VerticalScrollbarVisibility = sv.ComputedVerticalScrollBarVisibility;

                var scrollOffset = VerticalScrollbarVisibility == Visibility.Collapsed ? 10 : SystemParameters.VerticalScrollBarWidth + 10;

                var remainingWidth = listView.ActualWidth - scrollOffset;
                var starTotal = 0.0;
                var starMinWidth = 0.0;

                foreach (var column in gridView.Columns)
                {
                    var gridLength = ColumnAttachedProperties.GetGridLength_Width(column);

                    if (gridLength.IsStar)
                    {
                        // Get the cumlative star value while passing over the columns
                        // but don't set their width until absolute and auto have been set.
                        starTotal += gridLength.Value;
                        starMinWidth += ColumnAttachedProperties.GetGridLength_MinWidth(column);
                        continue;
                    }

                    if (gridLength.IsAbsolute)
                    {
                        column.Width = gridLength.Value;
                    }
                    else
                    {
                        column.Width = double.NaN;
                    }

                    remainingWidth -= column.ActualWidth;
                }

                if (remainingWidth < 0)
                    remainingWidth = starMinWidth; // minimum width needed for all star entries if no space is available. This will cause horizontal scrollbar to show up

                if (starTotal == 0.0) return;

                // now eval each star column
                foreach (var column in gridView.Columns)
                {
                    var gridLength = ColumnAttachedProperties.GetGridLength_Width(column);

                    if (!gridLength.IsStar) continue;

                    var minGridWidth = ColumnAttachedProperties.GetGridLength_MinWidth(column);

                    var starPercent = (gridLength.Value / starTotal);
                    var desiredWidth = remainingWidth * starPercent;
                    
                    if (desiredWidth < minGridWidth && minGridWidth > 0)
                    {
                        // Too small, convert this grid to static width
                        desiredWidth = minGridWidth;

                        starTotal -= gridLength.Value;
                        remainingWidth -= column.Width;
                    }

                    column.Width = desiredWidth;
                }
            }
        }
    }
}
