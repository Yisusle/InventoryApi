using Inventory.Api.Validators;
using Xunit;

namespace Inventory.Api.Tests.Validators;

public class CustomValidatorsTests
{
    [Theory]
    [InlineData("Abcdefg1", true)]
    [InlineData("abcdefg1", false)]
    [InlineData("ABCDEFG1", false)]
    [InlineData("Abcdefgh", false)]
    [InlineData("Ab1", false)]
    [InlineData(null, false)]
    public void IsValidPassword_RequiresUpperLowerDigitAndMinLength(string? password, bool expected)
    {
        Assert.Equal(expected, CustomValidators.IsValidPassword(password));
    }

    [Theory]
    [InlineData("valid_user1", true)]
    [InlineData("ab", false)]
    [InlineData("invalid user", false)]
    [InlineData("invalid-user", false)]
    [InlineData(null, false)]
    public void IsValidUsername_OnlyAllowsAlphanumericAndUnderscore(string? username, bool expected)
    {
        Assert.Equal(expected, CustomValidators.IsValidUsername(username));
    }

    [Theory]
    [InlineData("SKU-123", true)]
    [InlineData("SKU_123", true)]
    [InlineData("sk", false)]
    [InlineData("SKU 123", false)]
    [InlineData(null, false)]
    public void IsValidSku_AllowsAlphanumericHyphenUnderscore(string? sku, bool expected)
    {
        Assert.Equal(expected, CustomValidators.IsValidSku(sku));
    }
}
