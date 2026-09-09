using SkyRoute.Domain.Rules;

namespace SkyRoute.Tests.Domain;

public class DocumentValidatorTests
{
    [Theory]
    [InlineData("X1234567")]
    [InlineData("AB123456")]
    [InlineData("x1234567")] // lower case, normalized before matching
    public void IsValidPassport_AcceptsPassportShapedNumbers(string documentNumber)
    {
        Assert.True(DocumentValidator.IsValidPassport(documentNumber));
    }

    [Theory]
    [InlineData("123456789")] // national-id-shaped, must be rejected as a passport
    [InlineData("")]
    [InlineData(null)]
    public void IsValidPassport_RejectsNonPassportShapedNumbers(string? documentNumber)
    {
        Assert.False(DocumentValidator.IsValidPassport(documentNumber));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData(" 123456789 ")] // trimmed before matching
    public void IsValidNationalId_AcceptsNationalIdShapedNumbers(string documentNumber)
    {
        Assert.True(DocumentValidator.IsValidNationalId(documentNumber));
    }

    [Theory]
    [InlineData("X1234567")] // passport-shaped, must be rejected as a national id
    [InlineData("")]
    [InlineData(null)]
    public void IsValidNationalId_RejectsNonNationalIdShapedNumbers(string? documentNumber)
    {
        Assert.False(DocumentValidator.IsValidNationalId(documentNumber));
    }

    [Fact]
    public void IsValid_RoutesToPassportRule_WhenInternational()
    {
        Assert.True(DocumentValidator.IsValid("X1234567", isInternational: true));
        Assert.False(DocumentValidator.IsValid("123456789", isInternational: true));
    }

    [Fact]
    public void IsValid_RoutesToNationalIdRule_WhenDomestic()
    {
        Assert.True(DocumentValidator.IsValid("123456789", isInternational: false));
        Assert.False(DocumentValidator.IsValid("X1234567", isInternational: false));
    }
}
