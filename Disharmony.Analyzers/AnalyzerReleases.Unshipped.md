; Unreleased analyzer changes

### New Rules

Rule ID        | Category | Severity | Notes
---------------|----------|----------|------
DISHARMONY0001 | Correctness | Warning | Patch methods cannot contain generic parameters
DISHARMONY0002 | Correctness | Warning | Patch methods must be static
DISHARMONY0003 | Correctness | Warning | Prefixes must return bool or void
DISHARMONY0004 | Correctness | Warning | Postfixes must return void
DISHARMONY0005 | Correctness | Warning | AlwaysRun prefixes must return void
DISHARMONY0006 | Correctness | Warning | Patch methods require a discoverable containing class
DISHARMONY0007 | Correctness | Warning | Patch methods require a target attribute
DISHARMONY0008 | Correctness | Warning | Direct Disharmony method attributes require a patch type
DISHARMONY0009 | Correctness | Warning | Multiple patch type attributes
DISHARMONY0010 | Correctness | Warning | Multiple inner target attributes
DISHARMONY0011 | Correctness | Warning | Missing selector type and qualified name
DISHARMONY0012 | Correctness | Warning | Null inner constant
DISHARMONY0014 | Correctness | Warning | Duplicate patch markers or categories, including mixed Disharmony/Harmony attributes
DISHARMONY0015 | Correctness | Warning | Missing non-constructor member name
DISHARMONY0016 | Correctness | Warning | Multiple parameter binding attributes
DISHARMONY0017 | Correctness | Warning | Inner-only binding on an ordinary patch
DISHARMONY0018 | Correctness | Warning | Result binding in an AlwaysRun prefix
DISHARMONY0019 | Correctness | Warning | Exception binding outside an AlwaysRun postfix
DISHARMONY0020 | Correctness | Warning | Method binding requires a concrete delegate passed by value
DISHARMONY0021 | Correctness | Warning | Incompatible binding type for exceptions or inner constants
DISHARMONY0022 | Correctness | Warning | Conflicting types for a shared state key
DISHARMONY0024 | Correctness | Warning | Unavailable instance, argument, or field on an inner constant
DISHARMONY0025 | Correctness | Warning | Void prefix binds the result
DISHARMONY0027 | Correctness | Warning | Unknown special parameter name
DISHARMONY0028 | Correctness | Warning | Multiple parameters bind the same value in a patch
DISHARMONY0029 | Correctness | Warning | State key has no ref or out parameter in its patch class
DISHARMONY0030 | Correctness | Warning | State key is only bound through out parameters in its patch class
DISHARMONY0031 | Correctness | Warning | Patch writes to a parameter passed by value
DISHARMONY0032 | Correctness | Warning | AlwaysRun patch explicitly throws
