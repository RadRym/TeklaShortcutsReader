using ShortcutsReader.Services;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();
            LoadLastFilePath();

            _shortcutListener = new KeyboardKeyListener();
            _shortcutListener.OnKeyPressed += ShortcutListener_OnKeyPressed;
            _shortcutListener.OnKeyReleased += ShortcutListener_OnKeyReleased;
            _shortcutListener.HookKeyboard();
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
        }

        private void ShortcutListener_OnKeyReleased(object sender, KeyReleasedArgs e)
        {
            if ((e.KeyReleased == Key.LeftCtrl || e.KeyReleased == Key.RightCtrl ||
                 e.KeyReleased == Key.LeftAlt || e.KeyReleased == Key.RightAlt ||
                 e.KeyReleased == Key.LeftShift || e.KeyReleased == Key.RightShift) &&
                !string.IsNullOrEmpty(_currentFilter))
            {
                _currentFilter = "";
                ApplyShortcutFilter();
            }
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
    }
}