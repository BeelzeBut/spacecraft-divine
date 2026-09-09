#!/usr/bin/env bash
# Runs the EditMode test suite in Unity batchmode.
# No Unity editor instance may be open — batchmode needs the project lock.
set -uo pipefail

# Overridable so an editor upgrade does not need this file edited, and so the same
# harness can gate a migration by running against the OLD and NEW editor in turn:
#   UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity ./run-tests.sh
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity}"

if [ ! -x "$UNITY" ]; then
  echo "Unity not found or not executable: $UNITY"
  echo "Set UNITY=/path/to/Unity.app/Contents/MacOS/Unity"
  exit 1
fi
echo "Using editor: $UNITY"
PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS="$PROJECT/Temp/editmode-results.xml"
# On this machine, Unity's batchmode process deletes its own project Temp/ directory
# (results file included) as part of its normal exit cleanup, before this script gets
# control back. Copy the results out from under it (see poll loop below) rather than
# reading $RESULTS directly once Unity has exited. Unique per invocation (mktemp) so
# overlapping runs never cross-report each other's results.
LOG="$(mktemp /tmp/spaceship-divine-run-tests.log.XXXXXX)"
SAFE_RESULTS="$(mktemp /tmp/spaceship-divine-editmode-results.xml.XXXXXX)"
rm -f "$SAFE_RESULTS"

# Clear any stale results left at $RESULTS from a prior run that crashed or was killed
# before Unity's own cleanup fired — otherwise the poll loop's first iteration below
# would copy that stale file out as if it were this run's outcome.
rm -f "$RESULTS"

"$UNITY" \
  -runTests \
  -batchmode \
  -projectPath "$PROJECT" \
  -testPlatform EditMode \
  -testResults "$RESULTS" \
  -logFile "$LOG" &
UNITY_PID=$!

# Overall deadline for the whole run (results appearing + Unity exiting). A normal
# run here is 3-5 minutes; 900s is generous headroom. If Unity finishes the tests but
# hangs on exit (a known Unity batchmode issue on this machine), we don't want to
# block forever on `wait` — so we poll with a deadline instead.
DEADLINE=900
GRACE=20
start_ts=$(date +%s)
UNITY_EXIT=""
results_seen_ts=""

while kill -0 "$UNITY_PID" 2>/dev/null; do
  if [ -f "$RESULTS" ]; then
    cp "$RESULTS" "$SAFE_RESULTS" 2>/dev/null
    [ -z "$results_seen_ts" ] && results_seen_ts=$(date +%s)
  fi

  now=$(date +%s)
  elapsed=$(( now - start_ts ))

  if [ -n "$results_seen_ts" ] && [ $(( now - results_seen_ts )) -ge "$GRACE" ]; then
    echo "Results file captured but Unity is still running ${GRACE}s later — this is the known" \
         "batchmode hang-on-exit. Killing Unity (pid $UNITY_PID) and proceeding with captured results."
    kill -9 "$UNITY_PID" 2>/dev/null
    wait "$UNITY_PID" 2>/dev/null
    UNITY_EXIT=0   # tests already ran to completion; the hang is exit-only, not a failure
    break
  fi

  if [ "$elapsed" -ge "$DEADLINE" ]; then
    if [ -n "$results_seen_ts" ]; then
      echo "Deadline (${DEADLINE}s) reached with results already captured. Killing Unity (pid $UNITY_PID)."
      kill -9 "$UNITY_PID" 2>/dev/null
      wait "$UNITY_PID" 2>/dev/null
      UNITY_EXIT=0
    else
      echo "Deadline (${DEADLINE}s) reached with NO results file. Killing Unity (pid $UNITY_PID)."
      kill -9 "$UNITY_PID" 2>/dev/null
      wait "$UNITY_PID" 2>/dev/null
      tail -40 "$LOG" 2>/dev/null
      echo "NO RESULTS FILE — Unity failed to start or compile within the deadline. See log above."
      exit 1
    fi
    break
  fi

  sleep 0.2
done

if [ -z "$UNITY_EXIT" ]; then
  # Unity exited on its own before either the grace period or the deadline fired.
  wait "$UNITY_PID"
  UNITY_EXIT=$?
fi
[ -f "$RESULTS" ] && cp "$RESULTS" "$SAFE_RESULTS" 2>/dev/null

tail -40 "$LOG" 2>/dev/null

if [ ! -f "$SAFE_RESULTS" ] || [ ! -s "$SAFE_RESULTS" ]; then
  echo "NO RESULTS FILE — Unity failed to start or compile. See log above."
  exit 1
fi

python3 - "$SAFE_RESULTS" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
total  = r.get('total', '0')
passed = r.get('passed', '0')
failed = r.get('failed', '0')
print(f"\n=== total={total} passed={passed} failed={failed} ===")
for tc in r.iter('test-case'):
    if tc.get('result') != 'Passed':
        print(f"FAIL: {tc.get('fullname')}")
        f = tc.find('failure/message')
        if f is not None and f.text:
            print(f"      {f.text.strip()[:400]}")
sys.exit(1 if failed != '0' or total == '0' else 0)
PY
PY_EXIT=$?

FINAL_EXIT="$PY_EXIT"
if [ "$UNITY_EXIT" -ne 0 ]; then
  echo "UNITY EXITED WITH CODE $UNITY_EXIT — treating run as failed regardless of results file content."
  FINAL_EXIT=1
fi

# Only clean up our own log/temp-results on a successful run — keep them around on
# failure so there's something to inspect.
if [ "$FINAL_EXIT" -eq 0 ]; then
  rm -f "$LOG" "$SAFE_RESULTS"
fi

exit "$FINAL_EXIT"
