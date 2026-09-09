using System.Text.RegularExpressions;

namespace SkyRoute.Domain.Rules;

// Lives in Domain/Rules, not Application/Services: pure regex logic, zero dependencies,
// trivially unit-testable, and safe to call from both the Application layer (authoritative
// server-side booking validation) and, mirrored, from the Angular validator factory (UX only).
public static class DocumentValidator
{
    private const string PassportPattern = @"^[A-Z]{1,2}[0-9]{6,7}$";
    private const string NationalIdPattern = @"^[0-9]{9}$";

    public static bool IsValidPassport(string? documentNumber) =>
        Regex.IsMatch(Normalize(documentNumber), PassportPattern);

    public static bool IsValidNationalId(string? documentNumber) =>
        Regex.IsMatch(Normalize(documentNumber), NationalIdPattern);

    public static bool IsValid(string? documentNumber, bool isInternational) =>
        isInternational ? IsValidPassport(documentNumber) : IsValidNationalId(documentNumber);

    private static string Normalize(string? value) =>
        value?.Trim().ToUpperInvariant() ?? string.Empty;
}
