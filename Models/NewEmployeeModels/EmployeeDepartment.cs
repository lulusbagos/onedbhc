using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.NewEmployeeModels
{
    [Table("tbl_employee_department")]
    public class EmployeeDepartment
    {
        [Key]
        public Guid Id { get; set; }
        
        public string DeptCode { get; set; }
        public string DepartmentName { get; set; }
        public string CompanyCode { get; set; }
        
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}