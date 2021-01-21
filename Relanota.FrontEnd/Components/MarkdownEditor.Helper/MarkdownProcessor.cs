using CommonMark;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace UWP.FrontEnd.Components
{
    class MarkdownProcessor
    {
        public async Task<string> GetHTML(string mdContent)
        {
            List<string> css = new List<string>();
            List<string> js = new List<string>();
            StorageFolder highlightJSFolder = await Package.Current.InstalledLocation.GetFolderAsync("highlighting.js");
            var highlightJSFile = await highlightJSFolder.GetFileAsync("highlight.js");
            js.Add(await FileIO.ReadTextAsync(highlightJSFile));

            var highlightJSCSSFile = await (await highlightJSFolder.GetFolderAsync("styles")).GetFileAsync("default.css");
            css.Add(await FileIO.ReadTextAsync(highlightJSCSSFile));

            var cssFolder = await Package.Current.InstalledLocation.GetFolderAsync("css");
            var cssFile = await cssFolder.GetFileAsync(MainPage.IsDarkTheme ? "Dark.css" : "Default.css");
            css.Add(await FileIO.ReadTextAsync(cssFile));

            return ConstructHtml(GetBody(mdContent), string.Join("\n", css.Select(s => $"<style type=\"text/css\">\n{s}\n</style>")), string.Join("\n", js.Select(s => $"<script>\n{s}\n</script>")));
        }

        private string GetBody(string src)
        {
            using (var reader = new StringReader(src))
            using (var writer = new StringWriter())
            {
                var setting = CommonMarkSettings.Default.Clone();
                setting.AdditionalFeatures = CommonMarkAdditionalFeatures.StrikethroughTilde;
                setting.RenderSoftLineBreaksAsLineBreaks = true;
                CommonMarkConverter.Convert(reader, writer, setting);
                return writer.ToString();
            }
        }

        private string ConstructHtml(string body, string cssHeader, string jsHeader)
        {
            return $@"
<html>
    <head>
        <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
        <title></title>    
        {cssHeader}
        {jsHeader}
        </head>
    <body>
        {body}
    </body>        
</html>
            ";
        }
    }
}
