# EP-0004 — Graphify final build

Current run2026-10-08, source base6f9a666 plus final US9 TK2 texts. Graph is navigation evidence, not proof of behavior or FPS. Build and validation completed; this run document and three Board run/status documents are explicitly excluded from their own input hash set to avoid self-reference.

[Graph JSON](../../graphify-out/graph.json) · [Report](../../graphify-out/GRAPH_REPORT.md) · [Manifest](../../graphify-out/manifest.json) · [Diagnostics](../../graphify-out/diagnostics.json) · [Verification](../../graphify-out/verification.json) · [Rebuild script](../../graphify-out/rebuild.py).

## Scope and reproducibility

Selected corpus:582 C# files (246 src,312 tests,24 tools) and110 Markdown documents (72 Documentation,35 EP-0004 Board inputs,2 dependency handoffs,1 tooling README), about623319 whitespace-delimited words. Game media, bin/obj, other Board epics apart from handoffs, root workflow wrappers and generated graph artifacts are excluded. No sensitive files were detected in the scan.

Four self-referential run/status files are intentionally outside the hash set: this document, EP-0004 ImplementationStatus.md, US9 parent story and US9 TK2 card. Their final validation/publication notes are written after the export. Runtime source, canonical contracts, all functional cards and their acceptance state were finalized before extraction. The native FPS gate is represented by the indexed canonical contract and epic/US8 documents; it remains OPEN.

AST uses graphify0.9.66 with582 explicit C# inputs, `extract(paths, cache_root=root, root=root, parallel=False)` to avoid Windows process-pool failure.57 initial semantic documents are reused only after verifying their bytes unchanged since the read baseline4233f52; stale changed-source nodes and incident edges were removed.53 documents were read completely and extracted in3 parallel Graphify subagents (22/22/9); no API key required. Edges preserve EXTRACTED/INFERRED/AMBIGUOUS confidence, source path and location. Token counts are not exposed by the host: unmeasured, not zero cost.

Final extraction combines AST, validated semantic fragments, document file containers and explicit Markdown reference edges. A reference/containment edge does not imply a runtime call. `extraction.json` retains directed and duplicate evidence; exported `graph.json` is undirected and necessarily combines multiple relations between a pair. Unresolved AST namespace/type/import references are tagged rather than represented as verified declarations. Source files and line bounds are checked, all692 input SHA256 hashes are validated before export, and an empty graph or unintended shrink is rejected.

For unchanged source bytes, reproduce clustering/export/diagnostics from the audited extraction:

```powershell
Set-Location D:/DeepSpaceSaga/DSS-EP-0004
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe graphify-out/rebuild.py
```

The script refuses changed input hashes. For changed source, rerun Graphify detection and AST/semantic extraction with the same scope and skill extraction-spec, refresh the manifest/extraction, then re-export; the script alone does not re-read or reinterpret changed documentation. New semantic fragments were also saved in the local Graphify cache using the extraction-spec fingerprint. Temporary read/fragment/cache outputs are not part of the delivered graph. HTML is intentionally omitted (`--no-viz` choice) because the symbol graph exceeds5000 nodes; JSON/report/CLI are the supported navigation outputs.

The graph records source base6f9a666 plus the final working-tree text, not a fabricated future commit SHA. The actual final commit is verified after publication; manifest hashes identify its source content. Graph relationships are navigation evidence and never replace automated/native acceptance.

## Final verification — 2026-10-08

10376 nodes,32530 graph edges from33607 extraction edges,320 communities (297 non-thin communities shown in report),7 hyperedges. All692 source hashes match; missing source files0, out-of-range source locations0, AST failed sources0. Dangling/missing endpoints0, self-loops0. Health warning:1077 edges collapse in the undirected graph (1015 would collapse even in a directed simple graph); no evidence is discarded from extraction.json.

2144 AST reference nodes lack a verified declaration location.29 unresolved import stubs follow Graphify's own external-reference representation;24 import edges were bound to unique existing targets, retaining originalTarget and resolution metadata. These are navigation limits, not runtime failures.95 incident edges from stale semantic fragments were removed when changed documents were re-extracted.612 explicit document-content/Markdown bridges connect docs and code. All320 community labels were reviewed against their top nodes and generic method labels replaced with functional names.

Four named navigation edges were verified: AI Generate→temporal Validate, Environment Generate→ValidateWorld, POI Generate→ValidateWorld, and RenderNeverCallsSession→ThrowingConnection. Each retained source line and parser confidence is in verification.json. CLI query `python -m graphify query AiTradePlacementValidator --budget 1000` succeeded:32 nodes found,24 displayed within the budget (truncation explicitly reported). It finds the canonical AiMapEnvironment document as well as the validator and generator.

Re-export after label review produced the same counts and verified every input hash again. Final artifact SHA256 values are in verification.json. Old task-generated read chunks, fragments and temporary graph sidecars were removed after merging; only durable graph/report/manifest/diagnostics/verification/cohesion/labels/extraction/rebuild files are delivered. No API usage cost is asserted.
