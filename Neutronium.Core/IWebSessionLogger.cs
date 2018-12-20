﻿using System;
using Neutronium.Core.WebBrowserEngine.Window;

namespace Neutronium.Core {
    /// <summary>
    /// Interface to implement to receive event from browser
    /// </summary>
    public interface IWebSessionLogger
    {
        /// <summary>
        /// called for debug logging
        /// </summary>
        void Debug(Func<string> information);

        /// <summary>
        /// called for debug logging
        /// </summary>
        void Debug(string information);

        /// <summary>
        /// called for information logging
        /// </summary>
        void Info(string information);

        /// <summary>
        /// called for information logging 
        /// </summary>
        void Info(Func<string> information);

        /// <summary>
        /// called for warning 
        /// </summary>
        void Warning(string information);

        /// <summary>
        /// called for warning 
        /// </summary>
        void Warning(Func<string> information);

        /// <summary>
        /// called on critical event 
        /// </summary>
        void Error(string information);

        /// <summary>
        /// called on critical event 
        /// </summary>
        void Error(Func<string> information);

        /// <summary>
        /// called on each consolo log called by browser 
        /// </summary>
        void LogBrowser(ConsoleMessageArgs information, Uri url);

        /// <summary>
        /// called in case of browser critical error
        /// </summary>
        void WebBrowserError(Exception exception, Action cancel);
    }
}
