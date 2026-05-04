namespace BundleWrapper
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;

    public class BundleWrapperCert
    {
        public static bool Verify(string path)
        {
            try
            {
                Console.WriteLine($"自分自身を検証中: {path}");

                // 2. 証明書の検証
                X509Certificate2 cert = new X509Certificate2(path);
                X509Chain chain = new X509Chain();

                // 自己署名証明書の場合の設定
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                if (chain.Build(cert))
                {
                    Console.WriteLine("チェック成功: 正当な署名を確認しました。");
                    return true;
                }
                else
                {
                    Console.WriteLine("チェック失敗: 署名が信頼できないか、改ざんされています。");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: 署名が見つからないか、アクセスできません。({ex.Message})");
                return false;
            }
        }
    }
}
