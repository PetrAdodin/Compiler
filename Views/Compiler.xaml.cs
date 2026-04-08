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
using System.DirectoryServices;
using System.Net;

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

        // Создать новый файл
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
                {
                    Save_Click(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            SetInitialText();
            _currentFilePath = string.Empty;
        }

        // Открыть файл
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

        // Сохранить
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                SaveAs_Click(sender, e);
            else
                SaveToFile(_currentFilePath);
        }

        // Сохранить как
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
                saveFileDialog.FileName = "DayEnum.rtf";
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentFilePath = saveFileDialog.FileName;
                SaveToFile(_currentFilePath);
            }
        }

        // Выход
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

        // Справка
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

        // О программе
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

        // Пуск
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextRange textRange = new TextRange(
                    FileContentViewer.Document.ContentStart,
                    FileContentViewer.Document.ContentEnd);
                string code = textRange.Text;

                SearchRegex(code);

                var tokenizer = new CppTokenizer(code);
                var tokens = tokenizer.Tokenize();

                var lexemes = new List<LexemeInfo>();

                int i = 0;
                while (i < tokens.Count)
                {
                    Token token = tokens[i];

                    if (token.Type == TokenType.Error)
                    {
                        int startLine = token.Line;
                        int startColumn = token.StartColumn;
                        string errorText = token.Value;

                        int j = i + 1;
                        while (j < tokens.Count && tokens[j].Type == TokenType.Error)
                        {
                            errorText += tokens[j].Value;
                            j++;
                        }

                        int endLine = tokens[j - 1].Line;
                        int endColumn = tokens[j - 1].EndColumn;

                        lexemes.Add(new LexemeInfo
                        {
                            Code = 99,
                            Type = "недопустимая последовательность",
                            Lexeme = errorText,
                            Location = $"строка {startLine}, {startColumn}-{endColumn}",
                            Line = startLine,
                            StartColumn = startColumn,
                            EndColumn = endColumn
                        });

                        i = j;
                        continue;
                    }

                    // не ошибки
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

                    i++;
                }

                OutputDataGrid.ItemsSource = lexemes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сканировании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Получение кода лексемы
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

        // Получение описания типа лексемы
        private string GetTypeDescription(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Keyword: return "ключевое слово";
                case TokenType.Identifier: return "идентификатор";
                case TokenType.Punctuation:
                    return token.Value switch
                    {
                        "{" => "открывающая фигурная скобка",
                        "}" => "закрывающая фигурная скобка",
                        "," => "запятая",
                        ";" => "точка с запятой",
                        _ => "пунктуация"
                    };
                case TokenType.Whitespace: return "разделитель (пробел)";
                case TokenType.Error: return "недопустимый символ";
                default: return "неизвестно";
            }
        }

        // Навигация к ошибке по двойному клику
        private void OutputDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OutputDataGrid.SelectedItem is LexemeInfo selectedLexeme)
            {
                var content = new TextRange(FileContentViewer.Document.ContentStart, FileContentViewer.Document.ContentEnd).Text;
                var position = FindPositionInText(content, selectedLexeme.Line, selectedLexeme.StartColumn);

                if (position >= 0)
                {
                    var cursorPosition = FindTextPointerByIndex(FileContentViewer.Document.ContentStart, position);
                    if (cursorPosition is null)
                        return;

                    FileContentViewer.CaretPosition = cursorPosition;
                    FileContentViewer.Focus();
                }
            }
        }

        private void RegexDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RegexDataGrid.SelectedItem is RegexInfo selectedRegex)
            {
                var content = new TextRange(FileContentViewer.Document.ContentStart, FileContentViewer.Document.ContentEnd).Text;
                var position = FindPositionInText(content, selectedRegex.Line, selectedRegex.StartColumn);

                if (position >= 0)
                {
                    var cursorPosition = FindTextPointerByIndex(FileContentViewer.Document.ContentStart, position);
                    if (cursorPosition is null)
                        return;

                    FileContentViewer.CaretPosition = cursorPosition;
                    FileContentViewer.Focus();
                }
            }
        }

        private int FindPositionInText(string text, int line, int column)
        {
            int currentLine = 1;
            int currentColumn = 1;
            int countEscape = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (currentLine == line && currentColumn == column)
                    return i;

                if (text[i] == '\n')
                {
                    currentLine++;
                    currentColumn = 1;
                }
                else
                    currentColumn++;
            }

            return text.Length;
        }

        private static TextPointer FindTextPointerByIndex(TextPointer start, int targetIndex)
        {
            TextPointer current = start;
            int currentIndex = 0;

            while (current is not null && currentIndex <= targetIndex)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = current.GetTextInRun(LogicalDirection.Forward);
                    int runLength = textRun.Length + "\r\n".Length;

                    if (currentIndex + runLength > targetIndex)
                    {
                        int offset = targetIndex - currentIndex;
                        return current.GetPositionAtOffset(offset);
                    }

                    currentIndex += runLength;
                }

                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }

            return current ?? start;
        }

        // Сохранение в файл
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

        // Изменение текста
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

        // Обновление заголовка окна
        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath)
                ? "DayEnum"
                : Path.GetFileName(_currentFilePath);

            string modifiedMarker = _isFileModified ? "*" : "";
            this.Title = $"Сканер enum class Day: {fileName}{modifiedMarker}";
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

        private void SearchRegex(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Текст пуст. Введите данные для поиска.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RegexDataGrid.ItemsSource = null;
                    MatchesCountTextBlock.Text = "Найдено: 0";
                    return;
                }

                var results = new List<RegexInfo>();
                string selected = (SearchTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                switch (selected)
                {
                    case "Гласные буквы (кроме а/А)":
                        results = RegexSearchService.SearchRussianVowelsExceptA(text);
                        break;
                    case "Ethereum-адреса":
                        results = RegexSearchService.SearchEthereumAddresses(text);
                        break;
                    case "Надёжные пароли":
                        results = RegexSearchService.SearchStrongPasswords(text);
                        break;
                    case "Имена пользователей":
                        results = RegexSearchService.SearchUsername(text);
                        break;
                    default:
                        break;
                }

                RegexDataGrid.ItemsSource = results;
                MatchesCountTextBlock.Text = $"Найдено: {results?.Count ?? 0}";

                if (results != null && results.Count == 0)
                {
                    MessageBox.Show("Совпадений не найдено.", "Результат поиска", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Ошибка в регулярном выражении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}