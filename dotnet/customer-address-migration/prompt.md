# Support multiple customer addresses

Upgrade this existing SQLite customer application from schema version one to
version two using DatabaseMigrator and its schema_versions history. Retain
version-one history and append version two exactly once after a successful upgrade. Existing
populated databases and fresh databases must both work. Reapplying the migration
must not duplicate data. Apply the upgrade transactionally, including the version
record: a failed upgrade must leave the old schema and data intact.

Preserve all customer IDs, names and original address values. Move existing
non-null single addresses into a separate relationship as primary addresses.
Customers with no address remain supported. Each customer that has addresses
must have exactly one primary after every successful supported operation.

The database-facing relationship is customer_addresses with columns id,
customer_id, line1, city, postal_code, country_code and is_primary (0/1). Address
IDs are unique nonempty strings and customer_id refers to customers.id. Internal
implementation and constraint choices are yours; do not introduce a second
migration mechanism. Legacy address columns may remain; the existing retrieval contract must always
reflect the selected primary address.

Extend CustomerService with these synchronous public APIs:
- GetAddresses(string customerId): all CustomerAddress values for that customer;
  empty when it has no addresses. No list order is required.
- AddAddress(string customerId, Address address, bool makePrimary = false): return
  the new CustomerAddress. The first address automatically becomes primary;
  otherwise preserve the existing primary unless makePrimary is true. An unknown
  customer must be rejected with ArgumentException without adding an address.
- SetPrimaryAddress(string customerId, string addressId): true for a valid owned
  address (including selecting it again), false for unknown/foreign addresses;
  invalid selection must leave the current primary unchanged.
- GetPrimaryAddress(string customerId): its CustomerAddress or null when absent.

CustomerAddress exposes Id, CustomerId, Line1, City, PostalCode, CountryCode and
bool IsPrimary. Keep the existing Address record and CustomerView shape.
GetCustomer must still return the current primary as its legacy single Address,
or null when none exists. Preserve CreateCustomer, RenameCustomer, SetAddress,
the customer directory and CSV export. SetAddress updates the current primary,
or establishes the first address when there is none, without deleting others.

No rollback to version one or operation by an old unmodified binary is required.
Keep .NET 10, SQLite and the existing pinned packages. Do not add external
services or redesign the application. Extend public regression checks as needed.
