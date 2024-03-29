using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace CallWall.Windows.Contract
{
    public interface IProvider : IProviderDescription, INotifyPropertyChanged
    {
        AuthorizationStatus Status { get; }
        IResourceScope[] AvailableServices { get; }
        ObservableCollection<IResourceScope> SelectedServices { get; }
        ICommand AuthorizeCommand { get; }
    }
}