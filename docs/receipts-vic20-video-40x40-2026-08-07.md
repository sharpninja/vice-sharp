# Receipt: VIC-20 video stuck 40x40 (2026-08-07)

## Symptom
Xbox VIC-20: black / no video. Log: pull.Tick()=false, first present src 40x40, FPS 0.0 EMU 0.0 forever.

## Root cause
1. Mos6561.RenderCharacterFrame used Math.Clamp(reg,1,32) on zero power-on / -> 1x1 chars = 40x40.
2. VideoFramePullViewModel allocated once from first geometry; after KERNAL expanded to 208x216, TryCopyFrameInto failed (dest too small) forever.

Host pump was fine (Running, frames at 208x216 after boot).

## Fix
- Mos6561: zero regs keep ConfigureTiming defaults (22x23)
- VideoFramePullViewModel: grow buffer when geometry expands

## Tests
14 passed (Vic20VideoPumpSmoke, VideoFramePullViewModel growth, Vic20VideoTests)
