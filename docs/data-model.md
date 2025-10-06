# Data Model


## Supplier
Description: Represents a vendor or store from which goods are purchased.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Referenced by PurchaseOrder, StagingBatch, StagingProductCandidate |
| Name | Text | Supplier name | — |
| WebsiteUrl | Text (nullable) | Supplier website | — |
| ContactInfo | Text (nullable) | Contact details | — |


## PurchaseOrder
Description: A record of an order placed with a supplier, including costs and overhead allocations.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of PurchaseOrderItem, Delivery |
| SupplierId | Integer (FK) | Supplier reference | → Supplier |
| OrderDate | DateTime | Date order was placed | — |
| Status | Enum (Pending, PartiallyDelivered, Delivered) | Current state | — |
| ShippingFees | Currency | Shipping cost | — |
| ImportFees | Currency | Import duties | — |
| InsuranceFees | Currency | Insurance fees | — |
| AdditionalFees | Currency | Miscellaneous overhead | — |
| Notes | Text (nullable) | Free‑form notes | — |


## PurchaseOrderItem
Description: Line items within a purchase order, representing specific products or SKUs ordered.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | — |
| PurchaseOrderId | Integer (FK) | Parent order | → PurchaseOrder |
| ProductId | Integer (FK, nullable) | Linked product | → Product |
| SupplierReference | Text | Supplier SKU or reference | — |
| SupplierProductUrl | Text (nullable) | Link to supplier product page | — |
| Name | Text | Item name (from supplier) | — |
| Description | Text (nullable) | Item description | — |
| Quantity | Integer | Quantity ordered | — |
| ListedUnitPrice | Currency | Supplier listed price | — |
| ActualUnitPrice | Currency | Actual paid price | — |
| AllocatedUnitOverhead | Currency | Overhead share of the total order overhead | — |
| TotalUnitCost | Currency | Total cost of a unit factoring in discounts, overhead and any fees | — |


## Delivery
Description: Represents a shipment or receipt of goods, which may be linked to a purchase order or stand alone.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of DeliveryItem |
| PurchaseOrderId | Integer (FK, nullable) | Linked PO | → PurchaseOrder |
| DeliveryDate | DateTime | Date received | — |
| Courier | Text (nullable) | Courier name | — |
| TrackingNumber | Text (nullable) | Tracking reference | — |
| Status | Enum (Pending, Partial, Complete) | Delivery state | — |


## DeliveryItem
Description: Individual items received in a delivery, with quality and inspection details.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | — |
| DeliveryId | Integer (FK) | Parent delivery | → Delivery |
| ProductId | Integer (FK) | Linked product | → Product |
| Quantity | Integer | Quantity received | — |
| Quality | Enum (Excellent, Good, Fair, Poor, Terrible) | Quality rating | — |
| Notes | Text (nullable) | Inspection notes | — |


## Product
Description: Represents a catalog item that can be purchased, delivered, and sold. Central to linking orders, deliveries, and sales.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of ProductPhoto, ReconciledSale |
| SKU | Text (nullable) | Internal SKU | — |
| Name | Text | Product name | — |
| Description | Text (nullable) | Product description | — |
| Quality | Enum (Excellent, Good, Fair, Poor, Terrible) | Default quality rating | — |
| Notes | Text (nullable) | Additional notes | — |
| StockOnHand | Integer | Current stock | Derived from deliveries & sales |


## ProductPhoto
Description: Stores one or more images associated with a product.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | — |
| ProductId | Integer (FK) | Linked product | → Product |
| Url | Text | Photo URL or path | — |
| Caption | Text (nullable) | Description of photo | — |


## MarketEvent
Description: Represents a market day or event where sales occur. Used to group reconciled sales.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of ReconciledSale, StocktakeSession |
| Name | Text | Event name | — |
| Date | DateTime | Event date | — |
| Location | Text (nullable) | Event location | — |
| Notes | Text (nullable) | Additional notes | — |


## ReconciledSale
Description: A confirmed sale linked to a product and market event, derived from imported records and stocktake.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | — |
| ProductId | Integer (FK) | Linked product | → Product |
| MarketEventId | Integer (FK) | Linked event | → MarketEvent |
| Quantity | Integer | Units sold | — |
| SalePrice | Currency | Price per unit | — |



## StagingBatch
Description: Represents a single supplier data upload (e.g. Shein ZIP) or sales data upload (e.g. Yoco API load, grouping all parsed orders, sales and items.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of StagingPurchaseOrder |
| SupplierId | Integer (FK) | Supplier reference | → Supplier |
| UploadDate | DateTime | When file was uploaded | — |
| FileHash | Text | Hash for deduplication | — |
| Status | Enum (Pending, Processed) | Batch state | — |
| Notes | Text (nullable) | Free‑form notes | — |


## StagingSale
Description: A parsed sale stored in staging before validation and promotion.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of StagingSaleItem |
| StagingBatchId | Integer (FK) | Parent batch | → StagingBatch |
| SaleDate | DateTime | Sale date | — |
| RawData | JSON/Text | Original row data from sales endpoint | — |
| IsImported | Boolean | Whether promoted into production | — |

## StagingSaleItem
Description: Raw sales data imported from third‑party reports before reconciliation.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | — |
| StagingSaleId | Integer (FK) | Parent sale | → StagingSale |
| ProductDescription | Text | Description from report | — |
| ProductId | Integer (FK, nullable) | Linked product if matched | → Product |
| SaleDate | DateTime | Sale date | — |
| Price | Currency | Price from report | — |
| Quantity | Integer | Quantity sold (if available) | — |
| MarketEventName | Text (nullable) | Event name from report | — |
| RawData | JSON/Text | Raw data that represents the sale from report | — |
| IsImported | Boolean | Whether promoted into production | — |
| Status | Enum (PendingReview, Linked, Ignored) | Candidate state | — |


## StagingPurchaseOrder
Description: A parsed supplier order stored in staging before validation and promotion.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | Parent of StagingPurchaseOrderItem |
| StagingBatchId | Integer (FK) | Parent batch | → StagingBatch |
| PurchaseOrderId | Integer (FK, nullable) | Linked purchase order if matched | → PurchaseOrder |
| SupplierReferenceNumber | Text | Supplier order reference number | — |
| OrderDate | DateTime | Order date | — |
| RawData | JSON/Text | Original row data from supplier | — |
| IsImported | Boolean | Whether promoted into production | — |


## StagingPurchaseOrderItem
Description: Line items from a supplier order in staging, awaiting linking or confirmation.  
| Field | Type | Description | Relationships |
|-------|------|-------------|----------------|
| Id | Integer (PK) | Unique identifier | — |
| StagingPurchaseOrderId | Integer (FK) | Parent order | → StagingPurchaseOrder |
| ProductId | Integer (FK, nullable) | Linked product if matched | → Product |
| SupplierId | Integer (FK, nullable) | Linked  supplier if matched | → Supplier |
| PurchaseOrderItemId | Integer (FK, nullable) | Linked purchase order item if matched | → PurchaseOrderItem |
| SupplierReferenceNumber | Text | Supplier SKU | — |
| SupplierProductUrl | Text (nullable) | Product URL | — |
| Name | Text | Item name | — |
| Description | Text (nullable) | Item description | — |
| Quantity | Integer | Ordered quantity | — |
| ListedPrice | Currency | Listed price | — |
| ActualPrice | Currency | Paid price | — |
| RawData | JSON/Text | Original row data | — |
| IsImported | Boolean | Whether promoted into production | — |
| Status | Enum (PendingReview, Linked, Ignored) | Candidate state | — |


