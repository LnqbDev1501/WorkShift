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
                col.Item().Text("SOCIALIST REPUBLIC OF VIETNAM").Bold().FontSize(12).AlignCenter();
                col.Item().Text("Independence - Freedom - Happiness").Bold().FontSize(11).AlignCenter();
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
                    r.RelativeItem().Text("--- DRAFT PREVIEW - FOR REFERENCE ONLY, NO LEGAL VALIDITY ---")
                        .Bold().FontSize(11).FontColor(Colors.Amber.Darken3).AlignCenter();
                });
            }

            col.Item().Text("EMPLOYMENT CONTRACT").Bold().FontSize(18).FontColor(Colors.Indigo.Darken3).AlignCenter();
            col.Item().Text($"Contract No.: {contract.ContractNumber}").FontSize(10).FontColor(Colors.Grey.Darken1).AlignCenter();

            col.Item().PaddingTop(15).Text("1. EMPLOYER INFORMATION").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(130);
                    cols.RelativeColumn();
                });

                AddTableRow(table, "Branch:", contract.BranchNameSnapshot);
                AddTableRow(table, "Branch Address:", contract.BranchAddressSnapshot);
                AddTableRow(table, "Applicable Region:", contract.RegionNameSnapshot);
            });

            col.Item().PaddingTop(15).Text("2. EMPLOYEE INFORMATION").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(130);
                    cols.RelativeColumn();
                });

                var empDisplayName = !string.IsNullOrWhiteSpace(contract.EmployeeFullNameSnapshot)
                    ? contract.EmployeeFullNameSnapshot
                    : (!string.IsNullOrWhiteSpace(contract.User?.FullName) ? contract.User.FullName : "—");
                AddTableRow(table, "Full Name:", empDisplayName);
                AddTableRow(table, "Phone Number:", string.IsNullOrWhiteSpace(contract.EmployeePhoneSnapshot) ? "—" : contract.EmployeePhoneSnapshot);
                AddTableRow(table, "National ID / CCCD:", string.IsNullOrWhiteSpace(contract.EmployeeIdentityNoSnapshot) ? "—" : contract.EmployeeIdentityNoSnapshot);
                AddTableRow(table, "Permanent Address:", string.IsNullOrWhiteSpace(contract.EmployeeAddressSnapshot) ? "—" : contract.EmployeeAddressSnapshot);
                AddTableRow(table, "Position / Title:", contract.EmployeePositionSnapshot);
            });

            col.Item().PaddingTop(15).Text("3. CONTRACT CONTENT & TERMS").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(130);
                    cols.RelativeColumn();
                });

                AddTableRow(table, "Start Date:", contract.StartDate.ToString("dd/MM/yyyy"));
                AddTableRow(table, "End Date:", contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "Indefinite");
                AddTableRow(table, "Hourly Wage:", $"{contract.HourlyRate:N0} VND/hour");
            });

            if (!string.IsNullOrWhiteSpace(contract.ContractTerms))
            {
                col.Item().PaddingTop(10).Text("Specific Terms:").Bold();
                col.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(10).Text(contract.ContractTerms).FontSize(10);
            }

            col.Item().PaddingTop(20).Text("ACKNOWLEDGMENT OF THE PARTIES").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
            col.Item().PaddingTop(3).Text("This contract is generated and electronically authenticated on the WorkShift system. Accepting this contract carries the same legal validity as signing a physical document under applicable law.").FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken2);

            // Bố cục 2 cột: Bên trái là Nhân viên ký, Bên phải là WorkShift đóng mộc đỏ
            col.Item().PaddingTop(12).Row(row =>
            {
                // CỘT TRÁI: NGƯỜI LAO ĐỘNG
                row.RelativeItem().PaddingRight(12).Column(empCol =>
                {
                    empCol.Item().Text("EMPLOYEE REPRESENTATIVE").Bold().FontSize(10.5f).FontColor(Colors.Indigo.Darken3).AlignCenter();
                    empCol.Item().Text("(Signature and Full Name)").FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken1).AlignCenter();
                    empCol.Item().PaddingTop(8);

                    var empFullName = !string.IsNullOrWhiteSpace(contract.EmployeeFullNameSnapshot)
                        ? contract.EmployeeFullNameSnapshot
                        : (!string.IsNullOrWhiteSpace(contract.User?.FullName) ? contract.User.FullName : "Employee");

                    var isSigned = contract.StaffAcceptedAt.HasValue || contract.Status == "ACTIVE";
                    if (isSigned)
                    {
                        var signTime = (contract.StaffAcceptedAt ?? contract.UpdatedAt).AddHours(7);
                        empCol.Item().Border(1.5f).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(8).Column(box =>
                        {
                            box.Item().Text("✓ DIGITALLY SIGNED").Bold().FontSize(10.5f).FontColor(Colors.Blue.Darken3).AlignCenter();
                            box.Item().PaddingTop(4).Text(empFullName).Bold().FontSize(11).FontColor(Colors.Blue.Darken4).AlignCenter();
                            box.Item().PaddingTop(2).Text($"National ID: {contract.EmployeeIdentityNoSnapshot}").FontSize(8).FontColor(Colors.Grey.Darken3).AlignCenter();
                            box.Item().PaddingTop(2).Text($"Signed at: {signTime:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Darken3).AlignCenter();
                            box.Item().PaddingTop(2).Text("Verified securely via Staff account").FontSize(7.5f).Italic().FontColor(Colors.Blue.Darken2).AlignCenter();
                        });
                    }
                    else
                    {
                        empCol.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(12).Column(box =>
                        {
                            box.Item().PaddingVertical(14).Text("(Pending employee electronic acceptance)").FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken1).AlignCenter();
                            box.Item().Text(empFullName).Bold().FontSize(10).FontColor(Colors.Grey.Darken3).AlignCenter();
                        });
                    }
                });

                // CỘT PHẢI: ĐẠI DIỆN HỆ THỐNG WORKSHIFT
                row.RelativeItem().PaddingLeft(12).Column(companyCol =>
                {
                    companyCol.Item().Text("WORKSHIFT COMPANY REPRESENTATIVE").Bold().FontSize(10.5f).FontColor(Colors.Indigo.Darken3).AlignCenter();
                    companyCol.Item().Text("(Authorized Signature & Official Seal)").FontSize(8.5f).Italic().FontColor(Colors.Grey.Darken1).AlignCenter();
                    companyCol.Item().PaddingTop(8);

                    var authTime = (contract.ApprovedAt ?? contract.CreatedAt).AddHours(7);
                    companyCol.Item().Border(1.5f).BorderColor(Colors.Red.Darken2).Background(Colors.Red.Lighten5).Padding(8).Column(box =>
                    {
                        var stampBytes = TryLoadStampImage();
                        if (stampBytes != null && stampBytes.Length > 0)
                        {
                            box.Item().Height(42).AlignCenter().Image(stampBytes).FitArea();
                        }
                        else
                        {
                            box.Item().Border(2).BorderColor(Colors.Red.Darken2).Padding(4).AlignCenter().Text("WorkShift")
                                .Bold().FontSize(17).FontColor(Colors.Red.Darken2);
                        }

                        box.Item().PaddingTop(3).Text("✓ VERIFIED BY SYSTEM").Bold().FontSize(8.5f).FontColor(Colors.Red.Darken3).AlignCenter();
                        box.Item().PaddingTop(1).Text($"Branch: {contract.BranchNameSnapshot}").FontSize(8).FontColor(Colors.Grey.Darken3).AlignCenter();
                        box.Item().PaddingTop(1).Text($"Authenticated at: {authTime:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken3).AlignCenter();
                        box.Item().PaddingTop(1).Text($"Contract No.: {contract.ContractNumber}").FontSize(7.5f).FontColor(Colors.Grey.Darken2).AlignCenter();
                    });
                });
            });
        });
    }

    private static byte[]? _cachedStampImage;
    private static byte[]? TryLoadStampImage()
    {
        if (_cachedStampImage != null) return _cachedStampImage;

        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Resources", "workshift_stamp.png"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WorkShiftManagement.BLL", "Resources", "workshift_stamp.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "Resources", "workshift_stamp.png"),
            @"d:\DATN\CODE\WorkShift\WorkShiftServer\WorkShiftManagement.BLL\Resources\workshift_stamp.png",
            @"d:\DATN\CODE\WorkShift\WorkShiftServer\WorkShiftServer.API\Resources\workshift_stamp.png"
        };

        foreach (var path in candidatePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    _cachedStampImage = File.ReadAllBytes(path);
                    return _cachedStampImage;
                }
            }
            catch { }
        }
        return null;
    }

    private static void ComposeFooter(IContainer container, EmploymentContract contract)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Contract No.: {contract.ContractNumber}").FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
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
