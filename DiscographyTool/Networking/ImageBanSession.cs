using System;

namespace DiscographyTool.Networking
{
    public static class ImageBanSession
    {
        // 🔒 ЗАШИТЫЕ ТОКЕНЫ (6 ШТУК)
        private static readonly string[] Tokens =
        {
            "pWzRLlvUt9PLA9DcVpQUCX9hA8ICuG33I4N",
            "73ZxS2oZUmF8WCGElFioZScTQvLdByFNcM9",
            "jlg8YCRR9GZJ9ZRqr8D9EsYUbijyxce5tIV",
            "FV3PDI0u4CQRwirxKWpnCgzHCYZR6QeEU9o",
            "i1f50cFTqlpBGq32bnyGwSZ6QK8ISzNNt7y",
            "30OvHTO6YMtlBocXMzuUx7TgJlzOl0RsLWh"
        };

        public static string Token { get; }

        static ImageBanSession()
        {
            if (Tokens.Length == 0)
            {
                Token = string.Empty;
                return;
            }

            var rnd = new Random();
            Token = Tokens[rnd.Next(Tokens.Length)];
        }
    }
}
