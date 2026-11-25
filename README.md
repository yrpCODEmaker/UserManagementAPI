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
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs      # Captura global de excepciones (JSON 500)
│   ├── TokenAuthenticationMiddleware.cs    # Validación de tokens Bearer (401 Unauthorized)
│   └── RequestResponseLoggingMiddleware.cs # Auditoría HTTP (método, ruta, estado, duración)
├── Models/
│   └── User.cs                     # Entidad principal de usuario
├── Services/
│   ├── IUserRepository.cs          # Contrato de acceso a datos con soporte de paginación
│   └── InMemoryUserRepository.cs   # Repositorio thread-safe con índice O(1) y ReaderWriterLockSlim
├── Properties/
│   └── launchSettings.json         # Perfiles de ejecución local (http://localhost:5028)
├── appsettings.json                # Configuración de logging y Authentication:ValidTokens
├── Program.cs                      # Configuración de DI, OpenAPI y orden del pipeline HTTP
├── UserManagementAPI.csproj        # Definición del proyecto (.NET 10)
├── UserManagementAPI.http          # Colección de pruebas HTTP (incluye auth, errores y casos extremos)
└── README.md                       # Documentación de la API, mejoras de Copilot, depuración y middleware
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
2. Agrega la cabecera de autenticación `Authorization: Bearer techhive-it-token-2026` (o `techhive-hr-token-2026`, configurados en `appsettings.json`) en las solicitudes hacia `/api/users`.
3. Opcionalmente, importa `http://localhost:5028/openapi/v1.json` en Postman para generar la colección automáticamente, o crea solicitudes manuales apuntando a `http://localhost:5028/api/users`:
   - `GET http://localhost:5028/api/users?page=1&pageSize=20`
   - `GET http://localhost:5028/api/users?department=TI&isActive=true`
   - `GET http://localhost:5028/api/users/1`
   - `POST http://localhost:5028/api/users` (Body -> `raw` -> `JSON`)
   - `PUT http://localhost:5028/api/users/4` (Body -> `raw` -> `JSON`)
   - `DELETE http://localhost:5028/api/users/4`

### 2. Con `UserManagementAPI.http`

El archivo [UserManagementAPI.http](./UserManagementAPI.http) incluye 21 solicitudes listas para ejecutar desde VS Code (extensión REST Client) o Visual Studio, cubriendo pruebas de middleware (`401`, `500`), operaciones CRUD autenticadas y casos extremos (`400`, `404`, `409`).

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

---

## Middleware corporativo: auditoría, gestión de errores y autenticación (Actividad 3)

### Componentes implementados (`Middleware/`)

1. **`ExceptionHandlingMiddleware`** ([Middleware/ExceptionHandlingMiddleware.cs](./Middleware/ExceptionHandlingMiddleware.cs)):
   - Envuelve la ejecución posterior (`await _next(context)`) en un bloque `try-catch` global.
   - Registra el detalle completo de la excepción mediante `ILogger<ExceptionHandlingMiddleware>` (`LogError`) y, si la respuesta aún no comenzó a enviarse, limpia el buffer y responde con código `500 Internal Server Error` y JSON uniforme:
     ```json
     { "error": "Internal server error." }
     ```
2. **`TokenAuthenticationMiddleware`** ([Middleware/TokenAuthenticationMiddleware.cs](./Middleware/TokenAuthenticationMiddleware.cs)):
   - Carga los tokens autorizados desde `Authentication:ValidTokens` en [appsettings.json](./appsettings.json) (`techhive-hr-token-2026`, `techhive-it-token-2026`) como arreglos de bytes UTF-8 al iniciar.
   - Exige la cabecera `Authorization: Bearer <token>` en los endpoints de la API (permitiendo `/openapi` en entorno `Development`) y compara los bytes en tiempo constante mediante `CryptographicOperations.FixedTimeEquals`.
   - Si falta el token o es inválido, registra una advertencia (`401 Unauthorized`), establece `WWW-Authenticate: Bearer` y corta el pipeline retornando `401` con `{ "error": "Unauthorized. Missing or invalid token." }`.
3. **`RequestResponseLoggingMiddleware`** ([Middleware/RequestResponseLoggingMiddleware.cs](./Middleware/RequestResponseLoggingMiddleware.cs)):
   - Mide la latencia con `Stopwatch` y registra para cada solicitud autenticada el método HTTP, la ruta, el código de estado final y el tiempo de respuesta en milisegundos (`Audit HTTP {Method} {Path} => {StatusCode} ({ElapsedMs}ms)`).
   - Si ocurre una excepción no controlada aguas abajo, registra `Audit HTTP {Method} {Path} => 500 (unhandled exception, ...)` y relanza la excepción para que `ExceptionHandlingMiddleware` emita la respuesta estandarizada.

### Orden óptimo de la canalización en `Program.cs`

En [Program.cs](./Program.cs), los middlewares se registran exactamente en el siguiente orden:

1. `app.UseMiddleware<ExceptionHandlingMiddleware>();` — Debe envolver toda la canalización para capturar excepciones de cualquier middleware posterior o controlador.
2. `app.UseMiddleware<TokenAuthenticationMiddleware>();` — Rechaza peticiones sin token válido en etapas tempranas (`401`), evitando consumir recursos en auditoría detallada, enrutamiento o controladores.
3. `app.UseMiddleware<RequestResponseLoggingMiddleware>();` — Audita todas las solicitudes autorizadas junto con su código de estado saliente y duración.

### Configuración y uso de tokens

En [appsettings.json](./appsettings.json):

```json
"Authentication": {
  "ValidTokens": [
    "techhive-hr-token-2026",
    "techhive-it-token-2026"
  ]
}
```

Para pruebas con `curl`:

```bash
# Rechazo sin token (401)
curl -i http://127.0.0.1:5099/api/users

# Acceso autorizado (200)
curl -i -H "Authorization: Bearer techhive-it-token-2026" http://127.0.0.1:5099/api/users

# Simulación de excepción no controlada en Development (500 JSON)
curl -i -H "Authorization: Bearer techhive-it-token-2026" http://127.0.0.1:5099/api/test-error
```

### Resultados verificados de middleware y auditoría

| Escenario verificado | Código HTTP | Respuesta / Registro verificado |
| --- | --- | --- |
| `GET /api/users` sin cabecera `Authorization` | `401` | `{"error":"Unauthorized. Missing or invalid token."}` + `warn: ... rejected with 401 Unauthorized.` |
| `GET /api/users` con token inválido | `401` | `{"error":"Unauthorized. Missing or invalid token."}` + `warn: ... rejected with 401 Unauthorized.` |
| `GET /api/users` con token válido | `200` | `Audit HTTP GET /api/users => 200` |
| `POST /api/users` con token válido | `201` | `Audit HTTP POST /api/users => 201` |
| `PUT /api/users/4` con token válido | `200` | `Audit HTTP PUT /api/users/4 => 200` |
| `DELETE /api/users/4` con token válido | `204` | `Audit HTTP DELETE /api/users/4 => 204` |
| `GET /api/test-error` con token válido | `500` | `{"error":"Internal server error."}` + `Audit HTTP GET /api/test-error => 500` + `fail: ... Excepción no controlada` |
| `GET /openapi/v1.json` en `Development` | `200` | Esquema OpenAPI 3.1.1 accesible + `Audit HTTP GET /openapi/v1.json => 200` |

### Cómo contribuyó Microsoft Copilot en la fase de middleware

- **Andamiaje modular**: Generó la estructura base de `ExceptionHandlingMiddleware`, `TokenAuthenticationMiddleware` y `RequestResponseLoggingMiddleware` siguiendo las convenciones de ASP.NET Core (`InvokeAsync(HttpContext)` y `ILogger<T>`).
- **Seguridad y rendimiento**: Incorporó comparación de tokens en tiempo constante (`CryptographicOperations.FixedTimeEquals`), lectura de tokens desde `IConfiguration` sin valores fijos en el código y cortocircuito temprano para solicitudes no autorizadas.
- **Ordenamiento y verificación**: Garantizó la secuencia exacta `Errores -> Autenticación -> Registro` en `Program.cs` junto con un endpoint diagnóstico exclusivo de `Development` (`/api/test-error`) para validar de forma segura la captura de excepciones y los registros de auditoría.
