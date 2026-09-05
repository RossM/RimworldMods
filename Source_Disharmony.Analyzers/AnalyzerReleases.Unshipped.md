; Unreleased analyzer changes

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
DH0001 | Correctness | Warning | Patch methods cannot contain generic parameters
DH0002 | Correctness | Warning | Patch methods must be static
DH0003 | Correctness | Warning | Prefixes must return bool or void
DH0004 | Correctness | Warning | Postfixes must return void
DH0005 | Correctness | Warning | AlwaysRun prefixes must return void
DH0006 | Correctness | Warning | Patch methods require a discoverable containing class
DH0007 | Correctness | Warning | Patch methods require a target attribute
DH0008 | Correctness | Warning | Direct Disharmony method attributes require a patch type
