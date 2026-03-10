using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace VsLoggerEngine.Helpers;

public static class DataGridHelper
{
    public static readonly DependencyProperty AutoScrollOnChangeProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollOnChange",
            typeof(bool),
            typeof(DataGridHelper),
            new PropertyMetadata(false, OnAutoScrollOnChangeChanged));

    public static bool GetAutoScrollOnChange(DependencyObject obj) =>
        (bool)obj.GetValue(AutoScrollOnChangeProperty);

    public static void SetAutoScrollOnChange(DependencyObject obj, bool value) =>
        obj.SetValue(AutoScrollOnChangeProperty, value);

    private static void OnAutoScrollOnChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
            return;

        var enable = (bool)e.NewValue;

        if (enable)
        {
            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid))
                .RemoveValueChanged(dataGrid, OnItemsSourceChanged);
            DependencyPropertyDescriptor
                .FromProperty(DataGrid.SelectedItemProperty, typeof(DataGrid))
                .RemoveValueChanged(dataGrid, OnSelectedItemChanged);

            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid))
                .AddValueChanged(dataGrid, OnItemsSourceChanged);

            DependencyPropertyDescriptor
                .FromProperty(DataGrid.SelectedItemProperty, typeof(DataGrid))
                .AddValueChanged(dataGrid, OnSelectedItemChanged);

            HookCollectionChanged(dataGrid, true);
        }
        else if (!enable )
        {
            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid))
                .RemoveValueChanged(dataGrid, OnItemsSourceChanged);
            DependencyPropertyDescriptor
                .FromProperty(DataGrid.SelectedItemProperty, typeof(DataGrid))
                .RemoveValueChanged(dataGrid, OnSelectedItemChanged);

            HookCollectionChanged(dataGrid, false);
        }

        void OnItemsSourceChanged(object? sender, EventArgs args) => HookCollectionChanged(dataGrid, true);
        void OnSelectedItemChanged(object? sender, EventArgs args)
        {
            if (dataGrid.SelectedItem is not null)
            {
                dataGrid.Dispatcher.InvokeAsync(() =>
                    dataGrid.ScrollIntoView(dataGrid.SelectedItem));
            }
        }
    }

    private static void HookCollectionChanged(DataGrid dataGrid, bool bEnable)
    {
        NotifyCollectionChangedEventHandler CollectionChangedHandler = (s, args) =>
        {
            if (s is DataGrid dataGrid &&
                args.Action == NotifyCollectionChangedAction.Add &&
                args.NewItems?.Count > 0)
            {
                var newItem = args.NewItems[0];
                dataGrid.Dispatcher.InvokeAsync(() =>
                {
                    dataGrid.ScrollIntoView(newItem);
                    dataGrid.SelectedItem = newItem;
                });
            }
        };

        if (dataGrid.ItemsSource is not INotifyCollectionChanged collection)
            return;
        if (bEnable)
        {
            collection.CollectionChanged -= CollectionChangedHandler;
            collection.CollectionChanged += CollectionChangedHandler;
        }
        else
        {
            collection.CollectionChanged -= CollectionChangedHandler;
        }
    }
}
