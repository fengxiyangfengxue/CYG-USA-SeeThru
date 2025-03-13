using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Test._App;
using UserHelpers.Helpers;
using System.Windows.Media;
using System.Data;
using System.Reflection;
using System.Xml.Linq;
using Test._ScriptExtensions;
using Test._Definitions;
using Test._ScriptHelpers;
using System.Text.RegularExpressions;

namespace Test
{
    public partial class MainClass
    {
        FileInfo SearchAssemblyFile(string directory, string dllName)
        {
            FileInfo fiDLL = null;

            var di = new DirectoryInfo(directory);
            fiDLL = di.GetFiles("*.dll").Where(f => f.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (fiDLL != null)
                return fiDLL;

            var folders = di.GetDirectories();

            foreach (var folder in folders)
            {
                fiDLL = SearchAssemblyFile(folder.FullName, dllName);
                if (fiDLL != null)
                    return fiDLL;
            }

            return fiDLL;
        }

        Assembly Assembly_Resolver(object sender, ResolveEventArgs args)
        {
            FileInfo fiDLL = null;
            string dllName = (args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "")) + ".dll";

            var folders = _Config.StartupConfig.DllResolverFolder.SplitToList(";");

            foreach (var folder in folders)
            {
                fiDLL = SearchAssemblyFile(folder, dllName);
                if (fiDLL == null)
                    continue;
                break;
            }
            if (fiDLL == null)
                return null;
            return Assembly.LoadFile(fiDLL.FullName);
        }

        void Load_Assembly_Resolvers()
        {
            var method = this.GetType().GetMethod(nameof(Assembly_Resolver));
            if (!IsAssemblyResolveHandlerRegistered(method))
                AppDomain.CurrentDomain.AssemblyResolve += Assembly_Resolver;
        }

        void Unload_Assembly_Resolvers()
        {
            var method = this.GetType().GetMethod(nameof(Assembly_Resolver));
            if (IsAssemblyResolveHandlerRegistered(method))
                AppDomain.CurrentDomain.AssemblyResolve -= Assembly_Resolver;
        }

        bool IsAssemblyResolveHandlerRegistered(MethodInfo method)
        {
            FieldInfo field = typeof(AppDomain).GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                Delegate eventDelegate = (Delegate)field.GetValue(AppDomain.CurrentDomain);

                if (eventDelegate != null)
                {
                    foreach (Delegate handler in eventDelegate.GetInvocationList())
                    {
                        if (handler.Method == method)
                            return true;
                    }
                }
            }

            return false;
        }

    }
}

