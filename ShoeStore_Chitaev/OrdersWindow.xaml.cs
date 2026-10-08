using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ShoeStore_Chitaev
{
    public partial class OrdersWindow : Window
    {
        private readonly int _roleId;
        private List<Orders> _allOrders = new List<Orders>();

        public OrdersWindow(int roleId)
        {
            InitializeComponent();
            _roleId = roleId;

            // Кнопки "Добавить"/"Удалить" — только для администратора (roleId == 1)
            AddOrderButton.Visibility = (_roleId == 1) ? Visibility.Visible : Visibility.Collapsed;
            DeleteOrderButton.Visibility = (_roleId == 1) ? Visibility.Visible : Visibility.Collapsed;

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    _allOrders = context.Orders
                        .Include("Order_Status")
                        .Include("Point")
                        .Include("Order_Details")             
                        .Include("Order_Details.Products")    
                        .OrderByDescending(o => o.Order_ID)
                        .ToList();
                }

                OrdersList.ItemsSource = _allOrders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Двойной клик по заказу — редактирование (только админ).
        /// </summary>
        private void OrdersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_roleId != 1) return;

            var selected = OrdersList.SelectedItem as Orders;
            if (selected == null) return;

            var editWindow = new OrderEditWindow(selected.Order_ID);
            if (editWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        /// <summary>
        /// Кнопка "Добавить заказ" — только админ.
        /// </summary>
        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId != 1) return;

            var editWindow = new OrderEditWindow(null);
            if (editWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        /// <summary>
        /// Удаление выбранного заказа (только администратор).
        /// </summary>
        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_roleId != 1) return;

            var selected = OrdersList.SelectedItem as Orders;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ для удаления!",
                                "Внимание",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить заказ №{selected.PickupCode}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    // 1) Удаляем связанные Order_Details
                    var details = context.Order_Details
                        .Where(od => od.Order_ID == selected.Order_ID)
                        .ToList();

                    foreach (var detail in details)
                    {
                        context.Order_Details.Remove(detail);
                    }

                    context.SaveChanges();  

                    // 2) Удаляем сам заказ
                    var orderToDelete = context.Orders
                        .FirstOrDefault(o => o.Order_ID == selected.Order_ID);

                    if (orderToDelete != null)
                    {
                        context.Orders.Remove(orderToDelete);
                        context.SaveChanges();
                    }
                }

                MessageBox.Show("Заказ успешно удалён!",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                LoadOrders();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.InnerException?.Message
                            ?? dbEx.InnerException?.Message
                            ?? dbEx.Message;

                MessageBox.Show($"Ошибка БД:\n{inner}",
                                "Ошибка удаления",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления:\n{ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}