#!/usr/bin/env bash

set -Eeuo pipefail

info() {
  printf '\033[1;34m[INFO]\033[0m %s\n' "$1"
}

success() {
  printf '\033[1;32m[OK]\033[0m %s\n' "$1"
}

warn() {
  printf '\033[1;33m[WARN]\033[0m %s\n' "$1"
}

error() {
  printf '\033[1;31m[ERROR]\033[0m %s\n' "$1" >&2
}

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

# scripts/setup.sh の1階層上をリポジトリルートとして扱う。
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

info "Checking required commands..."

if ! command_exists git; then
  error "Git is not installed."
  exit 1
fi

if ! command_exists dotnet; then
  error ".NET SDK is not installed."
  error "Install the .NET SDK and run this script again."
  error "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-osx-arm64.pkg"
  exit 1
fi

success "Required commands are available."

info "Checking repository..."

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  error "This directory is not a Git repository."
  exit 1
fi

if [[ ! -f "ProjectSettings/ProjectVersion.txt" ]]; then
  error "ProjectSettings/ProjectVersion.txt was not found."
  error "Run this script from the Unity project repository."
  exit 1
fi

if [[ ! -f "unity1week-2026-07.slnx" ]] ||
  [[ ! -f "Assembly-CSharp.csproj" ]]; then
  error "Unity-generated .slnx / .csproj files were not found."
  error "Open the project in Unity Editor once, then run this script again."
  exit 1
fi

success "Git repository detected."

if [[ -f ".config/dotnet-tools.json" ]]; then
  info "Restoring repository-local .NET tools..."
  dotnet tool restore
  success ".NET tools restored."
else
  warn ".config/dotnet-tools.json was not found."
  warn "CSharpier and other local .NET tools were not restored."
fi

info "Configuring repository-local Git settings..."
git config --local commit.template .github/commit-message-template.txt
git config --local core.hooksPath .githooks
chmod +x .githooks/pre-commit .githooks/pre-push scripts/setup.sh scripts/setup.ps1
success "Commit template and Git hooks configured."

UNITY_VERSION="$(
  sed -n 's/^m_EditorVersion:[[:space:]]*//p' \
    ProjectSettings/ProjectVersion.txt |
    head -n 1 |
    tr -d '\r'
)"

if [[ -z "$UNITY_VERSION" ]]; then
  error "Could not read the Unity version from ProjectVersion.txt."
  exit 1
fi

info "Unity version: $UNITY_VERSION"

find_unity_yaml_merge() {
  local candidates=()

  if [[ -n "${UNITY_YAML_MERGE_PATH:-}" ]]; then
    candidates+=("$UNITY_YAML_MERGE_PATH")
  fi

  if [[ -n "${UNITY_EDITOR_PATH:-}" ]]; then
    candidates+=(
      "$(dirname "$UNITY_EDITOR_PATH")/Data/Tools/UnityYAMLMerge.exe"
      "$(dirname "$UNITY_EDITOR_PATH")/Data/Tools/UnityYAMLMerge"
      "$(dirname "$UNITY_EDITOR_PATH")/../Tools/UnityYAMLMerge"
    )
  fi

  case "$(uname -s)" in
    Darwin*)
      candidates+=(
        "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/Helpers/UnityYAMLMerge"
        "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/Tools/UnityYAMLMerge"
        "$HOME/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/Helpers/UnityYAMLMerge"
        "$HOME/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/Tools/UnityYAMLMerge"
      )
      ;;

    Linux*)
      candidates+=(
        "$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/Tools/UnityYAMLMerge"
        "/opt/unity/editors/$UNITY_VERSION/Editor/Data/Tools/UnityYAMLMerge"
      )
      ;;

    MINGW* | MSYS* | CYGWIN*)
      candidates+=(
        "/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/Tools/UnityYAMLMerge.exe"
        "/c/Program Files (x86)/Unity/Hub/Editor/$UNITY_VERSION/Editor/Data/Tools/UnityYAMLMerge.exe"
      )
      ;;

    *)
      warn "Unknown operating system: $(uname -s)"
      ;;
  esac

  local candidate
  for candidate in "${candidates[@]}"; do
    if [[ -f "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

info "Searching for UnityYAMLMerge..."

if UNITY_YAML_MERGE="$(find_unity_yaml_merge)"; then
  success "UnityYAMLMerge found:"
  printf '  %s\n' "$UNITY_YAML_MERGE"
else
  error "UnityYAMLMerge was not found."
  error "Make sure Unity $UNITY_VERSION is installed through Unity Hub."
  error ""
  error "You can explicitly provide the path:"
  error "  UNITY_YAML_MERGE_PATH=\"/path/to/UnityYAMLMerge\" ./scripts/setup.sh"
  exit 1
fi

GIT_DIR="$(git rev-parse --absolute-git-dir)"
MERGE_DRIVER="$GIT_DIR/unityyamlmerge-driver.sh"

info "Creating UnityYAMLMerge wrapper..."

cat >"$MERGE_DRIVER" <<EOF
#!/usr/bin/env bash
set -Eeuo pipefail

UNITY_YAML_MERGE=$(printf '%q' "$UNITY_YAML_MERGE")

exec "\$UNITY_YAML_MERGE" merge -p "\$1" "\$2" "\$3" "\$4"
EOF

chmod +x "$MERGE_DRIVER"
success "UnityYAMLMerge wrapper created."

info "Registering UnityYAMLMerge with Git..."

# %O: 共通祖先
# %A: 現在のブランチ側。マージ結果の出力先でもある
# %B: マージ対象ブランチ側
git config --local merge.unityyamlmerge.name "Unity Smart Merge"
git config --local merge.unityyamlmerge.driver \
  "\"$MERGE_DRIVER\" %O %B %A %A"
git config --local merge.unityyamlmerge.recursive binary

success "UnityYAMLMerge registered in the local repository."

if [[ -f ".config/dotnet-tools.json" ]] &&
  grep -qi "csharpier" ".config/dotnet-tools.json"; then
  info "Checking CSharpier installation..."

  if CSHARPIER_VERSION="$(dotnet csharpier --version 2>/dev/null)"; then
    success "CSharpier is available: $CSHARPIER_VERSION"
  else
    error "CSharpier could not be executed after tool restore."
    exit 1
  fi
fi

printf '\n'
success "Project setup completed."
printf '\n'
printf 'Configured Unity version : %s\n' "$UNITY_VERSION"
printf 'UnityYAMLMerge           : %s\n' "$UNITY_YAML_MERGE"
printf 'Git merge driver         : %s\n' \
  "$(git config --local --get merge.unityyamlmerge.driver)"
printf '\n'
printf 'Open the project through Unity Hub using Unity %s.\n' "$UNITY_VERSION"
