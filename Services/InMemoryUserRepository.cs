using UserManagementAPI.DTOs;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

/// <summary>
/// Implementación en memoria optimizada con índice de correos O(1),
/// paginación sin clonado innecesario y sincronización atómica mediante ReaderWriterLockSlim.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository, IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Dictionary<int, User> _users = new();
    private readonly Dictionary<string, int> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId;

    public InMemoryUserRepository()
    {
        SeedInitialUsers();
    }

    public PagedResult<User> GetAll(string? department = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "El número de página debe ser mayor o igual a 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "El tamaño de página debe ser mayor o igual a 1.");
        }

        _lock.EnterReadLock();
        try
        {
            IEnumerable<User> query = _users.Values;

            if (!string.IsNullOrWhiteSpace(department))
            {
                var normalizedDepartment = department.Trim();
                query = query.Where(u => string.Equals(u.Department, normalizedDepartment, StringComparison.OrdinalIgnoreCase));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var filtered = query.OrderBy(u => u.Id).ToList();
            var totalCount = filtered.Count;
            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CloneUser)
                .ToList();

            return new PagedResult<User>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public User? GetById(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        _lock.EnterReadLock();
        try
        {
            return _users.TryGetValue(id, out var user) ? CloneUser(user) : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool EmailExists(string email, int? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalizedEmail = email.Trim();

        _lock.EnterReadLock();
        try
        {
            return _emailIndex.TryGetValue(normalizedEmail, out var ownerId)
                && (excludeUserId is null || ownerId != excludeUserId.Value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public User Create(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var normalizedEmail = user.Email.Trim();

        _lock.EnterWriteLock();
        try
        {
            if (_emailIndex.ContainsKey(normalizedEmail))
            {
                throw new InvalidOperationException($"Ya existe un usuario registrado con el correo '{normalizedEmail}'.");
            }

            var id = Interlocked.Increment(ref _nextId);
            var created = BuildStoredUser(
                id: id,
                source: user,
                normalizedEmail: normalizedEmail,
                createdAt: user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt,
                updatedAt: null);

            _users[id] = created;
            _emailIndex[normalizedEmail] = id;
            return CloneUser(created);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public User? Update(int id, User updatedUser)
    {
        ArgumentNullException.ThrowIfNull(updatedUser);
        if (id <= 0)
        {
            return null;
        }

        var normalizedEmail = updatedUser.Email.Trim();

        _lock.EnterWriteLock();
        try
        {
            if (!_users.TryGetValue(id, out var existing))
            {
                return null;
            }

            if (_emailIndex.TryGetValue(normalizedEmail, out var ownerId) && ownerId != id)
            {
                throw new InvalidOperationException($"Ya existe otro usuario registrado con el correo '{normalizedEmail}'.");
            }

            if (!string.Equals(existing.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                _emailIndex.Remove(existing.Email);
            }

            var replacement = BuildStoredUser(
                id: existing.Id,
                source: updatedUser,
                normalizedEmail: normalizedEmail,
                createdAt: existing.CreatedAt,
                updatedAt: updatedUser.UpdatedAt ?? DateTime.UtcNow);

            _users[id] = replacement;
            _emailIndex[normalizedEmail] = id;
            return CloneUser(replacement);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Delete(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        _lock.EnterWriteLock();
        try
        {
            if (!_users.Remove(id, out var removed))
            {
                return false;
            }

            _emailIndex.Remove(removed.Email);
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }

    private void SeedInitialUsers()
    {
        var seedTime = DateTime.UtcNow;

        Create(new User
        {
            FirstName = "Ana",
            LastName = "Gómez",
            Email = "ana.gomez@techhive.com",
            Department = "RR.HH.",
            Role = "Especialista de Talento",
            IsActive = true,
            CreatedAt = seedTime
        });

        Create(new User
        {
            FirstName = "Carlos",
            LastName = "Mendoza",
            Email = "carlos.mendoza@techhive.com",
            Department = "TI",
            Role = "Administrador de Sistemas",
            IsActive = true,
            CreatedAt = seedTime
        });

        Create(new User
        {
            FirstName = "Sofía",
            LastName = "Ramírez",
            Email = "sofia.ramirez@techhive.com",
            Department = "TI",
            Role = "Ingeniera de Soporte",
            IsActive = true,
            CreatedAt = seedTime
        });
    }

    private static User BuildStoredUser(int id, User source, string normalizedEmail, DateTime createdAt, DateTime? updatedAt)
    {
        return new User
        {
            Id = id,
            FirstName = source.FirstName.Trim(),
            LastName = source.LastName.Trim(),
            Email = normalizedEmail,
            Department = source.Department.Trim(),
            Role = source.Role.Trim(),
            IsActive = source.IsActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static User CloneUser(User source)
    {
        return new User
        {
            Id = source.Id,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Email = source.Email,
            Department = source.Department,
            Role = source.Role,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }
}
