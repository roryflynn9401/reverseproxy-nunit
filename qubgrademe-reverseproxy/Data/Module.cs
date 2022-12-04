using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qubgrademe_totalmarks.Data
{
    public class Module
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public double Mark { get; set; }


        public int StudentModuleId { get; set; }
        public StudentModules Student { get; set; }
    }
}
