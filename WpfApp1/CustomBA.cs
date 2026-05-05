using System;
using System.Collections.Generic;
using System.Text;


namespace WpfApp1
{
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
        private LaunchAction launchAction = LaunchAction.Unknown;
        private LaunchAction commandAction = LaunchAction.Unknown;
        private Display displayLevel = Display.Unknown;
        private CustomLog customLog;

        protected override void OnCreate(WixToolset.BootstrapperApplicationApi.CreateEventArgs args)
        {
            base.OnCreate(args);

            commandAction = args.Command.Action;
            displayLevel = args.Command.Display; // UIレベルを保存

            this.engine.Log(LogLevel.Standard, $"OnCreate: Action={commandAction}, Display={displayLevel}");
        }

        protected override void Run()
        {
            customLog = new();

            this.engine.Log(LogLevel.Standard, "BA IS ALIVE!");

            this.BADispatcher = Dispatcher.CurrentDispatcher;

            this.engine.Detect();

            Dispatcher.Run();
            this.engine.Quit(0);
        }

        protected override void OnDetectPackageComplete(DetectPackageCompleteEventArgs args)
        {
            base.OnDetectPackageComplete(args);

            var dispText = "Installを実行します。";
            var isInstall = true;
            if (commandAction == LaunchAction.Install)
            {
                if (args.State == PackageState.Absent)
                {
                    launchAction = LaunchAction.Install;
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

            // サイレントモード（Upgrade等）かつ アンインストール要求の場合
            if ((displayLevel == Display.None || displayLevel == Display.Embedded) &&
                launchAction == LaunchAction.Uninstall)
            {
                this.engine.Log(LogLevel.Standard, "サイレントモードでのアンインストールを開始します（UI非表示）。");

                // UIを表示せずに直接Planを実行
                this.engine.Plan(this.launchAction, bundleScope);
                return;
            }

            // 通常時（UI表示が必要な場合）
            this.BADispatcher!.Invoke(() => {
                // MainWindowの作成と表示
            });

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

            customLog.LogData.var1 = "hogehoge";
            customLog.LogData.var2 = 100;
            customLog.writeLog(this.engine.Log);
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
    }
}
