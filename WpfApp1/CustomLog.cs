using Newtonsoft.Json;
using System;
using System.IO;
using WixToolset.BootstrapperApplicationApi;


namespace WpfApp1
{
    public class LogData
    {
        public string var1 { get; set; }
        public int var2 { get; set; }
    }

    public delegate void CustomBALog(LogLevel level, string messagey);

    public class CustomLog
    {
        public LogData LogData = new();
        public CustomLog() {
        }

        public void writeLog(CustomBALog customBALog)
        {
            customBALog(LogLevel.Standard, $"カスタムログ書き込み開始");

            //// ログフォルダを作成
            string targetDir = Path.Combine(Path.GetTempPath(), "hogehoge");
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                customBALog(LogLevel.Standard, $"書き込み先ディレクトリ作成: ${targetDir}");
            }


            // ファイル名を作成
            string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string fullPath = Path.Combine(targetDir, fileName);
            customBALog(LogLevel.Standard, $"カスタムログフルパス: {fullPath}");

            // --- 書き込み (Serialization) ---
            string jsonString = JsonConvert.SerializeObject(LogData, Formatting.Indented);
            customBALog(LogLevel.Standard, $"Json文字列: {jsonString}");

            File.WriteAllText(fullPath, jsonString);

            customBALog(LogLevel.Standard, $"カスタムログ書き込み完了");

            // --- 読み込み (Deserialization) ---
            //if (File.Exists(fullPath))
            //{
            //    string readJson = File.ReadAllText(fullPath);
            //    LogData loadedData = JsonConvert.DeserializeObject<LogData>(readJson);
            //    Console.WriteLine($"読込データ: var1={loadedData.var1}, var2={loadedData.var2}");
            //}
        }
    }
}
