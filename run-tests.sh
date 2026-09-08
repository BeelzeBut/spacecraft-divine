#!/usr/bin/env bash
# Runs the EditMode test suite in Unity batchmode.
# No Unity editor instance may be open — batchmode needs the project lock.
set -uo pipefail

UNITY="/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS="$PROJECT/Temp/editmode-results.xml"
LOG="/tmp/spaceship-divine-run-tests.log"
# On this machine, Unity's batchmode process deletes its own project Temp/ directory
# (results file included) as part of its normal exit cleanup, before this script gets
# control back. Copy the results out from under it (see poll loop below) rather than
# reading $RESULTS directly once Unity has exited.
SAFE_RESULTS="/tmp/spaceship-divine-editmode-results.xml"

rm -f "$SAFE_RESULTS"

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
[ -f "$RESULTS" ] && cp "$RESULTS" "$SAFE_RESULTS" 2>/dev/null

tail -40 "$LOG" 2>/dev/null

if [ ! -f "$SAFE_RESULTS" ]; then
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
