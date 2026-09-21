using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Styx.Common
{
    public class TypeOnlyLoader<T> : List<Type>
    {
        private readonly Assembly _asm;
        public TypeOnlyLoader(Assembly asm = null)
        {
            _asm = asm;
            Reload();
        }

        public T CreateInstance(Type t, params object[] args)
        {
            return (T) Activator.CreateInstance(t, args);
        }

        private void Load(params Assembly[] assemblies)
        {
            foreach (Type t in assemblies.SelectMany(asm => asm.GetTypes().Where(t => !t.IsAbstract && (t.IsAssignableFrom(typeof(T)) || t.IsSubclassOf(typeof(T)) || t.GetInterfaces().Any(i => i == typeof(T))))))
            {
                // If we already have a class of this type, ignore it.
                if (this.Any(type =>
                                 {
                                     string typeFull = type.GetType().FullName;
                                     string tFull = t.FullName;
                                     return typeFull == tFull;
                                 }))
                    continue;
                Add(t);
            }
        }

        public void Reload()
        {
            Clear();
            if (_asm == null)
            {
                Load(AppDomain.CurrentDomain.GetAssemblies());
            }
            else
            {
                Load(_asm);
            }
        }
    }
}
