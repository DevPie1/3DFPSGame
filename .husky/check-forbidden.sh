#!/usr/bin/env bash
bad=$(git diff --cached --name-only | grep -E '^(Library|Temp|Obj|Logs|UserSettings)/')
if [ -n "$bad" ]; then
  echo "❌ 커밋하면 안 되는 폴더가 포함됨:"
  echo "$bad"
  exit 1
fi