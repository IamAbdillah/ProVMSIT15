# ProVMS (Procurement & Vendor Management System) — UML Use Case Diagram

This document contains a structured UML Use Case Diagram representing the core modules and actor interactions in the system, modeled after standard association-labeled UML diagrams.

---

## 1. Actors & Interactions Overview
*   **Internal User (Requester):** Initiates purchase requests, confirms order receipts, and rates performance.
*   **Supplier (Vendor):** Onboards with tax credentials and handles cargo dispatch/shipping.
*   **System Admin:** Verifies supplier accreditation details.
*   **Finance Manager:** Reviews and authorizes department expenditures.
*   **Procurement Officer:** Converts approved requests into official purchase order tickets.

---

## 2. Diagram Code (Mermaid.js)

Below is the Mermaid.js source code depicting the use cases, actors, and labeled associations. You can copy this block and paste it directly into any Mermaid editor to render it visually.

```mermaid
flowchart LR
    %% Style Definitions
    classDef actorNode fill:#f1f8e9,stroke:#558b2f,stroke-width:2px;
    classDef usecaseNode fill:#fff8e1,stroke:#ff8f00,stroke-width:2px,shape:oval;

    %% Left-Side Actors
    subgraph Left_Actors [" "]
        Requester["Internal User\n(Requester)"]:::actorNode
        Supplier["Supplier\n(Vendor)"]:::actorNode
        Admin["System Admin"]:::actorNode
    end

    %% Right-Side Actors
    subgraph Right_Actors [" "]
        Finance["Finance Manager"]:::actorNode
        Procurement["Procurement Officer"]:::actorNode
    end

    %% System Boundary
    subgraph SystemBoundary ["System Boundary: ProVMS"]
        UC_Onboard(("Onboard & Register")):::usecaseNode
        UC_Accredit(("Vett & Accredit")):::usecaseNode
        
        UC_Request(("Place Purchase Request")):::usecaseNode
        UC_Override(("Request Budget Exception")):::usecaseNode
        
        UC_Approve(("Review & Approve Request")):::usecaseNode
        UC_IssuePO(("Issue Order Ticket (PO)")):::usecaseNode
        
        UC_Ship(("Deliver & Ship Items")):::usecaseNode
        UC_Receive(("Receive & Confirm Order")):::usecaseNode
        
        UC_Evaluate(("Submit Evaluation")):::usecaseNode
        UC_FlagLow(("Flag Low Performance")):::usecaseNode
    end

    %% Left Actor Associations
    Requester -- "request items" ---> UC_Request
    Requester -- "confirm receipt" ---> UC_Receive
    Requester -- "rate supplier" ---> UC_Evaluate

    Supplier -- "submit credentials" ---> UC_Onboard
    Supplier -- "dispatch cargo" ---> UC_Ship

    Admin -- "verify credentials" ---> UC_Accredit

    %% Right Actor Associations
    Finance -- "approve budget" ---> UC_Approve
    Finance -- "review exception" ---> UC_Override

    Procurement -- "issue ticket" ---> UC_IssuePO
    Procurement -- "track shipping" ---> UC_Ship

    %% Extend Relationships (Dashed arrows pointing back to Base Use Case)
    UC_Override -.->|"<<extend>>\n{if request exceeds budget}"| UC_Request
    UC_FlagLow -.->|"<<extend>>\n{if score is < 2.5}"| UC_Evaluate
```
