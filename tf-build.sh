#!/usr/bin/env bash
# Self-healing build/test helper for the TobiFund solution.
# The sandbox periodically wipes the .NET SDK (host/ + shared/) and NuGet cache;
# this script reinstalls/repopulates them before building.
chmod +x /home/user/dotnet-sdk/dotnet 2>/dev/null || true
export PATH=/home/user/dotnet-sdk:/home/user/dotnet-sdk/tools:/home/user/.dotnet/tools:$PATH
export DOTNET_ROOT=/home/user/dotnet-sdk DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export NUGET_PACKAGES=/home/user/nuget-packages

if [ ! -d /home/user/dotnet-sdk/host/fxr ] || [ ! -d /home/user/dotnet-sdk/shared/Microsoft.NETCore.App ]; then
  echo "==> SDK incomplete, reinstalling 8.0.425..."
  rm -rf /home/user/dotnet-sdk
  bash /home/user/dotnet-install.sh --version 8.0.425 --install-dir /home/user/dotnet-sdk >/tmp/sdk-install.log 2>&1
  chmod +x /home/user/dotnet-sdk/dotnet
  echo "==> SDK reinstalled."
fi

if [ ! -f /home/user/nuget-packages/microsoft.entityframeworkcore.sqlite.core/8.0.10/lib/net8.0/Microsoft.EntityFrameworkCore.Sqlite.dll ]; then
  echo "==> NuGet cache incomplete, wiping (restore will repopulate)..."
  rm -rf /home/user/nuget-packages
  mkdir -p /home/user/nuget-packages
fi

cd /home/user/TobiFund || exit 1
ACTION="${1:-all}"

if [ "$ACTION" = "build" ] || [ "$ACTION" = "all" ]; then
  echo "==> BUILD (Release) ==="
  dotnet build TobiFund.sln -c Release || { echo "BUILD FAILED"; exit 1; }
fi

if [ "$ACTION" = "test" ] || [ "$ACTION" = "all" ]; then
  echo "==> TEST ==="
  dotnet test tests/TobiFund.Tests/TobiFund.Tests.csproj -c Release || { echo "TEST FAILED"; exit 1; }
fi
