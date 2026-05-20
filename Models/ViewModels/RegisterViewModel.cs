using System.ComponentModel.DataAnnotations;

namespace ProVMSIT15.Models.ViewModels;

public class RegisterViewModel
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare("Password"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public UserRole UserRole { get; set; } = UserRole.User;

    [MaxLength(20)]
    public string? DepartmentCode { get; set; }
}
