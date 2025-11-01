namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Opciones para realizar una restauración de MongoDB
/// </summary>
public class RestoreOptions
{
    /// <summary>
    /// Nombre de la base de datos a restaurar (obligatorio)
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Host de MongoDB (por defecto: localhost)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Puerto de MongoDB (por defecto: 27017)
    /// </summary>
    public int Port { get; set; } = 27017;

    /// <summary>
    /// Usuario para autenticación (opcional)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Contraseña para autenticación (opcional)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Base de datos de autenticación (por defecto: admin)
    /// </summary>
    public string AuthenticationDatabase { get; set; } = "admin";

    /// <summary>
    /// URI de conexión completa (alternativa a host/port/user/password)
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Ruta de origen del backup a restaurar (obligatorio)
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se debe ejecutar dentro de un contenedor Docker
    /// </summary>
    public bool InDocker { get; set; }

    /// <summary>
    /// Nombre del contenedor Docker (requerido si InDocker es true)
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// Nivel de verbosidad para logs
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Indica si se debe eliminar la base de datos antes de restaurar (--drop)
    /// </summary>
    public bool Drop { get; set; }

    /// <summary>
    /// Formato de compresión del backup a restaurar (se auto-detecta si no se especifica)
    /// </summary>
    public CompressionFormat CompressionFormat { get; set; } = CompressionFormat.None;
}
