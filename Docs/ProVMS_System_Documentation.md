# ProVMS — Procurement & Vendor Management System
## Complete System Documentation & Manual Testing Guide

**Version:** 1.0 | **Stack:** ASP.NET Core 9.0 MVC + MySQL | **Environment:** `http://localhost:5239`

---

## TABLE OF CONTENTS

1. [Executive Summary](#1-executive-summary)
2. [System Architecture](#2-system-architecture)
3. [User Roles & Access Matrix](#3-user-roles--access-matrix)
4. [Module Reference Guide](#4-module-reference-guide)
5. [End-to-End Transaction Walkthrough](#5-end-to-end-transaction-walkthrough)
6. [Manual Testing Scripts](#6-manual-testing-scripts)
7. [Database Schema Reference](#7-database-schema-reference)
8. [Security & Authentication Model](#8-security--authentication-model)
9. [SLA & Business Rules](#9-sla--business-rules)
10. [Known Credentials & Setup](#10-known-credentials--setup)

---

## 1. EXECUTIVE SUMMARY

### Strategic Vision
ProVMS eliminates administrative drag from procurement operations by digitizing the full supplier lifecycle — from vendor onboarding to purchase order fulfillment — inside a single, role-governed web application.

### Core Value Propositions

| Mandate | Implementation |
|---------|----------------|
| Zero Operations Bureaucracy | Fully digital workflow — no paper, no email chains |
| Supplier Accountability | 5-star evaluation system + performance leaderboard |
| Strict Security Posture | Cookie-based JWT auth + ASP.NET Core RBAC policies |
| Budget Governance | Pre-encumbrance locking + hard budget ceiling enforcement |
| Operational Speed | In-app notification bell hub + real-time status transitions |

### Financial Controls (CFO Directives)
- **Pre-Encumbrance:** Budget is locked the moment a PR is submitted — preventing double-spend
- **Hard Ceiling:** Orders exceeding department budget by even ₱1.00 are rejected (`HTTP 400`)
- **Audit Trail:** Every workflow state change is timestamped and user-attributed in the database

### Operational SLAs (COO Directives)
| Process | Responsible Role | SLA Window |
|---------|-----------------|------------|
| Vendor Accreditation Review | Admin | 48 hours from wizard submission |
| Finance Approval/Rejection | Finance | 24 hours from PR entry |
| Vendor Dispatch Update | Vendor | 72 hours from PO issuance |

---

## 2. SYSTEM ARCHITECTURE

```
┌─────────────────────────────────────────────────────────┐
│                    BROWSER CLIENT                       │
│  Bootstrap 5 UI + Chart.js + In-App Notification Bell   │
└──────────────────────────┬──────────────────────────────┘
                           │ HTTP (localhost:5239)
┌──────────────────────────▼──────────────────────────────┐
│              ASP.NET Core 9.0 MVC (Kestrel)             │
│                                                         │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ Controllers │  │ Razor Views  │  │  Middleware    │  │
│  │ (RBAC Gate) │  │ (_Layout.cs) │  │  (Cookie Auth) │  │
│  └─────────────┘  └──────────────┘  └───────────────┘  │
└──────────────────────────┬──────────────────────────────┘
                           │ Entity Framework Core (Pomelo)
┌──────────────────────────▼──────────────────────────────┐
│                  MySQL — ProVMSDB                        │
│  Users │ Vendors │ VendorItems │ PurchaseRequisitions    │
│  SupplierEvaluations │ InAppNotifications │ Contracts    │
│  DepartmentBudgets │ SLATracking                        │
└─────────────────────────────────────────────────────────┘
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Web Framework | ASP.NET Core 9.0 MVC |
| ORM | Pomelo.EntityFrameworkCore.MySql |
| Authentication | Cookie-based JWT (BCrypt password hashing) |
| Database | MySQL — `ProVMSDB` |
| Frontend | Bootstrap 5, Bootstrap Icons, Inter font |
| Charts | Chart.js 4.4.3 |
| PDF Generation | iText7 |
| Hosting | Kestrel — `http://localhost:5239` |

---

## 3. USER ROLES & ACCESS MATRIX

### Role Definitions

| Role | Description | User Example |
|------|-------------|--------------|
| **Admin** | Full system access, user provisioning, vendor accreditation | Mark |
| **Finance** | Budget approval/rejection, financial ledger review | Priya |
| **Procurement** | PO issuance, vendor directory, delivery tracking | Ben |
| **User** | Marketplace browsing, PR submission, delivery confirmation | Juan |
| **Vendor** | Onboarding wizard, catalog management, PO receipt | Emma |

### Sidebar Module Access Matrix

| Module | Admin | Finance | Procurement | User | Vendor |
|--------|-------|---------|-------------|------|--------|
| Dashboard | ✅ | ✅ | ✅ | ❌ | ❌ |
| Vendor Accreditation Desk | ✅ | ❌ | ✅ | ❌ | ❌ |
| Vendor Directory | ✅ | ✅ | ✅ | ❌ | ❌ |
| Vendor Profile (own) | ❌ | ❌ | ❌ | ❌ | ✅ |
| Marketplace | ❌ | ❌ | ❌ | ✅ | ❌ |
| My Requests | ❌ | ❌ | ❌ | ✅ | ❌ |
| Approval Workflow | ✅ | ✅ | ❌ | ❌ | ❌ |
| PO Vault (full) | ✅ | 📖 Read | ✅ | ❌ | ❌ |
| PO Vault (own orders) | ❌ | ❌ | ❌ | ✅ | ✅ |
| Delivery Tracking | ✅ | ❌ | ✅ | ✅ | ✅ |
| Performance Evaluation | ✅ | ✅ | ✅ | ❌ | ❌ |
| Leaderboard | ✅ | ✅ | ✅ | ❌ | ❌ |
| Contract Management | ✅ | ✅ | ✅ | ❌ | ❌ |
| Procurement Analytics | ✅ | ✅ | ✅ | ❌ | ❌ |
| User Management | ✅ | ❌ | ❌ | ❌ | ❌ |

### Registration & Provisioning Rules

| Path | Method | Result |
|------|--------|--------|
| Public `/Vendor/Onboarding` | Self-service wizard | Creates Vendor account → `PendingVerification` |
| Admin `/UserManagement/Create` | Admin-only form | Provisions internal users (Admin/Finance/Procurement/User) |
| `/Auth/Register` GET | Blocked | Redirects to Vendor Onboarding (HTTP 301) |
| `/Auth/Register` POST | Blocked | Returns HTTP 403 Forbidden hardcoded |

---

## 4. MODULE REFERENCE GUIDE

### MODULE 1: Dashboard (Command Center)
**URL:** `/Dashboard/Index`
**Access:** Admin, Finance, Procurement

**What it shows:**
- KPI stat cards: Active Vendors, Awaiting Review, Pending Approvals, Total Requisitions
- Department Expenditure chart (bar/doughnut)
- Budget Allocation vs Remaining chart
- Vendor Performance Scores chart
- Quick Actions panel (role-filtered links)

**Key fields:**
| Field | Source |
|-------|--------|
| Active Vendors | `Vendors.OperationalStatus = 'Active'` |
| Awaiting Review | `Vendors.OperationalStatus = 'PendingVerification'` |
| Pending Approvals | `PurchaseRequisitions.WorkflowStatus = 'Pending_Finance'` |
| Total Requisitions | COUNT of all PRs |

---

### MODULE 2: Vendor Accreditation

#### 2A. Vendor Onboarding Wizard (Public)
**URL:** `/Vendor/Onboarding`
**Access:** Public (unauthenticated)

**Wizard Steps:**
1. **Company Profile** — CompanyName (max 150 chars), TaxID (unique numeric), ContactEmail
2. **Document Upload** — PDF only, max 5MB, drag-and-drop dropzone
3. **Catalog Builder** — Live editable grid: ItemName, Category (dropdown), UnitPrice (₱ decimal)

**Validation Rules:**
| Field | Rule |
|-------|------|
| CompanyName | Required, alphanumeric, max 150 chars |
| TaxID | Required, unique, numeric format |
| Document | PDF only, max 5MB |
| UnitPrice | Decimal ≥ 0, numeric-only input (blocks alphabetic characters) |
| ItemName | Required, max 100 chars |
| Category | Must be: IT Hardware / Office Facilities / Marketing Collateral |

**On Submit:** Status set to `PendingVerification`, Admin notified via bell hub.

#### 2B. Accreditation Desk (Admin/Procurement)
**URL:** `/Vendor/AccreditationDesk`
**Access:** Admin, Procurement

**Actions per vendor row:**
- **✅ Approve** → Sets `OperationalStatus = Active`, vendor catalog goes live
- **❌ Reject** → Sets `OperationalStatus = Suspended`
- **View Document** → Inline PDF preview pane

**SLA indicator:** Time since `SubmittedAt` stamp displayed per row. Flag if > 48h.

#### 2C. Vendor Directory
**URL:** `/Vendor/Directory`
**Access:** Admin, Finance, Procurement

**Features:** Searchable/filterable table of all vendors with status badges, contact info, item count.

---

### MODULE 3: Purchase Requisition

#### 3A. Marketplace
**URL:** `/Catalog/Marketplace`
**Access:** User (Requester) only

**Flow:**
1. Search/browse active catalog items from verified vendors
2. Select item → unit price auto-populates (locked/immutable)
3. Enter Quantity → system computes `Total = Quantity × UnitPrice`
4. Submit → system checks department budget:
   - ✅ Budget available → PR created, budget pre-encumbered, Finance notified
   - ❌ Budget exceeded → HTTP 400 returned, user shown `Error: Department Budget Exceeded`

#### 3B. My Requests
**URL:** `/Catalog/MyRequests`
**Access:** User (own PRs only)

Shows the submitting user's own requisitions with current workflow status.

#### 3C. Approval Workflow
**URL:** `/Catalog/ApprovalWorkflow`
**Access:** Finance, Admin

**Actions:**
- **Approve** → Status: `Approved_Budget`, Procurement notified
- **Reject** → Status: `Archived`, budget encumbrance released, requester notified

**SLA:** Finance must act within 24h of PR creation.

---

### MODULE 4: PO Vault
**URL:** `/Catalog/POVault`
**Access:** Admin (full), Procurement (full), Finance (read-only), User/Vendor (own records)

**Workflow Actions:**

| Current Status | Available Action | Result |
|----------------|-----------------|--------|
| `Approved_Budget` | Issue PO (Procurement) | → `PO_Issued`, Vendor notified, SLA 72h timer starts |
| `PO_Issued` | Mark Delivered (after Vendor dispatches) | → `In_Transit` |
| `In_Transit` | Confirm Delivery (User) | → `Delivered` |
| `Delivered` | Submit Evaluation (User) | → `Archived`, evaluation recorded |

**SLA Column:** Shows elapsed hours since `POIssuedAt`:
- 🟢 `sla-ok` — under 48h
- 🟡 `sla-warn` — 48–72h
- 🔴 `sla-breach` (pulsing) — over 72h

**PDF Download:** Every PO row has a Download button generating an iText7 PDF.

---

### MODULE 5: Supplier Evaluation

#### 5A. Performance Evaluation
**URL:** `/Evaluation/Performance`
**Access:** Admin, Finance, Procurement

Grid of all evaluated transactions with star scores per dimension.

#### 5B. Scoring & Rating (Leaderboard)
**URL:** `/Evaluation/Leaderboard`
**Access:** Admin, Finance, Procurement

**Top 3 podium cards** (1st/2nd/3rd place vendors by overall score).

**Full rankings table columns:**
- Rank | Vendor | Evaluations count | Delivery ⭐ | Condition ⭐ | Communication ⭐ | Overall (progress bar) | Flag

**Score dimensions:**
| Dimension | Description |
|-----------|-------------|
| Delivery Speed | Was the PO dispatched within 72h SLA? |
| Item Condition | Were goods received in acceptable condition? |
| Communication | Was vendor responsive during the process? |

**Flag logic:**
- 🔴 `Low` badge → overall average < 2.5
- 🟢 `Top` badge → overall average ≥ 4.5

#### 5C. Evaluation Submission
**URL:** `/Evaluation/Submit`
**Access:** User (after Delivered status)

3-axis star rating widget (1–5 stars each) + optional comments text area.

---

### MODULE 6: Contract Management

#### 6A. Contract Lifecycle
**URL:** `/Contract/Lifecycle`
**Access:** Admin, Finance, Procurement

Manage contract records with status badges: `Active`, `Expiring`, `Expired`, `Draft`, `Under Negotiation`.

#### 6B. Negotiations
**URL:** `/Contract/Negotiations`
**Access:** Admin, Finance, Procurement

Active negotiation threads and draft workspace.

#### 6C. Pricing Board
**URL:** `/Contract/Pricing`
**Access:** Admin, Finance, Procurement

Corporate pricing alignment matrix per vendor contract.

---

### MODULE 7: Procurement Analytics

#### 7A. Reports & Insights
**URL:** `/Dashboard/Reports`
**Access:** Admin, Finance, Procurement

KPI summary + Chart.js visualizations of spend trends.

#### 7B. Budget Management
**URL:** `/Dashboard/BudgetManagement`
**Access:** Admin, Finance

Department budget ledger: allocated vs. spent vs. remaining per fiscal year.

#### 7C. Benchmarking
**URL:** `/Evaluation/Benchmarking`
**Access:** Admin, Finance, Procurement

Radar/spider chart comparing vendor performance across all 3 evaluation dimensions.

---

### MODULE 8: User Management
**URL:** `/UserManagement/Index`
**Access:** Admin only

**Features:**
- View all internal users with role badges
- Provision new user (`/UserManagement/Create`) — assigns Admin/Finance/Procurement/User role
- Update role inline via dropdown + Save
- Deactivate user (remove from system)

**Restriction:** Cannot assign Vendor role through this interface. Vendors self-register only.

---

## 5. END-TO-END TRANSACTION WALKTHROUGH

### Scenario: "Corporate Infrastructure Expansion"
**Goal:** Source 50 Professional Workstations at ₱25,000 each = **₱1,250,000 total**

---

**PHASE 1: Vendor Onboarding**

```
Step 1 — Emma (Vendor) visits: http://localhost:5239/Vendor/Onboarding
  └── Wizard Step 1: CompanyName = "TechCorp Solutions", TaxID = "123456789", Email = "emma@techcorp.com"
  └── Wizard Step 2: Uploads tax_certificate.pdf (< 5MB)
  └── Wizard Step 3: Adds item row:
        ItemName = "ProWorkstation V1"
        Category = "IT Hardware"
        UnitPrice = ₱25,000.00
  └── Clicks Submit
  └── Result: Vendor status = PendingVerification, Admin bell badge shows 🔔1
```

**PHASE 2: Admin Accreditation**

```
Step 2 — Mark (Admin) logs in: admin@provms.com / Admin@ProVMS2026!
  └── Sees 🔔1 notification → clicks bell → "New vendor registration: TechCorp Solutions"
  └── Navigates to: /Vendor/AccreditationDesk
  └── Reviews inline PDF document viewer
  └── Clicks ✅ Approve
  └── Result: TechCorp Solutions → OperationalStatus = Active
              Vendor catalog now visible in Marketplace
              Emma receives notification: "Your accreditation has been approved"
```

**PHASE 3: Purchase Requisition**

```
Step 3 — Juan (User) logs in
  └── Navigates to: /Catalog/Marketplace
  └── Searches: "Hardware" → selects "ProWorkstation V1" by TechCorp Solutions
  └── Unit price auto-populates: ₱25,000 (immutable)
  └── Enters Quantity: 50
  └── System computes: 50 × ₱25,000 = ₱1,250,000
  └── Clicks Submit
  └── Backend checks: IT Dept budget ≥ ₱1,250,000? → YES
  └── Result: Budget pre-encumbered ₱1,250,000
              PR created: WorkflowStatus = Pending_Finance
              Priya (Finance) receives 🔔 notification
```

**PHASE 4: Finance Approval**

```
Step 4 — Priya (Finance) logs in
  └── Sees 🔔 notification: "PR #1042 requires budget clearance"
  └── Navigates to: /Catalog/ApprovalWorkflow
  └── Reviews: Requester = Juan, Item = ProWorkstation V1, Total = ₱1,250,000
  └── Clicks ✅ Approve Expenditure
  └── Result: WorkflowStatus → Approved_Budget
              Ben (Procurement) receives 🔔 notification
              Timestamped audit entry created
```

**PHASE 5: PO Issuance**

```
Step 5 — Ben (Procurement) logs in
  └── Sees 🔔 notification: "PR #1042 budget cleared — ready for PO"
  └── Navigates to: /Catalog/POVault
  └── Confirms: TechCorp = Active ✅, Budget = Cleared ✅
  └── Clicks 📄 Issue PO
  └── Result: WorkflowStatus → PO_Issued
              POIssuedAt timestamp recorded (72h SLA starts)
              Emma (Vendor) receives 🔔 notification
              PO PDF downloadable
```

**PHASE 6: Vendor Dispatch**

```
Step 6 — Emma (Vendor) logs in
  └── Sees 🔔 notification: "New PO received — PO-001042"
  └── Navigates to: /Catalog/DeliveryTracking
  └── Loads 50 workstations, clicks 🚚 Mark as Dispatched
  └── Result: WorkflowStatus → In_Transit
              Juan receives 🔔 notification: "Your order is in transit"
              SLA timer shows elapsed hours
```

**PHASE 7: Delivery Confirmation & Evaluation**

```
Step 7 — Juan (User) logs in
  └── Sees 🔔 notification: "PO-001042 is in transit"
  └── Receives delivery, navigates to: /Catalog/MyRequests
  └── Clicks ✅ Confirm Receipt
  └── Result: WorkflowStatus → Delivered

Step 8 — Juan submits evaluation
  └── Navigates to: /Evaluation/Submit
  └── Rates:
        Delivery Speed:   ⭐⭐⭐⭐⭐ (5)
        Item Condition:   ⭐⭐⭐⭐⭐ (5)
        Communication:    ⭐⭐⭐⭐⭐ (5)
        Comment: "Workstations delivered early, fantastic transaction lifecycle response time."
  └── Clicks Submit
  └── Result: WorkflowStatus → Archived
              TechCorp OverallAverage updated in leaderboard
              Dashboard charts refresh with ₱1,250,000 spend entry
```

---

## 6. MANUAL TESTING SCRIPTS

### TEST SUITE 1: Authentication & Access Control

---

#### TC-001: Seed Admin Login
**Precondition:** App running at `http://localhost:5239`
**Steps:**
1. Navigate to `http://localhost:5239/Auth/Login`
2. Enter Email: `admin@provms.com`
3. Enter Password: `Admin@ProVMS2026!`
4. Click Sign In

**Expected Result:** Redirected to `/Dashboard/Index`. Sidebar shows all 7 modules including User Management.

---

#### TC-002: Cross-Role Security Boundary — Finance Endpoint as User
**Precondition:** Logged in as a User-role account
**Steps:**
1. Manually navigate to `/Catalog/ApprovalWorkflow`

**Expected Result:** Redirected to `/Auth/AccessDenied` (HTTP 403). No data visible.

---

#### TC-003: Vendor Cannot Access Internal Modules
**Precondition:** Logged in as a Vendor account
**Steps:**
1. Manually navigate to `/Dashboard/Index`
2. Manually navigate to `/UserManagement/Index`
3. Manually navigate to `/Vendor/AccreditationDesk`

**Expected Result:** All three redirected to `/Auth/AccessDenied`. Vendor sidebar only shows Vendor-specific links.

---

#### TC-004: Self-Registration Blocked for Internal Roles
**Steps:**
1. Navigate to `/Auth/Register` (GET)

**Expected Result:** HTTP 301 redirect to `/Vendor/Onboarding`. No internal registration form accessible.

2. POST to `/Auth/Register`

**Expected Result:** HTTP 403 Forbidden. No database record created.

---

#### TC-005: Admin Cannot Assign Vendor Role via User Management
**Precondition:** Logged in as Admin
**Steps:**
1. Navigate to `/UserManagement/Create`
2. Observe the Role dropdown options

**Expected Result:** Dropdown contains only: Admin, Finance, Procurement, User. "Vendor" is NOT listed.

---

### TEST SUITE 2: Vendor Onboarding Wizard

---

#### TC-006: Successful Wizard Completion
**Steps:**
1. Navigate to `/Vendor/Onboarding` (logged out)
2. Step 1: Fill CompanyName = "Alpha Tech Ltd", TaxID = "987654321", Email = "alpha@techltd.com"
3. Step 2: Upload a valid `.pdf` file under 5MB
4. Step 3: Add item — Name: "Laptop Pro", Category: "IT Hardware", Price: ₱15,000
5. Click Submit

**Expected Result:**
- Vendor record created with `OperationalStatus = PendingVerification`
- Admin receives in-app notification (🔔 badge increments)
- Redirect to login/confirmation page

---

#### TC-007: UnitPrice Field Rejects Non-Numeric Input
**Steps:**
1. Navigate to Wizard Step 3 (Catalog Builder)
2. In UnitPrice field, type: `TEN THOUSAND PESOS`

**Expected Result:**
- Characters are blocked from rendering OR field shows red error border
- Submit button disabled / validation error displayed
- No form submission possible until corrected

---

#### TC-008: PDF-Only Document Upload Enforcement
**Steps:**
1. Navigate to Wizard Step 2
2. Attempt to upload a `.jpg` or `.docx` file

**Expected Result:** Upload rejected. Error message: file type not accepted. Only `.pdf` allowed.

---

#### TC-009: Duplicate TaxID Rejection
**Steps:**
1. Complete onboarding with TaxID = "123456789"
2. Attempt second onboarding with same TaxID = "123456789"

**Expected Result:** System rejects with duplicate key error. Second vendor not created.

---

### TEST SUITE 3: Budget Enforcement

---

#### TC-010: Hard Budget Ceiling — Over-Budget Requisition
**Precondition:** Department budget set to ₱500,000. Item price = ₱600,000.
**Steps:**
1. Login as User role
2. Navigate to `/Catalog/Marketplace`
3. Select item, enter quantity resulting in total > ₱500,000
4. Click Submit

**Expected Result:**
- HTTP 400 returned
- User sees error: `Error: Department Budget Exceeded`
- No PR record created in database
- Budget NOT encumbered

---

#### TC-011: Successful Budget Pre-Encumbrance
**Precondition:** Department budget = ₱1,500,000. Order = ₱1,250,000.
**Steps:**
1. Login as User, submit PR for ₱1,250,000
2. Login as Admin, check Budget Management

**Expected Result:**
- PR created with `WorkflowStatus = Pending_Finance`
- `DepartmentBudgets.SpentAmount` increases by ₱1,250,000 (encumbered)
- Available balance shows ₱250,000 remaining

---

#### TC-012: Concurrent Budget Overrun Prevention
**Steps:**
1. Open two browser windows/sessions, both logged in as different User accounts
2. Both users attempt to submit PRs of ₱1,000,000 each against a ₱1,500,000 budget simultaneously

**Expected Result:**
- First transaction locks budget → `SpentAmount` = ₱1,000,000, remaining = ₱500,000
- Second transaction encounters budget validation block → rejected with HTTP 400
- Database `SpentAmount` remains at ₱1,000,000 (no double-spend)

---

#### TC-013: Budget Released on Finance Rejection
**Precondition:** PR submitted and budget pre-encumbered.
**Steps:**
1. Login as Finance
2. Navigate to `/Catalog/ApprovalWorkflow`
3. Click ❌ Reject on the PR

**Expected Result:**
- `WorkflowStatus → Archived`
- `DepartmentBudgets.SpentAmount` decremented back (encumbrance released)
- Requester receives notification of rejection

---

### TEST SUITE 4: Purchase Order Workflow

---

#### TC-014: Full Workflow State Progression
**Steps:** Follow the complete Phase 1–7 walkthrough from Section 5.

**Expected Status Progression:**
```
[Submitted] → Pending_Finance
[Finance Approved] → Approved_Budget
[PO Issued] → PO_Issued
[Vendor Dispatches] → In_Transit
[User Confirms] → Delivered
[Evaluation Submitted] → Archived
```

Each transition must be verified in the PO Vault table.

---

#### TC-015: PO PDF Download
**Precondition:** At least one PO in `PO_Issued` or later status.
**Steps:**
1. Navigate to `/Catalog/POVault`
2. Click the PDF download button (red outline icon) on any row

**Expected Result:** PDF file downloads. Contains: PO Number, Vendor name, Item, Quantity, Total, Date, signatures.

---

#### TC-016: SLA Timer Display
**Precondition:** PO in `PO_Issued` status.
**Steps:**
1. Navigate to `/Catalog/POVault`
2. Observe the "Vendor SLA (72h)" column

**Expected Result:**
- If < 48h elapsed: green `sla-ok` badge showing `Xh / 72h`
- If 48–72h elapsed: yellow `sla-warn` badge
- If > 72h elapsed: red pulsing `sla-breach` badge showing `BREACH Xh`

---

### TEST SUITE 5: Notifications

---

#### TC-017: Notification Bell Badge Count
**Steps:**
1. Login as Admin
2. Have another session complete a vendor registration
3. Observe the 🔔 bell icon in the top bar

**Expected Result:** Badge shows unread count (e.g., `🔔 1`). Count is accurate.

---

#### TC-018: Notification Deep-Link Navigation
**Steps:**
1. Click the bell icon to open notification dropdown
2. Click on a notification item

**Expected Result:** User is taken directly to the relevant transaction record — no intermediate menu navigation required.

---

#### TC-019: Mark All Read
**Steps:**
1. With unread notifications present, open the bell dropdown
2. Click "Mark all read"

**Expected Result:** All notification items marked read. Badge count drops to 0 and badge disappears.

---

### TEST SUITE 6: Supplier Evaluation & Leaderboard

---

#### TC-020: 3-Dimension Star Rating Submission
**Precondition:** At least one PR in `Delivered` status for the logged-in User.
**Steps:**
1. Login as User, navigate to `/Evaluation/Submit`
2. Rate Delivery Speed: 4 stars, Item Condition: 5 stars, Communication: 3 stars
3. Add comment: "Good delivery, minor packaging issue."
4. Submit

**Expected Result:**
- Evaluation saved
- PR status → `Archived`
- Vendor's `OverallAverage` recalculated: `(4+5+3)/3 = 4.0`
- Leaderboard updates

---

#### TC-021: Leaderboard Ranking Order
**Precondition:** Multiple vendors evaluated.
**Steps:**
1. Navigate to `/Evaluation/Leaderboard`

**Expected Result:**
- Vendors ranked highest overall score first
- Top 3 shown as podium stat cards (1st/2nd/3rd)
- Vendors with average < 2.5 highlighted in red (`table-danger` row)
- `Top` green badge for ≥ 4.5 average

---

### TEST SUITE 7: User Management

---

#### TC-022: Provision New Internal User
**Precondition:** Logged in as Admin.
**Steps:**
1. Navigate to `/UserManagement/Create`
2. Fill: FullName = "Sarah Lee", Email = "sarah@provms.com", Role = Finance, DepartmentCode = "FIN-01", Password = "Finance@2026!"
3. Click Provision

**Expected Result:** New user appears in User Management table with Finance role badge. Sarah can login and access Finance-gated modules.

---

#### TC-023: Role Update
**Steps:**
1. Navigate to `/UserManagement/Index`
2. Find a User-role account, change dropdown to "Procurement", click Save

**Expected Result:** Role updated in database. User's sidebar now shows Procurement-specific modules on next login.

---

## 7. DATABASE SCHEMA REFERENCE

### Core Tables

```sql
-- Users
Users (ID, FullName, Email, PasswordHash, UserRole, DepartmentCode, CreatedAt)
UserRole: ENUM('Admin', 'Procurement', 'Finance', 'User', 'Vendor')

-- Vendors
Vendors (ID, UserID_FK, CompanyName, TaxID, ContactEmail, DocumentVaultURL,
         OperationalStatus, SubmittedAt, ApprovedAt, UpdatedAt)
OperationalStatus: ENUM('PendingVerification', 'Active', 'Suspended', 'Blacklisted')

-- Vendor Items (Catalog)
VendorItems (ID, VendorID_FK, ItemName, Category, UnitPrice)
Category: ENUM('IT Hardware', 'Office Facilities', 'Marketing Collateral')

-- Purchase Requisitions
PurchaseRequisitions (ID, RequesterID_FK, ItemID_FK, Quantity,
                      TotalCalculatedAmount, WorkflowStatus, IsEncumbered,
                      CreatedAt, FinanceSubmittedAt, ApprovedAt, POIssuedAt)
WorkflowStatus: ENUM('Pending_Finance','Approved_Budget','PO_Issued',
                     'In_Transit','Delivered','Archived')

-- Supplier Evaluations
SupplierEvaluations (ID, RequisitionID_FK, VendorID_FK,
                     DeliverySpeedStars, ItemConditionStars, CommunicationStars,
                     Comments, CreatedDate)
Stars: INT CHECK (1-5)

-- Notifications
InAppNotifications (ID, TargetUserID_FK, NotificationText, IsRead, CreatedAt)

-- Department Budgets
DepartmentBudgets (ID, DepartmentCode, DepartmentName, FiscalYear,
                   AllocatedBudget, SpentAmount, UpdatedAt)

-- Contracts
Contracts (ID, VendorID_FK, Title, Status, StartDate, EndDate,
           TotalValue, CreatedAt)
Status: ENUM('Draft','Active','Expiring','Expired','Under Negotiation')
```

### Key Relationships
```
Users ──< PurchaseRequisitions (RequesterID)
Users ──< InAppNotifications (TargetUserID)
Vendors ──< VendorItems (VendorID)
VendorItems ──< PurchaseRequisitions (ItemID)
PurchaseRequisitions ──< SupplierEvaluations (RequisitionID)
Vendors ──< Contracts (VendorID)
```

---

## 8. SECURITY & AUTHENTICATION MODEL

### Authentication Flow
```
[User submits login form]
         │
         ▼
[BCrypt.Verify(inputPassword, storedHash)]
         │
    ✅ Match
         │
         ▼
[Generate Claims Identity]
  • ClaimTypes.NameIdentifier = UserID
  • ClaimTypes.Name = Email
  • ClaimTypes.Role = UserRole
         │
         ▼
[Issue Encrypted Cookie — HttpOnly, SameSite=Strict]
         │
         ▼
[All subsequent requests authenticated via cookie middleware]
```

### RBAC Policy Map

| Policy Name | Allowed Roles | Used On |
|-------------|--------------|---------|
| `AdminOnly` | Admin | User Management |
| `ProcurementOrAdmin` | Procurement, Admin | Accreditation Desk, PO Vault write |
| `FinanceOnly` | Finance | Approval Workflow |
| `FinanceOrAdmin` | Finance, Admin | Approval Workflow GET |
| `InternalUsers` | Admin, Finance, Procurement, User | General internal access |
| `VendorOnly` | Vendor | Vendor Profile, Catalog |
| `AllAuthenticated` | All roles | Notifications, shared views |
| `AnalyticsViewers` | Admin, Finance, Procurement | Dashboard, Reports |
| `POVaultViewers` | Admin, Finance, Procurement, User, Vendor | PO Vault (row-filtered) |
| `DirectoryViewers` | Admin, Finance, Procurement | Vendor Directory |
| `RequesterOnly` | User | Marketplace, Place Requisition |

### Password Policy
- Hashed with BCrypt (cost factor 11)
- Never stored in plaintext
- Seed admin password reset on every startup from `appsettings.json` hash

---

## 9. SLA & BUSINESS RULES

### SLA Summary Table

| Process | Actor | SLA | System Enforcement |
|---------|-------|-----|--------------------|
| Vendor accreditation review | Admin | 48h from `SubmittedAt` | Highlighted in Accreditation Desk |
| Finance approval/rejection | Finance | 24h from `CreatedAt` | Highlighted in Approval Workflow |
| Vendor dispatch after PO | Vendor | 72h from `POIssuedAt` | SLA badge in PO Vault (ok/warn/breach) |

### Workflow Immutability Rules
- Once a PR reaches `Approved_Budget`, Finance cannot unilaterally revert it
- Once `PO_Issued`, only Procurement can action the next step
- `Archived` is a terminal state — no further transitions possible
- Contract pricing records in `Approved` state: no delete permissions for any role including Admin

### Budget Pre-Encumbrance Logic
```
OnPRSubmit:
  availableBudget = AllocatedBudget - SpentAmount
  if (TotalCalculatedAmount > availableBudget):
      return HTTP 400 "Department Budget Exceeded"
  else:
      SpentAmount += TotalCalculatedAmount
      PR.IsEncumbered = true
      PR.WorkflowStatus = Pending_Finance

OnFinanceReject:
  SpentAmount -= PR.TotalCalculatedAmount
  PR.IsEncumbered = false
  PR.WorkflowStatus = Archived
```

---

## 10. KNOWN CREDENTIALS & SETUP

### Seed Admin Account
| Field | Value |
|-------|-------|
| Email | `admin@provms.com` |
| Password | `Admin@ProVMS2026!` |
| Role | Admin |
| Note | Password auto-reset on every app startup |

### Application URL
```
http://localhost:5239
```

### Database Connection
```
Server: localhost
Database: ProVMSDB
User: root
Password: (none)
```

### Starting the Application
```powershell
# Navigate to project folder
cd c:\Users\hp\ProVMSIT15

# Build and run
dotnet run

# Or run without rebuild
dotnet run --no-build
```

### Applied Database Migrations
1. `InitialCreate` — Core schema (Users, Vendors, VendorItems, PRs, Evaluations, Notifications)
2. `AddContracts` — Contract management tables
3. `AddSLAAndEncumbrance` — SLA tracking + `IsEncumbered`, `POIssuedAt`, `FinanceSubmittedAt`, `ApprovedAt` fields

---

## APPENDIX: WORKFLOW STATUS REFERENCE

| Status | Label | Description |
|--------|-------|-------------|
| `Pending_Finance` | 🔵 Awaiting | PR submitted, awaiting Finance approval |
| `Approved_Budget` | 🟢 Budget Cleared | Finance approved, ready for PO issuance |
| `PO_Issued` | 🟣 PO Active | Purchase Order issued to vendor, SLA running |
| `In_Transit` | 🟡 In Transit | Vendor dispatched goods |
| `Delivered` | 🟢 Delivered | User confirmed receipt |
| `Archived` | ⚫ Complete | Evaluation submitted, record closed |

---

*Document generated for ProVMS v1.0 — ASP.NET Core 9.0 MVC*
*For internal use only — Manual Testing & QA Reference*
