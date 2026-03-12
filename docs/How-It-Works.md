# How It Works

This document explains the core concept behind NServiceBus EndpointHealth and why it provides reliable health monitoring.

## The Problem with Traditional Health Checks

A typical health check verifies that a process is running or that a database connection can be established. However, for message-based systems like NServiceBus, this approach misses critical failure modes:

- The process is running, but the message pump is stuck
- The transport connection appears healthy, but messages are not being dequeued
- The endpoint is processing messages, but at a rate too slow to keep up

**Simply checking "is the process alive?" doesn't tell you if the endpoint can actually process messages.**

## The Solution: Synthetic Health Pings

NServiceBus EndpointHealth uses a clever approach: it sends a small "ping" message to itself and measures whether that message gets processed within an expected time frame.

### The Concept

```
┌─────────────────────────────────────────────────────────────────┐
│                        Endpoint                                  │
│                                                                  │
│   ┌──────────┐    SendLocal     ┌───────────┐                   │
│   │ Handler  │ ───────────────► │ Transport │                   │
│   │          │                  │  Queue    │                   │
│   │ Schedule │                  │           │                   │
│   │ next     │ ◄─────────────── │ Dequeue   │                   │
│   │ ping     │    Message Pump  │           │                   │
│   └──────────┘                  └───────────┘                   │
│        │                                                         │
│        ▼                                                         │
│   Update LastPingTime                                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

1. **On startup**, the endpoint sends a `HealthPing` message to itself using `SendLocal()`
2. **The message travels through the full pipeline**: it goes to the transport (SQL Server, RabbitMQ, etc.), sits in the queue, and is picked up by the message pump
3. **When processed**, the handler updates a timestamp and schedules the next ping
4. **Health checks compare** the last processed timestamp against the current time

### Why This Works

The ping message takes the exact same path as any real business message:

```
SendLocal() → Serialize → Transport → Queue → Dequeue → Deserialize → Handler
```

If any part of this pipeline is broken, the ping won't be processed, and the health check will detect it:

| Failure Mode | Detection |
|--------------|-----------|
| Message pump stuck | Ping not dequeued → timestamp becomes stale |
| Transport unavailable | Ping cannot be sent/received → timestamp stale |
| Serialization broken | Ping fails to process → timestamp stale |
| Handler concurrency exhausted | Ping waits in queue → timestamp may become stale |
| Critical error occurred | Explicit flag set → immediate unhealthy |

### The Timing Model

Two time intervals control the health monitoring:

```
         PingInterval                    UnhealthyAfter
    ├───────────────────┤     ├────────────────────────────────┤
    │                   │     │                                │
────●───────────────────●─────●────────────────────────────────X────
    │                   │     │                                │
  Ping 1              Ping 2  │                          Health Check
  processed           sent    │                          returns Unhealthy
                              │
                         Expected Ping 2
                         processing time
```

- **PingInterval** (default: 30 seconds): How often a new ping is scheduled
- **UnhealthyAfter** (default: 2 minutes): How long without a processed ping before reporting unhealthy

The `UnhealthyAfter` threshold should be larger than `PingInterval` to allow for:
- Normal message processing latency
- Brief transport hiccups
- Queue backlog during high load

### Example Timeline

```
T+0s    Endpoint starts, sends first HealthPing
T+0.1s  HealthPing processed, schedules next ping for T+30s
T+30s   HealthPing sent
T+30.1s HealthPing processed, schedules next ping for T+60s
T+60s   HealthPing sent
T+60s   Transport goes down - message stuck in queue
T+120s  Health check: Last ping was 90s ago, UnhealthyAfter is 120s → Healthy
T+150s  Health check: Last ping was 120s ago → Unhealthy!
```

## Critical Error Handling

In addition to the ping mechanism, the library hooks into NServiceBus's critical error handling:

```csharp
cfg.DefineCriticalErrorAction(async (context, ct) =>
{
    healthState.RegisterCriticalError(context.Error, context.Exception);
});
```

When NServiceBus encounters a critical error (transport failure, poison message, etc.), the health state is immediately flagged as unhealthy - no need to wait for a ping timeout.

## Self-Contained Health Monitoring

A key design principle is that **each endpoint maintains its own health state independently**. There are no external dependencies required:

- No central health monitoring service
- No shared database for health status
- No service mesh or sidecar required
- No polling from external systems

The endpoint sends pings to itself, processes them itself, and tracks the state internally. External systems (like Kubernetes or a load balancer) simply query the endpoint's health check endpoint - the endpoint is the single source of truth for its own health.

This self-contained approach means:
- **Zero infrastructure overhead** - no additional services to deploy or maintain
- **No single point of failure** - each endpoint's health monitoring is independent
- **Works in any environment** - on-premises, cloud, containers, or hybrid
- **Scales naturally** - adding endpoints doesn't increase monitoring complexity

## Container Restart Safety

When running in containers (Docker, Kubernetes), endpoints restart frequently. Without protection, each restart would leave stale delayed pings in the transport — when those pings are eventually dispatched, they would each spawn a new ping chain, causing **exponential accumulation** over time (e.g. 1000+ active chains after many restarts).

### The Problem

```
Container restart #1 → sends HealthPing → schedules next in 60s
Container restart #2 → sends HealthPing → schedules next in 60s
                                          ↑ old ping from #1 also arrives → new chain
Container restart #3 → ...               ↑ old pings from #1 and #2 also arrive → N chains
```

After N restarts: **N active ping chains**, each producing load and noise.

### The Solution: InstanceId

Each `EndpointHealthState` generates a fresh `Guid` on startup (`InstanceId`). Every `HealthPing` carries this ID:

```
Container restart → new InstanceId=Y generated
Old pings (InstanceId=X) arrive → handler: X ≠ Y → silently dropped, no reschedule
New ping  (InstanceId=Y) arrives → handler: Y = Y → processed → next ping scheduled with Y
```

Old ping chains die naturally within one `PingInterval` (default: 60s). After that, exactly one active chain per endpoint instance remains — regardless of how many restarts occurred.

### Result

| Scenario | Behavior |
|----------|----------|
| Normal operation | 1 ping chain, processes every PingInterval |
| Container restart | Old chains drop within 1 PingInterval |
| Many rapid restarts | Old chains all drop within 1 PingInterval |
| Scale-out (multiple replicas) | Each replica has its own InstanceId and chain |

## Summary

NServiceBus EndpointHealth provides **true end-to-end health verification** by:

1. **Testing the actual message path** - not just checking if the process is alive
2. **Using timing thresholds** - allowing you to define acceptable latency
3. **Capturing critical errors immediately** - for fast failure detection
4. **Container-safe design** - InstanceId prevents ping chain accumulation across restarts

This approach ensures your container orchestrator (Kubernetes, etc.) knows when an endpoint genuinely cannot process messages, enabling proper failover and recovery.
