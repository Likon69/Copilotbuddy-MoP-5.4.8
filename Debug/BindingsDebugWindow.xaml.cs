using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using MahApps.Metro.Controls;

namespace CopilotBuddy.Debug
{
    public partial class BindingsDebugWindow : MetroWindow
    {
        public static readonly DependencyProperty ReflectPropertiesProperty = DependencyProperty.Register("ReflectProperties", typeof(ReadOnlyCollection<ReflectPropertyDescriptorInfo>), typeof(BindingsDebugWindow), new PropertyMetadata());

        public ReadOnlyCollection<ReflectPropertyDescriptorInfo> ReflectProperties
        {
            get
            {
                return (ReadOnlyCollection<ReflectPropertyDescriptorInfo>)GetValue(ReflectPropertiesProperty);
            }
            set
            {
                SetValue(ReflectPropertiesProperty, value);
            }
        }

        public BindingsDebugWindow()
        {
            InitializeComponent();
            ReflectProperties = GetReflectPropertyDescriptorInfo();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ReflectProperties = GetReflectPropertyDescriptorInfo();
        }

        private static ReadOnlyCollection<ReflectPropertyDescriptorInfo> GetReflectPropertyDescriptorInfo()
        {
            List<ReflectPropertyDescriptorInfo> list = new List<ReflectPropertyDescriptorInfo>();
            Type type = typeof(PropertyDescriptor).Module.GetType("System.ComponentModel.ReflectTypeDescriptionProvider");
            FieldInfo field = type.GetField("_propertyCache", BindingFlags.Static | BindingFlags.NonPublic);
            Hashtable hashtable = (Hashtable)field.GetValue(null);
            if (hashtable != null)
            {
                DictionaryEntry[] array = new DictionaryEntry[hashtable.Count];
                hashtable.CopyTo(array, 0);
                FieldInfo field2 = typeof(PropertyDescriptor).GetField("valueChangedHandlers", BindingFlags.Instance | BindingFlags.NonPublic);
                DictionaryEntry[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    DictionaryEntry dictionaryEntry = array2[i];
                    PropertyDescriptor[] array3 = (PropertyDescriptor[])dictionaryEntry.Value;
                    if (array3 == null)
                    {
                        continue;
                    }
                    PropertyDescriptor[] array4 = array3;
                    foreach (PropertyDescriptor propertyDescriptor in array4)
                    {
                        Hashtable hashtable2 = (Hashtable)field2.GetValue(propertyDescriptor);
                        if (hashtable2 != null && hashtable2.Count != 0)
                        {
                            list.Add(new ReflectPropertyDescriptorInfo(dictionaryEntry.Key.ToString(), propertyDescriptor.Name, hashtable2.Count));
                        }
                    }
                }
            }
            list.Sort();
            return list.AsReadOnly();
        }
    }
}
