using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PureBattleGame.Core;

public class WebUIHostForm : Form
{
    protected WebView2 WebViewControl { get; private set; } = null!;
    public WebUIBridge? Bridge { get; private set; }
    private string _initialRoute;

    public WebUIHostForm(string route = "social-hub", string title = "PureBattleGame UI")
    {
        _initialRoute = route;
        this.Text = title;
        this.Size = new Size(1100, 720);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(20, 20, 24);
        this.DoubleBuffered = true;

        WebViewControl = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(20, 20, 24)
        };
        this.Controls.Add(WebViewControl);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            string userDataPath = Path.Combine(Path.GetTempPath(), "PureBattleGame_WebUI_Data");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataPath, null);
            await WebViewControl.EnsureCoreWebView2Async(env);

            // 映射 WebUI 产物路径到虚拟域名 app.local
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string webUiFolder = Path.Combine(baseDir, "WebUI", "dist");

            if (!Directory.Exists(webUiFolder))
            {
                // 尝试相对工程路径
                string devPath = Path.Combine(baseDir, "..", "..", "..", "WebUI", "dist");
                if (Directory.Exists(devPath))
                {
                    webUiFolder = Path.GetFullPath(devPath);
                }
            }

            if (Directory.Exists(webUiFolder))
            {
                WebViewControl.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.local",
                    webUiFolder,
                    CoreWebView2HostResourceAccessKind.Allow
                );
            }

            Bridge = new WebUIBridge(WebViewControl.CoreWebView2);
            OnBridgeReady(Bridge);

            // 导航到指定路由
            string targetUrl = $"http://app.local/index.html#/{_initialRoute}";
            WebViewControl.CoreWebView2.Navigate(targetUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected virtual void OnBridgeReady(WebUIBridge bridge)
    {
        // 子类可覆盖此方法注册特定的 C# 消息处理例程
    }
}
