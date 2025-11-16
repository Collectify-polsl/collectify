# Quick Start Guide

## Running the Application

```bash
# 1. Build the project
dotnet build

# 2. Run the application
dotnet run

# The API will be available at:
# - HTTPS: https://localhost:7000
# - HTTP: http://localhost:5000
```

## Quick API Test

```bash
# Get all items
curl http://localhost:5000/api/items

# Get item by ID
curl http://localhost:5000/api/items/1

# Create new item
curl -X POST http://localhost:5000/api/items \
  -H "Content-Type: application/json" \
  -d '{"name":"My Item","description":"Description","category":"Category","dateAdded":"2025-11-16T10:00:00","value":99.99}'
```

## Database

The SQLite database file `collectify.db` is created automatically when you first run the application. It includes 2 pre-seeded sample items.

## Documentation

- **README.md** - Full project documentation
- **TESTING.md** - Detailed API testing examples
- **IMPLEMENTATION_SUMMARY.md** - Technical implementation details

## Technology Stack

- .NET 10.0
- ASP.NET Core Web API
- Entity Framework Core 10.0
- SQLite Database

---
For more information, see README.md
