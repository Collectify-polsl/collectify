# Collectify

**Collectify** is an open-source collection management application that allows users to **design custom collection schemas** and manage items using dynamically generated forms, fully offline and with complete local data ownership.

The project focuses on flexibility, clean architecture, and extensibility, avoiding rigid predefined data models and cloud dependencies.

---

## ✨ Key Features

- **Custom collection templates**  
  Define your own collection structures with custom fields and data types.

- **Dynamic form generation**  
  User interfaces are generated automatically based on the selected template.

- **Item management**  
  Create, edit, delete, search, sort, and filter items within collections.

- **Offline-first approach**  
  All data is stored locally — no cloud services, no external APIs.

- **Extensible architecture**  
  Designed to support future features such as import/export, synchronization, and analytics.

---

## 🎯 Use Cases

- Personal collections (books, games, cards, equipment, media, etc.)
- Custom datasets with non-standard schemas
- Local-first alternatives to SaaS collection tools
- Educational or experimental projects involving dynamic data models

---

## 🧱 Architecture Overview

Collectify is built around a **template-driven data model**:

- **Template** — defines the structure of a collection
- **FieldDefinition** — describes individual fields within a template
- **Collection** — an instance of a template
- **Item** — a single record belonging to a collection
- **FieldValue** — stores values linked to field definitions

The system dynamically maps templates to UI forms and persists data locally using a relational database.

---

## 🛠 Tech Stack

- **Language:** C#
- **Framework:** .NET
- **ORM:** Entity Framework Core
- **Database:** Local relational database
- **UI:** Dynamic, template-based forms

---

## 🚀 Getting Started

### Prerequisites

- .NET SDK (compatible version required)
- Supported operating system for .NET applications

### Run Locally

```bash
git clone https://github.com/Collectify-polsl/collectify.git
cd collectify
dotnet restore
dotnet run
```

---

## 📦 Project Status
- Core functionality implemented
- Open for refactoring, feature expansion, and contributions

---

## 📄 License
This project is licensed under the MIT License.
