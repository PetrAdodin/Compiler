using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Compiler_1.Views
{
    public partial class Compiler : Window
    {
        private bool _isFileModified = false;
        private string _currentFilePath = string.Empty;

        public Compiler()
        {
            InitializeComponent();
            UpdateWindowTitle();
        }

        // Кнопка создать
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            // Создание нового файла
            if (_isFileModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Текущий файл был изменен. Сохранить изменения?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Save_Click(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            // Очищаем содержимое
            FileContentViewer.Document.Blocks.Clear();

            _isFileModified = false;
            _currentFilePath = string.Empty;
            UpdateWindowTitle();
        }

        // Кнопка открыть
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "RTF Files (*.rtf)|*.rtf|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Загружаем файл
                    TextRange textRange = new TextRange(
                        FileContentViewer.Document.ContentStart,
                        FileContentViewer.Document.ContentEnd
                    );

                    using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open))
                    {
                        if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".rtf")
                            textRange.Load(fs, DataFormats.Rtf);
                        else
                            textRange.Load(fs, DataFormats.Text);
                    }

                    _currentFilePath = openFileDialog.FileName;
                    _isFileModified = false;
                    UpdateWindowTitle();

                    MessageBox.Show($"Файл успешно загружен: {Path.GetFileName(_currentFilePath)}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Кнопка сохранить
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Если файл не был сохранен ранее, вызываем SaveAs
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAs_Click(sender, e);
            }
            else
            {
                SaveToFile(_currentFilePath);
            }
        }

        // Кнопка сохранить как
        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            // Создаем диалог сохранения файла
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "RTF Files (*.rtf)|*.rtf|Text Files (*.txt)|*.txt";
            saveFileDialog.DefaultExt = "rtf";
            saveFileDialog.AddExtension = true;
            saveFileDialog.FilterIndex = 1; // RTF выбран по умолчанию

            // Устанавливаем начальную директорию и имя файла
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(_currentFilePath);
                saveFileDialog.FileName = Path.GetFileName(_currentFilePath);
            }
            else
            {
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                saveFileDialog.FileName = "NewFile.rtf";
            }

            // Показываем диалог и проверяем результат
            if (saveFileDialog.ShowDialog() == true)
            {
                _currentFilePath = saveFileDialog.FileName;
                SaveToFile(_currentFilePath);
            }
        }

        // Кнопка выход
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, есть ли несохраненные изменения
            if (_isFileModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Есть несохраненные изменения. Вы действительно хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show(
                    "Вы действительно хотите выйти из программы?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        // Кнопка справка
        private void Reference_Click(object sender, RoutedEventArgs e)
        {
            Reference reference= new Reference();
            reference.Show();
        }

        // Кнопка о программе
        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        // Общий метод сохранения в файл
        private void SaveToFile(string filePath)
        {
            try
            {
                // Получаем содержимое RichTextBox
                TextRange textRange = new TextRange(
                    FileContentViewer.Document.ContentStart,
                    FileContentViewer.Document.ContentEnd
                );

                // Проверяем, не пустой ли файл
                if (string.IsNullOrWhiteSpace(textRange.Text))
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Файл пуст. Все равно сохранить?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                        return;
                }

                // Сохраняем в зависимости от расширения
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".rtf")
                        textRange.Save(fs, DataFormats.Rtf);
                    else if (extension == ".txt")
                        textRange.Save(fs, DataFormats.Text);
                    else
                        textRange.Save(fs, DataFormats.Rtf); // По умолчанию RTF
                }

                _isFileModified = false;
                UpdateWindowTitle();

                MessageBox.Show($"Файл успешно сохранен:\n{filePath}",
                    "Сохранение завершено",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Обработка события изменение текста
        private void FileContentViewer_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextRange textRange = new TextRange(
                FileContentViewer.Document.ContentStart,
                FileContentViewer.Document.ContentEnd
            );

            string text = textRange.Text.Trim();

            bool hasContent = !string.IsNullOrWhiteSpace(text);

            if (hasContent)
            {
                if (string.IsNullOrEmpty(_currentFilePath) && text == "print ('Hello world!')")
                {
                    _isFileModified = false;
                }
                else
                {
                    _isFileModified = true;
                }
            }
            else
            {
                _isFileModified = false;
            }

            UpdateWindowTitle();
        }

        // Обновление заголовка окна
        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "NewFile"
                : Path.GetFileName(_currentFilePath);

            string modifiedMarker = _isFileModified ? "*" : "";
            this.Title = $"Compiler: {fileName}{modifiedMarker}";
        }

        // Обработка закрытия окна
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isFileModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Есть несохраненные изменения. Сохранить перед выходом?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Save_Click(null, null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }
    }
}