using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Balduu
{
    /// <summary>
    /// Đọc dữ liệu Google Sheet dạng CSV qua endpoint gviz, chọn tab theo tên.
    /// Sheet phải được share "Anyone with the link can view".
    /// Lưu ý: tên tab sai thì Google không báo lỗi mà trả về tab đầu tiên.
    /// </summary>
    public static class GoogleSheetHelper
    {
        private const int TimeoutSeconds = 15;

        public static string GetCsvUrl(string sheetId, string tabName)
        {
            return $"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:csv&sheet={UnityWebRequest.EscapeURL(tabName)}";
        }

        /// <summary>Tải raw CSV của 1 tab.</summary>
        public static async UniTask<string> FetchCsvAsync(string sheetId, string tabName, CancellationToken token = default)
        {
            string url = GetCsvUrl(sheetId, tabName);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = TimeoutSeconds;

            await request.SendWebRequest().WithCancellation(token);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"GoogleSheet fetch failed ({request.responseCode}): {request.error} - {url}");
            }

            // Sheet không public sẽ bị redirect về trang login HTML thay vì trả CSV
            string contentType = request.GetResponseHeader("Content-Type") ?? string.Empty;
            if (contentType.Contains("text/html"))
            {
                throw new Exception($"GoogleSheet is not public: {url}");
            }

            return request.downloadHandler.text;
        }

        /// <summary>Tải tab và trả về danh sách dòng, mỗi dòng là mảng cell.</summary>
        public static async UniTask<List<string[]>> FetchRowsAsync(string sheetId, string tabName, CancellationToken token = default)
        {
            string csv = await FetchCsvAsync(sheetId, tabName, token);
            return ParseCsv(csv);
        }

        /// <summary>
        /// Tải tab, dùng dòng đầu làm header, trả về mỗi dòng là Dictionary(header -> value).
        /// Bỏ qua dòng trống hoàn toàn.
        /// </summary>
        public static async UniTask<List<Dictionary<string, string>>> FetchTableAsync(string sheetId, string tabName, CancellationToken token = default)
        {
            List<string[]> rows = await FetchRowsAsync(sheetId, tabName, token);
            List<Dictionary<string, string>> result = new();
            if (rows.Count == 0) return result;

            string[] headers = rows[0];
            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (Array.TrueForAll(row, string.IsNullOrWhiteSpace)) continue;

                Dictionary<string, string> dict = new();
                for (int c = 0; c < headers.Length; c++)
                {
                    if (string.IsNullOrWhiteSpace(headers[c])) continue;
                    dict[headers[c].Trim()] = c < row.Length ? row[c] : string.Empty;
                }

                result.Add(dict);
            }

            return result;
        }

        /// <summary>Parse CSV theo RFC 4180: hỗ trợ cell có dấu phẩy, xuống dòng, và "" escape.</summary>
        public static List<string[]> ParseCsv(string csv)
        {
            List<string[]> rows = new();
            List<string> row = new();
            StringBuilder cell = new();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char ch = csv[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row.ToArray());
                        row.Clear();
                        break;
                    default:
                        cell.Append(ch);
                        break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }
    }
}
