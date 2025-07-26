using MassTransit;
using Api.Domains.OrderProcessing.SagaSteps;
using Messages;

namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Order Processing Saga - Domain-specific saga for order processing workflow
    /// Clean, concise orchestration using the generic framework
    /// </summary>
    public class OrderProcessingSaga : MassTransitStateMachine<OrderProcessingSagaState>
    {
        private readonly ILogger<OrderProcessingSaga> _logger;
        private readonly OrderCreateStep _createStep;
        private readonly OrderProcessStep _processStep;
        private readonly OrderShipStep _shipStep;

        public OrderProcessingSaga(
            ILogger<OrderProcessingSaga> logger,
            OrderCreateStep createStep,
            OrderProcessStep processStep,
            OrderShipStep shipStep)
        {
            _logger = logger;
            _createStep = createStep;
            _processStep = processStep;
            _shipStep = shipStep;

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
                    .PublishAsync(context => context.Init<CallOrderCreateApi>(_createStep.CreateCommand(context.Saga.CorrelationId, context.Message.OriginalMessage)))
                    .TransitionTo(WaitingForOrderCreate)
            );

            // 📦 Order Create: Success → Process, Failure → Retry or End
            During(WaitingForOrderCreate,
                When(OrderCreateApiSucceeded)
                    .Then(context => _createStep.HandleSuccess(context.Saga, context.Message.Response))
                    .PublishAsync(context => context.Init<CallOrderProcessApi>(_processStep.CreateCommand(context.Saga.CorrelationId, context.Saga.OriginalMessage!)))
                    .TransitionTo(WaitingForOrderProcess),
                When(OrderCreateApiFailed)
                    .IfElse(context => _createStep.HandleFailureAndShouldRetry(context.Saga, context.Message.Error, context.Message.RetryCount),
                        retry => retry.PublishAsync(context => context.Init<CallOrderCreateApi>(_createStep.CreateCommand(context.Saga.CorrelationId, context.Saga.OriginalMessage!, context.Message.RetryCount))),
                        fail => fail.Finalize())
            );

            // ⚙️ Order Process: Success → Ship, Failure → Retry or End
            During(WaitingForOrderProcess,
                When(OrderProcessApiSucceeded)
                    .Then(context => _processStep.HandleSuccess(context.Saga, context.Message.Response))
                    .PublishAsync(context => context.Init<CallOrderShipApi>(_shipStep.CreateCommand(context.Saga.CorrelationId, context.Saga.OriginalMessage!)))
                    .TransitionTo(WaitingForOrderShip),
                When(OrderProcessApiFailed)
                    .IfElse(context => _processStep.HandleFailureAndShouldRetry(context.Saga, context.Message.Error, context.Message.RetryCount),
                        retry => retry.PublishAsync(context => context.Init<CallOrderProcessApi>(_processStep.CreateCommand(context.Saga.CorrelationId, context.Saga.OriginalMessage!, context.Message.RetryCount))),
                        fail => fail.Finalize())
            );

            // 🚚 Order Ship: Success → Complete, Failure → Retry or End
            During(WaitingForOrderShip,
                When(OrderShipApiSucceeded)
                    .Then(context => {
                        _shipStep.HandleSuccess(context.Saga, context.Message.Response);
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        _logger.LogInformation($"🎉 ORDER PROCESSING COMPLETED for correlation ID: {context.Saga.CorrelationId}");
                        _logger.LogInformation($"All APIs called successfully: Create ✅ Process ✅ Ship ✅");
                    })
                    .Finalize(),
                When(OrderShipApiFailed)
                    .IfElse(context => _shipStep.HandleFailureAndShouldRetry(context.Saga, context.Message.Error, context.Message.RetryCount),
                        retry => retry.PublishAsync(context => context.Init<CallOrderShipApi>(_shipStep.CreateCommand(context.Saga.CorrelationId, context.Saga.OriginalMessage!, context.Message.RetryCount))),
                        fail => fail.Finalize())
            );

            SetCompletedWhenFinalized();
        }
    }
} 