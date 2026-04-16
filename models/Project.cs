using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditWave.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }  
        public DateTime LastModified { get; set; }
        public Project()
        {
            CreatedAt = DateTime.Now;
            LastModified = DateTime.Now;
        }
    }
}