using Pulse.UI;
using Pulse.ViewModels;

namespace Pulse.Views;

public partial class ProfileView : ContentPage
{
    public ProfileView(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        LargeTitles.Enable(this);
    }
}
