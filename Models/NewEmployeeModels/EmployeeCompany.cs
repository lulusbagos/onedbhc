using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.NewEmployeeModels
{
    [Table("tbl_employee_company")]
    public class EmployeeCompany
    {
        [Key]
        public Guid Id { get; set; }
        
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string CompanyType { get; set; }
        
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}