## 🔄 **Complete Sequence of Events When Message Arrives on `my-topic`**

Here's the **exact step-by-step flow**:

### **📨 Phase 1: Message Reception & Atomic Storage**
```
1. Kafka Message Arrives → MessageConsumer.Consume()
2. 🔐 Start Database Transaction (ATOMIC OPERATION)
3. 💾 Save original Message to messages table (audit trail)
4. 💾 Save OrderProcessingSagaStarted to outbox_events table
5. ✅ Commit Transaction (BOTH saved atomically)
6. 🚀 Try immediate publish of OrderProcessingSagaStarted event
```

### **🎭 Phase 2: Saga Initialization**
```
7. OrderProcessingSaga receives OrderProcessingSagaStarted event
8. 🏁 Initially() state handler executes:
   - Set saga correlation ID
   - Store original message 
   - Log saga start
9. 📤 Publish CallOrderCreateApi command
10. 🔄 Transition to WaitingForOrderCreate state
```

### **🔗 Phase 3: API Call Chain Execution**
```
11. CallOrderCreateApiConsumer receives command
12. 🌐 HTTP call to MockExternalApis/api/orders/create
13a. SUCCESS PATH:
    - Publish OrderCreateApiSucceeded event
    - Saga transitions to WaitingForOrderProcess
    - Publish CallOrderProcessApi command
    - Continue chain...

13b. FAILURE PATH:
    - Publish OrderCreateApiFailed event
    - Saga evaluates retry logic
    - Either retry or finalize saga
```

### **🔄 Phase 4: Background Resilience (OutboxProcessor)**
```
PARALLEL PROCESS (every 5 seconds):
14. OutboxProcessor scans for unprocessed events
15. Finds any failed immediate publishes from step 6
16. Re-publishes with exponential backoff
17. Marks as processed when successful
```

## 🎯 **Key Architectural Points:**

| **Phase** | **Component** | **Purpose** | **Resilience** |
|-----------|---------------|-------------|----------------|
| **1** | MessageConsumer | Entry point + Outbox Pattern | Message loss protection |
| **2** | OrderProcessingSaga | Workflow orchestration | State persistence |
| **3** | API Consumers | External integrations | Retry + timeout |
| **4** | OutboxProcessor | Guaranteed delivery | Restart recovery |

## 💪 **Resilience at Every Step:**

- **🔐 Step 2-5:** Atomic transaction prevents message loss
- **⚠️ Step 6:** Immediate publish failure doesn't break flow
- **🔄 Step 13b:** Built-in retry logic with exponential backoff
- **🕐 Step 14-17:** Background processor recovers from any restart

**This design ensures no message is ever lost and every workflow completes eventually, even through application restarts!** 🛡️