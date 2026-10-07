# AcxiomCRM — Enterprise Sales Customer Relationship Management System

**AcxiomCRM** is a web-based, role-governed Customer Relationship Management (CRM) application built for sales organizations using **ASP.NET Core 8.0 MVC**, **Entity Framework Core**, **ASP.NET Core Identity**, and **Bootstrap 5**.

The system manages the entire sales lifecycle:
```text
Lead ➔ Contact ➔ Qualification ➔ Conversion (Transactional) ➔ Customer ➔ Opportunity ➔ Follow-Up / Activities ➔ Pipeline ➔ Won / Lost
```

---

## 1. Architecture & Layering

AcxiomCRM strictly follows a clean, layered architectural design:

```text
AcxiomCRM/
│
├── Controllers/            # MVC Presentation Controllers (Filters, Model binding, Anti-Forgery)
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── CustomerController.cs
│   ├── LeadController.cs
│   ├── OpportunityController.cs
│   ├── FollowUpController.cs
│   ├── ActivityController.cs
│   ├── UserController.cs
│   ├── ReportController.cs
│   └── AuditController.cs
│
├── Api/                    # RESTful Web API Controllers (Swagger/OpenAPI enabled)
│   ├── CustomersApiController.cs
│   ├── LeadsApiController.cs
│   └── OpportunitiesApiController.cs
│
├── Models/                 # Domain Entities & Identity User
│   ├── ApplicationUser.cs
│   ├── Customer.cs
│   ├── Lead.cs
│   ├── Opportunity.cs
│   ├── FollowUp.cs
│   ├── Activity.cs
│   └── AuditLog.cs
│
├── Data/                   # EF Core Data Access & Migrations
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs    # Comprehensive Role & CRM Lifecycle Seeding
│   └── Migrations/
│
├── Services/               # Core Business Logic, Transactions, Scoping & Auditing
│   ├── UserScopeService.cs & UserScope.cs
│   ├── CustomerService.cs
│   ├── LeadService.cs
│   ├── OpportunityService.cs
│   ├── FollowUpService.cs
│   ├── ActivityService.cs
│   ├── DashboardService.cs
│   ├── ReportService.cs
│   └── AuditService.cs
│
├── ViewModels/             # MVC Form & View Models with Data Annotations
│   ├── AccountViewModels.cs
│   ├── CustomerViewModel.cs
│   ├── LeadViewModel.cs
│   ├── OpportunityViewModel.cs
│   ├── FollowUpViewModel.cs
│   ├── ActivityViewModel.cs
│   └── UserViewModels.cs
│
├── DTOs/                   # REST API Data Transfer Objects (No entity leakage)
│   └── CrmDtos.cs
│
├── Views/                  # Clean Bootstrap 5 Razor Views
│   ├── Account/
│   ├── Dashboard/
│   ├── Customer/
│   ├── Lead/
│   ├── Opportunity/
│   ├── FollowUp/
│   ├── Activity/
│   ├── User/
│   ├── Report/
│   ├── Audit/
│   └── Shared/_Layout.cshtml
│
├── wwwroot/                # Static assets (Bootstrap, Bootstrap Icons, Chart.js, custom CSS)
│   ├── css/site.css
│   └── js/site.js
│
├── Program.cs              # DI container, middleware pipeline, Identity & EF configuration
└── appsettings.json        # Configurable database provider, lockout, and password rules
```

---

## 2. Technology Stack

- **Backend Framework:** C# / .NET 8.0, ASP.NET Core MVC & ASP.NET Core Web API
- **Data Access:** Entity Framework Core 8.0 (Code-First with Migrations)
- **Database Engine:** Dual-provider support:
  - **SQLite** (Default for zero-dependency local testing without external services)
  - **Microsoft SQL Server** (Configured for enterprise production environments)
- **Authentication:** ASP.NET Core Identity with secure PBKDF2 hashing, cookie session handling, lockout tracking
- **Authorization:** ASP.NET Core Role-Based Authorization + Server-Side Resource Ownership Scoping (`UserScopeService`)
- **Frontend & Styling:** Razor Views, Bootstrap 5.3, Bootstrap Icons
- **Data Visualization:** Chart.js 4.4
- **API Documentation:** Swashbuckle Swagger UI (`/swagger`)

---

## 3. Getting Started & Setup Instructions

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed on your system.

### Quick Start (Local Run with SQLite)
The application is pre-configured with SQLite out of the box:
```bash
# 1. Clone or navigate to the project directory
cd "Acxiom Project"

# 2. Build the project
dotnet build

# 3. Run the application
dotnet run --urls "http://127.0.0.1:5123"
```
Once started:
- **Web Application:** [http://127.0.0.1:5123](http://127.0.0.1:5123)
- **Interactive Swagger REST Docs:** [http://127.0.0.1:5123/swagger](http://127.0.0.1:5123/swagger)

Database initialization and seeding occur automatically on first startup via `DbInitializer.cs`.

---

## 4. Switching to Microsoft SQL Server

To switch from SQLite to Microsoft SQL Server:
1. Open `appsettings.json`.
2. Change `"DatabaseProvider"` to `"SqlServer"`:
```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AcxiomCRM_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
    "SqliteConnection": "Data Source=acxiomcrm.db"
  }
}
```
3. Run migrations and start the application:
```bash
dotnet ef database update
dotnet run
```

---

## 5. Development Seed Credentials

The database automatically seeds realistic CRM test records and demo accounts for each role:

| Role | Email | Password | Allowed Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.local` | `Admin@12345` | Global scope: All records, full User Management, Audit Logs, all Reports |
| **Manager** | `manager@acxiomcrm.local` | `Manager@12345` | Team scope: Direct reports' leads, customers, team pipeline, team reports |
| **Sales Executive 1** | `sales@acxiomcrm.local` | `Sales@12345` | Assigned scope: Only owned leads, customers, opportunities, and activities |
| **Sales Executive 2** | `sales2@acxiomcrm.local` | `Sales@12345` | Assigned scope: Only owned leads, customers, opportunities, and activities |

*(Password policy requires minimum 8 characters with uppercase, lowercase, digit, and special symbol).*

---

## 6. Role-Based & Resource Ownership Authorization Model

Authorization in AcxiomCRM is strictly enforced **server-side** via `UserScopeService`:

```csharp
public class UserScope
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsManager { get; set; }
    public bool IsSalesExecutive { get; set; }
    public List<string> ManagedUserIds { get; set; }
}
```

- **Admin:** Receives unfiltered queries (`IsAdmin == true`).
- **Manager:** Receives queries filtered to their own records plus all subordinates whose `ManagerId == manager.Id`.
- **Sales Executive:** Receives queries strictly filtered to records where `AssignedTo == currentUser.Id` or `CreatedBy == currentUser.UserName`.
- **Server Enforcement:** If a Sales Executive directly navigates to `/Customer/Details/2` or invokes `GET /api/customers/2` for a customer belonging to another representative, the server intercepts the query, verifies ownership, and returns **HTTP 403 Forbidden** or **HTTP 404 Not Found**.

---

## 7. Sales CRM Lifecycle & Business Rules

### 1. Lead Workflow & Status Transitions
- Permitted Lead statuses: `New` ➔ `Contacted` ➔ `Qualified` ➔ `Converted` (or `Lost` / `Unqualified`).
- Invalid status jumps (e.g. attempting to jump straight from `New` to `Converted` without qualification) are rejected by `LeadService.ValidateStatusTransition`.

### 2. Transactional Lead Conversion
When a lead is qualified and converted:
1. An atomic database transaction (`BeginTransactionAsync`) is initiated.
2. A new `Customer` record is created (or existing selected) with duplicate prevention checks on email and phone.
3. An optional initial `Opportunity` is created and associated with the new customer and originating lead.
4. The `Lead` status is set to `Converted` and `ConvertedCustomerId` is linked.
5. An audit record is logged with action `CONVERT_LEAD`.
6. Transaction commits atomically; any failure rolls back all created records.

### 3. Opportunity Management & Calculations
- **Rules:**
  - Amount cannot be negative (`Amount >= 0`).
  - Active opportunity amount must be strictly greater than zero (`Amount > 0`).
  - Probability must be between 0 and 100 inclusive (`0 <= Probability <= 100`).
  - Active opportunity `ExpectedCloseDate` cannot be in the past.
- **Weighted Pipeline Formula:**
  $$\text{Weighted Value} = \frac{\text{Amount} \times \text{Probability}}{100}$$

### 4. Follow-Up Management
- Statuses: `Planned`, `Completed`, `Missed`, `Cancelled`.
- **Rule:** A new planned follow-up cannot have a date earlier than today.
- Automatic visual badges indicate `Upcoming`, `Due Today`, `Overdue`, and `Completed`.
- Completing or rescheduling generates audit log entries.

---

## 8. Security & Account Protection

1. **Anti-Forgery Protection:** All state-changing forms (`POST`, `PUT`, `DELETE`) require and validate anti-forgery tokens (`[ValidateAntiForgeryToken]`).
2. **Account Lockout:** Configured for 5 failed consecutive login attempts; locks account for 15 minutes and records an `ACCOUNT_LOCKOUT` audit entry. Admins can view lockout status and unlock accounts via `/User`.
3. **No Credential Exposure:** Passwords, password hashes, security stamps, and reset tokens are never exposed in UI, ViewModels, DTOs, or logs.
4. **Audit Logging:** Reusable `AuditService` captures IP address, user ID, timestamp, entity, record ID, and JSON-serialized old/new state changes. Append-only table protected from non-admin modification.

---

## 9. REST API Endpoints

All REST APIs use DTOs to prevent entity leakage and provide comprehensive HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `403 Forbidden`, `404 Not Found`, `409 Conflict`).

| Method | Endpoint | Description | Status Codes |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/customers` | Scoped list of customers | `200` |
| `GET` | `/api/customers/{id}` | Scoped customer details by ID | `200`, `403`, `404` |
| `POST` | `/api/customers` | Create new customer (with uniqueness check) | `201`, `400`, `409` |
| `PUT` | `/api/customers/{id}` | Update existing customer | `200`, `400`, `403`, `404`, `409` |
| `DELETE` | `/api/customers/{id}` | Deactivate/remove customer | `204`, `403`, `404` |
| `GET` | `/api/leads` | Scoped list of leads | `200` |
| `POST` | `/api/leads` | Create new lead with validation | `201`, `400` |
| `GET` | `/api/opportunities` | Scoped list of opportunities | `200` |
| `POST` | `/api/opportunities` | Create opportunity with business rules | `201`, `400` |

Explore and test all endpoints interactively in **Swagger UI** at `/swagger`.

---

## 10. Automated End-to-End Test Suite

An automated verification script is provided at `test_crm_e2e.py` covering 25 test cases:
```bash
python3 test_crm_e2e.py
```

### Verified Scenarios:
1. Admin authentication & CSRF cookie verification
2. Dashboard rendering with 8 scoped KPI cards & Chart.js canvas elements
3. All 8 Reports generation (Customer, Lead, Opportunity, Pipeline, Follow-Up, Conversion, User Activity, Audit)
4. Audit Log access & User Management page rendering
5. Sales Executive authentication & scope verification
6. Strict role boundary enforcement (Sales Executive blocked with HTTP 403 / Access Denied on `/User` and `/Audit`)
7. Full CRUD REST API lifecycle for Customers, Leads, and Opportunities
8. Negative business rule enforcement (HTTP 400 on negative opportunity amount)
9. Duplicate customer email prevention (HTTP 409 Conflict on existing customer email)
10. Transactional lead conversion workflow

---

## 11. Viva & Technical Review Talking Points

- **Why layered architecture?** Decouples UI controllers from business operations and data persistence, allowing independent unit testing, service reusability between MVC controllers and REST API controllers, and centralized transaction management.
- **Why not put authorization only on UI links?** UI links can be bypassed via direct URL navigation or API tools (Postman/cURL). True security requires server-side validation and database-level query scoping (`UserScopeService`).
- **Why use DTOs for APIs?** Prevents mass-assignment vulnerabilities, avoids circular reference serialization errors with EF Core navigation properties, and protects sensitive internal database columns from exposure.
- **Why use transactions during lead conversion?** Creating a customer, optionally generating an opportunity, updating lead status, and writing audit logs are multi-step operations. An atomic transaction guarantees that if any step fails, no orphan records or inconsistent states remain in the database.
