# ProVMS — Layman's Defense Study Guide
## "The Plain-English Companion to ProVMS"

This guide translates the technical jargon of ProVMS (Procurement & Vendor Management System) into simple, everyday concepts and analogies. Use this to prepare for your defense if you need to explain the system to non-technical panelists.

---

## 🍽️ The Tech Stack: Explained Like a Restaurant

If your system was a premium restaurant, here is how the tech stack maps out:

* **ASP.NET Core MVC (The Restaurant System):** The overall system that runs the kitchen and dining room.
* **The Model / M (The Ingredients & Recipes):** Holds the raw data structure (e.g., what details go into a Purchase Request).
* **The View / V (The Plating & Presentation):** The actual web page the user looks at (the layout, design, and buttons).
* **The Controller / C (The Waiter):** The middleman. When you click a button (order), the Controller takes that request, talks to the database (kitchen), and brings back the web page (your meal).
* **Entity Framework Core / ORM (The Kitchen Translator):** Instead of developers writing complex database queries (SQL), Entity Framework acts as a translator, automatically converting standard C# code into database-speak.
* **MySQL Database (The Pantry / Storage Locker):** The secure room where all files, user details, history, and budgets are permanently stored.
* **IIS / MonsterASP Hosting (The Restaurant Building):** The physical location where the app lives so that anyone on the internet can visit it.


---

## 🔐 Security & Login: The Real-World Analogies

When professors ask about authentication, security, and authorization, use these plain-English concepts:

### 1. BCrypt Password Hashing: "The Meat Grinder"
* **Technical:** A secure cryptographic hashing algorithm with a work factor of 11.
* **Non-Tech Explanation:** It is like a digital meat grinder. When you set a password, the system grinds it up into a scrambled mess of text and stores that instead. You can never "un-grind" the mess to find the original password. When you log in, the system grinds up your input and checks if the two ground-up outputs match. Even if a hacker steals the database, they just get useless ground-up text.

### 2. Antiforgery Tokens (CSRF): "The Security Stamp"
* **Technical:** Cross-Site Request Forgery protection using validation tokens.
* **Non-Tech Explanation:** Imagine a bank teller who only accepts forms stamped with the bank’s official daily ink watermark. If you try to hand the teller a photocopied form without the wet stamp, they reject it. In ProVMS, every form has a hidden, unique security watermark. If a malicious website tries to submit a form on your behalf, the server rejects it because it lacks the valid watermark.

### 3. Role-Based Access Control (RBAC): "The Keycard System"
* **Technical:** Policy-based authorization.
* **Non-Tech Explanation:** Just like in a secure office building, different employees have different keycards:
  * **Vendors** can only access public registration and their own catalog.
  * **Internal Users** can only walk around the Marketplace and request purchases.
  * **Finance Managers** have the key to the vault (Budgets & Approvals).
  * **Procurement Officers** can issue official contracts.
  * **System Admins** have the master key.

### 4. Account Lockout: "The Security Guard"
* **Technical:** Access lockout rules (5 attempts, 15-minute cooldown).
* **Non-Tech Explanation:** If someone tries to guess your front door lock code 5 times in a row, a security guard locks the gate and forces them to wait 15 minutes before they can try again. This stops automated guessing attacks.

### 5. Soft-Delete / Archiving: "The Filing Cabinet"
* **Technical:** Entity Framework Global Query Filter (`IsArchived = true`).
* **Non-Tech Explanation:** We never throw transaction records or user history into the paper shredder (hard-delete). Instead, we mark them as "Archived" and move them to a filing cabinet in the back room. They disappear from the active web screens, but the history is preserved for financial audits.

---

## 🔄 Step-by-Step System Workflow (Role-by-Role)

This is the end-to-end journey of how a vendor gets registered, how an item is requested and approved, and how delivery is finalized.

1. **The Vendor Registers (Role: Vendor)**
   * **Action:** A new external supplier fills out the onboarding form on the public website and uploads their tax documents (PDF).
   * **Status:** Their account is created, but they cannot log in yet (locked).

2. **The Office Vets the Vendor (Role: Procurement Officer or Admin)**
   * **Action:** The Procurement officer reviews the supplier's files at the Accreditation Desk. They click "Approve" if everything looks good.
   * **Status:** The Vendor status changes to "Active", and they are sent an in-app notification that they can now log in.

3. **The Vendor Posts Items (Role: Vendor)**
   * **Action:** The Vendor logs in, adds their products/services to the catalog, and sets the unit prices.

4. **The Employee Requests a Purchase (Role: Internal User)**
   * **Action:** An employee browses the Marketplace catalog, selects the Vendor's items, and clicks "Place Requisition".
   * **System Check:** The system automatically checks the department's budget. If there is enough money, the required amount is frozen (encumbered) so it cannot be spent elsewhere.
   * **Status:** Requisition status becomes "Pending Finance Approval".

5. **The Finance Manager Approves the Funds (Role: Finance Manager)**
   * **Action:** The Finance Manager reviews the request, verifies the remaining budget, and clicks "Approve".
   * **Status:** Requisition status changes to "Approved Budget".

6. **The Purchase Order is Sent (Role: Procurement Officer)**
   * **Action:** The Procurement Officer issues the official Purchase Order (PO). The system automatically generates a downloadable PDF of the order using the data.
   * **Status:** Requisition status changes to "PO Issued", and the Vendor is notified to start preparing the items.

7. **The Vendor Ships the Goods (Role: Vendor)**
   * **Action:** The Vendor packages the order and updates the shipping status on the portal to show the items are on the way.
   * **Status:** Order status updates to "In Transit".

8. **The Office Receives the Delivery (Role: Procurement Officer)**
   * **Action:** Once the items physically arrive, the Procurement Officer checks the boxes and marks the shipment as delivered.
   * **Status:** Requisition status changes to "Delivered".

9. **The Final Inspection & Rating (Role: Internal User / Requester)**
   * **Action:** The original employee inspects the items. They click "Confirm Receipt" and rate the vendor from 1 to 5 stars.
   * **System Check:** The frozen budget is permanently deducted, the vendor’s rating on the leaderboard is updated, and the transaction is closed.
   * **Status:** Order status changes to "Archived" (Completed).

---

## 💼 Business Workflows: Simplified

### 1. Budget Encumbrance: "Reserving a Table"
* **What happens:** When an employee makes a Purchase Request (PR), the money is **encumbered**.
* **Analogy:** Imagine calling a restaurant to reserve a table for 10 people tonight. They haven’t charged your credit card yet, but they cannot let anyone else sit at that table. Similarly, the system "freezes" that portion of the department budget so it cannot be double-spent while waiting for the manager's signature. If the manager rejects the request, the reservation is cancelled, and the money is unfrozen.

### 2. SLA (Service Level Agreement): "The Delivery Guarantee"
* **What happens:** Timestamps track task durations, highlighting overdue ones in red.
* **Analogy:** Like ordering a pizza with a "delivered in 30 minutes or it's free" promise. If the pizza takes 45 minutes, a warning light flashes. In ProVMS, tasks have deadlines (e.g., 24 hours for budget approvals). If the deadline passes, the item turns red on the screen to warn managers that a bottleneck has occurred.

### 3. Three-Way Matching: "The Delivery Receipt Double-Check"
* **What happens:** System validates orders, deliveries, and invoices.
* **Analogy:** When you buy a TV online:
  1. You check your email confirmation to see what you bought (**Purchase Order**).
  2. You check the box delivered to your house to ensure the TV is inside (**Delivery Receipt**).
  3. You check your bank statement to make sure you were charged the correct amount (**Invoice**).
  If all three match, you pay. ProVMS ensures this workflow is fully digitized.

---

## 💬 Responding to Common Questions (Non-Technical Version)

### Q: "Why did you build it as an MVC app instead of using React or Angular?"
> **Answer:** "MVC keeps everything simple and secure. Instead of sending raw database records to the browser and letting the browser build the page (which can be vulnerable to tampering), the server builds the page securely in the kitchen and serves a complete, safe webpage to the user. It also makes sure a user is authorized before they even get to see the screen."

### Q: "Is your system a CRM (Customer Relationship Management)?"
> **Answer:** "No, it's the exact opposite! A CRM is for managing **customers** you sell to. ProVMS is an **SRM (Supplier Relationship Management)** system for managing **vendors** we buy from. It handles the buying process (Procurement) rather than the selling process."

### Q: "What does the Google reCAPTCHA check do?"
> **Answer:** "It is the 'I am not a robot' checkbox. It stops malicious computer scripts from trying to spam-login or hack into the system by forcing a quick test that only a human brain can pass."

### Q: "How does the system generate PDFs?"
> **Answer:** "We use a tool called iText7. Think of it as a digital typewriter. When a purchase is approved, the system takes the order details, automatically formats them into a neat letterhead, and stamps it as a secure PDF document that can be downloaded or printed."
