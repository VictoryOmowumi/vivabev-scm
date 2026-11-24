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

## Budget commitments stay at the requisition estimate after an award (Finance)

Budgets commit the requisitions' estimated totals at approval. An award above the estimate adds a Finance controller step, but it doesn't change the committed amount.

**When to revisit:** with purchase orders. The PO should re-commit the actual amount and release the difference.
