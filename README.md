# 🚀 Challenge: Real-Time Order Orchestration Platform

## Scenario
You are building a **Fraud Detection & Order Orchestration System** for a high-volume e-commerce platform.  
Orders come in via gRPC, need real-time fraud checks, event-based orchestration, and eventual consistency across NoSQL (read/event store) and SQL (reporting/audit).

## Core Requirements

### 1. **gRPC Service (.NET)**
- Expose `SubmitOrder` (unary) and `TrackOrderStatus` (server streaming).
- Order contains: `OrderId`, `UserId`, `Items[]`, `TotalAmount`, `PaymentToken`.

### 2. **Mediator Pattern (MediatR)**
- Use MediatR to handle the `SubmitOrderCommand`.
- The handler should publish an `OrderSubmittedEvent`.

### 3. **Eventing & Orchestration**
- Events: `OrderSubmitted`, `FraudChecked`, `OrderApproved`, `OrderRejected`, `PaymentProcessed`, `ShipmentRequested`.
- Implement a **state machine orchestrator** (not a workflow engine, but your own lightweight orchestration logic) that listens to events and decides next steps.

### 4. **Storage**
- **NoSQL (MongoDB / Cosmos DB)** → Store order state (aggregate root) and events as a single document per order.
- **SQL (PostgreSQL / SQL Server)** → Store audit logs and reporting data (fraud check results, timestamps, user history).

### 5. **Fraud Check Simulation (gRPC client)**
- Call an external mock fraud service (gRPC) that randomly approves/rejects or takes 2 seconds to respond.
- Handle timeouts and retries.

### 6. **Containerization (Docker)**
- Dockerfile for .NET API + Angular UI.
- `docker-compose` to run: API, Angular (nginx), MongoDB, PostgreSQL, mock fraud service.

### 7. **Kubernetes Readiness**
- Provide K8s manifests (Deployment, Service, ConfigMap, Secret) for the API.
- Include liveness/readiness probes, resource limits, and an init container for DB migrations.

---

## 🧩 Bonus (Principal-Level Expectations)

- **Idempotency** – gRPC requests may be retried; ensure no duplicate processing.
- **Outbox pattern** – Events must be reliable (store in NoSQL + publish to message broker). You can simulate an in-memory broker but describe how you'd use RabbitMQ / Kafka.
- **Resilience** – Use Polly for retries, circuit breaker for fraud service.
- **Observability** – Add structured logging with OpenTelemetry + Jaeger traces for the full flow.
- **gRPC reflection & health checks**.
- **Angular UI** – Minimal dashboard showing real-time order status updates (via gRPC server streaming or SignalR if you prefer, but justify).

---

## 📦 Deliverables Expected from Candidate

| Artifact | Purpose |
|----------|---------|
| `README.md` | Architecture decisions, trade-offs, how to run everything |
| `docker-compose.yml` & Dockerfiles | Run full stack locally |
| K8s YAMLs | Deploy to minikube / kind |
| .NET solution | Clean Architecture (Commands, Events, Handlers, Orchestrator, Repos) |
| Angular app | One simple page consuming gRPC (or gRPC-web) |
| `orchestrator.cs` | Core orchestration logic (state machine) |
| Unit + integration tests | At least for the fraud check flow |

---

## 🧠 Evaluation Criteria (Principal Level)

| Area | What We Assess |
|------|----------------|
| **Architecture** | Separation of concerns, use of MediatR, event-driven vs request-driven choices |
| **Data consistency** | How you handle eventual consistency between NoSQL and SQL |
| **Orchestration** | Is the orchestrator stateless? How are sagas compensated? |
| **Resilience** | Retries, idempotency, outbox, timeout handling |
| **DevOps** | Docker best practices, K8s readiness probes, init containers |
| **Communication** | README clarity, trade-offs documented |

---

## 🧪 Example Flow (Expected Behavior)

1. Client calls `SubmitOrder` (gRPC).
2. API saves order as `Pending` in NoSQL, publishes `OrderSubmitted` event.
3. Orchestrator consumes event → calls Fraud Service (gRPC).
4. Fraud responds → `FraudChecked` event.
5. Orchestrator decides:
   - If approved → `OrderApproved` → simulate payment → `ShipmentRequested`.
   - If rejected → `OrderRejected` → stop.
6. All state changes append to NoSQL event log.
7. SQL audit table records each step with timestamp.
8. Client can stream `TrackOrderStatus` to see real-time updates.

---

## 🔁 Twist (Optional for harder mode)

> The fraud service sometimes sends a `ManualReviewRequired` response. Implement a **human-in-the-loop** via a simple Angular admin panel to approve/reject stalled orders, and continue orchestration.
