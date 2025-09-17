using ShortcutsReader.Services;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;

namespace ShortcutsReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _allShortcuts = "";
        private string _currentFilter = "";
        private KeyboardKeyListener _shortcutListener;
        private Dictionary<TextBox, string> _originalTexts = new Dictionary<TextBox, string>();
        private bool _isControlPressed = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadLastFilePath();

            InitializeOriginalTexts();

            _shortcutListener = new KeyboardKeyListener();
            _shortcutListener.OnKeyPressed += ShortcutListener_OnKeyPressed;
            _shortcutListener.OnKeyReleased += ShortcutListener_OnKeyReleased;
            _shortcutListener.HookKeyboard();
        }

        private void InitializeOriginalTexts()
        {
            var grid = FindName("GridWithNumbers") as Grid;
            if (grid == null)
            {
                // Jeśli nie możesz znaleźć po nazwie, znajdź siatką programowo
                grid = FindGridInVisualTree();
            }

            if (grid != null)
            {
                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBox textBox)
                    {
                        _originalTexts[textBox] = textBox.Text;
                    }
                }
            }
        }

        private Grid FindGridInVisualTree()
        {
            var mainGrid = this.Content as Grid;
            if (mainGrid != null)
            {
                foreach (UIElement child in mainGrid.Children)
                {
                    if (child is Grid grid && Grid.GetRow(child) == 3)
                    {
                        foreach (UIElement innerChild in grid.Children)
                        {
                            if (innerChild is Grid innerGrid && Grid.GetRow(innerChild) == 1)
                            {
                                return innerGrid;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void LoadLastFilePath()
        {
            txtFilePath.Text = Properties.Settings.Default.LastFilePath ?? "";
        }

        private void SaveLastFilePath(string filePath)
        {
            Properties.Settings.Default.LastFilePath = filePath;
            Properties.Settings.Default.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            _shortcutListener?.UnHookKeyboard();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            string filePath = txtFilePath.Text.Trim();

            if (string.IsNullOrEmpty(filePath))
            {
                lblStatus.Text = "Enter file path!";
                return;
            }

            if (!File.Exists(filePath))
            {
                lblStatus.Text = "File does not exist!";
                return;
            }

            try
            {
                LoadShortcuts(filePath);
                SaveLastFilePath(filePath);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                txtShortcuts.Text = "Failed to load file.";
            }
        }

        private void LoadShortcuts(string filePath)
        {
            var result = new StringBuilder();
            int count = 0;

            var doc = new XmlDocument();
            doc.Load(filePath);

            var shortcuts = doc.SelectNodes("//KeyboardShortcut[@Accelerator]");

            foreach (XmlNode shortcut in shortcuts)
            {
                string commandName = shortcut.Attributes["CommandName"]?.Value ?? "Unknown";
                string accelerator = shortcut.Attributes["Accelerator"]?.Value ?? "";

                if (!string.IsNullOrEmpty(accelerator))
                {
                    string displayName = commandName.Contains(".")
                        ? commandName.Substring(commandName.LastIndexOf('.') + 1)
                        : commandName;

                    result.AppendLine($"{accelerator.PadRight(20)} - {displayName}");
                    count++;
                }
            }

            _allShortcuts = result.ToString();
            txtShortcuts.Text = _allShortcuts;
            lblStatus.Text = $"Loaded {count} keyboard shortcuts.";
        }

        private void ShortcutListener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            bool ctrlPressed = IsKeyPressed(Key.LeftCtrl) || IsKeyPressed(Key.RightCtrl);
            bool altPressed = IsKeyPressed(Key.LeftAlt) || IsKeyPressed(Key.RightAlt);
            bool shiftPressed = IsKeyPressed(Key.LeftShift) || IsKeyPressed(Key.RightShift);

            if (ctrlPressed && !_isControlPressed)
            {
                _isControlPressed = true;
                ChangeGridTextsToKurde();
            }

            var filter = new StringBuilder(25);
            if (ctrlPressed) filter.Append("Control + ");
            if (altPressed) filter.Append("Alt + ");
            if (shiftPressed) filter.Append("Shift + ");

            string newFilter = filter.ToString();

            if (newFilter != _currentFilter && !string.IsNullOrEmpty(_allShortcuts))
            {
                _currentFilter = newFilter;
                ApplyShortcutFilter();      
            }
            if(txtShortcutName.Text != "")
                PerformSearch();
        }

        private void ShortcutListener_OnKeyReleased(object sender, KeyReleasedArgs e)
        {
            if ((e.KeyReleased == Key.LeftCtrl || e.KeyReleased == Key.RightCtrl) && _isControlPressed)
            {
                _isControlPressed = false;
                RestoreOriginalTexts();
            }

            if ((e.KeyReleased == Key.LeftCtrl || e.KeyReleased == Key.RightCtrl ||
                 e.KeyReleased == Key.LeftAlt || e.KeyReleased == Key.RightAlt ||
                 e.KeyReleased == Key.LeftShift || e.KeyReleased == Key.RightShift) &&
                !string.IsNullOrEmpty(_currentFilter))
            {
                _currentFilter = "";
                ApplyShortcutFilter();
            }
            if (txtShortcutName.Text != "")
                PerformSearch();
        }

        private bool IsKeyPressed(Key key)
        {
            return (Keyboard.GetKeyStates(key) & KeyStates.Down) == KeyStates.Down;
        }

        private void ApplyShortcutFilter()
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(_currentFilter))
                {
                    txtShortcuts.Text = _allShortcuts;
                    lblStatus.Text = $"Loaded {CountLines(_allShortcuts)} keyboard shortcuts.";
                    return;
                }

                var lines = _allShortcuts.Split('\n');
                var filtered = lines.Where(line => line.Trim().StartsWith(_currentFilter)).ToArray();

                txtShortcuts.Text = string.Join("\n", filtered);
                lblStatus.Text = $"Filter '{_currentFilter}' - found {filtered.Length} shortcuts.";
            });
        }

        private int CountLines(string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : text.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line));
        }

        private void txtShortcutName_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            string searchTerm = txtShortcutName.Text.Trim();

            if (string.IsNullOrEmpty(_allShortcuts))
            {
                lblStatus.Text = "First load shortcuts from XML file!";
                return;
            }

            if (string.IsNullOrEmpty(searchTerm))
            {
                txtShortcuts.Text = _allShortcuts;
                lblStatus.Text = $"Showing all {CountLines(_allShortcuts)} keyboard shortcuts.";
                return;
            }

            var lines = _allShortcuts.Split('\n');
            var filtered = lines.Where(line =>
                line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(line)
            ).ToArray();

            txtShortcuts.Text = string.Join("\n", filtered);
            lblStatus.Text = $"Search '{searchTerm}' - found {filtered.Length} shortcuts.";
        }

        private void ClearSearch()
        {
            txtShortcutName.Text = "";
            if (!string.IsNullOrEmpty(_allShortcuts))
            {
                txtShortcuts.Text = _allShortcuts;
                lblStatus.Text = $"Showing all {CountLines(_allShortcuts)} keyboard shortcuts.";
            }
        }

        private void ChangeGridTextsToKurde()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var textBox in _originalTexts.Keys)
                {
                    textBox.Text = "kurde";
                }
            });
        }
        private void RestoreOriginalTexts()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var kvp in _originalTexts)
                {
                    kvp.Key.Text = kvp.Value;
                }
            });
        }
    }
}