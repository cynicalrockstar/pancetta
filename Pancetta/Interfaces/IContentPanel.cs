﻿using Pancetta.DataObjects;
using Pancetta.Windows.ContentPanels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Pancetta.Windows.Interfaces
{
    public interface IContentPanel
    {
        /// <summary>
        /// Indicates how large the panel is in memory.
        /// </summary>
        PanelMemorySizes PanelMemorySize { get; }

        /// <summary>
        /// Called when the control should start preparing the content to be shown.
        /// </summary>
        void OnPrepareContent();

        /// <summary>
        /// Called when the control's visibility is changed.
        /// </summary>
        void OnVisibilityChanged(bool isVisible);

        /// <summary>
        /// Called when the flip view content should be destroyed.
        /// </summary>
        void OnDestroyContent();

        /// <summary>
        /// Fired when a new host has been added.
        /// </summary>
        void OnHostAdded();
    }
}
