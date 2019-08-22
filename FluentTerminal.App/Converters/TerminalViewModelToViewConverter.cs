﻿using FluentTerminal.App.ViewModels;
using FluentTerminal.App.Views;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace FluentTerminal.App.Converters
{
    internal class TerminalViewModelToViewConverter : IValueConverter
    {
        private readonly Dictionary<TerminalViewModel, TerminalView> _viewDictionary;

        public TerminalViewModelToViewConverter()
        {
            _viewDictionary = new Dictionary<TerminalViewModel, TerminalView>();
        }

        public void RemoveTerminal(TerminalViewModel viewModel)
        {
            if (_viewDictionary.TryGetValue(viewModel, out TerminalView terminalView))
            {
                ((XtermTerminalView)viewModel.TerminalView).ViewModel = null;
                terminalView.StopBindingsTracking();
                terminalView.ViewModel = null;
                viewModel.TerminalView = null;
                viewModel.Terminal = null;
                viewModel.Overlay = null;
            }
            _viewDictionary.Remove(viewModel);
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TerminalViewModel terminal)
            {
                if (_viewDictionary.TryGetValue(terminal, out TerminalView view))
                {
                    return view;
                }

                var newView = new TerminalView(terminal);
                _viewDictionary.Add(terminal, newView);
                return newView;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}