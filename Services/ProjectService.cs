using EditWave.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public void UpdateProjectName(int id, string newName)
        {
            using (var db = new LiteDatabase(DatabaseFile))
            {
                var projects = db.GetCollection<Project>("projects");
                var project = projects.FindById(id);
                if (project != null)
                {
                    project.Name = newName;
                    projects.Update(project);
                }
            }
        }

    }
}
