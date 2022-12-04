using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qubgrademe_totalmarks.Data
{
    public class StudentModules
    {
        public int Id { get; set; }

        public ICollection<Module> Modules { get; set; }
    }
}
