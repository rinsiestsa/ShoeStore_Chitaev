using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShoeStore_Chitaev
{
    public partial class AuthorizationWindow : Window
    {
        public AuthorizationWindow()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorMessageTextBlock.Text = "Пожалуйста, заполните все поля!";
                return;
            }

            try
            {
                using (var context = ChitaevDBEntities.GetContext())
                {
                    var employee = context.Employees
                        .FirstOrDefault(emp => emp.Login == login && emp.Pasword == password);

                    if (employee == null)
                    {
                        ErrorMessageTextBlock.Text = "Неверный логин или пароль!";
                        return;
                    }

                    string roleName = employee.Roles?.RoleName ?? "Неизвестная роль";

                    var mainWindow = new MainMenuWindow(
                        employee.FIO,
                        roleName,
                        employee.Id_Role ?? 0);

                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainMenuWindow("Гость", "Гость", 0);
            mainWindow.Show();
            this.Close();
        }
    }
}
