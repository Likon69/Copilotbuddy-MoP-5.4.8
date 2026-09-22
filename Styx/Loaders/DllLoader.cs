using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using Styx.Common;

namespace Styx.Loaders
{
    public class DllLoader<T> : List<T>
    {
        public DllLoader(string path, params object[] constructorArgs)
        {
            string[] assemblyPaths = (Directory.Exists(path) ? Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories) : new string[] { path });
            foreach (string assemblyPath in assemblyPaths)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    this.LoadTypesFromAssembly(assembly, constructorArgs);
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        public DllLoader(Assembly asm, params object[] constructorArgs)
        {
            this.LoadTypesFromAssembly(asm, constructorArgs);
        }

        internal void LoadTypesFromAssembly(Assembly assembly, params object[] constructorArgs)
        {
            bool isInterface = typeof(T).IsInterface;
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    try
                    {
                        if (type.IsClass)
                        {
                            if (isInterface && type.GetInterfaces().Contains(typeof(T)))
                            {
                                base.Add((T)((object)Activator.CreateInstance(type, constructorArgs)));
                            }
                            else if (type.IsSubclassOf(typeof(T)))
                            {
                                base.Add((T)((object)Activator.CreateInstance(type, constructorArgs)));
                            }
                        }
                    }
                    catch (TargetInvocationException exception)
                    {
                        Styx.Common.Logging.WriteDiagnostic("Could not construct instance of {0}! Exception was thrown. Exception:", new object[] { type.Name });
                        Styx.Common.Logging.WriteDiagnostic((exception.InnerException == null) ? "No inner exception" : exception.InnerException.Message);
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        Styx.Common.Logging.WriteException(exception);
                        Styx.Common.Logging.Write(Colors.Red, "LoaderExceptions:");
                        foreach (Exception loaderException in exception.LoaderExceptions)
                        {
                            Styx.Common.Logging.WriteException(loaderException);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Styx.Common.Logging.WriteException(exception);
                if (exception is ReflectionTypeLoadException)
                {
                    Styx.Common.Logging.Write(Colors.Red, "LoaderExceptions:");
                    foreach (Exception loaderException in (exception as ReflectionTypeLoadException).LoaderExceptions)
                    {
                        Styx.Common.Logging.WriteException(loaderException);
                    }
                }
            }
        }
    }
}
