using System;
using System.Diagnostics;
using System.IO;

namespace BundleWrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. 自身の実行ファイルのディレクトリを取得
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 2. artifact/Bundle/Bundle.exe へのパスを作成
            string bundleWrapperExe = Path.Combine(baseDir, "BundleWrapper.exe");
            string bundleExe = Path.Combine(baseDir, "Bundle", "Bundle.exe");

            // 3. コードサイニングの検証
            if (!BundleWrapperCert.Verify(bundleWrapperExe))
            {
                Console.WriteLine("Enterキーを押して終了。");
                Console.ReadLine();
                return;
            }

            // 3. ログパスの生成（AppData\Local\Temp\Setup_{GUID}.log）
            string tempDir = Path.GetTempPath(); // ユーザーの Temp フォルダを取得
            string guid = Guid.NewGuid().ToString(); // GUID を生成
            string logPath = Path.Combine(tempDir, $"Setup_{guid}.log");

            // 4. プロセス起動の設定
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = bundleExe,
                Arguments = $"-l \"{logPath}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            try
            {
                if (File.Exists(bundleExe))
                {
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"起動失敗: {ex.Message}");
            }
        }
    }
}