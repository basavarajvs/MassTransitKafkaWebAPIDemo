using Messages;

namespace Api.SagaFramework
{
    /// <summary>
    /// Generic step base class providing common functionality for saga steps.
    /// Uses command factories for type-safe command creation.
    /// </summary>
    /// <typeparam name="TCommand">The command type this step creates</typeparam>
    /// <typeparam name="TData">The data type extracted from message</typeparam>
    /// <typeparam name="TSagaState">The saga state type</typeparam>
    public abstract class GenericStepBase<TCommand, TData, TSagaState>
        where TCommand : class
    {
        protected readonly ILogger _logger;
        protected readonly ICommandFactory<TCommand, TData> _commandFactory;
        protected readonly string _messageKey;
        protected readonly int _maxRetries;

        /// <summary>
        /// Constructor with dependency injection of command factory.
        /// </summary>
        /// <param name="logger">Logger for observability</param>
        /// <param name="commandFactory">Command factory for creating commands</param>
        /// <param name="messageKey">Key to extract data from message StepData</param>
        /// <param name="maxRetries">Maximum retry attempts for this step</param>
        protected GenericStepBase(
            ILogger logger, 
            ICommandFactory<TCommand, TData> commandFactory,
            string messageKey, 
            int maxRetries = 3)
        {
            _logger = logger;
            _commandFactory = commandFactory;
            _messageKey = messageKey;
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// Create command using the injected factory.
        /// </summary>
        /// <typeparam name="TMessage">Message type (usually Messages.Message)</typeparam>
        /// <param name="correlationId">Saga correlation ID</param>
        /// <param name="message">Message containing step data</param>
        /// <param name="retryCount">Current retry attempt</param>
        /// <returns>Fully initialized command</returns>
        public TCommand CreateCommand<TMessage>(Guid correlationId, TMessage message, int retryCount = 0)
        {
            var stepData = ExtractStepData(message);
            return _commandFactory.Create(correlationId, stepData, retryCount);
        }

        /// <summary>
        /// Extract step-specific data from message StepData dictionary.
        /// </summary>
        /// <typeparam name="TMessage">Message type</typeparam>
        /// <param name="message">Message containing StepData</param>
        /// <returns>Strongly-typed step data</returns>
        /// <exception cref="InvalidOperationException">If step data not found or wrong type</exception>
        protected TData ExtractStepData<TMessage>(TMessage message)
        {
            if (message is Message msg && msg.StepData.TryGetValue(_messageKey, out var data))
            {
                if (data is TData typedData)
                {
                    return typedData;
                }
                throw new InvalidOperationException($"Step data for key '{_messageKey}' is of type {data.GetType().Name}, expected {typeof(TData).Name}");
            }
            throw new InvalidOperationException($"Step data not found for key: {_messageKey}");
        }

        /// <summary>
        /// Handle step failure and determine if retry should be attempted.
        /// </summary>
        /// <param name="sagaState">Current saga state</param>
        /// <param name="error">Error message or exception details</param>
        /// <param name="currentRetryCount">Current retry attempt number</param>
        /// <returns>True if step should be retried, false to fail the saga</returns>
        public virtual bool HandleFailureAndShouldRetry(TSagaState sagaState, string error, int currentRetryCount)
        {
            _logger.LogWarning($"Step failed (attempt {currentRetryCount}/{_maxRetries}): {error}");
            
            // Update saga state with error info
            UpdateSagaStateOnFailure(sagaState, error, currentRetryCount);
            
            // Default retry logic - can be overridden by subclasses
            return currentRetryCount < _maxRetries;
        }

        /// <summary>
        /// Handle successful step completion.
        /// </summary>
        /// <param name="sagaState">Current saga state</param>
        /// <param name="response">Successful response from external API</param>
        public virtual void HandleSuccess(TSagaState sagaState, string response)
        {
            _logger.LogInformation($"Step completed successfully");
            
            // Update saga state with success info
            UpdateSagaStateOnSuccess(sagaState, response);
        }

        /// <summary>
        /// Update saga state on step failure - can be overridden for custom state management.
        /// </summary>
        /// <param name="sagaState">Saga state to update</param>
        /// <param name="error">Error details</param>
        /// <param name="retryCount">Current retry count</param>
        protected abstract void UpdateSagaStateOnFailure(TSagaState sagaState, string error, int retryCount);

        /// <summary>
        /// Update saga state on step success - can be overridden for custom state management.
        /// </summary>
        /// <param name="sagaState">Saga state to update</param>
        /// <param name="response">Success response</param>
        protected abstract void UpdateSagaStateOnSuccess(TSagaState sagaState, string response);
    }
}