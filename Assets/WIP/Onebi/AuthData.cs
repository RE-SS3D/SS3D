using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

public class AuthData
{
    public string username;
    private string passwordHash;
    bool isPassSet = false;

    public void SetPassword(string clearPass)
    {
        passwordHash = ComputeSha256Hash(clearPass);
        isPassSet = true;
    }

    public string GetHashPass()
    {
        return passwordHash;
    }

    // Took this function from the internet, will probably need to change it
    static string ComputeSha256Hash(string rawData)  
    {  
        // Create a SHA256   
        using (SHA256 sha256Hash = SHA256.Create())  
        {  
            // ComputeHash - returns byte array  
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();  
            for (int i = 0; i < bytes.Length; i++)  
            {  
                builder.Append(bytes[i].ToString("x2"));  
            }  
            return builder.ToString();  
        }  
    }  
}
