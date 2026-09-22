using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Styx.Common.Compiler;
using Styx.Resources;

namespace Styx.Loaders
{
    public class DynamicLoader<T> : List<T>
    {
        public string Path { get; private set; }

        public bool IsDirectory { get; private set; }

        public CompilerResults CompilerResults { get; private set; }

        public DynamicLoader(string path, bool compileSource, params object[] constructorArgs)
        {
            this.Path = path;
            if (Directory.Exists(path))
            {
                this.IsDirectory = true;
            }
            else if (!File.Exists(path))
            {
                throw new FileNotFoundException(Styx.Resources.StyxResources.Invalid_path_specified_in_DynamicLoader_constructor_, path);
            }
            if (!compileSource)
            {
                base.AddRange(this.IsDirectory ? new DllLoader<T>(path, constructorArgs) : new DllLoader<T>(global::System.IO.Path.GetDirectoryName(path), constructorArgs));
                return;
            }
            Styx.Common.Compiler.CodeCompiler codeCompiler = new Styx.Common.Compiler.CodeCompiler(path);
            this.CompilerResults = codeCompiler.Compile();
            if (this.CompilerResults == null)
            {
                return;
            }
            IEnumerable<CompilerError> compilerErrors = this.CompilerResults.Errors.Cast<CompilerError>();
            if (compilerErrors.Any(new Func<CompilerError, bool>(DynamicLoader<T>.IsCompilerError)))
            {
                return;
            }
            base.AddRange(new DllLoader<T>(codeCompiler.CompiledAssembly, new object[0]));
        }

        private static bool IsCompilerError(CompilerError compilerError)
        {
            return !compilerError.IsWarning;
        }
    }
}
