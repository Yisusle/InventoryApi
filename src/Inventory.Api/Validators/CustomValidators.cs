using System.Linq;
using System.Text.RegularExpressions;
using Inventory.Api.Constants;

namespace Inventory.Api.Validators;

public static class CustomValidators
{
    public static bool IsValidPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < AppConstants.ValidationLimits.PasswordMinLength)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);

        return hasUpper && hasLower && hasDigit;
    }

    public static bool IsValidUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)
            || username.Length < AppConstants.ValidationLimits.UsernameMinLength
            || username.Length > AppConstants.ValidationLimits.UsernameMaxLength)
            return false;

        return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
    }

    public static bool IsValidSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku)
            || sku.Length < AppConstants.ValidationLimits.ProductSkuMinLength
            || sku.Length > AppConstants.ValidationLimits.ProductSkuMaxLength)
            return false;

        return Regex.IsMatch(sku, @"^[a-zA-Z0-9_-]+$");
    }
}
