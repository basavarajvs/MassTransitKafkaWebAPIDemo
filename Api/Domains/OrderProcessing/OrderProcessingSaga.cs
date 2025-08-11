using MassTransit;
using Api.Domains.OrderProcessing.CommandFactories;
using Messages;

namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Order Processing Saga - Domain-specific saga for order processing workflow
    /// Uses Factory Interface Pattern for optimal command creation
    /// </summary>
    public class OrderProcessingSaga : MassTransitStateMachine<OrderProcessingSagaState>
    {
        private readonly ILogger<OrderProcessingSaga> _logger;
        private readonly OrderCreateCommandFactory _createFactory;
        private readonly OrderProcessCommandFactory _processFactory;
        private readonly OrderShipCommandFactory _shipFactory;

        public OrderProcessingSaga(
            ILogger<OrderProcessingSaga> logger,
            OrderCreateCommandFactory createFactory,
            OrderProcessCommandFactory processFactory,
            OrderShipCommandFactory shipFactory)
        {
            _logger = logger;
            _createFactory = createFactory;
            _processFactory = processFactory;
            _shipFactory = shipFactory;

            ConfigureEvents();
            ConfigureStates();
            ConfigureWorkflow();
        }

        #region States & Events
        public State WaitingForOrderCreate { get; private set; } = null!;
        public State WaitingForOrderProcess { get; private set; } = null!;
        public State WaitingForOrderShip { get; private set; } = null!;

        public Event<OrderProcessingSagaStarted> SagaStarted { get; private set; } = null!;
        public Event<OrderCreateApiSucceeded> OrderCreateApiSucceeded { get; private set; } = null!;
        public Event<OrderCreateApiFailed> OrderCreateApiFailed { get; private set; } = null!;
        public Event<OrderProcessApiSucceeded> OrderProcessApiSucceeded { get; private set; } = null!;
        public Event<OrderProcessApiFailed> OrderProcessApiFailed { get; private set; } = null!;
        public Event<OrderShipApiSucceeded> OrderShipApiSucceeded { get; private set; } = null!;
        public Event<OrderShipApiFailed> OrderShipApiFailed { get; private set; } = null!;
        #endregion

        private void ConfigureEvents()
        {
            Event(() => SagaStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderCreateApiSucceeded, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderCreateApiFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderProcessApiSucceeded, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderProcessApiFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderShipApiSucceeded, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => OrderShipApiFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
        }

        private void ConfigureStates() => InstanceState(x => x.CurrentState);

        private void ConfigureWorkflow()
        {
            // 🚀 Start: Initialize saga and begin order creation
            Initially(
                When(SagaStarted)
                    .Then(context => {
                        // Initialize saga state directly (no longer coupled to step classes)
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.OriginalMessage = context.Message.OriginalMessage;
                        context.Saga.OriginalMessageJson = System.Text.Json.JsonSerializer.Serialize(context.Message.OriginalMessage);
                        context.Saga.StartedAt = context.Message.StartedAt;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                        
                        _logger.LogInformation($"🚀 Saga started for correlation ID: {context.Saga.CorrelationId}");
                    })
                    .PublishAsync(context => context.Init<CallOrderCreateApi>(_createFactory.Create(context.Saga.CorrelationId, ExtractOrderData(context.Message.OriginalMessage))))
                    .TransitionTo(WaitingForOrderCreate)
            );

            // 📦 Order Create: Success → Process, Failure → Retry or End
            During(WaitingForOrderCreate,
                When(OrderCreateApiSucceeded)
                    .Then(context => {
                        context.Saga.OrderCreatedApiCalled = true;
                        context.Saga.OrderCreateResponse = context.Message.Response;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                    })
                    .PublishAsync(context => context.Init<CallOrderProcessApi>(_processFactory.Create(context.Saga.CorrelationId, ExtractProcessData(context.Saga.OriginalMessage!))))
                    .TransitionTo(WaitingForOrderProcess),
                When(OrderCreateApiFailed)
                    .IfElse(context => ShouldRetryStep(context.Saga.OrderCreateRetryCount, maxRetries: 3),
                        retry => retry
                            .Then(context => {
                                context.Saga.OrderCreateRetryCount++;
                                context.Saga.LastError = context.Message.Error;
                                context.Saga.LastUpdated = DateTime.UtcNow;
                            })
                            .PublishAsync(context => context.Init<CallOrderCreateApi>(_createFactory.Create(context.Saga.CorrelationId, ExtractOrderData(context.Saga.OriginalMessage!), context.Saga.OrderCreateRetryCount))),
                        fail => fail.Finalize())
            );

            // ⚙️ Order Process: Success → Ship, Failure → Retry or End
            During(WaitingForOrderProcess,
                When(OrderProcessApiSucceeded)
                    .Then(context => {
                        context.Saga.OrderProcessedApiCalled = true;
                        context.Saga.OrderProcessResponse = context.Message.Response;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                    })
                    .PublishAsync(context => context.Init<CallOrderShipApi>(_shipFactory.Create(context.Saga.CorrelationId, ExtractShipData(context.Saga.OriginalMessage!))))
                    .TransitionTo(WaitingForOrderShip),
                When(OrderProcessApiFailed)
                    .IfElse(context => ShouldRetryStep(context.Saga.OrderProcessRetryCount, maxRetries: 3),
                        retry => retry
                            .Then(context => {
                                context.Saga.OrderProcessRetryCount++;
                                context.Saga.LastError = context.Message.Error;
                                context.Saga.LastUpdated = DateTime.UtcNow;
                            })
                            .PublishAsync(context => context.Init<CallOrderProcessApi>(_processFactory.Create(context.Saga.CorrelationId, ExtractProcessData(context.Saga.OriginalMessage!), context.Saga.OrderProcessRetryCount))),
                        fail => fail.Finalize())
            );

            // 🚚 Order Ship: Success → Complete, Failure → Retry or End
            During(WaitingForOrderShip,
                When(OrderShipApiSucceeded)
                    .Then(context => {
                        context.Saga.OrderShippedApiCalled = true;
                        context.Saga.OrderShipResponse = context.Message.Response;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                        _logger.LogInformation($"🎉 ORDER PROCESSING COMPLETED for correlation ID: {context.Saga.CorrelationId}");
                        _logger.LogInformation($"All APIs called successfully: Create ✅ Process ✅ Ship ✅");
                    })
                    .Finalize(),
                When(OrderShipApiFailed)
                    .IfElse(context => ShouldRetryStep(context.Saga.OrderShipRetryCount, maxRetries: 3),
                        retry => retry
                            .Then(context => {
                                context.Saga.OrderShipRetryCount++;
                                context.Saga.LastError = context.Message.Error;
                                context.Saga.LastUpdated = DateTime.UtcNow;
                            })
                            .PublishAsync(context => context.Init<CallOrderShipApi>(_shipFactory.Create(context.Saga.CorrelationId, ExtractShipData(context.Saga.OriginalMessage!), context.Saga.OrderShipRetryCount))),
                        fail => fail.Finalize())
            );

            SetCompletedWhenFinalized();
        }

        /// <summary>
        /// Extracts order data from the original message for order creation
        /// </summary>
        private static object ExtractOrderData(Message message)
        {
            return message.StepData.TryGetValue(OrderDomainConstants.StepKeys.OrderCreated, out var orderData)
                ? orderData
                : new { error = "Order data not found" };
        }

        /// <summary>
        /// Extracts process data from the original message for order processing
        /// </summary>
        private static object ExtractProcessData(Message message)
        {
            return message.StepData.TryGetValue(OrderDomainConstants.StepKeys.OrderProcessed, out var processData)
                ? processData
                : new { error = "Process data not found" };
        }

        /// <summary>
        /// Extracts ship data from the original message for order shipping
        /// </summary>
        private static object ExtractShipData(Message message)
        {
            return message.StepData.TryGetValue(OrderDomainConstants.StepKeys.OrderShipped, out var shipData)
                ? shipData
                : new { error = "Ship data not found" };
        }

        /// <summary>
        /// Determines if a step should be retried based on current retry count
        /// </summary>
        private static bool ShouldRetryStep(int currentRetryCount, int maxRetries)
        {
            return currentRetryCount < maxRetries;
        }
    }
} 