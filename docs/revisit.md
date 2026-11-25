# Things to revisit

Known limits we have accepted for now. Each one says why it is fine today and what would make us change it.

## Concurrent budget checks (Finance)

If two requisitions on the same cost centre are submitted at the same moment, both budget checks can read the same Available figure. Both may then pass as "within budget" even though together they go over.

**Why it's fine today:** budgets are soft. Nothing is blocked, the commitments still land on approval, and Finance sees the overspend as a negative Available.

**When to revisit:** if we ever add hard budgets that block a submit. The check and the commit would then need to run under a lock on the budget row (for example `UPDLOCK` / `SERIALIZABLE` on the budget, or an optimistic retry on its RowVersion).

## Requisition list loads up to 200 rows (Procurement)

The list returns the newest 200 requisitions for the chosen scope; the browser filters and searches them.

**Why it's fine today:** one department raises far fewer than 200 a year.

**When to revisit:** when the "All" tab gets slow or someone can't find an older requisition. Move status,
search and paging to the API (`?status=&q=&page=`) and keep the URL parameters the page already uses.

## Adding a quotation or invitation doesn't bump the RFQ's RowVersion (Procurement)

EF only bumps a row's RowVersion when that row changes. Recording a quotation inserts child rows and may leave the `sourcing_events` row untouched. Two buyers recording quotes for the same supplier at the same moment could therefore both pass the "one live quotation per supplier" check.

**Why it's fine today:** one buyer runs each RFQ, and the invitation row's status does change (Invited → Responded), which catches most races.

**When to revisit:** if several buyers share RFQs. Add a filtered unique index on `quotations (sourcing_event_id, supplier_id) WHERE status = 'Received'`, or touch a `LastChangedAt` column on the event in every command so its RowVersion always moves.

## Two issues of the same item at the same moment can overdraw stock (Inventory)

Issuing checks the item's on-hand figure and then adds a negative movement. Two storekeepers issuing the last units at the same moment can both pass the check, leaving stock below zero.

**Why it's fine today:** one storekeeper per site, and a count will show and correct it.

**When to revisit:** with several issuers per site. Keep a balance row per site and item, update it in the same transaction, and add a check constraint (`on_hand >= 0`) or take an `UPDLOCK` on it.

## Stock requests have no approval step (Inventory)

A request goes straight to the warehouse. The stock was approved when it was bought, and the warehouse can reject.

**When to revisit:** if departments start asking for expensive or controlled items. Route requests over a value limit through the department manager with the existing approvals module.

## Issued stock isn't charged to the requesting cost centre's budget (Inventory, Finance)

The budget is consumed when the purchase is invoiced, against the cost centre that bought it. A request records the cost centre it's for, but nothing moves between budgets when stock is issued.

**When to revisit:** if a central store buys for everyone and departments should carry their own usage. Post the issue value (at average cost) from the store's cost centre to the requester's.

## Cancelling an order doesn't notify the supplier (Procurement)

Cancelling or closing an issued order short records the reason and fixes the budget, but nobody tells the supplier. The buyer still has to email them.

**When to revisit:** with notifications. Email the supplier's primary contact a cancellation notice quoting the order number and reason.

## Supplier changes aren't audited field by field (Suppliers)

Contacts, addresses and payment terms can be edited by anyone with the manage permission. Each row records who last changed it (`ModifiedBy`), but not the old values.

**When to revisit:** before bank details are added to suppliers, where a changed account number is a classic fraud route. Keep a change log of old and new values, and require a second person to approve bank detail changes.

