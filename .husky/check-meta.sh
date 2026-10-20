#!/usr/bin/env bash
missing=0

# -z: NUL 구분으로 받아 공백/특수문자가 있는 경로도 안전하게 처리
declare -A staged=()
while IFS= read -r -d '' f; do
  staged["$f"]=1
done < <(git diff --cached --name-only --diff-filter=A -z -- Assets)

for f in "${!staged[@]}"; do
  case "$f" in
    *.meta) continue ;;
  esac
  if [[ -z "${staged["$f.meta"]:-}" && ! -f "$f.meta" ]]; then
    echo "❌ .meta 누락: $f"
    missing=1
  fi
done

exit $missing
