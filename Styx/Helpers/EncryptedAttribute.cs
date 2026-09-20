using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Styx.Common;

namespace Styx.Helpers
{
    /// <summary>
    /// Encrypts a setting using passed in key and IV.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EncryptedAttribute : SettingAttribute
    {
        private readonly RijndaelManaged _myRijndael;

        private string _key, _iv;

        /// <summary>Constructor. </summary>
        /// <remarks>Created 8/6/2011.</remarks>
        /// <param name="key">A base64 encoded string representing the Key to use for the cipher.</param>
        /// <param name="iv">A base64 encoded string representing the IV to use for the cipher.</param>
        public EncryptedAttribute(string key, string iv)
        {
            _key = key;
            _iv = iv;
            _myRijndael = new RijndaelManaged
            {
                Key = Convert.FromBase64String(key),
                IV = Convert.FromBase64String(iv),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.ISO10126
            };
        }

        public byte[] Encrypt(string stringToEncrypt)
        {
            return EncryptStringToBytesAES(stringToEncrypt, _myRijndael.Key, _myRijndael.IV);
        }

        public string Decrypt(byte[] encryptedData)
        {
            return DecryptStringFromBytesAES(encryptedData, _myRijndael.Key, _myRijndael.IV);
        }

        #region Instance Methods

        private byte[] EncryptStringToBytesAES(string plainText, byte[] Key, byte[] IV)
        {
            try
            {
                using (var crypto = _myRijndael.CreateEncryptor())
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var csCrypto = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                        {
                            csCrypto.Write(Encoding.ASCII.GetBytes(plainText), 0, Encoding.ASCII.GetBytes(plainText).Length);
                            csCrypto.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteDiagnostic("Error while encrypting setting: " + e.Message);
                Logging.WriteException(e);
                return null;
            }
        }

        private string DecryptStringFromBytesAES(byte[] cipherText, byte[] Key, byte[] IV)
        {
            try
            {
                using (var cs = new CryptoStream(new MemoryStream(cipherText), _myRijndael.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    var buffer = new byte[cipherText.Length];
                    // CryptoStream.Read returns as soon as it has data, so a single call truncates
                    // anything longer than the first block. .NET Framework filled the buffer outright.
                    int decryptedCount = 0, read;
                    while ((read = cs.Read(buffer, decryptedCount, buffer.Length - decryptedCount)) > 0)
                        decryptedCount += read;
                    var decryptedBytes = new byte[decryptedCount];
                    Buffer.BlockCopy(buffer, 0, decryptedBytes, 0, decryptedCount);
                    return Encoding.ASCII.GetString(decryptedBytes);
                }
            }
            catch (Exception)
            {
                return Convert.ToBase64String(cipherText);
            }
        }

        #endregion
    }
}
