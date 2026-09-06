using System;
using System.IO;
using System.Windows.Forms;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace RoomServer
{
	public class InMemorySink : ILogEventSink
	{
		private delegate void WriteFunc(string value);

		private readonly ITextFormatter _textFormatter = new MessageTemplateTextFormatter("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{Exception}");

		private RichTextBox rtextBox;

		private WriteFunc write;

		private WriteFunc writeLine;

		public InMemorySink(RichTextBox rtextBox)
		{
			this.rtextBox = rtextBox;
			write = Write;
			writeLine = WriteLine;
		}

		public void Emit(LogEvent logEvent)
		{
			if (logEvent == null)
			{
				throw new ArgumentNullException("logEvent");
			}
			StringWriter stringWriter = new StringWriter();
			_textFormatter.Format(logEvent, stringWriter);
			WriteLine(stringWriter.ToString());
		}

		private void Write(string value)
		{
			if (rtextBox.InvokeRequired)
			{
				rtextBox.BeginInvoke(write, value);
			}
			else
			{
				rtextBox.AppendText(value);
			}
		}

		private void WriteLine(string value)
		{
			if (rtextBox.InvokeRequired)
			{
				rtextBox.BeginInvoke(writeLine, value);
			}
			else
			{
				rtextBox.AppendText(value);
				rtextBox.AppendText(Environment.NewLine);
			}
		}
	}
}
