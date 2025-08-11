namespace Api.SagaFramework
{
    /// <summary>
    /// Generic command factory interface for type-safe command creation.
    /// Each command type implements this interface for consistent command creation.
    /// </summary>
    /// <typeparam name="TCommand">Type of command to create</typeparam>
    /// <typeparam name="TData">Type of data used to create the command</typeparam>
    public interface ICommandFactory<TCommand, TData>
        where TCommand : class
    {
        /// <summary>
        /// Create a command instance with the specified parameters.
        /// </summary>
        /// <param name="correlationId">Correlation ID for saga tracking</param>
        /// <param name="data">Data for command creation</param>
        /// <param name="retryCount">Number of retry attempts (default 0)</param>
        /// <returns>Fully initialized command ready for publishing</returns>
        TCommand Create(Guid correlationId, TData data, int retryCount = 0);
    }
}