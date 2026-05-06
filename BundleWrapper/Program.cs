using System;
using System.Diagnostics;
using System.IO;

namespace BundleWrapper
{
    class Program
    {
        private static string appGuid = "Global\\BundleWrapper-UNIQUE-ID-172e2882-fc33";
        static void Main(string[] args)
        {
            using (Mutex mutex = new Mutex(false, appGuid, out bool createdNew))
            {
                if (!createdNew)
                {
                    Console.WriteLine("多重起動しています。");
                    Console.WriteLine("Enterキーを押して終了。");
                    Console.ReadLine();
                    return;
                }

                // 1. 自身の実行ファイルのディレクトリを取得
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // 2. artifact/Bundle/Bundle.exe へのパスを作成
                string bundleWrapperExe = Path.Combine(baseDir, "BundleWrapper.exe");
                string bundleExe = Path.Combine(baseDir, "Bundle", "Bundle.exe");

                // 3. コードサイニングの検証
                if (!BundleWrapperCert.Verify(bundleExe))
                {
                    Console.WriteLine("自己証明書が証明書ストアの信頼されたルート証明機関に登録されていることを確認してください。");
                    Console.WriteLine("Enterキーを押して終了。");
                    Console.ReadLine();
                    return;
                }

                if (!BundleWrapperCert.Verify(bundleWrapperExe))
                {
                    Console.WriteLine("自己証明書が証明書ストアの信頼されたルート証明機関に登録されていることを確認してください。");
                    Console.WriteLine("Enterキーを押して終了。");
                    Console.ReadLine();
                    return;
                }

                // 4. ログパスの生成（AppData\Local\Temp\Setup_{GUID}.log）
                string tempDir = Path.GetTempPath(); // ユーザーの Temp フォルダを取得
                string guid = Guid.NewGuid().ToString(); // GUID を生成
                string logPath = Path.Combine(tempDir, $"Setup_{guid}.log");

                // 5. プロセス起動の設定
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = bundleExe,
                    Arguments = $"CUSTOMGUID=\"{guid}\" -l \"{logPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                try
                {
                    if (File.Exists(bundleExe))
                    {
                        Process p = Process.Start(psi);
                        if (p != null)
                        {
                            p.WaitForExit();
                            int exitCode = p.ExitCode;
                            Console.WriteLine($"インストーラーが終了しました。終了コード: {exitCode}");

                            // 成功（0）以外の場合にログの場所を通知するなど
                            if (exitCode != 0)
                            {
                                Console.WriteLine($"エラーが発生した可能性があります。ログを確認してください: {logPath}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"起動失敗: {ex.Message}");
                }
            }
        }
    }
}