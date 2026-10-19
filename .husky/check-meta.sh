#!/usr/bin/env bash
missing=0
staged=$(git diff --cached --name-only --diff-filter=A -- Assets)

for f in $staged; do
  case "$f" in
    *.meta) continue ;;
  esac
  if ! echo "$staged" | grep -qx "$f.meta" && [ ! -f "$f.meta" ]; then
    echo "❌ .meta 누락: $f"
    missing=1
  fi
done

exit $missing