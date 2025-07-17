# MassTransit Kafka Sample Project

This project demonstrates a basic Producer-Consumer setup using MassTransit with Kafka as the message broker in a .NET environment.

## Project Structure

- `MassTransitKafkaSample.sln`: The solution file.
- `Producer/`: A .NET Core console application that sends messages to a Kafka topic.
- `Consumer/`: A .NET Core console application that consumes messages from the same Kafka topic.
- `Messages/`: A .NET Standard class library containing the shared message contract (`Message.cs`).

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 9.0 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for running Kafka locally)

## Getting Started

### 1. Run Kafka using Docker

Open your terminal and run the following command to start a Kafka broker and Zookeeper. This command configures Kafka to advertise on `127.0.0.1:9092`, which matches the application's configuration.

```bash
docker run --rm -p 9092:9092 \
  -e KAFKA_NODE_ID=1 \
  -e KAFKA_PROCESS_ROLES=broker,controller \
  -e KAFKA_LISTENERS=PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:29093 \
  -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://127.0.0.1:9092 \
  -e KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER \
  -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT \
  -e KAFKA_CONTROLLER_QUORUM_VOTERS=1@localhost:29093 \
  -e CLUSTER_ID=MkU3OEVBNTcwNTJENDM2Qk \
  -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
  -e KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1 \
  -e KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1 \
  confluentinc/cp-kafka:latest
```

Keep this terminal window open as long as you want Kafka to be running.

### 2. Create the Kafka Topic

Even with `auto.create.topics.enable` enabled, it's good practice to explicitly create the topic to avoid timing issues. Open a **new** terminal and execute the following commands:

First, find the `CONTAINER ID` of your running Kafka container:
```bash
docker ps
```

Then, execute a bash shell inside the container (replace `<CONTAINER_ID>` with your actual ID):
```bash
docker exec -it <CONTAINER_ID> bash
```

Once inside the container, create the topic:
```bash
kafka-topics --create --topic my-topic --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1
```

You can verify the topic was created:
```bash
kafka-topics --list --bootstrap-server localhost:9092
```

Exit the Docker container's shell:
```bash
exit
```

### 3. Build the Project

Navigate to the root of the project directory (`MassTransitKafkaDemo`) in your terminal and build the solution:

```bash
cd MassTransitKafkaDemo
dotnet build
```

### 4. Run the Consumer Application

Open a **new** terminal window, navigate to the `Consumer` project directory, and run it:

```bash
cd MassTransitKafkaDemo/Consumer
dotnet run
```

You should see output indicating the consumer has started.

### 5. Run the Producer Application

Open another **new** terminal window, navigate to the `Producer` project directory, and run it:

```bash
cd MassTransitKafkaDemo/Producer
dotnet run
```

The Producer will start sending messages to the `my-topic` Kafka topic. You should see these messages being received and logged by the Consumer application in its terminal.

## Troubleshooting

- **"Connection refused"**: Ensure your Kafka Docker container is running and that `KAFKA_ADVERTISED_LISTENERS` is set to `PLAINTEXT://127.0.0.1:9092` in your Docker run command.
- **"Unknown topic or partition"**: Ensure the `my-topic` Kafka topic has been explicitly created using the `kafka-topics.sh` command as described above.
- **Messages not appearing in Consumer**: Verify the Producer is sending messages (check its console output). Ensure only one instance of the Consumer is running. Check Kafka topic offsets and consumer group status using `kafka-get-offsets` and `kafka-consumer-groups --describe` commands within the Docker container to see if messages are accumulating or being processed.

```