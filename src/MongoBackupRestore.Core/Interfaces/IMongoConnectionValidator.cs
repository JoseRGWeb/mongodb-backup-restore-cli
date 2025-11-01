namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para validar conexiones a MongoDB con credenciales
/// </summary>
public interface IMongoConnectionValidator
{
    /// <summary>
    /// Valida la conexión a MongoDB con las credenciales proporcionadas
    /// </summary>
    /// <param name="host">Host de MongoDB</param>
    /// <param name="port">Puerto de MongoDB</param>
    /// <param name="username">Usuario para autenticación (opcional)</param>
    /// <param name="password">Contraseña para autenticación (opcional)</param>
    /// <param name="authenticationDatabase">Base de datos de autenticación</param>
    /// <param name="uri">URI de conexión completa (alternativa a host/port/user/password)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Tupla con éxito de validación, mensaje de error si aplica</returns>
    Task<(bool Success, string? ErrorMessage)> ValidateConnectionAsync(
        string host,
        int port,
        string? username,
        string? password,
        string authenticationDatabase,
        string? uri,
        CancellationToken cancellationToken = default);
}
