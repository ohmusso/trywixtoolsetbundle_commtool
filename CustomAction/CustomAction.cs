using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WixToolset.Dtf.WindowsInstaller;
using Newtonsoft.Json.Linq;

namespace CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction(Session session)
        {
            session.Log("Begin CustomAction");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DoSettings(Session session)
        {
            try
            {
                // Deferred実行の場合、session.CustomActionDataから情報を取得する
                string installDir = session.CustomActionData["INSTALLFOLDER"];
                session.Log($"CustomAction: INSTALLFOLDER have {installDir}");

                string jsonPath = Path.Combine(installDir, "settings.json");
                session.Log($"CustomAction: Target JSON path is {jsonPath}");

                if (!File.Exists(jsonPath))
                {
                    session.Log($"CustomAction: settings.json not found at {jsonPath}");
                    // ディレクトリ内のファイル一覧を出力
                    if (Directory.Exists(installDir))
                    {
                        var files = Directory.GetFiles(installDir);
                        session.Log($"CustomAction: Files in dir: {string.Join(", ", files)}");
                    }
                    return ActionResult.Success;
                }

                // 設定処理
                string existingJson = File.ReadAllText(jsonPath);
                JObject jo = JObject.Parse(existingJson);

                // 例: 特定のキーがなければ追加する
                if (jo["UserSetting"] == null)
                {
                    jo["UserSetting"] = "DefaultValue";
                    session.Log("CustomAction: Added missing UserSetting.");
                    File.WriteAllText(jsonPath, jo.ToString());
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log($"CustomAction: Error occurred: {ex.Message}");
                // ActionResult.Failure にするとインストールがロールバックされます
                return ActionResult.Success;
            }
        }
    }
}
