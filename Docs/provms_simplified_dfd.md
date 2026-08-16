# ProVMS (Procurement & Vendor Management System) — Simplified Data Flow Diagram (DFD)

This document provides a high-level, easy-to-understand summary of the system's data flows. Unlike a complex Level 1 DFD, this diagram groups processes into the three main system modules and uses a single central database store to show how information moves through ProVMS.

---

## 1. DFD Components

### External Entities (The Users)
*   **Supplier (Vendor):** Submits onboarding info, receives orders, and updates shipping status.
*   **System Admin:** Verifies supplier credentials and manages accounts.
*   **Internal User (Requester):** Submits purchase requests, confirms receipt, and rates vendors.
*   **Finance Manager:** Allocates budgets and approves/rejects requests.
*   **Procurement Officer:** Reviews approved requests and issues purchase orders.

### System Processes (The Core Modules)
1.  **1.0 Vendor & Admin Module:** Handles sign-ups, vetting, and user management.
2.  **2.0 Finance Management:** Manages department budgets and approval queues.
3.  **3.0 Procurement Operations:** Facilitates shop catalog browsing, order requests, PO generation, and delivery confirmation.

### Data Store (The Database)
*   **Central Database:** Stores all budgets, vendor catalog details, order request logs, and permanent audit trails.

---

## 2. Simplified DFD (Mermaid.js)

Below is the Mermaid.js code for the simplified DFD. You can copy and paste this code block directly into any Mermaid viewer.

```mermaid
flowchart TD
    %% Style Definitions
    classDef entity fill:#f1f8e9,stroke:#558b2f,stroke-width:2px;
    classDef process fill:#e1f5fe,stroke:#0288d1,stroke-width:2px;
    classDef database fill:#f3e5f5,stroke:#8e24aa,stroke-width:2px,shape:cylinder;

    %% External Entities (Actors)
    Supplier["Supplier (Vendor)"]:::entity
    Admin["System Admin"]:::entity
    Requester["Internal User (Requester)"]:::entity
    Finance["Finance Manager"]:::entity
    Procurement["Procurement Officer"]:::entity

    %% Core System Processes (Modules)
    P_Admin["1.0 Vendor & Admin Module"]:::process
    P_Finance["2.0 Finance Management"]:::process
    P_Procure["3.0 Procurement Operations"]:::process

    %% Central Database Store
    DB[("ProVMS Central Database\n(Budgets, Orders, Vendors)")]:::database

    %% Data Flows - Vendor & Admin
    Supplier -->|1. Sign-Up Info & Tax PDF| P_Admin
    Admin -->|2. Verify & Approve Supplier| P_Admin
    P_Admin -->|3. Save Approved Vendor & Catalog| DB

    %% Data Flows - Finance
    Finance -->|4. Set Budgets & Clear Requests| P_Finance
    P_Finance -->|5. Update Budgets & Save Activity Log| DB

    %% Data Flows - Procurement Operations
    Requester -->|6. Submit Request & Rate Supplier| P_Procure
    Procurement -->|7. Issue Order Ticket (PO)| P_Procure
    P_Procure -->|8. Read/Write Orders & Evaluations| DB
    P_Procure -->|9. Send Order Ticket & Delivery Status| Supplier
    Supplier -->|10. Update Shipment Status| P_Procure
    P_Procure -->|11. Send Arrival Alert| Requester
```
