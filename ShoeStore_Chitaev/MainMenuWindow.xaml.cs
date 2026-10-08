using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShoeStore_Chitaev
{
    public partial class MainMenuWindow : Window
    {
        private List<Products> _allProducts = new List<Products>();
        private int _sortMode = 0;  // 0 — без сортировки, 1 — по возрастанию, 2 — по убыванию
        private int _roleId = 0;
        private bool _isLoaded = false;

        public MainMenuWindow()
        {
            InitializeComponent();
            LoadProducts();
            LoadSuppliers();
            _isLoaded = true;
        }

        public MainMenuWindow(string fio, string roleName, int roleId) : this()
        {
            _roleId = roleId;

            UserInfoTextBlock.Text = (roleId == 0) ? "Гость" : fio;
            RoleInfoTextBlock.Text = roleName;

            ConfigureAccess(roleId);
        }

        /// Настройка доступа:
        /// Администратор (1) — полный доступ (фильтры + кнопки управления).
        /// Менеджер (2) — фильтры без кнопок управления.
        /// Гость (0) и Клиент (3) — только просмотр.
        private void ConfigureAccess(int roleId)
        {
            switch (roleId)
            {
                case 1: // Администратор
                    Title = "ООО Обувь — Список товаров (Администратор)";
                    OrdersButton.Visibility = Visibility.Visible;
                    break;
                case 2: // Менеджер
                    Title = "ООО Обувь — Список товаров (Менеджер)";
                    OrdersButton.Visibility = Visibility.Visible;
                    break;
                case 0: // Гость
                    Title = "ООО Обувь — Список товаров (Гость)";
                    FilterPanel.Visibility = Visibility.Collapsed;
                    OrdersButton.Visibility = Visibility.Collapsed;
                    break;
                case 3: // Клиент
                    Title = "ООО Обувь — Список товаров (Клиент)";
                    FilterPanel.Visibility = Visibility.Collapsed;
                    OrdersButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    _allProducts = context.Products
                        .Include("Categories")
                        .Include("Manufacturers")
                        .Include("Suppliers")
                        .OrderBy(p => p.Product_ID)
                        .ToList();
                }

                ListTovar.ItemsSource = _allProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    var suppliers = context.Suppliers
                        .OrderBy(s => s.Name)
                        .Select(s => s.Name)
                        .ToList();

                    suppliers.Insert(0, "Все поставщики");
                    SupplierComboBox.ItemsSource = suppliers;
                    SupplierComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filtr()
        {
            if (!_isLoaded) return;
            if (SupplierComboBox == null || SearchTextBox == null) return;
            if (ListTovar == null || _allProducts == null) return;

            var searchList = _allProducts.AsEnumerable();

            // 1) Фильтр по поставщику
            if (SupplierComboBox.SelectedIndex > 0 &&
                SupplierComboBox.SelectedItem is string supplierName &&
                supplierName != "Все поставщики")
            {
                searchList = searchList.Where(x =>
                    x.Suppliers != null && x.Suppliers.Name == supplierName);
            }

            // 2) Поиск по всем текстовым полям
            string query = SearchTextBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(query))
            {
                searchList = searchList.Where(x =>
                    (!string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(x.Article) && x.Article.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(x.Unit) && x.Unit.ToLower().Contains(query)) ||
                    (x.Categories != null && !string.IsNullOrEmpty(x.Categories.Name) &&
                        x.Categories.Name.ToLower().Contains(query)) ||
                    (x.Manufacturers != null && !string.IsNullOrEmpty(x.Manufacturers.Name) &&
                        x.Manufacturers.Name.ToLower().Contains(query)) ||
                    (x.Suppliers != null && !string.IsNullOrEmpty(x.Suppliers.Name) &&
                        x.Suppliers.Name.ToLower().Contains(query))
                );
            }

            // 3) Сортировка (сохраняется при поиске и фильтрации)
            switch (_sortMode)
            {
                case 1: searchList = searchList.OrderBy(x => x.Stock_Quantity); break;
                case 2: searchList = searchList.OrderByDescending(x => x.Stock_Quantity); break;
            }

            ListTovar.ItemsSource = searchList.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => Filtr();

        private void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => Filtr();

        private void SortAscButton_Click(object sender, RoutedEventArgs e)
        {
            _sortMode = 1;
            Filtr();
        }

        private void SortDescButton_Click(object sender, RoutedEventArgs e)
        {
            _sortMode = 2;
            Filtr();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SupplierComboBox.SelectedIndex = 0;
            _sortMode = 0;
            Filtr();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var authWindow = new AuthorizationWindow();
                authWindow.Show();
                this.Close();
            }
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            var ordersWindow = new OrdersWindow(_roleId);
            ordersWindow.Show();
        }
        /// Добавление нового товара.
        private void AddTovarBtn_Click(object sender, RoutedEventArgs e)
        {
            // Защита: только одно окно редактирования
            foreach (Window w in Application.Current.Windows)
            {
                if (w is AddEditTovarWindow)
                {
                    MessageBox.Show("Окно редактирования уже открыто!",
                                    "Внимание",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }
            }

            var addWindow = new AddEditTovarWindow(null);
            if (addWindow.ShowDialog() == true)
            {
                // Обновляем список после добавления
                LoadProducts();
                LoadSuppliers();
                Filtr();
            }
        }

        /// Редактирование товара двойным кликом (только администратор).
        private void ListTovar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Только администратор
            if (_roleId != 1) return;

            var selected = ListTovar.SelectedItem as Products;
            if (selected == null) return;

            // Защита: только одно окно редактирования
            foreach (Window w in Application.Current.Windows)
            {
                if (w is AddEditTovarWindow)
                {
                    MessageBox.Show("Окно редактирования уже открыто!",
                                    "Внимание",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }
            }

            var editWindow = new AddEditTovarWindow(selected);
            if (editWindow.ShowDialog() == true)
            {
                LoadProducts();
                LoadSuppliers();
                Filtr();
            }
        }

        /// Удаление выбранного товара (только администратор).
        private void DeleteTovarBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = ListTovar.SelectedItem as Products;
            if (selected == null)
            {
                MessageBox.Show("Выберите товар для удаления!",
                                "Внимание",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить товар «{selected.Name}»?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    // Проверяем, есть ли товар в заказах
                    var inOrders = context.Order_Details
                        .Any(od => od.Product_ID == selected.Product_ID);

                    if (inOrders)
                    {
                        MessageBox.Show("Товар присутствует в заказах и не может быть удалён!",
                                        "Ошибка удаления",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    // Удаляем
                    var toDelete = context.Products
                        .FirstOrDefault(p => p.Product_ID == selected.Product_ID);

                    if (toDelete != null)
                    {
                        context.Products.Remove(toDelete);
                        context.SaveChanges();
                    }
                }

                MessageBox.Show("Товар успешно удалён!",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                LoadProducts();
                Filtr();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}