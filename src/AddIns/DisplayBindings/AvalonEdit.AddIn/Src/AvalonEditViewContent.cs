// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Gui;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Bookmarks;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.AvalonEdit.AddIn
{
	public class AvalonEditViewContent : AbstractViewContent, IEditable, IMementoCapable, ITextEditorProvider, IPositionable, IParseInformationListener
	{
		readonly CodeEditor codeEditor = new CodeEditor();
		
		public AvalonEditViewContent(OpenedFile file)
		{
			this.Files.Add(file);
			file.ForceInitializeView(this);
			codeEditor.Document.Changed += textEditor_Document_Changed;
		}
		
		void textEditor_Document_Changed(object sender, DocumentChangeEventArgs e)
		{
			if (!isLoading) {
				PrimaryFile.IsDirty = true;
			}
		}
		
		public CodeEditor CodeEditor {
			get { return codeEditor; }
		}
		
		public override object Control {
			get { return codeEditor; }
		}
		
		public override void Save(OpenedFile file, Stream stream)
		{
			if (file != PrimaryFile)
				return;
			codeEditor.TextEditor.Save(stream);
		}
		
		bool isLoading;
		
		public override void Load(OpenedFile file, Stream stream)
		{
			if (file != PrimaryFile)
				return;
			isLoading = true;
			try {
				BookmarksDetach();
				codeEditor.FileName = file.FileName;
				codeEditor.TextEditor.SyntaxHighlighting =
					HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(file.FileName));
				LoadFormatter();
				codeEditor.TextEditor.Load(stream);
				BookmarksAttach();
			} finally {
				isLoading = false;
			}
		}
		
		void LoadFormatter()
		{
			const string formatingStrategyPath = "/AddIns/DefaultTextEditor/Formatter";
			
			if (codeEditor.TextEditor.SyntaxHighlighting != null) {
				string formatterPath = formatingStrategyPath + "/" + codeEditor.TextEditor.SyntaxHighlighting.Name;
				var formatter = AddInTree.BuildItems<IFormattingStrategy>(formatterPath, this, false);
				if (formatter != null && formatter.Count > 0) {
					codeEditor.FormattingStrategy = formatter[0];
				}
			}
		}
		
		protected override void OnFileNameChanged(OpenedFile file)
		{
			base.OnFileNameChanged(file);
			if (file == PrimaryFile) {
				codeEditor.FileName = file.FileName;
				BookmarksNotifyNameChange(file.FileName);
			}
		}
		
		#region Bookmark Handling
		void BookmarksAttach()
		{
			foreach (SDBookmark bookmark in BookmarkManager.GetBookmarks(codeEditor.FileName)) {
				bookmark.Document = codeEditor.TextEditorAdapter.Document;
				codeEditor.IconBarMargin.Bookmarks.Add(bookmark);
			}
			BookmarkManager.Added += BookmarkManager_Added;
			BookmarkManager.Removed += BookmarkManager_Removed;
		}

		void BookmarksDetach()
		{
			BookmarkManager.Added -= BookmarkManager_Added;
			BookmarkManager.Removed -= BookmarkManager_Removed;
			foreach (SDBookmark bookmark in codeEditor.IconBarMargin.Bookmarks.OfType<SDBookmark>()) {
				if (bookmark.Document == codeEditor.TextEditorAdapter.Document) {
					bookmark.Document = null;
				}
			}
			codeEditor.IconBarMargin.Bookmarks.Clear();
		}
		
		void BookmarkManager_Removed(object sender, BookmarkEventArgs e)
		{
			codeEditor.IconBarMargin.Bookmarks.Remove(e.Bookmark);
			if (e.Bookmark.Document == codeEditor.TextEditorAdapter.Document) {
				e.Bookmark.Document = null;
			}
		}
		
		void BookmarkManager_Added(object sender, BookmarkEventArgs e)
		{
			if (FileUtility.IsEqualFileName(this.PrimaryFileName, e.Bookmark.FileName)) {
				codeEditor.IconBarMargin.Bookmarks.Add(e.Bookmark);
				e.Bookmark.Document = codeEditor.TextEditorAdapter.Document;
			}
		}
		
		void BookmarksNotifyNameChange(string newFileName)
		{
			foreach (SDBookmark bookmark in codeEditor.IconBarMargin.Bookmarks.OfType<SDBookmark>()) {
				bookmark.FileName = newFileName;
			}
		}
		#endregion
		
		public override void Dispose()
		{
			base.Dispose();
			BookmarksDetach();
			// Unload document on dispose.
			codeEditor.Document = null;
		}
		
		public override string ToString()
		{
			return "[" + GetType().Name + " " + this.PrimaryFileName + "]";
		}
		
		#region IEditable
		public string Text {
			get {
				return WorkbenchSingleton.SafeThreadFunction(() => codeEditor.Document.Text);
			}
			set {
				WorkbenchSingleton.SafeThreadCall(text => codeEditor.Document.Text = text, value);
			}
		}
		#endregion
		
		#region IMementoCapable
		public Properties CreateMemento()
		{
			Properties memento = new Properties();
			memento.Set("CaretOffset", codeEditor.TextEditor.CaretOffset);
			memento.Set("ScrollPositionY", codeEditor.TextEditor.VerticalOffset);
			return memento;
		}
		
		public void SetMemento(Properties memento)
		{
			codeEditor.TextEditor.ScrollToVerticalOffset(memento.Get("ScrollPositionY", 0.0));
			try {
				codeEditor.TextEditor.CaretOffset = memento.Get("CaretOffset", 0);
			} catch (ArgumentOutOfRangeException) {
				// ignore caret out of range - maybe file was changed externally?
			}
		}
		#endregion
		
		#region ITextEditorProvider
		public ITextEditor TextEditor {
			get { return codeEditor.TextEditorAdapter; }
		}
		
		public IDocument GetDocumentForFile(OpenedFile file)
		{
			if (file == this.PrimaryFile)
				return codeEditor.TextEditorAdapter.Document;
			else
				return null;
		}
		#endregion
		
		#region IPositionable
		public int Line {
			get { return codeEditor.TextEditor.TextArea.Caret.Line; }
		}
		
		public int Column {
			get { return codeEditor.TextEditor.TextArea.Caret.Column; }
		}
		
		public void JumpTo(int line, int column)
		{
			codeEditor.JumpTo(line, column);
		}
		#endregion
		
		#region IParseInformationListener
		public void ParseInformationUpdated(ParseInformation parseInfo)
		{
			WorkbenchSingleton.SafeThreadAsyncCall(codeEditor.ParseInformationUpdated, parseInfo);
		}
		#endregion
	}
}
