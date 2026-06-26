using ParaAthleticsResults.ViewModels;

namespace ParaAthleticsResults;

public class NavigationService
{
    public event Action<ViewModelBase>? NavigationRequested;

    public void NavigateTo<T>() where T : ViewModelBase
    {
        var vm = App.Services.GetService(typeof(T)) as ViewModelBase;
        if (vm != null)
            NavigationRequested?.Invoke(vm);
    }

    public void NavigateTo(ViewModelBase viewModel) =>
        NavigationRequested?.Invoke(viewModel);
}
