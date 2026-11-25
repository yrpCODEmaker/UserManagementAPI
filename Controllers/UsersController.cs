using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Controllers;

/// <summary>
/// Controlador RESTful para operaciones CRUD de usuarios internos en TechHive Solutions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private const string InvalidIdMessage = "El ID del usuario debe ser un número entero mayor que cero.";
    private const int MaxPageSize = 100;

    private readonly IUserRepository _repository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository repository, ILogger<UsersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Recupera la lista paginada de usuarios, con filtros opcionales por departamento y estado activo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<User>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<PagedResult<User>> GetAll(
        [FromQuery] string? department = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return BadRequest(new
            {
                message = $"Los parámetros de paginación son inválidos: 'page' debe ser >= 1 y 'pageSize' debe estar entre 1 y {MaxPageSize}."
            });
        }

        try
        {
            var result = _repository.GetAll(department, isActive, page, pageSize);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parámetros inválidos al listar usuarios.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al recuperar la lista de usuarios.");
            return InternalServerErrorProblem();
        }
    }

    /// <summary>
    /// Recupera un usuario específico por su identificador único.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<User> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = InvalidIdMessage });
        }

        try
        {
            var user = _repository.GetById(id);
            if (user is null)
            {
                _logger.LogWarning("Intento de consulta sobre usuario inexistente con ID {UserId}.", id);
                return NotFoundUser(id);
            }

            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al consultar el usuario con ID {UserId}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al recuperar el usuario con ID {UserId}.", id);
            return InternalServerErrorProblem();
        }
    }

    /// <summary>
    /// Crea un nuevo registro de usuario validando formato de datos y unicidad de correo electrónico.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<User> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            var normalizedEmail = dto.Email.Trim();
            if (_repository.EmailExists(normalizedEmail))
            {
                return Conflict(new { message = $"Ya existe un usuario registrado con el correo '{normalizedEmail}'." });
            }

            var userToCreate = MapDtoToUser(dto, createdAt: DateTime.UtcNow);
            var created = _repository.Create(userToCreate);

            _logger.LogInformation(
                "Usuario creado con ID {UserId} ({Email}) en departamento {Department}.",
                created.Id,
                created.Email,
                created.Department);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflicto de unicidad al crear usuario con correo {Email}.", dto.Email);
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos inválidos al crear usuario.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear usuario.");
            return InternalServerErrorProblem();
        }
    }

    /// <summary>
    /// Actualiza los datos de un usuario existente por su ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<User> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = InvalidIdMessage });
        }

        try
        {
            var existing = _repository.GetById(id);
            if (existing is null)
            {
                _logger.LogWarning("Intento de actualización sobre usuario inexistente con ID {UserId}.", id);
                return NotFoundUser(id);
            }

            var normalizedEmail = dto.Email.Trim();
            if (_repository.EmailExists(normalizedEmail, excludeUserId: id))
            {
                return Conflict(new { message = $"Ya existe otro usuario registrado con el correo '{normalizedEmail}'." });
            }

            var userToUpdate = MapDtoToUser(dto, updatedAt: DateTime.UtcNow);
            var updated = _repository.Update(id, userToUpdate);

            if (updated is null)
            {
                return NotFoundUser(id);
            }

            _logger.LogInformation("Usuario con ID {UserId} actualizado correctamente.", id);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflicto de unicidad al actualizar usuario con ID {UserId}.", id);
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Datos inválidos al actualizar usuario con ID {UserId}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar el usuario con ID {UserId}.", id);
            return InternalServerErrorProblem();
        }
    }

    /// <summary>
    /// Elimina un usuario existente por su ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Delete(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = InvalidIdMessage });
        }

        try
        {
            if (!_repository.Delete(id))
            {
                _logger.LogWarning("Intento de eliminación sobre usuario inexistente con ID {UserId}.", id);
                return NotFoundUser(id);
            }

            _logger.LogInformation("Usuario con ID {UserId} eliminado.", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al eliminar el usuario con ID {UserId}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el usuario con ID {UserId}.", id);
            return InternalServerErrorProblem();
        }
    }

    private ActionResult NotFoundUser(int id) =>
        NotFound(new { message = $"Usuario con ID {id} no encontrado." });

    private ObjectResult InternalServerErrorProblem() =>
        Problem(
            title: "Error interno del servidor.",
            detail: "Ocurrió un error inesperado al procesar la solicitud.",
            statusCode: StatusCodes.Status500InternalServerError);

    private static User MapDtoToUser(UserInputDto dto, DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        return new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Department = dto.Department.Trim(),
            Role = dto.Role.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = createdAt ?? default,
            UpdatedAt = updatedAt
        };
    }
}
