using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ShoeStore_Chitaev
{
    public partial class AddEditTovarWindow : Window
    {
        // Текущий товар (новый или редактируемый)
        private Products _currentTovar = new Products();

        // Имя файла изображения (только название, без пути)
        private string _photoPath = "picture.png";

        // Папка, куда сохраняем изображения (в папке приложения)
        private readonly string _targetDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        // Конструктор. Если selTovar == null — добавление, иначе — редактирование.
        public AddEditTovarWindow(Products selTovar)
        {
            InitializeComponent();

            // Создаём папку Images, если её нет
            if (!Directory.Exists(_targetDirectory))
                Directory.CreateDirectory(_targetDirectory);

            // Загружаем списки
            LoadCategories();
            LoadManufacturers();
            LoadSuppliers();

            if (selTovar != null && selTovar.Product_ID != 0)
            {
                // Режим редактирования
                _currentTovar = selTovar;
                Title = "Редактирование товара";

                // Заполняем поля вручную (без навигационных свойств)
                FillFields();
            }
            else
            {
                // Режим добавления: вычисляем новый ID = max + 1
                using (var context = ChitaevDBEntities.GetContext())
                {
                    int maxId = context.Products.Any()
                        ? context.Products.Max(p => p.Product_ID)
                        : 0;

                    int newId = maxId + 1;
                    // Защита от дубликата
                    while (context.Products.Any(p => p.Product_ID == newId))
                        newId++;

                    _currentTovar.Product_ID = newId;

                    // Артикул по умолчанию
                    _currentTovar.Article = $"ART-{_currentTovar.Product_ID:D4}";
                }

                Title = "Добавление товара";
            }

            // Фото
            _photoPath = string.IsNullOrWhiteSpace(_currentTovar.Photo)
                ? "picture.png"
                : Path.GetFileName(_currentTovar.Photo);

            FileNameTb.Text = _photoPath;
            PhotoTovarIm.Source = LoadImageOrDefault(_photoPath);
        }

        // Заполнение полей при редактировании
        private void FillFields()
        {
            NameTb.Text = _currentTovar.Name;
            DescriptionTb.Text = _currentTovar.Description;
            PriceTb.Text = _currentTovar.Price?.ToString();
            UnitTb.Text = _currentTovar.Unit;
            StockTb.Text = _currentTovar.Stock_Quantity?.ToString();
            DiscountTb.Text = _currentTovar.Discount?.ToString();

            // ComboBox — выбираем по ID
            if (_currentTovar.Category_ID.HasValue)
            {
                CategoryCombo.SelectedValue = _currentTovar.Category_ID.Value;
            }

            if (_currentTovar.Manufacturer_ID.HasValue)
            {
                ManufacturerCombo.SelectedValue = _currentTovar.Manufacturer_ID.Value;
            }

            if (_currentTovar.Supplier_ID.HasValue)
            {
                SupplierCombo.SelectedValue = _currentTovar.Supplier_ID.Value;
            }
        }

        // Загрузка справочников
        private void LoadCategories()
        {
            using (var context = ChitaevDBEntities.GetContext())
            {
                CategoryCombo.ItemsSource = context.Categories.ToList();
            }
        }

        private void LoadManufacturers()
        {
            using (var context = ChitaevDBEntities.GetContext())
            {
                ManufacturerCombo.ItemsSource = context.Manufacturers.ToList();
            }
        }

        private void LoadSuppliers()
        {
            using (var context = ChitaevDBEntities.GetContext())
            {
                SupplierCombo.ItemsSource = context.Suppliers.ToList();
            }
        }

        // Загрузка картинки
        private BitmapImage LoadImageOrDefault(string fileName)
        {
                string fullPath = Path.Combine(_targetDirectory, fileName);
                if (File.Exists(fullPath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return bmp;
                }

            // Заглушка

                const string placeholder =
                    @"Resources\picture.png";
                if (File.Exists(placeholder))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(placeholder, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    return bmp;
                }


            return null;
        }

        // Кнопка "Выбрать изображение"
        private void SelectImageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                dialog.DefaultExt = ".png";

                if (dialog.ShowDialog() == true)
                {
                    string sourceFilePath = dialog.FileName;
                    string oldFileName = _photoPath;       // запоминаем старое имя
                    _photoPath = dialog.SafeFileName;      // новое имя
                    FileNameTb.Text = _photoPath;

                    string newPath = Path.Combine(_targetDirectory, _photoPath);

                    //Удаляем старое фото (если не заглушка и не совпадает с новым)
                    if (!string.IsNullOrEmpty(oldFileName) &&
                        oldFileName != "picture.png" &&
                        oldFileName != _photoPath)
                    {
                        string oldPath = Path.Combine(_targetDirectory, oldFileName);
                        if (File.Exists(oldPath))
                            File.Delete(oldPath);
                    }

                    //Ресайз до 300×200
                    var original = new BitmapImage(new Uri(sourceFilePath));
                    var resized = new TransformedBitmap(original,
                        new System.Windows.Media.ScaleTransform(
                            300.0 / original.PixelWidth,
                            200.0 / original.PixelHeight));

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(resized));

                    using (var fs = new FileStream(newPath, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    // Отображаем в превью
                    PhotoTovarIm.Source = new BitmapImage(new Uri(newPath));

                    // Запоминаем в текущем товаре (только имя)
                    _currentTovar.Photo = _photoPath;

                    MessageBox.Show("Изображение товара успешно установлено!",
                                    "Успех",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка работы с изображением: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // Валидация ввода
        private void PriceTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string text = (sender as TextBox)?.Text ?? "";
            e.Handled = !IsValidDecimalInput(text + e.Text);
        }

        private void StockTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private bool IsValidDecimalInput(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            text = text.Replace(',', '.');
            return decimal.TryParse(text, out _);
        }

        // Кнопка "Сохранить данные"
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
                if (string.IsNullOrWhiteSpace(NameTb.Text))
                {
                    MessageBox.Show("Введите наименование товара!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CategoryCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите категорию!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ManufacturerCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите производителя!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SupplierCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите поставщика!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal price;
                string priceText = (PriceTb.Text ?? "").Replace(',', '.');
                if (!decimal.TryParse(priceText,
                                      System.Globalization.NumberStyles.Any,
                                      System.Globalization.CultureInfo.InvariantCulture,
                                      out price) || price < 0)
                {
                    MessageBox.Show("Введите корректную неотрицательную цену!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int stock;
                if (!int.TryParse(StockTb.Text, out stock) || stock < 0)
                {
                    MessageBox.Show("Количество на складе не может быть отрицательным!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int discount;
                if (!int.TryParse(DiscountTb.Text, out discount) || discount < 0)
                {
                    MessageBox.Show("Скидка не может быть отрицательной!",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentTovar.Name = NameTb.Text.Trim();
                _currentTovar.Description = DescriptionTb.Text?.Trim();
                _currentTovar.Price = (int)price;
                _currentTovar.Unit = UnitTb.Text?.Trim() ?? "шт.";
                _currentTovar.Stock_Quantity = stock;
                _currentTovar.Discount = discount;

                var cat = CategoryCombo.SelectedItem as Categories;
                _currentTovar.Category_ID = cat?.Category_ID;
                _currentTovar.Category = cat?.Name;
                _currentTovar.Categories = null;   

                var man = ManufacturerCombo.SelectedItem as Manufacturers;
                _currentTovar.Manufacturer_ID = man?.Manufacturer_ID;
                _currentTovar.Manufacturers = null; 

                var sup = SupplierCombo.SelectedItem as Suppliers;
                _currentTovar.Supplier_ID = sup?.Supplier_ID;
                _currentTovar.Supplier = sup?.Name;
                _currentTovar.Suppliers = null;    

                // Артикул
                if (string.IsNullOrWhiteSpace(_currentTovar.Article))
                {
                    _currentTovar.Article = $"ART-{_currentTovar.Product_ID:D4}";
                }

                // Фото
                _currentTovar.Photo = _photoPath;

                using (var context = ChitaevDBEntities.GetContext())
                {
                    var existing = context.Products
                        .FirstOrDefault(p => p.Product_ID == _currentTovar.Product_ID);

                    if (existing == null)
                    {
                        // Защита от дубликата ID
                        int newId = _currentTovar.Product_ID;
                        while (context.Products.Any(p => p.Product_ID == newId))
                            newId++;
                        _currentTovar.Product_ID = newId;

                        // Добавление
                        context.Products.Add(_currentTovar);
                    }
                    else
                    {
                        // Редактирование — синхронизируем все поля
                        existing.Article = _currentTovar.Article;
                        existing.Name = _currentTovar.Name;
                        existing.Unit = _currentTovar.Unit;
                        existing.Price = _currentTovar.Price;

                        existing.Supplier_ID = _currentTovar.Supplier_ID;
                        existing.Supplier = _currentTovar.Supplier;

                        existing.Manufacturer_ID = _currentTovar.Manufacturer_ID;

                        existing.Category_ID = _currentTovar.Category_ID;
                        existing.Category = _currentTovar.Category;

                        existing.Discount = _currentTovar.Discount;
                        existing.Stock_Quantity = _currentTovar.Stock_Quantity;
                        existing.Description = _currentTovar.Description;
                        existing.Photo = _currentTovar.Photo;

                    }

                    context.SaveChanges();
                }

                MessageBox.Show("Данные успешно сохранены!",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                DialogResult = true;
                this.Close();

        }
    }
}
