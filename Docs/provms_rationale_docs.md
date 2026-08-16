# ProVMS (Procurement & Vendor Management System) — Rationale & Core Design Documentation

---

## 1. System Rationale

The **Procurement and Vendor Management System (ProVMS)** is a centralized, role-governed web application designed to simplify the purchase-to-payment process. It replaces messy email chains and paper forms with automated workflows, strict budget limits, and secure, permanent activity logs.

Tailored for organizations with multiple departments (such as IT, HR, Finance, and Operations), ProVMS drives business value through three main pillars:
*   **Financial Control:** Prevents departments from overspending by automatically locking and reserving budget funds the moment a purchase request is submitted, enforcing strict spending limits.
*   **Time Savings:** Speeds up operations by letting suppliers register themselves online, allowing employees to submit digital purchase requests, and automating approval routing and order delivery.
*   **Reduced Risk & Quality Control:** Protects the organization from unverified suppliers through a structured supplier vetting desk and ensures quality with mandatory 5-star vendor ratings that build an active performance leaderboard.

---

## 2. Problems & Solutions

### Problem 1: Unapproved Spending, Double-Spending, and Late-Detected Budget Overruns
Departments often submit duplicate orders or buy items directly from suppliers without approval. This leads to double-spending and budget overruns that the finance team only discovers after the invoices arrive.

*   **Solution 1: Real-Time Budget Locking & Strict Spending Limits**
    The system locks and reserves department funds the moment a purchase request is submitted. If a request exceeds the department’s remaining budget by even ₱1.00, the system automatically blocks it, preventing unapproved purchases before they are finalized.

### Problem 2: Unverified or High-Risk Suppliers
Onboarding suppliers manually via email is slow and insecure. It exposes the company to risks from unverified businesses who upload invalid documents, charge incorrect prices, or fail compliance standards.

*   **Solution 2: Online Sign-up Wizard & Verification Desk**
    Suppliers must register themselves through an online sign-up wizard, uploading a digital PDF copy of their tax certificate and entering their catalog items. The supplier is blocked from selling or logging in until an administrator reviews their documents at the *Verification Desk* and approves their account.

### Problem 3: Guesswork in Rating Suppliers and Unnoticed Delays
Without clear ratings, it is hard for purchasing teams to know which suppliers are reliable. Additionally, without tracking, bottlenecks in approvals or shipments go unnoticed.

*   **Solution 3: Mandatory Supplier Ratings & Expected Response Time Tracking**
    Once an employee confirms they have received their items, the system requires them to complete a simple 5-star rating across three categories (Delivery Speed, Item Condition, and Communication). These ratings feed a live *Supplier Performance Leaderboard*. The system also displays response time indicators (e.g., color-coded warnings) to flag delays in approvals or shipments.

---

## 3. Use Case Diagram (Text Representation)

The system is organized into three core functional modules. Below is the structured mapping of how each actor interacts with these modules:

### Module 1: Vendor & Administration
*   **Registered Vendor (Supplier) Interactions:**
    *   **Self-Onboard:** Submit registration info and upload tax documents.
    *   **Manage Catalog:** Update pricing or add new catalog items.
    *   **Update Cargo Status:** Mark orders as "In Transit" during shipping.
*   **System Admin Interactions:**
    *   **Verify Supplier:** Review and approve/reject supplier applications at the Vetting Desk.
    *   **Manage User Accounts:** Create or deactivate (archive) employee logins.
    *   **Monitor Response Time Logs:** Check SLA indicators and task completion speed.

### Module 2: Finance Management
*   **Finance Manager Interactions:**
    *   **Review Approval Queue:** Approve or reject pending purchase requests.
    *   **Manage Department Budgets:** View and allocate funds to departments.
    *   **View Activity Log:** Inspect the permanent, unchangeable record of all budget actions.

### Module 3: Procurement Operations
*   **Internal User (Requester) Interactions:**
    *   **Browse Marketplace:** View catalogs from verified, active suppliers.
    *   **Submit Purchase Request:** Order items (triggers automatic budget checks).
    *   **Confirm Material Receipt:** Mark orders as received once they arrive.
    *   **Submit Evaluation:** Rate the supplier across three performance categories.
*   **Procurement Officer Interactions:**
    *   **View Approved Requests:** Review requests cleared by Finance.
    *   **Issue Official Order Tickets:** Generate and send official purchase orders (creates a downloadable PDF).
    *   **Track Deliveries:** Monitor shipment status.
    *   **Manage Contracts:** Maintain active contracts and agreements with suppliers.
