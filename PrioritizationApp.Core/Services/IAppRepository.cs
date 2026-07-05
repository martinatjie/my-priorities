using PrioritizationApp.Models;

namespace PrioritizationApp.Services;

public interface IAppRepository
{
    AppData Load();
    void Save(AppData data);
}
