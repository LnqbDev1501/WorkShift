import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import '../../controllers/contracts/staff_contract_controller.dart';
import '../../models/contracts/staff_contract_model.dart';
import '../../services/auth/auth_service.dart';
import '../../theme/app_colors.dart';
import '../../utils/file_download_helper.dart';
import '../../widgets/app_button.dart';
import '../../widgets/status_badge.dart';
import 'submit_contract_change_request_view.dart';

/// Screen: Staff Employment Contract Details
/// Matches UI_Admin/Views/StaffContracts/Details.cshtml:
/// - Dynamic Status Banners (Pending Acceptance, Active, Scheduled, Cancelled, Terminated)
/// - Shift Eligibility Policy Banner
/// - 3-Card Information Grid (Position & Hourly Rate, Work Location, Period & Lifecycle)
/// - Contract Terms
/// - Embedded Official Visual Contract Document with Two-Party Signatures & Red Certified Stamp
/// - Open PDF in Browser Tab (Chrome) & Download PDF (.pdf file)
/// - Two-Party Digital Signature Modal (Employee Signature + WorkShift Official Certified Seal)
/// - Contract Change Requests History with Manager Feedback
class StaffContractDetailView extends StatefulWidget {
  final int contractId;
  final StaffContractController controller;

  const StaffContractDetailView({
    super.key,
    required this.contractId,
    required this.controller,
  });

  @override
  State<StaffContractDetailView> createState() =>
      _StaffContractDetailViewState();
}

class _StaffContractDetailViewState extends State<StaffContractDetailView> {
  String? _localStaffName;

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerUpdate);
    widget.controller.loadContractDetail(widget.contractId);
    _loadLocalStaffName();
  }

  Future<void> _loadLocalStaffName() async {
    try {
      final user = await AuthService().getSavedUser();
      if (user != null && user['fullName'] != null) {
        if (mounted) {
          setState(() {
            _localStaffName = user['fullName']?.toString();
          });
        }
      }
    } catch (_) {}
  }

  String _getEmployeeDisplayName(StaffContractDetailModel contract) {
    if (contract.employeeFullName.trim().isNotEmpty) {
      return contract.employeeFullName.trim();
    }
    if (_localStaffName != null && _localStaffName!.trim().isNotEmpty) {
      return _localStaffName!.trim();
    }
    return 'Employee';
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerUpdate);
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '--';
    final d = dt.day.toString().padLeft(2, '0');
    final m = dt.month.toString().padLeft(2, '0');
    final y = dt.year.toString();
    return '$d/$m/$y';
  }

  String _formatDateTime(DateTime? dt) {
    if (dt == null) return '--';
    final local = dt.toLocal();
    final d = local.day.toString().padLeft(2, '0');
    final m = local.month.toString().padLeft(2, '0');
    final y = local.year.toString();
    final h = local.hour.toString().padLeft(2, '0');
    final min = local.minute.toString().padLeft(2, '0');
    return '$d/$m/$y $h:$min';
  }

  /// Two-Party Legal Execution Preview Modal (Exact match to web acceptModal)
  Future<void> _showAcceptContractModal(
    StaffContractDetailModel contract,
  ) async {
    bool hasAgreedTerms = false;

    final confirm = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => StatefulBuilder(
        builder: (context, setModalState) => AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(22),
          ),
          titlePadding: const EdgeInsets.fromLTRB(18, 18, 14, 8),
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 18,
            vertical: 8,
          ),
          actionsPadding: const EdgeInsets.fromLTRB(18, 4, 18, 16),
          title: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFFECFDF5),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFA7F3D0)),
                ),
                child: const Icon(
                  Icons.verified_rounded,
                  color: Color(0xFF059669),
                  size: 22,
                ),
              ),
              const SizedBox(width: 10),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Confirm & Digitally Sign',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    Text(
                      'Official legal contract execution',
                      style: TextStyle(
                        fontSize: 11,
                        color: AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                icon: const Icon(
                  Icons.close_rounded,
                  size: 20,
                  color: AppColors.textMuted,
                ),
                onPressed: () => Navigator.pop(ctx, false),
              ),
            ],
          ),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Contract Summary Card
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF8FAFC),
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: const Color(0xFFE2E8F0)),
                  ),
                  child: Column(
                    children: [
                      Row(
                        children: [
                          const Icon(
                            Icons.receipt_long_rounded,
                            size: 14,
                            color: AppColors.textMuted,
                          ),
                          const SizedBox(width: 5),
                          const Text(
                            'Contract No:',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w500,
                              color: AppColors.textMuted,
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              contract.contractNumber,
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                                color: AppColors.textPrimary,
                                fontFamily: 'monospace',
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              textAlign: TextAlign.end,
                            ),
                          ),
                        ],
                      ),
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 8),
                        child: Divider(
                          height: 1,
                          thickness: 0.6,
                          color: Color(0xFFE2E8F0),
                        ),
                      ),
                      Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Hourly Rate',
                                  style: TextStyle(
                                    fontSize: 10,
                                    color: AppColors.textMuted,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  contract.formattedHourlyRate,
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                    color: Color(0xFF047857),
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ],
                            ),
                          ),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Start Date',
                                  style: TextStyle(
                                    fontSize: 10,
                                    color: AppColors.textMuted,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  _formatDate(contract.startDate),
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Work Branch',
                                  style: TextStyle(
                                    fontSize: 10,
                                    color: AppColors.textMuted,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  contract.branchName.isNotEmpty
                                      ? contract.branchName
                                      : '--',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.textPrimary,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),

                // Two-Party Legal Execution Preview Container
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFEEF2FF).withValues(alpha: 0.6),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: const Color(0xFFC7D2FE)),
                  ),
                  child: Column(
                    children: [
                      const Text(
                        'TWO-PARTY LEGAL EXECUTION PREVIEW',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF312E81),
                          letterSpacing: 0.5,
                        ),
                      ),
                      const SizedBox(height: 8),
                      IntrinsicHeight(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            // Left: Employee Representative
                            Expanded(
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 6,
                                  vertical: 8,
                                ),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: const Color(0xFF93C5FD),
                                    style: BorderStyle.solid,
                                  ),
                                  boxShadow: [
                                    BoxShadow(
                                      color: Colors.black.withValues(
                                        alpha: 0.02,
                                      ),
                                      blurRadius: 4,
                                      offset: const Offset(0, 1),
                                    ),
                                  ],
                                ),
                                child: Column(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  crossAxisAlignment: CrossAxisAlignment.center,
                                  children: [
                                    const Column(
                                      children: [
                                        Text(
                                          'EMPLOYEE',
                                          style: TextStyle(
                                            fontSize: 9,
                                            fontWeight: FontWeight.bold,
                                            color: Color(0xFF1E3A8A),
                                          ),
                                        ),
                                        Text(
                                          '(Digital Signature)',
                                          style: TextStyle(
                                            fontSize: 8.5,
                                            color: AppColors.textMuted,
                                            fontStyle: FontStyle.italic,
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 6),
                                    FittedBox(
                                      fit: BoxFit.scaleDown,
                                      child: Container(
                                        padding: const EdgeInsets.symmetric(
                                          horizontal: 6,
                                          vertical: 2.5,
                                        ),
                                        decoration: BoxDecoration(
                                          color: const Color(0xFFEFF6FF),
                                          borderRadius: BorderRadius.circular(
                                            10,
                                          ),
                                          border: Border.all(
                                            color: const Color(0xFFBFDBFE),
                                          ),
                                        ),
                                        child: const Row(
                                          mainAxisSize: MainAxisSize.min,
                                          children: [
                                            Icon(
                                              Icons.edit_document,
                                              size: 11,
                                              color: Color(0xFF2563EB),
                                            ),
                                            SizedBox(width: 4),
                                            Text(
                                              'Ready to Sign',
                                              style: TextStyle(
                                                fontSize: 9.5,
                                                fontWeight: FontWeight.bold,
                                                color: Color(0xFF2563EB),
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                    ),
                                    const SizedBox(height: 6),
                                    Text(
                                      _getEmployeeDisplayName(contract),
                                      style: const TextStyle(
                                        fontSize: 10,
                                        color: AppColors.textPrimary,
                                        fontWeight: FontWeight.bold,
                                      ),
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      textAlign: TextAlign.center,
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),

                            // Right: WorkShift Company Stamp
                            Expanded(
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 6,
                                  vertical: 8,
                                ),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: const Color(0xFFFECACA),
                                    style: BorderStyle.solid,
                                  ),
                                  boxShadow: [
                                    BoxShadow(
                                      color: Colors.black.withValues(
                                        alpha: 0.02,
                                      ),
                                      blurRadius: 4,
                                      offset: const Offset(0, 1),
                                    ),
                                  ],
                                ),
                                child: Column(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  crossAxisAlignment: CrossAxisAlignment.center,
                                  children: [
                                    const Column(
                                      children: [
                                        Text(
                                          'WORKSHIFT CO.',
                                          style: TextStyle(
                                            fontSize: 9,
                                            fontWeight: FontWeight.bold,
                                            color: Color(0xFF991B1B),
                                          ),
                                        ),
                                        Text(
                                          '(Certified Seal)',
                                          style: TextStyle(
                                            fontSize: 8.5,
                                            color: AppColors.textMuted,
                                            fontStyle: FontStyle.italic,
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 4),
                                    Image.asset(
                                      'assets/images/workshift_stamp.png',
                                      height: 34,
                                      fit: BoxFit.contain,
                                      errorBuilder:
                                          (context, error, stackTrace) =>
                                              Container(
                                                padding:
                                                    const EdgeInsets.symmetric(
                                                      horizontal: 6,
                                                      vertical: 2,
                                                    ),
                                                decoration: BoxDecoration(
                                                  border: Border.all(
                                                    color: Colors.red,
                                                  ),
                                                  borderRadius:
                                                      BorderRadius.circular(4),
                                                ),
                                                child: const Text(
                                                  'WORKSHIFT SEAL',
                                                  style: TextStyle(
                                                    color: Colors.red,
                                                    fontSize: 8,
                                                    fontWeight: FontWeight.bold,
                                                  ),
                                                ),
                                              ),
                                    ),
                                    const SizedBox(height: 4),
                                    const FittedBox(
                                      fit: BoxFit.scaleDown,
                                      child: Text(
                                        '✓ VERIFIED BY SYSTEM',
                                        style: TextStyle(
                                          fontSize: 8.5,
                                          fontWeight: FontWeight.bold,
                                          color: Color(0xFFDC2626),
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),

                // Terms acknowledgment checkbox
                InkWell(
                  onTap: () {
                    setModalState(() {
                      hasAgreedTerms = !hasAgreedTerms;
                    });
                  },
                  borderRadius: BorderRadius.circular(8),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        SizedBox(
                          width: 22,
                          height: 22,
                          child: Checkbox(
                            value: hasAgreedTerms,
                            activeColor: const Color(0xFF059669),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(4),
                            ),
                            materialTapTargetSize:
                                MaterialTapTargetSize.shrinkWrap,
                            visualDensity: VisualDensity.compact,
                            onChanged: (val) {
                              setModalState(() {
                                hasAgreedTerms = val ?? false;
                              });
                            },
                          ),
                        ),
                        const SizedBox(width: 8),
                        const Expanded(
                          child: Text(
                            'I have carefully read all terms of this employment contract, understand my rights and obligations, and agree to digitally sign this contract.',
                            style: TextStyle(
                              fontSize: 11,
                              color: AppColors.textSecondary,
                              height: 1.35,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          actions: [
            Row(
              children: [
                Expanded(
                  flex: 2,
                  child: OutlinedButton(
                    onPressed: () => Navigator.pop(ctx, false),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.textSecondary,
                      side: const BorderSide(color: Color(0xFFCBD5E1)),
                      padding: const EdgeInsets.symmetric(vertical: 10),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    child: const FittedBox(
                      fit: BoxFit.scaleDown,
                      child: Text(
                        'Review Terms',
                        style: TextStyle(
                          fontSize: 12.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  flex: 3,
                  child: ElevatedButton.icon(
                    onPressed:
                        hasAgreedTerms ? () => Navigator.pop(ctx, true) : null,
                    icon: const Icon(Icons.draw_rounded, size: 16),
                    label: const FittedBox(
                      fit: BoxFit.scaleDown,
                      child: Text(
                        'Confirm & Sign',
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF059669),
                      foregroundColor: Colors.white,
                      disabledBackgroundColor: const Color(0xFFE2E8F0),
                      disabledForegroundColor: const Color(0xFF94A3B8),
                      elevation: hasAgreedTerms ? 1 : 0,
                      padding: const EdgeInsets.symmetric(vertical: 10),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );

    if (confirm == true && mounted) {
      final success = await widget.controller.acceptContract(
        contract.contractId,
      );
      if (mounted) {
        if (success) {
          await widget.controller.loadContractDetail(widget.contractId);
          if (!mounted) return;
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text(
                'Contract confirmed & digitally signed successfully!',
              ),
              backgroundColor: AppColors.success,
            ),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                widget.controller.actionErrorMessage ??
                    'Failed to accept contract',
              ),
              backgroundColor: AppColors.error,
            ),
          );
        }
      }
    }
  }

  /// Download PDF and save/trigger browser download
  Future<void> _handleDownloadPdf(StaffContractDetailModel contract) async {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Downloading contract PDF document...')),
    );
    final bytes = await widget.controller.fetchPdfBytes(
      contract.contractId,
      isDownload: true,
    );
    if (mounted) {
      if (bytes != null && bytes.isNotEmpty) {
        downloadPdfBytes(bytes, '${contract.contractNumber}.pdf');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              'Downloaded contract ${contract.contractNumber}.pdf successfully (${bytes.lengthInBytes} bytes)',
            ),
            backgroundColor: AppColors.success,
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              widget.controller.actionErrorMessage ??
                  'Unable to download contract PDF',
            ),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  /// Open PDF directly in a new browser tab
  Future<void> _handleOpenPdfInBrowser(
    StaffContractDetailModel contract,
  ) async {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Opening contract PDF document...')),
    );
    final bytes = await widget.controller.fetchPdfBytes(contract.contractId);
    if (mounted) {
      if (bytes != null && bytes.isNotEmpty) {
        previewPdfInBrowser(bytes);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              widget.controller.actionErrorMessage ??
                  'Unable to open contract PDF',
            ),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  /// Full Document Viewer Modal
  void _showPdfPreviewDialog(StaffContractDetailModel contract) {
    showDialog(
      context: context,
      builder: (ctx) => Dialog(
        insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 620),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Header
              Container(
                padding: const EdgeInsets.fromLTRB(20, 16, 16, 12),
                decoration: const BoxDecoration(
                  border: Border(bottom: BorderSide(color: Color(0xFFE2E8F0))),
                ),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(6),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFEE2E2),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: const Icon(
                        Icons.picture_as_pdf_rounded,
                        color: AppColors.error,
                        size: 20,
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            contract.contractNumber,
                            style: const TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const Text(
                            'Official Contract Document Preview',
                            style: TextStyle(
                              fontSize: 11,
                              color: AppColors.textMuted,
                            ),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      icon: const Icon(
                        Icons.close_rounded,
                        size: 20,
                        color: AppColors.textMuted,
                      ),
                      onPressed: () => Navigator.pop(ctx),
                    ),
                  ],
                ),
              ),

              // Scrollable Document Body
              Flexible(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: _buildContractDocumentPaper(contract),
                ),
              ),

              // Bottom Actions Bar
              Container(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
                decoration: const BoxDecoration(
                  border: Border(top: BorderSide(color: Color(0xFFE2E8F0))),
                  color: Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.vertical(
                    bottom: Radius.circular(20),
                  ),
                ),
                child: Wrap(
                  alignment: WrapAlignment.end,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    TextButton(
                      onPressed: () => Navigator.pop(ctx),
                      child: const Text(
                        'Close',
                        style: TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 13,
                        ),
                      ),
                    ),
                    if (kIsWeb)
                      OutlinedButton.icon(
                        onPressed: () {
                          Navigator.pop(ctx);
                          _handleOpenPdfInBrowser(contract);
                        },
                        icon: const Icon(Icons.open_in_new_rounded, size: 16),
                        label: const Text('Open in Browser Tab'),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: const Color(0xFF4F46E5),
                          side: const BorderSide(color: Color(0xFFC7D2FE)),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 12,
                            vertical: 8,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                      ),
                    ElevatedButton.icon(
                      onPressed: () {
                        Navigator.pop(ctx);
                        _handleDownloadPdf(contract);
                      },
                      icon: const Icon(Icons.download_rounded, size: 16),
                      label: const Text('Download PDF'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 8,
                        ),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final contract = widget.controller.selectedContract;
    final isLoading = widget.controller.isLoadingDetail;
    final isAccepting = widget.controller.isAccepting;
    final changeRequests = widget.controller.changeRequests;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          contract != null
              ? 'Contract ${contract.contractNumber}'
              : 'Contract Details',
          style: const TextStyle(
            fontSize: 17,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(
            Icons.arrow_back_ios_new_rounded,
            color: AppColors.textPrimary,
            size: 20,
          ),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          if (contract != null && contract.hasPdf) ...[
            if (kIsWeb)
              IconButton(
                tooltip: 'Open PDF in Tab',
                icon: const Icon(
                  Icons.open_in_new_rounded,
                  color: Color(0xFF4F46E5),
                ),
                onPressed: () => _handleOpenPdfInBrowser(contract),
              ),
            IconButton(
              tooltip: 'Download PDF',
              icon: const Icon(
                Icons.download_rounded,
                color: AppColors.primary,
              ),
              onPressed: () => _handleDownloadPdf(contract),
            ),
          ],
        ],
      ),
      body: isLoading
          ? const Center(
              child: CircularProgressIndicator(
                color: AppColors.primary,
                strokeWidth: 2.5,
              ),
            )
          : contract == null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      size: 48,
                      color: AppColors.error,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      widget.controller.actionErrorMessage ??
                          'Contract data not found.',
                      style: const TextStyle(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: 16),
                    AppButton(
                      text: 'Retry',
                      onPressed: () => widget.controller.loadContractDetail(
                        widget.contractId,
                      ),
                      width: 140,
                      height: 40,
                    ),
                  ],
                ),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // 1. Dynamic Status Banners (exact match to web Details.cshtml)
                  _buildDynamicStatusBanner(contract),
                  const SizedBox(height: 12),

                  // 2. Shift Eligibility Policy Banner
                  _buildShiftEligibilityBanner(contract),
                  const SizedBox(height: 16),

                  // 3. Card 1: Position & Hourly Rate
                  _buildPositionSalaryCard(contract),
                  const SizedBox(height: 14),

                  // 4. Card 2: Work Location
                  _buildLocationCard(contract),
                  const SizedBox(height: 14),

                  // 5. Card 3: Period & Lifecycle
                  _buildPeriodLifecycleCard(contract),
                  const SizedBox(height: 16),

                  // 6. Contract Terms
                  _buildContractTermsSection(contract),
                  const SizedBox(height: 16),

                  // 7. Full Document PDF & Digital Signature (With Embedded Visual Document!)
                  _buildPdfAndSignatureSection(contract, isAccepting),
                  const SizedBox(height: 20),

                  // 8. Contract Change Requests History
                  if (contract.isPendingStaff || changeRequests.isNotEmpty)
                    _buildChangeRequestsSection(contract, changeRequests),
                  if (contract.isPendingStaff || changeRequests.isNotEmpty)
                    const SizedBox(height: 24),
                ],
              ),
            ),
    );
  }

  /// Dynamic Status Banner (Matches Details.cshtml lines 49-159)
  Widget _buildDynamicStatusBanner(StaffContractDetailModel contract) {
    if (contract.isPendingStaff) {
      if (contract.canAccept) {
        return Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: const Color(0xFFFFFBEB),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: const Color(0xFFFDE68A)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.pending_actions_rounded,
                    color: Color(0xFFD97706),
                    size: 22,
                  ),
                  SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Contract Pending Your Acceptance',
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF92400E),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 4),
              const Text(
                'Your branch manager has issued this contract. Please carefully review the hourly rate, effective start date, and terms before confirming acceptance.',
                style: TextStyle(
                  fontSize: 12,
                  color: Color(0xFFB45309),
                  height: 1.35,
                ),
              ),
              const SizedBox(height: 10),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: () => _showAcceptContractModal(contract),
                  icon: const Icon(Icons.draw_rounded, size: 16),
                  label: const Text('Accept Contract Now'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFFD97706),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 10),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      } else {
        return Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: const Color(0xFFFFF1F2),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: const Color(0xFFFECDD3)),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(
                Icons.warning_amber_rounded,
                color: Color(0xFFE11D48),
                size: 22,
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Contract Cannot Be Accepted',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF881337),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      contract.cannotAcceptReason ??
                          'The acceptance period has expired (effective start date has passed).',
                      style: const TextStyle(
                        fontSize: 12,
                        color: Color(0xFF9F1239),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      }
    } else if (contract.status == 'CANCELLED') {
      final isAuto =
          contract.cancellationReason != null &&
          (contract.cancellationReason!.contains(
                'NOT_ACCEPTED_BY_START_DATE',
              ) ||
              contract.cancellationReason!.toLowerCase().contains(
                'quá ngày bắt đầu',
              ) ||
              contract.cancellationReason!.toLowerCase().contains(
                'auto-cancelled',
              ) ||
              contract.cancellationReason!.toLowerCase().contains(
                'tự động hủy',
              ));
      return Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: const Color(0xFFFFF1F2),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFFECDD3)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(
                  Icons.cancel_rounded,
                  color: Color(0xFFE11D48),
                  size: 20,
                ),
                const SizedBox(width: 8),
                const Expanded(
                  child: Text(
                    'Contract Cancelled (CANCELLED)',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF881337),
                    ),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFECDD3),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    isAuto
                        ? 'Auto-cancelled by System'
                        : 'Cancelled by Manager',
                    style: const TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF9F1239),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Text(
              'Cancellation Reason: ${isAuto ? "Past start date without employee acceptance." : (contract.cancellationReason ?? "No detailed reason provided.")}',
              style: const TextStyle(fontSize: 12, color: Color(0xFF9F1239)),
            ),
            if (contract.cancelledAt != null)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  'Cancelled at: ${_formatDateTime(contract.cancelledAt)}',
                  style: const TextStyle(
                    fontSize: 11,
                    color: Color(0xFFBE123C),
                  ),
                ),
              ),
            if (isAuto)
              const Padding(
                padding: EdgeInsets.only(top: 6),
                child: Text(
                  '* Note: This contract was automatically cancelled because it was not accepted prior to the effective start date.',
                  style: TextStyle(
                    fontSize: 11,
                    fontStyle: FontStyle.italic,
                    color: Color(0xFF9F1239),
                  ),
                ),
              ),
          ],
        ),
      );
    } else if (contract.status == 'TERMINATED') {
      return Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: const Color(0xFFF1F5F9),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFCBD5E1)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.gavel_rounded, color: Color(0xFF475569), size: 20),
                SizedBox(width: 8),
                Text(
                  'Contract Terminated (TERMINATED)',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF0F172A),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Text(
              'Termination Reason: ${contract.terminationReason ?? "No detailed reason provided."}',
              style: const TextStyle(fontSize: 12, color: Color(0xFF334155)),
            ),
            if (contract.terminationEffectiveAt != null)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  'Termination effective date: ${_formatDateTime(contract.terminationEffectiveAt)}',
                  style: const TextStyle(
                    fontSize: 11,
                    color: Color(0xFF64748B),
                  ),
                ),
              ),
          ],
        ),
      );
    } else if (contract.isScheduled) {
      return Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: const Color(0xFFEFF6FF),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFBFDBFE)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Icon(
              Icons.event_available_rounded,
              color: Color(0xFF2563EB),
              size: 22,
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Contract Accepted (SCHEDULED)',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF1E3A8A),
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'You accepted this contract on ${_formatDateTime(contract.staffAcceptedAt)}. The contract will automatically transition to ACTIVE on the start date: ${_formatDate(contract.startDate)}.',
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFF1E40AF),
                      height: 1.35,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      );
    } else if (contract.isActive) {
      return Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: const Color(0xFFECFDF5),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: const Color(0xFFA7F3D0)),
        ),
        child: const Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.verified_rounded, color: Color(0xFF059669), size: 22),
            SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Contract Active (ACTIVE)',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF064E3B),
                    ),
                  ),
                  SizedBox(height: 2),
                  Text(
                    'Your employment record is active and legally recognized. You are eligible to receive shift assignments and work at the branch.',
                    style: TextStyle(
                      fontSize: 12,
                      color: Color(0xFF047857),
                      height: 1.35,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      );
    }
    return const SizedBox.shrink();
  }

  /// Shift Eligibility Banner (Matches Details.cshtml lines 162-174)
  Widget _buildShiftEligibilityBanner(StaffContractDetailModel contract) {
    final isEligible = contract.isActive;
    final isScheduled = contract.isScheduled;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Row(
        children: [
          const Icon(
            Icons.calendar_month_outlined,
            size: 18,
            color: AppColors.textMuted,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              isEligible || isScheduled
                  ? 'Shift Eligibility: You are eligible to be assigned shifts within this contract period.'
                  : 'Shift Eligibility: Contract is not active yet. You cannot be assigned shifts at this branch.',
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            isEligible
                ? 'Eligible for Shifts'
                : isScheduled
                ? 'Scheduled (Pending Start)'
                : 'Not Eligible',
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              color: isEligible
                  ? const Color(0xFF047857)
                  : isScheduled
                  ? const Color(0xFF1D4ED8)
                  : const Color(0xFFB45309),
            ),
          ),
        ],
      ),
    );
  }

  /// Card 1: Position & Hourly Rate (Matches web Card 1)
  Widget _buildPositionSalaryCard(StaffContractDetailModel contract) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderLight),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.02),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.payments_rounded, size: 18, color: Color(0xFF059669)),
              SizedBox(width: 8),
              Text(
                'POSITION & HOURLY RATE',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textMuted,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Position / Title:',
            style: TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          const SizedBox(height: 2),
          Text(
            contract.employeePosition,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'Hourly Wage:',
            style: TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          const SizedBox(height: 2),
          Text(
            contract.formattedHourlyRate,
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w900,
              color: Color(0xFF047857),
            ),
          ),
        ],
      ),
    );
  }

  /// Card 2: Work Location (Matches web Card 2)
  Widget _buildLocationCard(StaffContractDetailModel contract) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderLight),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.02),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(
                Icons.storefront_rounded,
                size: 18,
                color: Color(0xFF2563EB),
              ),
              SizedBox(width: 8),
              Text(
                'WORK LOCATION',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textMuted,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          const Text(
            'Branch:',
            style: TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          const SizedBox(height: 2),
          Text(
            contract.branchName,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
            ),
          ),
          if (contract.branchAddress.isNotEmpty) ...[
            const SizedBox(height: 8),
            const Text(
              'Address:',
              style: TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
            const SizedBox(height: 2),
            Text(
              contract.branchAddress,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
                height: 1.3,
              ),
            ),
          ],
          const SizedBox(height: 8),
          const Text(
            'Region:',
            style: TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          const SizedBox(height: 2),
          Text(
            contract.regionName,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
          ),
        ],
      ),
    );
  }

  /// Card 3: Period & Lifecycle (Matches web Card 3)
  Widget _buildPeriodLifecycleCard(StaffContractDetailModel contract) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderLight),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.02),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(
                Icons.date_range_rounded,
                size: 18,
                color: Color(0xFF4F46E5),
              ),
              SizedBox(width: 8),
              Text(
                'PERIOD & LIFECYCLE',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textMuted,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Start Date:',
                      style: TextStyle(
                        fontSize: 12,
                        color: AppColors.textMuted,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      _formatDate(contract.startDate),
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'End Date:',
                      style: TextStyle(
                        fontSize: 12,
                        color: AppColors.textMuted,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      contract.endDate != null
                          ? _formatDate(contract.endDate)
                          : 'Indefinite-term',
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const Divider(height: 20, color: AppColors.borderLight),
          if (contract.sentToStaffAt != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Text(
                'Sent to you: ${_formatDateTime(contract.sentToStaffAt)}',
                style: const TextStyle(
                  fontSize: 11,
                  color: AppColors.textMuted,
                ),
              ),
            ),
          if (contract.staffAcceptedAt != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Text(
                'Accepted by you: ${_formatDateTime(contract.staffAcceptedAt)}',
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF047857),
                ),
              ),
            ),
          if (contract.createdByManagerName != null)
            Text(
              'Created by manager: ${contract.createdByManagerName}',
              style: const TextStyle(fontSize: 11, color: AppColors.textMuted),
            ),
        ],
      ),
    );
  }

  /// Contract Terms Section (Matches web lines 300-310)
  Widget _buildContractTermsSection(StaffContractDetailModel contract) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Icon(Icons.gavel_rounded, size: 16, color: AppColors.textMuted),
            SizedBox(width: 6),
            Text(
              'CONTRACT TERMS',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: AppColors.textMuted,
                letterSpacing: 0.5,
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: const Color(0xFFF8FAFC),
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: AppColors.borderLight),
          ),
          child: Text(
            contract.contractTerms != null && contract.contractTerms!.isNotEmpty
                ? contract.contractTerms!
                : 'Rights, obligations, and responsibilities are carried out in compliance with current workplace regulations and the Vietnam Labor Code.',
            style: const TextStyle(
              fontSize: 12.5,
              color: AppColors.textSecondary,
              height: 1.5,
              fontFamily: 'monospace',
            ),
          ),
        ),
      ],
    );
  }

  /// Visual Paper Contract Document (Matches QuestPDF ContractPdfGenerator layout)
  Widget _buildContractDocumentPaper(StaffContractDetailModel contract) {
    final isSigned =
        contract.staffAcceptedAt != null || contract.status == 'ACTIVE';

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFCBD5E1)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 15,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // National Motto Header
          const Text(
            'SOCIALIST REPUBLIC OF VIETNAM',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.bold,
              color: Color(0xFF1E293B),
              letterSpacing: 0.5,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 2),
          const Text(
            'Independence - Freedom - Happiness',
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              fontStyle: FontStyle.italic,
              color: Color(0xFF334155),
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 6),
          Container(width: 140, height: 1.5, color: const Color(0xFF94A3B8)),
          const SizedBox(height: 16),

          // Document Title
          const Text(
            'EMPLOYMENT CONTRACT',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w900,
              color: Color(0xFF1E3A8A),
              letterSpacing: 0.5,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 3),
          Text(
            'Contract No.: ${contract.contractNumber}',
            style: const TextStyle(fontSize: 11, color: AppColors.textMuted),
          ),
          const SizedBox(height: 16),

          // Section 1: Employer
          _buildLegalDocSection('1. EMPLOYER INFORMATION', [
            _buildDocRow('Branch:', contract.branchName),
            if (contract.branchAddress.isNotEmpty)
              _buildDocRow('Address:', contract.branchAddress),
            _buildDocRow('Applicable Region:', contract.regionName),
          ]),
          const SizedBox(height: 12),

          // Section 2: Employee
          _buildLegalDocSection('2. EMPLOYEE INFORMATION', [
            _buildDocRow(
              'Full Name:',
              _getEmployeeDisplayName(contract),
              isBoldValue: true,
            ),
            _buildDocRow('Position / Title:', contract.employeePosition),
            _buildDocRow('Work Location:', contract.branchName),
          ]),
          const SizedBox(height: 12),

          // Section 3: Terms & Wage
          _buildLegalDocSection('3. CONTRACT CONTENT & TERMS', [
            _buildDocRow('Start Date:', _formatDate(contract.startDate)),
            _buildDocRow(
              'End Date:',
              contract.endDate != null
                  ? _formatDate(contract.endDate)
                  : 'Indefinite',
            ),
            _buildDocRow(
              'Hourly Wage:',
              contract.formattedHourlyRate,
              isBoldValue: true,
              valueColor: const Color(0xFF047857),
            ),
          ]),
          if (contract.contractTerms != null &&
              contract.contractTerms!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Text(
                contract.contractTerms!,
                style: const TextStyle(
                  fontSize: 11,
                  color: Color(0xFF334155),
                  height: 1.4,
                ),
              ),
            ),
          ],
          const SizedBox(height: 14),

          // Acknowledgment Note
          const Text(
            'ACKNOWLEDGMENT OF THE PARTIES\nThis contract is generated and electronically authenticated on the WorkShift system. Accepting this contract carries the same legal validity as signing a physical document under applicable law.',
            style: TextStyle(
              fontSize: 9.5,
              fontStyle: FontStyle.italic,
              color: Color(0xFF64748B),
              height: 1.35,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 16),

          // 2-Column Signatures & Red Certified Stamp Box (Exact match to QuestPDF!)
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: const Color(0xFFFAFAFA),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: const Color(0xFFE2E8F0)),
            ),
            child: IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Left: Employee Representative
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        const FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            'EMPLOYEE REPRESENTATIVE',
                            style: TextStyle(
                              fontSize: 10,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF1E3A8A),
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                        const FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            '(Signature and Full Name)',
                            style: TextStyle(
                              fontSize: 8.5,
                              fontStyle: FontStyle.italic,
                              color: AppColors.textMuted,
                            ),
                          ),
                        ),
                        const SizedBox(height: 8),
                        Expanded(
                          child: Container(
                            width: double.infinity,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 6,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: isSigned
                                  ? const Color(0xFFEFF6FF)
                                  : Colors.white,
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: isSigned
                                    ? const Color(0xFF2563EB)
                                    : const Color(0xFFCBD5E1),
                                width: isSigned ? 1.5 : 1,
                              ),
                            ),
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                if (isSigned) ...[
                                  const FittedBox(
                                    fit: BoxFit.scaleDown,
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Icon(
                                          Icons.verified_rounded,
                                          size: 13,
                                          color: Color(0xFF2563EB),
                                        ),
                                        SizedBox(width: 4),
                                        Text(
                                          'DIGITALLY SIGNED',
                                          style: TextStyle(
                                            fontSize: 10,
                                            fontWeight: FontWeight.bold,
                                            color: Color(0xFF1D4ED8),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  FittedBox(
                                    fit: BoxFit.scaleDown,
                                    child: Text(
                                      _getEmployeeDisplayName(contract),
                                      style: const TextStyle(
                                        fontSize: 11,
                                        fontWeight: FontWeight.bold,
                                        color: Color(0xFF1E3A8A),
                                      ),
                                      textAlign: TextAlign.center,
                                    ),
                                  ),
                                  if (contract.staffAcceptedAt != null)
                                    Padding(
                                      padding: const EdgeInsets.only(top: 3),
                                      child: FittedBox(
                                        fit: BoxFit.scaleDown,
                                        child: Text(
                                          'Signed at: ${_formatDateTime(contract.staffAcceptedAt)}',
                                          style: const TextStyle(
                                            fontSize: 8.5,
                                            color: Color(0xFF475569),
                                          ),
                                          textAlign: TextAlign.center,
                                        ),
                                      ),
                                    ),
                                  const SizedBox(height: 3),
                                  const FittedBox(
                                    fit: BoxFit.scaleDown,
                                    child: Text(
                                      'Verified securely via Staff account',
                                      style: TextStyle(
                                        fontSize: 8,
                                        fontStyle: FontStyle.italic,
                                        color: Color(0xFF2563EB),
                                      ),
                                      textAlign: TextAlign.center,
                                    ),
                                  ),
                                ] else ...[
                                  const SizedBox(height: 6),
                                  const FittedBox(
                                    fit: BoxFit.scaleDown,
                                    child: Text(
                                      '(Pending employee electronic acceptance)',
                                      style: TextStyle(
                                        fontSize: 9,
                                        fontStyle: FontStyle.italic,
                                        color: AppColors.textMuted,
                                      ),
                                      textAlign: TextAlign.center,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  FittedBox(
                                    fit: BoxFit.scaleDown,
                                    child: Text(
                                      _getEmployeeDisplayName(contract),
                                      style: const TextStyle(
                                        fontSize: 10,
                                        fontWeight: FontWeight.w600,
                                        color: Color(0xFF475569),
                                      ),
                                      textAlign: TextAlign.center,
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  if (contract.canAccept)
                                    SizedBox(
                                      height: 28,
                                      child: ElevatedButton(
                                        onPressed: () =>
                                            _showAcceptContractModal(contract),
                                        style: ElevatedButton.styleFrom(
                                          backgroundColor: const Color(0xFF2563EB),
                                          foregroundColor: Colors.white,
                                          padding: const EdgeInsets.symmetric(
                                            horizontal: 10,
                                          ),
                                          shape: RoundedRectangleBorder(
                                            borderRadius: BorderRadius.circular(6),
                                          ),
                                        ),
                                        child: const Text(
                                          'Sign Now',
                                          style: TextStyle(
                                            fontSize: 10,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                    ),
                                ],
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 10),

                  // Right: WorkShift Company with Certified Red Seal
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        const FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            'WORKSHIFT COMPANY',
                            style: TextStyle(
                              fontSize: 10,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF991B1B),
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                        const FittedBox(
                          fit: BoxFit.scaleDown,
                          child: Text(
                            '(Authorized Signature & Official Seal)',
                            style: TextStyle(
                              fontSize: 8.5,
                              fontStyle: FontStyle.italic,
                              color: AppColors.textMuted,
                            ),
                          ),
                        ),
                        const SizedBox(height: 8),
                        Expanded(
                          child: Container(
                            width: double.infinity,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 6,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFEF2F2),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: const Color(0xFFDC2626),
                                width: 1.5,
                              ),
                            ),
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                // Official WorkShift Stamp Image
                                Image.asset(
                                  'assets/images/workshift_stamp.png',
                                  height: 48,
                                  fit: BoxFit.contain,
                                  errorBuilder: (context, error, stackTrace) =>
                                      Container(
                                        padding: const EdgeInsets.all(6),
                                        decoration: BoxDecoration(
                                          border: Border.all(
                                            color: Colors.red,
                                            width: 2,
                                          ),
                                          shape: BoxShape.circle,
                                        ),
                                        child: const Text(
                                          'WorkShift\nSeal',
                                          style: TextStyle(
                                            color: Colors.red,
                                            fontSize: 8,
                                            fontWeight: FontWeight.bold,
                                          ),
                                          textAlign: TextAlign.center,
                                        ),
                                      ),
                                ),
                                const SizedBox(height: 4),
                                const FittedBox(
                                  fit: BoxFit.scaleDown,
                                  child: Text(
                                    '✓ VERIFIED BY SYSTEM',
                                    style: TextStyle(
                                      fontSize: 9,
                                      fontWeight: FontWeight.bold,
                                      color: Color(0xFFB91C1C),
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                FittedBox(
                                  fit: BoxFit.scaleDown,
                                  child: Text(
                                    'Branch: ${contract.branchName}',
                                    style: const TextStyle(
                                      fontSize: 8.5,
                                      color: Color(0xFF475569),
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                ),
                                FittedBox(
                                  fit: BoxFit.scaleDown,
                                  child: Text(
                                    'Contract No.: ${contract.contractNumber}',
                                    style: const TextStyle(
                                      fontSize: 7.5,
                                      color: Color(0xFF64748B),
                                    ),
                                    textAlign: TextAlign.center,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLegalDocSection(String title, List<Widget> rows) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.bold,
            color: Color(0xFF1E3A8A),
          ),
        ),
        const SizedBox(height: 4),
        Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          decoration: BoxDecoration(
            color: const Color(0xFFF8FAFC),
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: const Color(0xFFE2E8F0)),
          ),
          child: Column(children: rows),
        ),
      ],
    );
  }

  Widget _buildDocRow(
    String label,
    String value, {
    bool isBoldValue = false,
    Color? valueColor,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2.5),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 130,
            child: Text(
              label,
              style: const TextStyle(fontSize: 10.5, color: Color(0xFF64748B)),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontSize: 11,
                fontWeight: isBoldValue ? FontWeight.bold : FontWeight.w600,
                color: valueColor ?? const Color(0xFF0F172A),
              ),
            ),
          ),
        ],
      ),
    );
  }

  /// PDF & Digital Signature Section with Embedded Visual Document
  Widget _buildPdfAndSignatureSection(
    StaffContractDetailModel contract,
    bool isAccepting,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Expanded(
              child: Row(
                children: [
                  Icon(
                    Icons.picture_as_pdf_rounded,
                    size: 16,
                    color: Color(0xFFE11D48),
                  ),
                  SizedBox(width: 6),
                  Flexible(
                    child: Text(
                      'FULL CONTRACT DOCUMENT & SIGNATURE',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textMuted,
                        letterSpacing: 0.5,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ),
            if (contract.hasPdf) ...[
              const SizedBox(width: 8),
              TextButton.icon(
                onPressed: () => _showPdfPreviewDialog(contract),
                icon: const Icon(Icons.fullscreen_rounded, size: 16),
                label: const Text('Full View'),
                style: TextButton.styleFrom(
                  foregroundColor: const Color(0xFF4F46E5),
                  padding: const EdgeInsets.symmetric(horizontal: 8),
                ),
              ),
            ],
          ],
        ),
        const SizedBox(height: 8),

        // PDF Actions Bar Card
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: AppColors.borderLight),
          ),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFFFEE2E2),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.picture_as_pdf_rounded,
                  color: AppColors.error,
                  size: 22,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${contract.contractNumber}.pdf',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 1),
                    const Text(
                      'Certified electronic document',
                      style: TextStyle(
                        fontSize: 11,
                        color: AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
              if (contract.hasPdf) ...[
                if (kIsWeb)
                  IconButton(
                    tooltip: 'Open in Browser Tab',
                    icon: const Icon(
                      Icons.open_in_new_rounded,
                      size: 18,
                      color: Color(0xFF4F46E5),
                    ),
                    onPressed: () => _handleOpenPdfInBrowser(contract),
                  ),
                IconButton(
                  tooltip: 'Download PDF',
                  icon: const Icon(
                    Icons.download_rounded,
                    size: 20,
                    color: AppColors.primary,
                  ),
                  onPressed: () => _handleDownloadPdf(contract),
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: 12),

        // Embedded Visual Contract Document Page
        _buildContractDocumentPaper(contract),

        // Callout Banner if CanAccept
        if (contract.canAccept) ...[
          const SizedBox(height: 14),
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [
                  Color(0xFFECFDF5),
                  Color(0xFFF0FDFA),
                  Color(0xFFECFDF5),
                ],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: const Color(0xFFA7F3D0)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Text('✍️', style: TextStyle(fontSize: 20)),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Review terms & confirm digital signature',
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF064E3B),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                const Text(
                  'Upon signing, your digital signature will be officially recorded with the certified WorkShift seal.',
                  style: TextStyle(
                    fontSize: 12,
                    color: Color(0xFF047857),
                    height: 1.3,
                  ),
                ),
                const SizedBox(height: 12),
                AppButton(
                  text: 'Confirm & Sign Contract Now',
                  icon: Icons.verified_rounded,
                  isLoading: isAccepting,
                  onPressed: isAccepting
                      ? null
                      : () => _showAcceptContractModal(contract),
                  width: double.infinity,
                  height: 48,
                ),
              ],
            ),
          ),
        ],
      ],
    );
  }

  /// Change Requests Section (Matches web lines 351-467)
  Widget _buildChangeRequestsSection(
    StaffContractDetailModel contract,
    List<StaffContractChangeRequestModel> changeRequests,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        Icons.edit_note_rounded,
                        size: 18,
                        color: Color(0xFF4F46E5),
                      ),
                      SizedBox(width: 6),
                      Flexible(
                        child: Text(
                          'CONTRACT CHANGE REQUESTS',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textMuted,
                            letterSpacing: 0.5,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 2),
                  Text(
                    'Propose adjustments directly to your manager.',
                    style: TextStyle(fontSize: 11, color: AppColors.textMuted),
                  ),
                ],
              ),
            ),
            if (contract.isPendingStaff && contract.canSubmitChangeRequest) ...[
              const SizedBox(width: 8),
              OutlinedButton.icon(
                onPressed: () async {
                  final res = await Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => SubmitContractChangeRequestView(
                        contract: contract,
                        controller: widget.controller,
                      ),
                    ),
                  );
                  if (res == true) {
                    widget.controller.loadContractDetail(widget.contractId);
                  }
                },
                icon: const Icon(Icons.add_rounded, size: 16),
                label: const Text('Change Request'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: const Color(0xFF4F46E5),
                  side: const BorderSide(color: Color(0xFFC7D2FE)),
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
              ),
            ],
          ],
        ),
        const SizedBox(height: 10),

        if (changeRequests.isEmpty)
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.borderLight),
            ),
            child: const Center(
              child: Text(
                'No change requests have been submitted for this contract.',
                style: TextStyle(fontSize: 12, color: AppColors.textMuted),
              ),
            ),
          )
        else
          ...changeRequests.map((cr) => _buildChangeRequestCard(cr)),
      ],
    );
  }

  Widget _buildChangeRequestCard(StaffContractChangeRequestModel cr) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Text(
                  'Request #${cr.changeRequestId}',
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: 8),
              StatusBadge.forChangeRequestStatus(cr.status),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            'Submitted: ${_formatDateTime(cr.submittedAt ?? cr.createdAt)}',
            style: const TextStyle(fontSize: 11, color: AppColors.textMuted),
          ),
          if (cr.staffComment != null && cr.staffComment!.isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(
              'Staff Note: ${cr.staffComment}',
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
          if (cr.items.isNotEmpty) ...[
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Column(
                children: cr.items.map((item) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 3),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          '• ',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              RichText(
                                text: TextSpan(
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: AppColors.textPrimary,
                                  ),
                                  children: [
                                    TextSpan(
                                      text: '${item.fieldDisplayName}: ',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                    if (item.originalValue != null &&
                                        item.originalValue!.isNotEmpty)
                                      TextSpan(
                                        text: '${item.originalValue} ',
                                        style: const TextStyle(
                                          decoration:
                                              TextDecoration.lineThrough,
                                          color: AppColors.textMuted,
                                        ),
                                      ),
                                    TextSpan(
                                      text: '➔ ${item.proposedValue}',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                        color: Color(0xFF047857),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              if (item.reason != null &&
                                  item.reason!.isNotEmpty)
                                Padding(
                                  padding: const EdgeInsets.only(top: 2),
                                  child: Text(
                                    'Reason: ${item.reason}',
                                    style: const TextStyle(
                                      fontSize: 11,
                                      color: AppColors.textMuted,
                                      fontStyle: FontStyle.italic,
                                    ),
                                  ),
                                ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  );
                }).toList(),
              ),
            ),
          ],
          if (cr.reviewComment != null && cr.reviewComment!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFEFF6FF),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFBFDBFE)),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    Icons.chat_bubble_outline_rounded,
                    size: 14,
                    color: Color(0xFF2563EB),
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Manager Feedback:',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF1E3A8A),
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          cr.reviewComment!,
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color(0xFF1E40AF),
                          ),
                        ),
                        if (cr.reviewedAt != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 2),
                            child: Text(
                              _formatDateTime(cr.reviewedAt),
                              style: const TextStyle(
                                fontSize: 10,
                                color: Color(0xFF60A5FA),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
