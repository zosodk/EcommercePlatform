# E-commerce Platform

A modern, scalable e-commerce platform built with ASP.NET Core, designed for handling second-hand item transactions. The platform implements clean architecture principles and includes features like caching, cloud storage, and robust error handling.

## 🚀 Features

- **Listings Management**: Create, read, update, and delete product listings
- **Order Processing**: Secure transaction handling with concurrency control
- **Cloud Storage**: AWS S3/MinIO integration for media storage
- **Caching**: Redis-based caching system
- **API Documentation**: OpenAPI/Swagger integration
- **Logging**: Comprehensive logging with Serilog
- **CORS Support**: Configured for Blazor WASM client
- **Health Checks**: Built-in endpoint monitoring

## 🛠 Tech Stack

- **Framework**: ASP.NET Core
- **Database**: MongoDB with Entity Framework Core
- **Caching**: Redis
- **Cloud Storage**: AWS S3/MinIO
- **Documentation**: Swagger/OpenAPI
- **Logging**: Serilog
- **Architecture**: Clean Architecture with CQRS pattern

## 🏗 Project Structure







## 🚦 Getting Started

### Prerequisites

- .NET 7.0 or later
- MongoDB instance
- Redis (optional, for caching)
- AWS S3 or MinIO (optional, for storage)

### Configuration

1. Update the connection strings in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "MongoDbConnection": "your_mongodb_connection_string",
    "RedisConnection": "your_redis_connection_string"
  }
}
```

2. Configure AWS/MinIO settings if using cloud storage:

```json
{
  "AWS": {
    "ServiceURL": "your_s3_endpoint",
    "ForcePathStyle": true
  }
}

```
### Running the Application
1. Clone the repository
2. Navigate to the API project directory:
3. cd EcommercePlatform.API
4. dotnet run
5. Access the Swagger UI at: `https://localhost:<port>/swagger`

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
- MinIO Console: http://localhost:9001

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



