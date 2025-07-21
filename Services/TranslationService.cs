using System;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using System.Diagnostics;            // ← для ProcessStartInfo
using GTranslate.Translators;
using Newtonsoft.Json.Linq;

namespace ScreTran
{
    public class TranslationService : ITranslationService
    {
        private readonly YandexTranslator _yandexTranslator;
        private readonly BingTranslator _bingTranslator;

        public TranslationService()
        {
            _yandexTranslator = new YandexTranslator();
            _bingTranslator   = new BingTranslator();
        }

        /// <summary>
        /// Синхронная обёртка над асинхронным TranslateAsync.
        /// </summary>
        public string Translate(string input, Enumerations.Translator translator)
        {
            return Task.Run(async () => await TranslateAsync(input, translator)).Result;
        }

        /// <summary>
        /// Асинхронный метод выбора движка и перевода.
        /// </summary>
        private async Task<string> TranslateAsync(string input, Enumerations.Translator translator)
        {
            if (translator == Enumerations.Translator.Google)
                return await TranslateGoogleAsync(input);

            if (translator == Enumerations.Translator.Yandex)
                return (await _yandexTranslator.TranslateAsync(input, "ru")).Translation;

            if (translator == Enumerations.Translator.Bing)
                return (await _bingTranslator.TranslateAsync(input, "ru")).Translation;

            if (translator == Enumerations.Translator.MarianMT)
                return await TranslateMarianAsync(input);

            // По умолчанию возвращаем исходный текст
            return input;
        }

        /// <summary>
        /// Перевод через публичное Google Translate API.
        /// </summary>
        public async Task<string> TranslateGoogleAsync(string input)
        {
            var to  = "ru";
            var url = $"https://translate.googleapis.com/translate_a/single" +
                      $"?client=gtx&sl=auto&tl={to}&dt=t&q={HttpUtility.UrlEncode(input)}";

            using var client   = new HttpClient();
            var      response = await client.GetStringAsync(url).ConfigureAwait(false);
            return string.Join(string.Empty, JArray.Parse(response)[0].Select(x => x[0]));
        }

        /// <summary>
        /// Локальный перевод через MarianMT (запускает Python-скрипт).
        /// </summary>
        private async Task<string> TranslateMarianAsync(string input)
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "python",                  // или полный путь к python.exe
                Arguments              = "marianmt_translate.py",   // скрипт в папке с EXE
                RedirectStandardInput  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("Не удалось запустить Python-процесс");

            await proc.StandardInput.WriteAsync(input);
            proc.StandardInput.Close();

            string result = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();

            return result.Trim();
        }
    }
}
