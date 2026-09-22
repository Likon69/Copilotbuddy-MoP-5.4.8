using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Media;

using Styx.Common;
using Styx.Common.Compiler;
using Styx.Helpers;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class QuestBehaviorHelper
    {
        private static TypeOnlyLoader<CustomForcedBehavior> _behaviorTypes;

        public QuestBehaviorHelper(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }

        private static string QuestBehaviorsDirectory
        {
            get
            {
                string path = GlobalSettings.Instance.QuestBehaviorsPath;
                if (!System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.Combine(Utilities.AssemblyDirectory, path);
                return path;
            }
        }

        public static Type GetBehaviorInstanceType(string path)
        {
            if (_behaviorTypes == null)
            {
                var cc = new Styx.Common.Compiler.CodeCompiler(QuestBehaviorsDirectory);
                CompilerResults results = cc.Compile();

                foreach (CompilerError error in results.Errors)
                {
                    if (error.IsWarning)
                    {
                        Logging.WriteVerbose("[Compiler Warning]: " + error);
                    }
                    else
                    {
                        Logging.Write(Colors.Red, "[Compiler Error]: " + error);
                    }
                }
                if (results.Errors.HasErrors)
                {
                    throw new ProfileException("Errors in quest behaviors!");
                }

                _behaviorTypes = new TypeOnlyLoader<CustomForcedBehavior>(cc.CompiledAssembly);

            }

            foreach (Type bt in _behaviorTypes)
            {
                if (
                    bt.GetCustomAttributes(typeof (CustomBehaviorFileNameAttribute), false)
                      .Any(a => (a as CustomBehaviorFileNameAttribute).FileName.ToLower() == path.ToLower()))
                {
                    return bt;
                }
            }

            return null;
        }

        public static Type GetBehaviorUrlInstanceType(string path, string url)
        {
            try
            {
                string fileData = new WebClient().DownloadString(url);
                Uri uri = new Uri(url);
                string fileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (fileName != null) fileName = fileName.Replace(".", "Url.");
                string filePath = System.IO.Path.Combine(Utilities.AssemblyDirectory, "Cache", fileName);

                File.WriteAllText(filePath, fileData);


                TypeOnlyLoader<CustomForcedBehavior> toRemove = new TypeOnlyLoader<CustomForcedBehavior>();
                toRemove.Clear();


                toRemove.AddRange(_behaviorTypes.Where(bt => bt.GetCustomAttributes(typeof (CustomBehaviorFileNameAttribute), false).Any(a => (a as CustomBehaviorFileNameAttribute).FileName.ToLower() == path.ToLower())));

                _behaviorTypes.RemoveAll(toRemove.Contains);

                var cc = new Styx.Common.Compiler.CodeCompiler(filePath);
                CompilerResults results = cc.Compile();

                foreach (CompilerError error in results.Errors)
                {
                    if (error.IsWarning)
                    {
                        Logging.WriteVerbose("[Compiler Warning]: " + error);
                    }
                    else
                    {
                        Logging.Write(Colors.Red, "[Compiler Error]: " + error);
                    }
                }
                if (results.Errors.HasErrors)
                {
                    throw new ProfileException("Errors in quest behaviors!");
                }

                TypeOnlyLoader<CustomForcedBehavior> toAdd = new TypeOnlyLoader<CustomForcedBehavior>(cc.CompiledAssembly);

                foreach (Type bt in toAdd)
                {
                    _behaviorTypes.Add(bt);
                }

                File.Delete(System.IO.Path.Combine(Utilities.AssemblyDirectory, "Cache", fileName));
            }
            catch (Exception ex)
            {
                Logging.Write("There was an error requesting the Behavior resource at " + url);
                Logging.WriteException(ex);
            }



            foreach (Type bt in _behaviorTypes)
            {
                if (
                    bt.GetCustomAttributes(typeof (CustomBehaviorFileNameAttribute), false)
                        .Any(a => (a as CustomBehaviorFileNameAttribute).FileName.ToLower() == path.ToLower()))
                {
                    return bt;
                }
            }

            return null;
        }
    }
}
