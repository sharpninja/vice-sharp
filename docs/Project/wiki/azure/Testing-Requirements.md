# Testing Requirements (MCP Server)

## TEST-DRV-MOTOR

| ID | Requirement |
| --- | --- |
| TEST-DRV-MOTOR-001 | After SetMotor(true) and 300,000 Tick() calls, MotorRotationCycles>0. Before 300,000, stays at 0. Motor off/on resets ramp. ReadSector(18,0) returns BAM bytes 0x12/0x01. 5 pass. |

## TEST-IEC-TIMING

| ID | Requirement |
| --- | --- |
| TEST-IEC-TIMING-001 | After setting IecBus.Atn=false and calling Tick() up to 985 times, Clock==false and Data==false. After ATN release and Tick() up to 985 times, both return true. 5 pass. |

## TEST-TAPE-RAMP

| ID | Requirement |
| --- | --- |
| TEST-TAPE-RAMP-001 | With tape inserted, MotorEnabled=true, PlayPressed=true: TryReadNextPulse returns false during first 12,808 Tick() calls; returns true after 32,000 Tick() calls. Motor off/on resets ramp. 4 pass. |

## TEST-TAPE-SENSE

| ID | Requirement |
| --- | --- |
| TEST-TAPE-SENSE-001 | SenseLine==false when PlayPressed or RecordPressed, true otherwise. TryWritePulse returns true only when MotorEnabled && RecordPressed, incrementing RecordedPulseCount. 7 pass. |

## TEST-VIC-CHECKPOINT

| ID | Requirement |
| --- | --- |
| TEST-VIC-CHECKPOINT-001 | Managed PAL frame periodic raster/cycle-counter (2 Facts), native screen-RAM roundtrip and one-frame DMA read-only (2 ViceFacts), sprite-3 DMA window for 5 models (5 ViceTheory). All 9 pass. |

## TEST-VIC-RC

| ID | Requirement |
| --- | --- |
| TEST-VIC-RC-001 | After writing DEN=1/YSCROLL=0/1 and advancing to specific rasterLine/rasterX, CurrentRowCounter and IsGraphicsIdle match VICE viciisc/vicii-cycle.c:541-563 expectations. All 11 pass. |
