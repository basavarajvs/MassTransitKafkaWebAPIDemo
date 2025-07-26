using MassTransit;

namespace Api.SagaFramework
{
    /// <summary>
    /// Generic interface for saga workflows - defines the contract for any domain saga
    /// </summary>
    public interface ISagaWorkflow<TState> where TState : SagaStateMachineInstance
    {
        /// <summary>
        /// The saga state machine instance
        /// </summary>
        TState State { get; }

        /// <summary>
        /// Starts the saga workflow with the provided message
        /// </summary>
        Task StartAsync<TMessage>(TMessage message) where TMessage : class;

        /// <summary>
        /// Gets the current status of the workflow
        /// </summary>
        WorkflowStatus GetStatus();

        /// <summary>
        /// Gets the correlation ID for this workflow instance
        /// </summary>
        Guid CorrelationId { get; }
    }

    /// <summary>
    /// Workflow status enumeration
    /// </summary>
    public enum WorkflowStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Base interface for step execution within a workflow
    /// </summary>
    public interface IWorkflowStep<TState> where TState : SagaStateMachineInstance
    {
        /// <summary>
        /// The name of this step
        /// </summary>
        string StepName { get; }

        /// <summary>
        /// Executes the step with the provided context
        /// </summary>
        Task<StepResult> ExecuteAsync(TState state, object data);

        /// <summary>
        /// Checks if this step can be retried
        /// </summary>
        bool CanRetry(TState state);

        /// <summary>
        /// Gets the maximum number of retries for this step
        /// </summary>
        int MaxRetries { get; }
    }

    /// <summary>
    /// Result of step execution
    /// </summary>
    public record StepResult
    {
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public object? Data { get; init; }
        public bool ShouldRetry { get; init; }

        public static StepResult Success(object? data = null) => new() { IsSuccess = true, Data = data };
        public static StepResult Failure(string error, bool shouldRetry = true) => new() 
        { 
            IsSuccess = false, 
            ErrorMessage = error, 
            ShouldRetry = shouldRetry 
        };
    }
} 