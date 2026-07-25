using System;

namespace EditWave.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }  
        public DateTime LastModified { get; set; }
        public Project()
        {
            CreatedAt = DateTime.Now;
            LastModified = DateTime.Now;
        }
    }
}