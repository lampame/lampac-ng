using OnlineUKR.Models.Eneyida;
using OnlineUKR.Models.KinoUkr;
using Shared.Services.RxEnumerate;
using System.Text.Json;

namespace OnlineUKR.Services
{
    public struct KinoukrInvoke
    {
        public static ConcurrentDictionary<string, Model> KinoukrDb = null;

        #region KinoukrInvoke
        string host;
        string apihost;
        HttpHydra http;
        Func<string, string> onstreamfile;
        public KinoukrInvoke(string host, string apihost, HttpHydra httpHydra, Func<string, string> onstreamfile)
        {
            this.host = host != null ? $"{host}/" : null;
            this.apihost = apihost;
            http = httpHydra;
            this.onstreamfile = onstreamfile;
        }
        #endregion

        #region EmbedKurwa
        public async Task<EmbedModel> EmbedKurwa(int clarification, string title, string original_title, int year, string href)
        {
            string iframeUri = href;
            var result = new EmbedModel();

            if (string.IsNullOrEmpty(iframeUri))
            {
                var root = SearchDb(title, original_title, null, null);
                if (root == null || root.Count == 0)
                    return null;

                result.similars = new List<Similar>(root.Count);

                foreach (var item in root)
                {
                    var model = new Similar()
                    {
                        href = !string.IsNullOrEmpty(item.tortuga) ? $"https://tortuga.tw/{item.tortuga}" : $"https://ashdi.vip/{item.ashdi}",
                        title = $"{item.name} / {item.eng_name}",
                        year = item.year
                    };

                    if (item.year == year.ToString())
                        result.similars.Insert(0, model);
                    else
                        result.similars.Add(model);
                }

                if (result.similars.Count == 0)
                    return new EmbedModel() { IsEmpty = true };

                if (result.similars.Count > 1 && result.similars[0].year != year.ToString())
                    return result;

                iframeUri = result.similars[0].href;
            }

            await http.GetSpan(iframeUri, content =>
            {
                if (!content.Contains("file:", StringComparison.Ordinal))
                    return;

                if (iframeUri.Contains("ashdi"))
                {
                    result.source_type = "ashdi";

                    if (Regex.IsMatch(content, "file: ?'\\["))
                    {
                        try
                        {
                            var root = JsonSerializer.Deserialize<Models.Ashdi.Voice[]>(Rx.Match(content, "file: ?'([^\n\r]+)',"), new JsonSerializerOptions
                            {
                                AllowTrailingCommas = true
                            });

                            if (root != null && root.Length > 0)
                                result.serial_ashdi = root;
                        }
                        catch (System.Exception ex)
                        {
                            Serilog.Log.Error(ex, "{Class} {CatchId}", "Kinoukr", "id_r8r610ak");
                        }
                    }
                    else
                    {
                        var rx = Rx.Split("new Playerjs", content);
                        if (1 > rx.Count)
                            return;

                        result.content = rx[1].ToString();
                    }
                }
                else
                {
                    result.source_type = "tortuga";

                    if (Regex.IsMatch(content, "file: ?'"))
                    {
                        try
                        {
                            string file = Rx.Match(content, "file: ?'([^\n\r]+)',");
                            if (file.EndsWith("=="))
                            {
                                file = Regex.Replace(file, "==$", "");
                                file = string.Join("", CrypTo.DecodeBase64(file).Reverse());
                            }

                            var root = JsonSerializer.Deserialize<Models.Tortuga.Voice[]>(file, new JsonSerializerOptions
                            {
                                AllowTrailingCommas = true
                            });

                            if (root != null && root.Length > 0)
                                result.serial = root;
                        }
                        catch (System.Exception ex)
                        {
                            Serilog.Log.Error(ex, "{Class} {CatchId}", "Kinoukr", "id_fec09d86");
                        }
                    }
                    else
                    {
                        result.content = content.ToString();
                    }
                }
            });

            if (string.IsNullOrEmpty(result.content) && result.serial_ashdi == null && result.serial == null)
            {
                return null;
            }

            return result;
        }
        #endregion

        #region SearchDb
        public static List<Model> SearchDb(string name, string eng_name, string kp, string imdb)
        {
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(eng_name) && string.IsNullOrEmpty(kp) && string.IsNullOrEmpty(imdb))
                return null;

            if (!string.IsNullOrEmpty(kp) || !string.IsNullOrEmpty(imdb))
            {
                var resultId = KinoukrDb.Where(i =>
                {
                    if (!string.IsNullOrEmpty(kp) && i.Value.kp_id == kp)
                        return true;

                    if (!string.IsNullOrEmpty(imdb) && i.Value.imdb_id == imdb)
                        return true;

                    return false;
                });

                if (resultId != null && resultId.Count() > 0)
                    return resultId.Select(i => i.Value).ToList();
            }

            string sname = StringConvert.SearchName(name);
            string seng_name = StringConvert.SearchName(eng_name);

            var result = KinoukrDb.Where(i =>
            {
                if (sname != null && StringConvert.SearchName(i.Value.name) != null)
                {
                    if (StringConvert.SearchName(i.Value.name) == sname)
                        return true;
                }

                if (seng_name != null && StringConvert.SearchName(i.Value.eng_name) != null)
                {
                    if (StringConvert.SearchName(i.Value.eng_name) == seng_name)
                        return true;
                }

                return false;
            });

            return result.Select(i => i.Value).ToList();
        }
        #endregion

        #region getIframeSource
        public async Task<string> getIframeSource(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return null;

            string iframeUri = null;

            await http.GetSpan(link, news =>
            {
                iframeUri = Rx.Match(news, "src=\"(https?://tortuga\\.[a-z]+/[^\"]+)\"");
                if (string.IsNullOrEmpty(iframeUri))
                    iframeUri = Rx.Match(news, "src=\"(https?://ashdi\\.vip/[^\"]+)\"");
            });

            if (string.IsNullOrEmpty(iframeUri))
            {
                return null;
            }

            return iframeUri;
        }
        #endregion

        #region Tpl
        public ITplResult Tpl(EmbedModel result, int clarification, string title, string original_title, int year, string t, int s, string href, VastConf vast = null, bool rjson = false)
        {
            if (result == null || result.IsEmpty)
                return default;

            string enc_title = HttpUtility.UrlEncode(title);
            string enc_original_title = HttpUtility.UrlEncode(original_title);
            string enc_href = HttpUtility.UrlEncode(href);

            #region similar
            if (result.content == null && result.serial == null && result.serial_ashdi == null)
            {
                if (string.IsNullOrWhiteSpace(href) && result.similars != null && result.similars.Count > 0)
                {
                    var stpl = new SimilarTpl(result.similars.Count);

                    foreach (var similar in result.similars)
                    {
                        string link = host + $"lite/kinoukr?rjson={rjson}&clarification={clarification}&title={enc_title}&original_title={enc_original_title}&year={year}&href={HttpUtility.UrlEncode(similar.href)}";

                        stpl.Append(similar.title, similar.year, string.Empty, link);
                    }

                    return stpl;
                }

                return default;
            }
            #endregion

            if (result.source_type == "ashdi")
            {
                var invk = new AshdiInvoke(host, apihost, http, onstreamfile);
                int.TryParse(t, out int _t);

                var md = new Models.Ashdi.EmbedModel()
                {
                    content = result.content,
                    serial = result.serial_ashdi
                };

                return invk.Tpl(md, enc_href, null, 0, title, original_title, 0, year, _t, s, vast, rjson, host + $"lite/kinoukr?rjson={rjson}&clarification={clarification}&title={enc_title}&original_title={enc_original_title}&year={year}&href={enc_href}");
            }

            if (result.content != null)
            {
                #region Фильм
                var mtpl = new MovieTpl(title, original_title, 1);

                string hls = Regex.Match(result.content, "file: ?(\"|')(?<hls>https?://[^\"']+/index\\.m3u8)(\"|')").Groups["hls"].Value;
                if (string.IsNullOrWhiteSpace(hls))
                {
                    string base64 = Regex.Match(result.content, "file: ?(\"|')(?<base64>[^\"']+)(\"|')").Groups["base64"].Value;
                    base64 = Regex.Replace(base64, "==$", "");

                    hls = string.Join("", CrypTo.DecodeBase64(base64).Reverse());

                    if (string.IsNullOrWhiteSpace(hls))
                        return default;
                }

                #region subtitle
                var subtitles = new SubtitleTpl();
                string subtitle = new Regex("\"subtitle\": ?\"([^\"]+)\"").Match(result.content).Groups[1].Value;

                if (!string.IsNullOrEmpty(subtitle))
                {
                    var match = new Regex("\\[([^\\]]+)\\](https?://[^\\,]+)").Match(subtitle);
                    while (match.Success)
                    {
                        subtitles.Append(match.Groups[1].Value, onstreamfile.Invoke(match.Groups[2].Value));
                        match = match.NextMatch();
                    }
                }
                #endregion

                mtpl.Append(string.IsNullOrEmpty(result.quel) ? "По умолчанию" : result.quel, onstreamfile.Invoke(hls), subtitles: subtitles, vast: vast);

                return mtpl;
                #endregion
            }
            else
            {
                #region Сериал
                try
                {
                    if (s == -1)
                    {
                        #region Сезоны
                        var tpl = new SeasonTpl();

                        foreach (var season in result.serial)
                        {
                            string link = host + $"lite/kinoukr?rjson={rjson}&clarification={clarification}&title={enc_title}&original_title={enc_original_title}&year={year}&href={enc_href}&s={season.season}";

                            tpl.Append(season.title, link, season.season);
                        }

                        return tpl;
                        #endregion
                    }
                    else
                    {
                        string sArhc = s.ToString();
                        var episodes = result.serial.First(i => i.season == sArhc).folder;

                        #region Перевод
                        var vtpl = new VoiceTpl();
                        var hashVoice = new HashSet<string>(20);

                        foreach (var episode in episodes)
                        {
                            foreach (var voice in episode.folder)
                            {
                                if (!hashVoice.Add(voice.title))
                                    continue;

                                if (string.IsNullOrEmpty(t))
                                    t = voice.title;

                                string link = host + $"lite/kinoukr?rjson={rjson}&clarification={clarification}&title={enc_title}&original_title={enc_original_title}&year={year}&href={enc_href}&s={s}&t={voice.title}";
                                vtpl.Append(voice.title, t == voice.title, link);
                            }
                        }
                        #endregion

                        var etpl = new EpisodeTpl(vtpl, episodes.Length);

                        foreach (var episode in episodes)
                        {
                            var video = episode.folder.FirstOrDefault(i => i.title == t);
                            if (video?.file == null)
                                continue;

                            #region subtitle
                            var subtitles = new SubtitleTpl();

                            if (!string.IsNullOrEmpty(video.subtitle))
                            {
                                var match = new Regex("\\[([^\\]]+)\\](https?://[^\\,]+)").Match(video.subtitle);
                                while (match.Success)
                                {
                                    subtitles.Append(match.Groups[1].Value, onstreamfile.Invoke(match.Groups[2].Value));
                                    match = match.NextMatch();
                                }
                            }
                            #endregion

                            string file = onstreamfile.Invoke(video.file);
                            etpl.Append(episode.title, title ?? original_title, sArhc, episode.number, file, subtitles: subtitles, vast: vast);
                        }

                        return etpl;
                    }
                }
                catch
                {
                    return default;
                }
                #endregion
            }
        }
        #endregion
    }
}
