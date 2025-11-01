namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Interfaz para ejecutar procesos externos
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Ejecuta un proceso externo
    /// </summary>
    /// <param name="fileName">Nombre del ejecutable o comando</param>
    /// <param name="arguments">Argumentos del comando</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Tupla con código de salida, salida estándar y error estándar</returns>
    Task<(int exitCode, string output, string error)> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default);
}
