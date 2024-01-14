using System;
using System.Windows.Input;

namespace File_Organizer
{
    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> executeAction;

        private readonly Func<T, bool> canExecuteAction;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<T> executeAction, Func<T, bool> canExecuteAction)
        {
            this.executeAction = executeAction;
            this.canExecuteAction = canExecuteAction;
        }

        public DelegateCommand(Action<T> executeAction)
            : this(executeAction, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            if (canExecuteAction != null)
            {
                return canExecuteAction((T)parameter);
            }

            return true;
        }

        public void Execute(object parameter)
        {
            executeAction((T)parameter);
        }

        public void InvalidateCanExecute()
        {
            CanExecuteChanged?.Invoke(this, null);
        }
    }
}
