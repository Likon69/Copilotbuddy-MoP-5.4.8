using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeCompiler = Styx.Common.Compiler.CodeCompiler;

namespace Styx.Common
{
    public class AssemblyLoader<T>
    {
        private readonly bool _detectFileChanges;
        private readonly string _directory;
        private readonly FileSystemWatcher _fileWatcher = new FileSystemWatcher();

        private DateTime _throttle;

        public AssemblyLoader(string directory, bool detectFileChanges)
        {
            Instances = new List<T>();
            _directory = directory;
            _detectFileChanges = detectFileChanges;
            if (_detectFileChanges)
            {
                _fileWatcher.Path = directory;
                _fileWatcher.Filter = "*.cs";
                _fileWatcher.IncludeSubdirectories = true;
                _fileWatcher.EnableRaisingEvents = true;
                _fileWatcher.Changed += (o, e) => Reload(e.ChangeType.ToString());
                _fileWatcher.Created += (s, e) => Reload(e.ChangeType.ToString());
                _fileWatcher.Deleted += (s, e) => Reload(e.ChangeType.ToString());
            }
        }

        public List<T> Instances { get; private set; }
        public event EventHandler Reloaded;

        public void Reload(string reason)
        {
            try
            {
                if (_detectFileChanges)
                    _fileWatcher.EnableRaisingEvents = false;

                if ((DateTime.Now - _throttle).TotalSeconds < 5.0)
                {
                    return;
                }

                _throttle = DateTime.Now;

                Logging.WriteDiagnostic("Reloading AssemblyLoader<{0}> - {1}", typeof(T).Name, reason);
                Instances = new List<T>();

                foreach (string dir in Directory.GetDirectories(_directory))
                {
                    try
                    {
                        CodeCompiler compiler = new CodeCompiler(dir);
                        CompilerResults results = compiler.Compile();
                        if (results == null)
                            continue;

                        if (results.Errors.HasErrors && !results.Errors.Cast<CompilerError>().All(e => e.IsWarning))
                        {
                            foreach (CompilerError error in results.Errors.Cast<CompilerError>())
                            {
                                if (error.IsWarning)
                                    continue;

                                Logging.Write("Compiler Error: " + error);
                            }
                            continue;
                        }

                        Instances.AddRange(new TypeLoader<T>(compiler.CompiledAssembly));
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                    }
                }

                if (Reloaded != null)
                {
                    Reloaded(this, null);
                }
            }
            finally
            {
                if (_detectFileChanges)
                    _fileWatcher.EnableRaisingEvents = true;
            }
        }
    }
}
