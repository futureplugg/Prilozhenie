using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ПагинацияКтиторов
{
    public partial class MainWindow : Window
    {
        private const string ConnectionString = "data source=stud-mssql.sttec.yar.ru,38325;initial catalog=user236_db;persist security info=True;user id=user236_db;password=user236;MultipleActiveResultSets=True;";

        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalPages;
        private bool _isSearch = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Формируем базовый SQL-запрос
                    var query = new StringBuilder(@"
                        SELECT 
                            COUNT(*) OVER () AS TotalCount,
                            u.id_user, u.familiya, u.imya, u.otchestvo, u.data_rozhdenia, u.id_roli,
                            r.naimenovanie AS RoleName
                        FROM 
                            User_PG u
                        LEFT JOIN 
                            Role_PG r ON u.id_roli = r.id_roli");

                    // Добавляем фильтрацию по поиску
                    if (_isSearch && !string.IsNullOrEmpty(SearchBox.Text))
                    {
                        query.Append(" WHERE u.familiya LIKE @SearchText OR u.imya LIKE @SearchText");
                    }

                    query.Append(" ORDER BY u.familiya OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        // Добавляем параметры
                        command.Parameters.AddWithValue("@SearchText", $"%{SearchBox.Text}%");
                        command.Parameters.AddWithValue("@Offset", (_currentPage - 1) * _pageSize);
                        command.Parameters.AddWithValue("@PageSize", _pageSize);

                        using (var reader = command.ExecuteReader())
                        {
                            var items = new List<User>();
                            int totalCount = 0;

                            while (reader.Read())
                            {
                                if (totalCount == 0)
                                {
                                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                                }

                                items.Add(new User
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id_user")),
                                    familiya = reader.GetString(reader.GetOrdinal("familiya")),
                                    imya = reader.GetString(reader.GetOrdinal("imya")),
                                    otchestvo = reader.IsDBNull(reader.GetOrdinal("otchestvo")) ? null : reader.GetString(reader.GetOrdinal("otchestvo")),
                                    data_rozhdenia = reader.GetDateTime(reader.GetOrdinal("data_rozhdenia")),
                                    Role = new Role
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("id_roli")),
                                        naimenovanie = reader.GetString(reader.GetOrdinal("RoleName"))
                                    }
                                });
                            }

                            _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                            _currentPage = Math.Max(1, Math.Min(_currentPage, _totalPages));

                            // Обновляем интерфейс
                            ListPags.ItemsSource = items;
                            curPage.Text = $"Страница {_currentPage} из {_totalPages}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isSearch = !string.IsNullOrEmpty(SearchBox.Text);
            _currentPage = 1;
            LoadData();
        }

        private void contCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (contCount.SelectedItem is ComboBoxItem selectedItem)
            {
                _pageSize = int.Parse(selectedItem.Content.ToString());
                _currentPage = 1;
                LoadData();
            }
        }

        private void prevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadData();
            }
        }

        private void nextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadData();
            }
        }
    }

    // Сущности для отображения данных
    public class User
    {
        public int Id { get; set; }
        public string familiya { get; set; }
        public string imya { get; set; }
        public string otchestvo { get; set; }
        public DateTime data_rozhdenia { get; set; }
        public Role Role { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public string naimenovanie { get; set; }
    }
}
