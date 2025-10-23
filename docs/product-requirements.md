

# Product Requirements Document (PRD)

## 1. Introduction
My Market Manager is a mobile and web application to manage weekend market purchasing, deliveries, inventory reconciliation, and profitability analysis. Purchases may be recorded before delivery or at delivery time. Deliveries can arrive in full or in multiple partial shipments. Sales are tracked offline and reconciled via stocktakes against imported reports.  

The system also supports ingestion of supplier data files (e.g. Shein “Request My Data” ZIPs) into a staging layer, with a two‑phase process: automated ingestion overnight, followed by validation and confirmation by a user.


## 2. Objectives

**Primary Objectives (Automation Focus)**:
- **Automate purchase order ingestion** via web scraping from supplier websites (Shein, others)
- **Automate sales data import** from payment providers and POS systems (CSV/Excel)
- **Automate data reconciliation** with intelligent matching, deduplication, and smart linking
- **Provide two-phase validation workflow** for automated ingestion with human oversight
- **Generate profitability reports** from consolidated data across all sources
- **Recommend reordering decisions** based on aggregated purchase and sales data

**Secondary Objectives (Manual Workflows)**:
- Support manual purchase order and delivery recording for exceptions
- Enable manual stocktake adjustments when automated reconciliation fails
- Allow manual linking of unresolved products during validation
- Provide override capabilities for pricing and product metadata  


## 3. User Roles and Personas
- **Data Validator / Manager** (Primary Role)
  - Reviews automated ingestion batches from web scrapers and uploads
  - Validates and links unresolved product candidates
  - Monitors automated reconciliation results
  - Reviews profitability reports and makes purchasing decisions
  - Configures web scraping schedules and data sources
  
- **Manual Data Entry Operator** (Secondary Role)
  - Handles exceptions: manual PO entry for non-automated suppliers
  - Records deliveries when web scraping is unavailable
  - Performs manual stocktakes when automated reconciliation fails
  - Captures quality inspection data during physical receipt  


## 4. User Stories

**Primary User Stories (Automation)**:

1. As a Manager, I want to **configure web scraping** for my supplier accounts so purchase orders are automatically ingested overnight.
2. As a Manager, I want the system to **automatically import sales data** from payment provider exports so I do not manually enter sales.
3. As a Manager, I want the system to **auto-link products** across suppliers and sales using reference numbers and fuzzy matching so I minimize manual matching.
4. As a Validator, I want to **review unresolved candidates** in a validation queue so I can confirm or link them before promotion to production.
5. As a Manager, I want **automated profitability reports** that consolidate all purchase and sales data so I can make informed reordering decisions.
6. As a Manager, I want to **upload supplier data files** (Shein ZIP) so the system can parse and stage orders automatically.

**Secondary User Stories (Manual Workflows)**:

7. As an Operator, I want to **manually create a purchase order** for a non-automated supplier so I can track exceptional purchases.
8. As an Operator, I want to **manually record a delivery** with quality notes when web scraping data is unavailable.
9. As an Operator, I want to **manually adjust stocktake results** when automated reconciliation produces incorrect matches.



## 5. Functional Requirements

### 5.1 Automated Purchase Order Ingestion (Primary)
- **Web scraping integration** for automated PO extraction from supplier websites (Shein, others)
- **Scheduled scraping jobs** that run overnight to fetch new orders
- **Cookie-based authentication** for accessing supplier accounts securely
- **Automatic parsing** of order data into staging entities with deduplication
- **Smart linking** of scraped items to existing products by supplier reference number or product URL
- **File upload support** for supplier data exports (ZIP files, CSV) as alternative to scraping

### 5.1.1 Manual Purchase Recording (Secondary/Exception Handling)
- Manual creation of purchase orders when automated ingestion is unavailable
- PO includes supplier/store, order date, expected items, preliminary costs, and overhead categories
- Ability to save PO draft when details are incomplete
- Link POs to delivery events for tracking  

### 5.2 Delivery Recording (Secondary/Exception Handling)
- Manual delivery recording for suppliers without automated ingestion
- Create delivery event linked to PO or standalone
- For standalone deliveries, auto-generate matching PO from delivery details
- Support full and partial deliveries with quantity tracking
- Capture quality metrics, photos, and delivery notes for pricing decisions
- Allocate overhead costs at delivery or upon final receipt
- Update PO status: Pending → Partially Delivered → Delivered

**Note**: Most deliveries are inferred from automated PO ingestion; manual recording is for exceptions.


### 5.3 Purchase Order Management
- List and filter POs by supplier, date, delivery status.  
- Drill into PO details showing ordered vs. delivered quantities and allocated costs.  
- Highlight unmatched POs (no delivery recorded) and unmatched deliveries (no PO link).  
- Maintain price and delivery history per SKU across all POs and deliveries.  

### 5.4 Pricing Process
- Record quality metrics and comments upon each delivery.  
- Configure pricing rules:  
  - Cost + margin %  
  - Fixed markup based on quality tiers  
- Search catalog by name, description keywords, or photo.  
- Show current cost (including per‑item overhead), quality rating, and past sale prices.  

### 5.5 Automated Sales Data Import & Reconciliation (Primary)
- **Automated import** of sales reports from payment providers (CSV/Excel exports)
- **Scheduled import jobs** or API integrations for automatic data ingestion
- **Field-mapping configuration** for various payment provider report formats
- **Fuzzy matching algorithm** on description, price, and other fields to auto-link sales to products
- **Confidence scoring** for automated matches with threshold-based auto-approval
- **Unmatched sales queue** for manual review and linking
- **Batch reconciliation** with automated inventory adjustments  

### 5.6 Stocktake & Sales Consolidation
- Start, pause, and resume stocktake sessions tied to a Market Event.  
- Present imported sales as provisional records alongside current inventory.  
- Search by description or photo to confirm each sale’s product link.  
- Link provisional sales to products, adjust on‑hand counts, and finalize reconciliation.  
- Compute reconciled units sold, total revenue, and profit per product for the event.  

### 5.7 Supplier Data Ingestion
- Support upload of supplier ZIP files (starting with Shein “Request My Data”).  
- Parse ZIP contents into normalized staging entities: StagingBatch, StagingPurchaseOrder, StagingPurchaseOrderItem.  
- Deduplicate by SupplierOrderId and SupplierReferenceNumber.  
- Preserve raw supplier rows as JSON in staging for audit.  
- Attempt smart linking of items to existing products by supplier reference number or product URL.  
- Create StagingProductCandidate entries for unresolved items.  

### 5.8 Two-Phase Automated Sync (Primary - Core Workflow)
**Phase 1: Automated Ingestion (Scheduled/Overnight)**:
- **Automatic execution** of web scraping and file processing jobs
- **Parse and normalize** data into staging entities
- **Deduplicate** by supplier order ID and reference numbers
- **Auto-link** with high confidence matches (>95%) to existing products
- **Auto-promote** fully-matched orders to production immediately
- **Flag unresolved items** as pending candidates for Phase 2 review
- **Generate ingestion report** with statistics and error logs

**Phase 2: Validation & Confirmation (Interactive - Exception Handling)**:
- **Validation dashboard** showing pending candidates requiring review
- **Batch operations** for bulk linking/confirmation
- **Similarity search** to suggest existing product matches
- Options: link to existing product, confirm as new product, or ignore
- **Confidence scoring** to prioritize review queue (lowest confidence first)
- Once resolved, promote staging orders/items to production
- Mark staging rows as imported with audit trail

**Goal**: Minimize Phase 2 manual work through continuous improvement of Phase 1 automation.  


## 6. Integration Requirements

**Primary Integrations (Automated)**:
- **Web Scraping APIs**: Automated extraction from supplier websites (Shein, others)
  - Cookie-based authentication framework
  - Rate limiting and session management
  - Pluggable scraper architecture for multiple suppliers
  - MAUI SheinCollector app for cookie capture
- **Payment Provider APIs**: Automatic sales data import from Yoco, Square, etc.
- **File Processing**: Automated parsing of uploaded CSV/Excel/ZIP files
- **Background Jobs**: Scheduled ingestion, reconciliation, and reporting tasks
- **Webhook Support**: Real-time notifications from external systems

**Secondary Integrations (Manual)**:
- Manual CSV/Excel upload for one-off sales reports
- Manual supplier data file upload when scraping is unavailable
- Manual PO and delivery entry forms for exceptional cases

**Configuration**:
- Configurable parsers per supplier with field mapping
- Configurable scraping schedules and authentication
- Configurable fuzzy matching rules and confidence thresholds
- Manual override capabilities for ambiguous matches

**Client Libraries and Tools**:
- Type-safe GraphQL client library (StrawberryShake) for cross-platform apps
- Reusable client for MAUI, Blazor WASM, and other .NET applications
- Cookie capture utility (SheinCollector MAUI app) for web scraping authentication  


## 7. Reporting Requirements
- Ingestion Summary (per batch): orders parsed, items parsed, imported, pending candidates.  
- Validation Summary: total candidates, linked, new products created, ignored, remaining pending.  
- Product Performance: units delivered, units sold, revenue, and profit per product (post‑reconciliation).  
- Time & Event Analysis: aggregate reconciled sales and profits by month and by Market Event.  
- Reorder Recommendations: identify top‑performing SKUs for restock; flag slow‑moving or low‑margin items.  


## 8. Non‑Functional Requirements
- Performance: sub‑500 ms for search, fuzzy‑match, and sale capture.  
- Scalability: handle thousands of SKUs, high‑volume deliveries, and bulk sales imports.  
- Usability: mobile‑first UI for delivery recording and stocktakes; clear state indicators for draft vs. finalized POs.  
- Reliability: offline support for delivery entry and stocktake with sync on connectivity.  
- Security: role‑based access control, SSL encryption, audit logs for manual edits.  
- Auditability: raw supplier data preserved in staging.  
- Incremental ingestion: only new data imported.  
- Extensibility: new suppliers supported by adding parsers.  
- Traceability: every staging record links back to its batch.  

