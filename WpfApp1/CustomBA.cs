using System;
using System.Collections.Generic;
using System.Text;


namespace WpfApp1
{
    using System.IO;
    using System.Windows;
    using System.Windows.Threading;
    using TryWpfAppCommTool.ViewModels;
    using WixToolset.BootstrapperApplicationApi;
    using WpfApp1.Views;

    public class CustomBA : BootstrapperApplication
    {
        public Dispatcher? BADispatcher { get; private set; }
        public Window? MainWindow { get; private set; }
        public MainWindowViewModel? ViewModel { get; private set; }

        private const BundleScope bundleScope = BundleScope.PerUser;

        // https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes
        public const int EXITCODE_SUCCESS = 0;
        public const int EXITCODE_CANCELLED = 1223;       // ユーザーによって操作が取り消されました。
        public const int EXITCODE_INSTALL_FAILURE = 1603; // 重大なエラーが発生しました。
        public const int EXITCODE_PRODUCT_VERSION = 1638; // この製品の別のバージョンが既にインストールされています。
        private int exitCode = 0;

        private string sessionId = "";
        private string logDirPath = "";
        private LaunchAction launchAction = LaunchAction.Unknown;
        private LaunchAction commandAction = LaunchAction.Unknown;
        private string commandLine = "";
        private Dictionary<string, string> commandLineDictionary = new Dictionary<string, string>();
        private Display displayLevel = Display.Unknown;
        private RelatedOperation relatedOperation = RelatedOperation.None;
        private CustomLog customLog;
        private CustomLogData customLogData = new();

        protected override void OnCreate(WixToolset.BootstrapperApplicationApi.CreateEventArgs args)
        {
            base.OnCreate(args);

            commandAction = args.Command.Action;
            displayLevel = args.Command.Display; // UIレベルを保存
            commandLine = args.Command.CommandLine;
            this.engine.Log(LogLevel.Standard, $"OnCreate: Action={commandAction}, Display={displayLevel}, commandline={commandLine}");

            commandLineDictionary = ParseCommandLine(commandLine);
        }

        protected override void Run()
        {
            logDirPath = CreateLogFolder();

            if (string.IsNullOrEmpty(logDirPath))
            {
                CustomBAQuit(EXITCODE_INSTALL_FAILURE);
            }

            sessionId = this.engine.GetVariableString("WixBundleProviderKey");
            customLog = new(this.engine.Log, logDirPath, sessionId);

            this.engine.Log(LogLevel.Standard, "BA IS ALIVE!");

            this.BADispatcher = Dispatcher.CurrentDispatcher;

            this.ExecutePackageComplete += OnExecutePackageComplete;

            this.engine.Detect();

            Dispatcher.Run();

            CustomBAQuit(EXITCODE_SUCCESS);
        }
        protected override void OnDetectRelatedMsiPackage(DetectRelatedMsiPackageEventArgs args)
        {
            base.OnDetectRelatedMsiPackage(args);

            this.engine.Log(LogLevel.Standard, $"関連するMSIパッケージを検出: パッケージ {args.Version} の状態: {args.Operation}");
            relatedOperation = args.Operation;
        }

        protected override void OnDetectPackageComplete(DetectPackageCompleteEventArgs args)
        {
            base.OnDetectPackageComplete(args);

            var regLogPath = GetVariableString("REG_LOG_PATH", "");
            this.engine.Log(LogLevel.Standard, $"regLogPath: {regLogPath}");

            var dispText = "Installを実行します。";
            var isInstall = true;
            if (commandAction == LaunchAction.Install)
            {
                if (args.State == PackageState.Absent)
                {
                    launchAction = LaunchAction.Install;
                }
                else if (args.State == PackageState.Obsolete)
                {
                    // skip because downgrade install
                    this.engine.Log(LogLevel.Standard, $"新しいバージョンが既にインストールされています。");
                    System.Windows.MessageBox.Show("新しいバージョンが既にインストールされています。", "中断");
                    this.BADispatcher.InvokeShutdown();

                    CustomBAQuit(EXITCODE_PRODUCT_VERSION);
                }
                else
                {
                    launchAction = LaunchAction.Uninstall;
                    dispText = "UnInstallを実行します。";
                    isInstall = false;
                }
            }
            else if (commandAction == LaunchAction.Uninstall)
            {
                launchAction = LaunchAction.Uninstall;
                dispText = "UnInstallを実行します。";
                isInstall = false;
            }
            else
            {
                launchAction = LaunchAction.Uninstall;
                dispText = "UnInstallを実行します。";
                isInstall = false;
            }

            // 状態が Present であればインストール済み
            this.engine.Log(LogLevel.Standard, $"検出完了: パッケージ {args.PackageId} の状態: {args.State}");

            // サイレントモード（Upgrade等）の場合
            if (displayLevel == Display.None || displayLevel == Display.Embedded)
            {
                this.engine.Log(LogLevel.Standard, "サイレントモードでのアンインストールを開始します（UI非表示）。");

                // UIを表示せずに直接Planを実行
                this.engine.Plan(this.launchAction, bundleScope);
                return;
            }

            // 通常時（UI表示が必要な場合）
            this.BADispatcher!.Invoke(() =>
            {
                this.ViewModel = new MainWindowViewModel(this);
                ViewModel.DispText = dispText;
                ViewModel.IsInstall = isInstall;

                var window = new MainWindow();
                window.DataContext = this.ViewModel; // DataContextにセット
                this.MainWindow = window;

                MainWindow.Closing += (s, e) => this.ViewModel.OnWindowClosing(s, e);

                MainWindow.Show();
            });
        }

        protected override void OnShutdown(ShutdownEventArgs args)
        {
            base.OnShutdown(args);

            var message = "Shutdown," + args.Action.ToString() + "," + args.HResult.ToString();
            this.engine.Log(LogLevel.Standard, message);

            MoveMsiLog();

            customLogData.sessionId = sessionId;
            customLogData.exitCode = exitCode;
            customLogData.operation = relatedOperation.ToString();
            customLogData.action = launchAction.ToString();
            customLog.writeLog(customLogData);
        }

        /// <summary>
        /// インストール処理を開始する関数
        /// </summary>
        public void StartInstallation()
        {
            if (ViewModel!.IsShortcut)
            {
                this.engine.SetVariableNumeric("CreateShortcut", 1);
            }
            else
            {
                this.engine.SetVariableNumeric("CreateShortcut", 0);
            }

            if(launchAction == LaunchAction.Uninstall)
            {
                this.engine.SetVariableNumeric("CreateShortcut", 1); // アンインストール時は常に1としてショートカットを削除する
            }

            this.engine.Log(LogLevel.Standard, $"MSIのインストール処理を開始します。 LaunchAction: {launchAction}, Scope: {bundleScope}, CreateShortcut: 0");
            this.engine.Plan(launchAction, bundleScope);
        }

        public void StartUninstallation()
        {
            this.engine.Log(LogLevel.Standard, "アンインストール処理を開始します。");
            // アンインストールとして計画を立てる
            //this.engine.Plan(LaunchAction.Uninstall, BundleScope.PerUser);
        }

        // 計画が完了した時に呼ばれるイベントをオーバーライド
        protected override void OnPlanComplete(PlanCompleteEventArgs args)
        {
            base.OnPlanComplete(args);

            if (args.Status >= 0)
            {
                this.engine.Log(LogLevel.Standard, "計画成功。適用(Apply)を開始します。");

                this.BADispatcher.Invoke(() =>
                {
                    IntPtr hwnd = IntPtr.Zero;

                    if (displayLevel == Display.None || displayLevel == Display.Embedded)
                    {
                        var hiddenWindow = new System.Windows.Window
                        {
                            Width = 0,
                            Height = 0,
                            WindowStyle = System.Windows.WindowStyle.None,
                            ShowInTaskbar = false,
                            Visibility = System.Windows.Visibility.Hidden // 非表示設定
                        };

                        hwnd = new System.Windows.Interop.WindowInteropHelper(hiddenWindow).EnsureHandle();
                    }
                    else
                    {

                        if (this.MainWindow != null)
                        {
                            // UIスレッド上であれば、安全にハンドルを取得できます
                            hwnd = new System.Windows.Interop.WindowInteropHelper(this.MainWindow).Handle;
                        }
                    }

                    this.engine.Log(LogLevel.Standard, $"UIスレッドから Apply を呼び出します。HWND: {hwnd}");
                    this.engine.Apply(hwnd);
                });
            }
            else
            {
                this.engine.Log(LogLevel.Error, $"計画失敗: 0x{args.Status:X}");
            }
        }

        protected override void OnApplyComplete(ApplyCompleteEventArgs args)
        {
            base.OnApplyComplete(args);

            exitCode = args.Status;

            // エンジンのログに結果を記録
            this.engine.Log(LogLevel.Standard, $"Apply完了: ステータス 0x{args.Status:X}");

            // UIスレッド上で処理を行う
            this.BADispatcher!.Invoke(() =>
            {
                if (displayLevel == Display.None || displayLevel == Display.Embedded)
                {
                    if (args.Status >= 0)
                    {
                        this.engine.Log(LogLevel.Standard, "サイレントモード正常終了。");
                    }
                    else
                    {
                        this.engine.Log(LogLevel.Error, $"サイレントモードでエラーが発生しました (コード: 0x{args.Status:X})");
                    }
                    this.BADispatcher.InvokeShutdown();
                }
                else
                {
                    if (args.Status >= 0)
                    {
                        this.engine.Log(LogLevel.Standard, "通常モード正常終了。");
                        System.Windows.MessageBox.Show("処理が正常に完了しました。", "完了");
                    }
                    else
                    {
                        this.engine.Log(LogLevel.Error, $"通常モードでエラーが発生しました (コード: 0x{args.Status:X})");
                        System.Windows.MessageBox.Show($"エラーが発生しました (コード: 0x{args.Status:X})", "エラー");
                    }
                    // ウィンドウを閉じる
                    // これにより Run メソッド内の Dispatcher.Run() が終了し、engine.Quit(0) へ進みます
                    this.ViewModel.IsNotInstalling = true;
                    this.MainWindow?.Close();
                }
            });

        }

        private String CreateLogFolder()
        {
            string path = "";
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tempPath = Path.GetTempPath();
            string logDirPath = this.engine.GetVariableString("LogDirectoryPathFromTemp");

            try
            {
                path = Path.Combine(localAppData, logDirPath);
                if (!Directory.Exists(path))
                {
                    this.engine.Log(LogLevel.Standard, $"Create Log Directory: {path}");
                    Directory.CreateDirectory(path);
                }
                return path;
            }
            catch (Exception ex)
            {
                this.engine.Log(LogLevel.Error, $"Create Log Directory: {path}");
            }

            try
            {
                path = Path.Combine(tempPath, logDirPath);
                if (!Directory.Exists(path))
                {
                    this.engine.Log(LogLevel.Standard, $"Create Log Directory: {path}");
                    Directory.CreateDirectory(path);
                }
                return path;
            }
            catch (Exception ex)
            {
                this.engine.Log(LogLevel.Error, $"Create Log Directory: {path}");
            }

            return "";
        }

        private void MoveMsiLog()
        {
            if(this.engine.ContainsVariable("WixBundleLog_Package.msi"))
            {
                var msiLogPath = this.engine.GetVariableString("WixBundleLog_Package.msi");
                var newMsiLogName = $"{sessionId}.msi.log";
                var newMsiLogPath = Path.Combine(logDirPath, newMsiLogName);
                this.engine.Log(LogLevel.Standard, $"MoveMsiLog: {msiLogPath}, {newMsiLogPath}");
                MoveFile(msiLogPath, newMsiLogPath);
            }


            if (this.engine.ContainsVariable("WixBundleRollbackLog_Package.msi"))
            {
                var msiRollbackLogPath = this.engine.GetVariableString("WixBundleRollbackLog_Package.msi");
                var newMsiRollbackLogName = $"{sessionId}.msi.Rollback.log";
                var newMsiRollbackLogPath = Path.Combine(logDirPath, newMsiRollbackLogName);
                this.engine.Log(LogLevel.Standard, $"MoveMsiLog: {msiRollbackLogPath}, {newMsiRollbackLogPath}");
                MoveFile(msiRollbackLogPath, newMsiRollbackLogPath);
            }

        }

        /// <summary>
        /// 指定したファイルを移動先に移動します。
        /// </summary>
        /// <param name="sourceFilePath">対象ファイルパス</param>
        /// <param name="destFilePath">移動先ディレクトリパス</param>
        private void MoveFile(string sourceFilePath, string destFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    this.engine.Log(LogLevel.Standard, $"MoveMsiLog not found: {sourceFilePath}");
                    return;
                }

                this.engine.Log(LogLevel.Standard, $"MoveMsiLog: {sourceFilePath}, {destFilePath}");
                File.Move(sourceFilePath, destFilePath);
            }
            catch (Exception ex)
            {
                // ディレクトリ作成権限がない場合や、
                // 移動先に同名ファイルが既に存在する場合などはここに入ります。
                // 仕様に基づき、エラー時は「何もしない」で抜けます。
            }
        }

        private void OnExecutePackageComplete(object? sender, ExecutePackageCompleteEventArgs e)
        {
            this.engine.Log(LogLevel.Standard, $"パッケージ {e.PackageId} が終了しました。ステータス: 0x{e.Status:X8}, Action: {e.Action}, HResult: {e.HResult}");
            exitCode = e.Status;
        }

        private void CustomBAQuit(int excode)
        {
            exitCode = excode;
            this.engine.Quit(excode);
        }

        private String GetVariableString(String varName, String defaultVal)
        {
            if (this.engine.ContainsVariable(varName))
            {
                return this.engine.GetVariableString(varName);
            }
            else
            {
                return defaultVal;
            }
        }

        private Dictionary<string, string> ParseCommandLine(string commandLine)
        {
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(commandLine)) return args;

            // スペースで分割するが、引用符の中のスペースは無視する正規表現
            var pattern = @"(?<name>[^=\s]+)=(?:""(?<value>[^""]*)""|(?<value>[^\s]*))";
            var matches = System.Text.RegularExpressions.Regex.Matches(commandLine, pattern);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string name = match.Groups["name"].Value;
                string value = match.Groups["value"].Value;
                args[name] = value;
            }

            return args;
        }
    }
}
