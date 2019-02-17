﻿using Neutronium.Core.WebBrowserEngine.JavascriptObject;
using System.Collections.Generic;

namespace Neutronium.Core.JavascriptFramework
{
    /// <summary>
    /// Interface responsible for updating javascript ViewModel to reflect
    /// C# ViewModel updates
    /// </summary>
    public interface IJavascriptViewModelUpdater
    {
        /// <summary>
        /// Update javascript viewmodel without raising listeners events
        /// </summary>
        /// <param name="father">
        /// view model to be updated
        /// </param>
        /// <param name="propertyName">
        /// Name of the property to be updated
        /// </param>
        /// <param name="value">
        /// new value of the property
        /// </param>
        /// <param name="childAllowWrite">
        /// true if child should be listened
        /// </param>
        void UpdateProperty(IJavascriptObject father, string propertyName, IJavascriptObject value, bool childAllowWrite);


        /// <summary>
        /// Add a property to javascript viewmodel without raising listeners events
        /// </summary>
        /// <param name="father">
        /// view model to be updated
        /// </param>
        /// <param name="propertyName">
        /// Name of the property to be added
        /// </param>
        /// <param name="value">
        /// new value of the property
        /// </param>
        void AddProperty(IJavascriptObject father, string propertyName, IJavascriptObject value);

        /// <summary>
        /// Update javascript collection without raising listeners events
        /// performing javascript splice operation https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/splice
        /// </summary>
        /// <param name="array">
        /// collection to be updated
        /// </param>
        /// <param name="index">
        /// index argument
        /// </param>
        /// <param name="number">
        /// number argument
        /// </param>
        /// <param name="item">
        /// item argument
        /// </param>
        void SpliceCollection(IJavascriptObject array, int index, int number, IJavascriptObject item);

        /// <summary>
        /// Update javascript collection without raising listeners events
        /// performing javascript splice operation https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/splice
        /// </summary>
        /// <param name="array">
        /// collection to be updated
        /// </param>
        /// <param name="index">
        /// index argument
        /// </param>
        /// <param name="number">
        /// number argument
        /// </param>
        /// <param name="items">
        /// item argument
        /// </param>
        void SpliceCollection(IJavascriptObject array, int index, int number, IJavascriptObject[] items);

        /// <summary>
        /// Update javascript collection without raising listeners events
        /// performing javascript splice operation https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/splice
        /// </summary>
        /// <param name="array">
        /// collection to be updated
        /// </param>
        /// <param name="index">
        /// index argument
        /// </param>
        /// <param name="number">
        /// number argument
        /// </param>
        void SpliceCollection(IJavascriptObject array, int index, int number);

        /// <summary>
        /// Update javascript collection without raising listeners events
        /// clearing collection
        /// </summary>
        /// <param name="array">
        /// collection to be cleared
        /// </param>
        void ClearAllCollection(IJavascriptObject array);

        /// <summary>
        /// Update javascript collection without raising listeners events
        /// moving an object position
        /// </summary>
        /// <param name="array">
        /// collection to be updated
        /// </param>
        /// <param name="item">
        /// item to be moved
        /// </param>
        /// <param name="oldIndex">
        /// old index value
        /// </param>
        /// <param name="newIndex">
        /// new index value
        /// </param>
        void MoveCollectionItem(IJavascriptObject array, IJavascriptObject item, int oldIndex, int newIndex);

        /// <summary>
        /// UnListen changes for the given elements
        /// </summary>
        /// <param name="elementsToUnlisten">
        /// collection of elements to unlisten
        /// </param>
        void UnListen(IEnumerable<IJavascriptObject> elementsToUnlisten);


        /// <summary>
        /// Make object reactive and listen to all changes
        /// </summary>
        /// <param name="javascriptObject">
        /// Javascript object to br registered
        /// </param>
        void InjectDetached(IJavascriptObject javascriptObject);
    }
}
