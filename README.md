TeklaShortcutsReader (ShortcutsReader)
======================================

TeklaShortcutsReader is a WPF-based utility application designed to read, parse, and display Tekla Structures keyboard shortcuts. It provides a convenient external interface for users to browse and search through their defined shortcuts without navigating Tekla's internal menus.

FEATURES
--------
* Shortcut Parsing:
    - Reads Tekla Structures shortcut configuration files (typically XML-based).
    - Extracts command names and their assigned key combinations.

* Search & Filter:
    - Allows users to quickly search for specific commands or shortcut keys.
    - Filters the list in real-time to find relevant bindings.

* Keyboard Listener:
    - Includes a background keyboard listener service (KeyboardKeyListener), likely enabling global hotkey activation or the ability to detect pressed keys to highlight corresponding commands.

* User Interface:
    - Modern WPF interface for clear visualization of command-key mappings.

PREREQUISITES
-------------
* Windows OS (Application is built on WPF/.NET).
* .NET Runtime (NET 8.0 Windows based on project files).
* Access to Tekla Structures shortcut files (for reading).

USAGE
-----
1. Run "ShortcutsReader.exe".
2. The application will load.
3. Use the search bar to find a specific command or key binding.
4. (Optional) The application may respond to specific global key presses if configured, bringing the shortcut list to the foreground.
