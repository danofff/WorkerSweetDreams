using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerSweetDreams
{
    public enum gender { Male, Female };
        public class WorkerUser
    {
        public int UserID { get; set; }
        public Guid PersonalKey { get; set; }
        public gender Gender { get; set; }
        public gender LookingFor { get; set; }
                       
    }
}
