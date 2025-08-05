# Outbox Pattern Sequence Diagram

## Complete Flow with Success and Failure Scenarios

Copy this code to [sequencediagram.org](https://sequencediagram.org) to visualize:

```
title Outbox Pattern: Success & Failure Scenarios

participant Producer
participant Kafka
participant MessageConsumer
participant SQLite
participant OutboxProcessor
participant InMemoryBus
participant OrderSaga
participant APIConsumer
participant MockAPI

Note over Producer,MockAPI: 🎯 SCENARIO 1: NORMAL SUCCESS FLOW
Producer->Kafka: Publish Message
Kafka->MessageConsumer: Consume Message
MessageConsumer->SQLite: BEGIN TRANSACTION
MessageConsumer->SQLite: Save Message
MessageConsumer->SQLite: Save OutboxEvent\n(OrderProcessingSagaStarted)
MessageConsumer->SQLite: COMMIT TRANSACTION
MessageConsumer->InMemoryBus: Publish OrderProcessingSagaStarted\n(immediate attempt)
MessageConsumer->SQLite: Mark OutboxEvent.Processed=true
InMemoryBus->OrderSaga: Route by CorrelationId\n(creates new saga instance)
OrderSaga->SQLite: Save Saga State
OrderSaga->InMemoryBus: Publish CallOrderCreateApi
InMemoryBus->APIConsumer: Route Command
APIConsumer->MockAPI: HTTP POST /api/orders/create
MockAPI->APIConsumer: 200 OK + Response
APIConsumer->InMemoryBus: Publish OrderCreateApiSucceeded
InMemoryBus->OrderSaga: Update Saga State
OrderSaga->SQLite: Update Saga Progress

Note over Producer,MockAPI: 🚨 SCENARIO 2: APP RESTART AFTER OUTBOX SAVE
Producer->Kafka: Publish Message
Kafka->MessageConsumer: Consume Message
MessageConsumer->SQLite: Save Message + OutboxEvent (ATOMIC)
Note over MessageConsumer: 💥 APP CRASH BEFORE PUBLISH
Note over OutboxProcessor: 🔄 APP RESTART
OutboxProcessor->SQLite: Query unprocessed events\nWHERE Processed=false
OutboxProcessor->InMemoryBus: Publish OrderProcessingSagaStarted\n(recovery publish)
OutboxProcessor->SQLite: Mark OutboxEvent.Processed=true
InMemoryBus->OrderSaga: Route by CorrelationId\n(creates new saga instance)
OrderSaga->SQLite: Save Saga State
Note over OrderSaga: ✅ SAGA CONTINUES NORMALLY

Note over Producer,MockAPI: 🚨 SCENARIO 3: IMMEDIATE PUBLISH FAILURE
Producer->Kafka: Publish Message
MessageConsumer->SQLite: Save Message + OutboxEvent (ATOMIC)
MessageConsumer->InMemoryBus: Publish OrderProcessingSagaStarted
Note over InMemoryBus: ❌ PUBLISH FAILS (bus overloaded)
MessageConsumer->MessageConsumer: Log warning, continue\n(OutboxEvent remains unprocessed)
Note over OutboxProcessor: ⏰ 5 SECONDS LATER
OutboxProcessor->SQLite: Query unprocessed events
OutboxProcessor->InMemoryBus: Retry Publish OrderProcessingSagaStarted
OutboxProcessor->SQLite: Mark OutboxEvent.Processed=true
InMemoryBus->OrderSaga: Route by CorrelationId
Note over OrderSaga: ✅ SAGA STARTS SUCCESSFULLY

Note over Producer,MockAPI: 🚨 SCENARIO 4: API FAILURE WITH RETRY
Producer->Kafka: Publish Message
MessageConsumer->SQLite: Save Message + OutboxEvent (ATOMIC)
MessageConsumer->InMemoryBus: Publish OrderProcessingSagaStarted
InMemoryBus->OrderSaga: Route by CorrelationId
OrderSaga->InMemoryBus: Publish CallOrderCreateApi
InMemoryBus->APIConsumer: Route Command
APIConsumer->MockAPI: HTTP POST /api/orders/create
Note over MockAPI: ❌ 500 INTERNAL SERVER ERROR
MockAPI->APIConsumer: 500 Error Response
APIConsumer->InMemoryBus: Publish OrderCreateApiFailed\n(RetryCount=1)
InMemoryBus->OrderSaga: Handle Failure
alt RetryCount < 3
    OrderSaga->InMemoryBus: Publish CallOrderCreateApi\n(RetryCount=1)
    InMemoryBus->APIConsumer: Retry Command
    APIConsumer->MockAPI: HTTP POST /api/orders/create (RETRY)
    MockAPI->APIConsumer: 200 OK (Success on retry)
    APIConsumer->InMemoryBus: Publish OrderCreateApiSucceeded
    InMemoryBus->OrderSaga: Continue to next step
else RetryCount >= 3
    OrderSaga->SQLite: Mark saga as Failed
    Note over OrderSaga: 💀 SAGA ENDS (Max retries exceeded)
end

Note over Producer,MockAPI: 🚨 SCENARIO 5: OUTBOX PROCESSOR RETRY WITH EXPONENTIAL BACKOFF
OutboxProcessor->SQLite: Query unprocessed events
OutboxProcessor->InMemoryBus: Attempt Publish
Note over InMemoryBus: ❌ PUBLISH FAILS
OutboxProcessor->SQLite: Increment RetryCount=1\nScheduledFor = Now + 2 seconds
Note over OutboxProcessor: ⏰ 2 SECONDS LATER
OutboxProcessor->SQLite: Query events WHERE ScheduledFor <= Now
OutboxProcessor->InMemoryBus: Retry Publish
Note over InMemoryBus: ❌ STILL FAILS
OutboxProcessor->SQLite: Increment RetryCount=2\nScheduledFor = Now + 4 seconds
Note over OutboxProcessor: ⏰ 4 SECONDS LATER
OutboxProcessor->InMemoryBus: Retry Publish
Note over InMemoryBus: ✅ SUCCESS
OutboxProcessor->SQLite: Mark Processed=true

Note over Producer,MockAPI: 🚨 SCENARIO 6: DEAD LETTER AFTER MAX RETRIES
loop RetryCount < 5
    OutboxProcessor->InMemoryBus: Attempt Publish
    Note over InMemoryBus: ❌ FAILS
    OutboxProcessor->SQLite: Increment RetryCount\nExponential backoff delay
end
Note over OutboxProcessor: RetryCount = 5 (MAX REACHED)
OutboxProcessor->SQLite: Mark Processed=true\n(Dead Letter - Stop Retrying)
Note over OutboxProcessor: 💀 EVENT DEAD LETTERED\n(Manual intervention required)

Note over Producer,MockAPI: 🎯 KEY GUARANTEES PROVIDED
Note over SQLite: ✅ ATOMIC PERSISTENCE\n(Message + Command together)
Note over OutboxProcessor: ✅ EXACTLY-ONCE DELIVERY\n(Events processed once only)
Note over OrderSaga: ✅ ZERO MESSAGE LOSS\n(Commands survive restarts)
Note over APIConsumer: ✅ AUTOMATIC RECOVERY\n(No manual intervention)
```

## Key Failure Points Protected

1. **🔐 Atomic Persistence**: Message and OutboxEvent saved in same transaction
2. **🔄 Restart Recovery**: OutboxProcessor handles missed publications
3. **⚠️ Publish Failures**: Background processor with exponential backoff
4. **🔁 API Failures**: Saga-level retry logic (3 attempts per step)
5. **💀 Dead Letter Handling**: Events stop retrying after 5 attempts
6. **📊 Full Audit Trail**: Complete visibility into failures and retries

## Architecture Benefits Demonstrated

- **No Single Point of Failure**: Multiple recovery mechanisms
- **Graceful Degradation**: System continues despite individual failures  
- **Observable**: Clear logging and state tracking throughout
- **Self-Healing**: Automatic recovery without manual intervention
- **Production Ready**: Handles real-world failure scenarios