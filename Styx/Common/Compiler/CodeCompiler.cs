using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Styx.Common.Compiler
{
    /// <summary>
    /// This class handles compiling any code that we need to do dynamically. CCs, plugins, etc.
    /// </summary>

    public class CodeCompiler
    {
        private static string CompiledAssemblyPath
        {
            get { return Path.Combine(Utilities.AssemblyDirectory, "CompiledAssemblies"); }
        }

        public static void DeleteOldAssemblies()
        {
            if (!Directory.Exists(CompiledAssemblyPath))
                Directory.CreateDirectory(CompiledAssemblyPath);
            else
                foreach (var file in Directory.GetFiles(CompiledAssemblyPath))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.CreationTime < DateTime.Now.Subtract(TimeSpan.FromHours(1)))
                            File.Delete(file);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Don't do anything. We're trying to delete a file that's in use!
                    }
                }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CodeCompiler"/> class.
        /// </summary>
        /// <param name="path"></param>
        public CodeCompiler(string path)
        {
            DeleteOldAssemblies();
            CompilerVersion = 4.0f;
            SourceFilePaths = new List<string>();

            if (File.Exists(path))
                FileStructure = FileStructureType.SingleFile;
            else if (Directory.Exists(path))
                FileStructure = FileStructureType.Folder;

            SourcePath = path;

            Options = new CompilerParameters();
            Options.GenerateExecutable = false;
            Options.GenerateInMemory = false;
            Options.IncludeDebugInformation = true;
            // Allow unsafe code.
            string extraversiondefine = "HB_" + Assembly.GetEntryAssembly().GetName().Version.Revision;
            Options.CompilerOptions = string.Format("/d:HB;{0} /unsafe", extraversiondefine);
            // Just create this stuff in the /Temp/ folder. Wherever that happens to be. This stuff *should* be cleaned up by
            // the user. However, we'll make sure we clean it up as well.
            Options.TempFiles = new TempFileCollection(Path.GetTempPath());

            Options.OutputAssembly = Path.Combine(CompiledAssemblyPath, AssemblyName);
            CompiledToLocation = Options.OutputAssembly;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (!asm.IsDynamic)
                        AddReference(asm.Location);
                }
                catch (NotSupportedException)
                {
                    // Ignore theese ones, happens when reloading if a soruce file changes.
                    continue;
                }
            }

            AddReference(Path.Combine(Utilities.AssemblyDirectory, "Tripper.Tools.dll"));
        }

        /// <summary>
        /// The final, compiled assembly.
        /// </summary>
        public Assembly CompiledAssembly { get; private set; }

        /// <summary>
        /// The path to the source files. This can be a directory, or a single file. See <see cref="CodeCompiler.FileStructure"/> for the current type.
        /// </summary>
        public string SourcePath { get; private set; }

        /// <summary>
        /// The type of source structure this compiler uses. File, folder, etc.
        /// </summary>
        public FileStructureType FileStructure { get; private set; }

        /// <summary>
        /// Retrieves the current compiler options.
        /// </summary>
        public CompilerParameters Options { get; private set; }

        /// <summary>
        /// The compiler version. [Default: 3.5]
        /// </summary>
        public float CompilerVersion { get; private set; }

        private long Ticks = DateTime.Now.Ticks;

        /// <summary>
        /// The name of the assembly on disk.
        /// </summary>
        public string AssemblyName
        {
            get
            {
                return string.Format(
                    "{0}_{1}.dll",
                    FileStructure == FileStructureType.SingleFile
                        ? Path.GetFileNameWithoutExtension(SourcePath)
                        : new DirectoryInfo(SourcePath).Name,
                    Ticks);
            }
        }

        public string CompiledToLocation { get; private set; }

        public List<string> SourceFilePaths { get; private set; }

        public void AddReference(string assembly)
        {
            if (!Options.ReferencedAssemblies.Contains(assembly))
                Options.ReferencedAssemblies.Add(assembly);
        }

        private void AddEmbeddedResource(string path)
        {
            string resPath = Path.ChangeExtension(path, ".resources");
            if (File.Exists(resPath))
            {
                File.Delete(resPath);
            }
            using (ResXResourceReader reader = new ResXResourceReader(path))
            {
                reader.BasePath = SourcePath;

                using (ResourceWriter writer = new ResourceWriter(resPath))
                {
                    foreach (DictionaryEntry o in reader)
                    {
                        writer.AddResource(o.Key.ToString(), o.Value);
                    }
                }
            }

            Options.EmbeddedResources.Add(resPath);
        }

        private void RetrieveAllFiles()
        {
            if (FileStructure == FileStructureType.Folder)
            {
                foreach (string f in Directory.GetFiles(SourcePath, "*.cs", SearchOption.AllDirectories))
                    SourceFilePaths.Add(f);

                foreach (string resx in Directory.GetFiles(SourcePath, "*.resx", SearchOption.AllDirectories))
                    AddEmbeddedResource(resx);
            }
            else
                SourceFilePaths.Add(SourcePath);
        }

        private bool AssemblyIsLoaded(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.Contains(name.Replace(".dll", string.Empty)));
        }

        public void ParseFilesForCompilerOptions()
        {
            foreach (string src in SourceFilePaths)
            {
                // This function currently allows the following compiler options to be set:

                // Allows reference inclusions in the compilation process.
                //!CompilerOption:AddRef:MyAwesomeDll.dll

                // Allows the developer to enable code optimisations
                //!CompilerOption:Optimise:On

                // Allows the developer to attempt to force the compiler to use the v4 framework compiler to allow the C# 4.0 changes to be used.
                //!CompilerOption:Version:v4.0

                string[] lines = File.ReadAllLines(src);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("//!CompilerOption:"))
                    {
                        string[] options = trimmed.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                        switch (options[1])
                        {
                            case "AddRef":
                                // If the developer needs extra assemblies referenced (SQLite, MySQL, IRC, ZIP libs, etc) let them do it.
                                // Just make sure the assembly is on the list of approved assemblies!
                                // TODO: Check the reference assembly against our list of 'marked-as-safe' assemblies.
                                if (options.Length == 3 && !string.IsNullOrEmpty(options[2]) && options[2].EndsWith(".dll") && !AssemblyIsLoaded(options[2]))
                                    AddReference(options[2]);
                                break;
                            case "Optimise":
                                // Just optimise the assembly if they want it optimised. Fairly simple...
                                if (options.Length == 3 && !string.IsNullOrEmpty(options[2]) && options[2] == "On" &&
                                    !Options.CompilerOptions.Contains("/optimise"))
                                {
                                    // If they want it optimised, they don't need any debug info.
                                    Options.IncludeDebugInformation = false;
                                    Options.CompilerOptions += " /optimise";
                                }
                                break;
                        }
                    }
                }
            }
        }

        public CompilerResults Compile()
        {
            // Make sure we have all the files we need to compile this bitch.
            RetrieveAllFiles();

            // Check for additional dependency references
            ParseFilesForCompilerOptions();

            if (SourceFilePaths.Count != 0)
            {
                var parseOptions = new CSharpParseOptions(preprocessorSymbols: ParsePreprocessorSymbols(Options.CompilerOptions));

                var syntaxTrees = SourceFilePaths
                    .Select(path => CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(path), Encoding.UTF8), parseOptions, path))
                    .ToList();

                var references = Options.ReferencedAssemblies
                    .Cast<string>()
                    .Where(reference => !string.IsNullOrEmpty(reference) && File.Exists(reference))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(reference => (MetadataReference)MetadataReference.CreateFromFile(reference))
                    .ToList();

                var compilationOptions = new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: Options.CompilerOptions != null && Options.CompilerOptions.Contains("/unsafe"),
                    optimizationLevel: Options.IncludeDebugInformation ? OptimizationLevel.Debug : OptimizationLevel.Release);

                var compilation = CSharpCompilation.Create(
                    Path.GetFileNameWithoutExtension(Options.OutputAssembly),
                    syntaxTrees,
                    references,
                    compilationOptions);

                CompilerResults ret = new CompilerResults(Options.TempFiles) { PathToAssembly = Options.OutputAssembly };

                EmitResult emitResult;
                if (Options.IncludeDebugInformation)
                {
                    string pdbPath = Path.ChangeExtension(Options.OutputAssembly, ".pdb");
                    using (FileStream assemblyStream = File.Create(Options.OutputAssembly))
                    using (FileStream pdbStream = File.Create(pdbPath))
                        emitResult = compilation.Emit(assemblyStream, pdbStream);
                }
                else
                {
                    using (FileStream assemblyStream = File.Create(Options.OutputAssembly))
                        emitResult = compilation.Emit(assemblyStream);
                }

                foreach (Diagnostic diagnostic in emitResult.Diagnostics)
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Error && diagnostic.Severity != DiagnosticSeverity.Warning)
                        continue;

                    FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
                    ret.Errors.Add(
                        new CompilerError(
                            span.Path ?? string.Empty,
                            span.StartLinePosition.Line + 1,
                            span.StartLinePosition.Character + 1,
                            diagnostic.Id,
                            diagnostic.GetMessage(CultureInfo.InvariantCulture))
                        {
                            IsWarning = diagnostic.Severity == DiagnosticSeverity.Warning
                        });
                }

                if (emitResult.Success)
                {
                    CompiledAssembly = Assembly.LoadFrom(Options.OutputAssembly);
                    ret.CompiledAssembly = CompiledAssembly;
                }

                // Clean up after ourselves.
                ret.TempFiles.Delete();

                // Only return the errors. The 'outside world' is scary, and we don't want them to know our sekretz!
                return ret;
            }

            return null;
        }

        private static IEnumerable<string> ParsePreprocessorSymbols(string compilerOptions)
        {
            if (string.IsNullOrEmpty(compilerOptions))
                yield break;

            foreach (string token in compilerOptions.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!token.StartsWith("/d:", StringComparison.OrdinalIgnoreCase) &&
                    !token.StartsWith("/define:", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (string symbol in token.Substring(token.IndexOf(':') + 1).Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    yield return symbol;
            }
        }

        #region FileStructureType enum

        public enum FileStructureType
        {
            SingleFile,
            Folder
        }

        #endregion
    }
}
