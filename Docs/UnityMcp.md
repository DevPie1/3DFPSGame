# Unity MCP / CLI 환경

설치 대상은 [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)의 **v10.3.0**이다. Unity 공식 CLI/MCP 및 별도 Coplay 유료 제품과 구분한다.

## 구성

- Unity 패키지: `Packages/manifest.json`에 Git 태그 v10.3.0 고정. 실제 커밋 `aa5fc638d623f56178d50329d9ad8c541a57fe66`.
- Python 서버와 CLI: `.utmp/unity-mcp-venv/`. 원본 소스는 `.utmp/unity-mcp-source/`에 보관한다. `.utmp/`는 Git에서 제외된다.
- HTTP MCP 주소: `http://127.0.0.1:8080/mcp`. 서버는 로컬 루프백에서 실행한다.
- Codex: 사용자 설정 `C:/Users/andy3/.codex/config.toml`의 기존 `[mcp_servers.unityMCP]` 항목을 사용한다.
- Unity 연결 설정: `Tools > MCP > Configure Local Connection`. HTTP, 로컬 주소, uvx 위치, v10.3.0 및 자동 연결 설정을 적용한다. 서버가 없으면 **Unity 자체의 서버 관리 기능으로 실행**한 뒤 연결한다. Unity EditorPrefs는 사용자 단위 설정이다.
- Unity가 관리하는 서버 로그: `Library/MCPForUnity/Logs/server-launch-8080.log`. 수동 PowerShell 실행 로그: `Logs/unity-mcp-server.log`, `Logs/unity-mcp-server-error.log`.
- 평소에는 Unity를 열면 패키지가 서버를 자동 실행하고 연결한다. Unity 종료 시 해당 에디터가 시작한 서버도 종료된다. Codex에서 실행한 임시 셸의 자식 프로세스에 의존하지 않는다.

## PowerShell에서 직접 Unity 작업

프로젝트 폴더에서 실행한다. Unity 에디터에서 이 프로젝트가 열려 있어야 씬 명령이 동작한다.

```powershell
cd C:\Users\andy3\3DFPSGame

# 서버 실행. 이미 응답 중이면 중복 실행하지 않는다.
.\Tools\Start-UnityMcp.ps1

# 서버 및 에디터 연결 확인
.\Tools\unity-mcp.ps1 status
.\Tools\unity-mcp.ps1 instances

# 읽기 전용 작업
.\Tools\unity-mcp.ps1 scene active
.\Tools\unity-mcp.ps1 scene hierarchy --max-depth 2 --limit 20
.\Tools\unity-mcp.ps1 gameobject find Player
.\Tools\unity-mcp.ps1 editor console --type error --count 10

# 명령 목록 / JSON 출력
.\Tools\unity-mcp.ps1 --help
.\Tools\unity-mcp.ps1 --format json scene active
```

CLI 래퍼는 프로젝트 Assets 경로의 SHA1을 이용해 이 프로젝트를 명시적으로 선택한다. 여러 Unity 프로젝트가 열려 있어도 임의의 다른 프로젝트로 명령을 보내지 않는다. 각 명령의 옵션은 `--help`로 확인한다.

PowerShell 실행 정책으로 스크립트 실행이 막히면 해당 실행에만 적용한다. 시스템 실행 정책을 바꿀 필요는 없다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\unity-mcp.ps1 status
```

## Codex CLI에서 자연어 작업

```powershell
.\Tools\codex-unity.ps1
```

기존 설치된 Codex CLI를 찾아 프로젝트 디렉터리에서 실행한다. 사용자 Codex MCP 설정과 로그인을 사용한다. `/mcp`에서 `unityMCP`를 확인한다. 예시 요청:

> unityMCP로 현재 씬과 Player의 컴포넌트를 읽어줘. Play나 테스트는 실행하지 마.

데스크톱의 기존 대화가 서버 연결 전부터 열려 있었다면 MCP 재연결 또는 앱 재시작이 필요할 수 있다. 서버 응답, Unity 인스턴스 연결, 현재 대화의 MCP 도구 노출은 각각 별개다. 이 대화에서 도구가 아직 노출되지 않아도 CLI를 통해 작업할 수 있다.

## 재연결

1. Unity에서 프로젝트를 열고 패키지 임포트와 스크립트 컴파일이 끝나도록 한다. 자동 시작이 활성화되어 있다.
2. 연결이 안 되어 있으면 `Tools > MCP > Configure Local Connection`을 실행한다. 서버 시작부터 연결까지 수행한다.
3. 사용자가 별도의 PowerShell 터미널에서 서버를 관리하려는 경우에만 `Start-UnityMcp.ps1`을 사용할 수 있다. Codex 임시 명령 세션에서는 서버의 장기 실행을 보장하지 않으므로 Unity 자체 실행을 우선한다.
4. `unity-mcp.ps1 status`에 이 프로젝트 인스턴스가 나타나고 `scene active`가 씬 정보를 반환하는지 확인한다.

게임 Play, 테스트, 빌드 검증은 사용자가 담당한다. 설치 확인에서는 서버 연결과 씬 정보 조회만 사용한다.

## 설치 시 연결 확인 결과 (2026-10-28)

- Unity 인스턴스: `3DFPSGame@5b8e2549f3a638fa`, Unity `6000.3.22f1`.
- CLI `scene active`: `GameScene` / `Assets/01.Scenes/GameScene.unity` 반환.
- CLI `gameobject find Player`: Player 1개 검색 성공.
- MCP 클라이언트 초기화 및 도구 목록 조회: 48개 도구 반환.
- MCP `manage_scene` / `get_active`: 같은 GameScene 반환.
- 현재 열린 씬의 미저장 상태는 유지했으며, 게임 실행·테스트·빌드를 하지 않았다.
- Codex `mcp get unityMCP`: HTTP 주소가 등록되고 enabled인 것을 확인했다. 기존 데스크톱 대화에 도구가 재노출되는지는 별도 재연결이 필요할 수 있다.

### 연결 종료 후 보완 (같은 날)

초기 외부 실행 서버가 사라지면서 Unity의 WebSocket 재접속 오류가 발생했다. 복구 메뉴에서 Unity의 `StartLocalHttpServer`로 서버를 시작하고 준비를 기다린 뒤 연결하도록 보완했다. 이후 서버 실행 계통이 Unity 프로세스로 이어지는 것을 확인했고, CLI로 GameScene 정보를 다시 읽는 데 성공했다. 실제 Unity 재시작 후 동작은 사용자가 확인한다.

## 실행 환경 재설치

`.utmp/`를 지웠거나 새 PC로 옮기면 Python 3.10 이상으로 전용 환경을 다시 만든다. 아래 예시는 Python 명령이 설치되어 있다는 전제다.

```powershell
python -m venv .utmp/unity-mcp-venv
.\.utmp\unity-mcp-venv\Scripts\python.exe -m pip install mcpforunityserver==10.3.0 uv
```

그 뒤 서버 실행과 Unity 연결 설정을 다시 수행한다. Codex 사용자 설정이 없는 PC에서는 `codex mcp add unityMCP --url http://127.0.0.1:8080/mcp`로 등록한다.
