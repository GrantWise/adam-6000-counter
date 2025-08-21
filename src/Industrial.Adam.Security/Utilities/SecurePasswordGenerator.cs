using System.Security.Cryptography;
using System.Text;

namespace Industrial.Adam.Security.Utilities;

/// <summary>
/// Generates cryptographically secure random passwords
/// </summary>
public static class SecurePasswordGenerator
{
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string NumberChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    /// <summary>
    /// Generates a cryptographically secure random password
    /// </summary>
    /// <param name="length">Password length (minimum 16 characters)</param>
    /// <param name="includeSpecialChars">Include special characters</param>
    /// <returns>Secure random password</returns>
    public static string GenerateSecurePassword(int length = 16, bool includeSpecialChars = true)
    {
        if (length < 16)
        {
            throw new ArgumentException("Password must be at least 16 characters long", nameof(length));
        }

        var charSet = LowerChars + UpperChars + NumberChars;
        if (includeSpecialChars)
        {
            charSet += SpecialChars;
        }

        var password = new StringBuilder(length);
        using var rng = RandomNumberGenerator.Create();

        // Ensure at least one character from each required character set
        password.Append(GetRandomChar(rng, LowerChars));
        password.Append(GetRandomChar(rng, UpperChars));
        password.Append(GetRandomChar(rng, NumberChars));

        if (includeSpecialChars)
        {
            password.Append(GetRandomChar(rng, SpecialChars));
        }

        // Fill the rest with random characters from the full set
        var remainingLength = length - password.Length;
        for (int i = 0; i < remainingLength; i++)
        {
            password.Append(GetRandomChar(rng, charSet));
        }

        // Shuffle the password to avoid predictable patterns
        return ShuffleString(rng, password.ToString());
    }

    /// <summary>
    /// Generates a passphrase-style password using random words
    /// </summary>
    /// <param name="wordCount">Number of words (minimum 4)</param>
    /// <returns>Secure passphrase</returns>
    public static string GeneratePassphrase(int wordCount = 4)
    {
        if (wordCount < 4)
        {
            throw new ArgumentException("Passphrase must contain at least 4 words", nameof(wordCount));
        }

        var words = new[]
        {
            "Secure", "Factory", "Machine", "Counter", "Production", "Quality", "Safety", "Monitor",
            "System", "Control", "Process", "Equipment", "Industrial", "Automation", "Digital", "Smart",
            "Efficient", "Reliable", "Accurate", "Precise", "Advanced", "Modern", "Innovative", "Robust",
            "Optimize", "Enhance", "Improve", "Streamline", "Integrate", "Configure", "Implement", "Deploy"
        };

        using var rng = RandomNumberGenerator.Create();
        var selectedWords = new List<string>();

        for (int i = 0; i < wordCount; i++)
        {
            var randomIndex = GetRandomInt(rng, 0, words.Length);
            selectedWords.Add(words[randomIndex]);
        }

        // Add random numbers to increase entropy
        var randomNumber = GetRandomInt(rng, 1000, 9999);

        return string.Join("-", selectedWords) + "-" + randomNumber;
    }

    private static char GetRandomChar(RandomNumberGenerator rng, string charSet)
    {
        var randomIndex = GetRandomInt(rng, 0, charSet.Length);
        return charSet[randomIndex];
    }

    private static int GetRandomInt(RandomNumberGenerator rng, int minValue, int maxValue)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0);
        return (int)(value % (maxValue - minValue)) + minValue;
    }

    private static string ShuffleString(RandomNumberGenerator rng, string input)
    {
        var chars = input.ToCharArray();

        for (int i = chars.Length - 1; i > 0; i--)
        {
            var j = GetRandomInt(rng, 0, i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
