:; set -eo pipefail
:; SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &> /dev/null && pwd)
:; "${SCRIPT_DIR}/build.sh" "$@"
:; exit $?

@ECHO OFF
powershell -NoLogo -NoProfile -ExecutionPolicy ByPass -Command "& '%~dp0build.ps1' %*; exit $LASTEXITCODE"
exit /b %ERRORLEVEL%
