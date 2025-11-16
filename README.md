# Collectify - Collection Management API

A simple ASP.NET Core Web API project with SQLite database integration for managing collection items.

## Features

- SQLite database integration using Entity Framework Core
- RESTful API endpoints for CRUD operations
- Sample data seeding
- OpenAPI/Swagger support for API documentation

## Prerequisites

- .NET 10.0 SDK or later

## Database

The project uses SQLite as its database provider. The database file (`collectify.db`) is created automatically when the application runs for the first time.

### Database Schema

**CollectionItems Table:**
- `Id` (int) - Primary Key
- `Name` (string, required, max 200 chars) - Item name
- `Description` (string, optional, max 1000 chars) - Item description
- `Category` (string, optional, max 100 chars) - Item category
- `DateAdded` (DateTime) - Date when item was added
- `Value` (decimal) - Item value

## API Endpoints

### Collection Items

- `GET /api/items` - Get all collection items
- `GET /api/items/{id}` - Get a specific item by ID
- `POST /api/items` - Create a new item
- `PUT /api/items/{id}` - Update an existing item
- `DELETE /api/items/{id}` - Delete an item

### Sample Endpoints

- `GET /weatherforecast` - Get sample weather forecast data

## Getting Started

### 1. Build the project

```bash
dotnet build
```

### 2. Run the application

```bash
dotnet run
```

The application will start at `https://localhost:7000` (or `http://localhost:5000` for HTTP).

### 3. Access the API

- OpenAPI documentation: `https://localhost:7000/openapi/v1.json` (in Development mode)
- Example: Get all items: `http://localhost:5000/api/items`

## Configuration

Database connection string can be configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=collectify.db"
  }
}
```

## Sample Data

The database is seeded with two sample collection items on first run:
1. Sample Item 1 (Books category, value: $25.99)
2. Sample Item 2 (Coins category, value: $150.00)

## Project Structure

```
Collectify.Api/
├── Data/
│   └── CollectifyDbContext.cs    # Database context
├── Models/
│   └── CollectionItem.cs         # Entity model
├── Program.cs                    # Application entry point and configuration
├── appsettings.json             # Configuration settings
└── Collectify.Api.csproj        # Project file
```

## Technologies Used

- ASP.NET Core 10.0
- Entity Framework Core 10.0
- SQLite 10.0
- Minimal APIs

## License

This project is open source and available under the MIT License.
