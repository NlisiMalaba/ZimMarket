# Zimbabwe Marketplace — MVP Task List

> Execute tasks **top-to-bottom** in Cursor. Each numbered task is a focused unit of work.  
> Tasks marked `*` are tests — write them immediately after the implementation task.  
> Complete each Checkpoint before proceeding to the next module.  
> All C# code follows SOLID, DRY, Clean Architecture, and the Result pattern (see DESIGN.md).

---

## Module 0 — Repository & Docker Scaffold

- [x] 0.1 Initialise Git repository; create `.gitignore` (dotnet, node, .env files); create root `README.md` with project overview and local setup instructions

- [x] 0.2 Create solution file and all project skeletons with correct references:
  - `ZimMarket.Domain` (class library, no external deps)
  - `ZimMarket.Application` (class library, refs Domain)
  - `ZimMarket.Infrastructure` (class library, refs Application + Domain)
  - `ZimMarket.API` (ASP.NET Core 10 Web API, refs Infrastructure + Application)
  - `ZimMarket.Shared` (class library, refs nothing — pure constants/models)
  - `ZimMarket.Domain.Tests` (xUnit, refs Domain)
  - `ZimMarket.Application.Tests` (xUnit, refs Application, Testcontainers)
  - `ZimMarket.Integration.Tests` (xUnit, refs API, Testcontainers)
  - Enforce project reference rules — no Infrastructure reference from Domain or Application

- [x] 0.3 Install NuGet packages per project:
  - **Domain**: none
  - **Application**: `MediatR`, `FluentValidation`, `Mapster`, `Mapster.DependencyInjection` *(replaces non-existent `Mapster.Generator` package)*
  - **Infrastructure**: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Hangfire.Core`, `Hangfire.PostgreSql`, `Azure.Storage.Blobs`, `Serilog`, `Twilio`, `SendGrid`, `FirebaseAdmin`; **`FrameworkReference` `Microsoft.AspNetCore.App`** for SignalR and other ASP.NET Core surface *(not the obsolete `Microsoft.AspNetCore.SignalR` package)*
  - **API**: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Scalar.AspNetCore`, `Serilog.AspNetCore`
  - **Tests**: `xUnit`, `Bogus`, `FluentAssertions`, `NSubstitute`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`

- [x] 0.4 Create `docker-compose.yml` and `docker-compose.override.yml` (dev) with services: `api`, `postgres`, `redis`, `hangfire-dashboard`; add health checks on postgres and redis; add named volumes for data persistence; configure environment variable passthrough from `.env`

- [x] 0.5 Create `Dockerfile` for `ZimMarket.API` using multi-stage build (SDK → runtime); add `.dockerignore`; confirm `docker compose up` starts all services and API returns 200 on `GET /health`

- [x] 0.6 Create `.env.example` with all required keys documented (DB connection, Redis, JWT, Blob, SMS, Email, Payment, FCM); commit `.env.example`, never `.env`

- [x] 0.7 Create `GitHub Actions` CI workflow (`.github/workflows/ci.yml`): restore → build → test → Docker build; trigger on `push` to `main` and all PRs

- [x] **Checkpoint 0** — `docker compose up` starts cleanly; `GET /health` returns 200; CI pipeline passes on a clean branch

---

## Module 1 — Domain Layer

- [x] 1.1 Create `BaseEntity` abstract class in Domain: `Id (Guid)`, `CreatedAt (DateTimeOffset)`, `UpdatedAt (DateTimeOffset)`, `DomainEvents (private List<IDomainEvent>)`, `AddDomainEvent()`, `PopDomainEvents()` (clears list and returns snapshot)

- [x] 1.2 Create `IDomainEvent` marker interface (MediatR `INotification` bridged in Application/Infrastructure; Domain stays package-free)

- [x] 1.3 Create value objects — each must be immutable (record or sealed class with private constructor + static `Create` factory returning `Result<T>`):
  - `Money(Amount: decimal, Currency: Currency)` — with `ToZwl(rate)`, `ToUsd(rate)` methods; no negative amounts; max 2 decimal places
  - `Address(Street, Suburb, City, Country)` — all fields required, max lengths enforced
  - `PhoneNumber(Value: string)` — Zimbabwe format validation (+263...), strip spaces
  - `GeoCoordinate(Latitude: double, Longitude: double)` — valid range checks
  - `Currency` enum: `USD`, `ZWL`

- [x] 1.4 Create `UserRole` enum: `Customer`, `Seller`, `Driver`, `Admin`, `SuperAdmin`

- [x] 1.5 Create `KycStatus` enum: `NotSubmitted`, `PendingReview`, `Approved`, `Rejected`

- [x] 1.6 Create `OrderStatus` enum with all states: `Pending → Paid → AtWarehouse → QcPassed → Batched → OutForDelivery → Delivered → Cancelled → Refunded`; add extension method `IsTerminal()` (Delivered, Cancelled, Refunded) and `CanTransitionTo(OrderStatus next)` with valid transition map

- [x] 1.7 Create `User` base aggregate root entity extending `BaseEntity`:
  - Properties: `Email`, `PhoneNumber (PhoneNumber)`, `PasswordHash`, `Role (UserRole)`, `KycStatus`, `IsActive`, `RefreshTokenHash`, `RefreshTokenExpiry`
  - No public property setters — all state changes via domain methods
  - Domain method: `Deactivate()` — sets `IsActive = false`

- [x] 1.8 Create `Customer` entity extending `User`:
  - `DeliveryAddresses: List<Address>` (max 5)
  - `PushNotificationToken: string?`
  - Domain methods: `AddAddress(Address)`, `RemoveAddress(Guid)`, `UpdatePushToken(string)`

- [x] 1.9 Create `Seller` entity extending `User`:
  - `BusinessName`, `NationalIdDocumentKey (string)`, `ProofOfResidenceDocumentKey (string)`, `IsApproved (bool)`, `RejectionReason (string?)`
  - Domain methods: `Approve()` — sets approved, raises `SellerApprovedEvent`; `Reject(reason)` — sets rejected, raises `SellerRejectedEvent`; `SubmitKyc(nationalIdKey, proofKey)` — transitions `KycStatus` to `PendingReview`

- [x] 1.10 Create `Driver` entity extending `User`:
  - `LicenseNumber`, `LicenseDocumentKey`, `VehicleRegistration`, `VehicleDocumentKey`, `DriverStatus (Available | OnDelivery | Offline)`, `LastKnownLocation (GeoCoordinate?)`
  - Domain methods: `Approve()`, `Reject(reason)`, `UpdateLocation(GeoCoordinate)` — raises `DriverLocationUpdatedEvent`, `SetStatus(DriverStatus)`

- [x] 1.11 Create `Category` entity: `Id`, `Name`, `Slug`, `ParentCategoryId (Guid?)` (supports nested categories, max 2 levels)

- [x] 1.12 Create `Product` aggregate root:
  - Properties: `SellerId (Guid)`, `Title`, `Description`, `Price (Money)`, `CategoryId`, `StockQuantity (int)`, `ImageKeys (List<string>)`, `Status (Active | Suspended | Deleted)`, `PickupAddress (Address)`
  - Domain methods: `UpdateDetails(...)`, `UpdateStock(int delta)` — validates never negative; raises `StockDepletedEvent` when hits 0; `Suspend()`, `Restore()`, `Delete()`
  - Invariant: max 5 image keys

- [x] 1.13 Create `Order` aggregate root:
  - Properties: `CustomerId`, `Items (List<OrderItem>)`, `DeliveryAddress (Address)`, `Status (OrderStatus)`, `PaymentStatus`, `PaymentReference`, `TotalAmount (Money)`
  - `OrderItem`: `ProductId`, `ProductTitle (snapshot)`, `UnitPrice (Money)`, `Quantity`, `LineTotal (Money)` — calculated, not stored
  - Domain methods: `ConfirmPayment(reference)` — validates `Pending` state, transitions to `Paid`, raises `PaymentConfirmedEvent`; `Cancel(reason)` — validates not terminal; `UpdateStatus(OrderStatus next)` — validates transition via `CanTransitionTo`

- [x] 1.14 Create `DeliveryBatch` aggregate root:
  - Properties: `DriverId (Guid)`, `OrderIds (List<Guid>)`, `Status (Created | Collected | InTransit | Completed)`, `WarehouseId (Guid)`, `CollectedAt`, `CompletedAt`
  - Domain methods: `AssignDriver(Guid driverId)`, `MarkCollected()`, `MarkInTransit()`, `Complete()` — each raises corresponding domain event

- [x] 1.15 Create `WarehouseItem` entity: `OrderId`, `ProductId`, `ArrivedAt`, `QcStatus (Pending | Passed | Failed)`, `QcNotes`, `PackagedAt`, `BatchId (Guid?)`

- [x] 1.16 Create all domain events as records implementing `IDomainEvent`:
  - `SellerRegisteredEvent(Guid SellerId)`
  - `SellerApprovedEvent(Guid SellerId)`
  - `SellerRejectedEvent(Guid SellerId, string Reason)`
  - `DriverRegisteredEvent(Guid DriverId)`
  - `DriverApprovedEvent(Guid DriverId)`
  - `OrderPlacedEvent(Guid OrderId, Guid CustomerId, decimal TotalUsd)`
  - `PaymentConfirmedEvent(Guid OrderId, string Reference)`
  - `ItemArrivedAtWarehouseEvent(Guid OrderId, Guid WarehouseItemId)`
  - `BatchCreatedEvent(Guid BatchId, Guid DriverId)`
  - `DriverLocationUpdatedEvent(Guid DriverId, double Lat, double Lng, List<Guid> ActiveOrderIds)`
  - `DeliveryCompletedEvent(Guid BatchId, Guid DriverId)`

- [x] 1.17 Create repository interfaces in `Domain/Interfaces/Repositories/`:
  - `IUserRepository<T>` (generic, constrained to `User`) — `GetByIdAsync`, `GetByEmailAsync`, `GetByPhoneAsync`, `AddAsync`, `UpdateAsync`
  - `IProductRepository` — `GetByIdAsync`, `GetPagedAsync(ProductFilter, PaginationParams)`, `FindBySellerAsync(Guid sellerId)`, `AddAsync`, `UpdateAsync`
  - `IOrderRepository` — `GetByIdAsync`, `GetByCustomerAsync`, `GetByStatusAsync`, `AddAsync`, `UpdateAsync`
  - `IDeliveryBatchRepository` — `GetByIdAsync`, `GetActiveByDriverAsync`, `GetPendingBatchesAsync`, `AddAsync`, `UpdateAsync`
  - `IWarehouseItemRepository` — `GetByOrderIdAsync`, `GetUnbatchedAsync`, `AddAsync`, `UpdateAsync`

- [x] 1.18 Create `IUnitOfWork` interface: exposes all repositories as properties; `SaveChangesAsync(CancellationToken)`; `BeginTransactionAsync()`; `CommitAsync()`; `RollbackAsync()`

- [x]* 1.19 Write unit tests for domain entities:
  - **Order status transition**: verify all valid transitions succeed; verify invalid transitions return failure
  - **Product stock**: verify stock cannot go negative; `StockDepletedEvent` raised at 0
  - **Money value object**: verify negative amount rejected; ZWL conversion is correct; equality works
  - **PhoneNumber**: valid Zimbabwe numbers accepted; invalid rejected
  - **Seller KYC state machine**: `SubmitKyc` only works from `NotSubmitted`; `Approve` only from `PendingReview`
  - _Requirements: Domain invariants_

- [x] **Checkpoint 1** — All domain unit tests pass; domain project has zero dependencies on external packages

---

## Module 2 — Application Layer Foundation

- [x] 2.1 Create `Result<T>` and `Result` (non-generic) classes in `Application/Common/Models/`:
  - `Result<T>`: `IsSuccess`, `Value`, `ErrorCode`, `ErrorMessage`, `ValidationErrors (List<ValidationError>?)`
  - Static factories: `Success(T value)`, `Failure(string code, string message)`, `ValidationFailure(List<ValidationError> errors)`
  - `Result` (non-generic): same but no value — for commands with no return
  - `ValidationError(string Field, string Message)`

- [x] 2.2 Create `PaginationParams` in `Shared`: `Page (default 1)`, `PageSize (default 20, max 100)`, `SortBy`, `SortDir`; create `PagedList<T>`: `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`

- [x] 2.3 Create common application interfaces in `Application/Common/Interfaces/`:
  - `ICurrentUser`: `UserId (Guid)`, `Role (UserRole)`, `IsAuthenticated (bool)`, `GetClaim(string)`
  - `IFileStorage`: `UploadAsync(stream, key, contentType, ct)`, `GenerateSasUrlAsync(key, expiry, ct)`, `DeleteAsync(key, ct)`, `GetPresignedUploadUrlAsync(key, contentType, ct)`
  - `IPaymentGateway`: `InitiateAsync(PaymentRequest, ct) → PaymentInitiateResult`; `VerifyWebhookAsync(payload, signature, ct) → PaymentWebhookResult`; `PollStatusAsync(pollUrl, ct) → PaymentPollResult`
  - `ISmsService`: `SendAsync(to, message, ct)`
  - `IEmailService`: `SendAsync(EmailMessage, ct)`
  - `IPushNotificationService`: `SendAsync(token, title, body, data, ct)`; `SendToTopicAsync(topic, ...)`
  - `ICacheService`: `GetAsync<T>(key, ct)`, `SetAsync<T>(key, value, ttl, ct)`, `RemoveAsync(key, ct)`, `RemoveByPatternAsync(pattern, ct)`
  - `IExchangeRateService`: `GetUsdToZwlAsync(ct) → decimal`

- [x] 2.4 Create MediatR pipeline behaviours in `Application/Common/Behaviours/`:
  - **`LoggingBehaviour<TRequest, TResponse>`** — log request name, `UserId`, duration (ms), success/failure; use structured logging (not string interpolation)
  - **`ValidationBehaviour<TRequest, TResponse>`** — discover all `IValidator<TRequest>` from DI; run all validators; if any fail, return `Result.ValidationFailure(errors)` without calling handler; never throw exceptions
  - **`TransactionBehaviour<TRequest, TResponse>`** — only for commands (implement `ICommand` marker interface); wrap handler in `IUnitOfWork` transaction; rollback on exception; no-op for queries
  - **`CachingBehaviour<TRequest, TResponse>`** — only for queries implementing `ICacheable (string CacheKey, TimeSpan Ttl)`; check `ICacheService` first; populate on miss; skip on cache error (never fail a request due to cache unavailability)

- [x] 2.5 Create marker interfaces: `ICommand`, `ICommand<T>`, `IQuery<T>` — all extend `IRequest<Result>` or `IRequest<Result<T>>`; use these as constraints in behaviour registrations

- [x] 2.6 Create `DependencyInjection.cs` in Application: register MediatR (scan assembly), FluentValidation (scan assembly), pipeline behaviours in correct order (Logging → Validation → Transaction → Caching)

- [x] **Checkpoint 2** — Application layer compiles; behaviours registered correctly; basic MediatR pipeline test passes (mock handler receives request)

---

## Module 3 — Infrastructure Layer

- [x] 3.1 Create `AppDbContext` extending `DbContext`:
  - `DbSet<Customer>`, `DbSet<Seller>`, `DbSet<Driver>`, `DbSet<Product>`, `DbSet<Category>`, `DbSet<Order>`, `DbSet<OrderItem>`, `DbSet<DeliveryBatch>`, `DbSet<WarehouseItem>`, `DbSet<ExchangeRate>`
  - Apply all configurations from assembly via `ApplyConfigurationsFromAssembly`
  - Override `SaveChangesAsync` to: auto-set `CreatedAt`/`UpdatedAt`; collect and dispatch domain events via `IPublisher` (MediatR); domain events dispatched **after** save succeeds

- [x] 3.2 Create EF entity configurations (`IEntityTypeConfiguration<T>`) for each entity — put in `Persistence/Configurations/`:
  - Use `HasKey`, `IsRequired`, `HasMaxLength`, value object conversions (`OwnsOne` for `Address`, `Money`, `GeoCoordinate`, `PhoneNumber`)
  - Table-per-hierarchy (TPH) for User discriminator column
  - Soft delete: global query filter `WHERE deleted_at IS NULL` on `Product`
  - Define all indexes (see DESIGN.md section 13)
  - Configure cascade delete rules explicitly (never rely on defaults)

- [x] 3.3 Create `UnitOfWork` implementing `IUnitOfWork`: wraps `AppDbContext`; lazily initialises repositories; transaction management via `IDbContextTransaction`

- [x] 3.4 Create concrete repositories implementing all interfaces from Domain:
  - Use `IQueryable` + LINQ — no raw SQL unless performance-critical
  - Use `AsNoTracking()` on all query-only reads
  - `ProductRepository.GetPagedAsync` must support: category filter, price range, text search (EF.Functions.ILike or full-text), seller filter; apply `OrderBy` before `Skip/Take`
  - Never expose `IQueryable` outside the repository — return concrete types

- [x] 3.5 Create initial EF Core migration; add seed data migration for: default categories (Electronics, Clothing, Food, Home & Garden, Agriculture, Other), SuperAdmin user (credentials from env), initial exchange rate row

- [x] 3.6 Create `JwtService`:
  - Generate access tokens (RS256, 15 min expiry) with claims: `sub (userId)`, `email`, `role`, `kycStatus`, `jti`
  - Generate refresh tokens (cryptographically random 64 bytes, base64 encoded)
  - `ValidateAccessToken(token) → ClaimsPrincipal?` — used in middleware
  - Store refresh token as PBKDF2 hash on the `User` entity (never plaintext)

- [x] 3.7 Create `RedisCacheService` implementing `ICacheService`:
  - Serialize/deserialize with `System.Text.Json`
  - `RemoveByPatternAsync` using `SCAN` (never `KEYS` — blocks Redis)
  - Swallow `RedisException` with logging — cache failures must never break requests
  - Use `IConnectionMultiplexer` (registered as singleton)

- [x] 3.8 Create `AzureBlobStorageService` implementing `IFileStorage`:
  - Separate containers per document type (product-images, kyc-documents, delivery-photos)
  - `GenerateSasUrlAsync` generates read-only SAS with TTL from config (KYC docs: 1 hour, others: 24 hours)
  - `GetPresignedUploadUrlAsync` generates write-only SAS for direct client upload (never relay file bytes through API)
  - Validate allowed content types before generating upload URL

- [x] 3.9 Create `PaynowService` implementing `IPaymentGateway`:
  - Initiate payment (returns redirect URL for web, or mobile checkout URL)
  - Poll payment status
  - `VerifyWebhookAsync` — verify HMAC-SHA512 signature using shared secret from config; return failure if invalid (do not process)
  - Never log payment amounts or references at DEBUG level in production

- [ ] 3.10 Create `EcocashService` implementing `IPaymentGateway` (same interface as Paynow — swappable via factory)

- [x] 3.11 Create `PaymentGatewayFactory` implementing `IPaymentGatewayFactory`: resolves correct gateway by `PaymentMethod` using .NET keyed services

- [x] 3.12 Create `TwilioSmsService` implementing `ISmsService`; create `SendGridEmailService` implementing `IEmailService` with HTML email templates for: welcome, KYC approved, KYC rejected, order confirmation, delivery notification

- [x] 3.13 Create `FcmPushNotificationService` implementing `IPushNotificationService` using Firebase Admin SDK

- [x] 3.14 Create `HangfireJobSetup.cs`: configure Hangfire with PostgreSQL storage; register all recurring jobs with `AddOrUpdate`; create `HangfireAuthFilter` (admin-only access to Hangfire dashboard)

- [x] 3.15 Create `ExchangeRateService` implementing `IExchangeRateService`: fetch from Redis first (key: `exchange-rate:usd-zwl`); if miss, read from DB `exchange_rates` table; return last known rate (never fail on stale rate)

- [x] 3.16 Create `Infrastructure/DependencyInjection.cs`: register all services, keyed services (payment gateways), DbContext (with retry policy for transient failures), Redis connection multiplexer (singleton), Hangfire, SignalR

- [x]* 3.17 Write integration tests using Testcontainers:
  - **ProductRepository**: add product → retrieve by id → update stock → soft delete → confirm filtered from queries
  - **UnitOfWork transaction**: command that fails mid-way → verify DB is unchanged (rollback works)
  - **RedisCacheService**: set → get → TTL expires → returns null; `RemoveByPattern` removes correct keys
  - _All tests use real Postgres + Redis containers spun up by Testcontainers_

- [x] **Checkpoint 3** — Infrastructure layer compiles; migration applies cleanly (`docker compose up`); integration tests pass

---

## Module 4 — Auth & Identity Features

- [x] 4.1 Create `RegisterCustomerCommand(Email, Phone, Password, FullName, PushToken?)` with handler:
  - Validate: email unique, phone unique, password meets policy (min 8 chars, 1 uppercase, 1 number)
  - Hash password via `IPasswordHasher<User>`
  - Create `Customer` entity; add to repo; save via `IUnitOfWork`
  - Return `Result<AuthTokensDto>` (access token + refresh token)
  - _No domain events needed for customer registration_

- [x] 4.2 Create `RegisterSellerCommand(Email, Phone, Password, FullName, BusinessName)` with handler:
  - Similar to customer registration
  - Set `KycStatus = NotSubmitted`
  - Raise `SellerRegisteredEvent` (handler sends welcome email with KYC instructions)
  - Return `Result<AuthTokensDto>`

- [x] 4.3 Create `RegisterDriverCommand(Email, Phone, Password, FullName)` with handler:
  - Set `KycStatus = NotSubmitted`
  - Raise `DriverRegisteredEvent`
  - Return `Result<AuthTokensDto>`

- [x] 4.4 Create `LoginQuery(Email, Password, DeviceInfo?)` with handler:
  - Validate credentials; check `IsActive`; check `KycStatus` where relevant
  - Issue access + refresh token pair
  - Return `Result<AuthTokensDto>` with `KycStatus` included so client can redirect to KYC upload screen

- [x] 4.5 Create `RefreshTokenCommand(AccessToken, RefreshToken)` with handler:
  - Validate access token is expired (not invalid sig); validate refresh token hash matches; check not expired
  - Rotate: generate new pair, invalidate old refresh token
  - Return `Result<AuthTokensDto>`

- [x] 4.6 Create `LogoutCommand(RefreshToken)`: clear refresh token hash from user record

- [x] 4.7 Create `SubmitSellerKycCommand(NationalIdKey, ProofOfResidenceKey)` with handler:
  - Verify caller is a `Seller` (via `ICurrentUser`)
  - Validate file keys exist in blob storage (call `IFileStorage.ExistsAsync`)
  - Call `seller.SubmitKyc(...)` domain method
  - Save, return `Result`

- [x] 4.8 Create `SubmitDriverKycCommand(LicenseDocKey, VehicleDocKey, LicenseNumber, VehicleRegistration)` — same pattern

- [x] 4.9 Create domain event handlers (MediatR `INotificationHandler<T>`):
  - `SellerRegisteredEventHandler` → send welcome email via `IEmailService`
  - `SellerApprovedEventHandler` → send approval SMS + email; update `KycStatus` claim
  - `SellerRejectedEventHandler` → send rejection email with reason
  - `DriverApprovedEventHandler` / `DriverRejectedEventHandler` — same pattern

- [x] 4.10 Configure ASP.NET Core Identity (custom user store backed by `AppDbContext` + EF Core); configure JWT bearer authentication; add role-based + policy-based authorisation; configure `ICurrentUser` via `HttpContextAccessor`

- [x] 4.11 Create `AuthController` (`/api/v1/auth`):
  - `POST /register/customer` → `RegisterCustomerCommand`
  - `POST /register/seller` → `RegisterSellerCommand`
  - `POST /register/driver` → `RegisterDriverCommand`
  - `POST /login` → `LoginQuery`
  - `POST /refresh` → `RefreshTokenCommand`
  - `POST /logout` → `LogoutCommand` [Authorize]
  - `POST /kyc/seller` → `SubmitSellerKycCommand` [Authorize(Seller)]
  - `POST /kyc/driver` → `SubmitDriverKycCommand` [Authorize(Driver)]
  - All endpoints return consistent JSON envelope; map `Result` to status codes

- [x]* 4.12 Write unit tests for auth handlers:
  - **RegisterCustomer**: duplicate email returns `USER_ALREADY_EXISTS`; duplicate phone returns conflict; successful registration returns tokens
  - **Login**: wrong password returns `AUTH_INVALID_CREDENTIALS`; deactivated account returns forbidden
  - **RefreshToken**: expired refresh token returns `AUTH_REFRESH_INVALID`; valid rotation returns new pair and invalidates old
  - **SubmitSellerKyc**: non-seller caller returns forbidden; already submitted returns conflict

- [x]* 4.13 Write integration tests for auth endpoints (using `WebApplicationFactory` + Testcontainers):
  - Full registration → login → refresh → logout flow
  - Concurrent registration with same email returns 409 on second request

- [x] **Checkpoint 4** — Auth endpoints working end-to-end; JWT validated on protected routes; KYC submission persisted; all auth tests pass

---

## Module 5 — File Upload

- [x] 5.1 Create `GetPresignedUploadUrlQuery(FileType, ContentType, FileSizeBytes)` with handler:
  - `FileType` enum: `ProductImage`, `NationalId`, `ProofOfResidence`, `DriverLicense`, `VehicleDoc`, `DeliveryPhoto`, `ProfilePhoto`
  - Validate content type (images: `image/jpeg`, `image/png`, `image/webp` only; max 5MB)
  - Generate file key: `{container}/{userId}/{guid}.{ext}`
  - Call `IFileStorage.GetPresignedUploadUrlAsync`
  - Return `Result<PresignedUrlDto> { UploadUrl, FileKey, ExpiresAt }`

- [x] 5.2 Create `FilesController` (`/api/v1/files`):
  - `POST /presigned-url` [Authorize] → `GetPresignedUploadUrlQuery`
  - `GET /kyc-document/{key}` [Authorize(Admin)] → calls `IFileStorage.GenerateSasUrlAsync` and returns short-lived URL (never redirect directly to SAS — log the access)

- [x] **Checkpoint 5** — Client can get presigned URL, upload directly to blob, use key in subsequent commands

---

## Module 6 — Catalogue (Products)

- [x] 6.1 Create `CreateProductCommand(Title, Description, PriceUsd, CategoryId, StockQuantity, ImageKeys, PickupAddress)` with handler:
  - `[Authorize(Policy = Policies.SellerApproved)]` — KYC must be approved
  - Validate: category exists, max 5 image keys, price > 0, stock ≥ 0, image keys exist in blob
  - Create `Product` entity; save; return `Result<Guid>` (product id)

- [x] 6.2 Create `UpdateProductCommand(ProductId, ...)` with handler:
  - Verify caller owns the product (`product.SellerId == currentUser.UserId`)
  - Call domain methods on `Product`; save; invalidate cache key `product:{id}`

- [x] 6.3 Create `DeleteProductCommand(ProductId)` with handler:
  - Verify ownership; call `product.Delete()` (soft delete); save; invalidate cache

- [x] 6.4 Create `UpdateStockCommand(ProductId, Delta)` with handler (for seller to adjust inventory)

- [x] 6.5 Create `GetProductByIdQuery(Guid ProductId)` implementing `ICacheable`:
  - Cache key: `product:{productId}`, TTL: 10 minutes
  - Return `Result<ProductDetailDto>` with seller name, category, all images as public URLs

- [x] 6.6 Create `SearchProductsQuery(SearchTerm?, CategoryId?, MinPriceUsd?, MaxPriceUsd?, Page, PageSize)` with handler:
  - Apply full-text search via `EF.Functions.ToTsVector` / `ILike` fallback
  - Apply all filters via `Specification<Product>` pattern (compose specs)
  - Return `Result<PagedList<ProductSummaryDto>>`
  - Do **not** cache search results — too many permutations

- [x] 6.7 Create `GetSellerProductsQuery(Page, PageSize)` [Authorize(Seller)] — returns caller's own products including inactive ones

- [x] 6.8 Create `GetCategoriesQuery` implementing `ICacheable` (key: `categories:all`, TTL: 1 hour)

- [x] 6.9 Create `ProductsController` (`/api/v1/products`):
  - `GET /` → `SearchProductsQuery` [public]
  - `GET /{id}` → `GetProductByIdQuery` [public]
  - `POST /` → `CreateProductCommand` [Authorize(SellerApproved)]
  - `PUT /{id}` → `UpdateProductCommand` [Authorize(SellerApproved)]
  - `DELETE /{id}` → `DeleteProductCommand` [Authorize(SellerApproved)]
  - `PATCH /{id}/stock` → `UpdateStockCommand` [Authorize(SellerApproved)]
  - `GET /my` → `GetSellerProductsQuery` [Authorize(Seller)]
  - `GET /categories` → `GetCategoriesQuery` [public]

- [x]* 6.10 Write unit tests for catalogue handlers:
  - **CreateProduct**: non-approved seller returns forbidden; more than 5 images returns validation error; invalid category returns error
  - **SearchProducts**: text filter returns matching results; price range filter excludes out-of-range; pagination returns correct page
  - **UpdateProduct**: caller not owner returns forbidden; valid update invalidates cache

- [x] **Checkpoint 6** — Product CRUD works end-to-end; search returns filtered paginated results; cache invalidated on updates

---

## Module 7 — Orders & Cart

- [x] 7.1 Create `PlaceOrderCommand(Items: List<OrderItemDto>, DeliveryAddress, PaymentMethod)` with handler:
  - `[Authorize(Customer)]`
  - For each item: verify product exists, is `Active`, has sufficient stock; create stock reservation (decrement `StockQuantity` within same transaction)
  - Calculate `TotalAmount` in USD (always); create `Order` entity with `Status = Pending`
  - Raise `OrderPlacedEvent`; save; return `Result<PlaceOrderResultDto> { OrderId, TotalUsd, TotalZwl }`
  - The total in ZWL is calculated at order time using current exchange rate and included in response for display — it is not stored

- [x] 7.2 Create `CancelOrderCommand(OrderId, Reason)` with handler:
  - `[Authorize(Customer)]` — customer can only cancel their own order
  - Verify order is in cancellable state (`CanTransitionTo(Cancelled)`)
  - Call `order.Cancel(reason)`; restore stock (reverse the reservation); save
  - Raise domain event to notify seller

- [x] 7.3 Create `GetOrderByIdQuery(OrderId)` with handler:
  - `[Authorize]` — customer sees own orders; admin/seller see relevant orders
  - Return `Result<OrderDetailDto>` including current status, items, payment status, delivery batch id if assigned

- [x] 7.4 Create `GetCustomerOrdersQuery(Page, PageSize, StatusFilter?)` [Authorize(Customer)]

- [x] 7.5 Create `OrdersController` (`/api/v1/orders`):
  - `POST /` → `PlaceOrderCommand` [Authorize(Customer)]
  - `GET /` → `GetCustomerOrdersQuery` [Authorize(Customer)]
  - `GET /{id}` → `GetOrderByIdQuery` [Authorize]
  - `POST /{id}/cancel` → `CancelOrderCommand` [Authorize(Customer)]

- [x]* 7.6 Write unit tests:
  - **PlaceOrder**: out-of-stock product returns `PRODUCT_OUT_OF_STOCK`; stock is decremented on success; `OrderPlacedEvent` is raised
  - **CancelOrder**: terminal status returns `ORDER_CANNOT_CANCEL`; stock is restored on cancellation

- [x] **Checkpoint 7** — Orders placed, cancelled, retrieved; stock correctly managed

---

## Module 8 — Payments

- [x] 8.1 Create `InitiatePaymentCommand(OrderId, PaymentMethod)` with handler:
  - `[Authorize(Customer)]` — verify caller owns the order
  - Verify order is `Pending`; check not already initiated (idempotency via `Idempotency-Key` header stored in DB)
  - Call `IPaymentGatewayFactory.Create(method).InitiateAsync(...)` with order total
  - Persist payment record (`PaymentStatus = Initiated`, `GatewayReference`, `Method`)
  - Return `Result<PaymentInitiateDto> { PaymentUrl, GatewayReference }`

- [x] 8.2 Create `ProcessPaymentWebhookCommand(Payload, Signature, GatewayType)` with handler:
  - Called from webhook endpoint (no `[Authorize]` — instead verify HMAC signature inside handler)
  - `IPaymentGateway.VerifyWebhookAsync(...)` — return `Result.Failure` if invalid signature (log the attempt)
  - On success: call `order.ConfirmPayment(reference)` → saves → dispatches `PaymentConfirmedEvent`
  - On failure: update `PaymentStatus = Failed`; raise event to notify customer
  - **Idempotent** — if reference already processed, return success without re-processing

- [x] 8.3 Create `PaymentConfirmedEventHandler`:
  - Update order status to `Paid`
  - Notify seller (push + email) via `IPushNotificationService` / `IEmailService` (dispatched via Hangfire fire-and-forget)
  - Notify customer (push + SMS receipt)

- [x] 8.4 Create `PaymentsController` (`/api/v1/payments`):
  - `POST /initiate` → `InitiatePaymentCommand` [Authorize(Customer)]
  - `POST /webhook/paynow` → `ProcessPaymentWebhookCommand(... GatewayType.Paynow)` [no auth — HMAC verified inside]
  - `POST /webhook/ecocash` → `ProcessPaymentWebhookCommand(... GatewayType.Ecocash)` [same]
  - Webhook endpoints must return `200 OK` immediately even if processing fails (acknowledge receipt; log internally)

- [x]* 8.5 Write unit tests:
  - **InitiatePayment**: order not owned by caller returns forbidden; already-initiated order with same idempotency key returns same result (not duplicate)
  - **ProcessWebhook**: invalid HMAC returns failure without processing; duplicate reference is idempotent; successful webhook triggers `PaymentConfirmedEvent`

- [x] **Checkpoint 8** — Payment initiation returns redirect URL; webhook processes and updates order; notifications dispatched

---

## Module 9 — Warehouse Management

- [x] 9.1 Create `RecordItemArrivalCommand(OrderId, Notes?)` with handler [Authorize(Admin)]:
  - Verify order is in `Paid` status
  - Create `WarehouseItem` with `QcStatus = Pending`
  - Transition order to `AtWarehouse`; raise `ItemArrivedAtWarehouseEvent`
  - Save; notify customer (push)

- [x] 9.2 Create `UpdateQcStatusCommand(WarehouseItemId, QcStatus, Notes?)` with handler [Authorize(Admin)]:
  - `QcStatus`: `Passed` or `Failed`
  - On `Passed`: transition order to `QcPassed`
  - On `Failed`: flag for admin review (do not auto-cancel — admin decides)
  - Save

- [x] 9.3 Create `GetWarehouseItemsQuery(QcStatus?, Page, PageSize)` [Authorize(Admin)]: returns paginated list of items at warehouse with order details

- [x] 9.4 Create `GetUnbatchedItemsQuery` [Authorize(Admin)]: returns `QcPassed` items not yet assigned to a batch; used for batch creation UI

- [x] 9.5 Create `WarehouseController` (`/api/v1/warehouse`) [Authorize(AdminOrAbove)]:
  - `POST /arrivals` → `RecordItemArrivalCommand`
  - `PATCH /items/{id}/qc` → `UpdateQcStatusCommand`
  - `GET /items` → `GetWarehouseItemsQuery`
  - `GET /items/unbatched` → `GetUnbatchedItemsQuery`

- [ ] **Checkpoint 9** — Items can be received, QC'd, and queried; order status transitions correctly

---

## Module 10 — Logistics & Driver GPS

- [x] 10.1 Create `CreateDeliveryBatchCommand(OrderIds: List<Guid>, DriverId)` with handler [Authorize(Admin)]:
  - Verify all orders are `QcPassed` and unbatched
  - Verify driver is `Approved` and `Available`
  - Create `DeliveryBatch` aggregate; transition each order to `Batched`; set driver status to `OnDelivery`
  - Raise `BatchCreatedEvent` → notify driver (push with pickup instructions)
  - Save; return `Result<Guid>` (batch id)

- [x] 10.2 Create `UpdateDriverLocationCommand(Latitude, Longitude)` with handler [Authorize(Driver)]:
  - Validate driver is `OnDelivery` (ignore if offline)
  - Upsert `driver_locations` table (one row per driver)
  - Cache location in Redis (key: `driver-location:{driverId}`, TTL: 90 seconds)
  - Raise `DriverLocationUpdatedEvent`
  - Event handler broadcasts via `IHubContext<TrackingHub>` to groups: `order:{orderId}` for each active order in batch; `admin:drivers` group

- [x] 10.3 Create `ConfirmBatchCollectedCommand(BatchId)` with handler [Authorize(Driver)]:
  - Verify driver owns batch; call `batch.MarkCollected()`; transition orders to `OutForDelivery`
  - Notify customers (push: "Your order is on the way!")
  - Save

- [x] 10.4 Create `ConfirmDeliveryCommand(BatchId, OrderId, DeliveryPhotoKey)` with handler [Authorize(Driver)]:
  - Validate photo key exists in blob
  - Transition specific `Order` to `Delivered`
  - If all orders in batch delivered: call `batch.Complete()`; set driver `Available`; raise `DeliveryCompletedEvent`
  - Notify customer (push + email receipt)

- [x] 10.5 Create `GetActiveDriverLocationsQuery` [Authorize(Admin)]: returns latest GPS coordinates for all `OnDelivery` drivers from Redis; fallback to DB if Redis miss

- [x] 10.6 Create `GetBatchDetailsQuery(BatchId)` [Authorize]: driver sees their own batches; admin sees all

- [x] 10.7 Create `TrackingHub` (SignalR):
  - `SubscribeToOrder(orderId)` — add caller to `order:{orderId}` group; validate caller owns the order
  - `SubscribeToAdminMap()` — add caller to `admin:drivers` group; validate admin role
  - `UnsubscribeFromOrder(orderId)` — remove from group on order delivered or customer navigates away
  - Broadcast method: `LocationUpdated(lat, lng, timestamp)` — called from `DriverLocationUpdatedEvent` handler via `IHubContext`

- [x] 10.8 Create `DriversController` (`/api/v1/drivers`):
  - `POST /location` → `UpdateDriverLocationCommand` [Authorize(DriverActive)]
  - `GET /batches/{id}` → `GetBatchDetailsQuery` [Authorize(DriverActive)]
  - `POST /batches/{id}/collected` → `ConfirmBatchCollectedCommand` [Authorize(DriverActive)]
  - `POST /batches/{id}/orders/{orderId}/delivered` → `ConfirmDeliveryCommand` [Authorize(DriverActive)]

- [x] 10.9 Create `BatchesController` (`/api/v1/batches`) [Authorize(AdminOrAbove)]:
  - `POST /` → `CreateDeliveryBatchCommand`
  - `GET /` → `GetBatchesQuery(Status?, Page, PageSize)`
  - `GET /{id}` → `GetBatchDetailsQuery`
  - `GET /drivers/locations` → `GetActiveDriverLocationsQuery`

- [x]* 10.10 Write unit tests:
  - **CreateBatch**: order not in `QcPassed` state returns error; driver not available returns error; batch created transitions orders to `Batched`
  - **UpdateDriverLocation**: `DriverLocationUpdatedEvent` raised with correct coordinates; non-OnDelivery driver is ignored
  - **ConfirmDelivery**: all orders delivered → batch completes → driver set to `Available`

- [x] **Checkpoint 10** — Driver can update GPS; customers receive real-time updates via SignalR; delivery lifecycle complete

---

## Module 11 — Admin Features

- [x] 11.1 Create `GetPendingKycQuery(Role: Seller|Driver, Page, PageSize)` [Authorize(Admin)]: returns paginated list of users with `KycStatus = PendingReview` with document SAS URLs

- [x] 11.2 Create `ApproveKycCommand(UserId, Role)` with handler [Authorize(Admin)]:
  - Load entity; call `Approve()` domain method; save; domain event fires notification

- [x] 11.3 Create `RejectKycCommand(UserId, Role, Reason)` with handler [Authorize(Admin)]:
  - Call `Reject(reason)` domain method; save; domain event fires rejection email/SMS

- [x] 11.4 Create `SuspendProductCommand(ProductId, Reason)` [Authorize(Admin)]: for admin to take down listings that violate policies

- [x] 11.5 Create `GetAllOrdersQuery(Status?, DateFrom?, DateTo?, Page, PageSize)` [Authorize(Admin)]: full order management view

- [x] 11.6 Create `OverrideOrderStatusCommand(OrderId, NewStatus, Reason)` [Authorize(Admin)]: for manual intervention; skips `CanTransitionTo` validation (admin override); logs reason

- [x] 11.7 Create `GetDashboardStatsQuery` [Authorize(Admin)]: returns aggregate stats — orders today, revenue today (USD), active drivers, pending KYC count, low stock products; cache 5 minutes

- [x] 11.8 Create `CreateAdminCommand(Email, Password, FullName)` [Authorize(SuperAdmin)]: creates an Admin-role user; sends credentials email

- [x] 11.9 Create `DeactivateUserCommand(UserId)` [Authorize(AdminOrAbove)]: sets `IsActive = false`; invalidates all refresh tokens; can reactivate via `ActivateUserCommand`

- [x] 11.10 Create `AdminController` (`/api/v1/admin`) [Authorize(AdminOrAbove)]:
  - `GET /kyc` → `GetPendingKycQuery`
  - `POST /kyc/{userId}/approve` → `ApproveKycCommand`
  - `POST /kyc/{userId}/reject` → `RejectKycCommand`
  - `PATCH /products/{id}/suspend` → `SuspendProductCommand`
  - `GET /orders` → `GetAllOrdersQuery`
  - `PATCH /orders/{id}/status` → `OverrideOrderStatusCommand`
  - `GET /dashboard` → `GetDashboardStatsQuery`
  - `POST /admins` → `CreateAdminCommand` [Authorize(SuperAdmin)]
  - `POST /users/{id}/deactivate` → `DeactivateUserCommand`

- [x] **Checkpoint 11** — Admin can review KYC, manage orders, view dashboard; super admin can create admins

---

## Module 12 — API Hardening

- [x] 12.1 Add global `ExceptionHandlingMiddleware`: catch all unhandled exceptions; log with `traceId`; return standardised 500 response with `traceId` for client correlation; never expose stack traces in production

- [x] 12.2 Configure ASP.NET Core Rate Limiting middleware:
  - Global: 200 req/min per IP
  - Auth endpoints (`/api/v1/auth`): 20 req/min per IP (prevent brute force)
  - File presign endpoint: 30 req/min per user
  - Return `429 Too Many Requests` with `Retry-After` header

- [x] 12.3 Configure Serilog with structured logging: console sink (dev), file sink (production with rolling), Seq sink if available; add request logging middleware; redact sensitive properties (Password, NationalIdNumber, CardNumber) via destructuring policy

- [x] 12.4 Add health check endpoints: `GET /health` (liveness), `GET /health/ready` (readiness — checks DB + Redis connectivity); used by Docker health checks

- [x] 12.5 Configure Scalar (OpenAPI) for API documentation: group endpoints by tag; include example request/response bodies; JWT auth scheme configured so devs can test authenticated endpoints

- [x] 12.6 Add `Idempotency-Key` middleware: for `POST /orders` and `POST /payments/initiate`; store key → response in Redis (TTL 24 hours); return cached response on duplicate key without re-processing

- [x] 12.7 Add CORS policy: explicit allow-list (mobile app origins + admin panel origin from config); disallow `*`

- [x] 12.8 Configure `appsettings.json` schema with all required configuration keys; add validation at startup via `IOptions<T>` with `ValidateOnStart` and `ValidateDataAnnotations` — application must fail-fast if required config is missing, not at runtime

- [x] **Checkpoint 12** — API is hardened; rate limiting prevents abuse; health checks pass; OpenAPI docs accessible; missing config causes immediate startup failure

---

## Module 13 — Background Jobs

- [x] 13.1 Implement `UpdateExchangeRateJob`:
  - Fetch USD/ZWL rate from RBZ API (or fallback provider)
  - Upsert `exchange_rates` table; update Redis cache key
  - Log old rate vs new rate for auditing
  - Schedule: daily at 06:00 UTC

- [x] 13.2 Implement `CleanExpiredRefreshTokensJob`:
  - Delete `User` rows where `RefreshTokenExpiry < now` and `RefreshTokenHash IS NOT NULL`
  - Only nullifies the hash — does not delete the user
  - Schedule: nightly at 02:00 UTC

- [x] 13.3 Implement `CancelStaleOrdersJob`:
  - Find `Orders` with `Status = Pending` and `CreatedAt < now - 2 hours` (unpaid)
  - Call `order.Cancel("Payment not completed within 2 hours")`; restore stock; notify customer
  - Schedule: every 30 minutes

- [x] 13.4 Implement `SendNotificationJob` (fire-and-forget, not scheduled):
  - Dispatched by domain event handlers via `IBackgroundJobClient.Enqueue`
  - Retry up to 3 times with exponential backoff on failure
  - Job payload: `{ UserId, Channel (Push|SMS|Email), TemplateId, Parameters }`

- [x] **Checkpoint 13** — Jobs registered and running; exchange rate updated; stale orders cancelled automatically

---

## Module 14 — Admin Panel (Next.js)

- [x] 14.1 Scaffold Next.js 14 app in `apps/admin/` with TypeScript, Tailwind CSS, shadcn/ui; configure `NEXT_PUBLIC_API_URL` env var; set up path aliases

- [x] 14.2 Create typed API client in `lib/api.ts`: all requests include `Authorization: Bearer {token}` header; handle `401` globally (redirect to login, clear session); handle `429` with user-visible "Too many requests" toast

- [x] 14.3 Implement login page (`/login`): email + password form; call `POST /api/v1/auth/login`; store access token in memory (not localStorage); store refresh token in httpOnly cookie; redirect to `/dashboard`

- [x] 14.4 Create authenticated layout with sidebar navigation: Dashboard, Orders, Sellers (KYC), Drivers (KYC + Map), Warehouse, Settings (SuperAdmin only); role-based menu items

- [x] 14.5 Implement Dashboard page (`/dashboard`):
  - Fetch `GET /api/v1/admin/dashboard` stats every 30 seconds
  - Stat cards: orders today, revenue (USD), active drivers, pending KYC, low stock
  - Recent orders table (last 10)

- [x] 14.6 Implement KYC Review pages (`/sellers` and `/drivers`):
  - Paginated table of pending KYC submissions with status badges
  - Click row to open side panel: user details, document preview (fetch SAS URL from API, render in iframe)
  - Approve button → `POST /api/v1/admin/kyc/{id}/approve`
  - Reject button → opens modal with reason text field → `POST /api/v1/admin/kyc/{id}/reject`
  - Optimistic UI update on approval/rejection

- [x] 14.7 Implement Order Management page (`/orders`):
  - Filterable, sortable, paginated table: status, date range, customer name
  - Order detail side panel: items, payment info, delivery batch, timeline
  - Status override button (admin) with confirmation modal

- [x] 14.8 Implement Warehouse page (`/warehouse`):
  - Tab 1: Record Arrival — order id input, notes, submit
  - Tab 2: QC Queue — list of arrived items, pass/fail buttons with notes
  - Tab 3: Unbatched items — ready to be batched, multi-select → "Create Batch" with driver assignment dropdown

- [x] 14.9 Implement Driver Live Map page (`/drivers/map`):
  - Google Maps component; connect to SignalR `TrackingHub`; call `SubscribeToAdminMap()`
  - Render driver markers; update position on `LocationUpdated` event
  - Click marker → show driver info panel (name, current batch, orders count)

- [x] 14.10 Implement Settings page (`/settings`) [SuperAdmin only]:
  - Create Admin form: email, name, temporary password
  - List existing admins with deactivate button

- [x] **Checkpoint 14** — Admin panel fully functional; KYC review workflow end-to-end; live driver map updates via SignalR

---

## Module 15 — Mobile App (React Native / Expo)

### Setup

- [x] 15.1 Scaffold Expo app in `apps/mobile/` with TypeScript, Expo Router; install: `react-native-maps`, `@tanstack/react-query`, `zustand`, `axios`, `react-hook-form`, `zod`, `@gorhom/bottom-sheet`, `expo-location`, `expo-image-picker`, `expo-notifications`

- [x] 15.2 Create API client with Axios: base URL from env; request interceptor adds auth token from Zustand store; response interceptor handles `401` (try refresh → if fails, clear auth and redirect to login)

- [x] 15.3 Create `useAuth` hook and Zustand auth store: persist tokens securely with `expo-secure-store` (never AsyncStorage for tokens); expose `login`, `logout`, `register`, `isAuthenticated`, `user`

- [x] 15.4 Create Expo Router layout with role-based root: unauthenticated → `(auth)` stack; Customer → `(customer)` tab layout; Seller → `(seller)` tab layout; Driver → `(driver)` tab layout

### Customer App

- [ ] 15.5 Customer onboarding: splash → register screen (email, phone, password, name) → OTP verification screen → home

- [ ] 15.6 Home / Browse screen: category grid at top; product search bar; paginated product list with infinite scroll (`useInfiniteQuery`); product card (image, title, price USD + ZWL equivalent)

- [ ] 15.7 Product Detail screen: image carousel (max 5); title, description, seller name, price; "Add to Cart" button; stock badge

- [ ] 15.8 Cart screen: list of items with quantity controls; remove item; subtotal in USD and ZWL; "Proceed to Checkout" button

- [ ] 15.9 Checkout screen: delivery address selector / add new address; payment method selector (Paynow / Ecocash); order summary; "Place Order" → calls `POST /api/v1/orders` → on success navigate to payment screen with redirect URL

- [ ] 15.10 Payment screen: WebView loading Paynow/Ecocash URL; listen for redirect back to app deep link; on success navigate to "Order Confirmed" screen

- [ ] 15.11 Orders list screen: tabbed by status (Active, Completed, Cancelled); order row with status badge and timestamp

- [ ] 15.12 Order tracking screen:
  - Connect to SignalR hub on mount; subscribe to `order:{orderId}`
  - Google Maps showing driver marker; animate marker movement on `LocationUpdated` event
  - Order status timeline (step indicator)
  - Disconnect hub on unmount

- [ ] 15.13 Profile screen: edit name, phone; manage delivery addresses; push notification preferences; logout

### Seller App

- [ ] 15.14 Seller onboarding: register → KYC upload screen (national ID photo picker → upload via presigned URL, proof of residence → same) → "Application submitted" screen with status polling

- [ ] 15.15 Seller dashboard: stats cards (active listings, orders pending, total earned); recent orders list

- [ ] 15.16 Listings screen: list of seller's products with status badges; tap to edit; FAB to create new listing

- [ ] 15.17 Create/Edit listing form: title, description, price, category picker, stock, address, image picker (max 5, upload via presigned URL); form validation with Zod + React Hook Form; submit → `POST /api/v1/products`

- [ ] 15.18 Orders screen (seller view): list of orders containing seller's products; order detail with customer address (no PII beyond city)

### Driver App

- [ ] 15.19 Driver onboarding: register → document upload (license, vehicle registration) → "Under review" screen

- [ ] 15.20 Driver home screen: current status toggle (Available / Offline); active batch card if `OnDelivery`; available batches list when `Available`

- [ ] 15.21 Batch detail screen: pickup warehouse address (Google Maps link); list of delivery orders grouped by area; "Mark as Collected" button

- [ ] 15.22 Active delivery screen:
  - Next delivery address in Google Maps navigation
  - "Confirm Delivery" button: opens camera to take photo → upload via presigned URL → call `POST /api/v1/drivers/batches/{id}/orders/{orderId}/delivered`
  - Progress indicator: X of N delivered

- [ ] 15.23 GPS background tracking:
  - Use `expo-task-manager` + `expo-location` background task
  - Start on "Mark as Collected"; stop when batch complete
  - POST to `PUT /api/v1/drivers/location` every 30 seconds
  - Handle permission denied gracefully with in-app prompt explaining why location is needed

- [ ] **Checkpoint 15** — Full mobile flows working: customer can browse → buy → track; seller can list → manage orders; driver can accept → collect → deliver with live GPS

---

## Module 16 — Final Integration & Testing

- [ ] 16.1 Write end-to-end integration test suite covering the full happy path:
  - Register seller → submit KYC → admin approves → list product
  - Register customer → browse → place order → Paynow webhook fires → order paid
  - Admin records warehouse arrival → QC pass → create batch → assign driver
  - Driver confirms collection → updates GPS (3 updates) → confirms delivery
  - Verify: customer receives delivery notification; order status is `Delivered`; driver status is `Available`

- [ ] 16.2 Write load test (k6 or NBomber): product search endpoint — 200 concurrent users, 60 seconds; target p95 < 500ms; fail if error rate > 1%

- [ ] 16.3 Review and confirm all endpoints in Scalar/OpenAPI docs have: correct auth requirements documented, example request and response bodies, error response codes listed

- [ ] 16.4 Security review checklist:
  - [ ] No secrets in code or git history
  - [ ] KYC document URLs are SAS-protected (never public)
  - [ ] Webhook endpoints verify HMAC before processing
  - [ ] Rate limiting active on all auth endpoints
  - [ ] All user inputs validated (FluentValidation)
  - [ ] No raw SQL with user-provided values

- [ ] 16.5 Add `CONTRIBUTING.md`: local setup guide, how to run tests, how to add a new feature (CQRS flow template), Docker commands reference, branching strategy

- [ ] **Final Checkpoint** — Full E2E test suite passes; load test within targets; all security checklist items verified; Docker Compose starts cleanly from a fresh clone with only `.env` populated
