# E-commerce Platform 🚀

A modern, scalable e-commerce platform built with ASP.NET Core (.NET 8), designed for handling second-hand item transactions. The platform implements clean architecture principles and includes features like caching with Redis, cloud storage integration with MinIO/S3, robust transaction management, and comprehensive API documentation.

## ✨ Core Features

* **Listings Management:** Create, read, update, and delete product listings with flexible, item-specific attributes.
* **Order Processing:** Secure and atomic transaction handling for placing orders, including optimistic concurrency control for listing updates.
* **User & Seller Reviews:** Functionality for users to review sellers post-transaction.
* **Cloud Storage Integration:** Seamless AWS S3/MinIO integration for storing and serving item images and multimedia content.
* **Caching System:** Redis-based caching for frequently accessed data like item listings, user profiles, and search results to improve performance.
* **API Documentation:** Integrated OpenAPI/Swagger for clear, interactive API documentation.
* **Structured Logging:** Comprehensive logging with Serilog for better diagnostics and monitoring.
* **CORS Support:** Configured for client applications (e.g., a Blazor WASM frontend).
* **Health Checks:** Built-in endpoint for monitoring API health and dependencies.
* **Dockerized Environment:** Full `docker-compose` setup for easy local development and dependency management.

## 🛠 Tech Stack

* **Framework:** ASP.NET Core 8
* **Database:** MongoDB (accessed via Entity Framework Core with the official MongoDB provider)
* **Caching:** Redis
* **Cloud Storage:** AWS S3 / MinIO (S3-compatible)
* **API Documentation:** Swagger (Swashbuckle)
* **Logging:** Serilog
* **Architecture:** Clean Architecture principles with a CQRS (Command Query Responsibility Segregation) pattern for application logic.
* **Containerization:** Docker & Docker Compose

## 🏗 Project Structure

The solution follows a clean architecture approach, separating concerns into distinct layers:

* **`EcommercePlatform.Domain`**: Contains core business entities, value objects, and domain logic. This layer has no dependencies on other layers.
* **`EcommercePlatform.Application`**: Contains application-specific business logic, including:
    * CQRS command and query definitions (DTOs).
    * Command and query handlers.
    * Interfaces for repositories, services (e.g., `IUnitOfWork`, `ICacheService`, `ICloudStorageService`), and other application-level contracts.
* **`EcommercePlatform.Infrastructure`**: Implements interfaces defined in the Application layer. This includes:
    * Data persistence logic using EF Core with MongoDB (`AppDbContext`, repository implementations, `UnitOfWork`).
    * Implementations for caching (`RedisCacheService`), cloud storage (`S3StorageService`), etc.
* **`EcommercePlatform.API`**: The ASP.NET Core Web API project. It handles HTTP requests, routes them to application layer handlers (often via controllers), and manages DI, configuration, and middleware.
```text
EcommercePlatform/
├── src/
│   ├── EcommercePlatform.Domain/
│   │   └── Entities/
│   │       ├── User.cs
│   │       ├── ListingItem.cs
│   │       ├── Order.cs
│   │       ├── Review.cs
│   │       ├── GeoLocation.cs
│   │       └── OrderItemInfo.cs
│   │
│   ├── EcommercePlatform.Application/
│   │   ├── Features/
│   │   │   ├── Listings/
│   │   │   │   ├── Commands/
│   │   │   │   ├── Queries/
│   │   │   │   └── Handlers/
│   │   │   └── Orders/
│   │   │   │   ├── Commands/
│   │   │   │   ├── Queries/
│   │   │   │   └── Handlers/
│   │   ├── Interfaces/
│   │   │   ├── Common/
│   │   │   │   └── IUnitOfWork.cs
│   │   │   ├── Repositories/
│   │   │   │   └── IListingRepository.cs 
                  └── ... (other repository interfaces)
│   │   │   └── Services/
│   │   │       ├── ICacheService.cs
│   │   │       └── ICloudStorageService.cs
│   │   └── DTOs/ (Optional)
│   │
│   ├── EcommercePlatform.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Common/
│   │   │   │   └── UnitOfWork.cs
│   │   │   └── Repositories/
│   │   │       └── EfCoreListingRepository.cs
                  └── ... (other repository implementations)
│   │   ├── Caching/
│   │   │   └── RedisCacheService.cs
│   │   ├── CloudStorage/
│   │   │   └── S3StorageService.cs
│   │
│   └── EcommercePlatform.API/
│       ├── Controllers/
│       │   ├── ListingsController.cs
│       │   └── OrdersController.cs
│       │   └── ... (other controllers)
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Dockerfile
│
├── tests/
│   ├── EcommercePlatform.Domain.UnitTests/
│   ├── EcommercePlatform.Application.UnitTests/
│   └── EcommercePlatform.API.IntegrationTests/
│
├── docker-compose.yml
├── EcommercePlatform.sln
└── README.md
```
## 💾 Database Solution Deep Dive

The persistence strategy for this platform is designed for flexibility, scalability, and maintainability, centered around MongoDB as the primary NoSQL database, accessed via Entity Framework Core, and structured with CQRS principles.

### Why MongoDB?

MongoDB, a document-oriented NoSQL database, was chosen for several key advantages:

* **Flexible Schema:** Essential for an e-commerce platform dealing with diverse second-hand items. Different items (e.g., electronics, apparel, furniture) have vastly different attributes. MongoDB's document model allows storing these varied structures within the same `Listings` collection without a rigid, predefined schema. For example, `ItemSpecifics` in a listing can be a flexible dictionary.
* **Scalability:** MongoDB offers robust horizontal scalability through:
    * **Replica Sets:** Provide high availability and read scaling by distributing read operations across multiple secondary nodes.
    * **Sharding:** For very large datasets and high throughput, data can be partitioned across multiple shards (replica sets), enabling near-linear scaling for both reads and writes.
* **Rich Querying & Indexing:** Supports a powerful query language, various index types (single-field, compound, text, geospatial), and an aggregation framework for complex data retrieval and analysis.
* **Geospatial Capabilities:** Built-in support for geospatial data and queries (e.g., `2dsphere` indexes on `Location.coordinates`) is crucial for features like "find items near me" or filtering by region.
* **Developer Productivity:** Working with JSON-like BSON documents often aligns well with modern application development object models.

### Entity Framework Core with MongoDB Provider

While MongoDB is NoSQL, we leverage **Entity Framework Core (EF Core)** with the official `MongoDB.EntityFrameworkCore` provider. This approach offers:

* **Familiar ORM Patterns:** Developers familiar with EF Core can use familiar patterns like `DbContext` (`AppDbContext.cs`), `DbSet<T>`, LINQ queries, and change tracking.
* **POCO Mapping:** Plain Old C# Objects (POCOs) defined in the `Domain` layer are mapped to MongoDB collections and BSON documents.
* **Owned Entity Types for Embedded Documents:** EF Core's concept of "owned entity types" (configured using `.OwnsOne()` and `.OwnsMany()`) maps naturally to MongoDB's embedded documents. This is used for:
    * `GeoLocation` within `ListingItem`.
    * `OrderItemInfo` (as a list) within `Order`.
    * `ContactInfo` and `Address` within `User`.
      This keeps related data together within a single document, which can improve read performance for common scenarios.
* **Unit of Work Pattern:** The `AppDbContext` serves as the core of our Unit of Work. We've abstracted this further with an `IUnitOfWork` interface (defined in the `Application` layer and implemented in `Infrastructure`) to manage transactions and `SaveChangesAsync()` calls. This decouples application logic from direct `DbContext` dependencies.

### CQRS (Command Query Responsibility Segregation)

The CQRS pattern is applied to separate write operations (Commands) from read operations (Queries).

* **Purpose:**
    * **Scalability:** Read and write workloads often have different performance characteristics and scaling requirements. CQRS allows them to be optimized and scaled independently.
    * **Maintainability:** Simplifies models. Write models can be focused on enforcing business rules and consistency for updates, while read models can be tailored specifically for the data needed by a particular view or API endpoint.
    * **Flexibility:** Allows for different data storage or optimization strategies for reads versus writes if needed in the future (though currently, both use the same MongoDB instance).

* **Implementation:**
    * **Commands:** Represent an intent to change the state of the system (e.g., `CreateListingCommand`, `PlaceOrderCommand`).
        * Defined as DTOs in `EcommercePlatform.Application/Features/{FeatureName}/Commands/`.
        * Processed by **Command Handlers** (e.g., `CreateListingCommandHandler.cs`) located in `EcommercePlatform.Application/Features/{FeatureName}/Handlers/`.
        * Command handlers encapsulate the business logic, validation, and orchestration for the operation. They interact with repositories to fetch and modify domain entities and use `IUnitOfWork.SaveChangesAsync()` to persist changes.
    * **Queries:** Represent a request for data, without side effects (e.g., `GetListingByIdQuery`).
        * Defined as DTOs (often simple identifiers or filter criteria) in `EcommercePlatform.Application/Features/{FeatureName}/Queries/`.
        * Processed by **Query Handlers** (e.g., `GetListingByIdQueryHandler.cs`).
        * Query handlers fetch data using repositories, often projecting it directly into specialized **ViewModels** or DTOs (like `ListingItemViewModel`) optimized for the specific needs of the API response or UI. This avoids over-fetching data.
        * Query handlers can also directly incorporate caching logic (e.g., using `ICacheService`).

### Transaction Management

Ensuring data consistency during critical operations is paramount.

* **Unit of Work (`IUnitOfWork`):** As mentioned, this interface (implemented by `UnitOfWork.cs` in `Infrastructure`) is injected into command handlers. It uses the `AppDbContext` to manage operations.
* **MongoDB Multi-Document Transactions:**
    * When `IUnitOfWork.BeginTransactionAsync()` is called, the underlying `AppDbContext` (via the `MongoDB.EntityFrameworkCore` provider) initiates a MongoDB server session and starts a multi-document ACID transaction. This requires MongoDB to be running as a replica set (which is the default for most production deployments and can be configured even for a single-node dev replica set).
    * All database operations (inserts, updates, deletes) performed via repositories and committed with `IUnitOfWork.SaveChangesAsync()` within the scope of this transaction are treated as an atomic unit. If any part fails, the entire transaction can be rolled back using `IUnitOfWork.RollbackTransactionAsync()`.
    * This is crucial for operations like `PlaceOrderCommand`, where updating a listing's status and creating an order document must succeed or fail together.
* **Optimistic Concurrency:**
    * Entities like `ListingItem` have a `Version` property (integer).
    * In `AppDbContext.OnModelCreating()`, this property is configured using `entity.Property(l => l.Version).IsConcurrencyToken();`.
    * When an update is saved via `IUnitOfWork.SaveChangesAsync()`, EF Core includes the original `Version` in the update command. If the `Version` in the database has changed since the entity was loaded, MongoDB won't find a matching document (or the driver/provider handles this check), and EF Core throws a `DbUpdateConcurrencyException`. The application can then handle this (e.g., by informing the user that the data has changed and they need to refresh).

This database solution provides a robust, flexible, and scalable foundation for the e-commerce platform, balancing the power of NoSQL with familiar development patterns and strong consistency guarantees where needed.

## 🚦 Getting Started

### Prerequisites

* .NET 8.0 SDK or later
* Docker Desktop (for running MongoDB, Redis, MinIO via `docker-compose`)
* An IDE like JetBrains Rider or Visual Studio 2022+
* Git

## Running with Docker Compose

### Prerequisites
Before running the application, ensure you have:
- Docker Engine installed
- Docker Compose installed

### Available Services
The application consists of several services:
- ASP.NET Core API (port 8080)
- MongoDB database (port 27017)
- Redis cache (port 6379)
- MinIO storage (API port 9000, Console port 9001)

### Starting the Application
1. Clone the repository
2. Navigate to the root directory containing docker-compose.yaml
3. Start all services:
   `docker-compose up -d`

### Accessing Services
- API: http://localhost:8080
- API Documentation: http://localhost:8080/swagger
- MinIO Console: http://localhost:9001 ("S3BucketName": "ecommerce-bucket", // Ensure this bucket exists)

### Basic Commands
- View running services:
  `docker-compose ps`
- View service logs:
  `docker-compose logs -f`
- Stop all services:
  `docker-compose down`
- Rebuild and restart API:
  `docker-compose build ecommerce-api`
  `docker-compose up -d`

### Data Persistence
Data is automatically persisted in Docker volumes:
- MongoDB data
- Redis cache
- MinIO storage files

### Troubleshooting
If services fail to start:
1. Check service status with `docker-compose ps`
2. View logs with `docker-compose logs [service-name]`
3. Restart specific service with `docker-compose restart [service-name]`

Available service names:
- ecommerce-api
- mongodb
- redis
- minio

## API Documentation

### Available Endpoints

#### Listings
- GET /api/listings/{id}
    - Gets a specific listing by ID
    - Required Path Parameter: id (string)

- POST /api/listings
    - Creates a new listing
    - Required fields: title, description, price, currency

#### Orders
- POST /api/orders
    - Places a new order
    - Required fields:
        - buyerId (string)
        - listingId (string)
        - quantity (number, 1-100)
        - shippingAddress:
            - street (3-200 characters)
            - city (2-100 characters)
            - postalCode (3-20 characters)
            - country (2-100 characters)

- GET /api/orders/{id}
    - Gets order details
    - Required Path Parameter: id (string)

#### Users
- GET /api/users/{id}
    - Gets user profile information
    - Required Path Parameter: id (string)

#### Reviews
- POST /api/reviews
    - Creates a product review
    - Required fields: orderId, rating (1-5), comment

#### Health Check
- GET /health
    - Returns API health status

### Authentication
All endpoints require Bearer token authentication:
- Header: Authorization
- Value: Bearer {your_access_token}

### Response Codes
- 200: Success
- 201: Created
- 400: Bad Request
- 401: Unauthorized
- 403: Forbidden
- 404: Not Found
- 409: Conflict
- 500: Internal Server Error

### Query Parameters
List endpoints support:
- page: Page number (default: 1)
- limit: Items per page (default: 10, max: 100)
- sort: Sort field (example: "createdAt:desc")
- search: Search term
- filter: Filter criteria

### Rate Limits
- 100 requests per minute per IP
- 1000 requests per hour per API key

### Success Response Format
All successful responses include:
- success: true/false
- data: Response data
- message: Operation description

### Error Response Format
All error responses include:
- success: false
- error:
    - code: Error code
    - message: Error description
    - details: Additional information

### Pagination
List endpoints return:
- items: Array of results
- total: Total number of items
- page: Current page number
- limit: Items per page
- pages: Total number of pages

### Configuration (for running API locally without Docker Compose for dependencies)

If you are running MongoDB, Redis, and MinIO/S3 separately (not using the provided `docker-compose.yml` for these services), update the connection strings and settings in `EcommercePlatform/src/EcommercePlatform.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "MongoDbConnection": "your_local_mongodb_connection_string_with_credentials", // e.g., "mongodb://mongoadmin:mongopassword@localhost:27017/"
    "RedisConnection": "your_local_redis_connection_string" // e.g., "localhost:6379"
  },
  "MongoDbDatabaseName": "EcommercePlatformDB_Dev",
  "AWS": {
    "ServiceURL": "your_local_minio_or_s3_endpoint", // e.g., "http://localhost:9000" for local MinIO
    "AccessKeyID": "your_access_key",
    "SecretAccessKey": "your_secret_key",
    "S3BucketName": "ecommerce-bucket", // Ensure this bucket exists
    "ForcePathStyle": true, // Typically true for MinIO
    "Region": "us-east-1" // Placeholder, adjust for actual S3
  }
}

Running the API Application Locally (without Docker for the API itself)Clone the repository:git clone <repository-url>
cd EcommercePlatform
Ensure external services (MongoDB, Redis, MinIO) are running and accessible according to your appsettings.Development.json. You can use the provided docker-compose up -d mongodb redis minio for this.Navigate to the API project directory:cd src/EcommercePlatform.API
Run the application:dotnet run
Access the Swagger UI at: https://localhost:PORT/swagger or http://localhost:PORT/swagger (check your launchSettings.json or Kestrel output for the correct port, typically 5001 for HTTPS or 5000 for HTTP in dev, or 8080 if configured).🐳 Running with Docker Compose (Recommended for Development)The docker-compose.yml file at the root of the project orchestrates all necessary services for a complete local development environment.PrerequisitesDocker Engine installed and running.Docker Compose installed (usually included with Docker Desktop).Available Services via Docker ComposeThe docker-compose.yml sets up:ecommerce-api: The ASP.NET Core API (exposed on host port 8080).mongodb: MongoDB database instance (exposed on host port 27017).redis: Redis cache instance (exposed on host port 6379).minio: MinIO S3-compatible object storage (API on host port 9000, Console on host port 9001).Starting the Application with Docker ComposeClone the repository (if you haven't already).Navigate to the root directory containing docker-compose.yml.Start all services in detached mode:docker-compose up --build -d
--build ensures the API image is built if there are changes.-d runs the containers in the background.Accessing ServicesAPI: http://localhost:8080API Documentation (Swagger UI): http://localhost:8080/swaggerMinIO Console: http://localhost:9001 (Login with minioadmin / minio_password as per docker-compose.yml)MongoDB: Connect using a client to mongodb://mongoadmin:mongopassword@localhost:27017/Redis: Connect using a client to localhost:6379Basic Docker Compose CommandsView running services: docker-compose psView logs for all services (follow): docker-compose logs -fView logs for a specific service: docker-compose logs -f ecommerce-apiStop all services: docker-compose down (add -v to also remove volumes and all data)Rebuild and restart a specific service (e.g., API):docker-compose build ecommerce-api
docker-compose up -d --no-deps ecommerce-api
(Or simply docker-compose up --build -d to rebuild what's necessary and restart)Data PersistenceData for MongoDB, Redis, and MinIO is persisted in Docker named volumes, so your data will remain even if you stop and restart the containers (unless you explicitly remove the volumes with docker-compose down -v).Troubleshooting Docker ComposeIf services fail to start, first check their status: docker-compose psThen, view the logs for the failing service: docker-compose logs [service-name](e.g., docker-compose logs mongodb)Ensure no other applications on your host machine are using the ports mapped in docker-compose.yml.Restart a specific service: docker-compose restart [service-name]📖 API Documentation (via Swagger UI)Once the API is running (either locally or via Docker Compose), navigate to /swagger on the API's base URL (e.g., http://localhost:8080/swagger).Available Endpoints (Examples)ListingsGET /api/listings/{id}Gets a specific listing by its ID.Path Parameter: id (string, e.g., a GUID or MongoDB ObjectId string).POST /api/listingsCreates a new listing.Request Body (JSON, based on CreateListingCommand.cs):{
  "sellerId": "string (user ID)",
  "title": "string (3-100 chars)",
  "description": "string (max 5000 chars)",
  "category": "string",
  "price": 0.00,
  "currency": "USD",
  "condition": "string",
  "itemSpecifics": { "key1": "value1", "key2": 123 },
  "imageObjectKeys": ["path/to/image1.jpg", "path/to/image2.png"],
  "locationLongitude": 0.0,
  "locationLatitude": 0.0,
  "tags": ["tag1", "tag2"]
}
OrdersPOST /api/ordersPlaces a new order for a listing.Request Body (JSON, based on PlaceOrderCommand.cs):{
  "buyerId": "string (user ID of the buyer)",
  "listingId": "string (ID of the listing to purchase)",
  "quantity": 1, // Typically 1 for second-hand items
  "shippingAddress": {
    "street": "string (3-200 chars)",
    "city": "string (2-100 chars)",
    "postalCode": "string (3-20 chars)",
    "country": "string (2-100 chars)"
  }
}
GET /api/orders/{id} (To be fully implemented)Gets order details by its ID.Path Parameter: id (string).Users (Examples - to be expanded)GET /api/users/{id} (To be fully implemented)Gets user profile information.Path Parameter: id (string).POST /api/auth/register (To be implemented)POST /api/auth/login (To be implemented)Reviews (Examples - to be expanded)POST /api/reviews (To be implemented)Creates a review for a seller/listing associated with an order.Required fields: orderId, sellerId, reviewerId, rating (1-5), comment.Health CheckGET /healthReturns the health status of the API and its critical dependencies.AuthenticationMost transactional endpoints (e.g., creating listings, placing orders, submitting reviews) will require Bearer token authentication once implemented.The token should be included in the Authorization header:Authorization: Bearer {your_jwt_access_token}Standard Response Codes200 OK: Request succeeded.201 Created: Resource was successfully created.204 No Content: Request succeeded, but there is no content to return (e.g., after a DELETE).400 Bad Request: The request was invalid (e.g., missing required fields, validation errors). Response body often contains error details.401 Unauthorized: Authentication is required and has failed or has not yet been provided.403 Forbidden: Authenticated user does not have permission to access the resource.404 Not Found: The requested resource could not be found.409 Conflict: The request could not be completed due to a conflict with the current state of the resource (e.g., optimistic concurrency failure, trying to create a resource that already exists with a unique constraint).500 Internal Server Error: An unexpected error occurred on the server.General API Conventions (Placeholders for future implementation)Success Response Format (Example):{
  "success": true,
  "data": { /* Response data object or array */ },
  "message": "Operation completed successfully." // Optional
}
Error Response Format (Example for 400/500 errors):{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR_OR_SPECIFIC_CODE",
    "message": "A description of the error.",
    "details": { /* Optional: field-specific errors or more details */ }
  }
}
Pagination (for list endpoints):Query Parameters: page (default: 1), limit (default: 10, max: 100).Response structure might include:{
  "items": [ /* array of results */ ],
  "totalItems": 150,
  "currentPage": 1,
  "itemsPerPage": 10,
  "totalPages": 15
}
Rate Limits (Example - to be implemented if needed):100 requests per minute per IP.1000 requests per hour per authenticated user/API key.🤝 ContributingDetails on how to contribute to this project will be added here. (Pull requests, issue tracking, coding