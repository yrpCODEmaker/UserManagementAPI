# UserManagementAPI — TechHive Solutions

API web RESTful construida con **ASP.NET Core (.NET 10)** para la gestión centralizada de registros de usuarios internos de **TechHive Solutions**, orientada a los departamentos de **RR.HH.** y **TI**.

---

## Estructura del proyecto

```text
UserManagementAPI/
├── Controllers/
│   └── UsersController.cs          # Endpoints CRUD (/api/users) con try-catch y validación de ID/paginación
├── DTOs/
│   └── UserDtos.cs                 # DTOs con IValidatableObject y PagedResult<T>
├── Models/
│   └── User.cs                     # Entidad principal de usuario
├── Services/
│   ├── IUserRepository.cs          # Contrato de acceso a datos con soporte de paginación
│   └── InMemoryUserRepository.cs   # Repositorio thread-safe con índice O(1) y ReaderWriterLockSlim
├── Properties/
│   └── launchSettings.json         # Perfiles de ejecución local (http://localhost:5028)
├── Program.cs                      # Configuración de DI, OpenAPI y pipeline HTTP
├── UserManagementAPI.csproj        # Definición del proyecto (.NET 10)
├── UserManagementAPI.http          # Colección de pruebas HTTP (incluye casos extremos)
└── README.md                       # Documentación de la API, mejoras de Copilot y depuración
```

---

## Requisitos y ejecución

- **.NET SDK 10.0** o superior

### Compilar el proyecto

```bash
dotnet build
```

### Ejecutar la API localmente

```bash
dotnet run
```

Por defecto (según `Properties/launchSettings.json`), el perfil `http` expone la API en `http://localhost:5028` y el documento OpenAPI en `http://localhost:5028/openapi/v1.json` en entorno `Development`.

También es posible especificar una URL explícita:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5099
```

---

## Endpoints CRUD (`/api/users`)

| Método | Ruta | Descripción | Códigos HTTP |
| --- | --- | --- | --- |
| `GET` | `/api/users` | Lista usuarios paginados (`PagedResult<User>`). Parámetros opcionales: `department`, `isActive`, `page` (por defecto `1`) y `pageSize` (por defecto `20`, máximo `100`) | `200 OK`, `400 Bad Request`, `500 Internal Server Error` |
| `GET` | `/api/users/{id}` | Obtiene un usuario por su identificador entero positivo (`id > 0`) | `200 OK`, `400 Bad Request`, `404 Not Found`, `500 Internal Server Error` |
| `POST` | `/api/users` | Crea un nuevo usuario validando formato de datos y unicidad de correo | `201 Created`, `400 Bad Request`, `409 Conflict`, `500 Internal Server Error` |
| `PUT` | `/api/users/{id}` | Actualiza los datos de un usuario existente (`id > 0`) | `200 OK`, `400 Bad Request`, `404 Not Found`, `409 Conflict`, `500 Internal Server Error` |
| `DELETE` | `/api/users/{id}` | Elimina un usuario por su identificador (`id > 0`) | `204 No Content`, `400 Bad Request`, `404 Not Found`, `500 Internal Server Error` |

### Datos iniciales precargados (Seed)

Al iniciar, `InMemoryUserRepository` carga 3 registros de ejemplo para pruebas inmediatas:

1. `id: 1` — **Ana Gómez** (`ana.gomez@techhive.com`, `RR.HH.`, `Especialista de Talento`)
2. `id: 2` — **Carlos Mendoza** (`carlos.mendoza@techhive.com`, `TI`, `Administrador de Sistemas`)
3. `id: 3` — **Sofía Ramírez** (`sofia.ramirez@techhive.com`, `TI`, `Ingeniera de Soporte`)

### Ejemplos de solicitud y respuesta

#### `GET /api/users?page=1&pageSize=2` (Respuesta `200 OK`)

```json
{
  "items": [
    {
      "id": 1,
      "firstName": "Ana",
      "lastName": "Gómez",
      "email": "ana.gomez@techhive.com",
      "department": "RR.HH.",
      "role": "Especialista de Talento",
      "isActive": true,
      "createdAt": "2026-11-25T14:10:45.7922415Z",
      "updatedAt": null
    },
    {
      "id": 2,
      "firstName": "Carlos",
      "lastName": "Mendoza",
      "email": "carlos.mendoza@techhive.com",
      "department": "TI",
      "role": "Administrador de Sistemas",
      "isActive": true,
      "createdAt": "2026-11-25T14:10:45.7922415Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 3,
  "totalPages": 2
}
```

#### `POST /api/users` (Solicitud)

```json
{
  "firstName": "Lucía",
  "lastName": "Fernández",
  "email": "lucia.fernandez@techhive.com",
  "department": "RR.HH.",
  "role": "Coordinadora de Nómina",
  "isActive": true
}
```

#### `POST /api/users` (Respuesta `201 Created`)

```json
{
  "id": 4,
  "firstName": "Lucía",
  "lastName": "Fernández",
  "email": "lucia.fernandez@techhive.com",
  "department": "RR.HH.",
  "role": "Coordinadora de Nómina",
  "isActive": true,
  "createdAt": "2026-11-25T14:10:46.7363537Z",
  "updatedAt": null
}
```

#### `PUT /api/users/4` (Solicitud)

```json
{
  "firstName": "Lucía",
  "lastName": "Fernández Vargas",
  "email": "lucia.fernandez@techhive.com",
  "department": "RR.HH.",
  "role": "Gerente de Nómina",
  "isActive": true
}
```

---

## Cómo probar la API

### 1. Con Postman

1. Inicia la API con `dotnet run` (escuchando en `http://localhost:5028`).
2. Opcionalmente, importa `http://localhost:5028/openapi/v1.json` en Postman para generar la colección automáticamente, o crea solicitudes manuales apuntando a `http://localhost:5028/api/users`:
   - `GET http://localhost:5028/api/users?page=1&pageSize=20`
   - `GET http://localhost:5028/api/users?department=TI&isActive=true`
   - `GET http://localhost:5028/api/users/1`
   - `POST http://localhost:5028/api/users` (Body -> `raw` -> `JSON`)
   - `PUT http://localhost:5028/api/users/4` (Body -> `raw` -> `JSON`)
   - `DELETE http://localhost:5028/api/users/4`

### 2. Con `UserManagementAPI.http`

El archivo [UserManagementAPI.http](./UserManagementAPI.http) incluye 18 solicitudes listas para ejecutar desde VS Code (extensión REST Client) o Visual Studio, cubriendo operaciones normales y casos extremos (`400`, `404`, `409`).

### 3. Con `curl` y resultados verificados

Todos los endpoints y casos extremos fueron probados en ejecución real contra `http://127.0.0.1:5099`:

| Caso probado | Método y ruta | Código esperado | Código obtenido |
| --- | --- | --- | --- |
| Listar usuarios paginados por defecto | `GET /api/users` | `200` | `200` (`totalCount: 3`, `items`: 3, `totalPages: 1`) |
| Listar página explícita | `GET /api/users?page=1&pageSize=2` | `200` | `200` (`items`: 2, `totalCount: 3`, `totalPages: 2`) |
| Rechazar paginación inválida | `GET /api/users?page=0&pageSize=500` | `400` | `400` |
| Rechazar ID cero en consulta | `GET /api/users/0` | `400` | `400` |
| Rechazar ID negativo en consulta | `GET /api/users/-5` | `400` | `400` |
| Consultar usuario inexistente | `GET /api/users/999` | `404` | `404` |
| Rechazar nombre corto con espacios (`"  A  "`) | `POST /api/users` | `400` | `400` (`ValidationProblemDetails`) |
| Rechazar correo sin dominio/TLD (`"usuario@dominio"`) | `POST /api/users` | `400` | `400` (`ValidationProblemDetails`) |
| Crear usuario válido | `POST /api/users` | `201` | `201` (`id: 4`) |
| Prevenir correo duplicado al crear | `POST /api/users` | `409` | `409` |
| Rechazar ID cero al actualizar | `PUT /api/users/0` | `400` | `400` |
| Actualizar usuario inexistente | `PUT /api/users/999` | `404` | `404` |
| Actualizar usuario existente | `PUT /api/users/4` | `200` | `200` (`updatedAt` asignado) |
| Rechazar ID negativo al eliminar | `DELETE /api/users/-1` | `400` | `400` |
| Eliminar usuario existente | `DELETE /api/users/4` | `204` | `204` |
| Verificar eliminación posterior | `GET /api/users/4` | `404` | `404` |
| Esquema OpenAPI | `GET /openapi/v1.json` | `200` | `200` (OpenAPI 3.1.1) |

---

## Cómo contribuyó Microsoft Copilot al desarrollo y mejora del código

Durante el andamiaje inicial de **UserManagementAPI**, Copilot aportó mejoras concretas en las siguientes áreas:

1. **Organización del arranque y código repetitivo en `Program.cs`**: Configuró `AddControllers()`, `AddOpenApi()`, el registro singleton `AddSingleton<IUserRepository, InMemoryUserRepository>()` y la separación de `MapOpenApi()` en desarrollo frente a `UseHttpsRedirection()`.
2. **Protección frente a *over-posting* con DTOs**: Separó `CreateUserDto` y `UpdateUserDto` de la entidad `User` mediante `UserInputDto` en `DTOs/UserDtos.cs` para proteger `Id`, `CreatedAt` y `UpdatedAt`.
3. **Semántica RESTful y trazas estructuradas**: Aplicó códigos HTTP estándar (`200`, `201 CreatedAtAction`, `204`, `400`, `404`, `409`), atributos `[ProducesResponseType]` y registro de eventos con `ILogger<UsersController>`.

---

## Depuración, validación y optimización (Actividad 2)

### Problemas identificados en el código inicial

1. **Brechas en la validación de entrada (`DTOs/UserDtos.cs` y `UsersController.cs`)**:
   - Los atributos `[StringLength(MinimumLength = 2)]` aceptaban cadenas con relleno de espacios como `"  A  "` porque validaban la longitud antes del `Trim()`, dejando pasar nombres o campos con un solo carácter visible.
   - `[EmailAddress]` en .NET valida únicamente la presencia básica de `@`, permitiendo correos incompletos sin TLD como `"usuario@dominio"`.
   - Los endpoints `GET`, `PUT` y `DELETE` con `{id:int}` aceptaban enteros `<= 0` (`0`, `-5`) y los enviaban al repositorio devolviendo `404` en lugar de rechazar el formato inválido con `400 Bad Request`.
2. **Falta de control de excepciones y condición de carrera en unicidad (`UsersController.cs` y `InMemoryUserRepository.cs`)**:
   - Las acciones del controlador carecían de bloques `try-catch`, por lo que cualquier excepción inesperada en tiempo de ejecución podía provocar fallos no controlados en lugar de respuestas HTTP estructuradas (`400`, `409` o `500`).
   - La comprobación `EmailExists` y el posterior `Create`/`Update` no eran atómicos entre sí, permitiendo que peticiones concurrentes con el mismo correo eludieran la verificación previa.
3. **Cuellos de botella de rendimiento en `GET /api/users` y búsquedas de correo (`InMemoryUserRepository.cs`)**:
   - `GET /api/users` clonaba y devolvía **todos** los usuarios en memoria en cada llamada sin paginación (`CloneUser` sobre el conjunto completo).
   - `EmailExists` realizaba un escaneo lineal $O(N)$ sobre `_users.Values` en cada creación o actualización.

### Correcciones implementadas

1. **Validación estricta tras normalización (`DTOs/UserDtos.cs`)**:
   - Se implementó `IValidatableObject` en `UserInputDto` para exigir al menos 2 caracteres visibles tras `Trim()` en `FirstName`, `LastName`, `Department` y `Role`.
   - Se incorporó una expresión regular compilada (`^[^@\s]+@[^@\s]+\.[^@\s]{2,}$`) para exigir dominio y extensión válidos en `Email`.
   - Se creó `PagedResult<T>` con `Items`, `Page`, `PageSize`, `TotalCount` y `TotalPages`.
2. **Optimización $O(1)$, paginación y atomicidad (`Services/IUserRepository.cs` y `Services/InMemoryUserRepository.cs`)**:
   - Se reemplazó el escaneo lineal por un índice secundario `_emailIndex` (`Dictionary<string, int>` con `StringComparer.OrdinalIgnoreCase`) que resuelve `EmailExists` en tiempo $O(1)$.
   - Se sincronizó todo acceso mediante `ReaderWriterLockSlim`, garantizando lecturas concurrentes y escrituras atómicas en `Create`, `Update` y `Delete` (lanzando `InvalidOperationException` si existe conflicto de correo dentro del bloqueo de escritura).
   - `GetAll` ahora pagina los resultados (`Skip`/`Take`) y ejecuta `CloneUser` únicamente sobre la porción solicitada de la página actual.
3. **Manejo robusto de errores y casos extremos (`Controllers/UsersController.cs`)**:
   - Se validan `id <= 0` y los rangos de paginación (`page >= 1`, `1 <= pageSize <= 100`) devolviendo `400 Bad Request` con mensajes claros.
   - Todas las acciones del controlador envuelven su lógica en bloques `try-catch` capturando `ArgumentException` (`400 Bad Request`), `InvalidOperationException` (`409 Conflict`) y `Exception` (`500 Internal Server Error` mediante `Problem(...)` y `LogError`).

### Cómo agilizó Microsoft Copilot el proceso de depuración

- **Auditoría dirigida de casos extremos**: Copilot permitió identificar rápidamente fallos sutiles que las pruebas felices iniciales no cubrían (cadenas con espacios antes del `Trim()`, correos sin TLD con `[EmailAddress]`, IDs negativos/cero y ausencia de paginación).
- **Refactorización segura hacia estructuras eficientes**: Sugirió e integró el patrón `ReaderWriterLockSlim` + índice `_emailIndex` $O(1)$ manteniendo compatibilidad con `IUserRepository` y evitando clonaciones masivas en `GET /api/users`.
- **Generación de pruebas de regresión y borde**: Aceleró la ampliación de `UserManagementAPI.http` y del script automatizado `curl` con los 17 escenarios verificados de extremo a extremo.
