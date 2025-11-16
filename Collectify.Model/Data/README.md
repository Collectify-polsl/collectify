# SQLite Database Setup for Collectify

This document describes the SQLite database implementation for the Collectify application.

## Overview

The database implementation uses Entity Framework Core 9.0 with SQLite as the database provider. It follows the Repository and Unit of Work patterns to provide a clean abstraction over data access.

## Database Structure

The database contains the following tables:

- **Templates**: Stores template definitions
- **FieldDefinitions**: Stores field definitions for templates
- **Collections**: Stores user collections based on templates
- **Items**: Stores individual items in collections
- **FieldValues**: Stores field values for items

## Usage

### Creating a Database Context

```csharp
using Microsoft.EntityFrameworkCore;
using Collectify.Model.Data;

// Configure the database path
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
    "Collectify", 
    "collectify.db");

// Create options
var optionsBuilder = new DbContextOptionsBuilder<CollectifyDbContext>();
optionsBuilder.UseSqlite($"Data Source={dbPath}");

// Create context
using var context = new CollectifyDbContext(optionsBuilder.Options);

// Ensure database is created and migrations are applied
context.Database.Migrate();
```

### Using the Unit of Work

```csharp
using Collectify.Model.Data;

// Create Unit of Work
await using var unitOfWork = new UnitOfWork(context);

// Use repositories
var templates = await unitOfWork.Templates.GetAllAsync();
var collections = await unitOfWork.Collections.GetWithItemsAsync();

// Add new entities
var newTemplate = new Template { Name = "Books" };
await unitOfWork.Templates.AddAsync(newTemplate);

// Save changes
await unitOfWork.SaveChangesAsync();
```

### Working with Repositories

Each repository provides standard CRUD operations:

```csharp
// Get by ID
var template = await unitOfWork.Templates.GetByIdAsync(1);

// Get all
var allTemplates = await unitOfWork.Templates.GetAllAsync();

// Find with predicate
var bookTemplates = await unitOfWork.Templates.FindAsync(t => t.Name.Contains("Book"));

// Update
template.Name = "Updated Name";
unitOfWork.Templates.Update(template);
await unitOfWork.SaveChangesAsync();

// Delete
unitOfWork.Templates.Remove(template);
await unitOfWork.SaveChangesAsync();
```

### Specialized Repository Methods

#### TemplateRepository
- `GetWithFieldsAsync(int id)`: Gets a template with all its field definitions loaded

#### CollectionRepository
- `GetWithItemsAsync()`: Gets all collections with their items loaded

#### ItemRepository
- `GetByCollectionIdAsync(int collectionId)`: Gets all items for a specific collection

## Migrations

The project includes Entity Framework Core migrations to manage database schema changes.

### Creating a Migration

```bash
cd Collectify.Model
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

### Applying Migrations

Migrations can be applied automatically at runtime:

```csharp
context.Database.Migrate();
```

Or manually using the CLI:

```bash
cd Collectify.Model
dotnet ef database update
```

## Connection String

The default database location is:
- **Windows**: `%LocalAppData%\Collectify\collectify.db`
- **Linux/Mac**: `~/.local/share/Collectify/collectify.db`

You can customize the database location by changing the connection string when creating the `DbContextOptions`.

## Dependencies

- **Microsoft.EntityFrameworkCore.Sqlite** (9.0.0): SQLite database provider
- **Microsoft.EntityFrameworkCore.Design** (9.0.0): Design-time support for migrations
