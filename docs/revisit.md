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

## Award approvals don't stop someone approving their own recommendation (Approvals)

The award policy's first step is "any Procurement manager". If a Procurement manager runs an RFQ and recommends the award, the chain resolves that step to them. The same can happen to a department manager raising their own requisition.

**When to revisit:** before real use. In `ApprovalService.StartAsync`, when a step resolves to the requester, escalate to the requester's manager (or the next person in the role).

## Issued purchase orders can't be cancelled (Procurement)

A draft order can be cancelled, but an issued one can't yet. Cancelling an issued order means telling the supplier, releasing the order's budget commitment, and deciding what happens to its requisitions (back to sourcing, or cancelled).

**When to revisit:** with goods receipt. An order with nothing received can be cancelled; one that is partly received is closed short instead.

## Supplier contacts and addresses can't be edited in the app (Suppliers)

A supplier gets one contact when it's created. There are no endpoints yet to add, change or remove contacts and addresses, so the supplier page only displays them.

**When to revisit:** before suppliers are onboarded for real. Add the endpoints, plus an edit panel on the supplier page.

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
