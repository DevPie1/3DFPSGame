#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

// CLI의 -executeMethod 또는 메뉴에서 실행하는 로컬 MCP 연결 설정.
public static class UnityMcpSetup
{
    private static bool _connecting;
    [InitializeOnLoadMethod]
    private static void ConfigureIfRequested()
    {
        // 설치 스크립트가 만든 요청 파일이 있을 때만 한 번 설정한다.
        EditorApplication.delayCall += () =>
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            string request = Path.Combine(project, ".utmp", "unity-mcp-configure.request");
            if (!File.Exists(request)) return;
            if (!File.Exists(Path.Combine(project, ".utmp", "unity-mcp-venv", "Scripts", "uvx.exe"))) return;
            Configure();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/MCP/Configure Local Connection")]
    public static void Configure()
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        string uvx = Path.Combine(project, ".utmp", "unity-mcp-venv", "Scripts", "uvx.exe");
        if (!File.Exists(uvx))
        {
            Debug.LogError("[UnityMCP] 프로젝트의 MCP 실행 환경이 없습니다. Docs/UnityMcp.md를 확인하세요.");
            return;
        }

        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
        EditorPrefs.SetString("MCPForUnity.UvxPath", uvx);
        EditorPrefs.SetString("MCPForUnity.GitUrlOverride", "mcpforunityserver==10.3.0");
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
        EditorPrefs.SetBool("MCPForUnity.SetupCompleted", true);
        EditorConfigurationCache.Instance.Refresh();
        Connect();
    }

    private static async void Connect()
    {
        if (_connecting) return;
        _connecting = true;
        try
        {
            // Unity가 서버를 실행해야 Codex 명령 세션 종료에 서버 수명이 묶이지 않는다.
            var server = MCPServiceLocator.Server;
            if (!server.IsLocalHttpServerReachable())
            {
                if (!server.StartLocalHttpServer(quiet: true))
                {
                    Debug.LogError("[UnityMCP] 로컬 서버 시작 실패. Library/MCPForUnity/Logs의 서버 로그를 확인하세요.");
                    return;
                }

                double deadline = EditorApplication.timeSinceStartup + 120d;
                while (!server.IsLocalHttpServerReachable())
                {
                    if (EditorApplication.timeSinceStartup > deadline)
                    {
                        Debug.LogError("[UnityMCP] 서버 시작 시간 초과. Library/MCPForUnity/Logs의 서버 로그를 확인하세요.");
                        return;
                    }
                    await Task.Delay(500);
                }
            }

            if (MCPServiceLocator.Bridge.IsRunning) return;
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            if (connected) Debug.Log("[UnityMCP] 로컬 MCP 서버 연결 완료: http://127.0.0.1:8080/mcp");
            else Debug.LogWarning("[UnityMCP] 서버를 먼저 실행한 뒤 Tools > MCP > Configure Local Connection을 실행하세요.");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            _connecting = false;
        }
    }
}
#endif
