# Things to revisit

Known limits we have accepted for now. Each one says why it is fine today and what would make us change it.

## Concurrent budget checks (Finance)

If two requisitions on the same cost centre are submitted at the same moment, both budget checks can read the same Available figure. Both may then pass as "within budget" even though together they go over.

**Why it's fine today:** budgets are soft. Nothing is blocked, the commitments still land on approval, and Finance sees the overspend as a negative Available.

**When to revisit:** if we ever add hard budgets that block a submit. The check and the commit would then need to run under a lock on the budget row (for example `UPDLOCK` / `SERIALIZABLE` on the budget, or an optimistic retry on its RowVersion).
