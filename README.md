# RotAndRuin API

A modern e-commerce API built with .NET 8.0, PostgreSQL, and Redis.

## Features

- Product management system
- Image storage and management
- Health checks
- Swagger documentation
- AWS S3 integration for cloud storage
- Redis caching
- PostgreSQL database

## Tech Stack

- Backend: .NET 8.0
- Database: PostgreSQL 15
- Caching: Redis
- Cloud Storage: AWS S3
- Containerization: Docker
- API Documentation: Swagger

## Project Structure

```
src/
├── RotAndRuin.Api/           # ASP.NET Core Web API
├── RotAndRuin.Application/   # Application logic and services
├── RotAndRuin.Domain/        # Domain models and interfaces
└── RotAndRuin.Infrastructure/ # Infrastructure implementations
```

## Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK (for local development)
- PostgreSQL 15
- Redis
- AWS Account (for S3 storage)

## Getting Started

1. Clone the repository
2. Create a `.env` file in the root directory with the following variables:
   ```
   POSTGRES_DB=your_database_name
   POSTGRES_USER=your_username
   POSTGRES_PASSWORD=your_password
   AWS_ACCESS_KEY=your_aws_access_key
   AWS_SECRET_KEY=your_aws_secret_key
   AWS_REGION=eu-west-1
   AWS_BUCKET_NAME=your_bucket_name
   ```

3. Start the application:
   ```bash
   docker-compose up --build
   ```

4. Access the API:
   - Swagger UI: http://localhost:8080/swagger
   - Health Check: http://localhost:8080/health

## API Endpoints

### Product Management

- **GET /api/products**
  - Get all products
  - Returns: List of Product objects

- **GET /api/products/{id}**
  - Get specific product by ID
  - Returns: Product object or 404 if not found

- **POST /api/products**
  - Create new product
  - Request body: ProductCreateRequest
  - Returns: Created product with 201 status

- **GET /api/products/grid**
  - Get products in grid view format
  - Returns: List of ProductGridDto objects

### Product Images

- **POST /api/products/{productId}/images**
  - Upload product image
  - Parameters:
    - `productId`: Guid (path parameter)
    - `file`: IFormFile (multipart/form-data)
    - `isFeatured`: bool (query parameter, optional)
  - Returns: Uploaded image object

- **DELETE /api/products/{productId}/images/{imageId}**
  - Delete specific product image
  - Parameters:
    - `productId`: Guid (path parameter)
    - `imageId`: Guid (path parameter)
  - Returns: 204 No Content or 404 if not found

- **PUT /api/products/{productId}/images/{imageId}/order**
  - Update image display order
  - Parameters:
    - `productId`: Guid (path parameter)
    - `imageId`: Guid (path parameter)
    - `newOrder`: int (request body)
  - Returns: Updated image object

## Development

### Running Locally

1. Build and run:
   ```bash
   dotnet build
   dotnet run --project src/RotAndRuin.Api
   ```

2. Access Swagger at http://localhost:8080/swagger

### Docker Development

1. Build and run with Docker:
   ```bash
   docker-compose up --build
   ```

2. Access Swagger at http://localhost:8080/swagger

## Testing

Run tests using:
```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
