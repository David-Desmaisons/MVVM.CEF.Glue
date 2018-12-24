﻿using System;
using System.Collections.Generic;
using Neutronium.Core;
using Neutronium.Core.Infra;
using Neutronium.Core.JavascriptFramework;
using Neutronium.Core.WebBrowserEngine.JavascriptObject;

namespace Neutronium.JavascriptFramework.Knockout
{
    public class KnockoutFrameworkManager : IJavascriptFrameworkManager
    {
        private string _JavascriptDebugScript;
        private string _MainScript;

        public string FrameworkName => "knockout.js";
        public string FrameworkVersion => "3.3.0";
        public string Name => "KnockoutInjector";
        public DebugToolsUI DebugToolsUI => null;
        public bool IsMappingObject => true;
        public bool IsSupportingVmDebug => true;

        public IJavascriptViewModelManager CreateManager(IWebView webView, IJavascriptObject listener, IWebSessionLogger logger, bool debugMode) 
        {
            return new KnockoutUiVmManager(webView, listener, logger);
        }

        private string GetDebugScript()
        {
            if (_JavascriptDebugScript != null)
                return  _JavascriptDebugScript ;

            _JavascriptDebugScript = GetResourceReader().Load("ko-view.min.js");
            return _JavascriptDebugScript ;
        }

        private string GetDebugToogleScript()
        {
            return "ko.dodebug();";
        }

        public string GetMainScript(bool debugMode)
        {
            if (_MainScript != null)
                return _MainScript;

            var resourceLoader = GetResourceReader();
            return _MainScript = resourceLoader.Load(JavascriptSource);
        }

        private static IEnumerable<string> JavascriptSource
        {
            get
            {
                yield return "knockout.js";
                yield return "knockout-delegatedEvents.min.js";
                yield return "promise.min.js";
                yield return "Ko_Extension.min.js";
            }
        }

        private ResourceReader GetResourceReader()
        {
            return new ResourceReader("scripts", this);
        }

        public void DebugVm(IDebugFacility debugHelper)
        {
            var javascriptDebugScript = GetDebugScript();
            debugHelper.RunJavascript(javascriptDebugScript);
            debugHelper.RunJavascript(GetDebugToogleScript());
        }
    }
}
