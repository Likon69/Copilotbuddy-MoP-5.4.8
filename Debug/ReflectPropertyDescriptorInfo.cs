using System;
using System.Globalization;

namespace CopilotBuddy.Debug
{
    public sealed class ReflectPropertyDescriptorInfo : IEquatable<ReflectPropertyDescriptorInfo>, IComparable<ReflectPropertyDescriptorInfo>
    {
        private readonly string m_typeName;

        private readonly string m_propertyName;

        private readonly int m_handlerCount;

        public string TypeName => m_typeName;

        public string PropertyName => m_propertyName;

        public int HandlerCount => m_handlerCount;

        public string DisplayHandlerCount
        {
            get
            {
                if (m_handlerCount != 1)
                {
                    return string.Format(CultureInfo.InvariantCulture, " ({0:n0} handlers)", new object[1] { m_handlerCount });
                }
                return "";
            }
        }

        public ReflectPropertyDescriptorInfo(string typeName, string propertyName, int handlerCount)
        {
            m_typeName = typeName;
            m_propertyName = propertyName;
            m_handlerCount = handlerCount;
        }

        public int CompareTo(ReflectPropertyDescriptorInfo other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }
            int num = m_typeName.CompareTo(other.m_typeName);
            if (num == 0)
            {
                num = m_propertyName.CompareTo(other.m_propertyName);
            }
            if (num == 0)
            {
                num = m_handlerCount.CompareTo(other.m_handlerCount);
            }
            return num;
        }

        public bool Equals(ReflectPropertyDescriptorInfo other)
        {
            if (other.m_typeName == m_typeName)
            {
                return other.m_propertyName == m_propertyName;
            }
            return false;
        }
    }
}
