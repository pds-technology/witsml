using System.Windows;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.ViewModels
{
    public class TextEditorViewModel : Screen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextEditorViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public TextEditorViewModel(IRuntimeService runtime, string language = null, bool isReadOnly = false)
        {
            Runtime = runtime;
            Language = language;
            IsReadOnly = isReadOnly;
            IsPasteEnabled = !IsReadOnly;
            Document = new TextDocument();
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime service.</value>
        public IRuntimeService Runtime { get; private set; }

        private TextDocument _document;

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>The document.</value>
        public TextDocument Document
        {
            get { return _document; }
            set
            {
                if (!ReferenceEquals(_document, value))
                {
                    _document = value;
                    NotifyOfPropertyChange(() => Document);
                }
            }
        }

        private string _language;

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string Language
        {
            get { return _language; }
            set
            {
                if (!string.Equals(_language, value))
                {
                    _language = value;
                    _syntax = HighlightingManager.Instance.GetDefinition(value);
                    NotifyOfPropertyChange(() => Language);
                    NotifyOfPropertyChange(() => Syntax);
                }
            }
        }

        private IHighlightingDefinition _syntax;

        /// <summary>
        /// Gets or sets the syntax.
        /// </summary>
        /// <value>The syntax.</value>
        public IHighlightingDefinition Syntax
        {
            get { return _syntax; }
            set
            {
                if (!ReferenceEquals(_syntax, value))
                {
                    _syntax = value;
                    _language = value.Name;
                    NotifyOfPropertyChange(() => Syntax);
                    NotifyOfPropertyChange(() => Language);
                }
            }
        }

        private bool _isReadOnly;

        /// <summary>
        /// Gets or sets the read only flag.
        /// </summary>
        /// <value>Is read only.</value>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    NotifyOfPropertyChange(() => IsReadOnly);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is paste enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is paste enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsPasteEnabled { get; private set; }

        private bool _isWordWrapEnabled;

        /// <summary>
        /// Gets or sets a value indicating whether word wrap is enabled.
        /// </summary>
        /// <value><c>true</c> if word wrap is enabled; otherwise, <c>false</c>.</value>
        public bool IsWordWrapEnabled
        {
            get { return _isWordWrapEnabled; }
            set
            {
                if (_isWordWrapEnabled != value)
                {
                    _isWordWrapEnabled = value;
                    NotifyOfPropertyChange(() => IsWordWrapEnabled);
                }
            }
        }

        private bool _isScrollingEnabled;

        /// <summary>
        /// Gets or sets a value indicating whether scrolling is enabled.
        /// </summary>
        /// <value><c>true</c> if scrolling is enabled; otherwise, <c>false</c>.</value>
        public bool IsScrollingEnabled
        {
            get { return _isScrollingEnabled; }
            set
            {
                if (_isScrollingEnabled != value)
                {
                    _isScrollingEnabled = value;
                    NotifyOfPropertyChange(() => IsScrollingEnabled);
                }
            }
        }

        /// <summary>
        /// Sets the document text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetText(string text)
        {
            Runtime.Invoke(() =>
            {
                Document.Text = text;
            });
        }

        /// <summary>
        /// Appends the specified text to the document.
        /// </summary>
        /// <param name="text">The text to append.</param>
        public void Append(string text)
        {
            Runtime.Invoke(() =>
            {
                Document.Insert(Document.TextLength, text);
            });
        }

        /// <summary>
        /// Copies the text to the clipboard.
        /// </summary>
        public void Copy()
        {
            Runtime.Invoke(() => Clipboard.SetText(Document.Text));
        }

        /// <summary>
        /// Pastes the clipboard text to the Document text.
        /// </summary>
        public void Paste()
        {
            Runtime.Invoke(() => Document.Text = Clipboard.GetText());
        }

        /// <summary>
        /// Clears the text.
        /// </summary>
        public void Clear()
        {
            Runtime.Invoke(() => Document.Text = string.Empty);
        }

        /// <summary>
        /// Scrolls to the bottom of the current text content.
        /// </summary>
        /// <param name="control">The control.</param>
        public void ScrollToBottom(TextEditor control)
        {
            if (IsScrollingEnabled)
            {
                control.ScrollToEnd();
            }
        }
    }
}
