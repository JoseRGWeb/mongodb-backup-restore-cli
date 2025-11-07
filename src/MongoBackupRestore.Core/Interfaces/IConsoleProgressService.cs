namespace MongoBackupRestore.Core.Interfaces;

/// <summary>
/// Servicio para gestionar la visualización de progreso y mensajes en la consola
/// </summary>
public interface IConsoleProgressService
{
    /// <summary>
    /// Inicia una operación con indicador de progreso
    /// </summary>
    /// <param name="description">Descripción de la operación</param>
    /// <param name="action">Acción a ejecutar</param>
    Task ExecuteWithProgressAsync(string description, Func<Task> action);

    /// <summary>
    /// Inicia una operación con indicador de progreso y retorna un resultado
    /// </summary>
    /// <typeparam name="T">Tipo del resultado</typeparam>
    /// <param name="description">Descripción de la operación</param>
    /// <param name="action">Función a ejecutar</param>
    /// <returns>Resultado de la operación</returns>
    Task<T> ExecuteWithProgressAsync<T>(string description, Func<Task<T>> action);

    /// <summary>
    /// Muestra un mensaje de éxito
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    void ShowSuccess(string message);

    /// <summary>
    /// Muestra un mensaje de error
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    void ShowError(string message);

    /// <summary>
    /// Muestra un mensaje informativo
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    void ShowInfo(string message);

    /// <summary>
    /// Muestra un mensaje de advertencia
    /// </summary>
    /// <param name="message">Mensaje a mostrar</param>
    void ShowWarning(string message);

    /// <summary>
    /// Muestra un panel con información
    /// </summary>
    /// <param name="title">Título del panel</param>
    /// <param name="content">Contenido del panel</param>
    void ShowPanel(string title, string content);

    /// <summary>
    /// Muestra una tabla con información clave-valor
    /// </summary>
    /// <param name="title">Título de la tabla</param>
    /// <param name="data">Datos a mostrar</param>
    void ShowTable(string title, Dictionary<string, string> data);
}
