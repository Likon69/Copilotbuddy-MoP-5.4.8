using System;

namespace Styx.CommonBot.CharacterManagement
{
    public class ClassProfileLoadException : Exception
    {
        public ClassProfileLoadException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        public ClassProfileLoadException(string filePath, string additionalInfo = null, Exception innerException = null)
            : this(FormatMessage(filePath, additionalInfo), innerException)
        {
            this.FilePath = filePath;
        }

        public string FilePath { get; private set; }

        private static string FormatMessage(string filePath, string additionalInfo)
        {
            if (string.IsNullOrWhiteSpace(additionalInfo))
            {
                return string.Format("Failed to load class profile '{0}'", filePath);
            }
            return string.Format("Failed to load class profile '{0}': {1}", filePath, additionalInfo);
        }
    }
}
