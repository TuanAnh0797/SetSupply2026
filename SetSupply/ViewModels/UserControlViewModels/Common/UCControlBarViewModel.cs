using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace APP.ViewModels
{
    public partial class UCControlBarViewModel: ObservableObject
    {
        public UCControlBarViewModel()
        {
            
        }
        [RelayCommand]
        public void CloseWindow(UserControl p)
        {
            var MyWindow = Window.GetWindow(p);
            if (MyWindow is MainWindow)
            {
                if (MessageBox.Show("Bạn chắc chắn muốn thoát chương trình không?", "Thông báo", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {

                    if (MyWindow != null)
                    {
                        MyWindow.Close();
                    }
                }
            }
            else
            {
                if (MyWindow != null)
                {
                    MyWindow.Close();
                }
            }

        }
        [RelayCommand]
        public void NormalWindow(UserControl p)
        {
            var MyWindow = Window.GetWindow(p);
            if (MyWindow != null)
            {
                if (MyWindow.WindowState == WindowState.Maximized)
                {
                    MyWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    MyWindow.WindowState = WindowState.Maximized;
                }
            }

        }
        [RelayCommand]
        public void MinimizeWindow(UserControl p)
        {
            var MyWindow = Window.GetWindow(p);
            if (MyWindow != null)
            {
                MyWindow.WindowState = WindowState.Minimized;
            }
        }
        [RelayCommand]
        public void MoveWindow(UserControl p)
        {
            try
            {
                var Mywindow = Window.GetWindow(p);
                if (Mywindow != null)
                {
                    Mywindow.DragMove();
                }
            }
            catch (Exception)
            {

               
            }
            
        }
    }
}
