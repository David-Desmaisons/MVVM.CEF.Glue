﻿using Chromium;
using Chromium.Event;
using Chromium.Remote.Event;
using Chromium.WebBrowser;
using Neutronium.Core;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;
using Neutronium.Core.WebBrowserEngine.Window;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Neutronium.Core.Infra;
using Neutronium.WebBrowserEngine.ChromiumFx.WPF;

namespace Neutronium.WebBrowserEngine.ChromiumFx.EngineBinding
{
    public class ChromiumFxControlWebBrowserWindow : IModernWebBrowserWindow
    {
        private readonly ChromiumWebBrowser _ChromiumWebBrowser;
        private readonly IDispatcher _Dispatcher;
        private readonly IWebSessionLogger _Logger;
        private CfxBrowser _CfxWebBrowser;
        private bool _FirstLoad = true;
        private bool _SendLoadOnContextCreated = false;

        public IWebView MainFrame { get; private set; }
        public Uri Url => _ChromiumWebBrowser.Url;
        public bool IsLoaded => !_ChromiumWebBrowser.IsLoading;
        private readonly List<ContextMenuItem> _Commands = new List<ContextMenuItem>();
        private readonly List<IEnumerable<ContextMenuItem>> _CommandDescription = new List<IEnumerable<ContextMenuItem>>();

        private readonly List<int> _MenuSeparatorIndex = new List<int>();

        public ChromiumFxControlWebBrowserWindow(ChromiumWebBrowser chromiumWebBrowser, IDispatcher dispatcher, IWebSessionLogger logger)
        {
            _Logger = logger;
            _Dispatcher = dispatcher;
            _ChromiumWebBrowser = chromiumWebBrowser;
            _ChromiumWebBrowser.DisplayHandler.OnConsoleMessage += OnConsoleMessage;
            _ChromiumWebBrowser.OnV8ContextCreated += OnV8ContextCreated;
            _ChromiumWebBrowser.LifeSpanHandler.OnBeforePopup += LifeSpanHandler_OnBeforePopup;
            ListenToLoadHandler(_ChromiumWebBrowser.LoadHandler);
            ListenToContextMenuHandler(_ChromiumWebBrowser.ContextMenuHandler);
            ListenToRequestHandler(_ChromiumWebBrowser.RequestHandler);
        }

        private void ListenToLoadHandler(CfxLoadHandler loadHandler)
        {
            loadHandler.OnLoadEnd += OnLoadEnd;
            loadHandler.OnLoadError += LoadHandler_OnLoadError;
        }

        private void ListenToContextMenuHandler(CfxContextMenuHandler contextMenuHandler)
        {
            contextMenuHandler.OnBeforeContextMenu += OnBeforeContextMenu;
            contextMenuHandler.OnContextMenuCommand += ContextMenuHandler_OnContextMenuCommand;
        }

        private void ListenToRequestHandler(CfxRequestHandler requestHandler)
        {
            requestHandler.OnBeforeBrowse += RequestHandler_OnBeforeBrowse;
            requestHandler.OnRenderProcessTerminated += RequestHandler_OnRenderProcessTerminated;
        }

        private void LifeSpanHandler_OnBeforePopup(object sender, CfxOnBeforePopupEventArgs e)
        {
            ProcessHelper.OpenUrlInBrowser(e.TargetUrl);
            e.SetReturnValue(true);
        }

        private void LoadHandler_OnLoadError(object sender, CfxOnLoadErrorEventArgs e)
        {
            if (e.ErrorCode == CfxErrorCode.Aborted)
            {
                //Aborted is raised during hot-reload
                //We will not pollute log nor stop the application
                return;
            }

            _Logger.Error($@"Unable to load ""{e.FailedUrl}"": ""{e.ErrorCode}"". Please check that the resource exists, has the correct ""Content"" and ""Build Type"" value or is correctly served.");
            if (!e.Frame.IsMain)
                return;

            _Logger.Error("Closing application");
            _Dispatcher.RunAsync(async () =>
            {
                //Delay here to be sure to finish all chromium related task
                //before closing application. This will avoid additional exception
                //due to inconsistent state.
                await Task.Delay(10);
                Application.Current.Shutdown(-1);
            });
        }

        private void RequestHandler_OnBeforeBrowse(object sender, CfxOnBeforeBrowseEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            var request = e.Request;

            switch (request.TransitionType)
            {
                case CfxTransitionType.Explicit:
                    return;

                case CfxTransitionType.ClientRedirectFlag:
                    e.SetReturnValue(true);
                    FireReload();
                    break;

                default:
                    _Logger.Error($@"Navigation to {request.Url} triggered by ""{request.TransitionType}"" has been cancelled. It is not possible to trigger a page loading from javascript that may corrupt session and hot-reload. Use Neutronium API to alter HTML view.");
                    e.SetReturnValue(true);
                    var url = _CfxWebBrowser.Host?.VisibleNavigationEntry?.Url;
                    FireReload(url ?? request.ReferrerUrl);
                    break;
            }
        }

        private void FireReload(string url = null)
        {
            _Dispatcher.Dispatch(() => OnClientReload?.Invoke(this, new ClientReloadArgs(url)));
        }

        private void RequestHandler_OnRenderProcessTerminated(object sender, CfxOnRenderProcessTerminatedEventArgs e)
        {
            var crashed = Crashed;
            if (crashed != null)
                _Dispatcher.Dispatch(() => crashed(this, new BrowserCrashedArgs()));
        }

        private void OnBeforeContextMenu(object sender, CfxOnBeforeContextMenuEventArgs e)
        {
            var model = e.Model;
            for (var index = model.Count - 1; index >= 0; index--)
            {
                if (!CfxContextMenu.IsEdition(model.GetCommandIdAt(index)))
                    model.RemoveAt(index);
            }

            if (model.Count != 0)
                return;

            ComputeCommands();
            var rank = (int)ContextMenuId.MENU_ID_USER_FIRST;
            _Commands.ForEach(command =>
            {
                model.AddItem(rank, command.Name);
                model.SetEnabled(rank++, command.Enabled);
            });
            _MenuSeparatorIndex.ForEach(index => model.InsertSeparatorAt(index));
        }

        public IModernWebBrowserWindow RegisterContextMenuItem(IEnumerable<ContextMenuItem> contextMenuItems)
        {
            if (contextMenuItems != null)
                _CommandDescription.Add(contextMenuItems);

            return this;
        }

        private void ComputeCommands()
        {
            _Commands.Clear();
            _MenuSeparatorIndex.Clear();

            foreach (var contextMenuItems in _CommandDescription)
            {
                var oldCount = _Commands.Count;
                _Commands.AddRange(contextMenuItems);
                var currentCount = _Commands.Count;
                if (oldCount != currentCount)
                    _MenuSeparatorIndex.Insert(0, currentCount);
            }
        }

        private void ContextMenuHandler_OnContextMenuCommand(object sender, CfxOnContextMenuCommandEventArgs e)
        {
            if (!CfxContextMenu.IsUserDefined(e.CommandId))
                return;

            var command = _Commands[e.CommandId - (int)ContextMenuId.MENU_ID_USER_FIRST].Command;
            command.Invoke();
        }

        private void OnV8ContextCreated(object sender, CfrOnContextCreatedEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            MainFrame = new ChromiumFxWebView(e.Browser, _Logger);

            var beforeJavascriptExecuted = BeforeJavascriptExecuted;
            if (beforeJavascriptExecuted == null)
                return;

            void Execute(string code) => e.Frame.ExecuteJavaScript(code, String.Empty, 0);
            beforeJavascriptExecuted(this, new BeforeJavascriptExcecutionArgs(MainFrame, Execute));

            if (_SendLoadOnContextCreated)
                SendLoad();
        }

        private void OnConsoleMessage(object sender, CfxOnConsoleMessageEventArgs e)
        {
            e.SetReturnValue(false);
            ConsoleMessage?.Invoke(this, new ConsoleMessageArgs(e.Message, e.Source, e.Line));
        }

        private void OnLoadEnd(object sender, CfxOnLoadEndEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            _CfxWebBrowser = e.Browser;

            if (_FirstLoad)
            {
                _FirstLoad = false;
                return;
            }

            SendLoad();
        }

        private void SendLoad()
        {
            var loadEnd = LoadEnd;
            if (loadEnd == null)
                return;

            if (MainFrame == null)
            {
                _ChromiumWebBrowser.ExecuteJavascript("(function(){})()");
                _SendLoadOnContextCreated = true;
                return;
            }

            _SendLoadOnContextCreated = false;
            loadEnd(this, new LoadEndEventArgs(MainFrame));
        }

        public void NavigateTo(Uri path)
        {
            var url = GetPathFromUri(path);
            UpdateClientSideRouteIfNeeded(path);
            _ChromiumWebBrowser.LoadUrl(url);
        }

        private string GetPathFromUri(Uri path)
        {
            switch (path.Scheme)
            {
                case "file":
                    return path.AbsolutePath;

                case "pack":
                    return NeutroniumResourceHandler.GetLoadPackUrl(path);

                case "https":
                    return NeutroniumResourceHandler.GetLoadHttpsUrl(path);

                default:
                    return path.ToString();
            }
        }

        private void UpdateClientSideRouteIfNeeded(Uri path)
        {
            if (!isPack(path) || (string.IsNullOrEmpty(path.Fragment)))
                return;

            void UpdateLocation(object _, BeforeJavascriptExcecutionArgs e)
            {
                BeforeJavascriptExecuted -= UpdateLocation;
                e.JavascriptExecutor($"window.location.href = '{NeutroniumResourceHandler.GetFullPackUrl(path)}';");
            }

            BeforeJavascriptExecuted += UpdateLocation;
        }

        private bool isPack(Uri path)
        {
            var scheme = path.Scheme;
            return ((scheme == "pack") || (scheme == "https"));

        }

        public event EventHandler<LoadEndEventArgs> LoadEnd;
        public event EventHandler<ConsoleMessageArgs> ConsoleMessage;
        public event EventHandler<BeforeJavascriptExcecutionArgs> BeforeJavascriptExecuted;
        public event EventHandler<BrowserCrashedArgs> Crashed;
        public event EventHandler<ClientReloadArgs> OnClientReload;
    }
}
