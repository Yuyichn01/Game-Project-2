using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
public class DataManager : MonoBehaviour
{
    private string filePath;

    private string encryptionKey = "A1B2C3D4E5F6G7H8"; // Must be 16, 24, or 32 bytes for AES

    private void Awake()
    {
        filePath = Application.persistentDataPath + "/gamedata.dat";
    }

    public void SaveGameData(GameData data)
    {
        string jsonData = JsonUtility.ToJson(data);
        string encryptedData = Encrypt(jsonData, encryptionKey);
        File.WriteAllText (filePath, encryptedData);
        Debug.Log("Game data saved.");
    }

    public GameData LoadGameData()
    {
        if (File.Exists(filePath))
        {
            string encryptedData = File.ReadAllText(filePath);
            string jsonData = Decrypt(encryptedData, encryptionKey);
            GameData data = JsonUtility.FromJson<GameData>(jsonData);
            Debug.Log("Game data loaded.");
            return data;
        }
        else
        {
            Debug.LogWarning("No saved data found.");
            return null;
        }
    }

    public GameData CreateNewGameData(int score)
    {
        // Check if the file exists and delete it
        if (File.Exists(filePath))
        {
            File.Delete (filePath);
            Debug.Log("Existing game data deleted.");
        }

        // Create new game data
        GameData data = new GameData(score);
        Debug.Log("New game data Created");

        // Save the new game data
        SaveGameData (data);

        return data;
    }

    private string Encrypt(string plainText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = new byte[16]; // AES block size is 16 bytes
        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (
                    CryptoStream cryptoStream =
                        new CryptoStream(memoryStream,
                            aes.CreateEncryptor(),
                            CryptoStreamMode.Write)
                )
                {
                    using (StreamWriter writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write (plainText);
                    }
                }
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }

    private string Decrypt(string cipherText, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] ivBytes = new byte[16]; // AES block size is 16 bytes
        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using (MemoryStream memoryStream = new MemoryStream(cipherBytes))
            {
                using (
                    CryptoStream cryptoStream =
                        new CryptoStream(memoryStream,
                            aes.CreateDecryptor(),
                            CryptoStreamMode.Read)
                )
                {
                    using (StreamReader reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
