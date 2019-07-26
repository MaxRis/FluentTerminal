using FluentTerminal.App.Services;
using FluentTerminal.App.ViewModels;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FluentTerminal.App.Views
{
    public sealed partial class TabBar : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<TerminalViewModel>), typeof(TabBar), new PropertyMetadata(null));

        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register(nameof(AddCommand), typeof(RelayCommand), typeof(TabBar), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TabBar), new PropertyMetadata(null));

        public static readonly DependencyProperty CanDragTabsProperty =
            DependencyProperty.Register(nameof(CanDragTabs), typeof(bool), typeof(TabBar), new PropertyMetadata(null));

        public TabBar()
        {
            InitializeComponent();
            ScrollViewer.RegisterPropertyChangedCallback(ScrollViewer.ScrollableWidthProperty, OnScrollableWidthChanged);
            ListView.SelectionChanged += OnListViewSelectionChanged;
            ScrollLeftButton.Tapped += OnScrollLeftButtonTapped;
            ScrollRightButton.Tapped += OnScrollRightButtonTapped;
        }

        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CanDragTabs = (ItemsSource.Count > 1);
        }

        public RelayCommand AddCommand
        {
            get { return (RelayCommand)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        public ObservableCollection<TerminalViewModel> ItemsSource
        {
            get { return (ObservableCollection<TerminalViewModel>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); ItemsSource.CollectionChanged += ItemsSource_CollectionChanged; }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public bool CanDragTabs
        {
            get { return (bool)GetValue(CanDragTabsProperty); }
            set { SetValue(CanDragTabsProperty, value); }
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ListView.SelectedItem;
            if (item != null)
            {
                var container = ListView.ContainerFromItem(item);

                if (container != null)
                {
                    ((UIElement)container).StartBringIntoView();
                    SetScrollButtonsEnabledState();
                }
                else
                {
                    Task.Run(async () =>
                    {
                        do
                        {
                            await Task.Delay(50);
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => container = ListView.ContainerFromItem(item));
                        }
                        while (container == null);

                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ((UIElement)container).StartBringIntoView();
                            SetScrollButtonsEnabledState();
                        });
                    });
                }
            }
        }

        private void OnScrollableWidthChanged(DependencyObject sender, DependencyProperty property)
        {
            if (ScrollViewer.ScrollableWidth > 0)
            {
                ScrollLeftButton.Visibility = Visibility.Visible;
                ScrollRightButton.Visibility = Visibility.Visible;
            }
            else
            {
                ScrollLeftButton.Visibility = Visibility.Collapsed;
                ScrollRightButton.Visibility = Visibility.Collapsed;
            }
        }

        private void OnScrollLeftButtonTapped(object sender, RoutedEventArgs e)
        {
            var offset = ScrollViewer.HorizontalOffset - 10;
            ScrollViewer.ChangeView(offset, null, null);
            SetScrollButtonsEnabledState();
        }

        private void OnScrollRightButtonTapped(object sender, RoutedEventArgs e)
        {
            var offset = ScrollViewer.HorizontalOffset + 10;
            ScrollViewer.ChangeView(offset, null, null);
            SetScrollButtonsEnabledState();
        }

        private void SetScrollButtonsEnabledState()
        {
            ScrollLeftButton.IsEnabled = ScrollViewer.HorizontalOffset > 0;
            ScrollRightButton.IsEnabled = ScrollViewer.HorizontalOffset < ScrollViewer.ScrollableWidth;
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            //e.AcceptedOperation = DataPackageOperation.Move;
            Logger.Instance.Debug($"!!ListView_DragOver. e.AcceptedOperation: {e.AcceptedOperation}.");
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            Logger.Instance.Debug($"!!ListView_DragEnter. e.AcceptedOperation: {e.AcceptedOperation}.");
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.IsGlyphVisible = false;
            e.DragUIOverride.Caption = "Drop tab here";
        }

        private const string ShellProfileIdentifier = "ShellProfile";
        private const string TerminalIdIdentifier = "TerminalId";
        private const string XtermStateIdentifier = "XtermState";

        private int _dragInitialPosition = 0;

        private async void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            _itemWasDropped = false;

            //e.Data.RequestedOperation = DataPackageOperation.Move;
            Logger.Instance.Debug($"ListView_DragItemsStarting. e.Data.RequestedOperation: {e.Data.RequestedOperation}. Items count: {e.Items.Count}");

            _dragInitialPosition = ItemsSource.IndexOf((TerminalViewModel)e.Items[0]);
            Logger.Instance.Debug($"ListView_DragItemsStarting. Initial position: {_dragInitialPosition}.");

            var item = e.Items.FirstOrDefault();

            if (item is TerminalViewModel model)
            {
                await model.TrayProcessCommunicationService.PauseTerminalOutput(model.Terminal.Id, true);

                string xtermState = await model.Serialize();

                e.Data.Properties.Add(ShellProfileIdentifier, JsonConvert.SerializeObject(model.ShellProfile));
                e.Data.Properties.Add(TerminalIdIdentifier, model.Terminal.Id);
                e.Data.Properties.Add(XtermStateIdentifier, xtermState);
                e.Data.Properties.Add("TerminalViewModel", JsonConvert.SerializeObject(model));
            }

        }

        public event EventHandler<TerminalViewModel> TabDraggingCompleted;

        public event TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs> TabDraggedOutside;

        private static bool _itemWasDropped;

        private void ListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            Logger.Instance.Debug($"ListView_DragItemsCompleted. Drop result: {args.DropResult}. Items count: {args.Items.Count}");

            var model = (TerminalViewModel)args.Items[0];

            int position = ItemsSource.IndexOf((TerminalViewModel)args.Items[0]);
            Logger.Instance.Debug($"ListView_DragItemsCompleted. Result position: {position}.");

            if (ItemsSource.Count > 1 && !_itemWasDropped && args.DropResult == DataPackageOperation.None)
            {
                TabDraggedOutside?.Invoke(sender, args);
                _itemWasDropped = true;
            }

            if (_itemWasDropped /*_dragInitialPosition == position*/)
            {
                TabDraggingCompleted?.Invoke(sender, (TerminalViewModel)args.Items[0]);
            }

            model.TrayProcessCommunicationService.PauseTerminalOutput(model.Terminal.Id, false);
        }

        public event DragEventHandler TabWindowChanged;

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            Logger.Instance.Debug($"ListView_Drop. e.AcceptedOperation: {e.AcceptedOperation}.");
            TabWindowChanged?.Invoke(sender, e);
            _itemWasDropped = true;
        }

        private void ListView_DragLeave(object sender, DragEventArgs e)
        {
            Logger.Instance.Debug($"!!ListView_DragLeave. e.AcceptedOperation: {e.AcceptedOperation}.");
            e.DragUIOverride.IsGlyphVisible = false;
            e.DragUIOverride.Caption = "Drop to open new window";
        }

        private void ListView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            Logger.Instance.Debug($"ListView_DropCompleted. args.DropResult {args.DropResult}.");
        }
    }
}