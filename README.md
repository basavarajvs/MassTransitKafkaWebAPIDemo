# MassTransit Kafka Web API Demo with Saga Orchestration

This project demonstrates how to use MassTransit with Kafka in a .NET 9 Web API application, featuring a **state machine saga** for orchestrating sequential external API calls with retry logic and persistence.

## Architecture Overview

The solution consists of **4 main components**:

- **`Messages`** - Shared message contracts library
- **`Producer`** - Web API that publishes messages to Kafka  
- **`Api`** - Web API that consumes Kafka messages and orchestrates business workflows via sagas
- **`MockExternalApis`** - Separate service that simulates external APIs for testing

## Key Features

### **🔄 Saga Orchestration**
- **Sequential API calls**: Create → Process → Ship (in order)
- **Retry logic**: 3 attempts per step with 5-second timeouts
- **State persistence**: Saga state survives application restarts
- **Error handling**: Comprehensive logging and failure tracking

### **📨 Message Processing**
- **Kafka integration** with MassTransit
- **Message structure** with 3 predefined step keys
- **Database persistence** for messages and saga states

### **🧪 Testing Infrastructure**
- **Mock external APIs** with realistic failure simulation (10% failure rate)
- **Separate test service** for clean architecture
- **Health check endpoints** for monitoring

## Getting Started

### Prerequisites

- .NET 9 SDK
- Kafka (e.g., using Docker or a local installation)

### Running the Application

1. **Start Kafka**: Ensure your Kafka instance is running and accessible at `127.0.0.1:9092`.

2. **Run the Mock External APIs Service**:
   ```bash
   cd MockExternalApis && dotnet run
   ```
   The Mock APIs will be accessible at `http://localhost:5027`
   Swagger UI: `http://localhost:5027/swagger`

3. **Run the Api Project (Consumer + Saga)**:
   ```bash
   cd Api && dotnet run
   ```
   The API will be accessible at `https://localhost:7019` (HTTPS) or `http://localhost:5026` (HTTP).
   Swagger UI: `https://localhost:7019/swagger`

4. **Run the Producer Project**:
   ```bash
   cd Producer && dotnet run
   ```
   The Producer API will be accessible at `http://localhost:5001`.
   Swagger UI: `http://localhost:5001/swagger`

## Message Format

The system uses a structured message format with 3 predefined step keys:

```json
{
  "Id": "guid-here",
  "StepData": {
    "order-created": {
      "OrderId": "ORD-12345",
      "OrderDetails": "Customer order details",
      "CreatedAt": "2024-07-25T14:30:00Z",
      "Status": "Created"
    },
    "order-processed": {
      "ProcessedBy": "System",
      "ProcessedAt": "2024-07-25T14:35:00Z",
      "Status": "Processed"
    },
    "order-shipped": {
      "TrackingNumber": "TRK87654321",
      "ShippedAt": "2024-07-25T16:00:00Z",
      "Status": "Shipped",
      "Carrier": "FedEx"
    }
  }
}
```

## Complete Workflow

1. **Send Message** → Producer API or Kafka console producer
2. **Message Consumed** → Api saves message to database + starts saga
3. **Saga Orchestration**:
   - Calls Mock Order Create API
   - On success → Calls Mock Order Process API  
   - On success → Calls Mock Order Ship API
   - Each step retries up to 3 times on failure
4. **Completion** → Saga logs success and completes

## Testing with Kafka Console Producer

You can also send messages directly via Kafka console producer:

```bash
kafka-console-producer --broker-list 127.0.0.1:9092 --topic my-topic
```

**Single-line message example:**
```json
{"StepData":{"order-created":{"OrderId":"ORD-001","OrderDetails":"Test order","CreatedAt":"2024-07-25T15:00:00Z","Status":"Created"},"order-processed":{"ProcessedBy":"TestSystem","ProcessedAt":"2024-07-25T15:01:00Z","Status":"Processed"},"order-shipped":{"TrackingNumber":"TRK123456","ShippedAt":"2024-07-25T15:02:00Z","Status":"Shipped","Carrier":"TestCarrier"}}}
```

## API Endpoints

### **Producer Service** (`http://localhost:5001`)
- `POST /Producer` - Send messages to Kafka

### **Api Service** (`https://localhost:7019`)
- `GET /Messages` - Retrieve all processed messages

### **Mock External APIs** (`http://localhost:5027`)
- `POST /api/orders/create` - Simulate order creation
- `POST /api/orders/process` - Simulate order processing  
- `POST /api/orders/ship` - Simulate order shipping
- `GET /api/orders/health` - Health check

## Project Structure

```
MassTransitKafkaDemo/
├── Api/ - Main consumer service with saga orchestration
├── Producer/ - Message producer service
├── Messages/ - Shared message contracts
├── MockExternalApis/ - Mock external APIs for testing
└── README.md
```

## Database

The system uses **SQLite** for:
- **Message storage**: All received Kafka messages
- **Saga state storage**: Workflow progress and retry tracking

## Architecture Benefits

✅ **Clean Separation**: Production code vs. test code  
✅ **Saga Resilience**: Survives application restarts  
✅ **Retry Logic**: Handles transient failures gracefully  
✅ **Monitoring**: Comprehensive logging and state tracking  
✅ **Testability**: Realistic failure simulation for robust testing
