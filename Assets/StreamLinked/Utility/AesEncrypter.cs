using System;
using System.IO;
using System.Security.Cryptography;

namespace ScoredProductions.StreamLinked.Utility {

	public static class AesEncrypter {

		private static Aes CreateAes(byte[] key) {
			Aes aes = Aes.Create();

			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			aes.KeySize = 256; // AES256
			aes.BlockSize = 128;

			// Consumer defined symmetrical key
			aes.Key = key;

			return aes;
		}

		/// <summary>
		/// Encrypts a string into a set of bytes using a provided key
		/// </summary>
		/// <param name="plainText"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static byte[] Encrypt(string plainText, byte[] key) {
			byte[] encryptedBytesWithIv = null;

			using (Aes aes = CreateAes(key)) {
				// Random, unique initialization vector every time
				aes.GenerateIV();

				using (ICryptoTransform encryptor = aes.CreateEncryptor())
				using (MemoryStream ms = new MemoryStream())
				using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
					using (StreamWriter wtr = new StreamWriter(cs)) {
						wtr.Write(plainText);
						wtr.Flush(); // Load buffer into Stream
					}

					byte[] encyptedBytes = ms.ToArray(); // Extract data from stream inserted into CryptoStream

					// Since the IV in unique for every encryption attempt,
					// Store it as the first 16 bytes of the encrypted byte array.
					int ivLen = aes.IV.Length;
					int enLen = encyptedBytes.Length;
					encryptedBytesWithIv = new byte[ivLen + enLen];
					Array.Copy(aes.IV, encryptedBytesWithIv, ivLen);
					Array.Copy(encyptedBytes, 0, encryptedBytesWithIv, ivLen, enLen);
				}
			}

			return encryptedBytesWithIv;
		}

		/// <summary>
		/// Decrypt a set of bytes using the provided key, compatible only with bytes Encrypted using this class
		/// </summary>
		/// <param name="encryptedBytes"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string Decrypt(byte[] encryptedBytes, byte[] key) {
			string plainText = null;

			using (Aes aes = CreateAes(key)) {
				// During encryption, we stored the unique IV in the first 16 bytes
				aes.IV = encryptedBytes[..16];

				// The actual value to decrypt is the rest of the bytes after the stored IV.
				byte[] encryptedValue = encryptedBytes[16..];

				using (ICryptoTransform decryptor = aes.CreateDecryptor())
				using (MemoryStream ms = new MemoryStream(encryptedValue))
				using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				using (StreamReader rdr = new StreamReader(cs)) {
					plainText = rdr.ReadToEnd();
				}
			}

			return plainText;
		}

		/// <summary>
		/// Creates an Aes instance and prompts it to generate a key
		/// </summary>
		/// <returns></returns>
		public static byte[] GenerateKey() {
			using (Aes aes = Aes.Create()) {
				aes.GenerateKey();
				return aes.Key;
			}
		}
	}
}
