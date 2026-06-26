# D64 Test Fixtures

This directory contains D64 disk images used as durable test inputs for drive and native VICE parity work.

Files:

- `Elise.d64`
- `frostpoint.d64`
- `pieces_of_light.d64`

`frostpoint.d64` is the default repository fixture for the long selected-media lockstep gate when `VICESHARP_RUN_SELECTED_D64=1` is set. `VICESHARP_SELECTED_D64` can still point at a different local D64 for ad hoc parity runs.
