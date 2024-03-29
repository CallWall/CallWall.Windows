using System;
using System.ComponentModel;
using Microsoft.Practices.Prism.Commands;

namespace CallWall.Windows.Shell.Settings.Demonstration
{
    public sealed class DemoViewModel : INotifyPropertyChanged
    {
        private readonly IDemoProfileActivator _demoListener;
        private readonly DelegateCommand _activateIdentityCommand;
        private string _identity;

        public DemoViewModel(IDemoProfileActivator demoListener)
        {
            _demoListener = demoListener;
            _activateIdentityCommand = new DelegateCommand(ActivateIdentity, IsIdentityValid);

            this.PropertyChanges(vm => vm.Identity).Subscribe(_ => _activateIdentityCommand.RaiseCanExecuteChanged());
        }

        public string Identity
        {
            get { return _identity; }
            set
            {
                if (_identity != value)
                {
                    _identity = value;
                    OnPropertyChanged("Identity");
                }
            }
        }

        public DelegateCommand ActivateIdentityCommand
        {
            get { return _activateIdentityCommand; }
        }

        
        private bool IsIdentityValid()
        {
            return !string.IsNullOrWhiteSpace(Identity);
        }

        private void ActivateIdentity()
        {
            _demoListener.ActivateIdentity(Identity);
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}