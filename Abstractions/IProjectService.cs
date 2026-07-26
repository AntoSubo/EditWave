using EditWave.Models;

namespace EditWave.Abstractions
{
    public interface IProjectService
    {
        void SaveProject(Project project);
        List<Project> GetAllProjects();
        Project? GetProjectById(int id);
        void DeleteProject(int id);
    }
}
