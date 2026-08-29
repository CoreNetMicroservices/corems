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

# Record which executions already exist so we can identify the new one afterwards, regardless
# of what `job start` returns (its output shape varies across CLI versions).
executions_json() {
  az containerapp job execution list --name "$JOB" --resource-group "$RG" -o json 2>/dev/null || echo '[]'
}
BEFORE=$(executions_json | jq -r '[.[].name] | @csv' 2>/dev/null || echo "")

# Start the execution, overriding the startup command for this run only. The job image's
# ENTRYPOINT is `dotnet <dll>`; we replace the whole command so the db-admin arg is applied.
# Do NOT suppress stderr — if start fails we need to see why.
echo "Starting job execution..."
set +e
START_OUT=$(az containerapp job start \
  --name "$JOB" \
  --resource-group "$RG" \
  --command "dotnet" "$DLL" "$ARG" \
  -o json 2>&1)
START_RC=$?
set -e
echo "start rc=$START_RC; output:"
echo "$START_OUT"

if [ "$START_RC" -ne 0 ]; then
  echo "::error::'az containerapp job start' failed for $JOB (rc=$START_RC). See output above."
  exit 1
fi

# Prefer the name from the start response; fall back to diffing the execution list.
EXEC_NAME=$(echo "$START_OUT" | jq -r '.name // empty' 2>/dev/null || true)

if [ -z "$EXEC_NAME" ]; then
  echo "start response had no .name; resolving the newly-created execution..."
  for attempt in 1 2 3 4 5 6; do
    sleep 5
    AFTER=$(executions_json)
    # Pick the most recently started execution that wasn't present before.
    EXEC_NAME=$(echo "$AFTER" | jq -r --arg before "$BEFORE" '
      map(select(($before | split(",") | map(gsub("\"";"")) | index(.name)) | not))
      | sort_by(.properties.startTime) | reverse | .[0].name // empty' 2>/dev/null || true)
    [ -n "$EXEC_NAME" ] && break
    # If nothing new is detectable, fall back to the latest overall.
    EXEC_NAME=$(echo "$AFTER" | jq -r 'sort_by(.properties.startTime) | reverse | .[0].name // empty' 2>/dev/null || true)
    [ -n "$EXEC_NAME" ] && break
    echo "  attempt $attempt: no execution yet..."
  done
fi

if [ -z "$EXEC_NAME" ] || [ "$EXEC_NAME" == "null" ]; then
  echo "::error::Could not resolve a job execution name for $JOB after start."
  echo "Current executions:"
  executions_json | jq -r '.[] | "  \(.name)\t\(.properties.status)\t\(.properties.startTime)"' 2>/dev/null || true
  exit 1
fi
echo "Resolved execution: $EXEC_NAME"

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
