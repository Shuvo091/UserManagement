# Kafka Integration Guide

This document outlines how to integrate Kafka into the **User Management Service** using .NET 8 and the shared library `IEventBus`.

---

## 1️⃣ Add Kafka Configuration

In `appsettings.json`, add the following:

```json
"KafkaOptions": {
  "BootstrapServers": "kafka:9092",
  "GroupId": "user-management-service"
}
```

---

## 2️⃣ Docker Compose Setup

Add **Zookeeper** and **Kafka** services in your `docker-compose.yml`:

```yaml
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.4.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"

  kafka:
    image: confluentinc/cp-kafka:7.4.0
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
```

> ⚠️ In your main service, ensure Kafka starts before it by adding:

```yaml
depends_on:
  kafka:
    condition: service_started
```

---

## 3️⃣ Environment Variables

Set the following environment variables for containerized deployments:

```yaml
KafkaOptions__BootstrapServers: "kafka:9092"
KafkaOptions__GroupId: "user-management-service"
```

---

## 4️⃣ Register Kafka in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Kafka via shared library
builder.Services.AddKafka(builder.Configuration);

var app = builder.Build();
app.Run();
```

---

## 5️⃣ Inject IEventBus in Service

```csharp
private readonly IEventBus eventBus;

public MyService(IEventBus eventBus)
{
    this.eventBus = eventBus;
}
```

---

## 6️⃣ Publish CloudEvents Example

```csharp
var cloudEvent = new CloudEvent
{
    Id = Guid.NewGuid().ToString(),
    Source = new Uri($"{TopicConstant.UserEloUpdated}:{request.WorkflowRequestId}"),
    Type = TopicConstant.UserEloUpdated,
    Time = DateTimeOffset.UtcNow,
    DataContentType = "application/json",
    Data = new
    {
        RequestId = request.WorkflowRequestId,
        Message = "Users Elo Updated."
    },
};

try
{
    await this.eventBus.PublishAsync(cloudEvent, TopicConstant.UserEloUpdated);
    this.logger.LogInformation("{RequestId} CloudEvent publish successful.", request.WorkflowRequestId);
}
catch (Exception ex)
{
    this.logger.LogWarning(ex, "{RequestId} CloudEvent publish failed.", request.WorkflowRequestId);
}
```

---

## 7️⃣ Summary of Steps

1. Add Kafka configuration to `appsettings.json`.
2. Add Zookeeper + Kafka services in `docker-compose.yml`.
3. Ensure main service waits for Kafka (`depends_on`).
4. Set Kafka environment variables.
5. Pull latest shared library with `IEventBus`.
6. Register Kafka in `Program.cs`.
7. Inject `IEventBus` and publish events via `CloudEvent`.
