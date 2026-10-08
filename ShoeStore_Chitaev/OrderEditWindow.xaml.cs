using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace ShoeStore_Chitaev
{
    public partial class OrderEditWindow : Window
    {
        private readonly int? _orderId;

        // Работаем напрямую с EDM-сущностями
        private List<Order_Details> _orderItems = new List<Order_Details>();
        private int _nextOrderDetailId = 1;

        public OrderEditWindow(int? orderId)
        {
            InitializeComponent();
            _orderId = orderId;

            LoadPoints();
            LoadStatuses();
            LoadAllProducts();

            if (_orderId.HasValue)
            {
                HeaderText.Text = "Редактирование заказа";
                LoadOrderData(_orderId.Value);
            }
            else
            {
                HeaderText.Text = "Добавление заказа";
                OrderDatePicker.SelectedDate = DateTime.Today;
                DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(3);
            }

            RefreshOrderItems();
        }

        /// <summary>
        /// Загрузка пунктов выдачи.
        /// </summary>
        private void LoadPoints()
        {
            using (var context = ChitaevDBEntities.GetContext())
            {
                var points = context.Point
                    .ToList()
                    .Select(p => new
                    {
                        Point_ID = p.Point_ID,
                        FullAddress = p.City + ", " + p.Street + ", " + p.House
                    })
                    .ToList();

                PointComboBox.ItemsSource = points;
                PointComboBox.DisplayMemberPath = "FullAddress";
                PointComboBox.SelectedValuePath = "Point_ID";
            }
        }

        /// <summary>
        /// Загрузка статусов заказа.
        /// </summary>
        private void LoadStatuses()
        {
            using (var context = ChitaevDBEntities.GetContext())
            {
                var statuses = context.Order_Status
                    .OrderBy(s => s.Status_ID)
                    .ToList();

                StatusComboBox.ItemsSource = statuses;
                StatusComboBox.DisplayMemberPath = "StatusName";
                StatusComboBox.SelectedValuePath = "Status_ID";

                if (statuses.Count > 0)
                    StatusComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Загрузка всех товаров в ComboBox (анонимный тип).
        /// </summary>
        private void LoadAllProducts()
        {
            using (var context = ChitaevDBEntities.GetContext())
            {
                var products = context.Products
                    .OrderBy(p => p.Product_ID)
                    .ToList()
                    .Select(p => new
                    {
                        Product_ID = p.Product_ID,
                        Display = p.Article + " — " + p.Name +
                                  " (" + p.Price + " руб.)"
                    })
                    .ToList();

                AllProductsComboBox.ItemsSource = products;
                AllProductsComboBox.DisplayMemberPath = "Display";
                AllProductsComboBox.SelectedValuePath = "Product_ID";

                if (products.Count > 0)
                    AllProductsComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Загрузка существующего заказа.
        /// </summary>
        private void LoadOrderData(int orderId)
        {
            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    var order = context.Orders
                        .Include("Order_Details")
                        .Include("Order_Details.Products")
                        .FirstOrDefault(o => o.Order_ID == orderId);

                    if (order == null)
                    {
                        MessageBox.Show("Заказ не найден.", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        this.Close();
                        return;
                    }

                    PickupCodeBox.Text = order.PickupCode?.ToString() ?? string.Empty;
                    StatusComboBox.SelectedValue = order.Status_ID;     
                    PointComboBox.SelectedValue = order.PickupPoint_ID;
                    OrderDatePicker.SelectedDate = order.OrderDate;
                    DeliveryDatePicker.SelectedDate = order.DeliveryDate;

                    // Товары заказа
                    _orderItems = order.Order_Details.ToList();

                    _nextOrderDetailId = context.Order_Details.Any()
                                         ? context.Order_Details.Max(x => x.OrderDetail_ID) + 1
                                         : 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказа:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновление ListView товаров заказа.
        /// </summary>
        private void RefreshOrderItems()
        {
            OrderItemsList.ItemsSource = null;
            OrderItemsList.ItemsSource = _orderItems;
        }

        /// <summary>
        /// Добавить товар в заказ.
        /// </summary>
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (AllProductsComboBox.SelectedValue == null)
            {
                ErrorMessage.Text = "Выберите товар для добавления.";
                return;
            }

            if (!int.TryParse(QuantityBox.Text.Trim(), out int qty) || qty <= 0)
            {
                ErrorMessage.Text = "Введите корректное количество (целое > 0).";
                return;
            }

            int productId = (int)AllProductsComboBox.SelectedValue;

            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    var product = context.Products
                        .FirstOrDefault(p => p.Product_ID == productId);

                    if (product == null)
                    {
                        ErrorMessage.Text = "Товар не найден в базе.";
                        return;
                    }

                    var existing = _orderItems.FirstOrDefault(x => x.Product_ID == productId);
                    if (existing != null)
                    {
                        existing.Quantity = (existing.Quantity ?? 0) + qty;
                    }
                    else
                    {
                        _orderItems.Add(new Order_Details
                        {
                            OrderDetail_ID = _nextOrderDetailId++,
                            Product_ID = product.Product_ID,
                            Quantity = qty,
                            Products = product
                        });
                    }

                    ErrorMessage.Text = string.Empty;
                    QuantityBox.Text = "1";
                    RefreshOrderItems();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления товара:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Удалить товар из заказа.
        /// </summary>
        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItemsList.SelectedItem is Order_Details selected)
            {
                _orderItems.Remove(selected);
                RefreshOrderItems();
            }
            else
            {
                ErrorMessage.Text = "Выберите товар в списке для удаления.";
            }
        }

        /// <summary>
        /// Сохранение заказа.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PickupCodeBox.Text))
            {
                ErrorMessage.Text = "Введите артикул заказа.";
                return;
            }
            if (!int.TryParse(PickupCodeBox.Text.Trim(), out int pickupCode))
            {
                ErrorMessage.Text = "Артикул заказа должен быть числом.";
                return;
            }
            if (StatusComboBox.SelectedValue == null)
            {
                ErrorMessage.Text = "Выберите статус заказа.";
                return;
            }
            if (PointComboBox.SelectedValue == null)
            {
                ErrorMessage.Text = "Выберите адрес пункта выдачи.";
                return;
            }
            if (OrderDatePicker.SelectedDate == null)
            {
                ErrorMessage.Text = "Укажите дату заказа.";
                return;
            }
            if (_orderItems.Count == 0)
            {
                ErrorMessage.Text = "Добавьте хотя бы один товар в заказ.";
                return;
            }

            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    Orders order;
                    int orderId;

                    if (_orderId.HasValue)
                    {
                        // Редактирование
                        order = context.Orders
                            .FirstOrDefault(o => o.Order_ID == _orderId.Value);

                        if (order == null)
                        {
                            MessageBox.Show("Заказ не найден.", "Ошибка");
                            return;
                        }

                        orderId = order.Order_ID;
                    }
                    else
                    {
                        // Добавление 
                        order = new Orders();
                        int maxId = context.Orders.Any() ? context.Orders.Max(o => o.Order_ID) : 0;
                        order.Order_ID = maxId + 1;
                        order.Employee_ID = 1;   

                        context.Orders.Add(order);
                        orderId = order.Order_ID;
                    }

                    // Заполняем поля заказа
                    order.PickupCode = pickupCode;
                    order.Status_ID = (int)StatusComboBox.SelectedValue;
                    order.PickupPoint_ID = (int)PointComboBox.SelectedValue;
                    order.OrderDate = OrderDatePicker.SelectedDate;
                    order.DeliveryDate = DeliveryDatePicker.SelectedDate;

                    context.SaveChanges();

                    var oldDetails = context.Order_Details
                        .Where(od => od.Order_ID == orderId)
                        .ToList();

                    if (oldDetails.Any())
                    {
                        foreach (var detail in oldDetails)
                        {
                            context.Order_Details.Remove(detail);
                        }

                        context.SaveChanges();
                    }

                    int nextId = context.Order_Details.Any()
                                  ? context.Order_Details.Max(x => x.OrderDetail_ID) + 1
                                  : 1;

                    foreach (var item in _orderItems)
                    {
                        context.Order_Details.Add(new Order_Details
                        {
                            OrderDetail_ID = nextId++,       
                            Order_ID = orderId,
                            Product_ID = item.Product_ID,
                            Quantity = item.Quantity
                        });
                    }

                    context.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}