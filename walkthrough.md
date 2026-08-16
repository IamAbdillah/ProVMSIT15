END-TO-END TRANSACTION WALKTHROUGH

Scenario: "The ₱1.25M IT Infrastructure Upgrade"
Goal: Source 50 Professional Corporate Workstations at ₱25,000 each = ₱1,250,000 total

 STEP 1 — External Vendor Self-Onboarding & Catalog Input
Actor: Emma Watson (Vendor)
URL: `/Vendor/Onboarding`

1. Emma fills Wizard Step 1:
     CompanyName = "TechCorp Solutions"
     TaxID       = "123456789"
     Email       = "emma@techcorp.com"

2. Wizard Step 2: Uploads tax_certificate.pdf (validated ≤ 5MB, .pdf only)

3. Wizard Step 3: Adds catalog row:
     ItemName  = "ProWorkstation V1"
     Category  = "IT Hardware"
     UnitPrice = ₱25,000.00

4. Clicks Submit


 STEP 2 — Administrative Accreditation & Document Vetting
Actor: Mark (System Admin)
URL:`/Vendor/AccreditationDesk`

```
1. Mark logs in → bell badge shows 🔔 1
2. Clicks notification → deep-links directly to AccreditationDesk (no manual navigation)
3. Reviews inline PDF viewer alongside vendor details
4. Clicks ✅ Approve


 STEP 3 — Requisition Demand & Financial Pre-Encumbrance
Actor: Juan (Internal User)
URL: `/Catalog/Marketplace`

1. Juan searches "Hardware" → selects "ProWorkstation V1" by TechCorp Solutions
2. UnitPrice auto-populates ₱25,000 (read-only — immutable server-side)
3. Enters Quantity: 50
4. System displays computed total: 50 × ₱25,000 = ₱1,250,000 (non-editable)
5. Clicks Submit Request


 STEP 4 — Fiscal Clearance & Audit Ledger Sign-Off
Actor: Priya (Finance Manager)
URL: `/Catalog/ApprovalWorkflow`


1. Priya sees 🔔 notification, navigates to Approval Workflow
2. Reviews: Requester = Juan | Item = ProWorkstation V1 | Total = ₱1,250,000
3. Clicks ✅ Approve Expenditure

STEP 5 — Procurement Validation & Purchase Order Issuance
Actor: Ben (Procurement Officer)
URL: `/Catalog/POVault`

1. Ben sees 🔔 notification, opens PO Vault
2. Confirms: TechCorp = Active ✅ | Budget = Cleared ✅
3. Clicks 📄 Issue PO

 STEP 6 — Logistics Handling & Cargo Dispatch
Actor: Emma Watson (Vendor)
URL:`/Catalog/DeliveryTracking`

1. Emma opens her Vendor Orders portal
2. Loads 50 workstation units onto transport carrier
3. Clicks 🚚 Mark as Dispatched
STEP 7 — Material Receipt & Evaluation Submission
Actor: Juan (Internal User)
URL:`/Catalog/MyRequests` → `/Evaluation/Submit`

Step 7A — Confirm Receipt:
1. Juan receives delivery of 50 workstations
2. Navigates to My Requests, finds order row
3. Clicks ✅ Confirm Material Receipt
   Result: WorkflowStatus → Delivered

Step 7B — Submit Evaluation:
1. Juan navigates to /Evaluation/Submit
2. Rates the transaction:
     Delivery Speed:  ⭐⭐⭐⭐⭐ (5)
     Item Condition:  ⭐⭐⭐⭐⭐ (5)
     Communication:   ⭐⭐⭐⭐⭐ (5)
     Comment: "Workstations delivered early, fantastic transaction lifecycle response time."
3. Clicks Submit Evaluation
   Result: WorkflowStatus → Archived
           TechCorp OverallAverage updated in Leaderboard
           Dashboard charts reflect ₱1,250,000 spend entry