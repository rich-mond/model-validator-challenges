# Add order search

Add GET /orders through the existing Minimal API, application service and SQLite
repository. Existing single-order retrieval and creation must continue working.

Optional query parameters:
- status: Pending, Cancelled or Dispatched, case-insensitive. Unknown values
  (including numeric enum values) return HTTP 400.
- createdFrom: inclusive UTC timestamp.
- createdTo: exclusive UTC timestamp.
Use ISO-8601 UTC date-times with seconds, optional fractional seconds, and Z or
+00:00 (URL-encode the plus sign). Invalid timestamps, non-UTC offsets and
createdFrom later than createdTo return HTTP 400. Equal bounds are a valid empty
interval.

Pagination: page is 1-based and defaults to 1; pageSize defaults to 20 and must
be between 1 and 50 inclusive. Invalid integers, page below 1, or pageSize outside
its range return HTTP 400. A page beyond the matching rows is empty.

Apply all filters before pagination. Sort by CreatedUtc descending, then Order
ID ascending for ties. Return JSON { items, total, page, pageSize }; total is the
matching count before pagination. Items use the existing OrderDto fields and
integer minor currency amounts. No matches means items [] and total 0.

Every query must use the authenticated tenant from the existing trusted identity
context. Do not take tenant identity from a query parameter or X-Tenant-Id.
Unauthenticated requests return 401; an order belonging to another tenant remains
404 through existing single-order retrieval. The provided local bearer-token
identity mechanism is already implemented and is not a production auth system;
do not replace it. Tests can use tenant-one-token and tenant-two-token.

Use real SQLite with the pinned existing dependencies and preserve application
conventions. Choose a coherent implementation within the existing layers; do not
add a generic query engine or new infrastructure. Extend public regression
checks as appropriate.
