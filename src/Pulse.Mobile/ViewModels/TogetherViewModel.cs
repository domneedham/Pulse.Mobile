using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;

namespace Pulse.ViewModels;

/// <summary>The Together tab — placeholder for now, replacing Profile as a tab (Profile moved to a
/// toolbar item reachable from every tab). Content TBD.</summary>
public partial class TogetherViewModel(INavigationService navigationService) : ObservableObject
{
    [RelayCommand]
    private Task OpenCompose() => navigationService.GoToAsync(Navigation.Relative().Push<ComposeSheetViewModel>());
}
