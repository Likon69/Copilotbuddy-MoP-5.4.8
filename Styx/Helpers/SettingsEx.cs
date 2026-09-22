using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Styx.SAAttribs;
using Styx.XmlEngine;

namespace Styx.Helpers
{
    [DoNotCaptureVariables]
    public abstract class SettingsEx : IDisposable
    {
        private readonly string _settingsPath;

        public static string SettingsDirectory => Settings.SettingsDirectory;

        public static string CharacterSettingsDirectory => Settings.CharacterSettingsDirectory;

        public void Dispose()
        {
            Save();
        }

        protected SettingsEx(string settingsPath)
        {
            _settingsPath = settingsPath;
            Load();
        }

        public void Load()
        {
            if (File.Exists(_settingsPath))
            {
                new Styx.XmlEngine.XmlEngine().Load(this, XElement.Load(_settingsPath));
            }
        }

        public void SaveToFile(string file)
        {
            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }
            new Styx.XmlEngine.XmlEngine().Save(this).Save(file);
        }

        public void Save()
        {
            SaveToFile(_settingsPath);
        }

        public Dictionary<string, object> GetSettings()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            Type type = GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.SetProperty);
            foreach (PropertyInfo propertyInfo in properties)
            {
                List<object> list = propertyInfo.GetCustomAttributes(typeof(XmlElementAttribute), inherit: false).Union(propertyInfo.GetCustomAttributes(typeof(XmlAttributeAttribute), inherit: false)).ToList();
                if (list.Count != 0)
                {
                    object value = propertyInfo.GetValue(this, null);
                    string key = ((INamedAttribute)list[0]).Name ?? propertyInfo.Name;
                    dictionary.Add(key, value);
                }
            }
            return dictionary;
        }
    }
}
