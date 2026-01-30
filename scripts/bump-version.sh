#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
csproj_path="${repo_root}/PulseAPK.csproj"
about_path="${repo_root}/ViewModels/AboutViewModel.cs"

current_version="$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "${csproj_path}")"

if [[ -z "${current_version}" ]]; then
  echo "Unable to determine current version from ${csproj_path}." >&2
  exit 1
fi

if [[ ! "${current_version}" =~ ^1\.1\.([0-9]+)$ ]]; then
  echo "Version '${current_version}' is not in the expected 1.1.x format." >&2
  exit 1
fi

patch="${BASH_REMATCH[1]}"
next_patch=$((patch + 1))
next_version="1.1.${next_patch}"

perl -0pi -e "s/<Version>[^<]+<\\/Version>/<Version>${next_version}<\\/Version>/" "${csproj_path}"
perl -0pi -e "s/\\?\\? \"1\\.1\\.\\d+\"/?? \"${next_version}\"/" "${about_path}"

echo "Bumped version to ${next_version}."
