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
DH0009 | Correctness | Warning | Multiple patch type attributes
DH0010 | Correctness | Warning | Multiple inner target attributes
DH0011 | Correctness | Warning | Missing selector type and qualified name
DH0012 | Correctness | Warning | Null inner constant
DH0014 | Correctness | Warning | Duplicate patch markers or categories, including mixed Disharmony/Harmony attributes
DH0015 | Correctness | Warning | Missing non-constructor member name
DH0016 | Correctness | Warning | Multiple parameter binding attributes
DH0017 | Correctness | Warning | Inner-only binding on an ordinary patch
DH0018 | Correctness | Warning | Result binding in an AlwaysRun prefix
DH0019 | Correctness | Warning | Exception binding outside an AlwaysRun postfix
DH0020 | Correctness | Warning | Method binding requires a concrete delegate passed by value
DH0021 | Correctness | Warning | Incompatible binding type for exceptions or inner constants
DH0022 | Correctness | Warning | Conflicting types for a shared state key
DH0024 | Correctness | Warning | Unavailable instance, argument, or field on an inner constant
DH0025 | Correctness | Warning | Void prefix binds the result
DH0026 | Correctness | Warning | Prefix result binding is not ref or out
DH0027 | Correctness | Warning | Unknown special parameter name
DH0028 | Correctness | Warning | Multiple parameters bind the same value in a patch
DH0029 | Correctness | Warning | State key has no ref or out parameter in its patch class
DH0030 | Correctness | Warning | State key is only bound through out parameters in its patch class
DH0031 | Correctness | Warning | Patch writes to a parameter passed by value
DH0032 | Correctness | Warning | AlwaysRun patch explicitly throws
