﻿using Neutronium.Core.Infra;
using System;
using System.Windows;
using Neutronium.Core.WebBrowserEngine.Window;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;

namespace Neutronium.WPF.Internal.DebugTools
{
    /// <summary>
    /// Interaction logic for ViewModelDebug.xaml
    /// </summary>
    public partial class HTMLSimpleWindow : IDisposable
    {
        private readonly IWPFWebWindow _WPFWebWindow;
        private readonly string _path;
        private UIElement _WebBrowser;
        private Func<IWebView, IDisposable> _OnWebViewCreated;
        private IDisposable _Disposable;

        public HTMLSimpleWindow()
        {
            InitializeComponent();
        }

        public HTMLSimpleWindow(IWPFWebWindow wpfWebWindow, string path, Func<IWebView, IDisposable> onWebViewCreated=null)
        {
            _WPFWebWindow = wpfWebWindow;
            _path = path;
            _OnWebViewCreated = onWebViewCreated;
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _WebBrowser = _WPFWebWindow.UIElement;
            MainGrid.Children.Add(_WebBrowser);
            _WPFWebWindow.HTMLWindow.LoadEnd += HTMLWindow_LoadEnd;
            if ((_OnWebViewCreated != null) && (_WPFWebWindow.HTMLWindow is IModernWebBrowserWindow modern))     
                modern.BeforeJavascriptExecuted += Modern_BeforeJavascriptExecuted;

           var uri = new Uri($"{GetType().Assembly.GetPath()}\\{_path}");
            _WPFWebWindow.HTMLWindow.NavigateTo(uri);
        }

        private void Modern_BeforeJavascriptExecuted(object sender, BeforeJavascriptExcecutionArgs e) 
        {
            var modern = _WPFWebWindow.HTMLWindow as IModernWebBrowserWindow;
            modern.BeforeJavascriptExecuted -= Modern_BeforeJavascriptExecuted;
            _Disposable = _OnWebViewCreated(e.WebView);
        }

        private void HTMLWindow_LoadEnd(object sender, Core.WebBrowserEngine.Window.LoadEndEventArgs e)
        {
            _WPFWebWindow.HTMLWindow.LoadEnd -= HTMLWindow_LoadEnd;
            _WebBrowser.Visibility = Visibility.Visible;
            Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispose();
        }

        public void Dispose()
        {
            _Disposable?.Dispose();
            _WPFWebWindow.Dispose();
            _Disposable = null;
        }
    }
}
