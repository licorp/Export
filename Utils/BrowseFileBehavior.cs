using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ProSheetsAddin.Utils
{
    /// <summary>
    /// Attached Behavior for Browse File buttons
    /// This avoids WPF XAML compilation issues with x:Name controls in code-behind
    /// </summary>
    public static class BrowseFileBehavior
    {
        #region TargetTextBoxName Attached Property

        /// <summary>
        /// Name of the target TextBox control to update with selected file path
        /// </summary>
        public static readonly DependencyProperty TargetTextBoxNameProperty =
            DependencyProperty.RegisterAttached(
                "TargetTextBoxName",
                typeof(string),
                typeof(BrowseFileBehavior),
                new PropertyMetadata(null, OnTargetTextBoxNameChanged));

        public static void SetTargetTextBoxName(Button button, string value)
        {
            button.SetValue(TargetTextBoxNameProperty, value);
        }

        public static string GetTargetTextBoxName(Button button)
        {
            return (string)button.GetValue(TargetTextBoxNameProperty);
        }

        #endregion

        #region DialogTitle Attached Property

        /// <summary>
        /// Title for the OpenFileDialog
        /// </summary>
        public static readonly DependencyProperty DialogTitleProperty =
            DependencyProperty.RegisterAttached(
                "DialogTitle",
                typeof(string),
                typeof(BrowseFileBehavior),
                new PropertyMetadata("Select File"));

        public static void SetDialogTitle(Button button, string value)
        {
            button.SetValue(DialogTitleProperty, value);
        }

        public static string GetDialogTitle(Button button)
        {
            return (string)button.GetValue(DialogTitleProperty);
        }

        #endregion

        #region FileFilter Attached Property

        /// <summary>
        /// File filter for the OpenFileDialog
        /// </summary>
        public static readonly DependencyProperty FileFilterProperty =
            DependencyProperty.RegisterAttached(
                "FileFilter",
                typeof(string),
                typeof(BrowseFileBehavior),
                new PropertyMetadata("Text Files (*.txt)|*.txt|All Files (*.*)|*.*"));

        public static void SetFileFilter(Button button, string value)
        {
            button.SetValue(FileFilterProperty, value);
        }

        public static string GetFileFilter(Button button)
        {
            return (string)button.GetValue(FileFilterProperty);
        }

        #endregion

        #region Event Handler

        private static void OnTargetTextBoxNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button && e.NewValue is string textBoxName && !string.IsNullOrEmpty(textBoxName))
            {
                // Remove old handler to avoid duplicates
                button.Click -= BrowseButton_Click;
                // Add new handler
                button.Click += BrowseButton_Click;
            }
        }

        private static void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;

            try
            {
                // Get the target TextBox name
                string textBoxName = GetTargetTextBoxName(button);
                if (string.IsNullOrEmpty(textBoxName)) return;

                // Find the TextBox by name in the visual tree
                TextBox targetTextBox = FindTextBoxByName(button, textBoxName);
                if (targetTextBox == null)
                {
                    MessageBox.Show(
                        $"Could not find TextBox with name '{textBoxName}'",
                        "Browse Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Get dialog settings
                string title = GetDialogTitle(button);
                string filter = GetFileFilter(button);

                // Create and configure OpenFileDialog
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = title,
                    Filter = filter,
                    FilterIndex = 1,
                    CheckFileExists = false
                };

                // Set initial directory from current TextBox value if exists
                string currentPath = targetTextBox.Text;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(currentPath);
                        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        {
                            dialog.InitialDirectory = directory;
                        }
                    }
                    catch
                    {
                        // Ignore invalid paths
                    }
                }

                // Show dialog and update TextBox
                if (dialog.ShowDialog() == true)
                {
                    targetTextBox.Text = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error selecting file: {ex.Message}",
                    "Browse Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Find a TextBox by name in the visual tree
        /// </summary>
        private static TextBox FindTextBoxByName(DependencyObject element, string name)
        {
            if (element == null || string.IsNullOrEmpty(name))
                return null;

            // Start from the root Window
            var window = Window.GetWindow(element);
            if (window == null) return null;

            // Use LogicalTreeHelper to find named element
            var foundElement = window.FindName(name);
            return foundElement as TextBox;
        }

        #endregion
    }
}
