# Collectify API Test Examples

## Testing the API Endpoints

After running the application with `dotnet run`, you can test the endpoints using curl:

### 1. Get all collection items
```bash
curl http://localhost:5000/api/items
```

**Expected Response:**
```json
[
  {
    "id": 1,
    "name": "Sample Item 1",
    "description": "This is a sample collection item",
    "category": "Books",
    "dateAdded": "2025-11-16T10:43:04.9265945",
    "value": 25.99
  },
  {
    "id": 2,
    "name": "Sample Item 2",
    "description": "Another sample item",
    "category": "Coins",
    "dateAdded": "2025-11-16T10:43:04.9266814",
    "value": 150
  }
]
```

### 2. Get a specific item by ID
```bash
curl http://localhost:5000/api/items/1
```

**Expected Response:**
```json
{
  "id": 1,
  "name": "Sample Item 1",
  "description": "This is a sample collection item",
  "category": "Books",
  "dateAdded": "2025-11-16T10:43:04.9265945",
  "value": 25.99
}
```

### 3. Create a new item
```bash
curl -X POST http://localhost:5000/api/items \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Vintage Comic Book",
    "description": "Amazing Spider-Man #1",
    "category": "Comics",
    "dateAdded": "2025-11-16T10:00:00",
    "value": 500.00
  }'
```

**Expected Response:** HTTP 201 Created with location header

### 4. Update an existing item
```bash
curl -X PUT http://localhost:5000/api/items/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Sample Item",
    "description": "Updated description",
    "category": "Books",
    "dateAdded": "2025-11-16T10:00:00",
    "value": 30.00
  }'
```

**Expected Response:** HTTP 204 No Content

### 5. Delete an item
```bash
curl -X DELETE http://localhost:5000/api/items/2
```

**Expected Response:** HTTP 204 No Content

## Using SQLite Browser

You can also inspect the database directly using a SQLite browser tool:

1. Install DB Browser for SQLite: https://sqlitebrowser.org/
2. Open the `collectify.db` file from the project directory
3. Browse the `CollectionItems` table to see the data

## Database Schema Verification

To verify the database schema, you can use the SQLite command line:

```bash
sqlite3 collectify.db ".schema CollectionItems"
```

**Expected Output:**
```sql
CREATE TABLE "CollectionItems" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CollectionItems" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Category" TEXT NULL,
    "DateAdded" TEXT NOT NULL,
    "Value" decimal(18,2) NULL
);
```
