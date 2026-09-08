#!/usr/bin/env bash
# Runs the EditMode test suite in Unity batchmode.
# No Unity editor instance may be open — batchmode needs the project lock.
set -uo pipefail

UNITY="/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity"
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

while kill -0 "$UNITY_PID" 2>/dev/null; do
  [ -f "$RESULTS" ] && cp "$RESULTS" "$SAFE_RESULTS" 2>/dev/null
  sleep 0.2
done
wait "$UNITY_PID"
UNITY_EXIT=$?
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

if [ "$UNITY_EXIT" -ne 0 ]; then
  echo "UNITY EXITED WITH CODE $UNITY_EXIT — treating run as failed regardless of results file content."
  exit 1
fi

exit "$PY_EXIT"
