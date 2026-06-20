using Pulse.ViewModels;

namespace Pulse.Views;

public partial class EditProfileView : ContentPage
{
    public EditProfileView(EditProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
