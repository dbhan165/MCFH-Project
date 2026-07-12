using System.Text.RegularExpressions;

namespace MCFH.Services;

public static class PasswordValidator
{
    public const string RequirementMessage =
        "Mật khẩu phải có tối thiểu 8 ký tự, bao gồm chữ in hoa, chữ cái, số và ký tự @.";

    private static readonly Regex PasswordPattern = new(
        @"^(?=.*[A-Z])(?=.*[a-zA-Z])(?=.*\d)(?=.*@).{8,}$",
        RegexOptions.Compiled);

    public static bool IsValid(string? password, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Mật khẩu không được để trống.";
            return false;
        }

        if (!PasswordPattern.IsMatch(password))
        {
            errorMessage = RequirementMessage;
            return false;
        }

        errorMessage = null;
        return true;
    }
}
