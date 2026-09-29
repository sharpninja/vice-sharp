$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\vice-sharp'

function Read-Head([string]$rel) {
    $relUnix = $rel.Replace('\', '/')
    $lines = & git -c core.quotepath=false show "HEAD:$relUnix"
    if ($LASTEXITCODE -ne 0) { throw "git show HEAD:$rel failed" }
    return (($lines | ForEach-Object { $_ }) -join "`n") + "`n"
}

function Write-Working([string]$path, [string]$text) {
    $enc = New-Object System.Text.UTF8Encoding $false
    $normalized = $text -replace "`r`n", "`n"
    if (-not $normalized.EndsWith("`n")) { $normalized += "`n" }
    [IO.File]::WriteAllText($path, ($normalized -replace "`n", "`r`n"), $enc)
}

function Stage-Transformed([string]$rel, [scriptblock]$transform) {
    $full = Join-Path (Get-Location) $rel
    $bak = $full + '.prgslice.bak'
    Copy-Item -LiteralPath $full -Destination $bak -Force
    try {
        $head = Read-Head $rel
        $next = & $transform $head
        Write-Working $full $next
        git add -- $rel
        if ($LASTEXITCODE -ne 0) { throw "git add failed for $rel" }
        Write-Output "STAGED-PARTIAL $rel"
    }
    finally {
        Copy-Item -LiteralPath $bak -Destination $full -Force
        Remove-Item -LiteralPath $bak -Force
    }
}

$clean = @(
    'src/ViceSharp.Host.InProcess/Runtime/PrgMemoryLoader.cs',
    'src/ViceSharp.Avalonia/Host/IHostProtocolClient.cs',
    'src/ViceSharp.Avalonia/Host/DisconnectedHostProtocolClient.cs',
    'src/ViceSharp.Avalonia/ViewModels/ShellViewModel.cs',
    'src/ViceSharp.Protocol/ProtocolContracts.cs',
    'src/ViceSharp.Host.InProcess/Services/EmulatorHostService.cs',
    'src/ViceSharp.Host.InProcess/Runtime/HostKeyboardAutomation.cs',
    'tests/ViceSharp.TestHarness/PrgMemoryLoaderTests.cs',
    'tests/ViceSharp.TestHarness/EmulatorHostLoadProgramTests.cs',
    'tests/ViceSharp.TestHarness/AvaloniaBoundaryTests.cs',
    'tests/ViceSharp.TestHarness/DisconnectedHostProtocolClientTests.cs',
    'tests/ViceSharp.TestHarness/GrpcContractTests.cs',
    'tests/ViceSharp.TestHarness/HostKeyboardAutomationTests.cs',
    'tests/ViceSharp.TestHarness/ShellViewModelTests.cs',
    'tests/ViceSharp.TestHarness/Xbox/XboxAppCommandDispatcherTests.cs',
    'docs/wiki.yaml',
    'docs/README.md',
    'docs/VICE-MIGRATION.md',
    'docs/wireframes/desktop-windows.md',
    'docs/requirements/functional/FR-Host-UI-Boundary.md',
    'docs/requirements/technical/TR-GRPC-Boundary.md',
    'docs/requirements/technical/TR-Host-Prg.md',
    'docs/receipts/hostile-validator-20260910T231142Z.md',
    'docs/receipts/hostile-validator-20260910T231142Z.json',
    'docs/Project/wiki/github/TR-Host-Prg.md'
)
# Clean files may already be staged from a prior run.
git add -- @clean
Write-Output "STAGED-CLEAN $($clean.Count)"

Stage-Transformed 'src/ViceSharp.Avalonia/MainWindow.axaml.cs' {
    param($h)
    $old = "        e.DragEffects = string.IsNullOrWhiteSpace(GetDroppedLocalFilePath(e))`n            ? DragDropEffects.None`n            : DragDropEffects.Copy;"
    $new = "        e.DragEffects = _shell.IsDropStartSupported(GetDroppedLocalFilePath(e))`n            ? DragDropEffects.Copy`n            : DragDropEffects.None;"
    if ($h.IndexOf($old) -lt 0) { throw 'MainWindow DragOver HEAD text not found' }
    $h.Replace($old, $new)
}

Stage-Transformed 'src/ViceSharp.Protocol/Protos/emulator_host.proto' {
    param($h)
    $h2 = $h.Replace(
        "  rpc ResetAndAutostartDrive8(ResetAndAutostartDrive8Request) returns (EmulatorCommandResponse);`n  rpc StepCycle",
        "  rpc ResetAndAutostartDrive8(ResetAndAutostartDrive8Request) returns (EmulatorCommandResponse);`n  rpc LoadProgram(LoadProgramRequest) returns (LoadProgramResponse);`n  rpc StepCycle")
    $insert = @"

message LoadProgramRequest {
  string session_id = 1;
  string file_path = 2;
  bytes payload = 3;
  string display_name = 4;
}

message LoadProgramResponse {
  RpcStatus status = 1;
  uint32 load_address = 2;
  uint32 byte_count = 3;
  bool ran = 4;
  EmulatorStatusDto emulator_status = 5;
}

"@
    $needle = "message ResetAndAutostartDrive8Request {`n  string session_id = 1;`n}`n"
    $idx = $h2.IndexOf($needle)
    if ($idx -lt 0) { throw 'proto ResetAndAutostart message not found' }
    $h2.Insert($idx + $needle.Length, $insert)
}

Stage-Transformed 'src/ViceSharp.Avalonia/Host/GrpcHostProtocolClient.cs' {
    param($h)
    $h2 = $h
    if ($h2 -notmatch '(?m)^using System\.IO;') {
        $h2 = $h2.Replace("using Google.Protobuf;", "using System.IO;`nusing Google.Protobuf;")
    }
    $old = @"
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(double ratePercent, CancellationToken cancellationToken = default)
"@
    $new = @"
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<LoadProgramResponse> LoadProgramAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var sessionId = await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        var response = await _hostClient.LoadProgramAsync(
            new GrpcContracts.LoadProgramRequest
            {
                SessionId = sessionId,
                FilePath = filePath ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(filePath) ? string.Empty : Path.GetFileName(filePath)
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new LoadProgramResponse(
            MapStatus(response.Status),
            (int)response.LoadAddress,
            (int)response.ByteCount,
            response.Ran,
            MapStatusDto(response.EmulatorStatus));
    }

    public async ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(double ratePercent, CancellationToken cancellationToken = default)
"@
    if ($h2.IndexOf($old) -lt 0) { throw 'GrpcHostProtocolClient insert point not found' }
    $h2.Replace($old, $new)
}

Stage-Transformed 'src/ViceSharp.Host/Services/GrpcHostServiceAdapters.cs' {
    param($h)
    $old = @"
        => MapCommandAsync(_inner.ResetAndAutostartDrive8Async(new ResetAndAutostartDrive8Request(request.SessionId), context.CancellationToken));

    public override Task<GrpcContracts.EmulatorCommandResponse> StepCycle(
"@
    $new = @"
        => MapCommandAsync(_inner.ResetAndAutostartDrive8Async(new ResetAndAutostartDrive8Request(request.SessionId), context.CancellationToken));

    public override async Task<GrpcContracts.LoadProgramResponse> LoadProgram(
        GrpcContracts.LoadProgramRequest request,
        ServerCallContext context)
    {
        var response = await _inner.LoadProgramAsync(
            new LoadProgramRequest(
                request.SessionId,
                request.FilePath ?? string.Empty,
                request.Payload.IsEmpty ? Array.Empty<byte>() : request.Payload.ToByteArray(),
                request.DisplayName ?? string.Empty),
            context.CancellationToken).ConfigureAwait(false);

        return new GrpcContracts.LoadProgramResponse
        {
            Status = HostMap.Map(response.Status),
            LoadAddress = (uint)Math.Max(0, response.LoadAddress),
            ByteCount = (uint)Math.Max(0, response.ByteCount),
            Ran = response.Ran,
            EmulatorStatus = HostMap.Map(response.EmulatorStatus)
        };
    }

    public override Task<GrpcContracts.EmulatorCommandResponse> StepCycle(
"@
    if ($h.IndexOf($old) -lt 0) { throw 'GrpcHostServiceAdapters insert point not found' }
    $h.Replace($old, $new)
}

Stage-Transformed 'README.md' {
    param($h)
    $old = '- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) - install, first run, CLI launcher, YAML topology, disk images, capture, diagnostics attach, what works today'
    $new = '- [docs/USER-GUIDE.md](docs/USER-GUIDE.md) - install, first run, CLI launcher, YAML topology, disk images, desktop drag-drop (including `*.prg` load/RUN), capture, diagnostics attach, what works today'
    if ($h.IndexOf($old) -lt 0) { throw 'README user-guide bullet not found' }
    $h.Replace($old, $new)
}

Stage-Transformed 'docs/USER-GUIDE.md' {
    param($h)
    $block = @"

## 6b. Avalonia desktop: attach, drop, and PRG load

Launch the desktop UI with Nuke ``RunAvalonia``, or install the Release MSI (``./build.ps1 InstallMsi --configuration Release``) and start ViceSharp from the Start menu.

The attach panel Browse buttons pick disk, tape, and cartridge images for the usual slots. You can also drop a local file onto the emulator video surface:

- ``.d64`` / ``.g64`` / ``.t64`` attach to drive 8 and reset/autostart (``LOAD"*",8,1`` then ``RUN``).
- ``.crt`` / ``.bin`` / ``.rom`` attach as a cartridge and cold-reset.
- ``.prg`` does **not** attach as media. The host loads the file into the **current** session RAM at the PRG little-endian load address and does not reset. If that address equals the live BASIC start (zero-page TXTTAB at ``$2B/$2C``), BASIC end pointers are updated and the host types ``RUN``. Otherwise the bytes are loaded only.

BASIC start is whatever the running machine wrote into TXTTAB after boot, so it follows the current machine and VIC-20 memory config (C64 ``$0801``; VIC-20 unexpanded ``$1001``, +3K ``$0401``, +8K and above ``$1201``). A C64 ``$0801`` BASIC PRG dropped on a VIC-20 unexpanded session is loaded and is not RUN.

Unsupported extensions (for example ``.txt``) are rejected on drag-over.

"@
    # The here-string used doubled backticks so the script file stores single backticks.
    $insert = $block.Replace('``', '`')
    $anchor = "## 7. What works today / what doesn't"
    $idx = $h.IndexOf($anchor)
    if ($idx -lt 0) { throw 'USER-GUIDE section 7 not found' }
    $h2 = $h.Insert($idx, $insert)
    $oldRow = '| Host UI (Avalonia desktop + Console; gRPC control) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Supported product shells: Avalonia desktop and Console. |'
    $newRow = '| Host UI (Avalonia desktop + Console; gRPC control) | Working core | Host-owned gRPC services, monitor/control adapters, view models, registry, frame source, generated clients, and in-process host are covered. Supported product shells: Avalonia desktop and Console. Video-surface drop accepts disks/carts (attach + boot) and `*.prg` (load into current RAM; RUN when load address equals live TXTTAB). |'
    if ($h2.IndexOf($oldRow) -lt 0) { throw 'USER-GUIDE Host UI row not found' }
    $h2.Replace($oldRow, $newRow)
}

Stage-Transformed 'docs/requirements/test/TEST-Requirements.md' {
    param($h)
    $add = @"

## TEST-UIDROP-002: PRG drag-drop load and BASIC RUN tests

**ID:** TEST-UIDROP-002
**Title:** PRG drag-drop load and BASIC RUN tests
**Priority:** P1 -- Important

### Condition

Verify PRG drop acceptance and host RAM load: ``ShellViewModel`` routes ``*.prg`` to ``LoadProgramAsync`` without attach/reset; ``IsDropStartSupported`` is true for ``.prg`` and existing media and false for unsupported types; ``PrgMemoryLoader`` writes at the load address, updates BASIC pointers only when load equals TXTTAB, and reports ran; ``EmulatorHostService.LoadProgramAsync`` loads payload into the session, sets Ran, and starts BASIC RUN automation only for BASIC-start PRGs; invalid PRGs return InvalidArgument; ``GrpcEmulatorHostService`` maps LoadProgram request/response fields.

### Traceability

- **Related FR Area(s):** FR-UIDROP-002, FR-CFG-005
- **Canonical FR IDs:** FR-UIDROP-002
- **Technical Requirements:** TR-HOST-PRG-001
"@
    $add = $add.Replace('``', '`')
    $trimmed = $h.TrimEnd("`r", "`n")
    $trimmed + "`n" + $add
}

Stage-Transformed 'tests/ViceSharp.TestHarness/GrpcHostServiceAdaptersTests.cs' {
    param($h)
    $test = @"

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a gRPC LoadProgram call must marshal path, payload, load
    /// address, byte count, and Ran back across the adapter.
    /// Acceptance: the inner host sees the proto session, path, and payload,
    /// and the proto response echoes load address 0x0801, byte count 12, and
    /// Ran true.
    /// </summary>
    [Fact]
    public async Task GrpcEmulatorHostService_LoadProgram_MarshalsPayloadAndRan()
    {
        var payload = new byte[] { 0x01, 0x08, 0x00 };
        var fake = new FakeEmulatorHost
        {
            LoadProgramResponse = new LoadProgramResponse(
                RpcStatus.Ok(),
                0x0801,
                12,
                true,
                new EmulatorStatusDto(
                    "session-42",
                    "minimal",
                    EmulatorRunState.Running,
                    0,
                    new MachineStateDto(0, 0, 0, 0, 0, 0, 0)))
        };
        var adapter = new GrpcEmulatorHostService(fake);

        var response = await adapter.LoadProgram(
            new GrpcContracts.LoadProgramRequest
            {
                SessionId = "session-42",
                FilePath = "hello.prg",
                DisplayName = "hello.prg",
                Payload = ByteString.CopyFrom(payload)
            },
            CreateContext());

        Assert.NotNull(fake.LastLoadProgramRequest);
        Assert.Equal("session-42", fake.LastLoadProgramRequest.SessionId);
        Assert.Equal("hello.prg", fake.LastLoadProgramRequest.FilePath);
        Assert.Equal(payload, fake.LastLoadProgramRequest.Payload);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(0x0801u, response.LoadAddress);
        Assert.Equal(12u, response.ByteCount);
        Assert.True(response.Ran);
    }

"@
    $anchor = "        Assert.Equal(`"session-42`", response.EmulatorStatus.SessionId);`n    }`n`n    /// <summary>`n    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters)."
    $idx = $h.IndexOf($anchor)
    if ($idx -lt 0) { throw 'adapter test insert after CreateSession not found' }
    $insertAt = $idx + "        Assert.Equal(`"session-42`", response.EmulatorStatus.SessionId);`n    }`n".Length
    $h2 = $h.Insert($insertAt, $test)

    $props = @"
        public EmulatorCommandResponse CommandResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public Exception? CommandException { get; set; }
"@
    $propsNew = @"
        public EmulatorCommandResponse CommandResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public LoadProgramResponse LoadProgramResponse { get; set; } =
            new(RpcStatus.Ok(), 0, 0, false, null);
        public LoadProgramRequest? LastLoadProgramRequest { get; private set; }
        public Exception? CommandException { get; set; }
"@
    if ($h2.IndexOf($props) -lt 0) { throw 'FakeEmulatorHost CommandResponse block not found' }
    $h2 = $h2.Replace($props, $propsNew)

    $method = @"
        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(ResetAndAutostartDrive8Request request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> StepCycleAsync(
"@
    $methodNew = @"
        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(ResetAndAutostartDrive8Request request, CancellationToken cancellationToken = default) => Command(cancellationToken);

        public ValueTask<LoadProgramResponse> LoadProgramAsync(LoadProgramRequest request, CancellationToken cancellationToken = default)
        {
            LastCommandToken = cancellationToken;
            LastLoadProgramRequest = request;
            if (CommandException is not null)
                throw CommandException;
            return ValueTask.FromResult(LoadProgramResponse);
        }
        public ValueTask<EmulatorCommandResponse> StepCycleAsync(
"@
    # HEAD may have StepCycle on same line or next; match actual HEAD
    if ($h2.IndexOf('ResetAndAutostartDrive8Async(ResetAndAutostartDrive8Request request, CancellationToken cancellationToken = default) => Command(cancellationToken);') -lt 0) {
        throw 'FakeEmulatorHost ResetAndAutostart not found'
    }
    $h2 = $h2.Replace(
        '        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(ResetAndAutostartDrive8Request request, CancellationToken cancellationToken = default) => Command(cancellationToken);',
        @"
        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(ResetAndAutostartDrive8Request request, CancellationToken cancellationToken = default) => Command(cancellationToken);

        public ValueTask<LoadProgramResponse> LoadProgramAsync(LoadProgramRequest request, CancellationToken cancellationToken = default)
        {
            LastCommandToken = cancellationToken;
            LastLoadProgramRequest = request;
            if (CommandException is not null)
                throw CommandException;
            return ValueTask.FromResult(LoadProgramResponse);
        }
"@)
    $h2
}

Write-Output 'STAGE-SCRIPT-DONE'
git diff --cached --stat
