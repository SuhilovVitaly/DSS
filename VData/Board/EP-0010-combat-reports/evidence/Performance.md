# EP-0010 performance acceptance

Status: **NOT MEASURED / OPEN**. User instruction2026-10-16 forbids autonomous game/native-window launches, including performance harnesses. No further runs were started after that instruction.

Target: frame p99 <=12.5ms for80FPS, compare same seed/speed/viewport/hardware with0 versus200 reports. Required p50/p95/p99/max and actual presentation context remain unavailable. Existing native sample has zero measured frames for both baseline0/history200; its targetPassed=false is an unmeasured gate, not a measured FPS failure.

Headless tests prove bounded queues4096, history200, portraitLRU32, cached layout and no repeated decode. They are not GPU/presentation evidence. No80FPS or native performance PASS is claimed.

Prepared owner-operated harness supports --performance:120 warmup +600 measured callbacks for0 reports, then120 warmup +600 for200. Any future result must distinguish callback intervals from physical scanout and retain hardware/VSync context. Agent must not execute this mode autonomously.
