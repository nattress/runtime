// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

namespace R2RTracingTests
{
    [AttributeUsage(System.AttributeTargets.Method)]
    class R2RTestAttribute : Attribute
    {
        public string TestSetup { get; private set; }
        public R2RTestAttribute(string testSetup = null)
        {
            TestSetup = testSetup;
        }
    }

    partial class R2RTracingTest
    {
        public class CustomALC : AssemblyLoadContext
        {
            private string assemblyNameToLoad;
            private string assemblyPathToLoad;
            private bool throwOnLoad;

            public CustomALC(string name, bool throwOnLoad = false) : base(name)
            {
                this.throwOnLoad = throwOnLoad;
            }

            public void EnableLoad(string assemblyName, string path)
            {
                assemblyNameToLoad = assemblyName;
                assemblyPathToLoad = path;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (throwOnLoad)
                    throw new Exception($"Exception on Load in '{ToString()}'");

                if (!string.IsNullOrEmpty(assemblyNameToLoad) && assemblyName.Name == assemblyNameToLoad)
                    return LoadFromAssemblyPath(assemblyPathToLoad);

                return null;
            }
        }

        private const string DefaultALC = "Default";
        private const string DependentAssemblyName = "AssemblyToLoad";
        private const string DependentAssemblyTypeName = "AssemblyToLoad.Program";
        private const string SubdirectoryAssemblyName = "AssemblyToLoad_Subdirectory";

        private static CultureInfo SatelliteCulture = CultureInfo.CreateSpecificCulture("fr-FR");

        private const int S_OK = unchecked((int)0);
        private const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);

        private static readonly AssemblyName CoreLibName = typeof(object).Assembly.GetName();

        public static bool RunAllTests()
        {
            MethodInfo[] methods = typeof(R2RTracingTest)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<R2RTestAttribute>() != null && m.ReturnType == typeof(bool))
                .ToArray();

            foreach (var method in methods)
            {
                R2RTestAttribute attribute = method.GetCustomAttribute<R2RTestAttribute>();
                
                bool success = RunTestInSeparateProcess(method);
                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        public static int Main(string[] args)
        {
            bool success;
            try
            {
                if (args.Length == 0)
                {
                    success = RunAllTests();
                }
                else
                {
                    // Run specific test - first argument should be the test method name
                    MethodInfo method = typeof(R2RTracingTest)
                        .GetMethod(args[0], BindingFlags.Public | BindingFlags.Static);
                    Assert.IsTrue(method != null && method.GetCustomAttribute<R2RTestAttribute>() != null && method.ReturnType == typeof(bool), "Invalid test method specified");
                    success = RunSingleTest(method);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return success ? 100 : 101;
        }

        private static bool RunSingleTest(MethodInfo method)
        {
            Console.WriteLine($"Running {method.Name}...");
            try
            {
                R2RTestAttribute attribute = method.GetCustomAttribute<R2RTestAttribute>();
                if (!string.IsNullOrEmpty(attribute.TestSetup))
                {
                    MethodInfo setupMethod = method.DeclaringType
                        .GetMethod(attribute.TestSetup, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.IsTrue(setupMethod != null);
                    setupMethod.Invoke(null, new object[0]);
                }

                Func<bool> func = (Func<bool>)method.CreateDelegate(typeof(Func<bool>));
                using (var listener = new R2REventListener())
                {
                    R2ROperation expected = func();
                    ValidateSingleBind(listener, expected.AssemblyName, expected);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test {method.Name} failed: {e}");
                return false;
            }

            return true;
        }

        private static bool RunTestInSeparateProcess(MethodInfo method)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Arguments = $"{Assembly.GetExecutingAssembly().Location} {method.Name}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Console.WriteLine($"Launching process for {method.Name}...");
            using (Process p = Process.Start(startInfo))
            {
                p.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
                p.BeginOutputReadLine();

                p.ErrorDataReceived += (_, args) => Console.Error.WriteLine(args.Data);
                p.BeginErrorReadLine();

                p.WaitForExit();
                return p.ExitCode == 100;
            }
        }

        private static void ValidateSingleBind(R2REventListener listener, AssemblyName assemblyName)
        {
            //BindOperation[] binds = listener.WaitAndGetEventsForAssembly(assemblyName);
            //Assert.IsTrue(binds.Length == 1, $"Bind event count for {assemblyName} - expected: 1, actual: {binds.Length}");
            //BindOperation actual = binds[0];
            listener.WaitAndGetEventsForAssembly(assemblyName);
            //Helpers.ValidateBindOperation(expected, actual);
        }
    }
}
