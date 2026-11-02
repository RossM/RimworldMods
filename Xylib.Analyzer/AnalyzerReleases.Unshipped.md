; Unreleased analyzer changes

### New Rules

Rule ID   | Category    | Severity | Notes
----------|-------------|----------|------
XYLIB0001 | Style       | Warning  | Disharmony patch classes should be static
XYLIB0002 | Style       | Warning  | Patch methods should document their feature
XYLIB0003 | Style       | Warning  | Feature attributes belong on patch methods
XYLIB0004 | Style       | Warning  | Prefix methods should follow the naming convention
XYLIB0005 | Style       | Warning  | Postfix methods should follow the naming convention
XYLIB0006 | Style       | Warning  | Concrete XML types should declare UsedFromXml
XYLIB0007 | Correctness | Warning  | ConfigErrors implementations should call their base method
XYLIB0008 | Correctness | Warning  | Mutable instance fields appear unsaved
XYLIB0009 | Correctness | Warning  | Save methods should preserve base data
XYLIB0010 | Correctness | Warning  | Use RandBlock instead of calling Rand.PushState(int) directly
XYLIB0011 | Style       | Warning  | Use GeneDef.Extension_GeneWithComps for the cached mod extension
XYLIB0012 | Style       | Warning  | Use Pawn.HasActiveGene instead of calling HasActiveGene directly on Pawn.genes
XYLIB0013 | Style       | Warning  | Use Pawn.FirstActiveGeneOfType instead of calling GetFirstGeneOfType directly on Pawn.genes
