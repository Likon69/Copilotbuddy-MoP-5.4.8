using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.Helpers
{
    public abstract class Settings
    {
        #region Constructor

        [Browsable(false)]
        public string SettingsPath { get; private set; }

        protected Settings(string settingsPath, string oldSettingsPath = null)
        {
            if (oldSettingsPath != null && File.Exists(oldSettingsPath) && !File.Exists(settingsPath))
            {
                if (!Directory.Exists(Path.GetDirectoryName(settingsPath)))
                {
                    Directory.CreateDirectory(settingsPath);
                }
                File.Move(oldSettingsPath, settingsPath);
            }

            SettingsPath = settingsPath;
            InitializeDefaultValues();

            if (!File.Exists(SettingsPath))
            {
                SaveToFile(SettingsPath);
                return;
            }

            LoadFromXML(XElement.Load(SettingsPath));
        }

        #endregion

        #region Instance Methods

        public static string SettingsDirectory
        {
            get
            {
                string ret = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings");
                if (!Directory.Exists(ret))
                {
                    Directory.CreateDirectory(ret);
                }
                return ret;
            }
        }

        public static string CharacterSettingsDirectory
        {
            get
            {
                try
                {
                    string fet = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings");
                    string ret = Path.Combine(Path.Combine(fet, StyxWoW.Me.RealmName), StyxWoW.Me.Name);
                    if (!Directory.Exists(ret))
                    {
                        Directory.CreateDirectory(ret);
                    }
                    return ret;
                }
                catch (Exception ex)
                {
                    Logging.WriteException(ex);
                    return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Settings");
                }
            }
        }

        public Dictionary<string, object> GetSettings()
        {
            var settingValues = new Dictionary<string, object>();
            Type t = GetType();
            PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.SetProperty);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];

                object[] settingAttributes = prop.GetCustomAttributes(typeof(SettingAttribute), false);
                if (settingAttributes == null || settingAttributes.Length <= 0)
                    continue;

                object val = prop.GetValue(this, null);

                string settingName = ((SettingAttribute)settingAttributes[0]).ElementName ?? prop.Name;

                settingValues.Add(settingName, val);
            }

            return settingValues;
        }

        public void InitializeDefaultValues()
        {
            Type t = GetType();
            PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.SetProperty);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];

                object[] defaultValueAttribs = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false);
                object[] encryptedSettingsAttibs = prop.GetCustomAttributes(typeof(EncryptedAttribute), false);
                if (defaultValueAttribs == null || defaultValueAttribs.Length <= 0)
                    continue;

                var attrib = (DefaultValueAttribute)defaultValueAttribs[0];

                if (encryptedSettingsAttibs != null && encryptedSettingsAttibs.Length != 0)
                {
                    string valueToSet = "";

                    var encryptedAttrib = (EncryptedAttribute)encryptedSettingsAttibs[0];
                    var value = (string)attrib.Value;

                    if (!string.IsNullOrEmpty(value))
                    {
                        var encrypted = encryptedAttrib.Encrypt((string)attrib.Value);
                        if (encrypted != null && encrypted.Length == 32)
                            valueToSet = encryptedAttrib.Decrypt(encrypted);
                        else
                            valueToSet = (string)attrib.Value;
                    }

                    prop.SetValue(this, valueToSet, null);
                }
                else
                {
                    try
                    {
                        prop.SetValue(this, attrib.Value, null);
                    }
                    catch (TargetException)
                    {
                        Logging.WriteDiagnostic("Could not set property {0} to {1} (DefaultValueAttribute)", prop.Name,
                                           attrib.Value == null ? "null" : attrib.Value.ToString());
                    }
                }
            }
        }

        public void SaveToFile(string path)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                GetXML().Save(path);
            }
            catch (IOException e)
            {
                string message = e.Message;
                if (StyxWoW.Me != null && StyxWoW.Me.IsValid)
                {
                    message = message.Replace(StyxWoW.Me.Name, StyxWoW.Me.Name.First() + "*****" + StyxWoW.Me.Name.Last());
                }
                Logging.Write(StyxResources.Error_trying_to_save_the_settings_file___0_, message);
            }
        }

        public XElement GetXML()
        {
            Type t = GetType();
            PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.SetProperty);
            var root = new XElement(t.Name);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                object value = prop.GetValue(this, null);
                if (value == null)
                    continue;

                object[] saveAttribs = prop.GetCustomAttributes(typeof(SettingAttribute), false);
                object[] encryptedSettingsAttibs = prop.GetCustomAttributes(typeof(EncryptedAttribute), false);
                if (saveAttribs == null || saveAttribs.Length <= 0)
                    continue;

                var attrib = (SettingAttribute)saveAttribs[0];
                string saveName = attrib.ElementName ?? prop.Name;
                if (!string.IsNullOrEmpty(attrib.Explanation))
                    root.Add(new XComment(attrib.Explanation));

                XElement elementToAdd;
                if (prop.PropertyType.IsArray)
                {
                    elementToAdd = new XElement(saveName);
                    var arr = (Array)value;
                    for (int j = 0; j < arr.Length; j++)
                        elementToAdd.Add(new XElement("Entry", arr.GetValue(j).ToString()));
                }
                else if (encryptedSettingsAttibs != null && encryptedSettingsAttibs.Length != 0)
                {
                    if (!string.IsNullOrEmpty((string)value))
                    {
                        var encryptedAttrib = (EncryptedAttribute)encryptedSettingsAttibs[0];
                        elementToAdd = new XElement(saveName, Convert.ToBase64String(encryptedAttrib.Encrypt((string)value)));
                    }
                    else
                    {
                        elementToAdd = new XElement(saveName, value.ToString());
                    }
                }
                else
                    elementToAdd = new XElement(saveName, value.ToString());

                root.Add(elementToAdd);
            }

            return root;
        }

        public void LoadFromXML(XElement root)
        {
            if (root == null)
                throw new ArgumentNullException("root");

            Type t = GetType();
            if (root.Name != t.Name)
                throw new ArgumentException(string.Format(StyxResources.The_settings_file_does_not_match_the_type_that_is_being_loaded__Root_name_is___0____expected___1___, root.Name, t.Name), "root");

            PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.SetProperty);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo prop = properties[i];
                object[] saveAttribs = prop.GetCustomAttributes(typeof(SettingAttribute), false);
                object[] encryptedSettingsAttibs = prop.GetCustomAttributes(typeof(EncryptedAttribute), false);
                if (saveAttribs == null || saveAttribs.Length <= 0)
                    continue;

                var attrib = (SettingAttribute)saveAttribs[0];
                string saveName = attrib.ElementName ?? prop.Name;
                XElement elm = root.Element(saveName);
                if (elm == null)
                    continue;

                string value = elm.Value;

                object objValue;
                if (prop.PropertyType.IsArray)
                {
                    Type arrayType = prop.PropertyType.GetElementType();
                    XElement[] subElements = elm.Elements("Entry").ToArray();
                    var objList = new List<object>();
                    for (int j = 0; j < subElements.Length; j++)
                    {
                        XElement arrayElm = subElements[j];

                        object subElementObjValue = ConvertType(arrayElm.Value, arrayType, string.Format(StyxResources.Entry_in__0_, elm.Name));

                        if (subElementObjValue == null)
                            continue;

                        objList.Add(subElementObjValue);
                    }

                    Array arr = Array.CreateInstance(arrayType, objList.Count);
                    for (int j = 0; j < objList.Count; j++)
                        arr.SetValue(objList[j], j);

                    objValue = arr;
                }
                else if (encryptedSettingsAttibs != null && encryptedSettingsAttibs.Length != 0 && !string.IsNullOrEmpty(value))
                {
                    string plaintextValue = value;
                    var encryptedAttrib = (EncryptedAttribute)encryptedSettingsAttibs[0];
                    objValue = encryptedAttrib.Decrypt(Convert.FromBase64String(plaintextValue));
                }
                else
                {
                    objValue = ConvertType(value, prop.PropertyType, elm.Name.ToString());
                }

                prop.SetValue(this, objValue, null);
            }
        }

        public void Save()
        {
            SaveToFile(SettingsPath);
        }

        public void Load()
        {
            if (File.Exists(SettingsPath))
            {
                using (TextReader tr = new StreamReader(SettingsPath, Encoding.UTF8, true))
                {
                    LoadFromXML(XElement.Load(tr));
                }
            }
        }

        #endregion

        #region Static Methods

        public static T LoadFromXML<T>(XElement root) where T : Settings, new()
        {
            var settings = new T();
            settings.LoadFromXML(root);
            return settings;
        }

        public static T LoadFromFile<T>(string path) where T : Settings, new()
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(StyxResources.Could_not_find_the_settings_path_, path);

            return LoadFromXML<T>(XElement.Load(path));
        }

        private static object ConvertType(string val, Type t, string elmName)
        {
            try
            {
                Type underlyingType = Nullable.GetUnderlyingType(t);
                if (underlyingType != null)
                    return ConvertType(val, underlyingType, elmName);

                return t.IsEnum ? Enum.Parse(t, val) : Convert.ChangeType(val, t);
            }
            catch (InvalidCastException)
            {
                Logging.Write(StyxResources.Setting__0__has_invalid_value___1___, elmName, val);
            }

            return null;
        }

        #endregion
    }
}
