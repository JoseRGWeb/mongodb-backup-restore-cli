namespace MongoBackupRestore.Core.Models;

/// <summary>
/// Información sobre las herramientas de MongoDB instaladas
/// </summary>
public class MongoToolsInfo
{
    /// <summary>
    /// Indica si mongodump está disponible
    /// </summary>
    public bool MongoDumpAvailable { get; set; }

    /// <summary>
    /// Ruta completa de mongodump
    /// </summary>
    public string? MongoDumpPath { get; set; }

    /// <summary>
    /// Versión de mongodump
    /// </summary>
    public string? MongoDumpVersion { get; set; }

    /// <summary>
    /// Indica si mongorestore está disponible
    /// </summary>
    public bool MongoRestoreAvailable { get; set; }

    /// <summary>
    /// Ruta completa de mongorestore
    /// </summary>
    public string? MongoRestorePath { get; set; }

    /// <summary>
    /// Versión de mongorestore
    /// </summary>
    public string? MongoRestoreVersion { get; set; }

    /// <summary>
    /// Indica si Docker está disponible
    /// </summary>
    public bool DockerAvailable { get; set; }

    /// <summary>
    /// Versión de Docker
    /// </summary>
    public string? DockerVersion { get; set; }
}
