using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Compiler_1.Services;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System;
using System.Diagnostics;

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
            SetInitialText();
        }

        private void SetInitialText()
        {
            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("enum class Day { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday };"));
            FileContentViewer.Document.Blocks.Clear();
            FileContentViewer.Document.Blocks.Add(paragraph);
            _isFileModified = false;
            UpdateWindowTitle();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (_isFileModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Текущий файл был изменен. Сохранить изменения?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    Save_Click(sender, e);
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            SetInitialText();
            _currentFilePath = string.Empty;
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "RTF Files (*.rtf)|*.rtf|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    TextRange textRange = new TextRange(
                        FileContentViewer.Document.ContentStart,
                        FileContentViewer.Document.ContentEnd);

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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                SaveAs_Click(sender, e);
            else
                SaveToFile(_currentFilePath);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "RTF Files (*.rtf)|*.rtf|Text Files (*.txt)|*.txt";
            saveFileDialog.DefaultExt = "rtf";
            saveFileDialog.AddExtension = true;
            saveFileDialog.FilterIndex = 1;

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

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentFilePath = saveFileDialog.FileName;
                SaveToFile(_currentFilePath);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (_isFileModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Есть несохраненные изменения. Вы действительно хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    Application.Current.Shutdown();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show(
                    "Вы действительно хотите выйти из программы?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    Application.Current.Shutdown();
            }
        }

        private void Reference_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Сканер распознает лексемы из объявления enum class Day:\n\n" +
                "Ключевые слова: enum, class\n" +
                "Идентификаторы: Day, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday\n" +
                "Операторы: {, }, ,, ;\n\n" +
                "Коды лексем:\n" +
                "1 - ключевое слово\n" +
                "2 - идентификатор\n" +
                "3 - оператор/пунктуация\n" +
                "4 - разделитель (пробел)\n" +
                "99 - ошибка",
                "Справка",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Сканер лексем для enum class Day\n" +
                "Версия 1.0\n\n" +
                "Разработано для распознавания конструкции:\n" +
                "enum class Day { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday };",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextRange textRange = new TextRange(
                    FileContentViewer.Document.ContentStart,
                    FileContentViewer.Document.ContentEnd);
                string code = textRange.Text;

                var lexemeTokenizer = new CppTokenizerForLexemes(code);
                var lexemeTokens = lexemeTokenizer.Tokenize();

                var lexemes = new List<LexemeInfo>();

                foreach (Token token in lexemeTokens)
                {
                    lexemes.Add(new LexemeInfo
                    {
                        Code = GetCode(token),
                        Type = GetTypeDescription(token),
                        Lexeme = token.Value,
                        Location = $"строка {token.Line}, {token.StartColumn}-{token.EndColumn}",
                        Line = token.Line,
                        StartColumn = token.StartColumn,
                        EndColumn = token.EndColumn
                    });
                }

                OutputDataGrid.ItemsSource = lexemes;

                var syntaxTokenizer = new CppTokenizer(code);
                var syntaxTokens = syntaxTokenizer.Tokenize();

                var parser = new CppParser(syntaxTokens);
                var syntaxErrors = parser.Parse();

                if (syntaxErrors.Count == 0)
                {
                    SyntaxResultTextBlock.Text = "Синтаксический анализ пройден успешно. Ошибок не обнаружено.";
                    SyntaxResultTextBlock.Foreground = Brushes.Green;
                    SyntaxDataGrid.ItemsSource = null;
                }
                else
                {
                    SyntaxResultTextBlock.Text = $"Обнаружено синтаксических ошибок: {syntaxErrors.Count}";
                    SyntaxResultTextBlock.Foreground = Brushes.Red;
                    SyntaxDataGrid.ItemsSource = syntaxErrors;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сканировании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Report_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://example.com";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetCode(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Keyword: return 1;
                case TokenType.Identifier: return 2;
                case TokenType.Punctuation: return 3;
                case TokenType.Whitespace: return 4;
                case TokenType.Error: return 99;
                default: return 0;
            }
        }

        private string GetTypeDescription(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Keyword:
                    return "ключевое слово";
                case TokenType.Identifier:
                    return "идентификатор";
                case TokenType.Punctuation:
                    return token.Value switch
                    {
                        "{" => "открывающая фигурная скобка",
                        "}" => "закрывающая фигурная скобка",
                        "," => "запятая",
                        ";" => "точка с запятой",
                        _ => "пунктуация"
                    };
                case TokenType.Whitespace:
                    return "разделитель (пробел)";
                case TokenType.Error:
                    return token.Value != null && token.Value.Length == 1
                        ? "Недопустимый символ"
                        : "Недопустимая последовательность";
                default:
                    return "неизвестно";
            }
        }

        private void OutputDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OutputDataGrid.SelectedItem is LexemeInfo selectedLexeme)
            {
                var index = FindPositionInText(FileContentViewer.Document, selectedLexeme.Line, selectedLexeme.StartColumn);
                TextPointer pointer = FileContentViewer.Document.ContentStart.GetPositionAtOffset(index);

                if (pointer != null)
                {
                    FileContentViewer.CaretPosition = pointer;
                    FileContentViewer.Focus();
                }
            }
        }

        private void SyntaxDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SyntaxDataGrid.SelectedItem is SyntaxError selectedError)
            {
                var index = FindPositionInText(FileContentViewer.Document, selectedError.Line, selectedError.StartColumn);
                TextPointer pointer = FileContentViewer.Document.ContentStart.GetPositionAtOffset(index);

                if (pointer != null)
                {
                    FileContentViewer.CaretPosition = pointer;
                    FileContentViewer.Focus();
                }
            }
        }

        private int FindPositionInText(FlowDocument document, int targetLine, int targetColumn)
        {
            TextPointer navigator = document.ContentStart;

            int currentLine = 1;
            int currentColumn = 1;

            while (navigator != null)
            {
                if (currentLine == targetLine && currentColumn == targetColumn)
                {
                    TextPointer corrected = navigator.GetInsertionPosition(LogicalDirection.Forward);
                    return document.ContentStart.GetOffsetToPosition(corrected);
                }

                var context = navigator.GetPointerContext(LogicalDirection.Forward);

                if (context == TextPointerContext.Text)
                {
                    string textRun = navigator.GetTextInRun(LogicalDirection.Forward);

                    for (int i = 0; i < textRun.Length; i++)
                    {
                        if (currentLine == targetLine && currentColumn == targetColumn)
                        {
                            TextPointer corrected = navigator.GetInsertionPosition(LogicalDirection.Forward);
                            return document.ContentStart.GetOffsetToPosition(corrected);
                        }

                        if (textRun[i] == '\r')
                            continue;

                        if (textRun[i] == '\n')
                        {
                            currentLine++;
                            currentColumn = 1;
                        }
                        else
                        {
                            currentColumn++;
                        }

                        navigator = navigator.GetPositionAtOffset(1, LogicalDirection.Forward);
                    }

                    continue;
                }

                if (context == TextPointerContext.ElementStart &&
                    navigator.GetAdjacentElement(LogicalDirection.Forward) is LineBreak)
                {
                    currentLine++;
                    currentColumn = 1;

                    navigator = navigator.GetPositionAtOffset(1, LogicalDirection.Forward);
                    continue;
                }

                if (context == TextPointerContext.ElementEnd &&
                    navigator.Parent is Paragraph)
                {
                    currentLine++;
                    currentColumn = 1;
                }

                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }

            return -1;
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                TextRange textRange = new TextRange(
                    FileContentViewer.Document.ContentStart,
                    FileContentViewer.Document.ContentEnd);

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

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".rtf")
                        textRange.Save(fs, DataFormats.Rtf);
                    else if (extension == ".txt")
                        textRange.Save(fs, DataFormats.Text);
                    else
                        textRange.Save(fs, DataFormats.Rtf);
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

        private void FileContentViewer_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextRange textRange = new TextRange(
                FileContentViewer.Document.ContentStart,
                FileContentViewer.Document.ContentEnd);
            string text = textRange.Text.Trim();

            bool hasContent = !string.IsNullOrWhiteSpace(text);

            if (hasContent)
            {
                if (string.IsNullOrEmpty(_currentFilePath) &&
                    text.Contains("enum class Day") &&
                    text.Contains("Monday") &&
                    text.Contains("Sunday"))
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

        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "NewFile"
                : Path.GetFileName(_currentFilePath);

            string modifiedMarker = _isFileModified ? "*" : "";
            this.Title = $"Компилятор: {fileName}{modifiedMarker}";
        }

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