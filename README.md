# MassTransit Kafka Web API Demo

This project demonstrates how to use MassTransit with Kafka in a .NET 9 Web API application. It consists of two main components:

- **Api**: A Web API that consumes messages from Kafka and stores them in an SQLite database. It also exposes an API endpoint to retrieve the stored messages.
- **Producer**: A Web API that exposes an endpoint to send messages to Kafka.

## Getting Started

### Prerequisites

- .NET 9 SDK
- Kafka (e.g., using Docker or a local installation)

### Running the Application

1.  **Start Kafka**: Ensure your Kafka instance is running and accessible at `127.0.0.1:9092`.

2.  **Run the Api Project (Consumer)**:
    Open a terminal, navigate to the `Api` directory, and run:
    ```bash
    dotnet run
    ```
    The API will be accessible at `https://localhost:7019` (HTTPS) or `http://localhost:5026` (HTTP).
    You can access the Swagger UI for the Api project at `https://localhost:7019/swagger`.

3.  **Run the Producer Project**:
    Open another terminal, navigate to the `Producer` directory, and run:
    ```bash
    dotnet run
    ```
    The Producer API will be accessible at `http://localhost:5001`.
    You can access the Swagger UI for the Producer project at `http://localhost:5001/swagger`.

### Sending Messages

1.  Open the Producer Swagger UI in your browser: `http://localhost:5001/swagger`.
2.  Use the `POST /Producer` endpoint to send messages. The `text` parameter is the message content.

### Retrieving Messages

1.  Open the Api Swagger UI in your browser: `https://localhost:7019/swagger`.
2.  Use the `GET /Messages` endpoint to retrieve all messages stored in the SQLite database.

## Project Structure

- `Api/`: The Web API project that consumes Kafka messages and stores them in SQLite.
- `Producer/`: The Web API project that produces Kafka messages.
- `Messages/`: A shared project containing the `Message` record definition.
