using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RecordingTest.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected BaseViewModel()
        {
            EnableCommands();
        }

        protected void EnableCommands()
        {
            foreach (var prop in GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(ICommand))
                {
                    if (prop.Name.EndsWith("Command"))
                    {
                        var name = prop.Name.Replace("Command", "");
                        var method = GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            var canExecute = GetType().GetMethod(name + "CanExecute", BindingFlags.NonPublic | BindingFlags.Instance);
                            prop.SetValue(this, new WiredCommand(this, method, canExecute));
                        }
                    }
                }
            }
        }
    }


    public class WiredCommand : ICommand
    {
        object instance;
        MethodInfo execute, canExecute;

        public WiredCommand(object instance, MethodInfo execute, MethodInfo canExecute = null)
        {
            this.instance = instance;
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (canExecute == null) return true;
            try
            {
                return (bool)canExecute.Invoke(instance, new object[] { parameter });
            }
            catch
            {
                return true;
            }
        }

        public void Execute(object parameter)
        {
            execute.Invoke(instance, new object[] { parameter });
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
