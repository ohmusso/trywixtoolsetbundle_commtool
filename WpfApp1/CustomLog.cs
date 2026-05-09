using Newtonsoft.Json;
using System;
using System.IO;
using WixToolset.BootstrapperApplicationApi;


namespace WpfApp1
{
    public class CustomLogData
    {
        public string guid { get; set; }
        public int exitCode { get; set; }
    }

    public delegate void CustomBALog(LogLevel level, string messagey);

    public class CustomLog
    {
        private String _filePath = "";
        private CustomBALog _customBALog;
        public CustomLog(CustomBALog customBALog, String logDirPath, String guid) {
            string fileName = $"Customlog_{guid}.json";
            _filePath = Path.Combine(logDirPath, fileName);

            _customBALog = customBALog;
            _customBALog(LogLevel.Standard, $"カスタムログフルパス: {_filePath}");
        }

        public void writeLog(CustomLogData LogData)
        {
            _customBALog(LogLevel.Standard, $"カスタムログ書き込み開始");


            // --- 書き込み (Serialization) ---
            string jsonString = JsonConvert.SerializeObject(LogData, Formatting.Indented);
            _customBALog(LogLevel.Standard, $"Json文字列: {jsonString}");

            File.WriteAllText(_filePath, jsonString);

            _customBALog(LogLevel.Standard, $"カスタムログ書き込み完了");

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
