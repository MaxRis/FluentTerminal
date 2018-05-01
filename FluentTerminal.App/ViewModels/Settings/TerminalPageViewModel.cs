﻿using FluentTerminal.App.Services;
using FluentTerminal.Models;
using FluentTerminal.Models.Enums;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Graphics.Canvas.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentTerminal.App.ViewModels.Settings
{
    public class TerminalPageViewModel : ViewModelBase
    {
        private readonly TerminalOptions _terminalOptions;
        private readonly ISettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private readonly IDefaultValueProvider _defaultValueProvider;
        private bool _isEditingCursorStyle;

        public bool BarIsSelected
        {
            get => CursorStyle == CursorStyle.Bar;
            set => CursorStyle = CursorStyle.Bar;
        }

        public bool BlockIsSelected
        {
            get => CursorStyle == CursorStyle.Block;
            set => CursorStyle = CursorStyle.Block;
        }

        public bool CursorBlink
        {
            get => _terminalOptions.CursorBlink;
            set
            {
                if (_terminalOptions.CursorBlink != value)
                {
                    _terminalOptions.CursorBlink = value;
                    _settingsService.SaveTerminalOptions(_terminalOptions);
                    RaisePropertyChanged();
                }
            }
        }

        public string FontFamily
        {
            get => _terminalOptions.FontFamily;
            set
            {
                if (_terminalOptions.FontFamily != value)
                {
                    _terminalOptions.FontFamily = value;
                    _settingsService.SaveTerminalOptions(_terminalOptions);
                    RaisePropertyChanged();
                }
            }
        }

        public IEnumerable<string> Fonts { get; }

        public int FontSize
        {
            get => _terminalOptions.FontSize;
            set
            {
                if (_terminalOptions.FontSize != value)
                {
                    _terminalOptions.FontSize = value;
                    _settingsService.SaveTerminalOptions(_terminalOptions);
                    RaisePropertyChanged();
                }
            }
        }

        public IEnumerable<int> Sizes { get; }

        public bool UnderlineIsSelected
        {
            get => CursorStyle == CursorStyle.Underline;
            set => CursorStyle = CursorStyle.Underline;
        }

        public RelayCommand RestoreDefaultsCommand { get; }

        private CursorStyle CursorStyle
        {
            get => _terminalOptions.CursorStyle;
            set
            {
                if (_terminalOptions.CursorStyle != value && !_isEditingCursorStyle)
                {
                    _isEditingCursorStyle = true;
                    _terminalOptions.CursorStyle = value;
                    _settingsService.SaveTerminalOptions(_terminalOptions);
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(BlockIsSelected));
                    RaisePropertyChanged(nameof(BarIsSelected));
                    RaisePropertyChanged(nameof(UnderlineIsSelected));
                    _isEditingCursorStyle = false;
                }
            }
        }

        private async Task RestoreDefaults()
        {
            var result = await _dialogService.ShowDialogAsnyc("Please confirm", "Are you sure you want to restore the default terminal options?", DialogButton.OK, DialogButton.Cancel).ConfigureAwait(false);

            if (result == DialogButton.OK)
            {
                var defaults = _defaultValueProvider.GetDefaultTerminalOptions();
                CursorBlink = defaults.CursorBlink;
                CursorStyle = defaults.CursorStyle;
                FontFamily = defaults.FontFamily;
                FontSize = defaults.FontSize;
            }
        }

        public TerminalPageViewModel(ISettingsService settingsService, IDialogService dialogService, IDefaultValueProvider defaultValueProvider)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
            _defaultValueProvider = defaultValueProvider;

            RestoreDefaultsCommand = new RelayCommand(async () => await RestoreDefaults().ConfigureAwait(false));

            Fonts = CanvasTextFormat.GetSystemFontFamilies().OrderBy(s => s);
            Sizes = Enumerable.Range(1, 72);

            _terminalOptions = _settingsService.GetTerminalOptions();
        }
    }
}
