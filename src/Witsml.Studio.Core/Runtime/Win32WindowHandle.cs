//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Interop;

namespace PDS.Witsml.Studio.Core.Runtime
{
    /// <summary>
    /// Provides a handle for a WPF <see cref="Window"/> that can be used along with Win32 windows and dialogs.
    /// </summary>
    /// <seealso cref="System.Windows.Forms.IWin32Window" />
    public class Win32WindowHandle : System.Windows.Forms.IWin32Window
    {
        private IntPtr _handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32WindowHandle"/> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public Win32WindowHandle(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _handle = helper.Handle;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Win32WindowHandle"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        public Win32WindowHandle(IntPtr handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// Gets the handle to the window represented by the implementer.
        /// </summary>
        public IntPtr Handle
        {
            get { return _handle; }
        }
    }
}
