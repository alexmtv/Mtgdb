using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mtgdb.Dal;

namespace Mtgdb.Gui
{
	public class DeckSerializationSubsystem
	{
		public event Action<GuiSettings> DeckLoaded;
		public event Action DeckSaved;

		public DeckSerializationSubsystem(CardRepository cardRepository)
		{
			_formatters = new IDeckFormatter[]
			{
				new JsonDeckFormatter(),
				new ForgeDeckFormatter(cardRepository),
				new MagarenaDeckFormatter(cardRepository),
				new XMageDeckFormatter(cardRepository)
			};

			string anyFormatFilter = $"Any deck|{string.Join(@";", _formatters.Where(_ => _.SupportsImport).Select(f => f.FileNamePattern).Distinct())}";

			_loadFilter = string.Join(@"|",
				Enumerable.Repeat(anyFormatFilter, 1).Concat(
					_formatters.Where(_ => _.SupportsImport).Select(f => $"{f.Description}|{f.FileNamePattern}")));

			_saveFilter = string.Join(@"|", _formatters.Where(_ => _.SupportsExport).Select(f => $"{f.Description}|{f.FileNamePattern}"));

			Directory.CreateDirectory(AppDir.Save);
		}



		public void SaveDeck(GuiSettings current)
		{
			var fileToSave = selectFileToSave();

			if (fileToSave == null)
				return;

			var name = Path.GetFileNameWithoutExtension(fileToSave.File);
			var formatter = _formatters[fileToSave.FormatIndex];

			string serialized = formatter.ExportDeck(name, current);
			File.WriteAllText(fileToSave.File, serialized);

			DeckSaved?.Invoke();

			//var thread = new Thread(_ =>
			//{
				
			//});

			//thread.SetApartmentState(ApartmentState.STA);
			//thread.Start();
		}

		public void LoadDeck()
		{
			var fileToOpen = selectFileToOpen();

			if (fileToOpen == null)
				return;

			var serialized = File.ReadAllText(fileToOpen);
			var format = @"*" + Path.GetExtension(fileToOpen);

			foreach (var formatter in _formatters)
			{
				if (!Str.Equals(formatter.FileNamePattern, format))
					continue;

				if (!formatter.ValidateFormat(serialized))
					continue;

				var settings = formatter.ImportDeck(serialized);
				DeckLoaded?.Invoke(settings);
				return;
			}

			MessageBox.Show($"Failed to read deck from {fileToOpen}", @"Message", MessageBoxButtons.OK, MessageBoxIcon.Information);

			//var thread = new Thread(_ =>
			//{
				
			//});

			//thread.SetApartmentState(ApartmentState.STA);
			//thread.Start();
		}


		public string LastFile { get; set; }

		private string LastSavedFile
		{
			get { return _lastSavedFile ?? LastFile; }

			set
			{
				_lastSavedFile = value;
				LastFile = value;
			}
		}

		private string LastLoadedFile
		{
			get { return _lastLoadedFile ?? LastFile; }

			set
			{
				_lastLoadedFile = value;
				LastFile = value;
			}
		}



		private string LastSavedDirectory => getDir(LastSavedFile);

		private string LastLoadedDirectory => getDir(LastLoadedFile);



		private string LastSavedExtension => Path.GetExtension(LastSavedFile);

		private string LastLoadedExtension => Path.GetExtension(_lastLoadedFile);



		private int SaveFilterIndex => getIndex(LastSavedExtension, _saveFilter);

		private int LoadFilterIndex => getIndex(LastLoadedExtension, _loadFilter);



		private DeckFile selectFileToSave()
		{
			var dlg = new SaveFileDialog
			{
				InitialDirectory = LastSavedDirectory ?? new DirectoryInfo(AppDir.Save).FullName,
				Filter = _saveFilter,
				AddExtension = true,
				FilterIndex = SaveFilterIndex,
				Title = @"Select a file to save deck"
			};

			if (dlg.ShowDialog() != DialogResult.OK)
				return null;

			LastSavedFile = dlg.FileName;
			return new DeckFile(dlg.FileName, dlg.FilterIndex - 1);
		}

		private string selectFileToOpen()
		{
			var dlg = new OpenFileDialog
			{
				InitialDirectory = LastLoadedDirectory ?? new DirectoryInfo(AppDir.Save).FullName,
				Filter = _loadFilter,
				AddExtension = true,
				FilterIndex = LoadFilterIndex,
				Title = @"Select a file to load deck"
			};

			if (dlg.ShowDialog() != DialogResult.OK)
				return null;

			LastLoadedFile = dlg.FileName;
			return dlg.FileName;
		}



		private static string getDir(string lastFile)
		{
			if (lastFile == null)
				return null;

			var result = Path.GetDirectoryName(lastFile);

			if (!Directory.Exists(result))
				return null;

			return result;
		}

		private static int getIndex(string extension, string filter)
		{
			if (extension == null)
				return 0;

			string filterPart = @"*" + extension;
			var filterParts = filter.Split('|');

			var index = Enumerable.Range(0, filterParts.Length)
				.First(i => filterParts[i].Equals(filterPart));

			return 1 + index / 2;
		}



		private readonly string _loadFilter;
		private readonly string _saveFilter;

		private string _lastSavedFile;
		private string _lastLoadedFile;

		private readonly IDeckFormatter[] _formatters;
	}
}