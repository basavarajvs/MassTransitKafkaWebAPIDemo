using MassTransit;
using System.Reflection;

namespace Api.SagaFramework
{
    /// <summary>
    /// Generic step attribute - works for ANY domain (Order, Payment, Shipping, etc.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SagaStepAttribute : Attribute
    {
        public required string StepName { get; init; }
        public required string MessageKey { get; init; }
        public int MaxRetries { get; init; } = 3;
        public required string DataPropertyName { get; init; } // e.g., "OrderData", "PaymentData", "EmailData", "NotificationData"
    }

    /// <summary>
    /// Generic configuration that works with ANY saga state type
    /// </summary>
    public record GenericStepConfiguration<TState> where TState : SagaStateMachineInstance
    {
        public required string StepName { get; init; }
        public required string MessageKey { get; init; }
        public int MaxRetries { get; init; } = 3;
        public required Func<TState, int> GetRetryCount { get; init; }
        public required Action<TState, int> SetRetryCount { get; init; }
        public required Func<TState, bool> IsCompleted { get; init; }
        public required Action<TState, string> MarkCompleted { get; init; }
    }

    /// <summary>
    /// Completely generic step factory - works with ANY saga state type
    /// </summary>
    public static class GenericStepFactory
    {
        public static GenericStepConfiguration<TState> Create<TStep, TState>()
            where TStep : class
            where TState : SagaStateMachineInstance, new()
        {
            var stepType = typeof(TStep);
            var attribute = stepType.GetCustomAttribute<SagaStepAttribute>()
                ?? throw new InvalidOperationException($"{stepType.Name} must have [SagaStep] attribute");

            var sampleState = new TState();
            var stepName = attribute.StepName;

            return new GenericStepConfiguration<TState>
            {
                StepName = attribute.StepName,
                MessageKey = attribute.MessageKey,
                MaxRetries = attribute.MaxRetries,
                GetRetryCount = CreateRetryCountGetter<TState>(stepName),
                SetRetryCount = CreateRetryCountSetter<TState>(stepName),
                IsCompleted = CreateCompletionChecker<TState>(stepName),
                MarkCompleted = CreateCompletionMarker<TState>(stepName)
            };
        }

        // Generic property accessors using flexible naming conventions
        private static Func<TState, int> CreateRetryCountGetter<TState>(string stepName)
            where TState : SagaStateMachineInstance
        {
            var possibleNames = new[] { 
                $"{stepName}RetryCount", 
                $"{stepName}Retries", 
                $"Retry{stepName}Count",
                $"{stepName.ToLower()}RetryCount"
            };

            var property = FindProperty<TState>(possibleNames);
            return state => (int)(property.GetValue(state) ?? 0);
        }

        private static Action<TState, int> CreateRetryCountSetter<TState>(string stepName)
            where TState : SagaStateMachineInstance
        {
            var possibleNames = new[] { 
                $"{stepName}RetryCount", 
                $"{stepName}Retries", 
                $"Retry{stepName}Count",
                $"{stepName.ToLower()}RetryCount"
            };

            var property = FindProperty<TState>(possibleNames);
            return (state, count) => property.SetValue(state, count);
        }

        private static Func<TState, bool> CreateCompletionChecker<TState>(string stepName)
            where TState : SagaStateMachineInstance
        {
            // Generate past-tense variations for better property name matching
            var pastTenseVariations = GeneratePastTenseVariations(stepName);
            
            var possibleNames = new[] { 
                $"{stepName}ApiCalled", 
                $"{stepName}Completed", 
                $"Is{stepName}Complete",
                $"{stepName.ToLower()}Completed"
            }.Concat(pastTenseVariations.Select(past => $"{past}ApiCalled"))
             .Concat(pastTenseVariations.Select(past => $"{past}Completed"))
             .ToArray();

            var property = FindProperty<TState>(possibleNames);
            return state => (bool)(property.GetValue(state) ?? false);
        }

        /// <summary>
        /// Generates past-tense variations of step names to match property naming conventions
        /// </summary>
        private static string[] GeneratePastTenseVariations(string stepName)
        {
            var variations = new List<string>();
            
            // Handle common past-tense patterns
            if (stepName.EndsWith("Create", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Create", "Created"));
            if (stepName.EndsWith("Process", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Process", "Processed"));
            if (stepName.EndsWith("Ship", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Ship", "Shipped"));
            if (stepName.EndsWith("Send", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Send", "Sent"));
            if (stepName.EndsWith("Pay", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Pay", "Paid"));
            if (stepName.EndsWith("Authorize", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Authorize", "Authorized"));
            if (stepName.EndsWith("Capture", StringComparison.OrdinalIgnoreCase))
                variations.Add(stepName.Replace("Capture", "Captured"));
                
            // Add generic past-tense ending if no specific rule matched
            if (variations.Count == 0)
            {
                variations.Add($"{stepName}ed"); // Generic past tense
                variations.Add($"{stepName}d");  // If already ends with 'e'
            }
            
            return variations.ToArray();
        }

        private static Action<TState, string> CreateCompletionMarker<TState>(string stepName)
            where TState : SagaStateMachineInstance
        {
            var pastTenseVariations = GeneratePastTenseVariations(stepName);
            
            var possibleNames = new[] { 
                $"{stepName}ApiCalled", 
                $"{stepName}Completed", 
                $"Is{stepName}Complete",
                $"{stepName.ToLower()}Completed"
            }.Concat(pastTenseVariations.Select(past => $"{past}ApiCalled"))
             .Concat(pastTenseVariations.Select(past => $"{past}Completed"))
             .ToArray();
            
            var responseNames = new[] { 
                $"{stepName}Response", 
                $"{stepName}Result", 
                $"{stepName.ToLower()}Response"
            }.Concat(pastTenseVariations.Select(past => $"{past}Response"))
             .Concat(pastTenseVariations.Select(past => $"{past}Result"))
             .ToArray();

            var completedProperty = FindProperty<TState>(possibleNames);
            
            // Find the response property - don't throw if not found
            PropertyInfo? responseProperty = null;
            foreach (var responseName in responseNames)
            {
                responseProperty = typeof(TState).GetProperty(responseName);
                if (responseProperty != null) break;
            }

            return (state, response) => {
                completedProperty.SetValue(state, true);
                responseProperty?.SetValue(state, response);
                
                // Try to set LastUpdated if it exists
                var lastUpdatedProperty = state.GetType().GetProperty("LastUpdated");
                lastUpdatedProperty?.SetValue(state, DateTime.UtcNow);
            };
        }

        private static PropertyInfo FindProperty<TState>(string[] possibleNames)
            where TState : SagaStateMachineInstance
        {
            var stateType = typeof(TState);
            
            foreach (var name in possibleNames)
            {
                var property = stateType.GetProperty(name);
                if (property != null) return property;
            }

            throw new InvalidOperationException(
                $"Could not find any property with names: {string.Join(", ", possibleNames)} in {stateType.Name}");
        }
    }

    /// <summary>
    /// Generic command factory that discovers property names automatically
    /// </summary>
    public static class GenericCommandFactory
    {
        public static TCommand Create<TCommand>(Guid correlationId, object data, string dataPropertyName, int retryCount = 0)
            where TCommand : class
        {
            var commandType = typeof(TCommand);
            
            // Create the command using reflection to bypass the new() constraint
            var constructorParams = new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RetryCount"] = retryCount,
                [dataPropertyName] = data
            };

            return CreateInstance<TCommand>(constructorParams);
        }

        private static TCommand CreateInstance<TCommand>(Dictionary<string, object> parameters)
            where TCommand : class
        {
            var type = typeof(TCommand);
            var constructors = type.GetConstructors();

            // Try parameterless constructor first
            var parameterlessConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            if (parameterlessConstructor != null)
            {
                var instance = (TCommand)Activator.CreateInstance(type)!;
                
                // Set properties using reflection
                foreach (var (propertyName, value) in parameters)
                {
                    SetPropertyIfExists(instance, propertyName, value);
                }
                
                return instance;
            }

            // For records with required properties, use constructor parameter matching
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
            if (constructor == null)
                throw new InvalidOperationException($"No suitable constructor found for {type.Name}");

            var constructorParams = constructor.GetParameters();
            var args = new object[constructorParams.Length];
            
            for (int i = 0; i < constructorParams.Length; i++)
            {
                var param = constructorParams[i];
                var paramName = param.Name!;
                
                // Try exact parameter name match first (case-insensitive)
                var matchingKey = parameters.Keys.FirstOrDefault(k => 
                    string.Equals(k, paramName, StringComparison.OrdinalIgnoreCase));
                
                if (matchingKey != null)
                {
                    args[i] = parameters[matchingKey];
                }
                else if (param.ParameterType == typeof(Guid))
                {
                    // Assume Guid parameter is CorrelationId
                    args[i] = parameters.GetValueOrDefault("CorrelationId", Guid.Empty);
                }
                else if (param.ParameterType == typeof(int))
                {
                    // Assume int parameter is RetryCount
                    args[i] = parameters.GetValueOrDefault("RetryCount", 0);
                }
                else
                {
                    // For remaining parameters, find the first unmatched value that isn't standard parameters
                    var usedValues = new HashSet<object>();
                    if (parameters.ContainsKey("CorrelationId")) usedValues.Add(parameters["CorrelationId"]);
                    if (parameters.ContainsKey("RetryCount")) usedValues.Add(parameters["RetryCount"]);
                    
                    var dataValue = parameters.Values.FirstOrDefault(v => !usedValues.Contains(v));
                    args[i] = dataValue ?? throw new InvalidOperationException(
                        $"Cannot find suitable value for parameter '{paramName}' of type {param.ParameterType.Name}");
                }
            }
            
            return (TCommand)Activator.CreateInstance(type, args)!;
        }

        private static bool SetPropertyIfExists(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Generic base class that works with ANY saga state type
    /// </summary>
    public abstract class GenericStepBase<TCommand, TState> 
        where TCommand : class
        where TState : SagaStateMachineInstance
    {
        protected readonly ILogger _logger;
        protected readonly GenericStepConfiguration<TState> _config;
        protected readonly string _dataPropertyName;

        protected GenericStepBase(ILogger logger, GenericStepConfiguration<TState> config, string dataPropertyName)
        {
            _logger = logger;
            _config = config;
            _dataPropertyName = dataPropertyName;
        }

        public string StepName => _config.StepName;
        public string MessageKey => _config.MessageKey;
        public bool IsCompleted(TState state) => _config.IsCompleted(state);

        public bool ShouldRetry(int currentRetryCount) => currentRetryCount < _config.MaxRetries;

        public void HandleSuccess(TState state, string response)
        {
            _config.MarkCompleted(state, response);
            _logger.LogInformation($"✅ {StepName} succeeded for correlation ID: {state.CorrelationId}");
        }

        public bool HandleFailureAndShouldRetry(TState state, string error, int retryCount)
        {
            _config.SetRetryCount(state, retryCount);
            _logger.LogWarning($"❌ {StepName} failed for correlation ID: {state.CorrelationId}, " +
                             $"Retry: {retryCount}/{_config.MaxRetries}, Error: {error}");
            
            if (ShouldRetry(retryCount))
            {
                return true;
            }
            else
            {
                _logger.LogError($"💀 {StepName} failed permanently for correlation ID: {state.CorrelationId} after {_config.MaxRetries} retries");
                return false;
            }
        }

        protected object ExtractStepData<TMessage>(TMessage message) where TMessage : class
        {
            var messageType = typeof(TMessage);
            var stepDataProperty = messageType.GetProperty("StepData");
            
            if (stepDataProperty?.GetValue(message) is Dictionary<string, object> stepData)
            {
                return stepData.GetValueOrDefault(MessageKey)
                       ?? throw new InvalidOperationException($"{MessageKey} data not found in message");
            }
            
            throw new InvalidOperationException($"StepData property not found on {messageType.Name}");
        }

        public abstract TCommand CreateCommand<TMessage>(Guid correlationId, TMessage message, int retryCount = 0) where TMessage : class;
    }
} 