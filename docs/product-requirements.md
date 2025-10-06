

# Product Requirements Document (PRD)
Project: My Market Manager  
Owner: Matthew  
Date: October 2025  

---

## 1. Introduction
My Market Manager is a mobile and web application to manage weekend market purchasing, deliveries, inventory reconciliation, and profitability analysis. Purchases may be recorded before delivery or at delivery time. Deliveries can arrive in full or in multiple partial shipments. Sales are tracked offline and reconciled via stocktakes against imported reports.  

The system also supports ingestion of supplier data files (e.g. Shein “Request My Data” ZIPs) into a staging layer, with a two‑phase process: automated ingestion overnight, followed by validation and confirmation by a user.

---

## 2. Objectives
- Separate recording of purchase orders and delivery events.  
- Support linking deliveries to existing purchase orders or creating new orders from delivery data.  
- Handle split and partial deliveries with quality inspection at receipt.  
- Allocate overhead costs across items, capture product metadata, and pricing rules.  
- Reconcile offline sales via import and stocktake workflows.  
- Provide reporting on product performance, profit, and reorder recommendations.  
- Automate ingestion of supplier data into staging, with deduplication and smart linking.  
- Provide a validation workflow for unresolved supplier items before promotion into production data.  

---

## 3. User Roles and Personas
- Buyer  
  - Creates purchase orders, records deliveries, inspects quality, and captures metadata.  
- Reconciliation Specialist  
  - Imports third‑party sales reports, conducts stocktakes, and reconciles sales to inventory.  
- Admin/Manager  
  - Configures catalog, delivery and purchase workflows, reviews performance reports, and validates supplier ingestion candidates.  

---

## 4. User Stories
1. As a Buyer, I want to create a purchase order before delivery so I can plan costs in advance.  
2. As a Buyer, I want to record a delivery event—full or partial—for an order and capture quality notes for pricing.  
3. As a Buyer, I want to record a delivery and have the system create a purchase order if none exists so I never lose track of items.  
4. As a Buyer, I want to split a large delivery into multiple receipts so I can inspect and price items as they arrive.  
5. As a Reconciliation Specialist, I want to import a minimal sales report (description + price) and link entries via stocktake.  
6. As a Manager, I want post‑event reports grouped by month and market event showing reconciled profit and units sold for reordering.  
7. As a Manager, I want to upload a supplier ZIP (e.g. Shein) so the system can parse and stage new orders automatically.  
8. As a Manager, I want the system to auto‑link staged items to existing products by supplier reference number so I don’t have to re‑enter data.  
9. As a Manager, I want to review unresolved product candidates and confirm or link them so that staging data can be promoted into production.  

---

## 5. Functional Requirements

### 5.1 Purchase Recording
- Manual creation of purchase orders (PO) before delivery.  
- PO includes supplier/store, order date, expected items, preliminary costs, and overhead categories.  
- Ability to save a PO draft when details are incomplete.  
- Link new POs to upcoming delivery events for planning.  

### 5.2 Delivery Recording
- Create a Delivery event linked to a PO or standalone.  
- For standalone deliveries, auto‑generate a matching PO from delivery details.  
- Support full and partial deliveries: record delivered quantity per line item.  
- Capture for each delivered item:  
  - Reference to existing product SKU or “new product” creation.  
  - Unit cost, quantity received, photos, quality metrics, delivery notes.  
- Allocate overhead costs (import, duty, shipping, admin) at delivery or upon final receipt.  
- Update PO status: Pending → Partially Delivered → Delivered.  

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

### 5.5 Sales Data Import & Reconciliation
- Import third‑party sales reports (CSV/Excel) with minimal columns: description, sale price, quantity (optional).  
- Provide field‑mapping interface for various report layouts.  
- Use fuzzy matching on description and price to suggest candidate products.  
- Flag unmatched rows for manual review.  

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

### 5.8 Two‑Phase Supplier Sync
- Phase 1: Automated Ingestion (Overnight)  
  - Run ingestion job to parse, deduplicate, and auto‑link supplier data.  
  - Mark batch as processed.  
  - Leave unresolved items as pending candidates.  
- Phase 2: Validation & Confirmation (Interactive)  
  - Admin reviews pending candidates.  
  - Options: link to existing product, confirm as new product, or ignore.  
  - Once resolved, staging orders/items are promoted into production entities.  
  - Mark staging rows as imported.  

---

## 6. Integration Requirements
- Purchases & Deliveries: manual entry and file import.  
- Sales: periodic import of CSV/Excel reports.  
- Supplier ingestion: ZIP upload → staging → validation → promotion.  
- Configurable parsers per supplier.  
- Manual overrides for ambiguous or unmatched records.  

---

## 7. Reporting Requirements
- Ingestion Summary (per batch): orders parsed, items parsed, imported, pending candidates.  
- Validation Summary: total candidates, linked, new products created, ignored, remaining pending.  
- Product Performance: units delivered, units sold, revenue, and profit per product (post‑reconciliation).  
- Time & Event Analysis: aggregate reconciled sales and profits by month and by Market Event.  
- Reorder Recommendations: identify top‑performing SKUs for restock; flag slow‑moving or low‑margin items.  

---

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

