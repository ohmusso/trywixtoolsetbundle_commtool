using System;
using System.Collections.Generic;
using System.Text;


namespace WpfApp1
{
    using System.Windows;
    using System.Windows.Threading;
    using WixToolset.BootstrapperApplicationApi;
    public class CustomBA : BootstrapperApplication
    {
        public Dispatcher BADispatcher { get; private set; }
        public Window MainWindow { get; private set; }
        
        protected override void Run()
        {
            this.engine.Log(LogLevel.Standard, "BA IS ALIVE!");

            this.engine.Detect();

            this.BADispatcher = Dispatcher.CurrentDispatcher;

            MainWindow = new MainWindow(this);
            MainWindow.Closed += (s, e) => this.BADispatcher.InvokeShutdown();

            MainWindow.Show();
            Dispatcher.Run();
            this.engine.Quit(0);
        }

        protected override void OnStartup(WixToolset.BootstrapperApplicationApi.StartupEventArgs args)
        {
            base.OnStartup(args);

            this.engine.Log(LogLevel.Standard, nameof(CustomBA));
        }

        protected override void OnShutdown(ShutdownEventArgs args)
        {
            base.OnShutdown(args);

            var message = "Shutdown," + args.Action.ToString() + "," + args.HResult.ToString();
            this.engine.Log(LogLevel.Standard, message);
        }

        /// <summary>
        /// インストール処理を開始する関数
        /// </summary>
        public void StartInstallation()
        {
            this.engine.Log(LogLevel.Standard, "MSIのインストール処理を開始します。");

            // 1. インストールを計画 (Plan)
            // ユーザ毎のインストール
            this.engine.Plan(LaunchAction.Install, BundleScope.PerUser);
        }

        public void StartUninstallation()
        {
            this.engine.Log(LogLevel.Standard, "アンインストール処理を開始します。");
            // アンインストールとして計画を立てる
            this.engine.Plan(LaunchAction.Uninstall, BundleScope.PerUser);
        }

        // 計画が完了した時に呼ばれるイベントをオーバーライド
        protected override void OnPlanComplete(PlanCompleteEventArgs args)
        {
            base.OnPlanComplete(args);

            if (args.Status >= 0)
            {
                this.engine.Log(LogLevel.Standard, "計画成功。適用(Apply)を開始します。");

                // 【重要】UIスレッド上で Apply を実行するように変更
                this.BADispatcher.Invoke(() =>
                {
                    IntPtr currentHwnd = IntPtr.Zero;
                    if (this.MainWindow != null)
                    {
                        // UIスレッド上であれば、安全にハンドルを取得できます
                        currentHwnd = new System.Windows.Interop.WindowInteropHelper(this.MainWindow).Handle;
                    }

                    this.engine.Log(LogLevel.Standard, $"UIスレッドから Apply を呼び出します。HWND: {currentHwnd}");
                    this.engine.Apply(currentHwnd);
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
            this.BADispatcher.Invoke(() =>
            {
                if (args.Status >= 0)
                {
                    System.Windows.MessageBox.Show("処理が正常に完了しました。", "完了");
                }
                else
                {
                    System.Windows.MessageBox.Show($"エラーが発生しました (コード: 0x{args.Status:X})", "エラー");
                }

                // ウィンドウを閉じる
                // これにより Run メソッド内の Dispatcher.Run() が終了し、engine.Quit(0) へ進みます
                this.MainWindow?.Close();
            });
        }
    }
}
