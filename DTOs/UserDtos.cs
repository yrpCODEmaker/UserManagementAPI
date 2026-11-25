using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UserManagementAPI.DTOs;

/// <summary>
/// Resultado paginado estándar para listados de recursos.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Campos comunes y validaciones para crear o actualizar usuarios.
/// </summary>
public abstract class UserInputDto : IValidatableObject
{
    private static readonly Regex StrictEmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(200, ErrorMessage = "El correo electrónico no puede superar los 200 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El departamento es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El departamento debe tener entre 2 y 100 caracteres.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El rol debe tener entre 2 y 100 caracteres.")]
    public string Role { get; set; } = "Empleado";

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var textFields = new (string Value, string MemberName, string Label)[]
        {
            (FirstName, nameof(FirstName), "El nombre"),
            (LastName, nameof(LastName), "El apellido"),
            (Department, nameof(Department), "El departamento"),
            (Role, nameof(Role), "El rol")
        };

        foreach (var (value, memberName, label) in textFields)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
            {
                yield return new ValidationResult(
                    $"{label} debe tener al menos 2 caracteres visibles (sin contar espacios).",
                    new[] { memberName });
            }
        }

        var trimmedEmail = Email?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(trimmedEmail) && !StrictEmailRegex.IsMatch(trimmedEmail))
        {
            yield return new ValidationResult(
                "El correo electrónico debe incluir un dominio válido (por ejemplo, usuario@techhive.com).",
                new[] { nameof(Email) });
        }
    }
}

/// <summary>
/// DTO de entrada para registrar un nuevo usuario.
/// </summary>
public sealed class CreateUserDto : UserInputDto
{
}

/// <summary>
/// DTO de entrada para actualizar los datos de un usuario existente.
/// </summary>
public sealed class UpdateUserDto : UserInputDto
{
}
