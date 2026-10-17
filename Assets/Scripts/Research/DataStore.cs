using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Writes study data as CSV files in Application.persistentDataPath/UXStudy.
// On Android: /sdcard/Android/data/<package>/files/UXStudy (copy with `adb pull`).
public static class DataStore
{
  public const string SessionsFile = "sessions.csv";
  public const string EventsFile = "events.csv";
  public const string AnswersFile = "answers.csv";
  public const string FinalFile = "final.csv";

  public static string Folder
  {
    get
    {
      string folder = Path.Combine(Application.persistentDataPath, "UXStudy");
      Directory.CreateDirectory(folder);
      return folder;
    }
  }

  public static string PathOf(string file) => Path.Combine(Folder, file);

  // Appends one row; writes the header first if the file is new. If the header of an
  // existing file differs (e.g. new metrics were added), the row goes to a versioned file.
  public static void AppendRow(string file, IList<string> header, IList<string> values)
  {
    string path = PathOf(file);
    string headerLine = string.Join(",", header.Select(Escape));

    if (File.Exists(path))
    {
      string existingHeader = File.ReadLines(path).FirstOrDefault();

      if (existingHeader != headerLine)
      {
        path = PathOf(Path.GetFileNameWithoutExtension(file) + "_v" + StableHash(headerLine) + ".csv");
      }
    }

    bool writeHeader = !File.Exists(path);
    StringBuilder sb = new StringBuilder();

    if (writeHeader)
      sb.AppendLine(headerLine);

    sb.AppendLine(string.Join(",", values.Select(Escape)));
    File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
  }

  public static void AppendRows(string file, IList<string> header, IEnumerable<IList<string>> rows)
  {
    foreach (IList<string> row in rows)
      AppendRow(file, header, row);
  }

  public static void WriteText(string file, string content)
  {
    File.WriteAllText(PathOf(file), content, new UTF8Encoding(false));
  }

  public static int CountRows(string file)
  {
    string path = PathOf(file);

    if (!File.Exists(path))
      return 0;

    return Math.Max(0, File.ReadLines(path).Count() - 1);
  }

  public static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
  public static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
  public static string F(float? value) => value.HasValue ? F(value.Value) : "";

  public static string Timestamp(DateTime time) => time.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

  // FNV-1a, stable across runs and platforms (string.GetHashCode is not guaranteed to be).
  static string StableHash(string text)
  {
    uint hash = 2166136261;

    foreach (char c in text)
    {
      hash ^= c;
      hash *= 16777619;
    }

    return hash.ToString("x8");
  }

  static string Escape(string value)
  {
    if (value == null)
      return "";

    if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
      return "\"" + value.Replace("\"", "\"\"") + "\"";

    return value;
  }
}
