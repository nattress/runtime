// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

namespace BinderTracingTests
{
    partial class R2RTracingTest
    {
        [R2RTest]
        public static bool IsR2RWorking()
        {
            return true;
        }
}