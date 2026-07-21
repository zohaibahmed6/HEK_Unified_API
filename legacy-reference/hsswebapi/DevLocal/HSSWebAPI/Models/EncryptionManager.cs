using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HSSWebAPI.Models
{
    public static class EncryptionManager
    {
        public static string GetDecryptedStringCSV(this string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText?.Trim()))
                return string.Empty;

            string[] lstDecStr = encryptedText.Split(',');




            try
            {
                for (int i = 0; i < lstDecStr.Length; i++)
                    lstDecStr[i] = Convert.FromBase64String(lstDecStr[i].Replace("_", "/").Replace("-", "+").Replace("£", "=")).DecryptStringFromBytes();

                return string.Join(",", lstDecStr);
            }
            catch (Exception ex)
            {

                //LogMeNow.Instance.Error(ex, "{FolderName}", "Error Log Encryption Layer");
            }

            return string.Empty;
        }


        private static RijndaelManaged myRijndael = new RijndaelManaged();
        static EncryptionManager()
        {
            //Generating key like below will cause issue when application is stopped and start again
            //whcih is why we have provided hard coded key, where as IV will be different is each case
            //myRijndael.GenerateKey();

            myRijndael.Key = new byte[32] { 137, 10, 237, 69, 157, 169, 181, 70, 216, 110, 47, 209, 193, 153, 196, 109, 25, 146, 165, 140, 128, 7, 175, 122, 34, 247, 157, 143, 54, 233, 124, 219 };

        }

        /// <summary>
        /// 8ms on average to execute
        /// </summary>
        /// <param name="clearText"></param>
        /// <returns></returns>
        public static string GetEncryptedString(this string clearText)
        {
            if (string.IsNullOrWhiteSpace(clearText?.Trim()))
                return string.Empty;

            try
            {
                return Convert.ToBase64String(clearText.EncryptStringToBytes()).Replace('/', '_').Replace('+', '-').Replace("=", "£");
            }
            catch (Exception ex)
            {

                //LogMeNow.Instance.Error(ex, "{FolderName}", "Error Log Encryption Layer");
            }

            return string.Empty;
        }

        /// <summary>
        /// 8ms on average to execute
        /// </summary>
        /// <param name="clearText"></param>
        /// <returns></returns>
        public static string GetEncryptedString(this int clearText)
        {
            try
            {
                return Convert.ToBase64String(clearText.ToString().EncryptStringToBytes()).Replace('/', '_').Replace('+', '-').Replace("=", "£");
            }
            catch (Exception ex)
            {
                //LogMeNow.Instance.Error(ex, "{FolderName}", "Error Log Encryption Layer");
            }

            return string.Empty;
        }

        public static string GetEncryptedString(this Int64 clearText)
        {
            try
            {
                return Convert.ToBase64String(clearText.ToString().EncryptStringToBytes()).Replace('/', '_').Replace('+', '-').Replace("=", "£");
            }
            catch (Exception ex)
            {
                //LogMeNow.Instance.Error(ex, "{FolderName}", "Error Log Encryption Layer");
            }

            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static byte[] EncryptStringToBytes(this string plainText)
        {
            byte[] encrypted;

            //IV is generate for all encryptions, so that it stay unique.
            myRijndael.GenerateIV();

            byte[] IV = myRijndael.IV;

            ICryptoTransform encryptor = myRijndael.CreateEncryptor(myRijndael.Key, IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                //It will write IV to the stream at start
                msEncrypt.Write(IV, 0, IV.Length);

                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }
        public static string ToDecrypt(this string stringToBeDecrypt) => GetDecryptString(stringToBeDecrypt);
        public static string ToEncrypt(this string stringToBeEncrypt) => GetEncryptedString(stringToBeEncrypt);
        public static int DecryptToInt(this string stringToBeDecrypt) => Convert.ToInt32(GetDecryptString(stringToBeDecrypt));
        public static string StringArrayToIntArray(this string str) => string.Join(",", str.Split(',').Select(x => x.ToString().ToDecrypt()));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <returns></returns>
        public static string GetDecryptString(this string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText?.Trim()))
                return string.Empty;

            try
            {
                return Convert.FromBase64String(cipherText.Replace("_", "/").Replace("-", "+").Replace("£", "=")).DecryptStringFromBytes();
            }
            catch (Exception ex)
            {
                // LogMeNow.Instance.Error(ex, "{FolderName}", "Error Log Encryption Layer");
            }

            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <returns></returns>
        private static string DecryptStringFromBytes(this byte[] cipherText)
        {
            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            //get first 16 bytes of IV and use it to decrypt
            var IV = new byte[16];
            Array.Copy(cipherText, 0, IV, 0, IV.Length);

            // Create a decrytor to perform the stream transform.
            ICryptoTransform decryptor = myRijndael.CreateDecryptor(myRijndael.Key, IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(cipherText, IV.Length, cipherText.Length - IV.Length))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {

                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }

            return plaintext;

        }
        /// <summary>
        /// Return decrypted comma separated values of EncryptedInt values
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string GetDecryptedIntValuesOfEncrypted(string Key)
        {
            var decryptedKey = "";
            var response = Key.Split(',');
            foreach (var item in response)
            {
                decryptedKey += EncryptionManager.GetDecryptString(item) + ",";
            }
            decryptedKey = decryptedKey.Remove(decryptedKey.Length - 1, 1);
            return decryptedKey;
        }
        /// <summary>
        /// Return EncyptedInt values of comma separated int values
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string GetEncryptedIntValuesOfDecrypted(string Key)
        {
            var encryptedKey = "";
            var response = Key.Split(',');
            foreach (var item in response)
            {
                encryptedKey += EncryptionManager.GetEncryptedString(item) + ",";
            }
            encryptedKey = encryptedKey.Remove(encryptedKey.Length - 1, 1);
            return encryptedKey;
        }
    }
}
