using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IContractPdfGenerator
{
    byte[] GeneratePdf(EmploymentContract contract, bool isPreview);
}
