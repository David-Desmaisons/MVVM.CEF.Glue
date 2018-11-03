﻿using System.Windows.Input;
using Neutronium.Core;
using Neutronium.Core.Infra;
using Neutronium.Core.Infra.VM;
using Neutronium.Core.JavascriptFramework;
using Neutronium.WPF.Utils;

namespace Neutronium.WPF.Internal.ViewModel
{
    public class About
    {
        public string CoreVersion { get; }
        public string WPFVersion { get; }
        public string WebBrowser { get; }
        public string JavascriptFrameworkVersion { get; }
        public string WebBrowserVersion { get; }
        public string WebEnvironment { get; }
        public string JavascriptFramework { get; }
        public string BrowserBinding { get; }
        public string MVVMBinding { get; }
        public string MVVMBindingVersion { get; }
        public ICommand GoToGithub { get; }

        public About(IWPFWebWindowFactory WindowFactory, IJavascriptFrameworkManager framework)
        {
            BrowserBinding = WindowFactory.Name;
            CoreVersion = VersionHelper.GetVersionDisplayName(typeof(IHtmlBinding));
            WPFVersion = VersionHelper.GetVersionDisplayName(this);
            WebBrowser = WindowFactory.EngineName;
            WebBrowserVersion = WindowFactory.EngineVersion;
            WebEnvironment = WindowFactory.Environment;
            JavascriptFramework = framework.FrameworkName;
            JavascriptFrameworkVersion = framework.FrameworkVersion;
            MVVMBinding = framework.Name;
            MVVMBindingVersion = VersionHelper.GetVersionDisplayName(framework);
            GoToGithub = new BasicRelayCommand(() => ProcessHelper.OpenUrlInBrowser(@"https://github.com/NeutroniumCore/Neutronium"));
        }
    }
}
