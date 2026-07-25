using EditWave.Models;
using LiteDB;

namespace EditWave.Services
{
    public class ProjectService
    {
        private const string DatabaseFile = "EditWave.db";
        public void SaveProject(Project project)
        {
            using (var db = new LiteDatabase(DatabaseFile))
            {
                var projects = db.GetCollection<Project>("projects");
                if (project.Id != 0)
                {
                    projects.Update(project);
                }
                else
                {
                    projects.Insert(project);
                }
            }
        }
        public List<Project> GetAllProjects()
        {
            using (var db = new LiteDatabase(DatabaseFile))
            {
                var projects = db.GetCollection<Project>("projects");
                return projects.Query().OrderByDescending(x => x.Id).ToList();
            }
        }
        public Project GetProjectById(int id)
        {
            using (var db = new LiteDatabase(DatabaseFile))
            {
                var projects = db.GetCollection<Project>("projects");
                return projects.FindById(id);
            }
        }
        public void DeleteProject(int id)
        {
            using (var db = new LiteDatabase(DatabaseFile))
            {
                var projects = db.GetCollection<Project>("projects");
                projects.Delete(id);
            }
        }
    }
}
