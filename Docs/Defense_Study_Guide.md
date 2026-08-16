# ProVMS — Defense Day Study Guide
## "What Your Professor Will Ask & How to Answer"

---

## WHAT IS PROVMS? (Your 30-Second Pitch)

> "ProVMS is a web-based Procurement and Vendor Management System built with ASP.NET Core 9.0 MVC and MySQL. It digitizes the full procurement lifecycle — from vendor registration and accreditation, to purchase requests, budget approval, purchase order issuance, delivery tracking, and supplier performance evaluation — all inside a single, role-governed web application."

---

## SECTION 1: THE TECH STACK — "What technologies did you use?"

| Layer | What You Used | Why |
|-------|--------------|-----|
| **Backend Framework** | ASP.NET Core 9.0 MVC | Structured MVC pattern, built-in dependency injection, policy-based authorization |
| **ORM** | Entity Framework Core + Pomelo MySQL | Code-first database management, migrations, LINQ queries |
| **Database** | MySQL (via XAMPP locally, databaseasp.net in production) | Relational database — suited for transactional data with FK relationships |
| **Authentication** | ASP.NET Cookie Auth + BCrypt password hashing | Secure, stateful session management |
| **Security** | Google reCAPTCHA v2, antiforgery tokens, RBAC policies | Multi-layer login protection |
| **Frontend** | Bootstrap 5 + Chart.js + Razor Views | Responsive UI, data visualization |
| **PDF** | iText7 | Purchase Order PDF generation |
| **Hosting** | Kestrel (dev) / MonsterASP IIS (production) | Cross-platform .NET web server |

---

## SECTION 2: THE ARCHITECTURE — "Explain the system architecture"

```
[Browser] → HTTP Request
     ↓
[ASP.NET Core Middleware Pipeline]
  → Cookie Authentication (validates session)
  → Authorization (checks RBAC policy)
  → Controller Action (business logic)
     ↓
[Entity Framework Core]
  → Builds SQL query from C# LINQ
     ↓
[MySQL — ProVMSDB]
  → Returns data
     ↓
[Razor View (.cshtml)]
  → Renders HTML with C# data
     ↓
[Browser] ← HTTP Response
```

**Key point to say:**
> "The system follows the MVC pattern — Models hold the data structure, Views handle the UI, and Controllers handle the business logic. Entity Framework acts as the bridge between C# objects and MySQL tables."

---

## SECTION 3: THE DATABASE — "Explain your database design"

### Tables and What They Store

| Table | Purpose |
|-------|---------|
| `Users` | All system users — Admin, Finance, Procurement, Internal Users, Vendors |
| `Vendors` | Company info, accreditation status, linked to a Users account |
| `VendorItems` | Products/services a vendor offers, with unit price and category |
| `PurchaseRequisitions` | The full procurement workflow record — from PR submission to delivery |
| `DepartmentBudgets` | Allocated, spent, and remaining budget per department per fiscal year |
| `SupplierEvaluations` | 5-star ratings submitted by users after delivery |
| `InAppNotifications` | Bell hub messages per user |
| `Contracts` | Vendor contracts with status lifecycle |
| `SLATracking` | Timestamps for workflow SLA compliance monitoring |
| `FinancialAuditTrail` | Immutable log of every financial transaction |

### Key Relationships
- `Users` ←→ `Vendors` (one-to-one via `LinkedUserID`)
- `Vendors` ←→ `VendorItems` (one-to-many)
- `PurchaseRequisitions` → `Users` (requester), → `VendorItems` (item ordered), → `DepartmentBudgets` (budget check)
- `SupplierEvaluations` → `PurchaseRequisitions` + `Vendors`

### Global Query Filter
> "AppUser has a global query filter that automatically excludes archived (soft-deleted) users from all queries — so I never hard-delete users, I just mark `IsArchived = true`."

---

## SECTION 4: AUTHENTICATION & SECURITY — "How does login work?"

### Step-by-Step Login Flow

1. **User submits** email + password + reCAPTCHA checkbox
2. **reCAPTCHA check** — Google verifies the token (bypassed on localhost for development)
3. **Lockout gate** — if `LockoutEnd > now`, block login immediately
4. **BCrypt.Verify** — compare input password against the stored BCrypt hash
5. **Fail path** — increment `AccessFailedCount`; at 5 failures → set `LockoutEnd = now + 15 minutes`
6. **Vendor gate** — if role is Vendor, check `OperationalStatus == Active`; block if Pending/Suspended
7. **Success** — reset fail count, issue ASP.NET cookie session + JWT in HttpOnly cookie
8. **Redirect** by role — Admin/Finance/Procurement → Dashboard, Vendor → Profile, User → Marketplace

### Security Layers
- **BCrypt** — passwords never stored in plaintext, cost factor 11
- **Antiforgery tokens** — every POST form has `[ValidateAntiForgeryToken]` to prevent CSRF attacks
- **RBAC Policies** — every controller action has `[Authorize(Policy = "...")]`
- **Account lockout** — 5 failed attempts = 15-minute lockout, per-account isolation
- **reCAPTCHA v2** — stops automated login attacks
- **Soft delete** — users are archived, not deleted — audit trail preserved

---

## SECTION 5: ROLE-BASED ACCESS CONTROL — "How do you control who can access what?"

> "I use ASP.NET Core's policy-based authorization. Each policy maps to one or more roles. Every controller action is decorated with `[Authorize(Policy = "...")]`. If a user's role doesn't match, they get HTTP 403 Forbidden."

### Policy Map

| Policy | Who Can Access |
|--------|---------------|
| `AdminOnly` | Admin only |
| `FinanceOnly` | Finance only |
| `VendorOnly` | Vendor only |
| `RequesterOnly` | Internal User only |
| `ProcurementOnly` | Procurement only |
| `ProcurementOrAdmin` | Procurement + Admin |
| `FinanceOrAdmin` | Finance + Admin |
| `AnalyticsViewers` | Admin + Finance + Procurement |
| `POVaultViewers` | Everyone except public |
| `InternalUsers` | Admin + Finance + Procurement + User (no Vendor) |

**Example to say:**
> "A Vendor cannot access the Marketplace — that's `RequesterOnly`. An Internal User cannot issue a Purchase Order — that's `ProcurementOnly`. Finance cannot see the Accreditation Desk — that's `ProcurementOrAdmin`."

---

## SECTION 6: THE PROCUREMENT WORKFLOW — "Walk me through how a purchase happens"

This is the most important flow to explain:

```
Step 1 — Internal User browses Marketplace (/Catalog/Marketplace)
         Sees vendor items filtered by availability

Step 2 — User submits Purchase Requisition (/Catalog/PlaceRequisition)
         System checks: Does dept budget have enough?
         YES → Budget encumbered (locked), PR status = "Pending_Finance"
         NO  → HTTP 400 rejected immediately

Step 3 — Finance Manager reviews (/Catalog/ApprovalWorkflow)
         APPROVE → Status = "Approved_Budget"
         REJECT  → Budget unlocked, status = "Rejected", user notified

Step 4 — Procurement Officer issues PO (/Catalog/IssuePO)
         Assigns vendor, status = "PO_Issued", vendor notified
         PDF Purchase Order generated via iText7

Step 5 — Vendor dispatches goods (/Catalog/UpdateCargoStatus)
         Status = "In_Transit"

Step 6 — Procurement confirms delivery (/Catalog/MarkDelivered)
         Status = "Delivered"

Step 7 — Internal User confirms receipt + submits evaluation
         5-star rating recorded, status = "Archived"
         Budget SpentAmount updated permanently
```

---

## SECTION 7: BUDGET SYSTEM — "How does budget control work?"

> "Every department has an allocated budget for the fiscal year stored in `DepartmentBudgets`. When a PR is submitted, the system immediately checks if `RemainingBudget >= TotalCost`. If not, it rejects with HTTP 400. If yes, the amount is encumbered — flagged as `IsEncumbered = true` — so no other PR can double-spend that money. The budget is only permanently deducted when delivery is confirmed."

### Budget Fields
- `AllocatedBudget` — set by Finance Manager
- `SpentAmount` — increases only on confirmed delivery
- `RemainingBudget` — computed: `AllocatedBudget - SpentAmount`
- `IsEncumbered` on PR — prevents double-spend during approval process

---

## SECTION 8: VENDOR MANAGEMENT — "How does vendor onboarding work?"

1. **Vendor self-registers** at `/Vendor/Onboarding` — public page, no login needed
2. A `UserRole.Vendor` account is created immediately, but **login is blocked**
3. **Admin/Procurement reviews** at `/Vendor/AccreditationDesk`
4. **Approve** → `OperationalStatus = Active`, vendor can now log in
5. **Reject** → `OperationalStatus = Suspended`, vendor is notified

> "I intentionally blocked vendor login until approval because otherwise any self-registered vendor could immediately access the system before being vetted."

---

## SECTION 9: NOTIFICATIONS — "How do notifications work?"

> "I built an in-app notification system using `InAppNotifications` table. Every workflow event — PR submitted, approved, rejected, PO issued, delivery confirmed — triggers a notification to the relevant user. The bell icon in the navbar shows unread count, fetched via AJAX from `/api/Notifications`."

---

## SECTION 10: SLA TRACKING — "What are SLAs and how do you enforce them?"

| Process | Time Limit | What Happens |
|---------|-----------|-------------|
| Vendor Accreditation Review | 48 hours | Highlighted in Accreditation Desk if overdue |
| Finance Approval/Rejection | 24 hours | Highlighted in Approval Workflow |
| Vendor Dispatch Update | 72 hours | Highlighted in Delivery Tracking |

> "SLAs are tracked in the `SLATracking` table with `OpenedAt` and `ClosedAt` timestamps. The system highlights overdue items in red so the responsible role can see what needs urgent action."

---

## SECTION 11: COMMON PROFESSOR QUESTIONS & ANSWERS

### Q: "Why did you use ASP.NET Core MVC instead of React or Angular?"
> "MVC is well-suited for this type of enterprise application because the server controls the routing, authorization, and data rendering. With Razor views, I can directly use C# objects in the UI without building a separate API layer. The RBAC policies are enforced at the server level, which is more secure."

### Q: "What is Entity Framework Core?"
> "It's an ORM — Object-Relational Mapper. Instead of writing raw SQL, I define C# model classes and EF Core generates the SQL automatically. I use `DbContext` with `DbSet<T>` for each table. `EnsureCreated()` creates the schema on first run, and I can use migrations for schema changes."

### Q: "What is the difference between authentication and authorization?"
> "Authentication is verifying WHO you are — that's the login process with BCrypt and cookies. Authorization is verifying WHAT you're allowed to do — that's the RBAC policies like `[Authorize(Policy = 'FinanceOnly')]`."

### Q: "How do you prevent SQL injection?"
> "Entity Framework Core uses parameterized queries by default — user input is never concatenated directly into SQL strings. EF Core handles all the escaping."

### Q: "What is BCrypt?"
> "BCrypt is a password hashing algorithm. When a user registers, the password is hashed using BCrypt with a cost factor of 11 — meaning it performs 2^11 = 2048 rounds of hashing. When logging in, I use `BCrypt.Verify()` to compare the input against the stored hash. The plaintext password is never stored."

### Q: "What is an antiforgery token?"
> "It's a hidden field in every form that contains a unique server-generated token. When the form is submitted, ASP.NET Core validates that the token matches. This prevents CSRF attacks — where a malicious website tricks a logged-in user into submitting a form to my app without their knowledge."

### Q: "What is a global query filter?"
> "It's a filter I set in `OnModelCreating` in the DbContext. For `AppUser`, I added `.HasQueryFilter(u => !u.IsArchived)` — so every single query on the Users table automatically excludes archived users. I never have to remember to add `WHERE IsArchived = 0` manually."

### Q: "What happens when a vendor is rejected?"
> "Their `OperationalStatus` is set to `Suspended`. They receive an in-app notification. Their login is blocked because the accreditation gate in `AuthController` checks the status before issuing the session cookie."

### Q: "How does the budget encumbrance work?"
> "When a PR is submitted, `IsEncumbered = true` is set on the requisition record. The budget check subtracts all encumbered amounts from the remaining budget — not just confirmed spending. So if two users try to buy ₱5M from a ₱5M budget at the same time, the second one will be rejected even if the first hasn't been approved yet."

### Q: "What is the MVC pattern?"
> "MVC stands for Model-View-Controller. The Model is the data — my C# classes like `Vendor`, `PurchaseRequisition`. The View is the UI — my `.cshtml` Razor files. The Controller handles the HTTP request, calls the database via EF Core, and passes data to the View. The flow is: Request → Controller → Model → View → Response."

### Q: "Is your system a CRM or SCM system?"
> "ProVMS is neither a pure CRM nor a pure SCM — it is best classified as a **Procurement & Vendor Management System (PVMS)**, which overlaps with elements of both.
>
> **What CRM is:** Customer Relationship Management — focuses on managing relationships with *customers* (sales pipeline, customer support, marketing). ProVMS does NOT do this.
>
> **What SCM is:** Supply Chain Management — focuses on the end-to-end flow of goods from raw materials to the end customer (logistics, warehousing, inventory). ProVMS does NOT manage the full supply chain.
>
> **What ProVMS actually is — it borrows from both:**
>
> | Feature | Closest System Type | Why |
> |---------|-------------------|-----|
> | Vendor registration & accreditation | **SRM** (Supplier Relationship Management) | Managing the relationship with *suppliers*, not customers |
> | Purchase requisitions & PO issuance | **Procurement System** | The buying process inside an organization |
> | Budget approval workflow | **ERP — Financial Module** | Budget governance across departments |
> | Vendor performance evaluation | **SRM / ERP** | Rating suppliers based on delivery performance |
> | Delivery tracking | **SCM (partial)** | Tracks goods from vendor dispatch to receipt |
>
> **The most accurate classification is SRM — Supplier Relationship Management** — a subcategory of SCM that focuses specifically on managing vendor/supplier interactions, accreditation, and performance. Combined with a procurement workflow, it is also called a **P2P system — Procure-to-Pay**.
>
> **One-line answer to say in defense:**
> 'ProVMS is an SRM and P2P system — it manages the full supplier lifecycle from onboarding to payment authorization, with budget governance built in. It is not a CRM because we manage suppliers, not customers.'"

### Q: "Is your system a REST API?"
> "ProVMS is primarily an **MVC Web Application**, not a REST API — but it has **one REST API endpoint** built into it.
>
> **The main system** uses the MVC pattern — the server renders full HTML pages using Razor Views and returns them to the browser. This is called a **server-side rendered (SSR)** web app. The browser requests a URL, the Controller processes it, and the server sends back a complete HTML page.
>
> **The one REST API endpoint** is the Notifications controller — `/api/Notifications` and `/api/Notifications/MarkAllRead`. These return JSON data instead of HTML, and the browser calls them via jQuery AJAX without reloading the page. That specific part follows REST principles — it uses HTTP GET to fetch data and HTTP POST to update state, and it responds with JSON.
>
> **So the honest answer is:** The system is a hybrid — 95% MVC web app with server-side rendering, and 5% REST API for the notification bell feature.
>
> **Why MVC instead of full REST API?**
> MVC with Razor Views is better suited for this type of enterprise admin system because authorization is enforced at the server level on every page render — a user cannot even receive the HTML if they don't have the right role. With a pure REST API + frontend SPA approach, I would need to build a separate frontend and handle authorization twice."

### Q: "What APIs or external libraries did you use in the system?"
> "The system uses a combination of external APIs and NuGet packages. Let me break them down:
>
> **External APIs (called over the internet):**
>
> | API | What It Does in ProVMS |
> |-----|----------------------|
> | **Google reCAPTCHA v2 API** | Called during login — sends the user's checkbox token to `https://www.google.com/recaptcha/api/siteverify` and Google returns whether it's a real human or a bot |
>
> **NuGet Packages (backend libraries installed via .csproj):**
>
> | Package | What It Does |
> |---------|-------------|
> | **Pomelo.EntityFrameworkCore.MySql** | Connects EF Core to MySQL — the bridge between C# and the database |
> | **Microsoft.AspNetCore.Authentication.JwtBearer** | Handles JWT token generation and validation for the session cookie |
> | **BCrypt.Net-Next** | Hashes passwords before storing and verifies them on login |
> | **iText7** | Generates the Purchase Order PDF file that can be downloaded |
> | **Microsoft.EntityFrameworkCore** | The ORM that turns C# LINQ queries into SQL |
>
> **Frontend CDN Libraries (loaded in the browser):**
>
> | Library | What It Does |
> |---------|-------------|
> | **Bootstrap 5** | Responsive UI layout, buttons, tables, modals |
> | **Bootstrap Icons** (CDN) | All the icons throughout the system (bell, cart, file, etc.) |
> | **Chart.js 4.4.3** (CDN) | The budget utilization bar charts and doughnut charts on the Dashboard |
> | **jQuery** | AJAX calls for the notification bell — fetches unread count without page reload |
>
> **Most important one to explain:** The Google reCAPTCHA API — when you submit the login form, the browser sends the reCAPTCHA token to my `AuthController`, which then calls Google's API to verify it. If Google says it's invalid, login is blocked. On localhost I bypass this because Google doesn't allow localhost as a registered domain."

### Q: "What's the ERP approach of your system?"
> "ProVMS follows a **modular ERP approach** — it integrates multiple business functions into a single unified system instead of having separate disconnected tools. Specifically, it covers four core ERP domains:
>
> - **Procurement Management** — the full purchase cycle from requisition to delivery
> - **Financial Management** — budget allocation, encumbrance, approval workflow, and audit trail
> - **Supplier/Vendor Management** — vendor onboarding, accreditation, evaluation, and contracts
> - **Inventory & Catalog Management** — vendor items, pricing, and the internal marketplace
>
> The ERP nature comes from the **shared database** — all modules read and write to the same `ProVMSDB`. A Purchase Requisition in the Procurement module directly affects the Budget record in the Finance module. A Vendor record in the Vendor module is the same record shown in the Marketplace and the Evaluation module. There is no data silos — everything is connected through shared entities and foreign key relationships.
>
> The system also enforces **business process integration** — a Finance user cannot skip the Procurement step, and a Procurement officer cannot bypass Finance approval. Each module has defined role gates that enforce the correct business flow end-to-end."

### Q: "How is the system deployed?"
> "Locally I run it with `dotnet run` using Kestrel. For production I published it using `dotnet publish -c Release`, zipped the output, and uploaded it to MonsterASP.net which runs it under IIS. The production database is on databaseasp.net. Environment-specific settings like the connection string are stored in `appsettings.Production.json`."

---

## SECTION 12: KEY NUMBERS TO REMEMBER

| Fact | Value |
|------|-------|
| Failed login attempts before lockout | **5** |
| Lockout duration | **15 minutes** |
| Vendor accreditation SLA | **48 hours** |
| Finance approval SLA | **24 hours** |
| Vendor dispatch SLA | **72 hours** |
| Max file upload size | **5 MB** |
| Allowed file type | **PDF only** |
| Department budgets seeded | **6** (IT, HR, FINANCE, OPS, PROCUREMENT, SYS) |
| User roles | **5** (Admin, Finance, Procurement, User, Vendor) |
| BCrypt cost factor | **11** |
| JWT token expiry | **480 minutes (8 hours)** |

---

## SECTION 13: ONE-LINE DESCRIPTIONS OF EACH MODULE

| Module | One-Line Description |
|--------|---------------------|
| **Login / Auth** | BCrypt + cookie session with lockout, reCAPTCHA, and vendor accreditation gate |
| **Dashboard** | Analytics overview — budget utilization charts, SLA logs, audit trail |
| **Vendor Onboarding** | Public self-registration wizard with document upload |
| **Accreditation Desk** | Admin/Procurement reviews and approves/rejects vendor applications |
| **Vendor Directory** | Searchable list of all vendors with status filter |
| **Marketplace** | Internal users browse vendor catalog and submit purchase requests |
| **Approval Workflow** | Finance approves or rejects purchase requisitions with budget enforcement |
| **PO Vault** | All purchase orders — Finance/Procurement/Admin read, Procurement issues |
| **Delivery Tracking** | Tracks cargo from PO issuance to confirmed delivery |
| **Evaluation** | Users rate vendors after delivery — feeds performance leaderboard |
| **Contract Management** | Vendor contracts with lifecycle status (Draft → Active → Expired) |
| **Budget Management** | Finance allocates department budgets per fiscal year |
| **User Management** | Admin creates/archives internal staff accounts |
| **Notifications** | Real-time bell hub with unread count for all workflow events |

---

*ProVMS v1.1 — Defense Study Guide | For internal use only*
