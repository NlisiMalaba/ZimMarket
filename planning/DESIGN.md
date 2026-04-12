# Zimbabwe Marketplace — Architecture & Design Reference

> **Version:** 1.0 — MVP  
> **Stack:** .NET 10 · PostgreSQL · Redis · SignalR · React Native (Expo) · Next.js  
> **Last updated:** 2025

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Tech Stack](#2-tech-stack)
3. [Clean Architecture](#3-clean-architecture)
4. [Project Structure](#4-project-structure)
5. [Domain Model](#5-domain-model)
6. [Design Patterns](#6-design-patterns)
7. [API Design Conventions](#7-api-design-conventions)
8. [Authentication & Authorisation](#8-authentication--authorisation)
9. [Real-Time Architecture (SignalR)](#9-real-time-architecture-signalr)
10. [Payment Integration](#10-payment-integration)
11. [File Storage](#11-file-storage)
12. [Notifications](#12-notifications)
13. [Database Strategy](#13-database-strategy)
14. [Caching Strategy](#14-caching-strategy)
15. [Background Jobs](#15-background-jobs)
16. [Docker & Infrastructure](#16-docker--infrastructure)
17. [Frontend Architecture](#17-frontend-architecture)
18. [Security Considerations](#18-security-considerations)
19. [Scalability Roadmap](#19-scalability-roadmap)
20. [Error Handling & Result Pattern](#20-error-handling--result-pattern)

---

## 1. System Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                          CLIENT LAYER                                        │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────────────┐    │
│  │  Customer App   │  │   Seller App    │  │     Driver App           │    │
│  │ React Native    │  │ React Native    │  │  React Native (Expo)     │    │
│  │ (Expo)          │  │ (Expo)          │  │  Background GPS          │    │
│  └────────┬────────┘  └────────┬────────┘  └──────────────┬───────────┘    │
│           │                    │                           │                │
│           │           ┌────────┴────────┐                  │                │
│           │           │  Admin Panel    │                  │                │
│           │           │  Next.js 14     │                  │                │
│           │           └────────┬────────┘                  │                │
└───────────┼────────────────────┼───────────────────────────┼────────────────┘
            │                    │                           │
            └────────────────────┼───────────────────────────┘
                                 ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                        API GATEWAY LAYER                                     │
│                                                                              │
│    ASP.NET Core 10 — JWT Validation · Rate Limiting · CORS · Versioning     │
│    HTTPS only · Request logging (Serilog) · Health checks                   │
└──────────────────────────────────┬───────────────────────────────────────────┘
                                   │
┌──────────────────────────────────▼───────────────────────────────────────────┐
│                      APPLICATION LAYER (.NET 10)                             │
│                                                                              │
│  ┌────────────┐ ┌───────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │  Identity  │ │ Catalogue │ │  Orders  │ │ Payments │ │   Logistics   │  │
│  │  + KYC     │ │ + Search  │ │  + Cart  │ │ Paynow   │ │  GPS/Driver   │  │
│  └────────────┘ └───────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌────────────┐ ┌───────────┐ ┌──────────┐                                  │
│  │ Warehouse  │ │  Notify   │ │  Admin   │                                  │
│  │ Management │ │ (Hub)     │ │  Panel   │                                  │
│  └────────────┘ └───────────┘ └──────────┘                                  │
└──────────────────────────────────┬───────────────────────────────────────────┘
                                   │
┌──────────────────────────────────▼───────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                    │
│                                                                              │
│  PostgreSQL 16    Redis 7      Hangfire      SignalR       Azure Blob        │
│  (primary store)  (cache+sess) (bg jobs)    (real-time)   (files/images)    │
│                                                                              │
│  Paynow Gateway   Ecocash API  SendGrid      Twilio        FCM/APNs          │
│  (USD + ZWL)      (mobile $)   (email)       (SMS OTP)     (push)           │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Tech Stack

### Backend

| Concern | Choice | Reason |
|---|---|---|
| Framework | ASP.NET Core 10 | LTS, minimal API + controller support, best .NET perf |
| ORM | EF Core 10 (Npgsql) | Type-safe, migration support, async-first |
| Database | PostgreSQL 16 | ACID, JSONB for flexible fields, PostGIS for geo |
| Cache | Redis 7 | Sessions, distributed cache, pub/sub |
| Mediator | MediatR 12 | CQRS decoupling, pipeline behaviours |
| Validation | FluentValidation 11 | Clean, testable validation rules |
| Background jobs | Hangfire | Persistent job queue with retry, dashboard |
| Real-time | ASP.NET SignalR | GPS tracking, order status push |
| Auth | ASP.NET Core Identity + JWT | Role-based, refresh tokens |
| Mapping | Mapster | Faster than AutoMapper, code-gen friendly |
| Logging | Serilog + Seq | Structured logs, queryable UI |
| Monitoring | Prometheus + Grafana | Metrics, dashboards |
| Testing | xUnit + Bogus + Testcontainers | Property tests, integration tests |
| API docs | Scalar (OpenAPI) | Modern alternative to Swagger UI |

### Frontend

| App | Framework | Why |
|---|---|---|
| Customer app | React Native (Expo SDK 52) | Cross-platform iOS/Android, fast iteration |
| Seller app | React Native (Expo SDK 52) | Shared mobile codebase with customer |
| Driver app | React Native (Expo SDK 52) | Background location, camera, shared repo |
| Admin panel | Next.js 14 (App Router) | SSR dashboard, TypeScript, shadcn/ui |
| State management | Zustand + React Query | Lightweight, server-state aware |
| Maps | react-native-maps + Google Maps SDK | GPS tracking, route display |
| Forms | React Hook Form + Zod | Type-safe validation |
| UI (admin) | shadcn/ui + Tailwind CSS | Accessible, composable components |

### Infrastructure & DevOps

| Concern | Choice |
|---|---|
| Containerisation | Docker + Docker Compose |
| CI/CD | GitHub Actions |
| Cloud | Azure (primary) or AWS |
| CDN | Cloudflare |
| File storage | Azure Blob Storage |
| Secrets | Docker secrets / Azure Key Vault |
| IaC (post-MVP) | Terraform |

---

## 3. Clean Architecture

The backend follows Clean Architecture (Uncle Bob). Dependencies always point **inward** — the Domain layer has zero external dependencies.

```
┌─────────────────────────────────────────────┐
│              Presentation Layer             │
│   Controllers · SignalR Hubs · Middleware   │
│   Depends on: Application                  │
├─────────────────────────────────────────────┤
│             Infrastructure Layer           │
│   EF Core · Redis · Blob · SMS · Email     │
│   Depends on: Application (interfaces)    │
├─────────────────────────────────────────────┤
│             Application Layer              │
│   CQRS Commands/Queries · MediatR          │
│   Use Cases · Validators · DTOs            │
│   Depends on: Domain only                 │
├─────────────────────────────────────────────┤
│               Domain Layer                 │
│   Entities · Value Objects · Enums         │
│   Domain Events · Repository Interfaces   │
│   No external dependencies                │
└─────────────────────────────────────────────┘
```

### Dependency Rules

- `Domain` → nothing
- `Application` → `Domain` only
- `Infrastructure` → `Application` (implements interfaces), `Domain`
- `API` → `Application`, `Infrastructure` (DI registration only)

---

## 4. Project Structure

```
ZimMarket/
├── docker-compose.yml
├── docker-compose.override.yml          # dev overrides
├── .env.example
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── deploy.yml
│
├── src/
│   ├── ZimMarket.Domain/
│   │   ├── Entities/
│   │   │   ├── Users/
│   │   │   │   ├── User.cs              # base user aggregate root
│   │   │   │   ├── Seller.cs
│   │   │   │   ├── Driver.cs
│   │   │   │   └── Customer.cs
│   │   │   ├── Catalogue/
│   │   │   │   ├── Product.cs
│   │   │   │   └── Category.cs
│   │   │   ├── Orders/
│   │   │   │   ├── Order.cs
│   │   │   │   └── OrderItem.cs
│   │   │   ├── Logistics/
│   │   │   │   ├── DeliveryBatch.cs
│   │   │   │   └── DriverLocation.cs
│   │   │   └── Warehouse/
│   │   │       └── WarehouseItem.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs                 # USD + ZWL, rate-aware
│   │   │   ├── Address.cs
│   │   │   ├── PhoneNumber.cs
│   │   │   └── GeoCoordinate.cs
│   │   ├── Enums/
│   │   │   ├── OrderStatus.cs
│   │   │   ├── KycStatus.cs
│   │   │   ├── DriverStatus.cs
│   │   │   └── PaymentStatus.cs
│   │   ├── Events/                      # domain events
│   │   │   ├── OrderPlacedEvent.cs
│   │   │   ├── SellerRegisteredEvent.cs
│   │   │   ├── DriverLocationUpdatedEvent.cs
│   │   │   └── DeliveryCompletedEvent.cs
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   └── Interfaces/
│   │       ├── Repositories/
│   │       │   ├── IUserRepository.cs
│   │       │   ├── IProductRepository.cs
│   │       │   ├── IOrderRepository.cs
│   │       │   └── IDeliveryBatchRepository.cs
│   │       └── IUnitOfWork.cs
│   │
│   ├── ZimMarket.Application/
│   │   ├── Common/
│   │   │   ├── Behaviours/
│   │   │   │   ├── ValidationBehaviour.cs
│   │   │   │   ├── LoggingBehaviour.cs
│   │   │   │   ├── TransactionBehaviour.cs
│   │   │   │   └── CachingBehaviour.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── ICurrentUser.cs
│   │   │   │   ├── IFileStorage.cs
│   │   │   │   ├── IPaymentGateway.cs
│   │   │   │   ├── ISmsService.cs
│   │   │   │   ├── IEmailService.cs
│   │   │   │   └── IPushNotificationService.cs
│   │   │   ├── Models/
│   │   │   │   └── Result.cs            # Result<T> pattern
│   │   │   └── Extensions/
│   │   │       └── PaginationExtensions.cs
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── RegisterCustomer/
│   │   │   │   │   ├── RegisterSeller/
│   │   │   │   │   ├── RegisterDriver/
│   │   │   │   │   └── RefreshToken/
│   │   │   │   └── Queries/
│   │   │   │       └── Login/
│   │   │   ├── Catalogue/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateProduct/
│   │   │   │   │   ├── UpdateProduct/
│   │   │   │   │   └── DeleteProduct/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetProducts/
│   │   │   │       └── SearchProducts/
│   │   │   ├── Orders/
│   │   │   ├── Payments/
│   │   │   ├── Logistics/
│   │   │   ├── Warehouse/
│   │   │   └── Admin/
│   │   └── DependencyInjection.cs
│   │
│   ├── ZimMarket.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/          # EF entity configs (IEntityTypeConfiguration)
│   │   │   ├── Repositories/
│   │   │   ├── Migrations/
│   │   │   └── UnitOfWork.cs
│   │   ├── Identity/
│   │   │   ├── JwtService.cs
│   │   │   └── TokenService.cs
│   │   ├── ExternalServices/
│   │   │   ├── Payments/
│   │   │   │   ├── PaynowService.cs
│   │   │   │   └── EcocashService.cs
│   │   │   ├── Storage/
│   │   │   │   └── AzureBlobStorageService.cs
│   │   │   ├── Notifications/
│   │   │   │   ├── TwilioSmsService.cs
│   │   │   │   ├── SendGridEmailService.cs
│   │   │   │   └── FcmPushService.cs
│   │   │   └── Maps/
│   │   │       └── GoogleMapsService.cs
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs
│   │   ├── BackgroundJobs/
│   │   │   ├── HangfireJobSetup.cs
│   │   │   └── Jobs/
│   │   │       ├── SendNotificationJob.cs
│   │   │       └── UpdateExchangeRateJob.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── ZimMarket.API/
│   │   ├── Controllers/
│   │   │   ├── v1/
│   │   │   │   ├── AuthController.cs
│   │   │   │   ├── ProductsController.cs
│   │   │   │   ├── OrdersController.cs
│   │   │   │   ├── PaymentsController.cs
│   │   │   │   ├── DriversController.cs
│   │   │   │   ├── WarehouseController.cs
│   │   │   │   └── AdminController.cs
│   │   ├── Hubs/
│   │   │   ├── TrackingHub.cs           # GPS real-time
│   │   │   └── NotificationsHub.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   └── ZimMarket.Shared/
│       ├── Constants/
│       │   ├── Roles.cs
│       │   └── Policies.cs
│       └── Pagination/
│           ├── PagedList.cs
│           └── PaginationParams.cs
│
├── tests/
│   ├── ZimMarket.Domain.Tests/
│   ├── ZimMarket.Application.Tests/
│   │   ├── Features/
│   │   └── Common/
│   └── ZimMarket.Integration.Tests/
│       └── (Testcontainers — real Postgres + Redis)
│
├── apps/
│   ├── mobile/                          # React Native (Expo) — customer + seller + driver
│   └── admin/                           # Next.js 14 — admin + super admin
```

---

## 5. Domain Model

### Core Aggregates

```
User (base)
├── Id: Guid
├── Email: string
├── PhoneNumber: PhoneNumber (value object)
├── PasswordHash: string
├── Role: UserRole (enum)
├── KycStatus: KycStatus (enum)
├── CreatedAt / UpdatedAt
└── DomainEvents: List<IDomainEvent>

Customer : User
└── DeliveryAddresses: List<Address>

Seller : User
├── BusinessName: string
├── NationalIdUrl: string
├── ProofOfResidenceUrl: string
├── IsApproved: bool
└── Products: List<Product>

Driver : User
├── LicenseNumber: string
├── LicenseImageUrl: string
├── VehicleRegNumber: string
├── VehicleImageUrl: string
├── IsApproved: bool
├── CurrentStatus: DriverStatus (Available | OnDelivery | Offline)
└── LastKnownLocation: GeoCoordinate

Product
├── Id: Guid
├── SellerId: Guid
├── Title: string
├── Description: string
├── Price: Money (value object — USD base)
├── CategoryId: Guid
├── StockQuantity: int
├── Images: List<string> (blob URLs)
├── Status: ProductStatus (Active | Suspended | Deleted)
└── Location: Address

Order
├── Id: Guid
├── CustomerId: Guid
├── Items: List<OrderItem>
├── TotalAmount: Money
├── DeliveryAddress: Address
├── Status: OrderStatus
├── PaymentStatus: PaymentStatus
├── PaymentReference: string
└── CreatedAt: DateTimeOffset

OrderStatus (enum)
  Pending → Paid → AtWarehouse → QcPassed → Batched
  → OutForDelivery → Delivered → Cancelled → Refunded

DeliveryBatch
├── Id: Guid
├── DriverId: Guid
├── Orders: List<Order>
├── PickupWarehouseId: Guid
├── Status: BatchStatus (Created | Collected | InTransit | Completed)
└── CollectedAt / CompletedAt

Money (value object)
├── Amount: decimal
├── Currency: Currency (USD | ZWL)
└── ToZwl(rate: decimal): Money   // converts using daily rate; stored separately
```

### Domain Events

| Event | Published by | Handled by |
|---|---|---|
| `SellerRegisteredEvent` | Seller aggregate | Admin notif, KYC queue |
| `DriverRegisteredEvent` | Driver aggregate | Admin notif, KYC queue |
| `OrderPlacedEvent` | Order aggregate | Inventory reserve, seller notif |
| `PaymentConfirmedEvent` | Payment service | Order status update, seller notif |
| `ItemArrivedAtWarehouseEvent` | Warehouse | Customer notif |
| `BatchCreatedEvent` | Batch aggregate | Driver notif |
| `DriverLocationUpdatedEvent` | Driver | SignalR broadcast to customer |
| `DeliveryCompletedEvent` | Driver | Order close, seller payout trigger |

---

## 6. Design Patterns

### CQRS with MediatR

All application logic is expressed as Commands (write) or Queries (read), handled by MediatR. Controllers are thin — they only map HTTP → MediatR → HTTP.

```csharp
// Command
public record CreateProductCommand(
    string Title, string Description, decimal PriceUsd,
    Guid CategoryId, int Stock, List<string> ImageUrls
) : IRequest<Result<Guid>>;

// Handler
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    // constructor-injected: IProductRepository, IUnitOfWork, ICurrentUser
    public async Task<Result<Guid>> Handle(CreateProductCommand cmd, CancellationToken ct) { ... }
}

// Controller (thin)
[HttpPost]
public async Task<IActionResult> Create(CreateProductCommand cmd)
    => (await _mediator.Send(cmd)).ToActionResult();
```

### Repository + Unit of Work

Repositories abstract EF Core. UoW wraps the transaction.

```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedList<Product>> GetPagedAsync(ProductFilter filter, PaginationParams pagination, CancellationToken ct);
    void Add(Product product);
    void Update(Product product);
    void Remove(Product product);
}

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IDeliveryBatchRepository Batches { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

### MediatR Pipeline Behaviours

Ordered pipeline applied automatically to every request:

```
Request → LoggingBehaviour → ValidationBehaviour → TransactionBehaviour → CachingBehaviour → Handler
```

- **LoggingBehaviour** — logs request name, user id, duration
- **ValidationBehaviour** — runs all FluentValidation validators; returns 422 on failure
- **TransactionBehaviour** — wraps commands in DB transactions; no-op for queries
- **CachingBehaviour** — for queries implementing `ICacheable`; checks Redis first

### Result Pattern (no exceptions for business logic)

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static Result<T> Success(T value) => ...;
    public static Result<T> Failure(string code, string message) => ...;
}

// Extension to map Result → IActionResult
public static IActionResult ToActionResult<T>(this Result<T> result)
    => result.IsSuccess
        ? new OkObjectResult(result.Value)
        : result.ErrorCode switch
        {
            ErrorCodes.NotFound => new NotFoundObjectResult(result.ErrorMessage),
            ErrorCodes.Forbidden => new ForbidResult(),
            ErrorCodes.Conflict => new ConflictObjectResult(result.ErrorMessage),
            _ => new UnprocessableEntityObjectResult(result.ErrorMessage)
        };
```

### Specification Pattern (complex queries)

```csharp
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    public bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);
    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
}

// Usage
var spec = new ActiveProductsSpec()
    .And(new InCategorySpec(categoryId))
    .And(new PriceRangeSpec(min, max));
var products = await _repo.FindAsync(spec, ct);
```

### Observer Pattern via Domain Events

Domain entities raise events; Infrastructure dispatches them after `SaveChanges`. Uses MediatR `INotification`.

```csharp
// In Order aggregate
public void ConfirmPayment(string reference)
{
    PaymentStatus = PaymentStatus.Paid;
    Status = OrderStatus.Paid;
    AddDomainEvent(new PaymentConfirmedEvent(Id, reference));
}

// In UnitOfWork.SaveChangesAsync
var events = ChangeTracker.Entries<BaseEntity>()
    .SelectMany(e => e.Entity.PopDomainEvents());
await _context.SaveChangesAsync(ct);
foreach (var ev in events)
    await _publisher.Publish(ev, ct);
```

### Decorator Pattern (cross-cutting concerns)

Used for adding caching, retry, or circuit-breaking to infrastructure services without modifying them.

```csharp
// Cached product repository decorator
public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IDistributedCache _cache;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var key = $"product:{id}";
        var cached = await _cache.GetAsync<Product>(key, ct);
        if (cached is not null) return cached;
        var product = await _inner.GetByIdAsync(id, ct);
        if (product is not null) await _cache.SetAsync(key, product, TimeSpan.FromMinutes(10), ct);
        return product;
    }
}
```

### Factory Pattern (payment gateway selection)

```csharp
public interface IPaymentGatewayFactory
{
    IPaymentGateway Create(PaymentMethod method);
}

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    // keyed services (IServiceProvider) registered by method
    public IPaymentGateway Create(PaymentMethod method) => method switch
    {
        PaymentMethod.Paynow => _serviceProvider.GetRequiredKeyedService<IPaymentGateway>("paynow"),
        PaymentMethod.Ecocash => _serviceProvider.GetRequiredKeyedService<IPaymentGateway>("ecocash"),
        _ => throw new NotSupportedException($"Payment method {method} not supported")
    };
}
```

---

## 7. API Design Conventions

- **Versioned**: `/api/v1/...` — all routes versioned from day one
- **Consistent envelope** for lists:
  ```json
  {
    "data": [...],
    "pagination": { "page": 1, "pageSize": 20, "totalCount": 150, "totalPages": 8 }
  }
  ```
- **Error envelope**:
  ```json
  {
    "errorCode": "PRODUCT_NOT_FOUND",
    "message": "Product with id X was not found.",
    "traceId": "abc-123"
  }
  ```
- **Standard HTTP status codes**: 200, 201, 204, 400, 401, 403, 404, 409, 422, 429, 500
- **Idempotency**: `POST /orders` accepts `Idempotency-Key` header
- **Pagination**: `?page=1&pageSize=20&sortBy=createdAt&sortDir=desc`
- **Soft deletes**: no hard deletes for products or orders (set `DeletedAt`)

---

## 8. Authentication & Authorisation

### Token Strategy

- **Access token**: JWT, 15-minute expiry, signed with RS256
- **Refresh token**: opaque, stored in DB (hashed), 30-day expiry, rotated on use
- **Roles**: `Customer`, `Seller`, `Driver`, `Admin`, `SuperAdmin`

### KYC Flow

```
Seller/Driver registers → uploads documents → status = PendingReview
→ Admin reviews in dashboard → Approve (status = Approved, notif sent)
                             → Reject  (status = Rejected, reason sent)
→ Approved seller can list products
→ Approved driver can accept batches
```

### Policy Examples

```csharp
// In Program.cs
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy(Policies.SellerApproved, p =>
        p.RequireRole(Roles.Seller)
         .RequireClaim(Claims.KycStatus, KycStatus.Approved.ToString()));

    opts.AddPolicy(Policies.AdminOrAbove, p =>
        p.RequireRole(Roles.Admin, Roles.SuperAdmin));

    opts.AddPolicy(Policies.DriverActive, p =>
        p.RequireRole(Roles.Driver)
         .RequireClaim(Claims.KycStatus, KycStatus.Approved.ToString()));
});
```

---

## 9. Real-Time Architecture (SignalR)

### TrackingHub — GPS Broadcasting

```
Driver App → POST /api/v1/drivers/location (every 30s while on delivery)
           → LocationUpdateCommand (MediatR)
           → Save to DB (DriverLocation table, latest only)
           → Publish DriverLocationUpdatedEvent
           → SignalR: TrackingHub.BroadcastLocation(orderId, lat, lng)
           → Customer App (subscribed to orderId group) receives update
```

### Hub Groups

- Customers subscribe to `order:{orderId}` group when viewing tracking screen
- Admins subscribe to `admin:drivers` group to see all active drivers on dashboard map

```csharp
public class TrackingHub : Hub
{
    public async Task SubscribeToOrder(string orderId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");

    public async Task SubscribeToAdminMap()
    {
        // Verify admin role before allowing
        if (!Context.User!.IsInRole(Roles.Admin)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin:drivers");
    }
}
```

---

## 10. Payment Integration

### Zimbabwe-Specific Stack

**Paynow** (primary): supports ZIPIT, bank cards, USD and ZWL  
**Ecocash** (secondary): mobile money, dominant consumer wallet

### Flow

```
Customer checkout
→ POST /api/v1/payments/initiate { orderId, method: "paynow" | "ecocash" }
→ Create PendingPayment record
→ Call Paynow/Ecocash API → get redirect URL / USSD prompt
→ Return paymentUrl to client
→ Client redirects / user approves on phone

Paynow → POST /api/v1/payments/webhook (result URL)
→ Verify signature
→ Update PaymentStatus
→ Publish PaymentConfirmedEvent or PaymentFailedEvent
→ Order status updates, notifications fire
```

### Currency Handling

```csharp
public record Money(decimal Amount, Currency Currency)
{
    // All prices stored in USD in DB
    public Money ToZwl(decimal rate) => new(Amount * rate, Currency.ZWL);
    public Money ToUsd(decimal rate) => new(Amount / rate, Currency.USD);
}
```

Exchange rate fetched daily from RBZ API via Hangfire job, cached in Redis.

---

## 11. File Storage

All uploads go to **Azure Blob Storage** (or AWS S3 as alternative).

### Containers / Buckets

| Container | Contents | Access |
|---|---|---|
| `product-images` | Product photos (up to 5 per product) | Public CDN |
| `kyc-documents` | National IDs, licenses, residence proof | Private (SAS URL, 1hr TTL) |
| `delivery-photos` | Delivery confirmation photos | Private |
| `profile-photos` | Seller/driver/customer avatars | Public CDN |

### Upload Flow

```
Client → POST /api/v1/files/presigned-url { type: "product-image" }
       ← { uploadUrl, fileKey }
Client → PUT uploadUrl (direct to blob, no server relay)
Client → POST /api/v1/products { ..., imageKeys: ["abc/img1.jpg"] }
API    → Validates keys exist in blob, stores final URLs in DB
```

---

## 12. Notifications

All notifications are dispatched asynchronously via Hangfire (fire-and-forget with retry).

| Event | Channel | Recipient |
|---|---|---|
| Order placed | Push + Email | Seller, Customer |
| Payment confirmed | Push + SMS | Customer |
| Item at warehouse | Push | Customer |
| Batch ready | Push | Driver |
| Driver picked up | Push | Customer |
| Delivery completed | Push + Email | Customer |
| KYC approved/rejected | Email + SMS | Seller / Driver |
| New admin account | Email | Admin |

---

## 13. Database Strategy

### Key Tables (simplified)

```sql
users           — base identity table (role discriminator)
sellers         — extends users (kyc fields, business name)
drivers         — extends users (license, vehicle)
customers       — extends users (delivery addresses jsonb)
products        — catalogue (soft delete, fts index on title+description)
categories      — product taxonomy (self-referential for nested)
orders          — order header
order_items     — line items
payments        — payment records (status, gateway reference)
delivery_batches — driver batch header
batch_orders    — junction: batch ↔ orders
driver_locations — latest GPS per driver (upsert on driver_id)
warehouse_items — item + QC status per order
notifications   — outbox for notifications
exchange_rates  — daily ZWL/USD rates
```

### Indexes

```sql
-- Full-text search on products
CREATE INDEX idx_product_fts ON products USING GIN (to_tsvector('english', title || ' ' || description));

-- Geo queries for future proximity search (PostGIS)
CREATE INDEX idx_seller_location ON sellers USING GIST (location);

-- Driver location lookup
CREATE UNIQUE INDEX idx_driver_location ON driver_locations (driver_id);

-- Order status filtering (frequent admin queries)
CREATE INDEX idx_orders_status ON orders (status, created_at DESC);
```

### Migration Strategy

- EF Core Code-First migrations, applied at startup (development) or via CI (staging/production)
- Never delete columns in production migrations — mark `IsObsolete`, remove in next cycle
- Seed data for: Categories, Admin account, Exchange rate

---

## 14. Caching Strategy

| Data | Cache key | TTL | Invalidation |
|---|---|---|---|
| Product detail | `product:{id}` | 10 min | On update |
| Category tree | `categories:all` | 1 hour | On admin change |
| Exchange rate (ZWL) | `exchange-rate:usd-zwl` | 24 hours | Daily Hangfire job |
| User session | `session:{userId}` | 30 min sliding | On logout |
| Driver location | `driver-location:{driverId}` | 60 seconds | On each update |

---

## 15. Background Jobs (Hangfire)

| Job | Schedule | Description |
|---|---|---|
| `UpdateExchangeRateJob` | Daily 06:00 | Fetch RBZ rate, update Redis + DB |
| `SendNotificationJob` | Fire & forget | Dispatch push/SMS/email notifications |
| `CleanExpiredTokensJob` | Nightly | Remove expired refresh tokens |
| `BatchStaleOrdersJob` | Every 30 min | Flag unpaid orders > 2 hours for cancellation |
| `ArchiveOldDeliveryDataJob` | Weekly | Move completed batches to archive table |

---

## 16. Docker & Infrastructure

### docker-compose.yml (production)

```yaml
version: "3.9"

services:
  api:
    build:
      context: .
      dockerfile: src/ZimMarket.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    env_file: .env
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: ${DB_NAME}
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER}"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      retries: 5
    restart: unless-stopped

  hangfire:
    build:
      context: .
      dockerfile: src/ZimMarket.API/Dockerfile
    command: ["dotnet", "ZimMarket.API.dll", "--worker"]
    env_file: .env
    depends_on:
      - postgres
      - redis
    restart: unless-stopped

volumes:
  postgres_data:
  redis_data:
```

### Dockerfile (multi-stage)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ZimMarket.API/ZimMarket.API.csproj", "src/ZimMarket.API/"]
COPY ["src/ZimMarket.Application/ZimMarket.Application.csproj", "src/ZimMarket.Application/"]
COPY ["src/ZimMarket.Domain/ZimMarket.Domain.csproj", "src/ZimMarket.Domain/"]
COPY ["src/ZimMarket.Infrastructure/ZimMarket.Infrastructure.csproj", "src/ZimMarket.Infrastructure/"]
RUN dotnet restore "src/ZimMarket.API/ZimMarket.API.csproj"
COPY . .
WORKDIR "/src/src/ZimMarket.API"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ZimMarket.API.dll"]
```

---

## 17. Frontend Architecture

### Mobile (React Native / Expo)

**App structure** — monorepo with role-based navigation stacks:

```
apps/mobile/
├── src/
│   ├── navigation/
│   │   ├── CustomerStack.tsx
│   │   ├── SellerStack.tsx
│   │   └── DriverStack.tsx
│   ├── screens/
│   │   ├── customer/
│   │   ├── seller/
│   │   └── driver/
│   ├── components/       # shared UI components
│   ├── hooks/            # useAuth, useOrder, useLocation
│   ├── stores/           # Zustand stores
│   ├── api/              # React Query + axios client
│   └── utils/
```

**Offline-first strategy**: React Query caches API responses. Product listings are readable offline. Checkout and payments require connectivity — show a clear offline banner.

### Admin Panel (Next.js)

```
apps/admin/
├── src/
│   ├── app/
│   │   ├── (auth)/login/
│   │   ├── dashboard/
│   │   ├── orders/
│   │   ├── sellers/      # KYC review queue
│   │   ├── drivers/      # KYC + live map
│   │   ├── warehouse/
│   │   └── settings/     # super admin only
│   ├── components/
│   │   ├── ui/           # shadcn/ui wrappers
│   │   └── maps/         # Google Maps live driver view
│   ├── lib/
│   │   ├── api.ts        # typed fetch client
│   │   └── auth.ts       # NextAuth config
│   └── hooks/
```

---

## 18. Security Considerations

| Concern | Approach |
|---|---|
| JWT secrets | RS256 keypair, private key in Key Vault / Docker secret |
| Password hashing | ASP.NET Core Identity (PBKDF2, 350k iterations) |
| KYC documents | Private blob, SAS URL with 1-hour TTL, never public |
| SQL injection | EF Core parameterized queries only, no raw SQL with user input |
| Rate limiting | ASP.NET Core rate limiting middleware — 100 req/min per IP, 20 req/min on auth endpoints |
| CORS | Explicit allow-list of frontend origins only |
| Input sanitisation | FluentValidation on all commands, HtmlEncoder on any stored text displayed |
| Webhook verification | Paynow callback signed — verify HMAC before processing |
| HTTPS | Enforced at load balancer; HSTS header on all responses |
| Sensitive log redaction | Serilog destructuring policy strips passwords, card numbers, national IDs |

---

## 19. Scalability Roadmap

### MVP (current)
- Single API instance, single PostgreSQL, Redis standalone
- Hangfire in same process (or separate container)
- SignalR in-memory backplane (single instance)

### Phase 2 (growth)
- Read replicas for PostgreSQL (reporting, search queries)
- Redis Cluster for distributed caching
- SignalR with Redis backplane (multiple API instances behind load balancer)
- CDN for product images

### Phase 3 (scale)
- Extract high-load modules (Catalogue, Orders, Logistics) into microservices
- Introduce message bus (RabbitMQ or Azure Service Bus) to replace in-process domain events
- Add Elasticsearch for product full-text search
- Kubernetes (AKS / EKS) for orchestration
- CQRS read models with separate read DB (denormalised for performance)

---

## 20. Error Handling & Result Pattern

All business logic errors use the `Result<T>` pattern. Exceptions are reserved for infrastructure failures and are caught at the middleware level.

### Error Codes Reference

```
AUTH_INVALID_CREDENTIALS
AUTH_ACCOUNT_LOCKED
AUTH_TOKEN_EXPIRED
AUTH_REFRESH_INVALID

USER_NOT_FOUND
USER_ALREADY_EXISTS
USER_KYC_PENDING
USER_KYC_REJECTED

PRODUCT_NOT_FOUND
PRODUCT_OUT_OF_STOCK
PRODUCT_NOT_OWNED

ORDER_NOT_FOUND
ORDER_CANNOT_CANCEL
ORDER_ALREADY_PAID

PAYMENT_FAILED
PAYMENT_GATEWAY_UNAVAILABLE
PAYMENT_WEBHOOK_INVALID

DRIVER_NOT_AVAILABLE
DRIVER_NOT_APPROVED

FILE_TOO_LARGE        (max 5MB per image)
FILE_TYPE_NOT_ALLOWED
```

### Global Exception Handler

```csharp
app.UseExceptionHandler(err => err.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
    _logger.LogError(ex, "Unhandled exception {TraceId}", traceId);

    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new
    {
        errorCode = "INTERNAL_SERVER_ERROR",
        message = "An unexpected error occurred. Please try again.",
        traceId
    });
}));
```
