# ApproachingToSessionLevelSignal — approaching profile levels

Source (canonical): `MZpack.NT8\Algo\Strategies\GhostResistance\ApproachingToSessionLevelSignal.cs`. This is a snapshot for learning; edit the original.

Pattern: emits direction `Any` when price approaches session volume profile levels.

What it demonstrates:
- the `strategy.VolumeProfileIndicator.Profiles` source (`IVolumeProfile`), navigation via `.Prior`, `.IsRTH()/.IsETH()`;
- a "gate" signal in an OR sub-branch of the tree (see GhostResistance: building a tree with an OR node);
- two constructor forms (with parameters and an empty one — for XML deserialization);
- `HasPrice = true`.

Note: the file is under `#if DATA` (not `#if STRAT`) — a detail of the product's conditional compilation.
