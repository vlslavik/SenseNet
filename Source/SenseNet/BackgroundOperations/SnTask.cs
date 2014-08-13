using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.BackgroundOperations
{
    public class SnTask
    {
        //[Column(Order = 0)]
        public int Id { get; set; }

        //[Column(Order = 1)]
        public string Type { get; set; }

        //[Column(Order = 2)]
        public double Order { get; set; }

        //[Column(Order = 3)]
        //[Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime RegisteredAt { get; set; }

        //[Column(Order = 4)]
        public DateTime? LastLockUpdate { get; set; }

        //[Column(Order = 5), MaxLength(450)]
        public string LockedBy { get; set; }

        //[Column(Order = 6), MaxLength(1000)]
        public string TaskKey { get; set; }

        //[Column(Order = 7)]
        public int Hash { get; set; }

        //[Column(Order = 8, TypeName = "ntext"), MaxLength]
        public string TaskData { get; set; }
    }
}
