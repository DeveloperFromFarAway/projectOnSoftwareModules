//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PR3.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Table_InspectionActs
    {
        public int ID_InspectionActs { get; set; }
        public Nullable<int> ID_AlarmTriggers { get; set; }
        public int ID_SecurityObjects { get; set; }
        public int ID_Inspector_Employee { get; set; }
        public System.DateTime Date { get; set; }
        public string Findings { get; set; }
        public string Inspectionresult { get; set; }
        public bool iscriminalcaseinitiated { get; set; }
    
        public virtual Table_AlarmTriggers Table_AlarmTriggers { get; set; }
        public virtual Table_Employees Table_Employees { get; set; }
        public virtual Table_SecurityObjects Table_SecurityObjects { get; set; }
    }
}
