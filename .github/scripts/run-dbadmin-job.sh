#!/usr/bin/env bash
#
# Start a Container Apps Job execution that runs the service image with a db-admin arg
# (--migrate / --seed), wait for it to finish, print its logs, and exit with a code that
# reflects whether the operation actually succeeded.
#
# Usage: run-dbadmin-job.sh <job-name> <dll-name> <arg>
#   e.g. run-dbadmin-job.sh user-ms-dbadmin CoreMs.UserMs.Api.dll --seed
#
# Requires env: RESOURCE_GROUP. Azure CLI must already be logged in.

set -euo pipefail

JOB="$1"
DLL="$2"
ARG="$3"
RG="${RESOURCE_GROUP:?RESOURCE_GROUP env var is required}"

echo "==> $JOB : dotnet $DLL $ARG"

# Start the execution, overriding the startup command for this run only. The job image's
# ENTRYPOINT is `dotnet <dll>`; we replace the whole command so the db-admin arg is applied.
EXEC_NAME=$(az containerapp job start \
  --name "$JOB" \
  --resource-group "$RG" \
  --command "dotnet" "$DLL" "$ARG" \
  --query "name" -o tsv 2>/dev/null || true)

# Fallback: some CLI versions don't return the execution name from `start`; take the latest.
if [ -z "$EXEC_NAME" ] || [ "$EXEC_NAME" == "null" ]; then
  echo "start did not return an execution name; resolving latest execution..."
  sleep 3
  EXEC_NAME=$(az containerapp job execution list \
    --name "$JOB" \
    --resource-group "$RG" \
    --query "[0].name" -o tsv 2>/dev/null || true)
fi

if [ -z "$EXEC_NAME" ] || [ "$EXEC_NAME" == "null" ]; then
  echo "::error::Failed to start or resolve a job execution for $JOB"
  exit 1
fi
echo "Started execution: $EXEC_NAME"

# Poll for terminal status (Succeeded / Failed). Timeout after ~12 min.
STATUS="Running"
for i in $(seq 1 144); do
  STATUS=$(az containerapp job execution show \
    --name "$JOB" \
    --resource-group "$RG" \
    --job-execution-name "$EXEC_NAME" \
    --query "properties.status" -o tsv 2>/dev/null || echo "Unknown")
  echo "  [$i] status=$STATUS"
  case "$STATUS" in
    Succeeded|Failed) break ;;
  esac
  sleep 5
done

# Fetch logs for the execution (best effort — Log Analytics ingestion can lag, so this is
# supplementary; the authoritative pass/fail signal is the execution status above).
# --container matches --container-name set at job creation.
echo "::group::$JOB $ARG logs"
az containerapp job logs show \
  --name "$JOB" \
  --resource-group "$RG" \
  --execution "$EXEC_NAME" \
  --container dbadmin \
  --tail 300 --format text 2>/dev/null || echo "(logs not available via Log Analytics yet)"
echo "::endgroup::"

if [ "$STATUS" != "Succeeded" ]; then
  echo "::error::$JOB '$ARG' did not succeed (status=$STATUS). See logs above."
  exit 1
fi

echo "==> $JOB '$ARG' completed successfully."
