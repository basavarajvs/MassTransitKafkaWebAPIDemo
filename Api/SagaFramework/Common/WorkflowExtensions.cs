using MassTransit;
using System.Reflection;

namespace Api.SagaFramework.Common
{
    /// <summary>
    /// Extension methods for workflow and saga operations
    /// </summary>
    public static class WorkflowExtensions
    {
        /// <summary>
        /// Safely gets a property value using reflection with fallback
        /// </summary>
        public static T? GetPropertyValue<T>(this object obj, string propertyName, T? defaultValue = default)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(obj);
                    return value is T typedValue ? typedValue : defaultValue;
                }
            }
            catch
            {
                // Silently return default on any error
            }
            return defaultValue;
        }

        /// <summary>
        /// Safely sets a property value using reflection
        /// </summary>
        public static bool TrySetPropertyValue(this object obj, string propertyName, object? value)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(obj, value);
                    return true;
                }
            }
            catch
            {
                // Silently fail
            }
            return false;
        }

        /// <summary>
        /// Gets the current state name from a saga state machine instance
        /// </summary>
        public static string GetCurrentStateName<TState>(this TState state) where TState : SagaStateMachineInstance
        {
            return state.GetPropertyValue<string>("CurrentState", "Unknown");
        }

        /// <summary>
        /// Checks if a saga instance is in a final state
        /// </summary>
        public static bool IsInFinalState<TState>(this TState state) where TState : SagaStateMachineInstance
        {
            var currentState = state.GetCurrentStateName();
            return currentState.Equals("Final", StringComparison.OrdinalIgnoreCase) ||
                   currentState.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                   currentState.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
                   currentState.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a correlation ID with optional prefix
        /// </summary>
        public static Guid CreateCorrelationId(string? prefix = null)
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Formats a correlation ID for logging
        /// </summary>
        public static string FormatCorrelationId(this Guid correlationId, int maxLength = 8)
        {
            return correlationId.ToString("N")[..Math.Min(maxLength, 32)].ToUpper();
        }
    }

    /// <summary>
    /// Common constants used across the framework
    /// </summary>
    public static class WorkflowConstants
    {
        public const int DefaultMaxRetries = 3;
        public const int DefaultTimeoutSeconds = 30;
        public const string DefaultCorrelationIdHeaderName = "CorrelationId";
        public const string DefaultRetryCountHeaderName = "RetryCount";
        
        /// <summary>
        /// Standard property names for saga states
        /// </summary>
        public static class StateProperties
        {
            public const string CorrelationId = "CorrelationId";
            public const string CurrentState = "CurrentState";
            public const string StartedAt = "StartedAt";
            public const string CompletedAt = "CompletedAt";
            public const string LastUpdated = "LastUpdated";
            public const string LastError = "LastError";
        }

        /// <summary>
        /// Standard suffixes for step properties
        /// </summary>
        public static class StepSuffixes
        {
            public const string RetryCount = "RetryCount";
            public const string ApiCalled = "ApiCalled";
            public const string Completed = "Completed";
            public const string Response = "Response";
            public const string Result = "Result";
        }
    }

    /// <summary>
    /// Utility class for working with step configurations
    /// </summary>
    public static class StepConfigurationUtilities
    {
        /// <summary>
        /// Generates property names for a step using standard conventions
        /// </summary>
        public static StepPropertyNames GeneratePropertyNames(string stepName)
        {
            return new StepPropertyNames
            {
                RetryCount = $"{stepName}{WorkflowConstants.StepSuffixes.RetryCount}",
                IsCompleted = $"{stepName}{WorkflowConstants.StepSuffixes.ApiCalled}",
                Response = $"{stepName}{WorkflowConstants.StepSuffixes.Response}",
                AlternateCompleted = $"{stepName}{WorkflowConstants.StepSuffixes.Completed}",
                AlternateResponse = $"{stepName}{WorkflowConstants.StepSuffixes.Result}"
            };
        }
    }

    /// <summary>
    /// Standard property names for a step
    /// </summary>
    public record StepPropertyNames
    {
        public required string RetryCount { get; init; }
        public required string IsCompleted { get; init; }
        public required string Response { get; init; }
        public required string AlternateCompleted { get; init; }
        public required string AlternateResponse { get; init; }
    }
} 