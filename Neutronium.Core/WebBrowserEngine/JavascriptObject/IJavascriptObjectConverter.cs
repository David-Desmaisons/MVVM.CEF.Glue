﻿using System;

namespace Neutronium.Core.WebBrowserEngine.JavascriptObject
{
    /// <summary>
    /// Converter from IJavascriptObject to basic CLR Type
    /// </summary>
    public interface IJavascriptObjectConverter
    {
        /// <summary>
        /// Convert a IJavascriptObject to basic CLR Type
        /// </summary>
        /// <param name="value">
        /// IJavascriptObject to convert
        /// </param>
        /// <param name="res">
        /// converted object
        /// </param>
        /// <param name="targetType">
        /// Target type for the result if any
        /// </param>
        /// <returns>
        /// true if the operation is successful
        ///</returns>
        bool GetSimpleValue(IJavascriptObject value, out object res, Type targetType = null);
    }
}
