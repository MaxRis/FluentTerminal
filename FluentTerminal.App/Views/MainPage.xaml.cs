using FluentTerminal.App.Utilities;
using FluentTerminal.App.ViewModels;
using FluentTerminal.Models;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace FluentTerminal.App.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private readonly CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

        public event PropertyChangedEventHandler PropertyChanged;

        public double CoreTitleBarHeight => coreTitleBar.Height;

        public TimeSpan NoDuration => TimeSpan.Zero;

        public Thickness CoreTitleBarPadding
        {
            get
            {
                if (FlowDirection == FlowDirection.LeftToRight)
                {
                    return new Thickness { Left = coreTitleBar.SystemOverlayLeftInset, Right = coreTitleBar.SystemOverlayRightInset };
                }
                else
                {
                    return new Thickness { Left = coreTitleBar.SystemOverlayRightInset, Right = coreTitleBar.SystemOverlayLeftInset };
                }
            }
        }

        public MainViewModel ViewModel { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            Root.DataContext = this;
            Window.Current.SetTitleBar(TitleBar);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Window.Current.Activated += OnWindowActivated;
            RegisterPropertyChangedCallback(RequestedThemeProperty, (s, e) =>
            {
                ContrastHelper.SetTitleBarButtonsForTheme(RequestedTheme);
            });

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is MainViewModel viewModel)
            {
                ViewModel = viewModel;
            }
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated && TerminalContainer.Content is TerminalView terminal)
            {
                terminal.ViewModel.FocusTerminal();
                ViewModel.FocusWindow();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            coreTitleBar.LayoutMetricsChanged -= OnLayoutMetricsChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            coreTitleBar.LayoutMetricsChanged += OnLayoutMetricsChanged;
            UpdateLayoutMetrics();
        }

        private void OnLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object e)
        {
            UpdateLayoutMetrics();
        }

        private void UpdateLayoutMetrics()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(CoreTitleBarHeight)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(CoreTitleBarPadding)));
            }
        }

        private async void TabView_TabDraggedOutside(object sender, Microsoft.Toolkit.Uwp.UI.Controls.TabDraggedOutsideEventArgs e)
        {
            if (e.Item is TerminalViewModel model)
            {
                ViewModel.TearOffTab(model);
            }
        }

        private void TabView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private const string ShellProfileIdentifier = "ShellProfile";
        private const string TerminalIdIdentifier = "TerminalId";
        private const string XtermStateIdentifier = "XtermState";

        private async void TabView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.TryGetValue("TerminalViewModel", out object terminalViewModelStr) &&
                (e.DataView.Properties.TryGetValue(ShellProfileIdentifier, out object profileObject) && profileObject is string serializedProfile) &&
                (e.DataView.Properties.TryGetValue(TerminalIdIdentifier, out object idObject) && idObject is byte terminalId) &&
                (e.DataView.Properties.TryGetValue(XtermStateIdentifier, out object stateObject) && stateObject is string state))
            {
                TerminalViewModel terminalViewModel = JsonConvert.DeserializeObject<TerminalViewModel>((string)terminalViewModelStr);
                var profile = JsonConvert.DeserializeObject<ShellProfile>(serializedProfile);

                await ViewModel.AddTerminalAsync(terminalId, profile, state, terminalViewModel);
            }
        }

        private void TabBar_TabDraggedOutside(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.Items.Count > 0 && args.Items[0] is TerminalViewModel model)
            {
                ViewModel.TearOffTab(model);
            }
        }

        private void TabBar_TabDraggingCompleted(object sender, TerminalViewModel e)
        {
            int position = ViewModel.Terminals.IndexOf(e);
            ViewModel.Terminals.Remove(e);
            if (ViewModel.Terminals.Count > 0)
            {
                ViewModel.SelectedTerminal = ViewModel.Terminals[Math.Max(0, position - 1)];
            }
            else
            {
                ViewModel.ApplicationView.TryClose();
            }
        }
    }
}