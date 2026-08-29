#!/usr/bin/env bash
#
# Run a one-off db-admin operation (--migrate / --seed) against a service by (re)creating a
# manual Container Apps Job whose container command is baked to run the operation, starting it,
# waiting for completion, printing logs, and exiting with a code that reflects success.
#
# The command+args are baked into the job definition at CREATE time (not overridden at start).
# Overriding --command/--args on `az containerapp job start` drops the image from the container
# spec and fails with "must have an 'Image' property specified", so we avoid start-time
# overrides entirely and just start the job plain.
#
# Usage: run-dbadmin-job.sh <job-name> <dll-name> <arg>
#   e.g. run-dbadmin-job.sh user-ms-dbadmin CoreMs.UserMs.Api.dll --migrate
#
# Required env:
#   RESOURCE_GROUP     resource group of the job / container app
#   CONTAINER_APP_ENV  Container Apps environment name
#   JOB_IMAGE          image to run (e.g. myacr.azurecr.io/corems-user-ms:latest)
#   JOB_DB_CONN        DB connection string (stored as a job secret)
#   ACR_SERVER         registry host (e.g. myacr.azurecr.io)
#   ACR_USER           registry username
#   ACR_PASS           registry password
# Azure CLI must already be logged in.

set -euo pipefail

JOB="$1"
DLL="$2"
ARG="$3"

: "${RESOURCE_GROUP:?RESOURCE_GROUP is required}"
: "${CONTAINER_APP_ENV:?CONTAINER_APP_ENV is required}"
: "${JOB_IMAGE:?JOB_IMAGE is required}"
: "${JOB_DB_CONN:?JOB_DB_CONN is required}"
: "${ACR_SERVER:?ACR_SERVER is required}"
: "${ACR_USER:?ACR_USER is required}"
: "${ACR_PASS:?ACR_PASS is required}"
RG="$RESOURCE_GROUP"

echo "==> $JOB : dotnet $DLL $ARG"

# Recreate the job fresh so the baked command matches THIS operation and the image/connection
# are current. Delete any prior definition first (also self-heals a leftover failed job).
if az containerapp job show --name "$JOB" --resource-group "$RG" >/dev/null 2>&1; then
  echo "Deleting existing job $JOB"
  az containerapp job delete --name "$JOB" --resource-group "$RG" --yes >/dev/null
fi

# Bake the command into the job. --command sets the entrypoint (dotnet + dll); --args carries
# the db-admin flag. The --args=... equals form is required because a value starting with "--"
# is otherwise mistaken by az's argparse for another CLI option ("unrecognized arguments").
echo "Creating job $JOB (command: dotnet $DLL $ARG)"
az containerapp job create \
  --name "$JOB" \
  --resource-group "$RG" \
  --environment "$CONTAINER_APP_ENV" \
  --trigger-type Manual \
  --replica-timeout 600 \
  --replica-retry-limit 0 \
  --parallelism 1 \
  --replica-completion-count 1 \
  --cpu 0.5 --memory 1Gi \
  --image "$JOB_IMAGE" \
  --container-name "$JOB" \
  --registry-server "$ACR_SERVER" \
  --registry-username "$ACR_USER" \
  --registry-password "$ACR_PASS" \
  --secrets "db-conn=$JOB_DB_CONN" \
  --env-vars "ConnectionStrings__corems=secretref:db-conn" \
  --command "dotnet" "$DLL" \
  --args="$ARG" >/dev/null

executions_json() {
  az containerapp job execution list --name "$JOB" --resource-group "$RG" -o json 2>/dev/null || echo '[]'
}

# Start the job plain — the command is already baked in, so no start-time override.
echo "Starting job execution..."
set +e
START_OUT=$(az containerapp job start --name "$JOB" --resource-group "$RG" -o json 2>&1)
START_RC=$?
set -e
echo "start rc=$START_RC; output:"
echo "$START_OUT"

if [ "$START_RC" -ne 0 ]; then
  echo "::error::'az containerapp job start' failed for $JOB (rc=$START_RC). See output above."
  exit 1
fi

# Prefer the name from the start response; fall back to the latest execution.
EXEC_NAME=$(echo "$START_OUT" | jq -r '.name // empty' 2>/dev/null || true)
if [ -z "$EXEC_NAME" ]; then
  echo "start response had no .name; resolving the latest execution..."
  for attempt in 1 2 3 4 5 6; do
    sleep 5
    EXEC_NAME=$(executions_json | jq -r 'sort_by(.properties.startTime) | reverse | .[0].name // empty' 2>/dev/null || true)
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

# Fetch logs (best effort — Log Analytics ingestion can lag; the execution status above is the
# authoritative pass/fail signal). --container matches --container-name (== job name).
echo "::group::$JOB $ARG logs"
az containerapp job logs show \
  --name "$JOB" \
  --resource-group "$RG" \
  --execution "$EXEC_NAME" \
  --container "$JOB" \
  --tail 300 --format text 2>/dev/null || echo "(logs not available via Log Analytics yet)"
echo "::endgroup::"

if [ "$STATUS" != "Succeeded" ]; then
  echo "::error::$JOB '$ARG' did not succeed (status=$STATUS). See logs above."
  exit 1
fi

echo "==> $JOB '$ARG' completed successfully."
