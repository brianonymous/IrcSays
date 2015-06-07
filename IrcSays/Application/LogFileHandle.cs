using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using IrcSays.Ui;

namespace IrcSays.Application
{
	public class LogFileHandle : IDisposable
	{
		private FileStream _logFile;

		public Queue<ChatLine> Buffer { get; private set; }

		public LogFileHandle(string folderPath, string fileName, int linesToRead)
		{
			Buffer = new Queue<ChatLine>();

			if (!App.Settings.Current.Buffer.IsLoggingEnabled)
			{
				_logFile = null;
				return;
			}

			try
			{
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				var filePath = Path.Combine(folderPath, fileName);
				if (File.Exists(filePath))
				{
					FillBuffer(filePath, linesToRead);
				}
				_logFile = File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error opening log file: " + ex.Message);
				_logFile = null;
			}
		}

		public void FillBuffer(string filePath, int linesToRead)
		{
			using (var reader = new StreamReader(filePath))
			{
				reader.BaseStream.Seek(Math.Max(0, reader.BaseStream.Length - (512 * (linesToRead + 1))), SeekOrigin.Begin);
				reader.DiscardBufferedData();

				var rawLines = new List<string>();
				while (!reader.EndOfStream)
				{
					rawLines.Add(reader.ReadLine());
				}
				for (var i = Math.Max(0, rawLines.Count - linesToRead); i < rawLines.Count; i++)
				{
					var cl = Parse(rawLines[i]);
					if (cl != null)
					{
						Buffer.Enqueue(cl);
					}
				}
			}
		}

		public void WriteLine(ChatLine line)
		{
			if (_logFile != null)
			{
				var s = string.Format("{0}\t{1}\t{2}\t{3}\t{4}{5}",
					line.ColorKey, line.Time.ToBinary(), line.NickHashCode, line.Nick ?? "*", line.RawText, Environment.NewLine);
				if (s.Length > 512)
				{
					s = s.Substring(0, 512);
				}
				var buf = Encoding.UTF8.GetBytes(s);
				try
				{
					_logFile.Write(buf, 0, buf.Length);
					_logFile.Flush();
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Error writing to log file: " + ex.Message);
					_logFile = null;
				}
			}
		}

		public void Dispose()
		{
			if (_logFile != null)
			{
				_logFile.Dispose();
			}
		}

		private ChatLine Parse(string s)
		{
			var parts = s.Split('\t');
			if (parts.Length != 5)
			{
				return null;
			}

			long dt;
			if (!long.TryParse(parts[1], out dt))
			{
				return null;
			}

			int hashCode;
			if (!int.TryParse(parts[2], out hashCode))
			{
				return null;
			}

			var time = DateTime.FromBinary(dt);
			return new ChatLine(parts[0], time, hashCode, parts[3] == "*" ? null : parts[3], parts[4], ChatMarker.None);
		}
	}
}