using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Manager;

public class ContractPdfGenerator : IContractPdfGenerator
{
    static ContractPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(EmploymentContract contract, bool isPreview)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(c => ComposeHeader(c, contract, isPreview));
                page.Content().Element(c => ComposeContent(c, contract, isPreview));
                page.Footer().Element(c => ComposeFooter(c, contract));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, EmploymentContract contract, bool isPreview)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(12).AlignCenter();
                col.Item().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(11).AlignCenter();
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        });
    }

    private static void ComposeContent(IContainer container, EmploymentContract contract, bool isPreview)
    {
        container.PaddingVertical(10).Column(col =>
        {
            if (isPreview)
            {
                col.Item().PaddingBottom(10).Background(Colors.Amber.Lighten4).Padding(8).Row(r =>
                {
                    r.RelativeItem().Text("--- BẢN NHÁP (DRAFT PREVIEW) - KHÔNG CÓ GIÁ TRỊ PHÁP LÝ ---")
                        .Bold().FontSize(11).FontColor(Colors.Amber.Darken3).AlignCenter();
                });
            }

            col.Item().Text("HỢP ĐỒNG LAO ĐỘNG").Bold().FontSize(18).FontColor(Colors.Indigo.Darken3).AlignCenter();
            col.Item().Text($"Mã hợp đồng: {contract.ContractNumber}").FontSize(10).FontColor(Colors.Grey.Darken1).AlignCenter();

            col.Item().PaddingTop(15).Text("1. BÊN SỬ DỤNG LAO ĐỘNG (EMPLOYER)").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(130);
                    cols.RelativeColumn();
                });

                AddTableRow(table, "Chi nhánh:", contract.BranchNameSnapshot);
                AddTableRow(table, "Địa chỉ chi nhánh:", contract.BranchAddressSnapshot);
                AddTableRow(table, "Vùng lương áp dụng:", contract.RegionNameSnapshot);
            });

            col.Item().PaddingTop(15).Text("2. BÊN NGƯỜI LAO ĐỘNG (EMPLOYEE)").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(130);
                    cols.RelativeColumn();
                });

                AddTableRow(table, "Họ và tên:", contract.EmployeeFullNameSnapshot);
                AddTableRow(table, "Số điện thoại:", string.IsNullOrWhiteSpace(contract.EmployeePhoneSnapshot) ? "—" : contract.EmployeePhoneSnapshot);
                AddTableRow(table, "Số CMND / CCCD:", string.IsNullOrWhiteSpace(contract.EmployeeIdentityNoSnapshot) ? "—" : contract.EmployeeIdentityNoSnapshot);
                AddTableRow(table, "Địa chỉ thường trú:", string.IsNullOrWhiteSpace(contract.EmployeeAddressSnapshot) ? "—" : contract.EmployeeAddressSnapshot);
                AddTableRow(table, "Chức danh công việc:", contract.EmployeePositionSnapshot);
            });

            col.Item().PaddingTop(15).Text("3. NỘI DUNG VÀ ĐIỀU KHOẢN HỢP ĐỒNG").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(130);
                    cols.RelativeColumn();
                });

                AddTableRow(table, "Ngày bắt đầu:", contract.StartDate.ToString("dd/MM/yyyy"));
                AddTableRow(table, "Ngày kết thúc:", contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "Vô thời hạn (Indefinite)");
                AddTableRow(table, "Mức lương giờ:", $"{contract.HourlyRate:N0} VNĐ/giờ");
            });

            if (!string.IsNullOrWhiteSpace(contract.ContractTerms))
            {
                col.Item().PaddingTop(10).Text("Điều khoản cụ thể:").Bold();
                col.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(10).Text(contract.ContractTerms).FontSize(10);
            }

            col.Item().PaddingTop(25).Text("XÁC NHẬN CỦA CÁC BÊN").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Text("Hợp đồng này được khởi tạo và ghi nhận điện tử trên hệ thống WorkShift. Việc chấp nhận hợp đồng được thực hiện qua phương thức xác nhận tài khoản điện tử của nhân viên.").FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
        });
    }

    private static void ComposeFooter(IContainer container, EmploymentContract contract)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Mã HĐ: {contract.ContractNumber}").FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Trang ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static void AddTableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(2).Text(label).Bold().FontSize(10);
        table.Cell().PaddingVertical(2).Text(value).FontSize(10);
    }
}
