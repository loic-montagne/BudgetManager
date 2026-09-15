using System.Security.Cryptography;
using BudgetManager.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Identity;

namespace BudgetManager.Infrastructure.Identity;

internal sealed class PasswordGenerator(UserManager<ApplicationUser> userManager) : IPasswordGenerator
{
    private const string LowercaseCharacters = "abcdefghijklmnopqrstuvwxyz";
    private const string UppercaseCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string DigitCharacters = "0123456789";
    private const string NonAlphanumericCharacters = "!@#$%^&*()-_=+[]{}";

    public string Generate(int length)
    {
        var options = userManager.Options.Password;

        ArgumentOutOfRangeException.ThrowIfLessThan(length, options.RequiredLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, options.RequiredUniqueChars);

        var characters = new List<char>(length);

        if (options.RequireLowercase)
            characters.Add(GetRandomCharacter(LowercaseCharacters));

        if (options.RequireUppercase)
            characters.Add(GetRandomCharacter(UppercaseCharacters));

        if (options.RequireDigit)
            characters.Add(GetRandomCharacter(DigitCharacters));

        if (options.RequireNonAlphanumeric)
            characters.Add(GetRandomCharacter(NonAlphanumericCharacters));

        const string allCharacters =
            LowercaseCharacters +
            UppercaseCharacters +
            DigitCharacters +
            NonAlphanumericCharacters;

        while (characters.Distinct().Count() < options.RequiredUniqueChars)
        {
            var character = GetRandomCharacter(allCharacters);

            if (!characters.Contains(character))
                characters.Add(character);
        }

        while (characters.Count < length)
            characters.Add(GetRandomCharacter(allCharacters));

        Shuffle(characters);

        return new string([.. characters]);
    }

    private static char GetRandomCharacter(string characters)
    {
        return characters[RandomNumberGenerator.GetInt32(characters.Length)];
    }

    private static void Shuffle(List<char> characters)
    {
        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) = (characters[j], characters[i]);
        }
    }
}
